using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Botlara "şuraya sığın" diyen hesap: tehditlerin çözüm alanlarına bakıp oyuncunun korunup korunmadığını
/// ve gitmesi gereken noktayı bulur. Tek kişilik sahnedeki <see cref="ThreatDirector"/> ve ağ maçındaki host simülasyonu
/// aynı hesabı kullanır. Düz C# sınıfı, test edilebilir.
/// </summary>
public static class RefugeFinder
{
    /// <summary>Oyuncu herhangi bir tehdidin çözüm alanında şu an korunuyor mu?</summary>
    public static bool IsSheltered(IReadOnlyList<GlobalThreat> threats, PlayerState player)
    {
        foreach (GlobalThreat threat in threats)
            if (threat.Zones.IsProtected(player.Id)) return true;
        return false;
    }

    /// <summary>
    /// Oyuncunun sığınacağı yeri verir: zaten içinde olduğu alan varsa orada kalır, yoksa en yakın boş alana gider.
    /// Sığınılacak yer yoksa false.
    /// </summary>
    public static bool TryGetRefuge(IReadOnlyList<GlobalThreat> threats, PlayerState player, Vector2 position, out Vector2 point)
    {
        point = Vector2.zero;
        float bestDistance = float.MaxValue;
        bool found = false;

        foreach (GlobalThreat threat in threats)
        {
            foreach (SafeZone zone in threat.Zones.Zones)
            {
                if (zone.IsOccupant(player.Id))
                {
                    point = zone.Position; // Zaten korunuyor: orada kal.
                    return true;
                }

                if (zone.Free <= 0) continue;

                float distance = (zone.Position - position).magnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    point = zone.Position;
                    found = true;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Bu ev, süren bir küresel tehdidin sığınağı mı? Evlere sığınılan ("houses") tehdidin çözüm alanı evin dikdörtgenine değiyorsa
    /// evet: tehdit sürerken (bitene dek) bu ev yıkılmaz, çözüm haritada kalır.
    /// </summary>
    public static bool IsHouseRefuge(IReadOnlyList<GlobalThreat> threats, TerrainMap map, House house)
    {
        if (threats == null || map == null) return false;

        Vector2 min = map.CellCenter(house.X, house.Y) - new Vector2(0.5f, 0.5f);
        Vector2 max = min + new Vector2(house.W, house.H);
        foreach (GlobalThreat threat in threats)
        {
            if (threat == null || threat.Phase == ThreatPhase.Finished) continue;
            foreach (SafeZone zone in threat.Zones.Zones)
            {
                if (zone.Type != SafeZone.HousesType) continue;
                Vector2 closest = new Vector2(Mathf.Clamp(zone.Position.x, min.x, max.x), Mathf.Clamp(zone.Position.y, min.y, max.y));
                if ((closest - zone.Position).sqrMagnitude <= zone.Radius * zone.Radius) return true;
            }
        }
        return false;
    }
}
