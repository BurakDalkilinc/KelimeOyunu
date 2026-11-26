using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Oyunun her yerinden kullanılan çeviri kapısı: <c>Loc.T("settings.title")</c>, <c>Loc.F("match.winner", ad)</c>.
/// Tablolar Resources/Data/lang_tr.json ve lang_en.json'dan ilk istendiğinde yüklenir. Seçili dil ayarlardan gelir
/// (<see cref="SettingsService"/> ayarları okurken <see cref="SetLanguage"/> çağırır); o zamana kadar Türkçe'dir.
/// İşin kendisi <see cref="Localizer"/>'dadır; burası paylaşılan tek örneği tutar.
/// </summary>
public static class Loc
{
    static Localizer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        LanguageChanged = null;
    }

    static Localizer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Localizer(LocTable.LoadFromResources);
                if (Application.isEditor) instance.Warn = message => Debug.LogWarning(message); // Oyuncuya giden sürümde sessiz
                instance.LanguageChanged += code => LanguageChanged?.Invoke(code);
            }
            return instance;
        }
    }

    /// <summary>Dil değişti (yeni dil kodu). Ekranlar yazılarını yenilemek için dinler.</summary>
    public static event Action<string> LanguageChanged;

    /// <summary>Seçili dil kodu ("tr" / "en").</summary>
    public static string Current => Instance.Current;

    /// <summary>Seçili dilin kültürü (büyük harf, sayı biçimi).</summary>
    public static CultureInfo Culture => Language.Culture(Current);

    /// <summary>Dili değiştirir (kaydetmez; kaydetmek için <see cref="SettingsService.SetLanguage"/>). Değiştiyse true.</summary>
    public static bool SetLanguage(string code) => Instance.SetLanguage(code);

    /// <summary>Anahtarın seçili dildeki metni.</summary>
    public static string T(string key) => Instance.T(key);

    /// <summary>Anahtarın verilen dildeki metni.</summary>
    public static string TextFor(string language, string key) => Instance.TextFor(language, key);

    /// <summary>Biçimli metin ({0}, {1} yer tutucuları).</summary>
    public static string F(string key, params object[] args) => Instance.F(key, args);

    /// <summary>Dilin tablosu (testler ve araçlar için).</summary>
    public static LocTable TableFor(string language) => Instance.TableFor(language);
}
