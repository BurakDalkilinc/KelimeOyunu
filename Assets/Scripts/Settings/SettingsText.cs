/// <summary>
/// Ayarlar ekranının bütün yazıları tek yerde. Metinlerin kendisi çeviri tablolarındadır (Resources/Data/lang_xx.json);
/// her biri seçili dilde okunur, dil değişince bir sonraki çizimde yenisi görünür.
/// </summary>
public static class SettingsText
{
    public static string Title => Loc.T("settings.title");
    public static string MenuButton => Loc.T("settings.menu_button");

    public static string MasterVolume => Loc.T("settings.master_volume");
    public static string MusicVolume => Loc.T("settings.music_volume");
    public static string SfxVolume => Loc.T("settings.sfx_volume");
    public static string Fullscreen => Loc.T("settings.fullscreen");
    public static string Resolution => Loc.T("settings.resolution");
    public static string VSync => Loc.T("settings.vsync");
    public static string NameTags => Loc.T("settings.name_tags");
    public static string LanguageLabel => Loc.T("settings.language");
    public static string ScreenShake => Loc.T("settings.screen_shake");
    public static string ScreenShakeStrength => Loc.T("settings.screen_shake_strength");

    public static string On => Loc.T("settings.on");
    public static string Off => Loc.T("settings.off");
    public const string Previous = "<";
    public const string Next = ">";
    public static string NativeResolution => Loc.T("settings.native_resolution");

    public static string Apply => Loc.T("settings.apply");
    public static string Close => Loc.T("settings.close");
    public static string BackToMenu => Loc.T("settings.back_to_menu");
    public static string DisplayHint => Loc.T("settings.display_hint");

    /// <summary>Dil düğmesinin yazısı: dilin kendi adı ("Türkçe", "English").</summary>
    public static string LanguageName(string code) => Language.NativeName(code);

    /// <summary>"%80" (Türkçe) ya da "80%" (İngilizce) gibi yüzde yazısı.</summary>
    public static string Percent(float value) => Loc.F("settings.percent", UnityEngine.Mathf.RoundToInt(value * 100f));

    /// <summary>"Açık" ya da "Kapalı".</summary>
    public static string OnOff(bool value) => value ? On : Off;

    /// <summary>"1920 × 1080".</summary>
    public static string Size(int width, int height) => width > 0 && height > 0 ? $"{width} × {height}" : NativeResolution;
}
