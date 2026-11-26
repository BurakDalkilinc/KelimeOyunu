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

    /// <summary>Taş duvar (karlı dağ evi): içinden geçilmez, yanmaz.</summary>
    StoneWall = 7,

    /// <summary>Taş zemin (karlı dağ evi içi, meydan): yürünür, yanmaz.</summary>
    StoneFloor = 8,

    /// <summary>Buz (donmuş göl): yürünür ama kaygan; üstünde karakter zor hızlanır, zor durur.</summary>
    Ice = 9,

    /// <summary>Dondurucu su (karlı dağda lavın yerine): üstünde durana hasar verir ve yavaşlatır; yanmaz, tutuşturmaz.</summary>
    FreezingWater = 10,

    /// <summary>Kerpiç duvar (çöl evi): içinden geçilmez, yanmaz.</summary>
    AdobeWall = 11,

    /// <summary>Kerpiç zemin (çöl evi içi, meydan): yürünür, yanmaz.</summary>
    AdobeFloor = 12,

    /// <summary>Bataklık kumu (çölde lavın yerine): yavaşlatır ve yavaş yavaş hasar verir.</summary>
    Quicksand = 13,

    /// <summary>Kaktüs: içinden geçilmez; dokunana küçük hasar verir.</summary>
    Cactus = 14,
}

