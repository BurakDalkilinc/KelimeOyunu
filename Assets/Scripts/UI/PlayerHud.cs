using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sol üstteki piksel gösterge (IMGUI): panel içinde kalpler (kırılır, büyüyerek dolar, düşük canda son kalp atar), itiş simgesi
/// + bekleme çubuğu + "BOŞLUK" tuş kapağı, durum simgeleri (süre çubuğu, sayı rozeti, bitmek üzereyken yanıp söner).
/// Elenince kalplerin yerine "HAYALET" çıkar. Bilgiyi tur kaynağının <see cref="TurnViewSource.FillHud"/>'undan okur, yalnızca çizer;
/// hesaplar <see cref="HudRules"/>, <see cref="HeartRowAnimator"/>, <see cref="DurationGauge"/>'tedir. Açıkken eski düz yazılar
/// (<see cref="HealthDisplay"/>, <see cref="ShoveDisplay"/>, <see cref="StatusEffectDisplay"/>) kendini gizler.
/// Tur kaynağının yanına kendiliğinden eklenir; sahneye elle koymak gerekmez. Resimler yoksa düz şekillere düşer.
/// </summary>
public class PlayerHud : MonoBehaviour
{
    const string HeartFullSprite = "ui_heart_full";
    const string HeartHalfSprite = "ui_heart_half";
    const string HeartEmptySprite = "ui_heart_empty";
    const string HeartBreakPrefix = "ui_heart_break_";
    const int HeartBreakFrames = 4;
    const string PanelSprite = "ui_hud_panel";
    const string ShoveSprite = "ui_icon_shove";
    const string ShoveCooldownSprite = "ui_icon_shove_cd";
    const string KeySpaceSprite = "ui_key_space";
    const string GhostSprite = "ui_status_ghost";
    const string GhostFallbackSprite = "hayalet";

    /// <summary>IMGUI çizim derinliği: küçük olan üstte; gösterge ekran kenarı parlamalarının üstünde kalsın.</summary>
    const int GuiDepth = -5;

    static PlayerHud current;

    /// <summary>Gösterge şu an çalışıyor mu? Eski düz yazılar ve ağ panelinin can yazısı buna bakıp kendini gizler.</summary>
    public static bool IsShowing => current != null && current.isActiveAndEnabled;

    // ---- Sahnedeki tur kaynaklarına kendiliğinden eklenme ----

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => current = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToSources();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToSources();

    static void AttachToSources()
    {
        foreach (TurnViewSource found in FindObjectsByType<TurnViewSource>(FindObjectsInactive.Include))
            if (found.GetComponent<PlayerHud>() == null) found.gameObject.AddComponent<PlayerHud>();
    }

    // ---- Durum ----

    TurnViewSource source;
    GameConfig config;
    readonly HudSnapshot hud = new HudSnapshot();
    readonly HeartRowAnimator hearts = new HeartRowAnimator();
    readonly DurationGauge shoveGauge = new DurationGauge();
    readonly Dictionary<HudStatusKind, DurationGauge> statusGauges = new Dictionary<HudStatusKind, DurationGauge>();
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    readonly HashSet<HudStatusKind> seenKinds = new HashSet<HudStatusKind>();
    bool shoveWasCooling;
    float shoveReadyAt = -1000f;

    // Stiller ölçek değişince yeniden kurulur.
    int styleScale = -1;
    GUIStyle panelStyle;
    GUIStyle keyStyle;
    GUIStyle textStyle;
    GUIStyle smallStyle;
    GUIStyle centerSmallStyle;

    void Awake()
    {
        source = GetComponent<TurnViewSource>();
        config = GameConfigLoader.Get();
    }

    void OnEnable()
    {
        if (current == null || !current.isActiveAndEnabled) current = this;
    }

    void OnDisable()
    {
        if (current == this) current = null;
    }

