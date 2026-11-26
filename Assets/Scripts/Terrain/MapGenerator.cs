using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Harita üretiminin ayarları: kaç köy, havuz, uçurum, buz gölü, kaktüs ve hangi tema.</summary>
public struct MapRecipe
{
    public int Villages;

    /// <summary>Tema havuzu sayısı: köyde lav, karda dondurucu su, çölde bataklık kumu.</summary>
    public int LavaPools;
    public int Chasms;

    /// <summary>Donmuş (kaygan) göl sayısı.</summary>
    public int IcePonds;

    /// <summary>Dağınık kaktüs sayısı.</summary>
    public int Cacti;

    /// <summary>Tema; boşsa (null) köy.</summary>
    public MapTheme Theme;

    /// <summary>
    /// Açıksa havuz, buz gölü ya da kaktüs istenip haritaya hiç sığmadıysa harita yeniden denenir (temanın kimliği her haritada görünsün).
    /// Köy tarifinde kapalı: köy haritası eskisiyle birebir aynı kalır.
    /// </summary>
    public bool RequireFeatures;

    public MapTheme ThemeOrDefault => Theme ?? MapTheme.Village;

    /// <summary>İlk harita: tahta köyler, lav, uçurum.</summary>
    public static MapRecipe Default => new MapRecipe { Villages = 3, LavaPools = 2, Chasms = 2, Theme = MapTheme.Village };

    /// <summary>Karlı dağ: taş köyler, dondurucu su, buz gölleri, uçurumlar.</summary>
    public static MapRecipe Snow => new MapRecipe { Villages = 3, LavaPools = 2, Chasms = 2, IcePonds = 3, Theme = MapTheme.Snow, RequireFeatures = true };

    /// <summary>Çöl kasabası: seyrek kerpiç köyler, geniş açıklık, bataklık kumu, kaktüsler, tek uçurum.</summary>
    public static MapRecipe Desert => new MapRecipe { Villages = 2, LavaPools = 3, Chasms = 1, Cacti = 16, Theme = MapTheme.Desert, RequireFeatures = true };

    /// <summary>Temanın tarifi.</summary>
    public static MapRecipe For(MapTheme theme)
    {
        if (theme == MapTheme.Snow) return Snow;
        if (theme == MapTheme.Desert) return Desert;
        return Default;
    }
}

