using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Harita temaları (köy, karlı dağ, çöl): seçim, üretim, bağlılık, evler, sığınaklar, ağ metni.</summary>
public class MapThemeTests
{
    const int W = 34, H = 19;

    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public bool TryGetPosition(int id, out Vector2 p) => Positions.TryGetValue(id, out p);
        public bool TryApplyKnockback(int id, Vector2 d) => Positions.ContainsKey(id);
        public bool TryGetFacing(int id, out Vector2 d) { d = Vector2.up; return false; }
        public bool TryTeleport(int id, Vector2 to) { Positions[id] = to; return true; }
    }

    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(W, H);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static IEnumerable<MapTheme> Themes => MapTheme.All;

    static string Snapshot(TerrainMap map) => map.Encode(null) + "|" + map.EncodeHouses();

    /// <summary>
    /// Parmak izi için temalardan önceki biçim: karolar + evlerin yalnızca dikdörtgeni ("x,y,w,h"). Kapı bilgisi sonradan eklendi;
    /// haritanın kendisi (karolar, evlerin yeri) değişmemeli.
    /// </summary>
    static string LegacySnapshot(TerrainMap map)
    {
        var rects = new List<string>();
        foreach (House h in map.Houses) rects.Add($"{h.X},{h.Y},{h.W},{h.H}");
        return map.Encode(null) + "|" + string.Join(";", rects);
    }

    /// <summary>FNV-1a: eski köy haritasının parmak izi.</summary>
    static uint Fingerprint(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }

    // ---- Tema seçimi ----

    [Test]
    public void Choose_ForcedThemeWins_OtherwiseSameSeedSameTheme()
    {
        Assert.AreSame(MapTheme.Snow, MapTheme.Choose("kar", 5));
        Assert.AreSame(MapTheme.Desert, MapTheme.Choose(" COL ", 5));
        Assert.AreSame(MapTheme.Village, MapTheme.Choose("koy", 5));

        for (int seed = 1; seed < 50; seed++)
            Assert.AreSame(MapTheme.Choose("", seed), MapTheme.Choose(null, seed));
        Assert.AreSame(MapTheme.Choose("bilinmeyen", 9), MapTheme.Choose("", 9), "Bilinmeyen tema rastgele seçime düşer");
    }

    [Test]
    public void Choose_Random_PicksEveryThemeAcrossSeeds()
    {
        var seen = new HashSet<string>();
        for (int seed = 1; seed <= 60; seed++) seen.Add(MapTheme.Choose("", seed).Id);
        Assert.AreEqual(3, seen.Count);
    }

    [Test]
    public void Config_ForcedTheme_IsUsedByTerrainSim()
    {
        config.forcedMapTheme = "col";
        Assert.AreSame(MapTheme.Desert, TerrainSim.ThemeFor(config, 123));

        var sim = new TerrainSim(config, 123, TerrainSim.ThemeFor(config, 123), new System.Random(1));
        Assert.AreSame(MapTheme.Desert, sim.Map.Theme);
        Assert.Greater(sim.Map.Count(TileType.AdobeWall), 0);
    }

    [Test]
    public void FromId_UnknownOrEmpty_IsVillage()
    {
        Assert.AreSame(MapTheme.Village, MapTheme.FromId(""));
        Assert.AreSame(MapTheme.Village, MapTheme.FromId(null));
        Assert.AreSame(MapTheme.Village, MapTheme.FromId("xyz"));
        Assert.AreSame(MapTheme.Snow, MapTheme.FromId("kar"));
    }

    // ---- Üretim ----

    [Test]
    public void EveryTheme_IsDeterministicForTheSameSeed([ValueSource(nameof(Themes))] MapTheme theme)
    {
        for (int seed = 1; seed <= 5; seed++)
        {
            string a = Snapshot(MapGenerator.Generate(W, H, seed, MapRecipe.For(theme)));
            string b = Snapshot(MapGenerator.Generate(W, H, seed, MapRecipe.For(theme)));
            Assert.AreEqual(a, b, $"{theme} tohum {seed}");
        }
        Assert.AreSame(theme, MapGenerator.Generate(W, H, 1, MapRecipe.For(theme)).Theme);
    }

    [Test]
    public void VillageTheme_IsIdenticalToTheOriginalMap()
    {
        // Parmak izleri, kapılar hep güney duvarına alındıktan sonra yenilendi (eskisi: 1 → 2262998696, 7 → 637149040,
        // 42 → 2114121051, 123 → 2527582666, 2024 → 2024983474). Evlerin yeri ve havuzlar aynı; yalnızca büyük köylerin alt
        // sırasındaki kapılar alta taşındı (tohum 7'de kapının önündeki iki uçurum karesi de zemine çevrildi).
        var expected = new Dictionary<int, uint>
        {
            { 1, 3552467176u }, { 7, 2830676206u }, { 42, 1822894795u }, { 123, 4047525594u }, { 2024, 252577226u },
        };
        foreach (KeyValuePair<int, uint> pair in expected)
        {
            Assert.AreEqual(pair.Value, Fingerprint(LegacySnapshot(MapGenerator.Generate(W, H, pair.Key, MapRecipe.Default))), $"tohum {pair.Key}");
            Assert.AreEqual(pair.Value, Fingerprint(LegacySnapshot(MapGenerator.Generate(W, H, pair.Key, MapRecipe.For(MapTheme.Village)))));
        }

        // Tema verilmeyen tarif de köydür.
        string plain = LegacySnapshot(MapGenerator.Generate(W, H, 7, new MapRecipe { Villages = 3, LavaPools = 2, Chasms = 2 }));
        Assert.AreEqual(expected[7], Fingerprint(plain));
    }

    [Test]
    public void EveryTheme_StaysConnected_AndEdgesAreSafe([ValueSource(nameof(Themes))] MapTheme theme)
    {
        for (int seed = 1; seed <= 30; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.For(theme));
            Assert.GreaterOrEqual(map.LargestWalkableRegionFraction(), 0.93f, $"{theme} tohum {seed}: harita bölünmüş");

            for (int x = 0; x < W; x++)
            {
                Assert.IsTrue(TerrainMap.IsSafeGround(map.At(x, 0)), $"{theme} {seed}: alt kenarda engel");
                Assert.IsTrue(TerrainMap.IsSafeGround(map.At(x, H - 1)), $"{theme} {seed}: üst kenarda engel");
            }
        }
    }

    [Test]
    public void SnowMap_HasStoneHousesIceAndWater_AndNothingBurns()
    {
        int withIce = 0, withWater = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.Snow);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.IsFalse(TerrainMap.IsFlammable(map.At(x, y)), $"tohum {seed}: yanan karo ({x},{y}) {map.At(x, y)}");

            Assert.Greater(map.Count(TileType.StoneWall), 10, $"tohum {seed}: taş ev yok");
            Assert.Greater(map.Count(TileType.StoneFloor), 10);
            Assert.AreEqual(0, map.Count(TileType.Lava));
            if (map.Count(TileType.Ice) > 0) withIce++;
            if (map.Count(TileType.FreezingWater) > 0) withWater++;
        }
        Assert.AreEqual(20, withIce, "Her karlı haritada buz gölü olmalı");
        Assert.AreEqual(20, withWater, "Her karlı haritada dondurucu su olmalı");
    }

    [Test]
    public void SnowMap_FireNeverSpreadsOrChars()
    {
        config.fireSpreadChancePerSecond = 50f;
        TerrainMap map = MapGenerator.Generate(W, H, 4, MapRecipe.Snow);
        string before = map.Encode(null);
        var sim = new TerrainSim(config, map, new System.Random(1));

        sim.Fire.IgniteArea(Vector2.zero, 40f); // Bütün harita
        for (int i = 0; i < 200; i++) sim.Fire.Tick(0.1f);

        Assert.AreEqual(0, map.Count(TileType.CharredFloor));
        Assert.AreEqual(0, sim.Fire.BurningCount, "Taş/kar üstündeki kısa alevler sönmeli");
        Assert.AreEqual(before, map.Encode(null), "Hiçbir karo değişmemeli");
    }

    [Test]
    public void DesertMap_HasAdobeHousesQuicksandAndCacti()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.Desert);
            Assert.Greater(map.Count(TileType.Quicksand), 0, $"tohum {seed}: bataklık kumu yok");
            Assert.Greater(map.Count(TileType.Cactus), 3, $"tohum {seed}: kaktüs yok");
            Assert.Greater(map.Count(TileType.AdobeWall), 10, $"tohum {seed}: kerpiç ev yok");
            Assert.AreEqual(0, map.Count(TileType.Lava));
            Assert.AreEqual(0, map.Count(TileType.WoodWall));
        }
    }

    [Test]
    public void DesertMap_HasFewerHousesThanVillage()
    {
        int desert = 0, village = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            desert += MapGenerator.Generate(W, H, seed, MapRecipe.Desert).Houses.Count;
            village += MapGenerator.Generate(W, H, seed, MapRecipe.Default).Houses.Count;
        }
        Assert.Less(desert, village);
    }

    [Test]
    public void EveryTheme_HasHousesWithThemeWallsAndTwoTileDoors([ValueSource(nameof(Themes))] MapTheme theme)
    {
        int withHouses = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.For(theme));
            if (map.Houses.Count > 0) withHouses++;

            foreach (House house in map.Houses)
            {
                Assert.AreEqual(theme.WallTile, map.At(house.X, house.Y), $"{theme} {seed}: köşe temanın duvarı olmalı");
                Assert.IsTrue(TerrainMap.IsHouseWall(map.At(house.X, house.Y)));

                int best = 0, run = 0;
                for (int x = house.X; x < house.X + house.W; x++)
                {
                    bool open = !TerrainMap.IsSolid(map.At(x, house.Y)) || !TerrainMap.IsSolid(map.At(x, house.Y + house.H - 1));
                    run = open ? run + 1 : 0;
                    best = Mathf.Max(best, run);
                }
                Assert.GreaterOrEqual(best, 2, $"{theme} {seed}: kapı en az 2 geniş olmalı");
            }
        }
        Assert.Greater(withHouses, 15, $"{theme}: evler bulunmalı");
    }

    [Test]
    public void EveryTheme_HouseDoor_IsRecordedOnTheStreetSide([ValueSource(nameof(Themes))] MapTheme theme)
    {
        int doors = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.For(theme));
            foreach (House house in map.Houses)
            {
                Assert.IsTrue(house.HasDoor, $"{theme} {seed}: üretilen evin kapısı kayıtlı olmalı");
                Assert.IsTrue(House.IsValidDoor(house.X, house.W, house.DoorX), $"{theme} {seed}: kapı köşeye değmemeli");
                Assert.IsFalse(house.DoorUp, $"{theme} {seed}: kapı hep güney (ön) duvarında");
                Assert.AreEqual(house.Y, house.DoorY);

                for (int i = 0; i < House.DoorWidth; i++)
                {
                    int x = house.DoorX + i;
                    Assert.AreEqual(theme.FloorTile, map.At(x, house.DoorY), $"{theme} {seed}: kapı karesi zemin olmalı");
                    Assert.IsTrue(house.IsDoorCell(x, house.DoorY));
                    Assert.IsTrue(house.IsDoorstep(x, house.DoorstepY));
                    Assert.IsFalse(house.Contains(x, house.DoorstepY), "kapının önü evin dışında");
                    Assert.IsTrue(TerrainMap.IsSafeGround(map.At(x, house.DoorstepY)), $"{theme} {seed}: kapının önü yürünür olmalı");

                    // İç taraf (kapının bir içerisi) evin zemini: kapıdan girilir.
                    int inside = house.DoorY + 1;
                    Assert.AreEqual(theme.FloorTile, map.At(x, inside));
                }

                // Kapının yanındaki duvar kareleri duvar kalır.
                Assert.IsTrue(TerrainMap.IsHouseWall(map.At(house.DoorX - 1, house.DoorY)));
                Assert.IsTrue(TerrainMap.IsHouseWall(map.At(house.DoorX + House.DoorWidth, house.DoorY)));
                doors++;
            }

            // Ağ metni kapıyı da taşır.
            var copy = new TerrainMap(W, H);
            Assert.IsTrue(copy.TryApplyHouses(map.EncodeHouses()));
            Assert.AreEqual(map.EncodeHouses(), copy.EncodeHouses());
            for (int i = 0; i < map.Houses.Count; i++)
            {
                Assert.AreEqual(map.Houses[i].DoorX, copy.Houses[i].DoorX);
                Assert.AreEqual(map.Houses[i].DoorUp, copy.Houses[i].DoorUp);
            }
        }
        Assert.Greater(doors, 15, $"{theme}: kapılı evler bulunmalı");
    }

    [Test]
    public void EveryTheme_HouseShelterThreat_PlacesZonesOnSafeGround([ValueSource(nameof(Themes))] MapTheme theme)
    {
        WordDatabase words = WordDatabase.LoadFromResources(config.maxWordLength);
        WordEntry horde = null;
        foreach (WordEntry w in words.Words) if (w.id == "zombi_suru") horde = w;
        Assert.IsNotNull(horde, "zombi_suru kelimesi yok");

        for (int seed = 1; seed <= 5; seed++)
        {
            TerrainMap map = MapGenerator.Generate(W, H, seed, MapRecipe.For(theme));
            var terrain = new TerrainSim(config, map, new System.Random(1));
            var writer = new PlayerState(1, "Yazan", 3, isBot: true);
            var other = new PlayerState(2, "Diğer", 3);
            var avatars = new FakeAvatars();
            avatars.Positions[1] = terrain.NearestSafe(map.CellCenter(3, 3));
            avatars.Positions[2] = terrain.NearestSafe(map.CellCenter(20, 10));
            var sim = new NetMatchSim(config, new MatchState(new[] { writer, other }), avatars, new System.Random(3), terrain);

            sim.OnWordPlayed(writer, horde);
            var zones = new List<ZoneSnapshot>();
            sim.SnapshotZones(zones);

            Assert.Greater(zones.Count, 0, $"{theme} {seed}: sığınak (ev) çıkmadı");
            foreach (ZoneSnapshot z in zones)
            {
                Assert.AreEqual("houses", z.Type);
                Assert.IsFalse(map.IsBlockedPoint(new Vector2(z.X, z.Y)), $"{theme} {seed}: ev alanı engelli karede");
            }
        }
    }

    [Test]
    public void EveryTheme_SpawnPointsAreSafeGround([ValueSource(nameof(Themes))] MapTheme theme)
    {
        TerrainMap map = MapGenerator.Generate(W, H, 11, MapRecipe.For(theme));
        var sim = new TerrainSim(config, map, new System.Random(1));
        for (int y = 0; y < H; y += 2)
            for (int x = 0; x < W; x += 2)
                Assert.IsTrue(TerrainMap.IsSafeGround(map.At(sim.NearestSafe(map.CellCenter(x, y)))), $"{theme}: ({x},{y})");
    }

    // ---- Ağ ----

    [Test]
    public void NewTiles_EncodeAndApply_RoundTrip()
    {
        var types = new[]
        {
            TileType.StoneWall, TileType.StoneFloor, TileType.Ice, TileType.FreezingWater,
            TileType.AdobeWall, TileType.AdobeFloor, TileType.Quicksand, TileType.Cactus,
        };
        var source = new TerrainMap(types.Length, 1);
        for (int i = 0; i < types.Length; i++) source.Set(i, 0, types[i]);

        var target = new TerrainMap(types.Length, 1);
        Assert.IsTrue(target.TryApply(source.Encode(null), null));
        for (int i = 0; i < types.Length; i++) Assert.AreEqual(types[i], target.At(i, 0));

        var flames = new bool[types.Length];
        Assert.IsTrue(target.TryApply(source.Encode((x, y) => x == 1), flames), "Yanan karo büyük harf olur");
        Assert.IsTrue(flames[1]);
    }

    [Test]
    public void Map_ThemeChange_RaisesEventOnce()
    {
        var map = new TerrainMap(4, 4);
        int raised = 0;
        map.ThemeChanged += () => raised++;

        map.Theme = MapTheme.Snow;
        map.Theme = MapTheme.Snow;
        map.Theme = null; // Köye döner

        Assert.AreEqual(2, raised);
        Assert.AreSame(MapTheme.Village, map.Theme);
    }
}
