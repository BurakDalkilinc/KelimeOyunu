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
    public void Push_IntoChasmOrLava_EliminatesVictim()
    {
        var map = new TerrainMap(12, 8);
        map.Set(6, 4, TileType.Chasm);
        map.Set(3, 6, TileType.Lava);
        map.Set(9, 2, TileType.WoodWall);
        map.Set(10, 2, TileType.Lava);
        var sim = new TerrainSim(config, map, new System.Random(1));

        var chasmVictim = new PlayerState(1, "a", 3);
        Assert.IsTrue(sim.TryEliminateByPush(chasmVictim, map.CellCenter(4, 4), new Vector2(3f, 0f)));
        Assert.IsFalse(chasmVictim.IsAlive);

        var lavaVictim = new PlayerState(2, "b", 3);
        Assert.IsTrue(sim.TryEliminateByPush(lavaVictim, map.CellCenter(3, 4), new Vector2(0f, 3f)));
        Assert.IsFalse(lavaVictim.IsAlive);

        var safe = new PlayerState(3, "c", 3);
        Assert.IsFalse(sim.TryEliminateByPush(safe, map.CellCenter(1, 1), new Vector2(3f, 0f)));
        Assert.IsTrue(safe.IsAlive);

        var shielded = new PlayerState(4, "d", 3);
        Assert.IsFalse(sim.TryEliminateByPush(shielded, map.CellCenter(8, 2), new Vector2(3f, 0f)), "duvar önce gelir");
    }
}
