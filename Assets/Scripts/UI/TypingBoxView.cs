using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yazı kutusunun görüntüsü (IMGUI, kodla kurulur; sahneye dokunmaz). Steam görselindeki gibi: ekranın ortasının biraz altında
/// geniş, alçak beyaz kutu, solda yanıp sönen imleç, hemen altında kutu genişliğinde sayaç çubuğu, üst köşelerin dışında eğik
/// iki zar ve çevrede süzülen beyaz parıltılar. Resimler (ui_typebox, ui_timer_*, ui_caret_*, ui_die_*, ui_key_spark_*,
/// fx_word_burst_*) yoksa aynı düzen düz renkli kutularla çizilir.
/// Haller: zar atılıyor (küçük, zarlar döner), sıra sende (büyük, parlayan kutu), başkası yazıyor (küçük, soluk, "X yazıyor...").
/// Girdi ve kurallar <see cref="TypingBoxDisplay"/>'dedir; burası yalnızca çizer ve efektleri oynatır.
/// </summary>
public class TypingBoxView : MonoBehaviour
{
    /// <summary>Kutu kapanırken küçüleceği ölçek.</summary>
    const float HiddenScale = 0.85f;

    /// <summary>"Yazıyor..." noktalarının en fazla sayısı.</summary>
    const int MaxDots = 3;

    struct Spark { public Vector2 At; public float StartAt; public float Size; }
    struct FallingLetter { public string Letter; public Vector2 At; public float StartAt; public int FontSize; public float Spin; }

    GameConfig config;
    TurnViewSource runner;
    ITurnView turns;

    TypingBoxMode mode = TypingBoxMode.Hidden;
    TurnPhase lastPhase = TurnPhase.NotStarted;
    float scale = HiddenScale;
    float alpha;
    float openedAt = -1000f;
    float landedAt = -1000f;
    float keyAt = -1000f;
    float rejectAt = -1000f;
    float acceptAt = -1000f;
    string feedback = string.Empty;
    float feedbackUntil;

    readonly List<float> popAt = new List<float>();
    readonly List<Spark> sparks = new List<Spark>();
    readonly List<FallingLetter> falling = new List<FallingLetter>();
    readonly List<Vector2> letterCenters = new List<Vector2>();
    readonly List<string> letters = new List<string>();
    readonly GUIContent content = new GUIContent();
    GUIStyle letterStyle;
    GUIStyle lineStyle;
    Vector2 burstAt;
    bool dieSparked = true;

    /// <summary>Kutunun içindeki metin (yalnızca sıra sendeyken çizilir).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Kutunun son çizildiği yer (gizlenince son açık hali saklanır). Uçan harfler buradan çıkar.</summary>
    public Rect LastBoxRect { get; private set; }

    /// <summary>Son çizimde bir resim pikselinin ekran pikseli karşılığı (kutu ölçeği dahil).</summary>
    public float LastUnit { get; private set; } = 1f;

    /// <summary>Ekran ölçeği: bir resim pikseli kaç ekran pikseli (ekran yüksekliğine göre).</summary>
    public float PixelScale => Screen.height / Mathf.Max(1f, config != null ? config.typeBoxReferenceHeight : 270f);

    public void Bind(GameConfig gameConfig, TurnViewSource source)
    {
        config = gameConfig;
        runner = source;
        turns = source != null ? source.View : null;
        lastPhase = turns != null ? turns.Phase : TurnPhase.NotStarted;
    }

    // ---- Olaylar (TypingBoxDisplay çağırır) ----

    /// <summary>Kutu açıldı (sıra sende): punch ile büyür.</summary>
    public void NotifyOpened()
    {
        openedAt = Time.unscaledTime;
        popAt.Clear();
        feedback = string.Empty;
    }

    /// <summary><paramref name="index"/>. harf eklendi: harf "pop", kıvılcım, kutu titremesi.</summary>
    public void NotifyKey(int index)
    {
        float now = Time.unscaledTime;
        while (popAt.Count <= index) popAt.Add(-1000f);
        popAt[index] = now;
        keyAt = now;
        sparks.Add(new Spark { At = new Vector2(float.NaN, index), StartAt = now, Size = 1f }); // Yeri çizimde harften bulunur
    }

