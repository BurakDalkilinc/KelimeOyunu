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

    /// <summary>İngilizce görünen ad (İngilizce oynayan görür; her dilde yazılınca da kelimeyi bulur).</summary>
    public string displayEn;

    /// <summary>İngilizce açıklama (defterde).</summary>
    public string descriptionEn;

    /// <summary>Isteğe bağlı: Resources/Sprites içindeki sprite adı. Boşsa kelime kimliği (id) kullanılır.</summary>
    public string sprite;

    /// <summary>
    /// İsteğe bağlı görsel tema (büyük tehditlerde): "nuke", "solar", "ice", "toxic", "lava". Uyarı süresince ekranın rengi ve
    /// parçacıklar, etki anındaki parlamanın rengi buna göre seçilir (bkz. <see cref="ThreatFxPreset"/>). Kural değiştirmez.
    /// </summary>
    public string fx;

    /// <summary>Büyük tehdit mi? (Tehdidin çözümü de haritada çıkar.)</summary>
    public bool IsThreat => category == CategoryThreat;

    /// <summary>Çözüm (counter) tanımlı mı? JSON'da "counter": null ise false.</summary>
    public bool HasCounter => counter != null && !string.IsNullOrEmpty(counter.type);

    /// <summary>Verilen dildeki görünen ad: İngilizce'de <see cref="displayEn"/>, yoksa Türkçe ad, o da yoksa id.</summary>
    public string DisplayFor(string language)
    {
        if (Language.Normalize(language) == Language.English && !string.IsNullOrWhiteSpace(displayEn)) return displayEn;
        return string.IsNullOrWhiteSpace(display) ? id : display;
    }

    /// <summary>Verilen dildeki açıklama: İngilizce'de <see cref="descriptionEn"/>, yoksa Türkçe açıklama.</summary>
    public string DescriptionFor(string language)
    {
        if (Language.Normalize(language) == Language.English && !string.IsNullOrWhiteSpace(descriptionEn)) return descriptionEn;
        return description ?? string.Empty;
    }

    /// <summary>Seçili dildeki görünen ad (bkz. <see cref="Loc.Current"/>).</summary>
    public string LocalizedDisplay => DisplayFor(Loc.Current);

    /// <summary>Seçili dildeki açıklama.</summary>
    public string LocalizedDescription => DescriptionFor(Loc.Current);

    /// <summary>Kelime bu etikete sahip mi?</summary>
    public bool HasTag(string tag)
    {
        if (tags == null) return false;
        return Array.IndexOf(tags, tag) >= 0;
    }
}
