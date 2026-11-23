using UnityEngine;

/// <summary>
/// Bir tehdidin çözümünün (counter) sayılarını hesaplar: kaç alan çıkar, her birinin kapasitesi ne kadar,
/// uyarı süresi ne kadar. Düz hesap, test edilebilir.
/// </summary>
public static class ThreatSpec
{
    /// <summary>"shadows" (gölge uyarısı) çözümü: haritada alan çıkmaz, tehdit kendi uyarısını verir.</summary>
    public const string ShadowsType = "shadows";

    /// <summary>Haritada çözüm alanı çıkaran bir counter mı?</summary>
    public static bool HasZones(WordEntry word)
    {
        return word != null && word.HasCounter && word.counter.type != ShadowsType;
    }

    /// <summary>Kaç alan çıkar (en az 1).</summary>
    public static int GetZoneCount(WordEntry word)
    {
        if (!HasZones(word)) return 0;
        return Mathf.Max(1, word.counter.count);
    }

    /// <summary>
    /// Hayattaki oyuncu sayısına göre alan sayısı. Kelimedeki sayı <see cref="GameConfig.zoneReferencePlayers"/> oyuncu için ayarlıdır;
    /// kule/mağara/ev gibi "alan başına kapasite" türleri oyuncu sayısıyla orantılı çoğalır (20 oyuncuda ~2,5 katı), en az 1.
    /// Kapasitesi zaten oyuncu sayısından hesaplanan türler (sığınak) kelimedeki sayıda kalır.
    /// </summary>
    public static int GetZoneCount(WordEntry word, int alivePlayers, GameConfig config)
    {
        int baseCount = GetZoneCount(word);
        if (baseCount == 0) return 0;
        if (word.counter.capacityPerItem <= 0) return baseCount; // Kapasite oranı ile ölçeklenen tür

        int reference = Mathf.Max(1, config.zoneReferencePlayers);
        return Mathf.Max(1, Mathf.CeilToInt(baseCount * Mathf.Max(0, alivePlayers) / (float)reference));
    }

    /// <summary>
    /// Bir alanın kapasitesi. "capacityPerItem" yazıyorsa her alan o kadar alır. Yazmıyorsa (sığınak gibi)
    /// toplam kapasite hayattaki oyuncuların "capacityRatio" oranıdır (aşağı yuvarlanır, en az GameConfig'teki alt sınır)
    /// ve alanlara bölünür.
    /// </summary>
    public static int GetZoneCapacity(WordEntry word, int alivePlayers, GameConfig config)
    {
        if (!HasZones(word)) return 0;

        WordCounter counter = word.counter;
        if (counter.capacityPerItem > 0) return counter.capacityPerItem;

        float ratio = counter.capacityRatio > 0f ? counter.capacityRatio : config.shelterCapacityRatio;
        int total = Mathf.Max(config.shelterMinCapacity, Mathf.FloorToInt(Mathf.Max(0, alivePlayers) * ratio));
        return Mathf.Max(1, Mathf.CeilToInt(total / (float)GetZoneCount(word)));
    }

    /// <summary>
    /// Tehdidin etkinleşmeden önceki uyarı süresi. Çözüm alanı çıkaran tehditlerde en az GameConfig'teki alt sınır
    /// (GDD: en az 5 sn); gölge uyarılı tehditte (meteor) tehdit uyarısı yok, her meteor kendi uyarısını verir.
    /// </summary>
    public static float GetWarningSeconds(WordEntry word, GameConfig config)
    {
        if (word == null) return 0f;
        if (!HasZones(word)) return 0f;

        float written = word.stats != null ? word.stats.warningSeconds : 0f;
        return Mathf.Max(config.globalThreatWarningSeconds, written);
    }
}