    /// <summary>Harf(ler) silindi: <paramref name="index"/>'ten başlayan harfler aşağı düşüp söner.</summary>
    public void NotifyDeleted(string removed, int index)
    {
        float now = Time.unscaledTime;
        List<string> gone = TypingFxMath.SplitLetters(removed);
        for (int i = 0; i < gone.Count; i++)
        {
            int at = index + i;
            if (at >= letterCenters.Count) break;
            falling.Add(new FallingLetter
            {
                Letter = gone[i], At = letterCenters[at], StartAt = now,
                FontSize = letterStyle != null ? letterStyle.fontSize : 20, Spin = at % 2 == 0 ? 1f : -1f,
            });
        }
        if (popAt.Count > index) popAt.RemoveRange(index, popAt.Count - index);
        keyAt = now;
    }

    /// <summary>Kelime reddedildi: kırmızı kutu, sallanma, harfler kırmızı dağılıp toplanır.</summary>
    public void NotifyRejected()
    {
        rejectAt = Time.unscaledTime;
    }

    /// <summary>Kelime kabul edildi: yeşil kutu ve kutudan çıkan patlama.</summary>
    public void NotifyAccepted()
    {
        acceptAt = Time.unscaledTime;
        burstAt = LastBoxRect.center;
    }

    /// <summary>Kutunun üstünde kırmızı hata yazısı (ör. "Bu kelime kullanıldı").</summary>
    public void ShowFeedback(string message, float seconds)
    {
        feedback = message ?? string.Empty;
        feedbackUntil = Time.unscaledTime + seconds;
    }

    // ---- Güncelleme ----

    void Update()
    {
        if (config == null || turns == null) return;
        float now = Time.unscaledTime;

        TurnPhase phase = turns.Phase;
        if (phase != lastPhase)
        {
            if (lastPhase == TurnPhase.Dice && phase == TurnPhase.Typing)
            {
                landedAt = now;
                dieSparked = false; // Zarlar durunca kıvılcım (yerleri çizimde bilinir)
            }
            lastPhase = phase;
        }

        TypingBoxMode next = TypingFxMath.ModeFor(phase, runner.IsLocal(turns.Writer));
        if (next != mode && next == TypingBoxMode.Typing) NotifyOpened();
        mode = next;

        float targetScale = mode == TypingBoxMode.Typing ? 1f : mode == TypingBoxMode.Hidden ? HiddenScale : config.typeBoxWatchScale;
        float targetAlpha = mode == TypingBoxMode.Typing ? 1f : mode == TypingBoxMode.Hidden ? 0f : config.typeBoxWatchAlpha;
        float follow = 1f - Mathf.Exp(-config.typeBoxModeSharpness * Time.unscaledDeltaTime);
        scale = Mathf.Lerp(scale, targetScale, follow);
        alpha = Mathf.Lerp(alpha, targetAlpha, follow);

        float sparkLife = Mathf.Max(0.2f, 4f * config.typeBoxSparkFrameSeconds);
        sparks.RemoveAll(s => now - s.StartAt > sparkLife);
        falling.RemoveAll(f => now - f.StartAt > config.typeBoxFallSeconds);
    }

    // ---- Çizim ----

    void OnGUI()
    {
        if (config == null || turns == null || Event.current.type != EventType.Repaint) return;
        GUI.depth = 5; // Maç sonu, ayarlar ve vinyetler kutunun üstünde
        EnsureStyles();

        float now = Time.unscaledTime;
        float p = PixelScale;
        bool visible = alpha > 0.01f;

        float punch = mode == TypingBoxMode.Typing
            ? UiTween.PunchIn(now - openedAt, config.uiTypingOpenSeconds, 0.7f, Easing.DefaultOvershoot)
            : 1f;
        float s = scale * punch;
        float u = p * s; // Bir resim pikseli (kutu ölçeğiyle)

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * config.typeBoxCenterY) + BoxShake(now, p);
        Vector2 size = new Vector2(config.typeBoxWidthPixels, config.typeBoxHeightPixels) * u;
        Rect box = new Rect(center - size * 0.5f, size);

