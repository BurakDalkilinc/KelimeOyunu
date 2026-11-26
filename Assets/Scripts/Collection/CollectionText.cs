using System.Globalization;
using System.Text;

/// <summary>
/// Defterdeki oyuncuya görünen yazılar: ipucu ("Z _ _ _ _"), sayaçlar, ilerleme, maç sonu bildirimi. Düz hesap, test edilebilir.
/// </summary>
public static class CollectionText
{
    static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

    /// <summary>Kilitli kartın adı.</summary>
    public const string LockedName = "???";

    /// <summary>Görülmüş ama keşfedilmemiş kelimede açıklamanın yerine yazan söz.</summary>
    public const string SeenOnlyText = "Kendin yazınca açılır";

    /// <summary>
    /// Harf ipucu: ilk harf büyük, kalanlar alt çizgi ("Zombi" → "Z _ _ _ _"). Boşluk alt çizgi olmaz, kelimeler arasında
    /// geniş boşluk kalır ("Muz kabuğu" → "M _ _   _ _ _ _ _ _").
    /// </summary>
    public static string LetterHint(WordEntry word)
    {
        string text = word?.display?.Trim();
        if (string.IsNullOrEmpty(text)) return LockedName;

        var builder = new StringBuilder();
        builder.Append(text.Substring(0, 1).ToUpper(Turkish));
        for (int i = 1; i < text.Length; i++)
            builder.Append(char.IsWhiteSpace(text[i]) ? "  " : " _");
        return builder.ToString();
    }

    /// <summary>Harf sayısı; boşluklar sayılmaz.</summary>
    public static int LetterCount(WordEntry word)
    {
        string text = word?.display;
        if (string.IsNullOrEmpty(text)) return 0;
        int count = 0;
        foreach (char c in text)
            if (!char.IsWhiteSpace(c)) count++;
        return count;
    }

    /// <summary>Kilitli kartın tür ve uzunluk ipucu: "Yaratık · 5 harf".</summary>
    public static string KindAndLength(WordEntry word)
    {
        return $"{CollectionTabs.KindName(word)} · {LetterCount(word)} harf";
    }

    /// <summary>Sayaç satırı: "3 kez yazdın, 1 kez gördün".</summary>
    public static string Counts(int timesWritten, int timesSeen)
    {
        return $"{timesWritten} kez yazdın, {timesSeen} kez gördün";
    }

    /// <summary>İlerleme: "47 / 312 keşfedildi".</summary>
    public static string Progress(CollectionBook book)
    {
        if (book == null) return "0 / 0 keşfedildi";
        return $"{book.DiscoveredCount} / {book.TotalCount} keşfedildi";
    }

    /// <summary>Maç sonu bildirimi: "Deftere 3 yeni kelime eklendi!"; yeni kelime yoksa boş (satır gizlenir).</summary>
    public static string NewWordsNotice(int newCount)
    {
        return newCount > 0 ? $"Deftere {newCount} yeni kelime eklendi!" : string.Empty;
    }

    /// <summary>Sayfa yazısı: "Sayfa 2 / 5".</summary>
    public static string PageLabel(int page, int pageCount)
    {
        return $"Sayfa {page + 1} / {pageCount}";
    }
}
