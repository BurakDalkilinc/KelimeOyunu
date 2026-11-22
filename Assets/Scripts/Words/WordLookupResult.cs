/// <summary>Kelime aramasının sonucu: durum ve (varsa) bulunan kayıt.</summary>
public readonly struct WordLookupResult
{
    public WordLookupStatus Status { get; }

    /// <summary><see cref="WordLookupStatus.Found"/> ve <see cref="WordLookupStatus.AlreadyUsed"/> durumunda dolu, diğerlerinde null.</summary>
    public WordEntry Entry { get; }

    public WordLookupResult(WordLookupStatus status, WordEntry entry = null)
    {
        Status = status;
        Entry = entry;
    }
}
