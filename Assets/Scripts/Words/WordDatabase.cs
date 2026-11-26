using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// words.json içindeki kelimeleri yükler ve yazılan metni bir kelime kaydıyla eşleştirir.
/// id, görünen ad, İngilizce ad (displayEn) ve eş anlamlılar aksansız/büyük-küçük harf fark etmeden eşleşir;
/// oyuncunun seçtiği dil eşleştirmeyi etkilemez (sunucu dilden bağımsızdır).
/// Hiçbir kelimeye özel kod yoktur; davranış etiketlerden gelir.
/// Bu sınıf değişmezdir: kelime kilitleme maç başına <see cref="MatchWordTracker"/> içindedir.
/// </summary>
public class WordDatabase
{
    public const string DefaultResourcePath = "Data/words";

    readonly List<WordEntry> words = new List<WordEntry>();
    readonly Dictionary<string, WordEntry> byKey = new Dictionary<string, WordEntry>();
    readonly HashSet<string> blockedKeys = new HashSet<string>();
    readonly List<string> englishConflicts = new List<string>();
    readonly int maxLength;

    /// <summary>Yüklenen tüm kelimeler (dosyadaki sırayla).</summary>
    public IReadOnlyList<WordEntry> Words => words;

    /// <summary>
    /// Başka bir kelimenin anahtarıyla çakıştığı için eşleştirmeye eklenemeyen İngilizce adlar (boş olmalı; test korur).
    /// Çakışan İngilizce ad yazılırsa diğer kelime bulunur.
    /// </summary>
    public IReadOnlyList<string> EnglishNameConflicts => englishConflicts;

    /// <summary>Kimliğiyle kelimeyi bulur; yoksa null.</summary>
    public WordEntry FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (WordEntry entry in words)
            if (entry.id == id) return entry;
        return null;
    }

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

        // İngilizce adlar en son eklenir: oyuncu hangi dilde oynarsa oynasın kelimeyi İngilizce de yazabilsin.
        // Başka bir kelimenin id/ad/eş anlamlısıyla çakışan İngilizce ad yüklemeyi bozmaz, yalnızca listelenir (test yakalar).
        foreach (WordEntry entry in words)
            RegisterEnglishName(entry);

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

    void RegisterEnglishName(WordEntry entry)
    {
        string key = WordNormalizer.ToMatchKey(entry.displayEn);
        if (key.Length == 0) return;

        if (byKey.TryGetValue(key, out WordEntry existing))
        {
            if (!ReferenceEquals(existing, entry))
                englishConflicts.Add($"'{entry.displayEn}' ({entry.id}) zaten '{existing.id}' kelimesinin anahtarı");
            return;
        }

        byKey.Add(key, entry);
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