    void Update()
    {
        if (current == null || !current.isActiveAndEnabled) current = this;
        if (current != this || source == null || config == null) return;

        source.FillHud(hud);
        float now = Time.unscaledTime;

        if (hud.HasPlayer && hud.Alive) hearts.Update(hud.Health, hud.MaxHealth, config.hudHealthPerHeart, now);
        else hearts.Reset();

        // İtiş: bekleme bitince kısa "pop".
        bool cooling = hud.ShowShove && !hud.Armed && hud.ShoveLeft > 0f;
        shoveGauge.Update(cooling ? hud.ShoveLeft : 0f);
        if (shoveWasCooling && !cooling) shoveReadyAt = now;
        shoveWasCooling = cooling;

        // Etki süre çubukları: görünen türler güncellenir, kaybolanlar sıfırlanır.
        seenKinds.Clear();
        foreach (HudStatus status in hud.Statuses)
        {
            seenKinds.Add(status.Kind);
            GaugeFor(status.Kind).Update(status.HasTime ? status.TimeLeft : 0f);
        }
        foreach (KeyValuePair<HudStatusKind, DurationGauge> pair in statusGauges)
            if (!seenKinds.Contains(pair.Key)) pair.Value.Update(0f);
    }

    DurationGauge GaugeFor(HudStatusKind kind)
    {
        if (!statusGauges.TryGetValue(kind, out DurationGauge gauge))
        {
            gauge = new DurationGauge();
            statusGauges[kind] = gauge;
        }
        return gauge;
    }

    // ---- Çizim ----

    void OnGUI()
    {
        if (current != this || config == null || !hud.HasPlayer) return;
        if (Event.current.type != EventType.Repaint) return;
        if (source != null && source.View != null && source.View.Phase == TurnPhase.MatchOver) return; // Maç sonu ekranı kaplar

        GUI.depth = GuiDepth;
        int s = HudRules.PixelScale(Screen.height, config.hudReferenceHeight, config.hudMinScale, config.hudMaxScale);
        EnsureStyles(s);
        float now = Time.unscaledTime;

        // Ölçüler resim pikseli (u); ekrana çizerken s ile çarpılır.
        int pad = config.hudPadding;
        int rowGap = config.hudRowGap;
        int iconBlock = config.hudIconSize + 1 + config.hudBarHeight; // simge + 1 piksel boşluk + çubuk
        int statusBlock = config.hudStatusIconSize + 1 + config.hudBarHeight;

        // Satır boyları
        Vector2Int heartsSize = hud.Alive ? HeartsBlockSize() : GhostBlockSize(s);
        bool showShove = hud.Alive && hud.ShowShove;
        int keyWidth = showShove ? KeyCapWidth(s) : 0;
        int shoveWidth = showShove ? config.hudIconSize + config.hudIconGap + keyWidth : 0;
        int statusCount = hud.Alive ? hud.Statuses.Count : 0;
        int statusWidth = statusCount > 0 ? statusCount * config.hudStatusIconSize + (statusCount - 1) * config.hudIconGap : 0;

        int contentWidth = Mathf.Max(heartsSize.x, Mathf.Max(shoveWidth, statusWidth));
        int contentHeight = heartsSize.y;
        if (showShove) contentHeight += rowGap + iconBlock;
        if (statusCount > 0) contentHeight += rowGap + statusBlock;

        int originX = config.hudMargin, originY = config.hudMargin;
        Rect panel = ToScreen(originX, originY, contentWidth + 2 * pad, contentHeight + 2 * pad, s);
        DrawPanel(panel, s);

        int x = originX + pad, y = originY + pad;
        if (hud.Alive) DrawHearts(x, y, s, now);
        else DrawGhost(x, y, heartsSize.y, s);
        y += heartsSize.y;

        if (showShove)
        {
            y += rowGap;
            DrawShove(x, y, keyWidth, s, now);
            y += iconBlock;
        }

        if (statusCount > 0)
        {
            y += rowGap;
            DrawStatuses(x, y, s, now);
        }

        // Seçilme şansı: panelin hemen altında küçük yazı.
        if (hud.Alive && !string.IsNullOrEmpty(hud.ChanceText))
        {
            Rect line = ToScreen(originX + 1, originY + contentHeight + 2 * pad + 1, 400, 16, s);
            Shadowed(line, hud.ChanceText, smallStyle, config.hudDimTextColor, s);
        }
    }

    static Rect ToScreen(float x, float y, float w, float h, int s) => new Rect(x * s, y * s, w * s, h * s);

    void DrawPanel(Rect panel, int s)
    {
        if (panelStyle != null)
        {
            UiDraw.Frame(panel, panelStyle, config.hudPanelFallbackColor, s);
            return;
        }
        UiDraw.Outline(panel, config.hudPanelFallbackEdge, s);
        UiDraw.Fill(panel, config.hudPanelFallbackColor);
    }

