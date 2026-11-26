/// <summary>
/// Ayarlar ekranının bütün yazıları tek yerde. Dil desteği geldiğinde bu metinler çeviri tablosuna taşınacak.
/// </summary>
public static class SettingsText
{
    public const string Title = "Ayarlar";
    public const string MenuButton = "Ayarlar";

    public const string MasterVolume = "Ana ses";
    public const string MusicVolume = "Müzik";
    public const string SfxVolume = "Efektler";
    public const string Fullscreen = "Tam ekran";
    public const string Resolution = "Çözünürlük";
    public const string VSync = "Dikey eşitleme (VSync)";
    public const string NameTags = "İsim etiketleri";
    public const string Language = "Dil";
    public const string LanguageTurkish = "Türkçe";
    public const string ComingSoon = "(yakında)";

    public const string On = "Açık";
    public const string Off = "Kapalı";
    public const string Previous = "<";
    public const string Next = ">";
    public const string NativeResolution = "Ekranın kendisi";

    public const string Apply = "Uygula";
    public const string Close = "Kapat (Esc)";
    public const string BackToMenu = "Ana menüye dön";
    public const string DisplayHint = "Görüntü ayarları \"Uygula\" ile değişir. Ses ayarları hemen duyulur.";

    /// <summary>"%80" gibi yüzde yazısı.</summary>
    public static string Percent(float value) => "%" + UnityEngine.Mathf.RoundToInt(value * 100f);

    /// <summary>"Açık" ya da "Kapalı".</summary>
    public static string OnOff(bool value) => value ? On : Off;

    /// <summary>"1920 × 1080".</summary>
    public static string Size(int width, int height) => width > 0 && height > 0 ? $"{width} × {height}" : NativeResolution;
}
