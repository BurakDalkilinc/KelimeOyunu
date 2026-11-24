using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Arazi testleri: harita, üretici, yangın, engeller, tehlikeler ve yaratıkların tutuşturması.</summary>
public class TerrainTests
{
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(34f, 19f);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static TerrainMap Plain(int w = 10, int h = 8) => new TerrainMap(w, h);

    // ---- TerrainMap ----

    [Test]
    public void Map_CellsAndWorldPositions_AreConsistent()
    {
        var map = new TerrainMap(34, 19);

        Assert.AreEqual(new Vector2(-17f, -9.5f), map.Origin);
        Assert.AreEqual(new Vector2(-16.5f, -9f), map.CellCenter(0, 0));
        Assert.AreEqual(new Vector2Int(17, 9), map.CellOf(new Vector2(0.2f, 0.1f)));
        Assert.AreEqual(new Vector2Int(0, 0), map.CellOf(new Vector2(-99f, -99f))); // Dışarısı kenara sıkışır
        Assert.AreEqual(new Vector2Int(33, 18), map.CellOf(new Vector2(99f, 99f)));
    }

    [Test]
    public void Map_TileTraits()
    {
        Assert.IsTrue(TerrainMap.IsSolid(TileType.WoodWall));
        Assert.IsFalse(TerrainMap.IsSolid(TileType.WoodFloor));
        Assert.IsTrue(TerrainMap.IsHazard(TileType.Lava));
        Assert.IsTrue(TerrainMap.IsHazard(TileType.Chasm));
        Assert.IsTrue(TerrainMap.IsFlammable(TileType.WoodFloor));
        Assert.IsTrue(TerrainMap.IsFlammable(TileType.WoodWall));
        Assert.IsFalse(TerrainMap.IsFlammable(TileType.CharredFloor));
        Assert.IsFalse(TerrainMap.IsFlammable(TileType.Grass));
        Assert.IsTrue(TerrainMap.IsWalkable(TileType.CharredFloor));
        Assert.IsFalse(TerrainMap.IsWalkable(TileType.Lava));
        Assert.IsFalse(TerrainMap.IsWalkable(TileType.WoodWall));
    }

    [Test]
    public void Map_SetRaisesEvent_OnlyOnRealChange()
    {
        TerrainMap map = Plain();
        int events = 0;
        map.TileChanged += (_, __) => events++;

        map.Set(2, 3, TileType.Lava);
        map.Set(2, 3, TileType.Lava);
        map.Set(-1, 0, TileType.Lava); // Harita dışı yok sayılır

        Assert.AreEqual(1, events);
        Assert.AreEqual(TileType.Lava, map.At(2, 3));
        Assert.AreEqual(TileType.Grass, map.At(-1, 0));
    }

    [Test]
    public void Map_FindNearestWalkable_SkipsHazardsAndWalls()
    {
        TerrainMap map = Plain();
        for (int x = 2; x <= 6; x++) for (int y = 2; y <= 5; y++) map.Set(x, y, TileType.Lava);

        Assert.IsTrue(map.TryFindNearestWalkable(map.CellCenter(4, 3), out Vector2 safe));

        Assert.IsTrue(TerrainMap.IsWalkable(map.At(safe)));
        Assert.LessOrEqual((safe - map.CellCenter(4, 3)).magnitude, 3f);
    }

    [Test]
    public void Map_FindNearestWalkable_CanAvoidBurningTiles()
    {
        TerrainMap map = Plain();

        Assert.IsTrue(map.TryFindNearestWalkable(map.CellCenter(4, 4), out Vector2 safe, (x, y) => x == 4 && y == 4));

        Assert.AreNotEqual(map.CellCenter(4, 4), safe);
    }

    [Test]
    public void Map_FindNearestWalkable_WhenNothingIsWalkable_ReturnsFalse()
    {
        var map = new TerrainMap(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) map.Set(x, y, TileType.Lava);

        Assert.IsFalse(map.TryFindNearestWalkable(Vector2.zero, out _));
    }