    // ---- Kalpler ----

    Vector2Int HeartsBlockSize()
    {
        int count = HudRules.HeartCount(hud.MaxHealth, config.hudHealthPerHeart);
        int perRow = Mathf.Clamp(count, 1, config.hudHeartsPerRow);
        int rows = (count + perRow - 1) / perRow;
        int size = config.hudHeartSize, gap = config.hudHeartGap;
        return new Vector2Int(perRow * size + (perRow - 1) * gap, rows * size + (rows - 1) * gap);
    }

    void DrawHearts(int x, int y, int s, float now)
    {
        HeartFill[] fills = hearts.Fills;
        int size = config.hudHeartSize, gap = config.hudHeartGap;
        int perRow = Mathf.Max(1, config.hudHeartsPerRow);
        int beating = HitFeelDirector.LowHealthPulse > 0f ? HudRules.LastFilledHeart(fills) : -1;

        // Vurulunca sallanma ve kırmızımsı parlama (eski can yazısındaki his), düşük canda kalp atışı.
        float age = HitFeelDirector.LocalHitAge;
        float seconds = config.healthShakeSeconds;
        float hurt = seconds > 0f && age < seconds && !HitFeelDirector.LastLocalHitBlocked ? 1f - age / seconds : 0f;
        float shake = hurt * hurt * config.healthShakePixels * SettingsService.ScreenShakeScale;
        float shakeX = HudRules.SnapToPixel(Mathf.Sin(age * 70f) * shake, s);
        float shakeY = HudRules.SnapToPixel(Mathf.Cos(age * 53f) * shake * 0.4f, s);
        float red = Mathf.Max(hurt, HitFeelDirector.LowHealthPulse * 0.8f) * 0.6f;
        Color tint = Color.Lerp(Color.white, config.healthHurtColor, red);

        Color old = GUI.color;
        for (int i = 0; i < fills.Length; i++)
        {
            int cx = x + (i % perRow) * (size + gap);
            int cy = y + (i / perRow) * (size + gap);
            Rect cell = ToScreen(cx, cy, size, size, s);
            cell.x += shakeX;
            cell.y += shakeY;

            GUI.color = tint;
            DrawHeartSprite(cell, HeartFill.Empty, 1f);

            float breakAge = hearts.BreakAge(i, now);
            int frame = HudRules.BreakFrame(breakAge, config.hudHeartBreakSeconds, HeartBreakFrames);
            if (frame >= 0)
            {
                Sprite breakSprite = Find(HeartBreakPrefix + frame);
                if (breakSprite != null) DrawCrisp(cell, breakSprite, 1f);
                else DrawHeartSprite(cell, hearts.BrokenFrom(i), 1f - breakAge / config.hudHeartBreakSeconds); // Resim yoksa söner
                if (fills[i] == HeartFill.Half) DrawHeartSprite(cell, HeartFill.Half, 1f);
                continue;
            }

            if (fills[i] == HeartFill.Empty) continue;

            float scale = 1f;
            float fillAge = hearts.FillAge(i, now);
            if (fillAge < config.hudHeartFillSeconds) scale = Easing.OutBack(fillAge / config.hudHeartFillSeconds);
            if (i == beating) scale *= 1f + HitFeelDirector.LowHealthPulse * config.hudLowHealthBeatScale;
            DrawHeartSprite(ScaledCell(cx, cy, size, scale, s, shakeX, shakeY), fills[i], 1f);
        }
        GUI.color = old;

        DrawShards(fills, x, y, s, now);
    }

    /// <summary>Kalbi ortasından büyütür/küçültür; boyu tam resim pikseline yuvarlanır ki pikseller bozulmasın.</summary>
    static Rect ScaledCell(int cx, int cy, int size, float scale, int s, float shakeX, float shakeY)
    {
        int scaled = Mathf.Max(0, Mathf.RoundToInt(size * scale));
        float offset = (size - scaled) * 0.5f;
        float left = HudRules.SnapToPixel((cx + offset) * s, s) + shakeX;
        float top = HudRules.SnapToPixel((cy + offset) * s, s) + shakeY;
        return new Rect(left, top, scaled * s, scaled * s);
    }