/// <summary>Bir evin kapladığı dikdörtgen (duvarlar dahil, karo cinsinden). Çatı bu alanın üstüne çizilir.</summary>
public readonly struct House
{
    public int X { get; }
    public int Y { get; }
    public int W { get; }
    public int H { get; }

    public House(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public bool Contains(int x, int y) => x >= X && x < X + W && y >= Y && y < Y + H;
}

/// <summary>
/// Haritanın karo ızgarası (her kare 1×1 birim, merkezde 0,0). Hangi karonun ne olduğunu ve türlerin özelliklerini
/// (içinden geçilir mi, tehlikeli mi, yanar mı) bilir. Düz C# sınıfı, test edilebilir.
/// </summary>
public class TerrainMap
{
    readonly TileType[] tiles;

    readonly List<House> houses = new List<House>();

    public int Width { get; }
    public int Height { get; }

    /// <summary>Haritadaki evler (çatı çizimi için).</summary>
    public IReadOnlyList<House> Houses => houses;

    /// <summary>Evlerin listesi değişti (üretimde eklendi, ağdan geldi).</summary>
    public event Action HousesChanged;

    MapTheme theme = MapTheme.Village;

    /// <summary>Haritanın teması (köy, karlı dağ, çöl): zeminin ve çatıların resmi buna göre seçilir. Varsayılan köy; null verilirse köy.</summary>
    public MapTheme Theme
    {
        get => theme;
        set
        {
            MapTheme next = value ?? MapTheme.Village;
            if (next == theme) return;
            theme = next;
            ThemeChanged?.Invoke();
        }
    }

    /// <summary>Tema değişti (ağdan yeni maçın teması geldi): görünüm bütün karoları yeniden çizer.</summary>
    public event Action ThemeChanged;

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

    // ---- Evler ----

    public void AddHouse(House house)
    {
        houses.Add(house);
        HousesChanged?.Invoke();
    }

    /// <summary>Evleri "x,y,w,h;x,y,w,h" metnine çevirir (ağda karo durumuyla birlikte gider).</summary>
    public string EncodeHouses()
    {
        var parts = new List<string>();
        foreach (House h in houses) parts.Add($"{h.X},{h.Y},{h.W},{h.H}");
        return string.Join(";", parts);
    }

    /// <summary>Evleri metinden kurar; bozuksa hiçbir şey değiştirmeden false döner. Boş metin "ev yok" demektir.</summary>
    public bool TryApplyHouses(string encoded)
    {
        var parsed = new List<House>();
        if (!string.IsNullOrEmpty(encoded))
        {
            foreach (string part in encoded.Split(';'))
            {
                string[] f = part.Split(',');
                if (f.Length != 4 || !int.TryParse(f[0], out int x) || !int.TryParse(f[1], out int y)
                    || !int.TryParse(f[2], out int w) || !int.TryParse(f[3], out int h) || w < 1 || h < 1)
                    return false;
                parsed.Add(new House(x, y, w, h));
            }
        }

        houses.Clear();
        houses.AddRange(parsed);
        HousesChanged?.Invoke();
        return true;
    }

    // ---- Türlerin özellikleri ----

    // "Duvar mı, tehlikeli mi?" soruları yalnızca buradan sorulur; başka yerde karo türüne tek tek bakılmaz.

    /// <summary>Ev duvarı (tahta, taş, kerpiç): evleri kuran duvarlar.</summary>
    public static bool IsHouseWall(TileType type) => type == TileType.WoodWall || type == TileType.StoneWall || type == TileType.AdobeWall;

    /// <summary>Yapılmış zemin (tahta, taş, kerpiç): ev içi, meydan, yürüyüş yolu.</summary>
    public static bool IsBuiltFloor(TileType type) => type == TileType.WoodFloor || type == TileType.StoneFloor || type == TileType.AdobeFloor;

    /// <summary>İçinden geçilmez: ev duvarları ve kaktüs.</summary>
    public static bool IsSolid(TileType type) => IsHouseWall(type) || type == TileType.Cactus;

    /// <summary>Düşülen tehlike (lav, uçurum): giren bütün canını kaybeder. Yürünmez.</summary>
    public static bool IsHazard(TileType type) => type == TileType.Lava || type == TileType.Chasm;

    /// <summary>Üstünde durana zamanla hasar veren ama yürünebilen karo (dondurucu su, bataklık kumu).</summary>
    public static bool IsDamaging(TileType type) => type == TileType.FreezingWater || type == TileType.Quicksand;

    /// <summary>Dokunana hasar veren engel (kaktüs).</summary>
    public static bool IsPrickly(TileType type) => type == TileType.Cactus;

    /// <summary>Kaygan zemin (buz): karakter zor hızlanır, zor durur.</summary>
    public static bool IsSlippery(TileType type) => type == TileType.Ice;

    /// <summary>Su ya da kum havuzu (lav gibi çukur çizilir ama düşülmez).</summary>
    public static bool IsPool(TileType type) => type == TileType.Lava || IsDamaging(type);

    public static bool IsFlammable(TileType type) => type == TileType.WoodFloor || type == TileType.WoodWall;

    /// <summary>Alev tutabilir mi: lav, uçurum, su, kum ve yanmayan engeller (taş, kerpiç, kaktüs) alev almaz.</summary>
    public static bool CanHoldFire(TileType type) => !IsHazard(type) && !IsDamaging(type) && (!IsSolid(type) || IsFlammable(type));

    /// <summary>Üstüne basılabilir (duvar ya da düşülen tehlike değil). Dondurucu su ve bataklık kumu da yürünür.</summary>
    public static bool IsWalkable(TileType type) => !IsSolid(type) && !IsHazard(type);

    /// <summary>Güvenli zemin: yürünür ve hasar vermez. Doğma, ışınlanma ve "harita bağlı mı" hesabı buna bakar.</summary>
    public static bool IsSafeGround(TileType type) => IsWalkable(type) && !IsDamaging(type);

    /// <summary>Botların kendi isteğiyle girmediği karo: duvar, kaktüs, lav, uçurum, dondurucu su, bataklık kumu.</summary>
    public static bool IsDangerous(TileType type) => !IsSafeGround(type);

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

    /// <summary>Nokta güvenli zeminde değil mi: duvar, kaktüs, lav, uçurum, dondurucu su ya da bataklık kumu.</summary>
    public bool IsBlockedPoint(Vector2 world) => IsDangerous(At(world));

    /// <summary>
    /// <paramref name="world"/> noktasına en yakın güvenli (duvar, lav, uçurum, su, kum olmayan) karonun merkezi. Hiç yoksa false.
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
                    if (!InBounds(x, y) || !IsSafeGround(tiles[Index(x, y)])) continue;
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

    /// <summary>
    /// Güvenli karolardan en büyük bağlı bölgenin güvenli karoların kaçta kaçı olduğu (0-1). Harita bölünmüş mü anlamak için.
    /// Dondurucu su ve bataklık kumu da lav gibi bölgeleri ayırır.
    /// </summary>
    public float LargestWalkableRegionFraction()
    {
        int total = 0;
        for (int i = 0; i < tiles.Length; i++) if (IsSafeGround(tiles[i])) total++;
        if (total == 0) return 0f;

        var seen = new bool[tiles.Length];
        var stack = new Stack<int>();
        int largest = 0;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (seen[i] || !IsSafeGround(tiles[i])) continue;

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
        if (seen[index] || !IsSafeGround(tiles[index])) return;
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
            case TileType.StoneWall: return 'r';
            case TileType.StoneFloor: return 'n';
            case TileType.Ice: return 'i';
            case TileType.FreezingWater: return 'z';
            case TileType.AdobeWall: return 'a';
            case TileType.AdobeFloor: return 'b';
            case TileType.Quicksand: return 'q';
            case TileType.Cactus: return 't';
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
            case 'r': type = TileType.StoneWall; return true;
            case 'n': type = TileType.StoneFloor; return true;
            case 'i': type = TileType.Ice; return true;
            case 'z': type = TileType.FreezingWater; return true;
            case 'a': type = TileType.AdobeWall; return true;
            case 'b': type = TileType.AdobeFloor; return true;
            case 'q': type = TileType.Quicksand; return true;
            case 't': type = TileType.Cactus; return true;
            default: type = TileType.Grass; return false;
        }
    }
}
