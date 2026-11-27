using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kelime büyüsü (imza efekti, yalnızca görüntü): kelime kabul edilince harfler tek tek yazı kutusundan kopar, kutunun üstünde
/// bekler, hedef belli olunca fx_letter_trail iziyle eğri yaylar çizerek uçar. Hedef: doğan yaratık (yaratık 0'dan büyüyerek
/// belirir; ilk harf varınca yerde fx_summon, son harf varınca beyaz flaş ve kamera itmesi), koruma kelimesinde yazanın kendisi,
/// büyük tehditte ekranın tepesi (ardından ekran kenarında fx_threat_warn üçgenleri). Hedef kamera dışındaysa harfler ekran
/// kenarına uçup söner. Zamanlama <see cref="WordSpellPlan"/>'da; kural ve doğma anı değişmez.
/// Tek kişilikte ve ağ istemcisinde aynı çalışır (kelime olayı <see cref="ITurnView"/>'dan, doğuşlar <see cref="SpellBus"/>'tan).
/// </summary>
public class WordSpellFx : MonoBehaviour
{
    /// <summary>Hedefi gelmeyen büyünün harflerinin sönme süresi (sn).</summary>
    const float ExpireFadeSeconds = 0.3f;

    sealed class Spell
    {
        public WordSpellPlan Plan;
        public List<string> Letters;
        public string WordId;
        public SpellTargetKind Kind;
        public Transform Target;
        public Vector2 TargetWorld;
        public bool HasWorldTarget;
        public readonly List<Vector2> Slots = new List<Vector2>();
        public int SlotFontSize;
        public bool SlotsReady;
        public float HoverY;
        public bool Summoned;
        public bool Flashed;
    }

    GameConfig config;
    TurnViewSource runner;
    ITurnView turns;
    TypingBoxView box;
    TypingBoxDisplay typing;

    readonly List<Spell> spells = new List<Spell>();
    readonly List<Transform> found = new List<Transform>();
    readonly List<Vector2> markerPositions = new List<Vector2>();
    readonly List<float> markerAngles = new List<float>();
    GUIStyle letterStyle;
    float threatWarnAt = -1000f;
    float screenFlashAt = -1000f;

    public void Bind(GameConfig gameConfig, TurnViewSource source, TypingBoxView view, TypingBoxDisplay display)
    {
        Unsubscribe();
        config = gameConfig;
        runner = source;
        turns = source != null ? source.View : null;
        box = view;
        typing = display;
        if (isActiveAndEnabled) Subscribe();
    }

    void OnEnable() => Subscribe();

    void OnDisable()
    {
        Unsubscribe();
        spells.Clear();
    }

    bool subscribed;

    void Subscribe()
    {
        if (subscribed || turns == null) return;
        turns.WordPlayed += OnWordPlayed;
        SpellBus.Appeared += OnAppeared;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        if (turns != null) turns.WordPlayed -= OnWordPlayed;
        SpellBus.Appeared -= OnAppeared;
        subscribed = false;
    }

    /// <summary>Kelime kabul edildi: büyüyü başlat. Hedef hemen belliyse (tehdit, koruma, önceden doğmuş yaratık) uçuş da başlar.</summary>
    void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        float now = Time.unscaledTime;
        string text = TextFor(writer, word);
        List<string> letters = TypingFxMath.SplitLetters(text);
        if (letters.Count == 0) return;

        var spell = new Spell
        {
            Plan = new WordSpellPlan(letters.Count, now, SpellTimings.From(config)),
            Letters = letters,
            WordId = word != null ? word.id : string.Empty,
            Kind = WordSpellRules.KindFor(word),
        };
        spells.Add(spell);

