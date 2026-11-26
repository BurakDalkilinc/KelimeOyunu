using System.Globalization;
using UnityEngine;

/// <summary>
/// Oyunun desteklediği diller ("tr", "en") ve dile bağlı küçük yardımcılar (kültür, dilin kendi adı, sistem dilinden seçim).
/// Dil kişiseldir: ayarlarda tutulur, sunucuya gitmez. Düz C# sınıfı.
/// </summary>
public static class Language
{
    public const string Turkish = "tr";
    public const string English = "en";

    /// <summary>Bir metin seçili dilde bulunamazsa bakılan dil.</summary>
    public const string Fallback = Turkish;

    /// <summary>Desteklenen diller, ayarlar ekranındaki sırayla.</summary>
    public static readonly string[] All = { Turkish, English };

    static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");
    static readonly CultureInfo EnglishCulture = new CultureInfo("en-US");

    /// <summary>Kod destekleniyor mu ("tr", "en"; büyük/küçük harf ve boşluk fark etmez)?</summary>
    public static bool IsSupported(string code) => Normalize(code) != null;

    /// <summary>Kodu küçük harfe çevirip doğrular; desteklenmiyorsa null.</summary>
    public static string Normalize(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string clean = code.Trim().ToLowerInvariant();
        foreach (string each in All)
            if (each == clean) return each;
        return null;
    }

    /// <summary>İlk açılışta kullanılacak dil: sistem Türkçe ise Türkçe, değilse İngilizce.</summary>
    public static string FromSystem(SystemLanguage system) => system == SystemLanguage.Turkish ? Turkish : English;

    /// <summary>Dilin kendi dilindeki adı (ayarlar düğmesinde): "Türkçe", "English". Hangi dil seçili olursa olsun aynı yazılır.</summary>
    public static string NativeName(string code) => Normalize(code) == English ? "English" : "Türkçe";

    /// <summary>Büyük harf ve sayı biçimi için kültür (Türkçe'de "i" → "İ").</summary>
    public static CultureInfo Culture(string code) => Normalize(code) == English ? EnglishCulture : TurkishCulture;

    /// <summary>Listede bir sonraki dil (ayarlar düğmesine basınca); sondan başa döner.</summary>
    public static string Next(string code)
    {
        string current = Normalize(code) ?? Fallback;
        int index = System.Array.IndexOf(All, current);
        return All[(index + 1) % All.Length];
    }
}
