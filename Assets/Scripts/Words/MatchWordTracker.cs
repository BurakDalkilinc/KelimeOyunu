using System.Collections.Generic;

/// <summary>
/// Bir maçta hangi kelimelerin kullanıldığını tutar.
/// Kural: her kelime ve eş anlamlıları maçta bir kez kullanılır ("zombi" yazıldıysa "zombie" da kilitlidir).
/// Yeni maçta <see cref="Reset"/> çağrılır. Sunucuda çalışacak şekilde ağdan bağımsızdır.
/// </summary>
public class MatchWordTracker
{
    readonly WordDatabase database;
    readonly HashSet<string> usedIds = new HashSet<string>();
    readonly List<WordEntry> usedWords = new List<WordEntry>();

    public MatchWordTracker(WordDatabase database)
    {
        this.database = database;
    }

    /// <summary>Bu maçta kullanılan kelimeler, kullanılma sırasıyla (maç sonu ekranı için).</summary>
    public IReadOnlyList<WordEntry> UsedWords => usedWords;

    /// <summary>Kelime bu maçta kullanıldı mı?</summary>
    public bool IsUsed(string wordId) => usedIds.Contains(wordId);

    /// <summary>
    /// Yazılan kelimeyi dener. <see cref="WordLookupStatus.Found"/> dönerse kelime kabul edilmiş ve kilitlenmiştir.
    /// <see cref="WordLookupStatus.Unknown"/> kilitlenmez (bilinmeyen kelime tekrar yazılabilir).
    /// </summary>
    public WordLookupResult Use(string input)
    {
        WordLookupResult result = database.Lookup(input);
        if (result.Status != WordLookupStatus.Found)
            return result;

        if (!usedIds.Add(result.Entry.id))
            return new WordLookupResult(WordLookupStatus.AlreadyUsed, result.Entry);

        usedWords.Add(result.Entry);
        return result;
    }

    /// <summary>Yeni maç için tüm kilitleri kaldırır.</summary>
    public void Reset()
    {
        usedIds.Clear();
        usedWords.Clear();
    }
}
