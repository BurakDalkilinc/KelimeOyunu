/// <summary>
/// Defter sekmelerinin kuralı ve Türkçe adları. Sekme kelimenin "category" ve "scope" alanından çıkar, kelimeye özel kod yoktur:
/// Yaratıklar = saldırı (category "attack") ve harita çapında değil; Savunma = category "defense"; Büyük tehditler = scope "global".
/// </summary>
public static class CollectionTabs
{
    /// <summary>Sekmeler ekrandaki sırasıyla.</summary>
    public static readonly CollectionTab[] All = { CollectionTab.All, CollectionTab.Creatures, CollectionTab.Defense, CollectionTab.Threats };

    /// <summary>Kelime bu sekmede mi?</summary>
    public static bool Contains(CollectionTab tab, WordEntry word)
    {
        if (word == null) return false;
        switch (tab)
        {
            case CollectionTab.All: return true;
            case CollectionTab.Creatures: return word.category == WordEntry.CategoryAttack && word.scope != WordEntry.ScopeGlobal;
            case CollectionTab.Defense: return word.category == WordEntry.CategoryDefense;
            case CollectionTab.Threats: return word.scope == WordEntry.ScopeGlobal;
            default: return false;
        }
    }

    /// <summary>Sekme düğmesinin yazısı.</summary>
    public static string Title(CollectionTab tab)
    {
        switch (tab)
        {
            case CollectionTab.Creatures: return "Yaratıklar";
            case CollectionTab.Defense: return "Savunma";
            case CollectionTab.Threats: return "Büyük tehditler";
            default: return "Tümü";
        }
    }

    /// <summary>
    /// Kilitli kartın ipucundaki tür adı ("Yaratık", "Savunma", "Büyük tehdit"); sekme kuralıyla aynıdır.
    /// Hiçbir sekmeye uymayan kelimede "Kelime".
    /// </summary>
    public static string KindName(WordEntry word)
    {
        if (Contains(CollectionTab.Threats, word)) return "Büyük tehdit";
        if (Contains(CollectionTab.Defense, word)) return "Savunma";
        if (Contains(CollectionTab.Creatures, word)) return "Yaratık";
        return "Kelime";
    }
}
