using UnityEngine;

/// <summary>
/// Zeminin harekete etkisi: dondurucu su ve bataklık kumu yavaşlatır, buz kaydırır. Uçan ve hayalet karakter zeminden etkilenmez.
/// Değerler <see cref="GameConfig"/>'ten gelir. Düz hesap, test edilebilir.
/// </summary>
public static class GroundRules
{
    /// <summary>Zeminin hız çarpanı (1 = normal).</summary>
    public static float SpeedMultiplier(TileType ground, GameConfig config)
    {
        if (config == null) return 1f;
        switch (ground)
        {
            case TileType.FreezingWater: return config.freezingWaterSpeedMultiplier;
            case TileType.Quicksand: return config.quicksandSpeedMultiplier;
            default: return 1f;
        }
    }

    /// <summary>
    /// Üstünde durana zamanla hasar veren zeminin değerleri: hasar, aralık ve girdikten sonra ilk hasara kadar bekleme.
    /// Dondurucu su girişte hemen vurur; bataklık kumu yavaş batırır (ilk hasar bir aralık sonra). Hasar vermeyen zeminde false.
    /// </summary>
    public static bool TryGetGroundDamage(TileType ground, GameConfig config, out int damage, out float interval, out float firstDelay)
    {
        damage = 0;
        interval = firstDelay = 0f;
        if (config == null) return false;

        switch (ground)
        {
            case TileType.FreezingWater:
                damage = config.freezingWaterDamage;
                interval = Mathf.Max(0.1f, config.freezingWaterDamageIntervalSeconds);
                firstDelay = 0f;
                return true;
            case TileType.Quicksand:
                damage = config.quicksandDamage;
                interval = Mathf.Max(0.1f, config.quicksandDamageIntervalSeconds);
                firstDelay = interval;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Karakterin bastığı zemin. Harita yoksa, karakter uçuyorsa ya da hayaletse çimen sayılır (yani hiçbir etki yok).
    /// </summary>
    public static TileType GroundUnder(PlayerState player, TerrainMap map, Vector2 position)
    {
        if (map == null || player == null || player.IsGhost || player.Status.IsFlying) return TileType.Grass;
        return map.At(position);
    }
}
