using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Etiketlerin oyuncuya görünen adları (fast → Hızlı / Fast). Adlar kodda değil Resources/Data/tag_names.json'dadır
/// ("name" Türkçe, "nameEn" İngilizce); yeni etiket = dosyaya bir satır. İngilizce adı yoksa Türkçe adı, o da yoksa kimliği gösterilir.
/// </summary>
public class TagNames
{
    public const string DefaultResourcePath = "Data/tag_names";

    readonly Dictionary<string, string> names = new Dictionary<string, string>();
    readonly Dictionary<string, string> englishNames = new Dictionary<string, string>();

    /// <summary>JSON metninden kurar; metin bozuksa boş sözlükle başlar (çökmez).</summary>
    public static TagNames FromJson(string json)
    {
        var result = new TagNames();
        if (string.IsNullOrWhiteSpace(json)) return result;

        TagNameFile file;
        try { file = JsonUtility.FromJson<TagNameFile>(json); }
        catch (Exception) { return result; }

        if (file?.tags == null) return result;
        foreach (TagNameRecord record in file.tags)
        {
            if (record == null || string.IsNullOrEmpty(record.id)) continue;
            if (!string.IsNullOrEmpty(record.name)) result.names[record.id] = record.name;
            if (!string.IsNullOrEmpty(record.nameEn)) result.englishNames[record.id] = record.nameEn;
        }
        return result;
    }

    /// <summary>Resources'tan yükler; dosya yoksa boş sözlük.</summary>
    public static TagNames LoadFromResources(string resourcePath = DefaultResourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        return FromJson(asset != null ? asset.text : null);
    }

    /// <summary>Etiketin Türkçe adı tanımlı mı?</summary>
    public bool Has(string tag) => tag != null && names.ContainsKey(tag);

    /// <summary>Etiketin İngilizce adı tanımlı mı?</summary>
    public bool HasEnglish(string tag) => tag != null && englishNames.ContainsKey(tag);

    /// <summary>Etiketin Türkçe adı; tanımlı değilse etiketin kendisi.</summary>
    public string Get(string tag) => Get(tag, Language.Turkish);

    /// <summary>Etiketin verilen dildeki adı; İngilizce yoksa Türkçe, o da yoksa etiketin kendisi.</summary>
    public string Get(string tag, string language)
    {
        if (string.IsNullOrEmpty(tag)) return string.Empty;
        if (Language.Normalize(language) == Language.English && englishNames.TryGetValue(tag, out string english)) return english;
        return names.TryGetValue(tag, out string name) ? name : tag;
    }

    /// <summary>Kelimenin etiketleri virgülle: "Uçan, Hızlı, Sürü" (Türkçe). Etiketi yoksa boş.</summary>
    public string Describe(WordEntry word) => Describe(word, Language.Turkish);

    /// <summary>Kelimenin etiketleri verilen dilde virgülle ("Flying, Fast, Swarm"). Etiketi yoksa boş.</summary>
    public string Describe(WordEntry word, string language)
    {
        if (word?.tags == null || word.tags.Length == 0) return string.Empty;
        var list = new List<string>();
        foreach (string tag in word.tags)
            if (!string.IsNullOrEmpty(tag)) list.Add(Get(tag, language));
        return string.Join(", ", list);
    }
}
