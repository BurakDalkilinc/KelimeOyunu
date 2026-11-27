using UnityEngine;

/// <summary>
/// Yaratığın doğacağı yeri haritaya göre düzeltir: gövdesinin (yarıçapı kadar dairenin) değdiği hiçbir kare evin içinde (duvar, çatı
/// altı, kapı), içinden geçilmez (duvar, kaktüs) ya da tehlikeli (lav, uçurum, su, kum) olmamalı. Uygun değilse en yakın uygun yere
/// kaydırır. Böylece boss bir evin içinde doğup orada sıkışmaz. Düz C# sınıfı, rastgelelik yok, test edilebilir.
/// </summary>
public static class SpawnPlacement
{
    /// <summary>Gövde bu noktada durabilir mi: değdiği her kare haritanın içinde, güvenli zemin ve hiçbir evin içinde değil.</summary>
    public static bool IsClearSpot(TerrainMap map, Vector2 center, float radius)
    {
        if (map == null) return true;

        float r = Mathf.Max(0f, radius);
        Vector2Int cell = map.CellOf(center);
        int reach = Mathf.CeilToInt(r) + 1;
        for (int y = cell.y - reach; y <= cell.y + reach; y++)
        {
            for (int x = cell.x - reach; x <= cell.x + reach; x++)
            {
                if (!Touches(map, center, r, x, y)) continue;
                if (!map.InBounds(x, y)) return false; // Gövde haritadan taşıyor
                if (!TerrainMap.IsSafeGround(map.At(x, y)) || map.HouseIndexAt(x, y) >= 0) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// <paramref name="desired"/>'a en yakın, gövdenin sığdığı kare merkezini verir; nokta zaten uygunsa aynen verir. Tam yarıçapla
    /// yer yoksa (çok dolu ya da küçük harita) yarıçap küçültülerek aranır; hiç yoksa nokta olduğu gibi kalır.
    /// </summary>
    public static Vector2 FindClearSpot(TerrainMap map, Vector2 desired, float radius)
    {
        if (map == null || IsClearSpot(map, desired, radius)) return desired;

        for (float r = Mathf.Max(0f, radius); ; r *= 0.5f)
        {
            if (TryNearest(map, desired, r, out Vector2 found)) return found;
            if (r < 0.05f) break;
        }
        return desired;
    }

    /// <summary>Halka halka genişleyerek en yakın uygun kare merkezini arar; bulduğumuzdan uzak halkalara girince durur.</summary>
    static bool TryNearest(TerrainMap map, Vector2 desired, float radius, out Vector2 result)
    {
        result = desired;
        Vector2Int start = map.CellOf(desired);
        float best = float.MaxValue;
        bool found = false;
        int maxRing = Mathf.Max(map.Width, map.Height);

        for (int ring = 0; ring <= maxRing; ring++)
        {
            if (found && ring - 1 > Mathf.Sqrt(best) + 1.5f) break;

            for (int dy = -ring; dy <= ring; dy++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                    int x = start.x + dx, y = start.y + dy;
                    if (!map.InBounds(x, y)) continue;

                    Vector2 center = map.CellCenter(x, y);
                    float distance = (center - desired).sqrMagnitude;
                    if (distance >= best || !IsClearSpot(map, center, radius)) continue;

                    best = distance;
                    result = center;
                    found = true;
                }
            }
        }
        return found;
    }

    /// <summary>Daire (merkez, yarıçap) bu karenin kutusuna değiyor mu? Yarıçap 0 ise yalnızca merkezin karesi.</summary>
    static bool Touches(TerrainMap map, Vector2 center, float radius, int x, int y)
    {
        Vector2 min = new Vector2(map.Origin.x + x, map.Origin.y + y);
        Vector2 closest = new Vector2(Mathf.Clamp(center.x, min.x, min.x + 1f), Mathf.Clamp(center.y, min.y, min.y + 1f));
        if (radius <= 0f) return closest == center && map.CellOf(center) == new Vector2Int(x, y);
        return (closest - center).sqrMagnitude < radius * radius;
    }
}