    void DrawHeartSprite(Rect cell, HeartFill fill, float alpha)
    {
        if (alpha <= 0f) return;
        string name = fill == HeartFill.Full ? HeartFullSprite : fill == HeartFill.Half ? HeartHalfSprite : HeartEmptySprite;
        Sprite sprite = Find(name);
        if (sprite != null)
        {
            DrawCrisp(cell, sprite, alpha);
            return;
        }

        // Yedek: düz kare kalp (dolu kırmızı, boş koyu, yarım sol yarısı kırmızı).
        Color tint = GUI.color;
        Color empty = config.hudHeartEmptyColor * tint;
        Color full = config.hudHeartFullColor * tint;
        empty.a *= alpha;
        full.a *= alpha;
        Rect inner = UiDraw.Shrink(cell, 0.85f);
        if (fill == HeartFill.Empty) UiDraw.Fill(inner, empty);
        else if (fill == HeartFill.Full) UiDraw.Fill(inner, full);
        else
        {
            UiDraw.Fill(inner, empty);
            UiDraw.Fill(new Rect(inner.x, inner.y, inner.width * 0.5f, inner.height), full);
        }
    }

    /// <summary>Kırılma kareleri bitince kalpten küçük parçalar fırlayıp düşer.</summary>
    void DrawShards(HeartFill[] fills, int x, int y, int s, float now)
    {
        int count = config.hudHeartShardCount;
        if (count <= 0 || config.hudHeartShardSeconds <= 0f) return;

        int size = config.hudHeartSize, gap = config.hudHeartGap;
        int perRow = Mathf.Max(1, config.hudHeartsPerRow);
        int shard = config.hudHeartShardSize;
        for (int i = 0; i < fills.Length; i++)
        {
            float t = hearts.BreakAge(i, now) - config.hudHeartBreakSeconds;
            if (t < 0f || t >= config.hudHeartShardSeconds) continue;

            float fade = 1f - t / config.hudHeartShardSeconds;
            Color color = config.hudHeartFullColor;
            color.a *= fade;
            float centerX = x + (i % perRow) * (size + gap) + size * 0.5f;
            float centerY = y + (i / perRow) * (size + gap) + size * 0.5f;
            for (int k = 0; k < count; k++)
            {
                Vector2 offset = HudRules.ShardOffset(k, count, t, config.hudHeartShardSpeed, config.hudHeartShardGravity);
                float px = Mathf.Round(centerX + offset.x - shard * 0.5f);
                float py = Mathf.Round(centerY + offset.y - shard * 0.5f);
                UiDraw.Fill(ToScreen(px, py, shard, shard, s), color);
            }
        }
    }

    // ---- Hayalet ----

    Vector2Int GhostBlockSize(int s)
    {
        int textWidth = TextWidth(Loc.T("hud.ghost"), textStyle, s);
        return new Vector2Int(config.hudHeartSize + config.hudIconGap + textWidth, config.hudHeartSize);
    }

    void DrawGhost(int x, int y, int height, int s)
    {
        Rect icon = ToScreen(x, y, config.hudHeartSize, config.hudHeartSize, s);
        Sprite ghost = Find(GhostSprite) ?? Find(GhostFallbackSprite);
        Color old = GUI.color;
        GUI.color = config.hudGhostColor;
        if (ghost != null) DrawCrisp(icon, ghost, 1f);
        else UiDraw.Fill(UiDraw.Shrink(icon, 0.8f), config.hudGhostColor);
        GUI.color = old;

        Rect label = ToScreen(x + config.hudHeartSize + config.hudIconGap, y, 200, height, s);
        Shadowed(label, Loc.T("hud.ghost"), textStyle, config.hudGhostColor, s);
    }

    // ---- İtiş ----

    int KeyCapWidth(int s)
    {
        return TextWidth(Loc.T("hud.key_space"), smallStyle, s) + 2 * (config.hudKeyCapPadding + config.hudKeyCapBorder);
    }

