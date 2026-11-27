using System;
using UnityEngine;

/// <summary>
/// Ayarlar ekranı (IMGUI): ses kaydırıcıları, tam ekran, çözünürlük, VSync, isim etiketleri, ekran sarsıntısı, dil. Menüdeki "Ayarlar"
/// düğmesiyle ya da maçta Esc ile açılır. Görünüşü koleksiyon defteriyle aynıdır (<see cref="UiDraw"/>).
/// Ses, isim etiketi ve dil değişiklikleri hemen uygulanır; ekran ayarları "Uygula" ile. Kapanırken ayarlar kaydedilir.
/// Açıkken oyunun kendisi durmaz (çok oyunculu), yalnızca yazma/hareket girdisi oyuna gitmez (bkz. <see cref="UiInputBlock"/>).
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    const string PageSprite = "ui_book_page";

    static SettingsScreen instance;
    static Func<string> leaveLabel;
    static Action leaveAction;

    /// <summary>Ayarlar açık mı? Açıkken menüler kendilerini çizmez, oyun girdisi kapanır.</summary>
    public static bool IsOpen => instance != null && instance.isActiveAndEnabled;

    /// <summary>Ayarları açar (yoksa oluşturur).</summary>
    public static void Open()
    {
        if (instance == null) instance = new GameObject("SettingsScreen").AddComponent<SettingsScreen>();
        instance.gameObject.SetActive(true);
        instance.BeginEditing();
    }

    /// <summary>Ayarları kapatır ve kaydeder (uygulanmamış ekran ayarları atılır).</summary>
    public static void Close()
    {
        if (!IsOpen) return;
        SettingsService.Save();
        instance.gameObject.SetActive(false);
    }

    /// <summary>Açıksa kapatır, kapalıysa açar.</summary>
    public static void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    /// <summary>
    /// Ekranın altına ek bir düğme koyar (ör. tek kişilik maçta "Ana menüye dön"). Yazı her çizimde
    /// <paramref name="label"/>'dan okunur (dil değişince güncellensin diye). Sahibi yok olurken
    /// <see cref="ClearLeaveAction"/> ile kaldırmalıdır.
    /// </summary>
    public static void SetLeaveAction(Func<string> label, Action action)
    {
        leaveLabel = label;
        leaveAction = action;
    }

    /// <summary>Ek düğmeyi kaldırır (yalnızca aynı işi koyan kaldırabilir).</summary>
    public static void ClearLeaveAction(Action action)
    {
        if (leaveAction != action) return;
        leaveAction = null;
        leaveLabel = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        leaveAction = null;
        leaveLabel = null;
    }

    GameConfig config;
    GameSettings settings;     // Geçerli ayarlar (ses ve isim etiketi hemen buraya yazılır)
    GameSettings display;      // Ekran ayarlarının taslağı ("Uygula"ya kadar)
    ResolutionList resolutions;
    int resolutionIndex;
    float lastSliderClick;

    GUIStyle pageStyle, titleStyle, labelStyle, valueStyle, smallStyle, buttonStyle;
    bool stylesReady;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void BeginEditing()
    {
        config = GameConfigLoader.Get();
        settings = SettingsService.Current;
        display = settings.Clone();
        resolutions = SettingsService.AvailableResolutions();
        Vector2Int now = new Vector2Int(Screen.width, Screen.height);
        resolutionIndex = resolutions.IndexOf(display.resolutionWidth, display.resolutionHeight, now);
    }

    // ---- Çizim ----

    void OnGUI()
    {
        if (settings == null) BeginEditing();
        EnsureStyles();

        GUI.depth = -110; // Defterin ve diğer menülerin üstünde
        float scale = Screen.height / config.collectionReferenceHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = config.collectionReferenceHeight;

        UiDraw.Fill(new Rect(0, 0, width, height), config.collectionBackdropColor);

        const int rowCount = 10;
        float p = config.collectionPadding;
        float gap = config.collectionCardSpacing;
        float row = config.settingsRowHeight;
        float panelWidth = Mathf.Min(config.settingsPanelWidth, width - 2 * config.collectionMargin);
        float panelHeight = 2 * p + config.collectionHeaderHeight * 0.6f + rowCount * row + gap + config.collectionFooterHeight * 1.6f;
        Rect panel = new Rect((width - panelWidth) * 0.5f, Mathf.Max(config.collectionMargin, (height - panelHeight) * 0.5f), panelWidth, panelHeight);
        UiDraw.Frame(panel, pageStyle, config.collectionPageColor, config.collectionFramePixelScale);

        Rect inner = new Rect(panel.x + p, panel.y + p, panel.width - 2 * p, panel.height - 2 * p);
        float y = inner.y;
        GUI.Label(new Rect(inner.x, y, inner.width, config.collectionHeaderHeight * 0.6f), SettingsText.Title, titleStyle);
        y += config.collectionHeaderHeight * 0.6f;

        VolumeRow(new Rect(inner.x, y, inner.width, row), SettingsText.MasterVolume, ref settings.masterVolume); y += row;
        VolumeRow(new Rect(inner.x, y, inner.width, row), SettingsText.MusicVolume, ref settings.musicVolume); y += row;
        VolumeRow(new Rect(inner.x, y, inner.width, row), SettingsText.SfxVolume, ref settings.sfxVolume); y += row;
        display.fullscreen = ToggleRow(new Rect(inner.x, y, inner.width, row), SettingsText.Fullscreen, display.fullscreen); y += row;
        ResolutionRow(new Rect(inner.x, y, inner.width, row)); y += row;
        display.vSync = ToggleRow(new Rect(inner.x, y, inner.width, row), SettingsText.VSync, display.vSync); y += row;
        settings.showNameTags = ToggleRow(new Rect(inner.x, y, inner.width, row), SettingsText.NameTags, settings.showNameTags); y += row;
        settings.screenShake = ToggleRow(new Rect(inner.x, y, inner.width, row), SettingsText.ScreenShake, settings.screenShake); y += row;
        PercentRow(new Rect(inner.x, y, inner.width, row), SettingsText.ScreenShakeStrength, ref settings.screenShakeStrength, settings.screenShake); y += row;
        LanguageRow(new Rect(inner.x, y, inner.width, row)); y += row;

        y += gap;
        GUI.Label(new Rect(inner.x, y, inner.width, config.collectionFooterHeight * 0.6f), SettingsText.DisplayHint, smallStyle);
        DrawFooter(new Rect(inner.x, inner.yMax - config.collectionFooterHeight, inner.width, config.collectionFooterHeight));

        GUI.matrix = oldMatrix;
    }

    /// <summary>Satırı ad | denetim | değer olarak böler.</summary>
    void SplitRow(Rect area, out Rect label, out Rect control, out Rect value)
    {
        float gap = config.collectionCardSpacing;
        float labelWidth = area.width * config.settingsLabelRatio;
        float valueWidth = config.settingsValueWidth;
        label = new Rect(area.x, area.y, labelWidth, area.height);
        value = new Rect(area.xMax - valueWidth, area.y, valueWidth, area.height);
        control = new Rect(label.xMax + gap, area.y, value.x - label.xMax - 2 * gap, area.height);
    }

    /// <summary>Ses kaydırıcısı: değişince hemen uygulanır ve tık sesi (yeni düzeyde) duyulur.</summary>
    void VolumeRow(Rect area, string title, ref float volume)
    {
        SplitRow(area, out Rect label, out Rect control, out Rect value);
        GUI.Label(label, title, labelStyle);

        Rect slider = new Rect(control.x, control.y + control.height * 0.4f, control.width, control.height * 0.3f);
        float next = GameSettings.ClampVolume(GUI.HorizontalSlider(slider, volume, 0f, 1f));
        GUI.Label(value, SettingsText.Percent(next), valueStyle);

        if (Mathf.Approximately(next, volume)) return;
        volume = next;
        SettingsService.ApplyAudio();

        if (Time.unscaledTime - lastSliderClick >= config.audioSliderClickInterval)
        {
            lastSliderClick = Time.unscaledTime;
            GameAudio.Play(SoundId.UiClick, config.audioUiClickVolume, 0f);
        }
    }

    /// <summary>
    /// Yüzde kaydırıcısı (ekran sarsıntısının şiddeti): değer hemen geçerli olur, kapanırken kaydedilir.
    /// <paramref name="enabled"/> kapalıysa kaydırıcı soluk ve dokunulmaz.
    /// </summary>
    void PercentRow(Rect area, string title, ref float amount, bool enabled)
    {
        SplitRow(area, out Rect label, out Rect control, out Rect value);
        GUI.Label(label, title, labelStyle);

        Rect slider = new Rect(control.x, control.y + control.height * 0.4f, control.width, control.height * 0.3f);
        GUI.enabled = enabled;
        float next = GameSettings.ClampVolume(GUI.HorizontalSlider(slider, amount, 0f, 1f));
        GUI.enabled = true;
        GUI.Label(value, SettingsText.Percent(next), valueStyle);

        if (Mathf.Approximately(next, amount)) return;
        amount = next;
        if (Time.unscaledTime - lastSliderClick >= config.audioSliderClickInterval)
        {
            lastSliderClick = Time.unscaledTime;
            GameAudio.Play(SoundId.UiClick, config.audioUiClickVolume, 0f);
            CameraShake.Shake(config.cameraShakeMaxOffset * 0.25f * next, 0.15f); // Ne kadar sarsılacağını hemen göster
        }
    }

    bool ToggleRow(Rect area, string title, bool on)
    {
        SplitRow(area, out Rect label, out Rect control, out Rect _);
        GUI.Label(label, title, labelStyle);
        Rect button = new Rect(control.x, control.y + control.height * 0.12f, control.width, control.height * 0.76f);
        if (!GUI.Button(button, SettingsText.OnOff(on), buttonStyle)) return on;
        GameAudio.Click();
        return !on;
    }

    void ResolutionRow(Rect area)
    {
        SplitRow(area, out Rect label, out Rect control, out Rect _);
        GUI.Label(label, SettingsText.Resolution, labelStyle);

        float h = control.height * 0.76f;
        float y = control.y + control.height * 0.12f;
        Rect prev = new Rect(control.x, y, h, h);
        Rect next = new Rect(control.xMax - h, y, h, h);
        Rect text = new Rect(prev.xMax, control.y, next.x - prev.xMax, control.height);

        var sizes = resolutions.Sizes;
        bool any = resolutionIndex >= 0 && resolutionIndex < sizes.Count;
        GUI.enabled = any && resolutionIndex > 0;
        if (GUI.Button(prev, SettingsText.Previous, buttonStyle)) StepResolution(-1);
        GUI.enabled = any && resolutionIndex < sizes.Count - 1;
        if (GUI.Button(next, SettingsText.Next, buttonStyle)) StepResolution(1);
        GUI.enabled = true;

        // Henüz seçilmediyse (0) "Ekranın kendisi" yazar; önceki/sonraki ona en yakın boyuttan başlar.
        bool chosen = display.resolutionWidth > 0 && display.resolutionHeight > 0;
        string shown = chosen || !any ? SettingsText.Size(display.resolutionWidth, display.resolutionHeight)
                                      : SettingsText.NativeResolution;
        GUI.Label(text, shown, valueStyle);
    }

    void StepResolution(int delta)
    {
        GameAudio.Click();
        resolutionIndex = resolutions.Step(resolutionIndex, delta);
        if (resolutionIndex < 0) return;
        Vector2Int size = resolutions.Sizes[resolutionIndex];
        display.resolutionWidth = size.x;
        display.resolutionHeight = size.y;
    }

    /// <summary>Dil düğmesi: basınca sıradaki dile geçer (Türkçe ↔ English); yazılar hemen değişir ve kaydedilir.</summary>
    void LanguageRow(Rect area)
    {
        SplitRow(area, out Rect label, out Rect control, out Rect _);
        GUI.Label(label, SettingsText.LanguageLabel, labelStyle);
        Rect button = new Rect(control.x, control.y + control.height * 0.12f, control.width, control.height * 0.76f);
        if (!GUI.Button(button, SettingsText.LanguageName(settings.language), buttonStyle)) return;
        GameAudio.Click();
        SettingsService.SetLanguage(Language.Next(settings.language));
    }

    void DrawFooter(Rect area)
    {
        bool leave = leaveAction != null;
        int count = leave ? 3 : 2;
        float gap = config.collectionCardSpacing;
        float w = (area.width - gap * (count - 1)) / count;
        float x = area.x;

        if (GUI.Button(new Rect(x, area.y, w, area.height), SettingsText.Apply, buttonStyle)) ApplyDisplay();
        x += w + gap;

        if (leave)
        {
            if (GUI.Button(new Rect(x, area.y, w, area.height), leaveLabel != null ? leaveLabel() : string.Empty, buttonStyle))
            {
                GameAudio.Click();
                Action action = leaveAction;
                Close();
                action();
                return;
            }
            x += w + gap;
        }

        if (GUI.Button(new Rect(x, area.y, w, area.height), SettingsText.Close, buttonStyle))
        {
            GameAudio.Click();
            Close();
        }
    }

    /// <summary>Taslaktaki ekran ayarlarını geçerli ayarlara yazar, uygular ve kaydeder.</summary>
    void ApplyDisplay()
    {
        GameAudio.Click();
        settings.fullscreen = display.fullscreen;
        settings.vSync = display.vSync;
        settings.resolutionWidth = display.resolutionWidth;
        settings.resolutionHeight = display.resolutionHeight;
        SettingsService.ApplyDisplay();
        SettingsService.Save();
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        pageStyle = UiDraw.NineSlice(PageSprite, config.collectionPageBorder);

        Font font = GameFont.Get();
        Color ink = config.collectionTextColor;
        labelStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = config.collectionFontSize, alignment = TextAnchor.MiddleLeft };
        labelStyle.normal.textColor = ink;
        valueStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
        titleStyle = new GUIStyle(labelStyle) { fontSize = config.collectionTitleFontSize, alignment = TextAnchor.UpperCenter };
        smallStyle = new GUIStyle(labelStyle) { fontSize = config.collectionSmallFontSize, wordWrap = true };
        buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = config.collectionFontSize };
    }
}
