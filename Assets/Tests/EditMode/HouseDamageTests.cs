using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Boss'ların ev yıkması: duvar yıkımı, evin çökmesi, sığınak evinin korunması, boss olmayanın yıkmaması ve ağ gidiş-dönüşü.</summary>
public class HouseDamageTests
{
    const int W = 20, H = 12;

    sealed class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        public IReadOnlyList<PlayerState> Players => players;
        public bool TryGetPosition(int playerId, out Vector2 position) { position = Vector2.zero; return false; }
    }

    GameConfig config;
    ArenaBounds arena;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(W, H);
        arena = new ArenaBounds(config.arenaSize);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    /// <summary>5×4 tahta ev: kenarlar duvar, içi zemin, alt duvarda 2 karelik kapı (x0+1, x0+2). 12 duvar karesi.</summary>
    static House BuildHouse(TerrainMap map, int x0, int y0)
    {
        for (int y = y0; y < y0 + 4; y++)
            for (int x = x0; x < x0 + 5; x++)
                map.Set(x, y, x == x0 || x == x0 + 4 || y == y0 || y == y0 + 3 ? TileType.WoodWall : TileType.WoodFloor);
        var house = new House(x0, y0, 5, 4, x0 + 1, false);
        map.Set(x0 + 1, y0, TileType.WoodFloor);
        map.Set(x0 + 2, y0, TileType.WoodFloor);
        map.AddHouse(house);
        return house;
    }

    CreatureModel Creature(Vector2 at, bool boss)
    {
        var word = new WordEntry
        {
            id = boss ? "dev" : "kurt", display = "x", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget,
            tags = boss ? new[] { "boss" } : new[] { "fast" }, stats = new WordStats { damage = 1, speed = 2f, duration = 40f, radius = 1.3f, hp = 10 },
        };
        var model = new CreatureModel(word, new PlayerState(1, "a", 3), at, 2f, 1, 1.3f, 40f, arena);
        TagRegistry.Apply(model, word.tags, new SpawnContext(0, 1, config));
        return model;
    }

    void Tick(TerrainSim sim, params CreatureModel[] creatures)
    {
        sim.Tick(0.016f, creatures, new List<PlayerState>(), id => null, (id, to) => { });
    }

    // ---- Duvar yıkımı ----

    [Test]
    public void TouchedWallCell_IsDemolished_HouseStays()
    {
        var map = new TerrainMap(W, H);
        BuildHouse(map, 5, 4);
        IReadOnlyList<DemolishedCell> reported = null;
        int reportedCollapsed = -1;
        map.WallsDemolished += (cells, collapsed) => { reported = cells; reportedCollapsed = collapsed; };

        var result = new HouseDamageResult();
        Assert.IsTrue(HouseDamage.Apply(map, new[] { new Vector2Int(9, 5), new Vector2Int(12, 5) }, 0.35f, null, result));

        Assert.AreEqual(TileType.WoodFloor, map.At(9, 5), "yıkılan duvar temanın zemini olur");
        Assert.AreEqual(TileType.Grass, map.At(12, 5), "ev dışındaki kare değişmez");
        Assert.AreEqual(1, result.Cells.Count);
        Assert.AreEqual(TileType.WoodWall, result.Cells[0].Wall);
        Assert.AreEqual(1, map.Houses.Count, "tek duvar karesi evi çökertmez");
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual(0, reportedCollapsed);
        Assert.IsTrue(RoofRules.IsBrokenRoofCell(map, map.Houses[0], 9, 5), "yıkılan karenin çatısı kalkar");
        Assert.IsFalse(RoofRules.IsUnderVisibleRoof(map, map.CellCenter(9, 5), null, null));
        Assert.IsTrue(RoofRules.IsUnderVisibleRoof(map, map.CellCenter(7, 6), null, null), "evin içi hâlâ çatılı");
    }

    [Test]
    public void Snow_And_Desert_WallsBecomeTheirOwnFloor()
    {
        var map = new TerrainMap(W, H) { Theme = MapTheme.Snow };
        map.Set(5, 4, TileType.StoneWall);
        map.AddHouse(new House(5, 4, 5, 4, 6, false));

        var result = new HouseDamageResult();
        HouseDamage.Apply(map, new[] { new Vector2Int(5, 4) }, 0.35f, null, result);
        Assert.AreEqual(MapTheme.Snow.FloorTile, map.At(5, 4));
        Assert.AreEqual(TileType.StoneWall, result.Cells[0].Wall);
    }

    [Test]
    public void WallWithoutHouse_AndCactus_AreNotDemolished()
    {
        var map = new TerrainMap(W, H);
        map.Set(2, 2, TileType.WoodWall);
        map.Set(3, 2, TileType.Cactus);

        var result = new HouseDamageResult();
        Assert.IsFalse(HouseDamage.Apply(map, new[] { new Vector2Int(2, 2), new Vector2Int(3, 2) }, 0.35f, null, result));
        Assert.AreEqual(TileType.WoodWall, map.At(2, 2));
        Assert.AreEqual(TileType.Cactus, map.At(3, 2));
    }

    [Test]
    public void BreakingMoreThanTheRatio_CollapsesTheWholeHouse()
    {
        var map = new TerrainMap(W, H);
        House house = BuildHouse(map, 5, 4);
        BuildHouse(map, 12, 4);
        int housesChanged = 0;
        map.HousesChanged += () => housesChanged++;
        Assert.AreEqual(12, HouseDamage.WallCellCount(house));

        var result = new HouseDamageResult();
        var four = new[] { new Vector2Int(9, 4), new Vector2Int(9, 5), new Vector2Int(9, 6), new Vector2Int(9, 7) };
        HouseDamage.Apply(map, four, 0.35f, null, result);
        Assert.AreEqual(2, map.Houses.Count, "4/12 = %33: oran (%35) aşılmadı");
        Assert.AreEqual(0, result.Collapsed.Count);

        HouseDamage.Apply(map, new[] { new Vector2Int(8, 7) }, 0.35f, null, result);
        Assert.AreEqual(1, result.Collapsed.Count, "5/12 > %35: ev çöker");
        Assert.AreEqual(1, map.Houses.Count, "çöken ev listeden çıkar");
        Assert.AreEqual(12, map.Houses[0].X, "diğer ev yerinde");
        Assert.AreEqual(1, housesChanged);
        for (int y = 4; y < 8; y++)
            for (int x = 5; x < 10; x++)
                Assert.IsFalse(TerrainMap.IsHouseWall(map.At(x, y)), $"{x},{y}: çöken evin bütün duvarları kalkar");
        Assert.AreEqual(8, result.Cells.Count, "değilen kare + kalan 7 duvar");
        Assert.IsTrue(TerrainMap.IsHouseWall(map.At(12, 4)), "komşu ev sağlam");
        Assert.IsFalse(RoofRules.IsUnderVisibleRoof(map, map.CellCenter(7, 6), null, null), "çöken evdeki oyuncu açığa çıkar");
    }

    [Test]
    public void CellsUnderBody_FindsCellsTheCircleTouches()
    {
        var map = new TerrainMap(W, H);
        var cells = new List<Vector2Int>();
        HouseDamage.CellsUnderBody(map, map.CellCenter(10, 6), 0.4f, cells);
        Assert.AreEqual(1, cells.Count);
        Assert.AreEqual(new Vector2Int(10, 6), cells[0]);

        HouseDamage.CellsUnderBody(map, map.CellCenter(10, 6), 1.55f, cells);
        Assert.Contains(new Vector2Int(9, 6), cells);
        Assert.Contains(new Vector2Int(11, 7), cells);
        Assert.IsFalse(cells.Contains(new Vector2Int(8, 4)), "uzaktaki köşe karesi değmez");
    }

    // ---- Arazi simülasyonu üzerinden: boss yıkar, diğerleri yıkmaz ----

    [Test]
    public void Boss_WalkingIntoAHouse_BreaksWalls_ThenCollapsesIt()
    {
        var map = new TerrainMap(W, H);
        BuildHouse(map, 5, 4);
        var sim = new TerrainSim(config, map, new System.Random(1));
        CreatureModel boss = Creature(map.CellCenter(10, 6), boss: true);
        Assert.IsTrue(HouseDamage.CanBreakHouses(boss), "boss etiketi ev yıkmayı açar");

        sim.Dirty = false;
        Tick(sim, boss);
        Assert.AreEqual(TileType.WoodFloor, map.At(9, 5));
        Assert.AreEqual(TileType.WoodFloor, map.At(9, 6));
        Assert.AreEqual(TileType.WoodFloor, map.At(9, 7));
        Assert.IsTrue(TerrainMap.IsHouseWall(map.At(9, 4)), "gövdenin değmediği duvar kalır");
        Assert.AreEqual(1, map.Houses.Count);
        Assert.IsTrue(sim.Dirty, "ağ yeni durumu yaysın");

        boss.PlaceAt(map.CellCenter(7, 8));
        Tick(sim, boss);
        Assert.AreEqual(0, map.Houses.Count, "6/12 duvar yıkıldı: ev çöktü");
    }

    [Test]
    public void NonBossCreature_DoesNotBreakHouses()
    {
        var map = new TerrainMap(W, H);
        BuildHouse(map, 5, 4);
        var sim = new TerrainSim(config, map, new System.Random(1));
        CreatureModel wolf = Creature(map.CellCenter(10, 6), boss: false);
        Assert.IsFalse(HouseDamage.CanBreakHouses(wolf));

        Tick(sim, wolf);
        Assert.IsTrue(TerrainMap.IsHouseWall(map.At(9, 6)));
        Assert.AreEqual(1, map.Houses.Count);

        CreatureModel flyingBoss = Creature(map.CellCenter(10, 6), boss: true);
        flyingBoss.IsFlying = true;
        Tick(sim, flyingBoss);
        Assert.IsTrue(TerrainMap.IsHouseWall(map.At(9, 6)), "uçan boss duvara değmez");
    }

    [Test]
    public void RefugeHouse_IsNotDemolished_WhileTheThreatLasts()
    {
        var map = new TerrainMap(W, H);
        House house = BuildHouse(map, 5, 4);
        var sim = new TerrainSim(config, map, new System.Random(1));

        WordEntry rats = WordDatabase.LoadFromResources(50).Lookup("fare_istilasi").Entry;
        Assert.AreEqual(SafeZone.HousesType, rats.counter.type, "fare istilası evlere sığınılan bir tehdit");
        var threat = new GlobalThreat(rats, null, config, arena, new FakeWorld(), new System.Random(3));
        threat.Zones.Add(new SafeZone(SafeZone.HousesType, map.CellCenter(7, 5), 1.6f, 2, float.MaxValue)); // Bu evin sığınağı
        var threats = new List<GlobalThreat> { threat };
        sim.IsHouseProtected = h => RefugeFinder.IsHouseRefuge(threats, map, h);

        Assert.IsTrue(RefugeFinder.IsHouseRefuge(threats, map, house));
        Assert.IsFalse(RefugeFinder.IsHouseRefuge(threats, map, new House(15, 1, 3, 3)), "uzaktaki ev sığınak değil");

        CreatureModel boss = Creature(map.CellCenter(10, 6), boss: true);
        Tick(sim, boss);
        Assert.IsTrue(TerrainMap.IsHouseWall(map.At(9, 6)), "tehdit sürerken sığınak evi yıkılmaz");
        Assert.AreEqual(1, map.Houses.Count);

        for (int i = 0; i < 200 && threat.Phase != ThreatPhase.Finished; i++) threat.Tick(1f);
        Assert.AreEqual(ThreatPhase.Finished, threat.Phase);
        Assert.IsFalse(RefugeFinder.IsHouseRefuge(threats, map, house));

        Tick(sim, boss);
        Assert.AreEqual(TileType.WoodFloor, map.At(9, 6), "tehdit bitince ev artık yıkılabilir");
    }

    [Test]
    public void NetMatchSim_WiresRefugeProtection()
    {
        var map = new TerrainMap(W, H);
        var terrain = new TerrainSim(config, map, new System.Random(1));
        var match = new MatchState(new List<PlayerState> { new PlayerState(1, "a", 3), new PlayerState(2, "b", 3) });
        var sim = new NetMatchSim(config, match, new NullAvatars(), new System.Random(1), terrain);
        Assert.IsNotNull(sim.Terrain.IsHouseProtected);
    }

    sealed class NullAvatars : INetAvatars
    {
        public bool TryGetPosition(int id, out Vector2 p) { p = Vector2.zero; return false; }
        public bool TryApplyKnockback(int id, Vector2 d) => false;
        public bool TryGetFacing(int id, out Vector2 d) { d = Vector2.up; return false; }
        public bool TryTeleport(int id, Vector2 to) => false;
    }

    // ---- Ağ: tam durum gidiş-dönüş ----

    [Test]
    public void Network_RoundTrip_ClientSeesSameCellsAndCollapsedHouses()
    {
        var host = new TerrainMap(W, H);
        BuildHouse(host, 2, 4);
        BuildHouse(host, 12, 4);

        var client = new TerrainMap(W, H);
        Assert.IsTrue(client.TryApply(host.Encode(null), null));
        Assert.IsTrue(client.TryApplyHouses(host.EncodeHouses()));

        // Host: birinci ev çöker, ikincinin bir duvarı yıkılır.
        var result = new HouseDamageResult();
        var all = new List<DemolishedCell>();
        HouseDamage.Apply(host, new[] { new Vector2Int(2, 5), new Vector2Int(2, 6), new Vector2Int(3, 7), new Vector2Int(4, 7), new Vector2Int(5, 7) },
                          0.35f, null, result);
        all.AddRange(result.Cells);
        HouseDamage.Apply(host, new[] { new Vector2Int(16, 5) }, 0.35f, null, result);
        all.AddRange(result.Cells);
        Assert.AreEqual(1, host.Houses.Count);

        // İstemci: önceki durumu saklar, host'un metnini uygular, farkı bulur.
        var wallsBefore = new List<DemolishedCell>();
        HouseDamage.SnapshotHouseWalls(client, wallsBefore);
        var housesBefore = new List<House>(client.Houses);
        Assert.IsTrue(client.TryApply(host.Encode(null), null));
        Assert.IsTrue(client.TryApplyHouses(host.EncodeHouses()));

        var found = new List<DemolishedCell>();
        Assert.IsTrue(HouseDamage.FindDemolished(client, wallsBefore, housesBefore, found, out int collapsed));
        Assert.AreEqual(1, collapsed);
        Assert.AreEqual(Keys(all), Keys(found), "istemci host'un yıktığı karelerin aynısını bulur");
        Assert.AreEqual(host.Encode(null), client.Encode(null));
        Assert.AreEqual(host.EncodeHouses(), client.EncodeHouses());
        Assert.IsTrue(RoofRules.IsBrokenRoofCell(client, client.Houses[0], 16, 5));

        // Geç bağlanan istemci: boş haritaya tam durum gelir; aynı harita, ama yıkım efekti yok.
        var late = new TerrainMap(W, H);
        HouseDamage.SnapshotHouseWalls(late, wallsBefore);
        housesBefore = new List<House>(late.Houses);
        Assert.IsTrue(late.TryApply(host.Encode(null), null));
        Assert.IsTrue(late.TryApplyHouses(host.EncodeHouses()));
        Assert.IsFalse(HouseDamage.FindDemolished(late, wallsBefore, housesBefore, found, out _));
        Assert.AreEqual(host.Encode(null), late.Encode(null));
        Assert.AreEqual(host.EncodeHouses(), late.EncodeHouses());
    }

    [Test]
    public void Network_NewMatchMap_AndBurnedWalls_AreNotCountedAsDemolition()
    {
        var client = new TerrainMap(W, H);
        BuildHouse(client, 2, 4);
        var wallsBefore = new List<DemolishedCell>();
        HouseDamage.SnapshotHouseWalls(client, wallsBefore);
        var housesBefore = new List<House>(client.Houses);
        var found = new List<DemolishedCell>();

        // Yangın: duvar kömürleşti (yanıp çöktü) → yıkım değil.
        client.Set(2, 5, TileType.CharredFloor);
        Assert.IsFalse(HouseDamage.FindDemolished(client, wallsBefore, housesBefore, found, out _));

        // Yeni maçın haritası: evler başka yerde → eski duvarların kalkması yıkım sayılmaz.
        var next = new TerrainMap(W, H);
        BuildHouse(next, 12, 4);
        Assert.IsTrue(client.TryApply(next.Encode(null), null));
        Assert.IsTrue(client.TryApplyHouses(next.EncodeHouses()));
        Assert.IsFalse(HouseDamage.FindDemolished(client, wallsBefore, housesBefore, found, out _));
    }

    static string Keys(List<DemolishedCell> cells)
    {
        var keys = new List<string>();
        foreach (DemolishedCell c in cells) keys.Add($"{c.X},{c.Y},{c.Wall}");
        keys.Sort(System.StringComparer.Ordinal);
        return string.Join(";", keys);
    }
}