        if (visible)
        {
            if (mode != TypingBoxMode.Hidden)
            {
                LastBoxRect = box;
                LastUnit = u;
            }
            DrawSparkles(box, u, now);
            DrawGlow(box, u, now);
            DrawFrame(box, u, now);
            DrawContent(box, u, now);
            DrawTimer(box, u, now);
            DrawDice(box, u, now);
            DrawHint(box, u, now);
        }

        DrawSparks(u, now);
        DrawFalling(now, p);
        DrawBurst(p, now);
    }

    void EnsureStyles()
    {
        if (letterStyle != null) return;
        Font font = GameFont.Get();
        letterStyle = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow, wordWrap = false };
        lineStyle = new GUIStyle { font = font, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow, wordWrap = false };
    }

    /// <summary>Tuş titremesi + red sallanması + son saniyelerin titremesi (ekran pikseli).</summary>
    Vector2 BoxShake(float now, float p)
    {
        float jitter = config.uiKeyJitterPixels;
        float x = UiTween.Shake(now - rejectAt, config.uiRejectShakeSeconds, config.uiRejectShakePixels, config.uiShakeFrequency)
                  + UiTween.Shake(now - keyAt, config.uiKeyJitterSeconds, jitter, 1f / Mathf.Max(0.001f, config.uiKeyJitterSeconds));
        float y = UiTween.Shake(now - keyAt + config.uiKeyJitterSeconds * 0.25f, config.uiKeyJitterSeconds, jitter * 0.6f,
                                1.5f / Mathf.Max(0.001f, config.uiKeyJitterSeconds));
        if (mode == TypingBoxMode.Typing)
        {
            float amp = TimerBarRules.ShakeAmplitude(turns.PhaseTimeLeft, config.uiCountdownWarnSeconds, config.typeBoxTimerShakePixels) * p * 0.5f;
            x += Mathf.Sin(now * 71f) * amp;
            y += Mathf.Sin(now * 53f + 1.3f) * amp;
        }
        return new Vector2(x, y);
    }

    /// <summary>Kutunun çevresinde süzülen küçük beyaz parıltı noktaları (artı biçimli, yanıp söner).</summary>
    void DrawSparkles(Rect box, float u, float now)
    {
        int count = config.typeBoxSparkleCount;
        float spread = config.typeBoxSparkleSpreadPixels * u;
        for (int i = 0; i < count; i++)
        {
            // Her nokta kendi açısında, kutu çevresinde yavaşça yukarı süzülür ve döngüye girer.
            float seed = i * 2.399f;
            float phase = Mathf.Repeat(now * config.typeBoxSparkleSpeed + i / (float)Mathf.Max(1, count), 1f);
            float side = Mathf.Cos(seed) >= 0f ? 1f : -1f;
            float x = box.center.x + side * (box.width * 0.5f + spread * (0.2f + 0.8f * Mathf.Abs(Mathf.Sin(seed * 1.7f))))
                      - side * (i % 3) * box.width * 0.18f;
            float y = box.yMax + spread * 0.5f - phase * (box.height + spread * 1.5f);
            float twinkle = Mathf.Sin(phase * Mathf.PI);
            Color c = new Color(1f, 1f, 1f, 0.9f * twinkle * alpha);
            float d = Mathf.Max(1f, Mathf.Round(u));
            UiSprites.Fill(new Rect(x - d * 0.5f, y - d * 1.5f, d, d * 3f), c);
            UiSprites.Fill(new Rect(x - d * 1.5f, y - d * 0.5f, d * 3f, d), c);
        }
    }

    /// <summary>Sıra sendeyken kutunun çevresinde nefes alan sarı parıltı.</summary>
    void DrawGlow(Rect box, float u, float now)
    {
        if (mode != TypingBoxMode.Typing || config.typeBoxGlowPixels <= 0f) return;
        float breathe = 0.5f + 0.5f * Mathf.Sin(now * config.typeBoxBreatheSpeed * 2f * Mathf.PI);
        Color c = config.typeBoxGlowColor;
        for (int ring = 2; ring >= 1; ring--)
        {
            float grow = config.typeBoxGlowPixels * u * ring * (0.6f + 0.4f * breathe);
            Rect r = new Rect(box.x - grow, box.y - grow, box.width + 2f * grow, box.height + 2f * grow);
            UiSprites.Fill(r, new Color(c.r, c.g, c.b, c.a * alpha * (0.35f + 0.65f * breathe) / ring));
        }
    }

    /// <summary>Kutunun çerçevesi: red/kabul anında kırmızı/yeşil resim, sıra sendeyken "sıcak" resim, yoksa düz kutu.</summary>
    void DrawFrame(Rect box, float u, float now)
    {
        bool bad = now - rejectAt < config.typeBoxResultSeconds;
        bool ok = !bad && now - acceptAt < config.typeBoxResultSeconds;
        Sprite sprite = null;
        if (bad) sprite = UiSprites.Get("ui_typebox_bad");
        else if (ok) sprite = UiSprites.Get("ui_typebox_ok");
        else if (mode == TypingBoxMode.Typing) sprite = UiSprites.Get("ui_typebox_hot");
        if (sprite == null) sprite = UiSprites.Get("ui_typebox");

        Color tint = new Color(1f, 1f, 1f, alpha);
        if (sprite != null && (bad || ok) && sprite == UiSprites.Get("ui_typebox"))
            tint = Color.Lerp(Color.white, bad ? config.typeBoxBadColor : config.typeBoxOkColor, 0.5f); // Renkli resim yoksa boyanır
        tint.a = alpha;
        if (UiSprites.NineSlice(box, sprite, tint, u, config.typeBoxBorderPixels)) return;

        // Resim yok: Steam görselindeki kutu (koyu kenar, beyaz yüz, altta açık gri şerit, üstte açık mavi çizgi).
        Color edge = bad ? config.typeBoxBadColor : ok ? config.typeBoxOkColor : config.typeBoxOutlineColor;
        float e = Mathf.Max(1f, Mathf.Round(u)) * (bad || ok ? 2f : 1f);
        UiSprites.Fill(box, WithAlpha(edge, alpha));
        Rect inner = new Rect(box.x + e, box.y + e, box.width - 2f * e, box.height - 2f * e);
        UiSprites.Fill(inner, WithAlpha(config.typeBoxFaceColor, alpha));
        UiSprites.Fill(new Rect(inner.x, inner.yMax - 2f * u, inner.width, 2f * u), WithAlpha(config.typeBoxShadeColor, alpha));
        UiSprites.Fill(new Rect(inner.x + u, inner.y + u, inner.width - 2f * u, u), WithAlpha(config.typeBoxHighlightColor, alpha));
    }

    /// <summary>Kutunun içi: sıra sendeyken harfler ve imleç, değilse "X yazıyor..." / "Zar atılıyor...".</summary>
    void DrawContent(Rect box, float u, float now)
    {
        float left = box.x + config.typeBoxTextPaddingPixels * u;
        float midY = box.y + (box.height - 2f * u) * 0.5f; // Alt gri şeridin üstünde ortalı

        if (mode == TypingBoxMode.Typing)
        {
            DrawLetters(box, left, midY, u, now);
            return;
        }

        letterCenters.Clear();
        string message = mode == TypingBoxMode.Watching && turns.Writer != null
            ? Loc.F("typing.writing_other", turns.Writer.Name)
            : Loc.T("typing.rolling");
        message += new string('.', TypingFxMath.DotCount(now, config.typeBoxDotsPeriodSeconds, MaxDots));

        lineStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(config.typeBoxFontPixels * u * 0.8f));
        content.text = message;
        float width = lineStyle.CalcSize(content).x;
        float maxWidth = box.width - 2f * config.typeBoxTextPaddingPixels * u;
        if (width > maxWidth && width > 0f)
            lineStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(lineStyle.fontSize * maxWidth / width));
        Color c = WithAlpha(config.typeBoxWatchTextColor, alpha);
        lineStyle.normal.textColor = c;
        GUI.Label(new Rect(left, midY - box.height * 0.5f, maxWidth, box.height), message, lineStyle);
    }

    void DrawLetters(Rect box, float left, float midY, float u, float now)
    {
        letters.Clear();
        letters.AddRange(TypingFxMath.SplitLetters(Text));
        letterCenters.Clear();

        // Yazı boyu: sığmazsa en çok typeBoxMinFontScale katına küçülür.
        int baseSize = Mathf.Max(1, Mathf.RoundToInt(config.typeBoxFontPixels * u));
        letterStyle.fontSize = baseSize;
        float total = 0f;
        foreach (string letter in letters) total += LetterWidth(letter);
        float maxWidth = box.width - 2f * config.typeBoxTextPaddingPixels * u - config.typeBoxCaretWidthPixels * u * 2f;
        if (total > maxWidth && total > 0f)
        {
            float shrink = Mathf.Max(config.typeBoxMinFontScale, maxWidth / total);
            letterStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseSize * shrink));
        }

        float rejectAge = now - rejectAt;
        bool rejecting = rejectAge < config.typeBoxScatterSeconds;
        Color textColor = rejecting ? config.typeBoxBadTextColor : config.typeBoxTextColor;
        float height = letterStyle.fontSize * 1.3f;
        float x = left;
        for (int i = 0; i < letters.Count; i++)
        {
            float w = LetterWidth(letters[i]);
            Vector2 c = new Vector2(x + w * 0.5f, midY);
            letterCenters.Add(c);
            Vector2 scatter = TypingFxMath.Scatter(i, letters.Count, rejectAge, config.typeBoxScatterSeconds, config.typeBoxScatterPixels * u);

            float pop = i < popAt.Count
                ? UiTween.PunchIn(now - popAt[i], config.uiLetterPopSeconds, config.uiLetterPopStartScale, config.uiLetterPopOvershoot)
                : 1f;
            Rect r = new Rect(c.x - w * 0.5f + scatter.x, c.y - height * 0.5f + scatter.y, w, height);
            Matrix4x4 old = GUI.matrix;
            if (!Mathf.Approximately(pop, 1f)) GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), r.center);
            letterStyle.normal.textColor = WithAlpha(textColor, alpha);
            GUI.Label(r, letters[i], letterStyle);
            GUI.matrix = old;
            x += w;
        }

        DrawCaret(x, midY, u, now, letters.Count == 0);
    }

    float LetterWidth(string letter)
    {
        content.text = letter;
        return letterStyle.CalcSize(content).x;
    }

    /// <summary>İmleç: son harfin arkasında yanıp söner (tuşa basınca kısa süre sabit yanar). Boşken arkasında soluk çizgi.</summary>
    void DrawCaret(float x, float midY, float u, float now, bool empty)
    {
        float blink = config.typeBoxCaretBlinkSeconds;
        bool typedRecently = now - keyAt < blink;
        int frame = typedRecently ? 0 : Mathf.FloorToInt(now / blink) % 2;
        float h = letterStyle.fontSize * 0.95f;
        float w = config.typeBoxCaretWidthPixels * u;
        Rect r = new Rect(x + u, midY - h * 0.5f, w, h);

        Sprite sprite = UiSprites.Get("ui_caret_" + frame);
        if (sprite != null) UiSprites.Draw(r, sprite, new Color(1f, 1f, 1f, alpha));
        else if (frame == 0) UiSprites.Fill(r, WithAlpha(config.typeBoxOutlineColor, alpha));

        if (empty) // Steam görselindeki gibi imlecin arkasında harfsiz yazı çizgisi
            UiSprites.Fill(new Rect(r.xMax + 3f * u, r.yMax - u, 9f * u, u), WithAlpha(config.typeBoxShadeColor, alpha));
    }

    /// <summary>Kutunun hemen altında, kutu genişliğinde sayaç: kalan süreyle kısalır, yeşil → sarı → kırmızı, son saniyelerde titrer.</summary>
    void DrawTimer(Rect box, float u, float now)
    {
        if (mode != TypingBoxMode.Typing && mode != TypingBoxMode.Watching) return;

        float left = turns.PhaseTimeLeft;
        float fraction = TimerBarRules.Fraction(left, config.typingSeconds);
        float shake = TimerBarRules.ShakeAmplitude(left, config.uiCountdownWarnSeconds, config.typeBoxTimerShakePixels) * u;
        Vector2 offset = new Vector2(Mathf.Sin(now * 90f) * shake, Mathf.Sin(now * 67f + 0.7f) * shake * 0.5f);

        Rect bar = new Rect(box.x + offset.x, box.yMax + config.typeBoxTimerGapPixels * u + offset.y,
                            box.width, config.typeBoxTimerHeightPixels * u);
        Color fill = TimerBarRules.ColorFor(fraction, config.typeBoxTimerGreen, config.typeBoxTimerYellow, config.typeBoxTimerRed,
                                            config.typeBoxTimerYellowAt, config.typeBoxTimerRedAt);

        if (!UiSprites.NineSlice(bar, UiSprites.Get("ui_timer_bg"), new Color(1f, 1f, 1f, alpha), u, 1))
        {
            UiSprites.Fill(bar, WithAlpha(config.typeBoxOutlineColor, alpha));
            UiSprites.Fill(new Rect(bar.x + u, bar.y + u, bar.width - 2f * u, bar.height - 2f * u), WithAlpha(config.typeBoxTimerBackColor, alpha));
        }

        float inset = config.typeBoxTimerInsetPixels * u;
        Rect inner = new Rect(bar.x + inset, bar.y + inset, Mathf.Max(0f, (bar.width - 2f * inset) * fraction), bar.height - 2f * inset);
        if (inner.width <= 0f) return;

        // Normalde Steam görselindeki sarı-turuncu dolgu; süre azalınca beyaz dolgu sarıdan kırmızıya boyanır.
        Sprite gold = UiSprites.Get("ui_timer_fill_gold");
        if (gold != null && fraction > config.typeBoxTimerRedAt)
        {
            UiSprites.NineSlice(inner, gold, new Color(1f, 1f, 1f, alpha), u, 1);
            return;
        }
        if (gold != null)
        {
            float redAt = Mathf.Max(0.001f, config.typeBoxTimerRedAt);
            fill = Color.Lerp(config.typeBoxTimerRed, config.typeBoxTimerYellow, Mathf.Clamp01(fraction / redAt) * 0.6f);
        }
        if (!UiSprites.NineSlice(inner, UiSprites.Get("ui_timer_fill"), WithAlpha(fill, alpha), u, 1))
        {
            UiSprites.Fill(inner, WithAlpha(fill, alpha));
            Color light = Color.Lerp(fill, Color.white, 0.45f);
            UiSprites.Fill(new Rect(inner.x, inner.y, inner.width, u), WithAlpha(light, alpha)); // Üst parlak çizgi
        }
    }

    /// <summary>
    /// Kutunun üst köşelerinin biraz dışında, eğik iki zar (Steam görselindeki gibi). Zar atılırken dönerek kare değiştirir,
    /// durunca sıçrar ve kıvılcım çıkarır. Sayı süstür (bkz. <see cref="DieFaces.LandedFace"/>).
    /// </summary>
    void DrawDice(Rect box, float u, float now)
    {
        float d = config.typeBoxDieSizePixels * u;
        bool rolling = mode == TypingBoxMode.Rolling;
        float bump = UiTween.Bump(now - landedAt, config.typeBoxDieLandSeconds, config.typeBoxDieLandBump);

        for (int side = 0; side < 2; side++)
        {
            // Steam görselindeki yerler (16 piksellik zara göre): sol zar sol üst köşenin dışında biraz aşağıda, sağ zar sağ üstte.
            float k = d / 16f;
            float h = d * DieAspect();
            Rect r = side == 0
                ? new Rect(box.x - d + 2f * k, box.y - h + 6f * k, d, h)
                : new Rect(box.xMax - 4f * k, box.y - h + 2f * k, d, h);
            float tilt = (side == 0 ? -1f : 1f) * config.typeBoxDieTiltDegrees;

            Sprite sprite;
            float angle = tilt;
            if (rolling)
            {
                float t = now * config.typeBoxDieRollFps + side * 2.3f;
                sprite = UiSprites.FrameAt("ui_die_roll", t / config.typeBoxDieRollFps + side * 0.07f, 1f / config.typeBoxDieRollFps, true);
                if (sprite == null) sprite = DieFaces.SpriteFor(Mathf.FloorToInt(t) % 6 + 1, config.uiDiceColor, config.uiDicePipColor);
                angle += Mathf.Sin(now * 22f + side * 2f) * 25f;
                r.y -= Mathf.Abs(Mathf.Sin(now * 14f + side)) * 3f * u; // Zıplayarak döner
            }
            else sprite = DieFaces.SpriteFor(DieFaces.LandedFace(turns.TurnNumber, side), config.uiDiceColor, config.uiDicePipColor);

            if (bump > 1f) r = UiDraw.Shrink(r, bump);
            UiSprites.Draw(r, sprite, new Color(1f, 1f, 1f, alpha), angle);

            if (!dieSparked && mode != TypingBoxMode.Hidden)
                sparks.Add(new Spark { At = r.center, StartAt = now, Size = 1.3f });
        }
        dieSparked = true;
    }

    /// <summary>Zar resminin boy/en oranı (resim yoksa kare).</summary>
    static float DieAspect()
    {
        Sprite die = UiSprites.Get("ui_die_1");
        return die != null && die.rect.width > 0f ? die.rect.height / die.rect.width : 1f;
    }

    /// <summary>Kutunun üstünde: sıra sendeyken ipucu, hata olunca kırmızı sebep.</summary>
    void DrawHint(Rect box, float u, float now)
    {
        bool hasFeedback = feedback.Length > 0 && now < feedbackUntil;
        if (mode != TypingBoxMode.Typing && !hasFeedback) return;

        string text = hasFeedback ? feedback : Loc.T("typing.hint");
        lineStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(config.typeBoxHintFontPixels * PixelScale));
        lineStyle.alignment = TextAnchor.LowerCenter;
        Rect r = new Rect(box.x - box.width, box.y - config.typeBoxDieSizePixels * u - lineStyle.fontSize * 1.6f, box.width * 3f, lineStyle.fontSize * 1.5f);
        Color color = hasFeedback ? config.typeBoxBadTextColor : config.typeBoxHintColor;
        UiSprites.OutlinedLabel(r, text, lineStyle, WithAlpha(color, alpha), WithAlpha(config.typeBoxOutlineColor, alpha * 0.9f),
                                Mathf.Max(1f, Mathf.Round(PixelScale * 0.5f)));
        lineStyle.alignment = TextAnchor.MiddleLeft;
    }

    /// <summary>Tuş ve zar kıvılcımları: ui_key_spark kareleri, yoksa dışa saçılan dört sarı kare.</summary>
    void DrawSparks(float u, float now)
    {
        foreach (Spark spark in sparks)
        {
            Vector2 at = spark.At;
            if (float.IsNaN(at.x))
            {
                int index = (int)at.y;
                if (index < 0 || index >= letterCenters.Count) continue;
                at = letterCenters[index];
            }
            float age = now - spark.StartAt;
            float size = config.typeBoxSparkSizePixels * u * spark.Size;
            Sprite frame = UiSprites.FrameAt("ui_key_spark", age, config.typeBoxSparkFrameSeconds, false);
            if (frame != null)
            {
                UiSprites.Draw(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), frame, Color.white);
                continue;
            }

            float life = Mathf.Max(0.05f, 4f * config.typeBoxSparkFrameSeconds);
            float t = Mathf.Clamp01(age / life);
            float dist = size * 0.6f * Easing.OutCubic(t);
            float dot = Mathf.Max(1f, u);
            Color c = new Color(1f, 0.9f, 0.3f, 1f - t);
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                UiSprites.Fill(new Rect(at.x + Mathf.Cos(a) * dist - dot * 0.5f, at.y + Mathf.Sin(a) * dist - dot * 0.5f, dot, dot), c);
            }
        }
    }

    /// <summary>Silinen harfler: aşağı düşer, döner ve söner.</summary>
    void DrawFalling(float now, float p)
    {
        foreach (FallingLetter f in falling)
        {
            float age = now - f.StartAt;
            Vector2 fall = TypingFxMath.Fall(age, config.typeBoxFallSeconds, config.typeBoxFallGravityPixels * p);
            if (fall.y <= 0f) continue;
            letterStyle.fontSize = f.FontSize;
            float h = f.FontSize * 1.3f;
            Rect r = new Rect(f.At.x - h, f.At.y - h * 0.5f + fall.x, h * 2f, h);
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(f.Spin * age * 160f, r.center);
            letterStyle.normal.textColor = WithAlpha(config.typeBoxWatchTextColor, fall.y);
            GUI.Label(r, f.Letter, letterStyle);
            GUI.matrix = old;
        }
    }

    /// <summary>Kabulde kutudan çıkan patlama: fx_word_burst kareleri, yoksa dışa saçılan sarı kareler.</summary>
    void DrawBurst(float p, float now)
    {
        float age = now - acceptAt;
        if (age < 0f || age > 1f) return;
        float size = config.typeBoxBurstSizePixels * p;
        Sprite frame = UiSprites.FrameAt("fx_word_burst", age, config.typeBoxBurstFrameSeconds, false);
        if (frame != null)
        {
            UiSprites.Draw(new Rect(burstAt.x - size * 0.5f, burstAt.y - size * 0.5f, size, size), frame, Color.white);
            return;
        }

        float life = 5f * config.typeBoxBurstFrameSeconds;
        if (age > life) return;
        float t = age / life;
        float dot = Mathf.Max(2f, 2f * p);
        Color c = new Color(1f, 0.88f, 0.3f, 1f - t);
        for (int k = 0; k < 14; k++)
        {
            float a = k / 14f * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a) * 1.8f, Mathf.Sin(a));
            Vector2 at = burstAt + dir * (size * 0.5f * Easing.OutCubic(t));
            UiSprites.Fill(new Rect(at.x - dot * 0.5f, at.y - dot * 0.5f, dot, dot), c);
        }
    }

    // ---- Kelime büyüsü için ----

    /// <summary>
    /// <paramref name="text"/> kutuda yazılı olsaydı harflerinin ekrandaki ortaları (kutunun son açık halinde) ve yazı boyu.
    /// OnGUI içinde çağrılmalıdır (yazı ölçümü için).
    /// </summary>
    public int LetterSlots(string text, List<Vector2> into)
    {
        into.Clear();
        EnsureStyles();
        Rect box = LastBoxRect;
        float u = LastUnit;
        if (box.width <= 0f)
        {
            float p = PixelScale;
            Vector2 size = new Vector2(config.typeBoxWidthPixels, config.typeBoxHeightPixels) * p;
            box = new Rect(new Vector2(Screen.width * 0.5f, Screen.height * config.typeBoxCenterY) - size * 0.5f, size);
            u = p;
        }

        List<string> parts = TypingFxMath.SplitLetters(text);
        letterStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(config.typeBoxFontPixels * u));
        float x = box.x + config.typeBoxTextPaddingPixels * u;
        float midY = box.y + (box.height - 2f * u) * 0.5f;
        foreach (string letter in parts)
        {
            float w = LetterWidth(letter);
            into.Add(new Vector2(x + w * 0.5f, midY));
            x += w;
        }
        return letterStyle.fontSize;
    }

    static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
}