    [Test]
    public void Map_EncodeAndApply_RoundTrips_WithFire()
    {
        TerrainMap source = Plain(6, 4);
        source.Set(1, 1, TileType.WoodWall);
        source.Set(2, 1, TileType.WoodFloor);
        source.Set(3, 2, TileType.Lava);
        source.Set(4, 3, TileType.CharredFloor);
        string text = source.Encode((x, y) => x == 2 && y == 1);

        var target = Plain(6, 4);
        var burning = new bool[24];
        Assert.IsTrue(target.TryApply(text, burning));

        Assert.AreEqual(TileType.WoodWall, target.At(1, 1));
        Assert.AreEqual(TileType.Lava, target.At(3, 2));
        Assert.AreEqual(TileType.CharredFloor, target.At(4, 3));
        Assert.IsTrue(burning[target.Index(2, 1)]);
        Assert.IsFalse(burning[target.Index(1, 1)]);
    }

    [TestCase("")]
    [TestCase("ggg")]
    [TestCase("gggggggggggggggggggggggx")]
    public void Map_Apply_RejectsBrokenText_WithoutChangingAnything(string text)
    {
        TerrainMap map = Plain(6, 4);
        map.Set(0, 0, TileType.Lava);

        Assert.IsFalse(map.TryApply(text, null));
        Assert.AreEqual(TileType.Lava, map.At(0, 0));
    }

    [Test]
    public void Map_Apply_NullText_IsRejected()
    {
        Assert.IsFalse(Plain().TryApply(null, null));
    }

    // ---- MapGenerator ----

    [Test]
    public void Generator_SameSeed_GivesTheSameMap_DifferentSeedsDiffer()
    {
        string a = MapGenerator.Generate(34, 19, 42, MapRecipe.Default).Encode(null);
        string b = MapGenerator.Generate(34, 19, 42, MapRecipe.Default).Encode(null);
        string c = MapGenerator.Generate(34, 19, 43, MapRecipe.Default).Encode(null);

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }

    [Test]
    public void Generator_HasVillagesLavaAndChasms_AndEveryHouseHasADoor()
    {
        TerrainMap map = MapGenerator.Generate(34, 19, 7, MapRecipe.Default);

        Assert.Greater(map.Count(TileType.WoodWall), 10, "Ev duvarları olmalı");
        Assert.Greater(map.Count(TileType.WoodFloor), 10, "Tahta zemin (meydan, yol, ev içi) olmalı");
        Assert.Greater(map.Count(TileType.Lava), 3);
        Assert.Greater(map.Count(TileType.Chasm), 3);
        Assert.Greater(map.Count(TileType.Grass), 100);
    }

    [Test]
    public void Generator_MapsStayConnected_AcrossManySeeds()
    {
        for (int seed = 1; seed <= 40; seed++)
        {
            TerrainMap map = MapGenerator.Generate(34, 19, seed, MapRecipe.Default);
            Assert.GreaterOrEqual(map.LargestWalkableRegionFraction(), 0.93f, $"{seed} tohumlu harita bölünmüş");
        }
    }

