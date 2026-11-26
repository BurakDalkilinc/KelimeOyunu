using System;
using UnityEngine;

/// <summary>
/// Oyuncunun kendi bilgisayarındaki tercihleri (ses düzeyleri, ekran, isim etiketleri, dil). Düz veri; JSON'a aynen yazılır.
/// Maç kurallarını değiştirmez, sunucuya gitmez. Varsayılan değerler aşağıdaki alanlardadır (ilk açılış).
/// </summary>
[Serializable]
public class GameSettings
{
    public const int CurrentVersion = 1;
    public const string DefaultLanguage = "tr";

    /// <summary>
    /// Kayıt biçiminin sürümü. Yeni nesnede 0'dır, kaydederken <see cref="Clamp"/> yazar; böylece sürümsüz bir JSON
    /// (başka bir dosya) okunursa tanınır ve varsayılanlara dönülür.
    /// </summary>
    public int version;

    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    public bool fullscreen = true;

    /// <summary>Çözünürlük genişliği/yüksekliği; 0 ise ekranın kendi çözünürlüğü.</summary>
    public int resolutionWidth;
    public int resolutionHeight;

    public bool vSync = true;
    public bool showNameTags = true;

    /// <summary>Dil kodu. Şimdilik yalnızca "tr" (dil desteği sonraki aşamada).</summary>
    public string language = DefaultLanguage;

    /// <summary>Değerleri geçerli aralığa çeker: ses düzeyleri 0..1, çözünürlük negatif olamaz, dil boşsa Türkçe.</summary>
    public void Clamp()
    {
        masterVolume = ClampVolume(masterVolume);
        musicVolume = ClampVolume(musicVolume);
        sfxVolume = ClampVolume(sfxVolume);
        if (resolutionWidth <= 0 || resolutionHeight <= 0) { resolutionWidth = 0; resolutionHeight = 0; }
        if (string.IsNullOrWhiteSpace(language)) language = DefaultLanguage;
        version = CurrentVersion;
    }

    /// <summary>Ses düzeyini 0..1 aralığına çeker; sayı değilse (NaN) 0.</summary>
    public static float ClampVolume(float value)
    {
        if (float.IsNaN(value)) return 0f;
        return Mathf.Clamp01(value);
    }

    /// <summary>Bağımsız bir kopya (taslak düzenleme için).</summary>
    public GameSettings Clone() => (GameSettings)MemberwiseClone();
}
