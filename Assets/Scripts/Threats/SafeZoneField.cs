using System;
using System.Collections.Generic;

/// <summary>
/// Bir tehdidin bütün çözüm alanları. Korunanları güncel tutar, süresi biten alanları kaldırır ve
/// "bu oyuncu korunuyor mu?" sorusunu cevaplar (<see cref="IProtection"/>). Düz C# sınıfı.
/// </summary>
public class SafeZoneField : IProtection
{
    readonly List<SafeZone> zones = new List<SafeZone>();

    public IReadOnlyList<SafeZone> Zones => zones;

    /// <summary>Yeni alan eklenince (ekranda çizmek için).</summary>
    public event Action<SafeZone> Added;

    /// <summary>Alan kaldırılınca.</summary>
    public event Action<SafeZone> Removed;

    public SafeZone Add(SafeZone zone)
    {
        if (zone == null) throw new ArgumentNullException(nameof(zone));

        zones.Add(zone);
        Added?.Invoke(zone);
        return zone;
    }

    /// <summary>Korunanları günceller, zamanı ilerletir, süresi biten alanları kaldırır.</summary>
    public void Update(IPlayerLocator world, float deltaSeconds)
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            SafeZone zone = zones[i];
            zone.Tick(deltaSeconds);
            if (zone.IsExpired)
            {
                zones.RemoveAt(i);
                Removed?.Invoke(zone);
                continue;
            }
            zone.UpdateOccupants(world);
        }
    }

    public bool IsProtected(int playerId)
    {
        foreach (SafeZone zone in zones)
            if (zone.IsOccupant(playerId)) return true;
        return false;
    }

    /// <summary>Tüm alanları hemen kaldırır.</summary>
    public void Clear()
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            SafeZone zone = zones[i];
            zones.RemoveAt(i);
            Removed?.Invoke(zone);
        }
    }
}