    [Test]
    public void Generator_KeepsTheOuterEdgeFree_ForSpawning()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(34, 19, seed, MapRecipe.Default);
            for (int x = 0; x < 34; x++)
            {
                for (int d = 0; d < 2; d++)
                {
                    Assert.IsTrue(TerrainMap.IsWalkable(map.At(x, d)), $"{seed}: alt kenarda engel ({x},{d})");
                    Assert.IsTrue(TerrainMap.IsWalkable(map.At(x, 18 - d)), $"{seed}: üst kenarda engel ({x},{18 - d})");
                }
            }
            for (int y = 0; y < 19; y++)
            {
                for (int d = 0; d < 2; d++)
                {
                    Assert.IsTrue(TerrainMap.IsWalkable(map.At(d, y)), $"{seed}: sol kenarda engel ({d},{y})");
                    Assert.IsTrue(TerrainMap.IsWalkable(map.At(33 - d, y)), $"{seed}: sağ kenarda engel ({33 - d},{y})");
                }
            }
        }
    }

    [Test]
    public void Generator_TinyMap_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => MapGenerator.Generate(8, 6, 1, MapRecipe.Default));
        Assert.DoesNotThrow(() => MapGenerator.Generate(34, 19, 1, new MapRecipe { Villages = 0, LavaPools = 0, Chasms = 0 }));
    }

    // ---- FireSim ----

    FireSim NewFire(TerrainMap map, float spread = 0f, float lava = 0f, int seed = 1)
    {
        config.fireSpreadChancePerSecond = spread;
        config.lavaIgniteChancePerSecond = lava;
        config.fireBurnSeconds = 5f;
        config.groundFireSeconds = 2.5f;
        return new FireSim(map, config, new System.Random(seed));
    }

    static void FillWood(TerrainMap map, int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++) map.Set(x, y, TileType.WoodFloor);
    }

    [Test]
    public void Fire_WoodBurnsThenTurnsToCharcoal()
    {
        TerrainMap map = Plain();
        map.Set(3, 3, TileType.WoodFloor);
        FireSim fire = NewFire(map);

        Assert.IsTrue(fire.Ignite(3, 3));
        Assert.IsTrue(fire.IsBurning(3, 3));

        fire.Tick(4.9f);
        Assert.IsTrue(fire.IsBurning(3, 3));
        Assert.AreEqual(TileType.WoodFloor, map.At(3, 3));

        fire.Tick(0.3f);
        Assert.IsFalse(fire.IsBurning(3, 3));
        Assert.AreEqual(TileType.CharredFloor, map.At(3, 3));
        Assert.IsFalse(fire.Ignite(3, 3) && fire.BurningCount > 1);
    }

    [Test]
    public void Fire_BurnedWall_CollapsesIntoWalkableCharcoal()
    {
        TerrainMap map = Plain();
        map.Set(3, 3, TileType.WoodWall);
        FireSim fire = NewFire(map);
        fire.Ignite(3, 3);

        fire.Tick(6f);

        Assert.AreEqual(TileType.CharredFloor, map.At(3, 3));
        Assert.IsTrue(TerrainMap.IsWalkable(map.At(3, 3)));
    }

    [Test]
    public void Fire_SpreadsAlongWood_ButNotAcrossGrass()
    {
        TerrainMap map = Plain(12, 3);
        FillWood(map, 1, 5, 1);   // Bir tahta şerit
        map.Set(7, 1, TileType.WoodFloor); // Çimenle ayrılmış ayrı bir tahta
        FireSim fire = NewFire(map, spread: 100f);

        fire.Ignite(1, 1);
        fire.Tick(0.5f);
        fire.Tick(0.5f);
        fire.Tick(0.5f);

        Assert.IsTrue(fire.IsBurning(2, 1) || map.At(2, 1) == TileType.CharredFloor);
        Assert.IsTrue(fire.IsBurning(5, 1) || map.At(5, 1) == TileType.CharredFloor || fire.IsBurning(4, 1));
        Assert.IsFalse(fire.IsBurning(7, 1), "Çimeni geçemez");
        Assert.AreEqual(TileType.Grass, map.At(6, 1));
    }

    [Test]
    public void Fire_OnGrass_IsShort_AndDoesNotSpread()
    {
        TerrainMap map = Plain();
        FillWood(map, 4, 4, 3); // Yanıbaşında tahta var
        FireSim fire = NewFire(map, spread: 100f);

        Assert.IsTrue(fire.Ignite(3, 3)); // Çimende alev
        fire.Tick(1f);

        Assert.IsFalse(fire.IsBurning(4, 3), "Çimendeki alev tahtaya geçmez");
        fire.Tick(2f);
        Assert.IsFalse(fire.IsBurning(3, 3));
        Assert.AreEqual(TileType.Grass, map.At(3, 3));
    }

    [Test]
    public void Fire_DoesNotBurnLavaOrChasm()
    {
        TerrainMap map = Plain();
        map.Set(1, 1, TileType.Lava);
        map.Set(2, 1, TileType.Chasm);
        FireSim fire = NewFire(map);

        Assert.IsFalse(fire.Ignite(1, 1));
        Assert.IsFalse(fire.Ignite(2, 1));
        Assert.IsFalse(fire.Ignite(-3, 0));
        Assert.AreEqual(0, fire.BurningCount);
    }

    [Test]
    public void Fire_LavaIgnitesAdjacentWood()
    {
        TerrainMap map = Plain();
        map.Set(3, 3, TileType.Lava);
        map.Set(4, 3, TileType.WoodFloor);
        map.Set(6, 6, TileType.WoodFloor); // Lavdan uzak tahta
        FireSim fire = NewFire(map, lava: 100f);

        fire.Tick(1f);

        Assert.IsTrue(fire.IsBurning(4, 3));
        Assert.IsFalse(fire.IsBurning(6, 6));
    }

    [Test]
    public void Fire_IgniteAreaAndExtinguishArea()
    {
        TerrainMap map = Plain(12, 12);
        for (int x = 3; x <= 8; x++) for (int y = 3; y <= 8; y++) map.Set(x, y, TileType.WoodFloor);
        FireSim fire = NewFire(map);

        int ignited = fire.IgniteArea(map.CellCenter(5, 5), 1.2f);
        Assert.Greater(ignited, 3);
        Assert.AreEqual(ignited, fire.BurningCount);

        int out2 = fire.ExtinguishArea(map.CellCenter(5, 5), 5f);
        Assert.AreEqual(ignited, out2);
        Assert.AreEqual(0, fire.BurningCount);
        Assert.AreEqual(TileType.WoodFloor, map.At(5, 5)); // Söndürülen tahta kömürleşmez
    }

    [Test]
    public void Fire_RaisesBurnChanged_ForIgniteAndBurnOut()
    {
        TerrainMap map = Plain();
        map.Set(3, 3, TileType.WoodFloor);
        FireSim fire = NewFire(map);
        int changes = 0;
        fire.BurnChanged += (_, __) => changes++;

        fire.Ignite(3, 3);
        fire.Tick(6f);

        Assert.AreEqual(2, changes); // Tutuştu, söndü
    }

    [Test]
    public void Fire_SameSeed_BehavesTheSame()
    {
        string Run(int seed)
        {
            TerrainMap map = Plain(14, 3);
            FillWood(map, 1, 12, 1);
            FireSim fire = NewFire(map, spread: 1.2f, seed: seed);
            fire.Ignite(1, 1);
            for (int i = 0; i < 60; i++) fire.Tick(0.1f);
            return map.Encode(fire.IsBurning);
        }

        Assert.AreEqual(Run(5), Run(5));
    }

    [Test]
    public void Fire_ApplyBurning_FromHostState()
    {
        TerrainMap map = Plain(4, 4);
        map.Set(1, 1, TileType.WoodFloor);
        FireSim fire = NewFire(map);
        var state = new bool[16];
        state[map.Index(1, 1)] = true;

        fire.ApplyBurning(state);
        Assert.IsTrue(fire.IsBurning(1, 1));

        state[map.Index(1, 1)] = false;
        fire.ApplyBurning(state);
        Assert.IsFalse(fire.IsBurning(1, 1));
        Assert.DoesNotThrow(() => fire.ApplyBurning(new bool[3])); // Yanlış boyut yok sayılır
    }

    // ---- TerrainObstacles ----

    [Test]
    public void Obstacles_WoodWall_PushesACharacterOut()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.WoodWall);
        var obstacles = new TerrainObstacles(map, null, blockHazards: false);

        Vector2 inside = map.CellCenter(4, 4) + new Vector2(0.45f, 0f); // Duvarın içinde, sağ kenara yakın
        Vector2 pushed = obstacles.Resolve(inside, 0.5f);

        Assert.GreaterOrEqual(pushed.x - map.CellCenter(4, 4).x, 1f - 0.0001f); // Sağ kenarın dışına, yarıçap kadar açıkta
        Assert.AreEqual(inside.y, pushed.y, 0.001f);
    }

    [Test]
    public void Obstacles_OpenGround_LeavesThePositionAlone()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.WoodWall);
        var obstacles = new TerrainObstacles(map, null, false);
        Vector2 free = map.CellCenter(1, 1);

        Assert.AreEqual(free, obstacles.Resolve(free, 0.5f));
    }

    [Test]
    public void Obstacles_NormalMode_LetsYouWalkIntoLava_ButAvoidingModeBlocksIt()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.Lava);
        Vector2 inLava = map.CellCenter(4, 4);

        Assert.AreEqual(inLava, new TerrainObstacles(map, null, false).Resolve(inLava, 0.5f));
        Assert.AreNotEqual(inLava, new TerrainObstacles(map, null, true).Resolve(inLava, 0.5f));
    }

    [Test]
    public void Obstacles_AvoidingMode_AlsoBlocksBurningTiles()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.WoodFloor);
        FireSim fire = NewFire(map);
        var avoiding = new TerrainObstacles(map, fire, true);
        Vector2 spot = map.CellCenter(4, 4);

        Assert.AreEqual(spot, avoiding.Resolve(spot, 0.5f)); // Yanmıyorken serbest
        fire.Ignite(4, 4);
        Assert.AreNotEqual(spot, avoiding.Resolve(spot, 0.5f));
    }

    [Test]
    public void Obstacles_CharacterCanNotWalkThroughAWoodWall_UsingTheRealMover()
    {
        TerrainMap map = Plain(20, 8);
        for (int y = 0; y < 8; y++) map.Set(10, y, TileType.WoodWall); // Tam bir duvar sırası
        var arena = new ArenaBounds(new Vector2(20f, 8f));
        var obstacles = new TerrainObstacles(map, null, false);

        Vector2 position = map.CellCenter(5, 4);
        for (int i = 0; i < 100; i++)
            position = ObstacleMover.Move(position, position + new Vector2(0.4f, 0f), 0.5f, arena, 0.5f, obstacles);

        Assert.Less(position.x, map.CellCenter(10, 4).x - 0.5f); // Duvarın solunda kaldı
    }

    [Test]
    public void Composite_AppliesEveryField()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.WoodWall);
        var walls = new WallField();
        var composite = new CompositeObstacles(walls, new TerrainObstacles(map, null, false), null);

        Vector2 inside = map.CellCenter(4, 4);

        Assert.AreNotEqual(inside, composite.Resolve(inside, 0.5f));
    }

    // ---- TerrainHazards ----

    sealed class Placer
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public readonly List<(int id, Vector2 to)> Teleports = new List<(int, Vector2)>();

        public Vector2? Position(int id) => Positions.TryGetValue(id, out Vector2 p) ? p : (Vector2?)null;
        public void Teleport(int id, Vector2 to) { Teleports.Add((id, to)); Positions[id] = to; }
    }

    TerrainHazards NewHazards(TerrainMap map, FireSim fire) => new TerrainHazards(map, fire, config);

    [Test]
    public void FlyingAndGhosts_AreImmuneToLavaAndChasm()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.Lava);
        map.Set(5, 4, TileType.Chasm);
        var flyer = new PlayerState(1, "Uçan", 3);
        flyer.Status.ApplyFlight(10f, 1.2f);
        var ghost = new PlayerState(2, "Hayalet", 3);
        ghost.Health.TakeDamage(99);
        var placer = new Placer();
        placer.Positions[1] = map.CellCenter(4, 4);
        placer.Positions[2] = map.CellCenter(5, 4);

        NewHazards(map, null).Tick(0.5f, new[] { flyer, ghost }, placer.Position, placer.Teleport);

        Assert.AreEqual(3, flyer.Health.Current);
        Assert.AreEqual(0, placer.Teleports.Count);
    }

    [Test]
    public void FireTile_HurtsEverySecond_AndWoodBurnsOutUnderYourFeet()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.WoodFloor);
        FireSim fire = NewFire(map);
        fire.Ignite(4, 4);
        TerrainHazards hazards = NewHazards(map, fire);
        var player = new PlayerState(1, "A", 10);
        var placer = new Placer();
        placer.Positions[1] = map.CellCenter(4, 4);

        hazards.Tick(0.05f, new[] { player }, placer.Position, placer.Teleport);

        Assert.AreEqual(9, player.Health.Current);
    }

    // ---- TerrainSim ----

    [Test]
    public void Sim_NearestSafe_MovesOutOfWallsAndLava_AndKeepsSafePointsAsIs()
    {
        TerrainMap map = Plain();
        map.Set(4, 4, TileType.Lava);
        var sim = new TerrainSim(config, map, new System.Random(1));

        Vector2 safe = map.CellCenter(1, 1);
        Assert.AreEqual(safe, sim.NearestSafe(safe));
        Assert.AreNotEqual(map.CellCenter(4, 4), sim.NearestSafe(map.CellCenter(4, 4)));
        Assert.IsTrue(sim.IsBlocked(map.CellCenter(4, 4)));
        Assert.IsFalse(sim.IsBlocked(safe));
    }

    [Test]
    public void Sim_DirtyFlag_RaisedByFireAndTileChanges()
    {
        TerrainMap map = Plain();
        map.Set(3, 3, TileType.WoodFloor);
        var sim = new TerrainSim(config, map, new System.Random(1));

        Assert.IsFalse(sim.Dirty);
        sim.Fire.Ignite(3, 3);
        Assert.IsTrue(sim.Dirty);
    }

    sealed class FakeWorld : IPlayerLocator
    {
        public IReadOnlyList<PlayerState> Players { get; set; } = new List<PlayerState>();
        public bool TryGetPosition(int id, out Vector2 p) { p = Vector2.zero; return false; }
    }

    WordEntry Real(string id)
    {
        WordDatabase words = WordDatabase.LoadFromResources(config.maxWordLength);
        foreach (WordEntry w in words.Words) if (w.id == id) return w;
        Assert.Fail("Kelime yok: " + id);
        return null;
    }

    [Test]
    public void Sim_FireWord_IgnitesTheTargetArea_OnceWhenItStrikes()
    {
        config.fireSpreadChancePerSecond = 0f;
        TerrainMap map = Plain(20, 12);
        for (int x = 5; x <= 12; x++) for (int y = 3; y <= 8; y++) map.Set(x, y, TileType.WoodFloor);
        var sim = new TerrainSim(config, map, new System.Random(1));
        var target = new PlayerState(2, "Hedef", 3);
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        CreatureModel fireStrike = CreatureFactory.CreateAttack(Real("ates"), target, map.CellCenter(8, 5), config, arena);
        fireStrike.PlaceAt(map.CellCenter(8, 5));
        var creatures = new List<CreatureModel> { fireStrike };

        Assert.IsTrue(fireStrike.IgnitesTerrain);
        Assert.IsTrue(fireStrike.IsStrike);

        sim.Tick(0.1f, creatures, new List<PlayerState>(), id => null, (id, p) => { });
        Assert.AreEqual(0, sim.Fire.BurningCount, "Uyarı bitmeden tutuşmaz");

        var hits = new List<CreatureHit>();
        var world = new FakeWorld { Players = new[] { target } };
        for (int i = 0; i < 40 && !fireStrike.HasStruck; i++) fireStrike.TickStrike(0.1f, world, config.playerRadius, hits);
        Assert.IsTrue(fireStrike.HasStruck);

        sim.Tick(0.01f, creatures, new List<PlayerState>(), id => null, (id, p) => { });
        int burningAfterStrike = sim.Fire.BurningCount;
        Assert.Greater(burningAfterStrike, 3);

        sim.Tick(0.01f, creatures, new List<PlayerState>(), id => null, (id, p) => { });
        Assert.LessOrEqual(sim.Fire.BurningCount, burningAfterStrike, "Aynı vuruş ikinci kez tutuşturmaz");
    }

    [Test]
    public void Sim_Dragon_KeepsIgnitingTheWoodUnderIt()
    {
        config.fireSpreadChancePerSecond = 0f;
        config.creatureIgniteChancePerSecond = 50f;
        TerrainMap map = Plain(20, 12);
        for (int x = 5; x <= 12; x++) for (int y = 3; y <= 8; y++) map.Set(x, y, TileType.WoodFloor);
        var sim = new TerrainSim(config, map, new System.Random(1));
        var target = new PlayerState(2, "Hedef", 3);
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        CreatureModel dragon = CreatureFactory.CreateAttack(Real("ejderha"), target, map.CellCenter(8, 5), config, arena);
        dragon.PlaceAt(map.CellCenter(8, 5));

        Assert.IsTrue(dragon.BurnsArea);
        Assert.IsTrue(dragon.IgnitesTerrain);

        sim.Tick(0.5f, new List<CreatureModel> { dragon }, new List<PlayerState>(), id => null, (id, p) => { });

        Assert.Greater(sim.Fire.BurningCount, 3);
    }

    [Test]
    public void Sim_NonIgnitingCreatures_LeaveTheMapAlone()
    {
        TerrainMap map = Plain(20, 12);
        for (int x = 5; x <= 12; x++) map.Set(x, 5, TileType.WoodFloor);
        var sim = new TerrainSim(config, map, new System.Random(1));
        var target = new PlayerState(2, "Hedef", 3);
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        CreatureModel wolf = CreatureFactory.CreateAttack(Real("kurt"), target, map.CellCenter(8, 5), config, arena);

        sim.Tick(1f, new List<CreatureModel> { wolf }, new List<PlayerState>(), id => null, (id, p) => { });

        Assert.AreEqual(0, sim.Fire.BurningCount);
        Assert.IsFalse(wolf.IgnitesTerrain);
    }

    [Test]
    public void Sim_GeneratedFromSeed_HasTheArenaSize()
    {
        var sim = new TerrainSim(config, 5, new System.Random(1));

        Assert.AreEqual(34, sim.Map.Width);
        Assert.AreEqual(19, sim.Map.Height);
    }

    // ---- Kule/sığınak yerleşimi tehlikeli karelerden kaçınır ----

    [Test]
    public void ZonePlacement_AvoidsBlockedTiles()
    {
        TerrainMap map = MapGenerator.Generate(34, 19, 3, MapRecipe.Default);
        var arena = new ArenaBounds(new Vector2(34f, 19f));

        for (int seed = 0; seed < 20; seed++)
        {
            List<Vector2> spots = ZonePlacement.Pick(arena, 6, 2.5f, 4f, new System.Random(seed), map.IsBlockedPoint);
            foreach (Vector2 spot in spots)
                Assert.IsFalse(map.IsBlockedPoint(spot), $"Alan engelli bir karede (tohum {seed})");
        }
    }

    // ---- Yeni kelime ----

    [TestCase("ateş")]
    [TestCase("ates")]
    [TestCase("yangın")]
    [TestCase("fire")]
    [TestCase("molotof")]
    public void FireWord_IsFound_AndIsAnInstantIgniter(string text)
    {
        WordDatabase words = WordDatabase.LoadFromResources(config.maxWordLength);
        WordLookupResult result = words.Lookup(text);

        Assert.AreEqual(WordLookupStatus.Found, result.Status);
        Assert.AreEqual("ates", result.Entry.id);
        Assert.IsTrue(result.Entry.HasTag("instant"));
        Assert.IsTrue(result.Entry.HasTag("ignite"));
    }
}
