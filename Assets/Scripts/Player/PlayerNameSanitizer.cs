using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// İstemcinin bildirdiği görünen adı sunucuda temizler: kontrol/biçim karakterleri (satır sonu, sekme, görünmez yön
/// işaretleri) ve ağ kodlayıcısının ayraçları atılır, art arda boşluklar teke iner, baş/son boşluk kırpılır, ad en çok
/// <c>maxLength</c> harfe kısaltılır; boş kalırsa "Oyuncu N" olur. Aynı ad ikinci kez gelirse sonuna " (2)" eklenir.
/// İstemciye güvenilmez: her şey burada, sunucuda yapılır. Düz hesap, test edilebilir.
/// </summary>
public static class PlayerNameSanitizer
{
    /// <summary>Ayar verilmezse kullanılan en uzun ad (harf). Asıl değer <see cref="GameConfig.playerNameMaxLength"/>'tedir.</summary>
    public const int DefaultMaxLength = 24;

    /// <summary>Ağ kodlayıcılarının (<see cref="RosterCodec"/>) ayraçları; adın içinde olursa liste bozulur.</summary>
    static readonly char[] ReservedChars = { ';', '|' };

    /// <summary>Adı temizler; boş kalırsa "Oyuncu <paramref name="playerId"/>" döner.</summary>
    public static string Sanitize(string raw, int playerId, int maxLength)
    {
        string clean = Clean(raw, maxLength);
        return clean.Length > 0 ? clean : NameTagRules.DefaultName(playerId, false, RosterBuilder.FirstBotId);
    }

    /// <summary>Adı temizler; hiçbir şey kalmazsa boş metin döner.</summary>
    public static string Clean(string raw, int maxLength)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        if (maxLength < 1) maxLength = 1;

        var builder = new StringBuilder(raw.Length);
        bool lastWasSpace = true; // Baştaki boşluklar atılır
        foreach (char c in raw)
        {
            if (IsDropped(c)) continue;

            bool space = char.IsWhiteSpace(c) || System.Array.IndexOf(ReservedChars, c) >= 0;
            if (space)
            {
                if (!lastWasSpace) builder.Append(' ');
                lastWasSpace = true;
                continue;
            }

            builder.Append(c);
            lastWasSpace = false;
        }

        string text = builder.ToString().Trim();
        return Truncate(text, maxLength);
    }

    /// <summary>
    /// <paramref name="name"/> <paramref name="taken"/> içinde varsa sonuna " (2)", " (3)"... ekler (büyük/küçük harf
    /// ayrımı yok). Toplam uzunluk <paramref name="maxLength"/>'i aşmasın diye gerekirse ad kısaltılır.
    /// </summary>
    public static string MakeUnique(string name, ICollection<string> taken, int maxLength)
    {
        name ??= "";
        if (!Contains(taken, name)) return name;

        for (int n = 2; ; n++)
        {
            string suffix = " (" + n + ")";
            string head = Truncate(name, System.Math.Max(1, maxLength - suffix.Length)).TrimEnd();
            string candidate = head + suffix;
            if (!Contains(taken, candidate)) return candidate;
        }
    }

    /// <summary>Kontrol karakteri, görünmez biçim işareti ya da özel kullanım karakteri mi? (Satır sonu/sekme boşluğa döner.)</summary>
    static bool IsDropped(char c)
    {
        if (char.IsWhiteSpace(c)) return false; // Satır sonu, sekme dahil boşluklar teke indirilir
        UnicodeCategory category = char.GetUnicodeCategory(c);
        return category == UnicodeCategory.Control
            || category == UnicodeCategory.Format       // Sıfır genişlikli boşluk, yön değiştirme (RTL) işaretleri
            || category == UnicodeCategory.LineSeparator
            || category == UnicodeCategory.ParagraphSeparator
            || category == UnicodeCategory.PrivateUse;
    }

    /// <summary>En çok <paramref name="maxLength"/> karakter; vekil çifti (emoji gibi) ortadan bölünmez.</summary>
    static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        int cut = maxLength;
        if (cut > 0 && char.IsHighSurrogate(text[cut - 1])) cut--;
        return text.Substring(0, cut).TrimEnd();
    }

    static bool Contains(ICollection<string> taken, string name)
    {
        if (taken == null) return false;
        foreach (string other in taken)
            if (string.Equals(other, name, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