    void DrawShove(int x, int y, int keyWidth, int s, float now)
    {
        int size = config.hudIconSize;
        bool cooling = !hud.Armed && hud.ShoveLeft > 0f;
        Rect iconRect = ToScreen(x, y, size, size, s);

        // Hazır olunca: büyüyüp lastik gibi oturur ve arkasında sönen bir parlama.
        float popAge = now - shoveReadyAt;
        bool popping = !cooling && popAge >= 0f && popAge < config.hudShoveReadyPopSeconds;
        float scale = popping ? UiTween.Bump(popAge, config.hudShoveReadyPopSeconds, config.hudShoveReadyPopScale) : 1f;
        if (popping)
        {
            Color glow = config.hudReadyColor;
            glow.a *= 1f - popAge / config.hudShoveReadyPopSeconds;
            UiDraw.Fill(ScaledCell(x, y, size, scale + 0.15f, s, 0f, 0f), glow);
        }

        Sprite sprite;
        Color tint = Color.white;
        if (hud.Armed)
        {
            // Piksel net kalsın diye önce tür simgesi (göstergeye göre çizilmiş), yoksa silahın kendi resmi.
            sprite = Find(HudRules.IconName(HudStatusKind.Weapon)) ?? Find(hud.WeaponSprite) ?? Find(ShoveSprite);
        }
        else if (cooling)
        {
            sprite = Find(ShoveCooldownSprite);
            if (sprite == null)
            {
                sprite = Find(ShoveSprite);
                tint = config.hudCooldownTint;
            }
        }
        else sprite = Find(ShoveSprite);

        Rect drawn = ScaledCell(x, y, size, scale, s, 0f, 0f);
        Color old = GUI.color;
        GUI.color = tint;
        if (sprite != null) DrawCrisp(drawn, sprite, 1f);
        else UiDraw.Fill(UiDraw.Shrink(drawn, 0.8f), cooling ? config.hudCooldownTint : config.hudBarFillColor);
        GUI.color = old;

        if (hud.Armed && hud.WeaponShots >= 0) DrawBadge(iconRect, hud.WeaponShots, s);

        // Çubuk: beklerken dolar, hazırken tam ve yeşil; silahta çubuk yok.
        if (!hud.Armed)
        {
            float filled = cooling ? shoveGauge.FilledFraction : 1f;
            DrawBar(x, y + size + 1, size, filled, cooling ? config.hudBarFillColor : config.hudReadyColor, s);
        }

        // Tuş kapağı ve üstünde "BOŞLUK" (yazı kapağın üst yüzüne, alt kenar kalınlığı hariç).
        int keyHeight = Mathf.Min(config.hudKeyCapHeight, size);
        Rect key = ToScreen(x + size + config.hudIconGap, y + (size - keyHeight) / 2, keyWidth, keyHeight, s);
        if (keyStyle != null) UiDraw.Frame(key, keyStyle, config.hudBadgeColor, s);
        else
        {
            UiDraw.Outline(key, config.hudPanelFallbackEdge, s);
            UiDraw.Fill(key, config.hudBadgeColor);
        }
        Rect face = new Rect(key.x, key.y + config.hudKeyCapBorderTop * s, key.width,
            key.height - (config.hudKeyCapBorderTop + config.hudKeyCapBorderBottom) * s);
        Shadowed(face, Loc.T("hud.key_space"), centerSmallStyle, cooling ? config.hudDimTextColor : config.hudTextColor, s);
    }

    // ---- Durumlar ----

    void DrawStatuses(int x, int y, int s, float now)
    {
        int size = config.hudStatusIconSize;
        for (int i = 0; i < hud.Statuses.Count; i++)
        {
            HudStatus status = hud.Statuses[i];
            int ix = x + i * (size + config.hudIconGap);
            Rect icon = ToScreen(ix, y, size, size, s);

            bool visible = HudRules.IsBlinkVisible(status.TimeLeft, config.hudStatusWarnSeconds, now, config.hudStatusBlinkHz);
            if (visible)
            {
                Sprite sprite = Find(HudRules.IconName(status.Kind)) ?? Find(status.SpriteOverride);
                if (sprite != null) DrawCrisp(icon, sprite, 1f);
                else UiDraw.Fill(UiDraw.Shrink(icon, 0.8f), FallbackColor(status.Kind));
            }

            if (status.Count >= 0) DrawBadge(icon, status.Count, s);

            if (status.HasTime)
            {
                bool warn = status.TimeLeft <= config.hudStatusWarnSeconds;
                DrawBar(ix, y + size + 1, size, GaugeFor(status.Kind).RemainingFraction,
                    warn ? config.hudBarWarnColor : config.hudBarFillColor, s);
            }
        }
    }

