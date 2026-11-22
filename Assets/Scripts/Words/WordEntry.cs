using System;

/// <summary>
/// words.json içindeki tek bir kelime kaydı. Alan adları JSON ile birebir aynıdır.
/// Kelimenin ne yaptığı kodda değil, <see cref="tags"/> etiketlerinden gelir.
/// </summary>
[Serializable]
public class WordEntry
{
    public const string CategoryAttack = "attack";
    public const string CategoryThreat = "threat";
    public const string CategoryDefense = "defense";

    public const string ScopeTarget = "target";
    public const string ScopeGlobal = "global";
    public const string ScopeSelf = "self";

    public string id;
    public string display;
    public string[] aliases;
    public string category;
    public string scope;
    public string[] tags;
    public WordStats stats;
    public WordCounter counter;
    public string description;

    /// <summary>Isteğe bağlı: Resources/Sprites içindeki sprite adı. Boşsa kelime kimliği (id) kullanılır.</summary>
    public string sprite;

    /// <summary>Büyük tehdit mi? (Tehdidin çözümü de haritada çıkar.)</summary>
    public bool IsThreat => category == CategoryThreat;

    /// <summary>Çözüm (counter) tanımlı mı? JSON'da "counter": null ise false.</summary>
    public bool HasCounter => counter != null && !string.IsNullOrEmpty(counter.type);

    /// <summary>Kelime bu etikete sahip mi?</summary>
    public bool HasTag(string tag)
    {
        if (tags == null) return false;
        return Array.IndexOf(tags, tag) >= 0;
    }
}
