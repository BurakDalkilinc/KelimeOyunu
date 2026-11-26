using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Bu bilgisayardaki oyuncunun tek koleksiyon defteri: ilk istenince words.json ve kayıt dosyasından kurulur, sahneler arasında
/// aynı kalır. Kaydı maç sonunda, sahneden çıkarken ve oyun kapanırken yazılır. Kişisel ilerlemedir; sunucuya gitmez.
/// </summary>
public static class CollectionService
{
    const string ConfigResourcePath = "Data/GameConfig";

    static CollectionBook book;
    static TagNames tagNames;
    static GameConfig config;
    static ICollectionStore store;
    static bool quitHooked;

    /// <summary>Domain reload kapalıyken önceki oynatmanın defteri kalmasın.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        book = null;
        tagNames = null;
        config = null;
        store = null;
        if (quitHooked) Application.quitting -= OnQuitting;
        quitHooked = false;
    }

    /// <summary>Ayarlar (Resources/Data/GameConfig); bulunamazsa koddaki varsayılanlar.</summary>
    public static GameConfig Config
    {
        get
        {
            if (config == null)
            {
                config = Resources.Load<GameConfig>(ConfigResourcePath);
                if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
            }
            return config;
        }
    }

    /// <summary>Defter; ilk çağrıda yüklenir.</summary>
    public static CollectionBook Book
    {
        get
        {
            if (book == null) Load();
            return book;
        }
    }

    /// <summary>Etiketlerin Türkçe adları.</summary>
    public static TagNames Tags
    {
        get
        {
            if (tagNames == null) tagNames = TagNames.LoadFromResources(Config.collectionTagNamesPath);
            return tagNames;
        }
    }

    /// <summary>Defteri diske yazar.</summary>
    public static void Save()
    {
        if (book == null || store == null) return;
        CollectionStorage.Save(store, book);
    }

    static void Load()
    {
        WordDatabase words = null;
        try { words = WordDatabase.LoadFromResources(Config.maxWordLength); }
        catch (Exception exception) { Debug.LogWarning("Koleksiyon defteri: kelimeler yüklenemedi: " + exception.Message); }

        store = new FileCollectionStore(Path.Combine(Application.persistentDataPath, Config.collectionFileName));
        book = CollectionStorage.Load(store, words?.Words);

        if (!quitHooked)
        {
            Application.quitting += OnQuitting;
            quitHooked = true;
        }
    }

    static void OnQuitting() => Save();
}
