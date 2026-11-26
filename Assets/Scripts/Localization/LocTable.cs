using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tek bir dilin metin tablosu (anahtar → metin). Metinler kodda değil Resources/Data/lang_xx.json dosyasındadır;
/// yeni metin = iki dosyaya birer satır. Bozuk ya da eksik dosyada boş tabloyla başlar, çökmez.
/// </summary>
public class LocTable
{
    public const string ResourcePrefix = "Data/lang_";

    readonly Dictionary<string, string> texts = new Dictionary<string, string>();

    /// <summary>Tablodaki anahtarlar.</summary>
    public IEnumerable<string> Keys => texts.Keys;

    /// <summary>Tablodaki metin sayısı.</summary>
    public int Count => texts.Count;

    /// <summary>Dosyada iki kez yazılmış anahtarlar (sonraki yazılan geçerlidir; test bunu yakalar).</summary>
    public List<string> DuplicateKeys { get; } = new List<string>();

    /// <summary>Dilin Resources yolu: "Data/lang_tr".</summary>
    public static string ResourcePathFor(string language) => ResourcePrefix + language;

    /// <summary>JSON metninden kurar; metin boş ya da bozuksa boş tablo.</summary>
    public static LocTable FromJson(string json)
    {
        var table = new LocTable();
        if (string.IsNullOrWhiteSpace(json)) return table;

        LocFile file;
        try { file = JsonUtility.FromJson<LocFile>(json); }
        catch (Exception) { return table; }

        if (file?.entries == null) return table;
        foreach (LocEntry entry in file.entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            if (table.texts.ContainsKey(entry.key)) table.DuplicateKeys.Add(entry.key);
            table.texts[entry.key] = entry.value ?? string.Empty;
        }
        return table;
    }

    /// <summary>Kod içinden tablo kurar (testler için).</summary>
    public static LocTable FromPairs(IDictionary<string, string> pairs)
    {
        var table = new LocTable();
        if (pairs == null) return table;
        foreach (KeyValuePair<string, string> pair in pairs)
            if (!string.IsNullOrEmpty(pair.Key)) table.texts[pair.Key] = pair.Value ?? string.Empty;
        return table;
    }

    /// <summary>Resources'tan yükler; dosya yoksa boş tablo.</summary>
    public static LocTable LoadFromResources(string language)
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePathFor(language));
        return FromJson(asset != null ? asset.text : null);
    }

    /// <summary>Anahtarın metni; anahtar yoksa ya da değeri boşsa false.</summary>
    public bool TryGet(string key, out string value)
    {
        if (key != null && texts.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return true;
        value = null;
        return false;
    }

    /// <summary>Anahtarın ham değeri (boş olabilir); anahtar yoksa null. Testler için.</summary>
    public string RawValue(string key) => key != null && texts.TryGetValue(key, out string value) ? value : null;
}
