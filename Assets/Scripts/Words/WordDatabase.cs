using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// words.json içindeki kelimeleri yükler ve yazılan metni bir kelime kaydıyla eşleştirir.
/// id, görünen ad ve eş anlamlılar aksansız/büyük-küçük harf fark etmeden eşleşir.
/// Hiçbir kelimeye özel kod yoktur; davranış etiketlerden gelir.
/// Bu sınıf değişmezdir: kelime kilitleme maç başına <see cref="MatchWordTracker"/> içindedir.
/// </summary>
public class WordDatabase
{
    public const string DefaultResourcePath = "Data/words";

    readonly List<WordEntry> words = new List<WordEntry>();
    readonly Dictionary<string, WordEntry> byKey = new Dictionary<string, WordEntry>();
    readonly HashSet<string> blockedKeys = new HashSet<string>();
    readonly int maxLength;

    /// <summary>Yüklenen tüm kelimeler (dosyadaki sırayla).</summary>
    public IReadOnlyList<WordEntry> Words => words;

    /// <summary>
    /// Dosya verisinden veritabanı kurar. Dosyada hata varsa (aynı eş anlamlı iki kelimede,
    /// eksik id, ...) tüm hataları listeleyen bir <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    public WordDatabase(WordFile file, int maxLength)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        this.maxLength = maxLength;

        var errors = new List<string>();
        var ids = new HashSet<string>();

        if (file.words != null)
        {
            foreach (WordEntry entry in file.words)
                AddEntry(entry, ids, errors);
        }

        if (file.blocked != null)
        {
            foreach (string blocked in file.blocked)
            {
                string key = WordNormalizer.ToMatchKey(blocked);
                if (key.Length > 0) blockedKeys.Add(key);
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("words.json hatalı:\n- " + string.Join("\n- ", errors));
    }

    /// <summary>JSON metninden veritabanı kurar.</summary>
    public static WordDatabase FromJson(string json, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("words.json boş.", nameof(json));

        WordFile file = JsonUtility.FromJson<WordFile>(json);
        return new WordDatabase(file, maxLength);
    }

    /// <summary>Resources klasöründen yükler (varsayılan: Resources/Data/words.json).</summary>
    public static WordDatabase LoadFromResources(int maxLength, string resourcePath = DefaultResourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
            throw new InvalidOperationException("Kelime dosyası bulunamadı: Resources/" + resourcePath);

        return FromJson(asset.text, maxLength);
    }

    /// <summary>
    /// Yazılan metni arar. Kilit kontrolü yapmaz (bkz. <see cref="MatchWordTracker"/>).
    /// Sıra: boş → çok uzun → yasaklı → bulundu / bilinmiyor.
    /// </summary>
    public WordLookupResult Lookup(string input)
    {
        string key = WordNormalizer.ToMatchKey(input);
        if (key.Length == 0)
            return new WordLookupResult(WordLookupStatus.Empty);

        if (input.Trim().Length > maxLength)
            return new WordLookupResult(WordLookupStatus.TooLong);

        if (blockedKeys.Contains(key))
            return new WordLookupResult(WordLookupStatus.Blocked);

        if (byKey.TryGetValue(key, out WordEntry entry))
            return new WordLookupResult(WordLookupStatus.Found, entry);

        return new WordLookupResult(WordLookupStatus.Unknown);
    }

    void AddEntry(WordEntry entry, HashSet<string> ids, List<string> errors)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.id))
        {
            errors.Add("id'si olmayan bir kelime kaydı var.");
            return;
        }

        if (!ids.Add(entry.id))
        {
            errors.Add($"'{entry.id}' id'si iki kez kullanılmış.");
            return;
        }

        words.Add(entry);

        RegisterKey(entry.id, entry, errors);
        RegisterKey(entry.display, entry, errors);
        if (entry.aliases == null) return;
        foreach (string alias in entry.aliases)
            RegisterKey(alias, entry, errors);
    }

    void RegisterKey(string text, WordEntry entry, List<string> errors)
    {
        string key = WordNormalizer.ToMatchKey(text);
        if (key.Length == 0) return;

        if (byKey.TryGetValue(key, out WordEntry existing))
        {
            if (!ReferenceEquals(existing, entry))
                errors.Add($"'{text}' hem '{existing.id}' hem '{entry.id}' kaydında geçiyor.");
            return;
        }

        byKey.Add(key, entry);
    }
}
