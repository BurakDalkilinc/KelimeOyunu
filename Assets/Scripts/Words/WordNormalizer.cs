using System.Globalization;
using System.Text;

/// <summary>
/// Oyuncunun yazdığı metni karşılaştırılabilir hale getirir.
/// Küçük harfe çevirirken Türkçe kuralı kullanılır: "I" → "ı", "İ" → "i".
/// Düz C# sınıfı; Unity'ye bağlı değil, kolayca test edilir.
/// </summary>
public static class WordNormalizer
{
    static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

    /// <summary>
    /// Metni temizler: Türkçe küçük harf, baştaki/sondaki boşluk silinir, ardışık boşluklar tek olur.
    /// Harf ve rakam dışındaki işaretler (noktalama, emoji) atılır; "_" ve "-" boşluk sayılır.
    /// Türkçe harfler korunur ("ışık" → "ışık").
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        string lower = input.ToLower(Turkish);
        var builder = new StringBuilder(lower.Length);
        bool pendingSpace = false;

        foreach (char c in lower)
        {
            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && builder.Length > 0) builder.Append(' ');
                pendingSpace = false;
                builder.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c == '_' || c == '-')
            {
                pendingSpace = true;
            }
            // Diğer her şey (noktalama, emoji, simgeler) atılır.
        }

        return builder.ToString();
    }

    /// <summary>
    /// Eşleştirme anahtarı: <see cref="Normalize"/> sonucu, Türkçe harfler aksansız hale getirilir
    /// (ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u). "ÖLÜ", "ölü" ve "olu" aynı anahtarı verir.
    /// </summary>
    public static string ToMatchKey(string input)
    {
        string normalized = Normalize(input);
        if (normalized.Length == 0) return normalized;

        var builder = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
            builder.Append(FoldChar(c));
        return builder.ToString();
    }

    static char FoldChar(char c)
    {
        switch (c)
        {
            case 'ç': return 'c';
            case 'ğ': return 'g';
            case 'ı': return 'i';
            case 'ö': return 'o';
            case 'ş': return 's';
            case 'ü': return 'u';
            case 'â': return 'a';
            case 'î': return 'i';
            case 'û': return 'u';
            default: return c;
        }
    }
}
