using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Haritanın içinden geçilemeyen karoları (tahta duvar) engel olarak verir: daire biçimli bir karakteri bu karoların dışına iter.
/// <c>blockHazards</c> açıksa lav, uçurum ve yanan karolar da engel sayılır: botlar kendi isteğiyle oralara girmesin diye kullanılır
/// (ama itilen ya da fırlatılan karakter yine de girebilir). Düz C# sınıfı, test edilebilir.
/// </summary>
public class TerrainObstacles : IObstacleField
{
    readonly TerrainMap map;
    readonly FireSim fire;
    readonly bool blockHazards;

    public TerrainObstacles(TerrainMap map, FireSim fire, bool blockHazards)
    {
        this.map = map;
        this.fire = fire;
        this.blockHazards = blockHazards;
    }

    public Vector2 Resolve(Vector2 position, float radius)
    {
        // Köşelerde birden fazla karo itebilir: iki tur.
        for (int pass = 0; pass < 2; pass++)
        {
            bool moved = false;
            Vector2Int cell = map.CellOf(position);
            int reach = Mathf.CeilToInt(radius) + 1;

            for (int y = cell.y - reach; y <= cell.y + reach; y++)
            {
                for (int x = cell.x - reach; x <= cell.x + reach; x++)
                {
                    if (!map.InBounds(x, y) || !Blocks(x, y)) continue;

                    Vector2 pushed = PushOutOfCell(position, radius, x, y);
                    if (pushed != position)
                    {
                        position = pushed;
                        moved = true;
                    }
                }
            }

            if (!moved) break;
        }
        return position;
    }

    bool Blocks(int x, int y)
    {
        TileType type = map.At(x, y);
        if (TerrainMap.IsSolid(type)) return true;
        if (!blockHazards) return false;
        return TerrainMap.IsHazard(type) || (fire != null && fire.IsBurning(x, y));
    }

    /// <summary>Daireyi karo kutusunun dışına iter; kutu içindeyse en yakın kenara çıkarır.</summary>
    Vector2 PushOutOfCell(Vector2 position, float radius, int x, int y)
    {
        Vector2 min = new Vector2(map.Origin.x + x, map.Origin.y + y);
        Vector2 max = min + Vector2.one;

        Vector2 closest = new Vector2(Mathf.Clamp(position.x, min.x, max.x), Mathf.Clamp(position.y, min.y, max.y));
        Vector2 delta = position - closest;
        float distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            if (distance >= radius) return position;
            return closest + delta / distance * radius;
        }

        // Merkez kutunun içinde: en yakın kenara doğru dışarı çık.
        float left = position.x - min.x, right = max.x - position.x, down = position.y - min.y, up = max.y - position.y;
        float smallest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
        if (smallest == left) return new Vector2(min.x - radius, position.y);
        if (smallest == right) return new Vector2(max.x + radius, position.y);
        if (smallest == down) return new Vector2(position.x, min.y - radius);
        return new Vector2(position.x, max.y + radius);
    }
}

/// <summary>Birden çok engel alanını art arda uygular (duvarlar + harita karoları).</summary>
public class CompositeObstacles : IObstacleField
{
    readonly List<IObstacleField> fields = new List<IObstacleField>();

    public CompositeObstacles(params IObstacleField[] fields)
    {
        foreach (IObstacleField field in fields)
            if (field != null) this.fields.Add(field);
    }

    public void Add(IObstacleField field)
    {
        if (field != null) fields.Add(field);
    }

    public Vector2 Resolve(Vector2 position, float radius)
    {
        // İki tur: bir engelin ittiği yer başka bir engelin içine girebilir.
        for (int pass = 0; pass < 2; pass++)
        {
            Vector2 before = position;
            foreach (IObstacleField field in fields) position = field.Resolve(position, radius);
            if (position == before) break;
        }
        return position;
    }
}
