using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Harita üretiminin ayarları.</summary>
public struct MapRecipe
{
    public int Villages;
    public int LavaPools;
    public int Chasms;

    public static MapRecipe Default => new MapRecipe { Villages = 3, LavaPools = 2, Chasms = 2 };
}

/// <summary>
/// Tohumdan harita üretir: tahta köyler (meydan, yürüyüş yolları, kapılı evler), lav havuzları, uçurum yarıkları, patikalar.
/// Aynı tohum her zaman aynı haritayı verir. Harita bölünürse (yürünür alanın çoğu birbirinden kopuksa) başka bir deneme yapılır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class MapGenerator
{
    const int MaxAttempts = 30;
    const float MinConnectedFraction = 0.93f;
    const int EdgeMargin = 2;
    const int HouseWidth = 4;
    const int HouseHeight = 3;

    /// <summary>Köy alanının kapladığı kutu (kenarlar dahil). Lav ve uçurum buradan uzak durur.</summary>
    struct Box
    {
        public int X0, Y0, X1, Y1; // X1, Y1 dahil
        public bool Overlaps(Box other, int gap) => X0 - gap <= other.X1 && X1 + gap >= other.X0 && Y0 - gap <= other.Y1 && Y1 + gap >= other.Y0;
        public bool Contains(int x, int y, int gap) => x >= X0 - gap && x <= X1 + gap && y >= Y0 - gap && y <= Y1 + gap;
    }

    public static TerrainMap Generate(int width, int height, int seed, MapRecipe recipe)
    {
        TerrainMap best = null;
        float bestFraction = -1f;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            TerrainMap map = BuildOnce(width, height, seed + attempt * 7919, recipe);
            float fraction = map.LargestWalkableRegionFraction();
            if (fraction >= MinConnectedFraction) return map;

            if (fraction > bestFraction)
            {
                best = map;
                bestFraction = fraction;
            }
        }

        return best; // En iyi deneme (pratikte bu satıra gelinmez)
    }

    static TerrainMap BuildOnce(int width, int height, int seed, MapRecipe recipe)
    {
        var map = new TerrainMap(width, height);
        var random = new System.Random(seed);
        var villages = new List<Box>();

        PlaceVillages(map, random, recipe.Villages, villages);
        ConnectVillagesWithPaths(map, villages, random);
        PlaceLava(map, random, recipe.LavaPools, villages);
        PlaceChasms(map, random, recipe.Chasms, villages);

        return map;
    }

    // ---- Köyler ----

    static void PlaceVillages(TerrainMap map, System.Random random, int count, List<Box> villages)
    {
        // Köy: 2×2 ev (her biri 4×3) ve aralarında 1 karelik tahta yol; yollar köyün dışına 2 kare uzanır.
        int villageW = HouseWidth * 2 + 1;
        int villageH = HouseHeight * 2 + 1;

        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int margin = EdgeMargin + 3;
                int x0 = random.Next(margin, Math.Max(margin + 1, map.Width - villageW - margin));
                int y0 = random.Next(margin, Math.Max(margin + 1, map.Height - villageH - margin));
                var box = new Box { X0 = x0, Y0 = y0, X1 = x0 + villageW - 1, Y1 = y0 + villageH - 1 };

                if (box.X1 >= map.Width - EdgeMargin || box.Y1 >= map.Height - EdgeMargin) continue;
                if (villages.Exists(v => v.Overlaps(box, 3))) continue;

                BuildVillage(map, box, random);
                villages.Add(box);
                break;
            }
        }
    }

    static void BuildVillage(TerrainMap map, Box box, System.Random random)
    {
        int midX = box.X0 + HouseWidth;   // Dikey yürüyüş yolu
        int midY = box.Y0 + HouseHeight;  // Yatay yürüyüş yolu

        // Yürüyüş yolları (köyün dışına 2 kare taşar).
        for (int x = box.X0 - 2; x <= box.X1 + 2; x++) SetIfGrass(map, x, midY);
        for (int y = box.Y0 - 2; y <= box.Y1 + 2; y++) SetIfGrass(map, midX, y);

        // Dört ev; ikisi yoksa köy boş kalır, o yüzden en az iki ev yapılır.
        var origins = new List<Vector2Int>
        {
            new Vector2Int(box.X0, box.Y0),
            new Vector2Int(midX + 1, box.Y0),
            new Vector2Int(box.X0, midY + 1),
            new Vector2Int(midX + 1, midY + 1),
        };
        int skip = random.Next(0, 3); // 0-2 ev eksik: köyler birbirine benzemesin
        for (int i = 0; i < origins.Count; i++)
        {
            if (skip > 0 && i >= origins.Count - skip) continue;
            BuildHouse(map, origins[i].x, origins[i].y, i < 2, random);
        }
    }

    /// <summary>Evin kapısı yola bakar: alttaki evler (i&lt;2) yukarıya, üsttekiler aşağıya.</summary>
    static void BuildHouse(TerrainMap map, int x0, int y0, bool doorTop, System.Random random)
    {
        for (int y = y0; y < y0 + HouseHeight; y++)
        {
            for (int x = x0; x < x0 + HouseWidth; x++)
            {
                bool border = x == x0 || x == x0 + HouseWidth - 1 || y == y0 || y == y0 + HouseHeight - 1;
                map.Set(x, y, border ? TileType.WoodWall : TileType.WoodFloor);
            }
        }

        // Kapı: yola bakan kenarın ortasındaki kare (2 kare geniş ev içi için soldan ikinci).
        int doorX = x0 + 1 + random.Next(0, HouseWidth - 2);
        int doorY = doorTop ? y0 + HouseHeight - 1 : y0;
        map.Set(doorX, doorY, TileType.WoodFloor);
    }

    static void SetIfGrass(TerrainMap map, int x, int y)
    {
        if (map.InBounds(x, y) && map.At(x, y) == TileType.Grass) map.Set(x, y, TileType.WoodFloor);
    }

    static void ConnectVillagesWithPaths(TerrainMap map, List<Box> villages, System.Random random)
    {
        // Köyleri sırayla L biçiminde patikayla bağla (çimenin üzerine; tahtayı ezmez).
        for (int i = 0; i + 1 < villages.Count; i++)
        {
            Vector2Int a = Center(villages[i]);
            Vector2Int b = Center(villages[i + 1]);

            int x = a.x, y = a.y;
            while (x != b.x) { SetPath(map, x, y); x += Math.Sign(b.x - x); }
            while (y != b.y) { SetPath(map, x, y); y += Math.Sign(b.y - y); }
        }
    }

    static Vector2Int Center(Box box) => new Vector2Int((box.X0 + box.X1) / 2, (box.Y0 + box.Y1) / 2);

    static void SetPath(TerrainMap map, int x, int y)
    {
        if (map.InBounds(x, y) && map.At(x, y) == TileType.Grass) map.Set(x, y, TileType.Path);
    }

    // ---- Lav ve uçurum ----

    static void PlaceLava(TerrainMap map, System.Random random, int count, List<Box> villages)
    {
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                float rx = 1.3f + (float)random.NextDouble() * 1.1f;
                float ry = 1.0f + (float)random.NextDouble() * 1.0f;
                int cx = Roll(random, EdgeMargin + 3, map.Width - EdgeMargin - 3);
                int cy = Roll(random, EdgeMargin + 3, map.Height - EdgeMargin - 3);

                int reach = Mathf.CeilToInt(Mathf.Max(rx, ry)) + 1;
                bool clear = true;
                foreach (Box v in villages)
                    if (v.Contains(cx - reach, cy - reach, 2) || v.Contains(cx + reach, cy + reach, 2) || v.Contains(cx, cy, reach + 2)) clear = false;
                if (!clear || HasHazardNear(map, cx, cy, reach + 2)) continue;

                for (int y = cy - reach; y <= cy + reach; y++)
                {
                    for (int x = cx - reach; x <= cx + reach; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        float noise = (float)random.NextDouble() * 0.35f;
                        if (dx * dx + dy * dy <= 1f + noise - 0.2f && map.InBounds(x, y) && !IsWood(map.At(x, y)))
                            map.Set(x, y, TileType.Lava);
                    }
                }
                break;
            }
        }
    }

    static void PlaceChasms(TerrainMap map, System.Random random, int count, List<Box> villages)
    {
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                bool horizontal = random.Next(2) == 0;
                int length = random.Next(5, 9);
                int thickness = random.Next(1, 3);
                int x0 = Roll(random, EdgeMargin + 3, map.Width - EdgeMargin - 3 - (horizontal ? length : thickness));
                int y0 = Roll(random, EdgeMargin + 3, map.Height - EdgeMargin - 3 - (horizontal ? thickness : length));

                int w = horizontal ? length : thickness, h = horizontal ? thickness : length;
                bool clear = true;
                foreach (Box v in villages)
                    if (v.Overlaps(new Box { X0 = x0, Y0 = y0, X1 = x0 + w - 1, Y1 = y0 + h - 1 }, 2)) clear = false;
                if (!clear || HasHazardNear(map, x0 + w / 2, y0 + h / 2, Math.Max(w, h) / 2 + 3)) continue;

                for (int y = y0; y < y0 + h; y++)
                {
                    for (int x = x0; x < x0 + w; x++)
                    {
                        // Hafif dalgalı kenar: uçlardaki kareleri ara sıra atla.
                        bool end = horizontal ? (x == x0 || x == x0 + w - 1) : (y == y0 || y == y0 + h - 1);
                        if (end && random.Next(2) == 0) continue;
                        if (map.InBounds(x, y) && !IsWood(map.At(x, y))) map.Set(x, y, TileType.Chasm);
                    }
                }
                break;
            }
        }
    }

    /// <summary>[min, max) aralığında rastgele sayı; aralık boşsa (küçük harita) min.</summary>
    static int Roll(System.Random random, int min, int max) => max <= min ? min : random.Next(min, max);

    static bool IsWood(TileType type) => type == TileType.WoodFloor || type == TileType.WoodWall;

    static bool HasHazardNear(TerrainMap map, int cx, int cy, int radius)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if (map.InBounds(x, y) && TerrainMap.IsHazard(map.At(x, y))) return true;
        return false;
    }
}
