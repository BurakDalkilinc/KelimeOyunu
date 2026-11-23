using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Haritadaki bir karonun türü.</summary>
public enum TileType : byte
{
    /// <summary>Çimen/toprak: yürünür, yanmaz.</summary>
    Grass = 0,

    /// <summary>Patika: yürünür, yanmaz.</summary>
    Path = 1,

    /// <summary>Tahta zemin (meydan, yürüyüş yolu, ev içi): yürünür, yanar.</summary>
    WoodFloor = 2,

    /// <summary>Tahta duvar (ev): içinden geçilmez, yanar; yanınca çöker.</summary>
    WoodWall = 3,

    /// <summary>Lav: üstünde durana hasar verir.</summary>
    Lava = 4,

    /// <summary>Uçurum: düşen can kaybeder ve kenara döner.</summary>
    Chasm = 5,

    /// <summary>Kömürleşmiş zemin (yanmış tahta): yürünür, bir daha yanmaz.</summary>
    CharredFloor = 6,
}

/// <summary>
/// Haritanın karo ızgarası (her kare 1×1 birim, merkezde 0,0). Hangi karonun ne olduğunu ve türlerin özelliklerini
/// (içinden geçilir mi, tehlikeli mi, yanar mı) bilir. Düz C# sınıfı, test edilebilir.
/// </summary>
public class TerrainMap
{
    readonly TileType[] tiles;

    public int Width { get; }
    public int Height { get; }

    /// <summary>(0,0) karosunun sol alt köşesinin dünya konumu.</summary>
    public Vector2 Origin { get; }

    /// <summary>Bir karo değişince (yanıp kömürleşme gibi) karonun sütun ve satırıyla çağrılır.</summary>
    public event Action<int, int> TileChanged;

