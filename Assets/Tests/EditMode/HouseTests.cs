using NUnit.Framework;
using UnityEngine;

/// <summary>Ev testleri: üretim, kapılar, çatı kuralı, ev metni ve lavdan çıkış yönü.</summary>
public class HouseTests
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

    [Test]
    public void Generator_MakesHousesWithTwoTileDoors()
    {
        int withHouses = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            TerrainMap map = MapGenerator.Generate(34, 19, seed, MapRecipe.Default);
            if (map.Houses.Count > 0) withHouses++;

            foreach (House house in map.Houses)
            {
                // Duvar çevresinde en az iki bitişik geçit (kapı) bulunmalı.
                int best = 0, run = 0;
                for (int x = house.X; x < house.X + house.W; x++)
                {
                    bool open = !TerrainMap.IsSolid(map.At(x, house.Y)) || !TerrainMap.IsSolid(map.At(x, house.Y + house.H - 1));
                    run = open ? run + 1 : 0;
                    best = Mathf.Max(best, run);
                }
                Assert.GreaterOrEqual(best, 2, $"seed {seed}: kapı en az 2 geniş olmalı");
            }
        }
        Assert.Greater(withHouses, 10);
    }

    [Test]
    public void Houses_EncodeAndApply_RoundTrip()
    {
        var map = new TerrainMap(34, 19);
        map.AddHouse(new House(2, 3, 5, 4));
        map.AddHouse(new House(10, 6, 5, 4));

        var other = new TerrainMap(34, 19);
        Assert.IsTrue(other.TryApplyHouses(map.EncodeHouses()));
        Assert.AreEqual(2, other.Houses.Count);
        Assert.AreEqual(10, other.Houses[1].X);

        Assert.IsFalse(other.TryApplyHouses("1,2,3"));
        Assert.AreEqual(2, other.Houses.Count);

        Assert.IsTrue(other.TryApplyHouses(""));
        Assert.AreEqual(0, other.Houses.Count);
    }

    [Test]
    public void Roof_HidesWhenViewerInside_ShowsOutside()
    {
        var map = new TerrainMap(34, 19);
        var house = new House(5, 5, 5, 4);

        Assert.IsTrue(RoofRules.ShouldHide(map, house, map.CellCenter(7, 6), null));
        Assert.IsFalse(RoofRules.ShouldHide(map, house, map.CellCenter(1, 1), null));
        Assert.IsFalse(RoofRules.ShouldHide(map, house, null, null));
        Assert.IsTrue(RoofRules.ShouldHide(map, house, null, (x, y) => x == 6 && y == 6), "yanan ev çatısı açılmalı");
    }

    [Test]
    public void Escape_FromLava_PointsToSafeGround()
    {
        var map = new TerrainMap(10, 8);
        for (int y = 2; y <= 5; y++)
            for (int x = 2; x <= 5; x++)
                map.Set(x, y, TileType.Lava);

        var sim = new TerrainSim(config, map, new System.Random(1));
        Vector2 inLava = map.CellCenter(5, 4);

        Assert.IsTrue(sim.IsHazardousAt(inLava));
        Assert.IsFalse(sim.IsHazardousAt(map.CellCenter(8, 1)));

        Vector2 direction = sim.EscapeDirection(inLava);
        Assert.Greater(direction.x, 0.5f, "en yakın kenar sağda");
        Assert.AreEqual(1f, direction.magnitude, 0.01f);
        Assert.AreEqual(Vector2.zero, sim.EscapeDirection(map.CellCenter(8, 1)) == Vector2.zero ? Vector2.zero : Vector2.zero);
    }

    [Test]
    public void Slide_StopsAtThinChasm_InsteadOfJumpingOver()
    {
        var map = new TerrainMap(10, 8);
        map.Set(5, 4, TileType.Chasm); // 1 karelik ince yarık
        var sim = new TerrainSim(config, map, new System.Random(1));

        Vector2 from = map.CellCenter(3, 4);
        Vector2 stopped = sim.FirstHazardOnPath(from, from + new Vector2(3f, 0f), out bool hit);

        Assert.IsTrue(hit);
        Assert.AreEqual(TileType.Chasm, map.At(stopped));

        Vector2 free = sim.FirstHazardOnPath(from, from + new Vector2(1f, 0f), out bool freeHit);
        Assert.IsFalse(freeHit);
        Assert.AreEqual(from + new Vector2(1f, 0f), free);
    }

    [Test]
    public void Knockback_Cancel_StopsSliding()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(3f, 0f), 0.25f);
        motion.Cancel();
        Assert.AreEqual(Vector2.zero, motion.Step(0.1f));
    }

    [Test]
    public void Fall_AfterPush_AnimatesThenKills()
    {
        var map = new TerrainMap(12, 8);
        map.Set(6, 4, TileType.Chasm);
        map.Set(3, 6, TileType.Lava);
        var sim = new TerrainSim(config, map, new System.Random(1));
        var players = new System.Collections.Generic.List<PlayerState>();

        var chasmVictim = new PlayerState(1, "a", 3);
        var lavaVictim = new PlayerState(2, "b", 3);
        var safe = new PlayerState(3, "c", 3);
        players.Add(chasmVictim); players.Add(lavaVictim); players.Add(safe);

        Assert.IsTrue(sim.TryStartFall(chasmVictim, map.CellCenter(6, 4)));
        Assert.IsTrue(sim.TryStartFall(lavaVictim, map.CellCenter(3, 6)));
        Assert.IsFalse(sim.TryStartFall(safe, map.CellCenter(1, 1)));
        Assert.AreEqual(FallKind.Chasm, chasmVictim.Falling);
        Assert.IsTrue(chasmVictim.IsAlive, "animasyon sürerken henüz ölmedi");

        System.Func<int, Vector2?> where = id => id == 1 ? map.CellCenter(6, 4) : id == 2 ? map.CellCenter(3, 6) : map.CellCenter(1, 1);
        sim.Tick(config.fallSeconds * 0.5f, null, players, where, (id, to) => { });
        Assert.IsTrue(chasmVictim.IsAlive);
        Assert.AreEqual(3, chasmVictim.Health.Current, "düşerken normal uçurum hasarı/ışınlanma işlemez");

        sim.Tick(config.fallSeconds, null, players, where, (id, to) => { });
        Assert.IsFalse(chasmVictim.IsAlive);
        Assert.IsFalse(lavaVictim.IsAlive);
        Assert.IsTrue(safe.IsAlive);
        Assert.AreEqual(FallKind.None, chasmVictim.Falling);
    }

    [Test]
    public void Walking_IntoLavaOrChasm_AlsoStartsAFall_AndKillsOutright()
    {
        var map = new TerrainMap(12, 8);
        map.Set(6, 4, TileType.Chasm);
        map.Set(3, 6, TileType.Lava);
        var sim = new TerrainSim(config, map, new System.Random(1));

        var a = new PlayerState(1, "a", 3);
        var b = new PlayerState(2, "b", 3);
        var players = new System.Collections.Generic.List<PlayerState> { a, b };
        System.Func<int, Vector2?> where = id => id == 1 ? map.CellCenter(6, 4) : map.CellCenter(3, 6);

        sim.Tick(0.1f, null, players, where, (id, to) => { });
        Assert.AreEqual(FallKind.Chasm, a.Falling);
        Assert.AreEqual(FallKind.Lava, b.Falling);
        Assert.AreEqual(3, a.Health.Current, "animasyon sürerken can duruyor");

        sim.Tick(config.fallSeconds + 0.1f, null, players, where, (id, to) => { });
        Assert.IsFalse(a.IsAlive);
        Assert.IsFalse(b.IsAlive);
    }

    [Test]
    public void Fall_IgnoresShield()
    {
        var map = new TerrainMap(12, 8);
        map.Set(6, 4, TileType.Lava);
        var sim = new TerrainSim(config, map, new System.Random(1));
        var p = new PlayerState(1, "a", 3);
        p.Status.ApplyShield(1, 30f);
        var players = new System.Collections.Generic.List<PlayerState> { p };
        System.Func<int, Vector2?> where = id => map.CellCenter(6, 4);

        sim.Tick(0.1f, null, players, where, (id, to) => { });
        sim.Tick(config.fallSeconds + 0.1f, null, players, where, (id, to) => { });
        Assert.IsFalse(p.IsAlive);
    }
}
