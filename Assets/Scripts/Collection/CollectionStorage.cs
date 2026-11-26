using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defterin kaydı: JSON'a çevirir, JSON'dan okur. Kayıt yoksa ya da bozuksa boş defterle başlar, asla çökmez.
/// Diske dokunmaz; okuma/yazma <see cref="ICollectionStore"/> üzerinden yapılır (testte bellekte).
/// </summary>
public static class CollectionStorage
{
    /// <summary>Defteri JSON metnine çevirir (sürüm alanıyla).</summary>
    public static string ToJson(CollectionBook book)
    {
        CollectionSaveData data = book != null ? book.ToSaveData() : new CollectionSaveData();
        data.version = CollectionSaveData.CurrentVersion;
        return JsonUtility.ToJson(data, true);
    }

    /// <summary>JSON metnini okur; boş, bozuk ya da tanınmayan metinde boş kayıt döner.</summary>
    public static CollectionSaveData FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new CollectionSaveData();

        CollectionSaveData data;
        try { data = JsonUtility.FromJson<CollectionSaveData>(json); }
        catch (Exception) { return new CollectionSaveData(); }

        // Sürüm alanı olmayan metin bizim dosyamız değildir (örneğin "{}" ya da başka bir JSON).
        if (data == null || data.version <= 0) return new CollectionSaveData();
        if (data.entries == null) data.entries = new List<CollectionEntry>();
        return data;
    }

    /// <summary>Kayıttan defteri kurar.</summary>
    public static CollectionBook Load(ICollectionStore store, IEnumerable<WordEntry> words)
    {
        string text = null;
        if (store != null)
        {
            try { text = store.Read(); }
            catch (Exception exception) { Debug.LogWarning("Koleksiyon defteri okunamadı, boş defterle başlanıyor: " + exception.Message); }
        }
        return new CollectionBook(words, FromJson(text));
    }

    /// <summary>Defteri kaydeder. Başarılıysa true.</summary>
    public static bool Save(ICollectionStore store, CollectionBook book)
    {
        if (store == null || book == null) return false;
        try { return store.Write(ToJson(book)); }
        catch (Exception exception)
        {
            Debug.LogWarning("Koleksiyon defteri kaydedilemedi: " + exception.Message);
            return false;
        }
    }
}
