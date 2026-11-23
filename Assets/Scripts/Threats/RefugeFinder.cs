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
}