    Color FallbackColor(HudStatusKind kind)
    {
        Color[] colors = config.hudStatusFallbackColors;
        int index = (int)kind;
        return colors != null && index < colors.Length ? colors[index] : config.hudDimTextColor;
    }

    /// <summary>Simgenin sağ üst köşesinde küçük sayı rozeti.</summary>
    void DrawBadge(Rect icon, int count, int s)
    {
        string text = count.ToString();
        int width = TextWidth(text, smallStyle, s) + 2;
        int height = Mathf.CeilToInt(smallStyle.fontSize / (float)s) + 1;
        var badge = new Rect(icon.xMax - (width - 2) * s, icon.y - 2 * s, width * s, height * s);
        UiDraw.Outline(badge, Color.black, s);
        UiDraw.Fill(badge, config.hudBadgeColor);
        Shadowed(badge, text, centerSmallStyle, config.hudTextColor, s);
    }

    void DrawBar(int x, int y, int width, float fraction, Color fill, int s)
    {
        Rect back = ToScreen(x, y, width, config.hudBarHeight, s);
        UiDraw.Fill(back, config.hudBarBackColor);
        int filled = Mathf.RoundToInt(width * Mathf.Clamp01(fraction)); // Tam resim pikseline yuvarlanır
        if (filled > 0) UiDraw.Fill(ToScreen(x, y, filled, config.hudBarHeight, s), fill);
    }

    /// <summary>
    /// Sprite'ı kutuya oranını koruyarak, tam ekran pikseline oturtarak çizer. Kutu resmin piksel boyunun tam katıysa (normal durum)
    /// her resim pikseli aynı sayıda ekran pikseline düşer; 12x11 kalp 12x12 kutuda yarım piksel kayıp bulanıklaşmaz.
    /// Büyüme/dolma animasyonu sırasında kat geçici olarak kesirli olabilir.
    /// </summary>
    static void DrawCrisp(Rect box, Sprite sprite, float alpha)
    {
        Rect tr = sprite.textureRect;
        float k = Mathf.Min(box.width / tr.width, box.height / tr.height);
        float w = Mathf.Round(tr.width * k), h = Mathf.Round(tr.height * k);
        var fitted = new Rect(Mathf.Round(box.x + (box.width - w) * 0.5f), Mathf.Round(box.y + (box.height - h) * 0.5f), w, h);
        UiDraw.DrawSprite(fitted, sprite, alpha);
    }

    // ---- Yazı ve stiller ----

    /// <summary>Bir resim pikseli aşağı-sağa siyah gölgeli yazı (her zeminde okunur).</summary>
    static void Shadowed(Rect rect, string text, GUIStyle style, Color color, int s)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
        GUI.Label(new Rect(rect.x + s, rect.y + s, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = old;
    }

    /// <summary>Yazının resim pikseli cinsinden genişliği (yukarı yuvarlanır).</summary>
    static int TextWidth(string text, GUIStyle style, int s)
    {
        if (string.IsNullOrEmpty(text) || style == null) return 0;
        return Mathf.CeilToInt(style.CalcSize(new GUIContent(text)).x / s);
    }

    void EnsureStyles(int s)
    {
        if (styleScale == s && textStyle != null) return;
        styleScale = s;

        Font font = GameFont.Get();
        int small = Mathf.Max(1, s - config.hudSmallTextScaleDrop);
        textStyle = new GUIStyle { font = font, fontSize = GameFont.PixelGrid * s, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
        smallStyle = new GUIStyle(textStyle) { fontSize = GameFont.PixelGrid * small };
        centerSmallStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };

        panelStyle = UiDraw.NineSlice(PanelSprite, config.hudPanelBorder);
        keyStyle = UiDraw.NineSlice(KeySpaceSprite, config.hudKeyCapBorder);
        if (keyStyle != null) // Tuş kapağının alt kenarı (yan yüzü) üstünden kalın
            keyStyle.border = new RectOffset(config.hudKeyCapBorder, config.hudKeyCapBorder, config.hudKeyCapBorderTop, config.hudKeyCapBorderBottom);
    }

    /// <summary>Resmi bir kez arar (yoksa null'ı da hatırlar; her karede diskten aranmasın).</summary>
    Sprite Find(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        if (!sprites.TryGetValue(spriteName, out Sprite sprite))
        {
            sprite = SpriteLibrary.Get(spriteName);
            sprites[spriteName] = sprite;
        }
        return sprite;
    }
}