    public TerrainMap(int width, int height)
    {
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), "Harita en az 1×1 olmalı.");
        Width = width;
        Height = height;
        Origin = new Vector2(-width * 0.5f, -height * 0.5f);
        tiles = new TileType[width * height];
    }

    // ---- Türlerin özellikleri ----

    public static bool IsSolid(TileType type) => type == TileType.WoodWall;
    public static bool IsHazard(TileType type) => type == TileType.Lava || type == TileType.Chasm;
    public static bool IsFlammable(TileType type) => type == TileType.WoodFloor || type == TileType.WoodWall;
    public static bool IsWalkable(TileType type) => !IsSolid(type) && !IsHazard(type);

    // ---- Erişim ----

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public int Index(int x, int y) => y * Width + x;

    /// <summary>Karonun türü; harita dışı için çimen (engelsiz) sayılır.</summary>
    public TileType At(int x, int y) => InBounds(x, y) ? tiles[Index(x, y)] : TileType.Grass;

    public TileType At(Vector2 world)
    {
        Vector2Int cell = CellOf(world);
        return At(cell.x, cell.y);
    }

    public void Set(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        int index = Index(x, y);
        if (tiles[index] == type) return;

        tiles[index] = type;
        TileChanged?.Invoke(x, y);
    }

    /// <summary>Dünya konumunun düştüğü karo (harita dışındaysa en yakın kenar karosu).</summary>
    public Vector2Int CellOf(Vector2 world)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(world.x - Origin.x), 0, Width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(world.y - Origin.y), 0, Height - 1);
        return new Vector2Int(x, y);
    }

    public Vector2 CellCenter(int x, int y) => new Vector2(Origin.x + x + 0.5f, Origin.y + y + 0.5f);

    /// <summary>Karo, haritanın tamamını (yürünür mü) etkileyen bir "engel" mi: duvar, lav ya da uçurum.</summary>
    public bool IsBlockedPoint(Vector2 world)
    {
        TileType type = At(world);
        return IsSolid(type) || IsHazard(type);
    }

    /// <summary>
    /// <paramref name="world"/> noktasına en yakın yürünür (duvar, lav, uçurum olmayan) karonun merkezi. Hiç yoksa false.
    /// <paramref name="avoid"/> verilirse true döndüren karolar (ör. yanan) atlanır.
    /// </summary>
    public bool TryFindNearestWalkable(Vector2 world, out Vector2 result, Func<int, int, bool> avoid = null)
    {
        Vector2Int start = CellOf(world);
        float best = float.MaxValue;
        result = world;
        bool found = false;

        // Halka halka genişleyen arama: bulduğumuz en iyiden uzak halkalara girince dur.
        int maxRadius = Mathf.Max(Width, Height);
        for (int radius = 0; radius <= maxRadius; radius++)
        {
            if (found && radius - 1 > Mathf.Sqrt(best) + 1.5f) break;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;

                    int x = start.x + dx, y = start.y + dy;
                    if (!InBounds(x, y) || !IsWalkable(tiles[Index(x, y)])) continue;
                    if (avoid != null && avoid(x, y)) continue;

                    Vector2 center = CellCenter(x, y);
                    float distance = (center - world).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        result = center;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    /// <summary>Verilen türdeki karoların sayısı.</summary>
    public int Count(TileType type)
    {
        int count = 0;
        foreach (TileType tile in tiles)
            if (tile == type) count++;
        return count;
    }

    /// <summary>Yürünür karolardan en büyük bağlı bölgenin yürünür karoların kaçta kaçı olduğu (0-1). Harita bölünmüş mü anlamak için.</summary>
    public float LargestWalkableRegionFraction()
    {
        int total = 0;
        for (int i = 0; i < tiles.Length; i++) if (IsWalkable(tiles[i])) total++;
        if (total == 0) return 0f;

        var seen = new bool[tiles.Length];
        var stack = new Stack<int>();
        int largest = 0;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (seen[i] || !IsWalkable(tiles[i])) continue;

            int size = 0;
            stack.Push(i);
            seen[i] = true;
            while (stack.Count > 0)
            {
                int current = stack.Pop();
                size++;
                int cx = current % Width, cy = current / Width;
                TryVisit(cx + 1, cy, seen, stack);
                TryVisit(cx - 1, cy, seen, stack);
                TryVisit(cx, cy + 1, seen, stack);
                TryVisit(cx, cy - 1, seen, stack);
            }
            largest = Mathf.Max(largest, size);
        }

        return largest / (float)total;
    }

    void TryVisit(int x, int y, bool[] seen, Stack<int> stack)
    {
        if (!InBounds(x, y)) return;
        int index = Index(x, y);
        if (seen[index] || !IsWalkable(tiles[index])) return;
        seen[index] = true;
        stack.Push(index);
    }

    // ---- Ağ için kısa metin ----

    /// <summary>Her karo için bir harf (türün baş harfi); yanan karo büyük harf olur. Satır satır, aşağıdan yukarı.</summary>
    public string Encode(Func<int, int, bool> isBurning)
    {
        var chars = new char[tiles.Length];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char c = LetterOf(tiles[Index(x, y)]);
                chars[Index(x, y)] = isBurning != null && isBurning(x, y) ? char.ToUpperInvariant(c) : c;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// Metni haritaya uygular; yanma durumunu <paramref name="burning"/> bool dizisine yazar (null olabilir).
    /// Boyut ya da harf bozuksa hiçbir şey değiştirmeden false döner.
    /// </summary>
    public bool TryApply(string encoded, bool[] burning)
    {
        if (encoded == null || encoded.Length != tiles.Length) return false;
        if (burning != null && burning.Length != tiles.Length) return false;

        var parsed = new TileType[tiles.Length];
        var flames = new bool[tiles.Length];
        for (int i = 0; i < encoded.Length; i++)
        {
            char c = encoded[i];
            bool upper = char.IsUpper(c);
            if (!TryTypeOf(char.ToLowerInvariant(c), out parsed[i])) return false;
            flames[i] = upper;
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == parsed[i]) continue;
            tiles[i] = parsed[i];
            TileChanged?.Invoke(i % Width, i / Width);
        }
        if (burning != null) Array.Copy(flames, burning, flames.Length);
        return true;
    }

    static char LetterOf(TileType type)
    {
        switch (type)
        {
            case TileType.Path: return 'p';
            case TileType.WoodFloor: return 'f';
            case TileType.WoodWall: return 'w';
            case TileType.Lava: return 'l';
            case TileType.Chasm: return 'c';
            case TileType.CharredFloor: return 'k';
            default: return 'g';
        }
    }

    static bool TryTypeOf(char letter, out TileType type)
    {
        switch (letter)
        {
            case 'g': type = TileType.Grass; return true;
            case 'p': type = TileType.Path; return true;
            case 'f': type = TileType.WoodFloor; return true;
            case 'w': type = TileType.WoodWall; return true;
            case 'l': type = TileType.Lava; return true;
            case 'c': type = TileType.Chasm; return true;
            case 'k': type = TileType.CharredFloor; return true;
            default: type = TileType.Grass; return false;
        }
    }
}