/// <summary>
/// Tohumdan harita üretir: köyler (meydan, yürüyüş yolları, kapılı evler), havuzlar (lav / dondurucu su / bataklık kumu), uçurum
/// yarıkları, patikalar; temaya göre buz gölleri ve kaktüsler. Evin ve havuzun karosu temadan (<see cref="MapTheme"/>) gelir.
/// Aynı tohum her zaman aynı haritayı verir. Harita bölünürse (yürünür alanın çoğu birbirinden kopuksa) başka bir deneme yapılır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class MapGenerator
{
    const int MaxAttempts = 30;
    const float MinConnectedFraction = 0.93f;
    const int EdgeMargin = 2;
    /// <summary>Ev boyutu (duvarlar dahil). İç mekân 3×2, kapı 2 kare geniş: çapı 1 olan karakter rahat geçer.</summary>
    public const int HouseWidth = 5;
    public const int HouseHeight = 4;

    /// <summary>Sokak ve evler arası yürüyüş yolu genişliği (kare). 2: karakterler duvara sürtmeden yan yana geçebilir.</summary>
    const int StreetWidth = 2;

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
            bool featuresOk = !recipe.RequireFeatures || HasRequiredFeatures(map, recipe);
            if (fraction >= MinConnectedFraction && featuresOk) return map;

            // Bağlı olmak her şeyden önemli; özellikleri tam olan deneme biraz öne geçer.
            float score = fraction + (featuresOk && fraction >= MinConnectedFraction ? 1f : 0f);
            if (score > bestFraction)
            {
                best = map;
                bestFraction = score;
            }
        }

        return best; // En iyi deneme (pratikte bu satıra gelinmez)
    }

    /// <summary>Tarifte istenen her özellikten (havuz, buz gölü, kaktüs) haritada en az bir tane var mı?</summary>
    static bool HasRequiredFeatures(TerrainMap map, MapRecipe recipe)
    {
        if (recipe.LavaPools > 0 && map.Count(recipe.ThemeOrDefault.PoolTile) == 0) return false;
        if (recipe.IcePonds > 0 && map.Count(TileType.Ice) == 0) return false;
        if (recipe.Cacti > 0 && map.Count(TileType.Cactus) == 0) return false;
        return true;
    }

    static TerrainMap BuildOnce(int width, int height, int seed, MapRecipe recipe)
    {
        MapTheme theme = recipe.ThemeOrDefault;
        var map = new TerrainMap(width, height) { Theme = theme };
        var random = new System.Random(seed);
        var villages = new List<Box>();

        // Önce köyler (yerleşmeleri zor), sonra buz gölleri (geniş yer ister), havuz ve uçurum kalan boşlukta. Lav, köyün
        // yürüyüş yoluna değebilir: yoldaki tahta tutuşunca yangın köye doğru yayılır. Kaktüsler en son.
        // Köy temasında buz ve kaktüs yok (sayıları 0): rastgele sayı çekilmez, köy haritası eskisiyle birebir aynı kalır.
        PlaceVillages(map, random, recipe.Villages, villages, theme);
        ConnectVillagesWithPaths(map, villages, random);
        PlaceIcePonds(map, random, recipe.IcePonds, villages);
        PlacePools(map, random, recipe.LavaPools, villages, theme.PoolTile);
        PlaceChasms(map, random, recipe.Chasms, villages);
        PlaceCacti(map, random, recipe.Cacti, villages);

        return map;
    }

    // ---- Köyler ----

    /// <summary>
    /// Köy: iki sütun ev (aralarında 2 karelik dikey yol). Büyük köyde iki sıra ev ve aralarında 2 karelik sokak,
    /// küçük köyde tek sıra ev ve altında sokak vardır. Yollar köyün dışına 2 kare taşar. Evlerin kapısı sokağa bakar.
    /// </summary>
    static void PlaceVillages(TerrainMap map, System.Random random, int count, List<Box> villages, MapTheme theme)
    {
        int villageW = HouseWidth * 2 + StreetWidth;
        int bigH = HouseHeight * 2 + StreetWidth;
        int smallH = HouseHeight + StreetWidth;

        for (int i = 0; i < count; i++)
        {
            // İlk köyler büyük; sığmazsa küçüğe düşer.
            bool placed = false;
            foreach (bool big in new[] { i < theme.BigVillages, false })
            {
                int villageH = big ? bigH : smallH;
                for (int attempt = 0; attempt < 120 && !placed; attempt++)
                {
                    int margin = EdgeMargin + 1; // Yürüyüş yolları köyün 2 kare dışına taşar: kenarda 1 kare boşluk kalır
                    int x0 = random.Next(margin, Math.Max(margin + 1, map.Width - villageW - margin));
                    int y0 = random.Next(margin, Math.Max(margin + 1, map.Height - villageH - margin));
                    var box = new Box { X0 = x0, Y0 = y0, X1 = x0 + villageW - 1, Y1 = y0 + villageH - 1 };

                    if (box.X1 >= map.Width - EdgeMargin || box.Y1 >= map.Height - EdgeMargin) continue;
                    if (villages.Exists(v => v.Overlaps(box, theme.VillageGap))) continue;

                    BuildVillage(map, box, big, random, theme);
                    villages.Add(box);
                    placed = true;
                }
                if (placed) break;
            }
        }
    }

    static void BuildVillage(TerrainMap map, Box box, bool big, System.Random random, MapTheme theme)
    {
        TileType floor = theme.FloorTile;
        int gapX = box.X0 + HouseWidth;                   // Dikey yol (2 kare geniş): gapX ve gapX+1
        int streetY = big ? box.Y0 + HouseHeight : box.Y0; // Sokak (2 kare yüksek): streetY ve streetY+1

        // Yürüyüş yolları (köyün dışına 2 kare taşar).
        for (int x = box.X0 - 2; x <= box.X1 + 2; x++)
            for (int t = 0; t < StreetWidth; t++) SetIfGrass(map, x, streetY + t, floor);
        for (int y = box.Y0 - 2; y <= box.Y1 + 2; y++)
            for (int t = 0; t < StreetWidth; t++) SetIfGrass(map, gapX + t, y, floor);

        int rightX = gapX + StreetWidth;
        var houses = new List<(int x, int y, bool doorUp)>();
        if (big)
        {
            houses.Add((box.X0, box.Y0, true));                         // Alt sıra: kapı yukarı (sokağa)
            houses.Add((rightX, box.Y0, true));
            houses.Add((box.X0, streetY + StreetWidth, false));         // Üst sıra: kapı aşağı
            houses.Add((rightX, streetY + StreetWidth, false));
        }
        else
        {
            houses.Add((box.X0, streetY + StreetWidth, false));         // Tek sıra: sokağın üstünde, kapı aşağı
            houses.Add((rightX, streetY + StreetWidth, false));
        }

        // Büyük köyde bir ev eksik olabilir (köyler birbirine benzemesin); en az 3 ev kalır.
        int skip = big && random.Next(3) == 0 ? 1 : -1;
        for (int i = 0; i < houses.Count; i++)
            if (i != skip) BuildHouse(map, houses[i].x, houses[i].y, houses[i].doorUp, random, theme);
    }

    /// <summary>
    /// Ev: çevresi duvar, içi zemin (3×2), sokağa bakan duvarda 2 kare geniş kapı. Duvar ve zemin temadan gelir
    /// (tahta, taş, kerpiç). Evin yeri haritaya kaydedilir (çatı çizimi için).
    /// </summary>
    static void BuildHouse(TerrainMap map, int x0, int y0, bool doorUp, System.Random random, MapTheme theme)
    {
        for (int y = y0; y < y0 + HouseHeight; y++)
        {
            for (int x = x0; x < x0 + HouseWidth; x++)
            {
                bool border = x == x0 || x == x0 + HouseWidth - 1 || y == y0 || y == y0 + HouseHeight - 1;
                map.Set(x, y, border ? theme.WallTile : theme.FloorTile);
            }
        }

        // Kapı: iç mekânın üstündeki iki kare (soldan ya da sağdan hizalı).
        int doorX = x0 + 1 + random.Next(0, 2);
        int doorY = doorUp ? y0 + HouseHeight - 1 : y0;
        map.Set(doorX, doorY, theme.FloorTile);
        map.Set(doorX + 1, doorY, theme.FloorTile);

        map.AddHouse(new House(x0, y0, HouseWidth, HouseHeight));
    }

    static void SetIfGrass(TerrainMap map, int x, int y, TileType floor)
    {
        if (map.InBounds(x, y) && map.At(x, y) == TileType.Grass) map.Set(x, y, floor);
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

    // ---- Havuzlar (lav, dondurucu su, bataklık kumu) ve uçurum ----

    static void PlacePools(TerrainMap map, System.Random random, int count, List<Box> villages, TileType poolTile)
    {
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 250; attempt++)
            {
                float rx = 1.2f + (float)random.NextDouble() * 0.8f;
                float ry = 1.0f + (float)random.NextDouble() * 0.8f;
                int cx = Roll(random, EdgeMargin + 2, map.Width - EdgeMargin - 2);
                int cy = Roll(random, EdgeMargin + 2, map.Height - EdgeMargin - 2);

                int reach = Mathf.CeilToInt(Mathf.Max(rx, ry));
                var lavaBox = new Box { X0 = cx - reach, Y0 = cy - reach, X1 = cx + reach, Y1 = cy + reach };
                if (villages.Exists(v => v.Overlaps(lavaBox, 1)) || HasHazardNear(map, cx, cy, reach + 1)) continue;

                for (int y = cy - reach; y <= cy + reach; y++)
                {
                    for (int x = cx - reach; x <= cx + reach; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        float noise = (float)random.NextDouble() * 0.35f;
                        if (dx * dx + dy * dy <= 1f + noise - 0.2f && map.InBounds(x, y) && !IsBuilt(map.At(x, y)))
                            map.Set(x, y, poolTile);
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
            for (int attempt = 0; attempt < 250; attempt++)
            {
                bool horizontal = random.Next(2) == 0;
                int length = random.Next(6, 11);
                int thickness = random.Next(1, 3);
                int x0 = Roll(random, EdgeMargin, map.Width - EdgeMargin - (horizontal ? length : thickness));
                int y0 = Roll(random, EdgeMargin, map.Height - EdgeMargin - (horizontal ? thickness : length));

                int w = horizontal ? length : thickness, h = horizontal ? thickness : length;
                var chasmBox = new Box { X0 = x0, Y0 = y0, X1 = x0 + w - 1, Y1 = y0 + h - 1 };
                if (villages.Exists(v => v.Overlaps(chasmBox, 1)) || HasHazardNear(map, x0 + w / 2, y0 + h / 2, Math.Max(w, h) / 2 + 2)) continue;

                for (int y = y0; y < y0 + h; y++)
                {
                    for (int x = x0; x < x0 + w; x++)
                    {
                        // Hafif dalgalı kenar: uçlardaki kareleri ara sıra atla.
                        bool end = horizontal ? (x == x0 || x == x0 + w - 1) : (y == y0 || y == y0 + h - 1);
                        if (end && random.Next(2) == 0) continue;
                        if (map.InBounds(x, y) && !IsBuilt(map.At(x, y))) map.Set(x, y, TileType.Chasm);
                    }
                }
                break;
            }
        }
    }

    // ---- Buz gölleri ve kaktüsler ----

    /// <summary>Donmuş göller: köylerden ve tehlikelerden uzak, çimenin/patikanın üstüne oval buz.</summary>
    static void PlaceIcePonds(TerrainMap map, System.Random random, int count, List<Box> villages)
    {
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 250; attempt++)
            {
                float rx = 1.6f + (float)random.NextDouble() * 1.2f;
                float ry = 1.2f + (float)random.NextDouble() * 1.0f;
                int cx = Roll(random, EdgeMargin + 2, map.Width - EdgeMargin - 2);
                int cy = Roll(random, EdgeMargin + 2, map.Height - EdgeMargin - 2);

                int reach = Mathf.CeilToInt(Mathf.Max(rx, ry));
                var pondBox = new Box { X0 = cx - reach, Y0 = cy - reach, X1 = cx + reach, Y1 = cy + reach };
                if (villages.Exists(v => v.Overlaps(pondBox, 1)) || HasFeatureNear(map, cx, cy, reach)) continue;

                for (int y = cy - reach; y <= cy + reach; y++)
                {
                    for (int x = cx - reach; x <= cx + reach; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        float noise = (float)random.NextDouble() * 0.3f;
                        if (dx * dx + dy * dy <= 1f + noise - 0.15f && map.InBounds(x, y) && IsOpenGround(map.At(x, y)))
                            map.Set(x, y, TileType.Ice);
                    }
                }
                break;
            }
        }
    }

    /// <summary>Dağınık kaktüsler: her biri tek karo, çevresi (3×3) açık zemin olan yerlere; köylere ve birbirine bitişik değil.</summary>
    static void PlaceCacti(TerrainMap map, System.Random random, int count, List<Box> villages)
    {
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Roll(random, EdgeMargin + 1, map.Width - EdgeMargin - 1);
                int y = Roll(random, EdgeMargin + 1, map.Height - EdgeMargin - 1);
                if (map.At(x, y) != TileType.Grass || villages.Exists(v => v.Contains(x, y, 1))) continue;
                if (!IsOpenAround(map, x, y)) continue;

                map.Set(x, y, TileType.Cactus);
                break;
            }
        }
    }

    /// <summary>Karonun 3×3 çevresinin tamamı açık zemin (çimen, patika) mı?</summary>
    static bool IsOpenAround(TerrainMap map, int cx, int cy)
    {
        for (int y = cy - 1; y <= cy + 1; y++)
            for (int x = cx - 1; x <= cx + 1; x++)
                if (!map.InBounds(x, y) || !IsOpenGround(map.At(x, y))) return false;
        return true;
    }

    /// <summary>[min, max) aralığında rastgele sayı; aralık boşsa (küçük harita) min.</summary>
    static int Roll(System.Random random, int min, int max) => max <= min ? min : random.Next(min, max);

    /// <summary>Köyün parçası (ev duvarı ya da yapılmış zemin): havuz ve uçurum bunu ezmez.</summary>
    static bool IsBuilt(TileType type) => TerrainMap.IsHouseWall(type) || TerrainMap.IsBuiltFloor(type);

    /// <summary>Üstüne buz/kaktüs konabilecek açık zemin.</summary>
    static bool IsOpenGround(TileType type) => type == TileType.Grass || type == TileType.Path;

    /// <summary>Yakında lav, uçurum, dondurucu su ya da bataklık kumu var mı?</summary>
    static bool HasHazardNear(TerrainMap map, int cx, int cy, int radius)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (!map.InBounds(x, y)) continue;
                TileType type = map.At(x, y);
                if (TerrainMap.IsHazard(type) || TerrainMap.IsDamaging(type)) return true;
            }
        }
        return false;
    }

    /// <summary>Yakında açık zemin (çimen, patika) olmayan bir şey (tehlike, buz, kaktüs, köy) var mı?</summary>
    static bool HasFeatureNear(TerrainMap map, int cx, int cy, int radius)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if (map.InBounds(x, y) && !IsOpenGround(map.At(x, y))) return true;
        return false;
    }
}