        switch (spell.Kind)
        {
            case SpellTargetKind.Global:
                spell.Plan.SetTarget(now);
                break;
            case SpellTargetKind.Self:
                Vector2? at = writer != null ? SpellBus.PositionOf(writer.Id) : null;
                if (at.HasValue)
                {
                    spell.TargetWorld = at.Value;
                    spell.HasWorldTarget = true;
                    spell.Plan.SetTarget(now);
                }
                break;
            default:
                found.Clear();
                SpellBus.Recent(spell.WordId, now - config.spellAppearBufferSeconds, found);
                foreach (Transform visual in found) Attach(spell, visual, now);
                break;
        }
    }

    /// <summary>
    /// Uçacak yazı: kendi yazdığın kelimede yazdığın metin (kutudan kopan harfler aynı olsun), başkasınınkinde kelimenin
    /// seçili dildeki adı, bilinmeyen kelimede "???".
    /// </summary>
    string TextFor(PlayerState writer, WordEntry word)
    {
        if (writer != null && runner.IsLocal(writer) && typing != null && Time.unscaledTime - typing.LastSubmittedAt < config.typingSeconds)
        {
            string typed = typing.LastSubmitted.Trim();
            if (typed.Length > 0) return typed;
        }
        return word != null ? word.LocalizedDisplay : "???";
    }

    /// <summary>Yeni doğan yaratık bekleyen bir büyünün kelimesine aitse harfler ona yönelir ve yaratık büyüyerek belirir.</summary>
    void OnAppeared(SpellBus.Appearance appearance)
    {
        float now = Time.unscaledTime;
        foreach (Spell spell in spells)
        {
            if (spell.Kind != SpellTargetKind.Creature || !WordSpellRules.Matches(spell.WordId, appearance.WordId)) continue;
            if (spell.Plan.HasExpired(now) || (spell.Plan.TargetTime.HasValue && now >= spell.Plan.LastArrival)) continue;
            Attach(spell, appearance.Visual, now);
            return;
        }
    }

    void Attach(Spell spell, Transform visual, float now)
    {
        if (visual == null) return;
        if (spell.Target == null)
        {
            spell.Target = visual;
            spell.TargetWorld = visual.position;
            spell.HasWorldTarget = true;
            spell.Plan.SetTarget(now);
        }
        SpawnGrow.Begin(visual, spell.Plan.LastArrival); // Sürü üyeleri de aynı anda belirir
    }

    void Update()
    {
        if (config == null) return;
        float now = Time.unscaledTime;

        for (int i = spells.Count - 1; i >= 0; i--)
        {
            Spell spell = spells[i];
            if (spell.Target != null) spell.TargetWorld = spell.Target.position;
            WordSpellPlan plan = spell.Plan;

            if (plan.TargetTime.HasValue && !spell.Summoned && now >= plan.FirstArrival)
            {
                spell.Summoned = true;
                if (spell.HasWorldTarget) CreatureFx.Summon(spell.TargetWorld);
                if (spell.Kind == SpellTargetKind.Global) threatWarnAt = now;
            }

            if (plan.TargetTime.HasValue && !spell.Flashed && now >= plan.LastArrival)
            {
                spell.Flashed = true;
                Arrive(spell, now);
            }

            if (plan.IsDone(now) && (!plan.HasExpired(now) || now - plan.StartTime > config.spellHoverMaxSeconds + ExpireFadeSeconds))
                spells.RemoveAt(i);
        }
    }

    /// <summary>Son harf vardı: beyaz flaş, kamera itmesi ve hafif sarsıntı (tehditte yalnızca sarsıntı).</summary>
    void Arrive(Spell spell, float now)
    {
        if (spell.Kind == SpellTargetKind.Global)
        {
            CameraShake.AddTrauma(config.spellTrauma * 1.5f);
            return;
        }
        if (!spell.HasWorldTarget) return;

        CreatureFx.Flash(spell.TargetWorld);
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 viewport = cam.WorldToViewportPoint(spell.TargetWorld);
        bool onScreen = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (onScreen) screenFlashAt = now;
        Vector2 push = spell.TargetWorld - (Vector2)cam.transform.position;
        if (push.sqrMagnitude < 0.01f) push = Vector2.down;
        CameraShake.Kick(push, config.spellKickDistance, spell.TargetWorld);
        CameraShake.AddTrauma(config.spellTrauma, spell.TargetWorld);
    }

    // ---- Çizim ----

    void OnGUI()
    {
        if (config == null || Event.current.type != EventType.Repaint) return;
        GUI.depth = 4; // Yazı kutusunun üstünde
        float now = Time.unscaledTime;
        float p = Screen.height / Mathf.Max(1f, config.typeBoxReferenceHeight);

        if (letterStyle == null)
            letterStyle = new GUIStyle { font = GameFont.Get(), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow, wordWrap = false };

        foreach (Spell spell in spells) DrawSpell(spell, now, p);
        DrawScreenFlash(now);
        DrawThreatWarnings(now, p);
    }

    void DrawSpell(Spell spell, float now, float p)
    {
        if (!spell.SlotsReady)
        {
            spell.SlotFontSize = box != null ? box.LetterSlots(string.Concat(spell.Letters), spell.Slots) : Mathf.RoundToInt(12f * p);
            spell.HoverY = (box != null ? box.LastBoxRect.y : Screen.height * 0.6f) - config.spellHoverPixels * p;
            spell.SlotsReady = true;
            if (spell.Slots.Count < spell.Letters.Count) return;
        }

        WordSpellPlan plan = spell.Plan;
        float expireFade = plan.HasExpired(now)
            ? 1f - Mathf.Clamp01((now - plan.StartTime - config.spellHoverMaxSeconds) / ExpireFadeSeconds)
            : 1f;
        if (expireFade <= 0f) return;

        int count = spell.Letters.Count;
        int spellFont = Mathf.Max(1, Mathf.RoundToInt(config.spellLetterFontPixels * p));
        Rect view = GameRect();
        float inset = config.spellEdgeInsetPixels * p;

        for (int i = 0; i < count && i < spell.Slots.Count; i++)
        {
            if (spell.Letters[i] == " " || plan.HasArrived(i, now)) continue; // Boşluk çizilmez, varan harf kaybolur

            float rise = plan.RiseProgress(i, now);
            int font = Mathf.RoundToInt(Mathf.Lerp(spell.SlotFontSize, spellFont, rise));
            Vector2 pos = Resting(spell, i, now, p);
            float alpha = expireFade;
            float tilt = 0f;

            if (plan.IsFlying(i, now))
            {
                float depart = plan.DepartTime(i);
                Vector2 from = Resting(spell, i, depart, p);
                Vector2 to = TargetScreen(spell, i, view, inset, p, out bool fadeAtEnd);
                Vector2 control = ArcPath.Control(from, to, config.spellArcBend, ArcPath.SideFor(i));
                float u = plan.FlightProgress(i, now);
                pos = ArcPath.Point(from, control, to, u);
                font = spellFont;
                if (fadeAtEnd) alpha *= 1f - Mathf.Clamp01((u - 0.7f) / 0.3f);
                tilt = ArcPath.SideFor(i) * 25f * Mathf.Sin(u * Mathf.PI);
                DrawTrail(from, control, to, u, now - depart, alpha, p);
            }

            letterStyle.fontSize = Mathf.Max(1, font);
            float h = font * 1.4f;
            Rect r = new Rect(pos.x - h, pos.y - h * 0.5f, h * 2f, h);
            Matrix4x4 old = GUI.matrix;
            if (Mathf.Abs(tilt) > 0.1f) GUIUtility.RotateAroundPivot(tilt, pos);
            Color color = config.spellLetterColor;
            UiSprites.OutlinedLabel(r, spell.Letters[i], letterStyle, new Color(color.r, color.g, color.b, color.a * alpha),
                                    WithAlpha(config.spellLetterOutlineColor, alpha), Mathf.Max(1f, Mathf.Round(p * 0.5f)));
            GUI.matrix = old;
        }
    }

    /// <summary>Harfin uçmadan önceki yeri: kutudaki yerinden kutunun üstündeki bekleme sırasına yükselir, hafifçe inip kalkar.</summary>
    Vector2 Resting(Spell spell, int i, float time, float p)
    {
        Vector2 slot = spell.Slots[i];
        float rise = spell.Plan.RiseProgress(i, time);
        float bob = Mathf.Sin((time - spell.Plan.StartTime) * config.spellHoverBobSpeed * 2f * Mathf.PI + i * 0.8f)
                    * config.spellHoverBobPixels * p * rise;
        Vector2 hover = new Vector2(slot.x, spell.HoverY + bob);
        return Vector2.LerpUnclamped(slot, hover, rise);
    }

    /// <summary>
    /// Harfin uçtuğu ekran noktası. Tehditte ekranın tepesi (harfler yan yana dizilir); dünyadaki hedef kamera dışındaysa ekran
    /// kenarı (<paramref name="fadeAtEnd"/> true: harf varırken söner).
    /// </summary>
    Vector2 TargetScreen(Spell spell, int i, Rect view, float inset, float p, out bool fadeAtEnd)
    {
        if (spell.Kind == SpellTargetKind.Global || !spell.HasWorldTarget)
        {
            fadeAtEnd = true;
            float spacing = config.spellLetterFontPixels * p * 0.9f;
            return new Vector2(view.center.x + (i - (spell.Letters.Count - 1) * 0.5f) * spacing, view.y + inset);
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            fadeAtEnd = true;
            return view.center;
        }
        Vector3 s = cam.WorldToScreenPoint(spell.TargetWorld);
        Vector2 local = new Vector2(s.x, Screen.height - s.y) - view.position; // IMGUI'de y aşağı; oyun alanına göre
        fadeAtEnd = !SpellScreenMath.IsInside(local, view.size, inset);
        return view.position + (fadeAtEnd ? SpellScreenMath.ClampToEdge(local, view.size, inset) : local);
    }

    /// <summary>Oyun görüntüsünün ekrandaki alanı (Pixel Perfect kamera kenarda siyah şerit bırakabilir); IMGUI koordinatında.</summary>
    static Rect GameRect()
    {
        Camera cam = Camera.main;
        if (cam == null) return new Rect(0f, 0f, Screen.width, Screen.height);
        Rect px = cam.pixelRect;
        return new Rect(px.x, Screen.height - px.yMax, px.width, px.height);
    }

    /// <summary>Harfin arkasındaki iz: yolun biraz gerisindeki noktalarda küçülüp sönen fx_letter_trail kareleri (yoksa kareler).</summary>
    void DrawTrail(Vector2 from, Vector2 control, Vector2 to, float u, float age, float alpha, float p)
    {
        int count = config.spellTrailCount;
        for (int k = 1; k <= count; k++)
        {
            float uk = u - k * config.spellTrailSpacing;
            if (uk <= 0f) break;
            Vector2 at = ArcPath.Point(from, control, to, uk);
            float fade = 1f - k / (float)(count + 1);
            float size = config.spellTrailSizePixels * p * fade;
            Rect r = new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size);
            Sprite frame = UiSprites.FrameAt("fx_letter_trail", age + k * config.spellTrailFrameSeconds, config.spellTrailFrameSeconds, true);
            if (frame != null) UiSprites.Draw(r, frame, new Color(1f, 1f, 1f, alpha * fade));
            else UiSprites.Fill(UiDraw.Shrink(r, 0.5f), WithAlpha(config.spellLetterColor, alpha * fade), 45f);
        }
    }

    /// <summary>Son harf ekrandaki hedefe varınca kısa beyaz ekran flaşı.</summary>
    void DrawScreenFlash(float now)
    {
        float age = now - screenFlashAt;
        if (config.spellFlashSeconds <= 0f || age < 0f || age > config.spellFlashSeconds) return;
        float a = config.spellScreenFlashAlpha * (1f - age / config.spellFlashSeconds);
        UiSprites.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(1f, 1f, 1f, a));
    }

    /// <summary>Büyük tehdit: ekran kenarında dönen/yanıp sönen uyarı üçgenleri (fx_threat_warn; yoksa kırmızı elmas ve "!").</summary>
    void DrawThreatWarnings(float now, float p)
    {
        float age = now - threatWarnAt;
        if (age < 0f || age > config.spellThreatWarnSeconds) return;

        float fade = Mathf.Min(Mathf.Clamp01(age / 0.15f), Mathf.Clamp01((config.spellThreatWarnSeconds - age) / 0.4f));
        float size = config.spellThreatWarnSizePixels * p;
        Rect view = GameRect();
        SpellScreenMath.EdgeMarkers(config.spellThreatWarnCount, view.size, config.spellEdgeInsetPixels * p + size * 0.5f,
                                    markerPositions, markerAngles);

        for (int i = 0; i < markerPositions.Count; i++)
        {
            float pulse = 1f + 0.15f * Mathf.Sin(now * 10f + i);
            float s = size * pulse;
            Vector2 at = view.position + markerPositions[i];
            Rect r = new Rect(at.x - s * 0.5f, at.y - s * 0.5f, s, s);
            Sprite frame = UiSprites.FrameAt("fx_threat_warn", age + i * config.spellThreatWarnFrameSeconds, config.spellThreatWarnFrameSeconds, true);
            if (frame != null)
            {
                UiSprites.Draw(r, frame, new Color(1f, 1f, 1f, fade));
                continue;
            }
            UiSprites.Fill(UiDraw.Shrink(r, 0.7f), WithAlpha(config.spellThreatWarnColor, fade), 45f + age * 90f);
            letterStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(s * 0.6f));
            letterStyle.normal.textColor = new Color(1f, 1f, 1f, fade);
            GUI.Label(r, "!", letterStyle);
        }
    }

    static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
}
