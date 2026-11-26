using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Etiketlerin oyuncuya görünen Türkçe adları (fast → Hızlı). Adlar kodda değil Resources/Data/tag_names.json'dadır;
/// yeni etiket = dosyaya bir satır. Adı yazılmamış etiket kimliğiyle gösterilir.
/// </summary>
public class TagNames
{
    public const string DefaultResourcePath = "Data/tag_names";

    readonly Dictionary<string, string> names = new Dictionary<string, string>();

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
            if (record != null && !string.IsNullOrEmpty(record.id) && !string.IsNullOrEmpty(record.name))
                result.names[record.id] = record.name;
        return result;
    }

    /// <summary>Resources'tan yükler; dosya yoksa boş sözlük.</summary>
    public static TagNames LoadFromResources(string resourcePath = DefaultResourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        return FromJson(asset != null ? asset.text : null);
    }

    /// <summary>Etiketin adı tanımlı mı?</summary>
    public bool Has(string tag) => tag != null && names.ContainsKey(tag);

    /// <summary>Etiketin Türkçe adı; tanımlı değilse etiketin kendisi.</summary>
    public string Get(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return string.Empty;
        return names.TryGetValue(tag, out string name) ? name : tag;
    }

    /// <summary>Kelimenin etiketleri virgülle: "Uçan, Hızlı, Sürü". Etiketi yoksa boş.</summary>
    public string Describe(WordEntry word)
    {
        if (word?.tags == null || word.tags.Length == 0) return string.Empty;
        var list = new List<string>();
        foreach (string tag in word.tags)
            if (!string.IsNullOrEmpty(tag)) list.Add(Get(tag));
        return string.Join(", ", list);
    }
}
