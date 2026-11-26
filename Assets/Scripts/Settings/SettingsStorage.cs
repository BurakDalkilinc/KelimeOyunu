using System;
using UnityEngine;

/// <summary>
/// Ayarların kaydı: JSON'a çevirir, JSON'dan okur. Kayıt yoksa ya da bozuksa varsayılan ayarlarla başlar, asla çökmez.
/// Diske dokunmaz; okuma/yazma <see cref="ISettingsStore"/> üzerinden yapılır (testte bellekte).
/// </summary>
public static class SettingsStorage
{
    /// <summary>Ayarları JSON metnine çevirir (sürüm alanıyla).</summary>
    public static string ToJson(GameSettings settings)
    {
        GameSettings copy = settings != null ? settings.Clone() : new GameSettings();
        copy.Clamp();
        return JsonUtility.ToJson(copy, true);
    }

    /// <summary>JSON metnini okur; boş, bozuk ya da tanınmayan metinde varsayılan ayarlar döner. Değerler aralığa çekilir.</summary>
    public static GameSettings FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new GameSettings();

        GameSettings settings;
        try { settings = JsonUtility.FromJson<GameSettings>(json); }
        catch (Exception) { return new GameSettings(); }

        // Sürüm alanı olmayan metin bizim dosyamız değildir (örneğin "{}" ya da başka bir JSON).
        if (settings == null || settings.version <= 0) return new GameSettings();
        settings.Clamp();
        return settings;
    }

    /// <summary>Kayıttan ayarları okur.</summary>
    public static GameSettings Load(ISettingsStore store)
    {
        string text = null;
        if (store != null)
        {
            try { text = store.Read(); }
            catch (Exception exception) { Debug.LogWarning("Ayarlar okunamadı, varsayılanlarla başlanıyor: " + exception.Message); }
        }
        return FromJson(text);
    }

    /// <summary>Ayarları kaydeder. Başarılıysa true.</summary>
    public static bool Save(ISettingsStore store, GameSettings settings)
    {
        if (store == null || settings == null) return false;
        try { return store.Write(ToJson(settings)); }
        catch (Exception exception)
        {
            Debug.LogWarning("Ayarlar kaydedilemedi: " + exception.Message);
            return false;
        }
    }
}
