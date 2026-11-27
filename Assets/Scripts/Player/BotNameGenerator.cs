using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tek kişilik maçta botlara gerçek oyuncu gibi görünen takma adlar verir. Adlar kodda değil Resources/Data/bot_names.json'dadır;
/// seçim maç tohumuyla yapılır (aynı tohum = aynı adlar), adlar tekrar etmez ve yerel oyuncunun adıyla çakışmaz.
/// Liste boş ya da bozuksa "Bot N" adlarına düşer. Düz hesap, test edilebilir.
/// </summary>
public class BotNameGenerator
{
    public const string DefaultResourcePath = "Data/bot_names";

    readonly List<string> names = new List<string>();

    /// <summary>Geçerli sayılan (temiz, uzunluğu uygun, tekrarsız) adlar, dosyadaki sırayla.</summary>
    public IReadOnlyList<string> Names => names;

    /// <summary>
    /// JSON metninden kurar. Temizleyiciden (<see cref="PlayerNameSanitizer.Clean"/>) değişerek çıkan, <paramref name="minLength"/>
    /// ile <paramref name="maxLength"/> arasında olmayan ya da ikinci kez geçen adlar atlanır. Metin bozuksa liste boş kalır (çökmez).
    /// </summary>
    public static BotNameGenerator FromJson(string json, int minLength, int maxLength)
    {
        var result = new BotNameGenerator();
        if (string.IsNullOrWhiteSpace(json)) return result;

        BotNameFile file;
        try { file = JsonUtility.FromJson<BotNameFile>(json); }
        catch (Exception) { return result; }

        if (file?.names == null) return result;
        foreach (string name in file.names)
            if (IsValid(name, minLength, maxLength) && !ContainsIgnoreCase(result.names, name)) result.names.Add(name);
        return result;
    }

    /// <summary>Resources'tan yükler; dosya yoksa liste boş kalır.</summary>
    public static BotNameGenerator LoadFromResources(string resourcePath, int minLength, int maxLength)
    {
        TextAsset asset = string.IsNullOrEmpty(resourcePath) ? null : Resources.Load<TextAsset>(resourcePath);
        return FromJson(asset != null ? asset.text : null, minLength, maxLength);
    }

    /// <summary>Ad listeye girebilir mi? Temizleyiciden aynen geçmeli ve uzunluğu sınırlar içinde olmalı.</summary>
    public static bool IsValid(string name, int minLength, int maxLength)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (name.Length < minLength || name.Length > maxLength) return false;
        return PlayerNameSanitizer.Clean(name, maxLength) == name;
    }

    /// <summary>Liste yetmediğinde ya da takma adlar kapalıyken kullanılan ad: "Bot 1", "Bot 2"...</summary>
    public static string FallbackName(int botNumber) => "Bot " + botNumber;

    /// <summary>
    /// <paramref name="count"/> bot için ad seçer. Listeden <paramref name="taken"/>'dakiler (büyük/küçük harf ayrımı yok) çıkarılır,
    /// kalanlar <paramref name="seed"/> ile karıştırılır ve sırayla verilir. Liste biterse kalan botlar "Bot N" alır
    /// (o da alınmışsa " (2)" eklenir). <paramref name="taken"/> değiştirilmez.
    /// </summary>
    public List<string> Pick(int count, int seed, ICollection<string> taken)
    {
        var used = new List<string>();
        if (taken != null) used.AddRange(taken);

        var pool = new List<string>();
        foreach (string name in names)
            if (!ContainsIgnoreCase(used, name)) pool.Add(name);

        // Fisher-Yates karıştırma: tohum aynıysa sıra da aynı.
        var random = new System.Random(seed);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var result = new List<string>(Math.Max(0, count));
        for (int i = 0; i < count; i++)
        {
            string name = i < pool.Count
                ? pool[i]
                : PlayerNameSanitizer.MakeUnique(FallbackName(i + 1), used, PlayerNameSanitizer.DefaultMaxLength);
            used.Add(name);
            result.Add(name);
        }
        return result;
    }

    /// <summary>
    /// Tek kişilik maçın bot adları: ayarda takma adlar açıksa listeden (<see cref="Pick"/>), kapalıysa "Bot 1", "Bot 2"...
    /// </summary>
    public static List<string> ForSinglePlayer(GameConfig config, int count, int seed, ICollection<string> taken)
    {
        BotNameGenerator generator = config != null && config.singlePlayerFakeBotNames
            ? LoadFromResources(config.botNamesPath, config.botNameMinLength, config.botNameMaxLength)
            : new BotNameGenerator();
        return generator.Pick(count, seed, taken);
    }

    static bool ContainsIgnoreCase(List<string> list, string name)
    {
        foreach (string other in list)
            if (string.Equals(other, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    [Serializable]
    class BotNameFile
    {
        public int version;
        public string[] names;
    }
}
