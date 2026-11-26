using System.Collections.Generic;

/// <summary>
/// Koleksiyon defterinin verisi ve kuralları: kendi yazdığın kelime keşfedilir, başkasının yazdığı görülür.
/// Eş anlamlılar ayrı sayılmaz; her şey kelimenin kimliğine (<see cref="WordEntry.id"/>) yazılır. Unity'den bağımsızdır, test edilebilir.
/// </summary>
public class CollectionBook
{
    readonly List<WordEntry> words = new List<WordEntry>();
    readonly HashSet<string> knownIds = new HashSet<string>();
    readonly Dictionary<string, CollectionEntry> entries = new Dictionary<string, CollectionEntry>();
    readonly HashSet<string> newThisMatch = new HashSet<string>();

    /// <summary>
    /// Defteri kurar. <paramref name="allWords"/> words.json'daki tüm kelimelerdir (sayfadaki sıra bu sıradır);
    /// <paramref name="saved"/> önceki kayıttır (yoksa boş defter).
    /// </summary>
    public CollectionBook(IEnumerable<WordEntry> allWords, CollectionSaveData saved = null)
    {
        if (allWords != null)
        {
            foreach (WordEntry word in allWords)
            {
                if (word == null || string.IsNullOrEmpty(word.id) || !knownIds.Add(word.id)) continue;
                words.Add(word);
            }
        }

        if (saved?.entries == null) return;
        foreach (CollectionEntry entry in saved.entries)
        {
            // Bozuk satırlar atlanır, eksi sayılar sıfırlanır; aynı kimlik iki kez geçerse sayaçlar toplanır.
            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
            CollectionEntry mine = GetOrCreate(entry.id);
            if (entry.timesWritten > 0) mine.timesWritten += entry.timesWritten;
            if (entry.timesSeen > 0) mine.timesSeen += entry.timesSeen;
        }
    }

    /// <summary>Defterdeki bütün kelimeler (words.json sırasıyla).</summary>
    public IReadOnlyList<WordEntry> Words => words;

    /// <summary>Toplam kelime sayısı (words.json'dan).</summary>
    public int TotalCount => words.Count;

    /// <summary>Keşfedilen (kendin yazdığın) kelime sayısı. Artık words.json'da olmayan eski kayıtlar sayılmaz.</summary>
    public int DiscoveredCount => CountWithStatus(CollectionStatus.Discovered);

    /// <summary>Yalnızca görülmüş (keşfedilmemiş) kelime sayısı.</summary>
    public int SeenCount => CountWithStatus(CollectionStatus.Seen);

    /// <summary>Bu maçta kilitli durumdan çıkan (ilk kez görülen ya da keşfedilen) kelime sayısı.</summary>
    public int NewThisMatchCount => newThisMatch.Count;

    /// <summary>Yeni maç başladı: "bu maçta yeni" sayacı sıfırlanır.</summary>
    public void BeginMatch()
    {
        newThisMatch.Clear();
    }

    /// <summary>Kelimeyi kendin yazdın: keşfedildi. Gördün sayacı artmaz. Kelime kilitliydiyse true döner.</summary>
    public bool RecordWritten(WordEntry word)
    {
        if (!IsRecordable(word)) return false;
        CollectionEntry entry = GetOrCreate(word.id);
        bool wasLocked = entry.Status == CollectionStatus.Locked;
        entry.timesWritten++;
        if (wasLocked) newThisMatch.Add(word.id);
        return wasLocked;
    }

    /// <summary>Kelimeyi başkası (oyuncu ya da bot) yazdı: görüldü. Kelime kilitliydiyse true döner.</summary>
    public bool RecordSeen(WordEntry word)
    {
        if (!IsRecordable(word)) return false;
        CollectionEntry entry = GetOrCreate(word.id);
        bool wasLocked = entry.Status == CollectionStatus.Locked;
        entry.timesSeen++;
        if (wasLocked) newThisMatch.Add(word.id);
        return wasLocked;
    }

    /// <summary>Kelimenin durumu (kilitli, görüldü, keşfedildi).</summary>
    public CollectionStatus GetStatus(string id)
    {
        return id != null && entries.TryGetValue(id, out CollectionEntry entry) ? entry.Status : CollectionStatus.Locked;
    }

    /// <summary>Kaç kez kendin yazdın.</summary>
    public int TimesWritten(string id)
    {
        return id != null && entries.TryGetValue(id, out CollectionEntry entry) ? entry.timesWritten : 0;
    }

    /// <summary>Kaç kez başkasının yazdığını gördün.</summary>
    public int TimesSeen(string id)
    {
        return id != null && entries.TryGetValue(id, out CollectionEntry entry) ? entry.timesSeen : 0;
    }

    /// <summary>Sekmeye düşen kelimeler, words.json sırasıyla.</summary>
    public List<WordEntry> Filter(CollectionTab tab)
    {
        var result = new List<WordEntry>();
        foreach (WordEntry word in words)
            if (CollectionTabs.Contains(tab, word)) result.Add(word);
        return result;
    }

    /// <summary>Kaydedilecek veri (yalnızca sayacı olan kelimeler).</summary>
    public CollectionSaveData ToSaveData()
    {
        var data = new CollectionSaveData();
        foreach (CollectionEntry entry in entries.Values)
        {
            if (entry.timesWritten == 0 && entry.timesSeen == 0) continue;
            data.entries.Add(new CollectionEntry { id = entry.id, timesWritten = entry.timesWritten, timesSeen = entry.timesSeen });
        }
        return data;
    }

    // Kimliği olan her kelime kaydedilir; words.json'da olmayan (örneğin ileride silinmiş) kelime de kayıtta kalır ama sayılmaz.
    static bool IsRecordable(WordEntry word) => word != null && !string.IsNullOrEmpty(word.id);

    int CountWithStatus(CollectionStatus status)
    {
        int count = 0;
        foreach (CollectionEntry entry in entries.Values)
            if (entry.Status == status && knownIds.Contains(entry.id)) count++;
        return count;
    }

    CollectionEntry GetOrCreate(string id)
    {
        if (!entries.TryGetValue(id, out CollectionEntry entry))
        {
            entry = new CollectionEntry { id = id };
            entries.Add(id, entry);
        }
        return entry;
    }
}
