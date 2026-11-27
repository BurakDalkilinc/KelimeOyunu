using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Yaratık doğma yeri: ev içine, duvara ya da tehlikeli zemine doğma isteği en yakın uygun yere kaydırılır (gövde yarıçapıyla).</summary>
public class SpawnPlacementTests
{
    const int W = 20, H = 12;

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

    static void BuildHouse(TerrainMap map, int x0, int y0)
    {
        for (int y = y0; y < y0 + 4; y++)
            for (int x = x0; x < x0 + 5; x++)
                map.Set(x, y, x == x0 || x == x0 + 4 || y == y0 || y == y0 + 3 ? TileType.WoodWall : TileType.WoodFloor);
        map.Set(x0 + 1, y0, TileType.WoodFloor);
        map.Set(x0 + 2, y0, TileType.WoodFloor);
        map.AddHouse(new House(x0, y0, 5, 4, x0 + 1, false));
    }

    /// <summary>Gövdenin (yarıçap) değdiği hiçbir kare ev, duvar ya da tehlike değil mi? (Testin kendi bağımsız kontrolü.)</summary>
    static void AssertBodyIsClear(TerrainMap map, Vector2 at, float radius, string message)
    {
        var cells = new List<Vector2Int>();
        HouseDamage.CellsUnderBody(map, at, radius, cells);
        Assert.IsNotEmpty(cells);
        foreach (Vector2Int c in cells)
        {
            Assert.IsTrue(TerrainMap.IsSafeGround(map.At(c.x, c.y)), $"{message}: {c} güvenli zemin olmalı");
            Assert.AreEqual(-1, map.HouseIndexAt(c.x, c.y), $"{message}: {c} evin içinde olmamalı");
        }
    }

    [Test]
    public void SpawnRequestInsideAHouse_IsMovedOutside()
    {
        var map = new TerrainMap(W, H);
        BuildHouse(map, 5, 4);
        Vector2 inside = map.CellCenter(7, 5);
        Assert.IsFalse(SpawnPlacement.IsClearSpot(map, inside, 0.4f));

        Vector2 moved = SpawnPlacement.FindClearSpot(map, inside, 0.4f);
        Vector2Int cell = map.CellOf(moved);
        Assert.AreEqual(-1, map.HouseIndexAt(cell.x, cell.y), "ev içinden dışarı kaydırılır");
        AssertBodyIsClear(map, moved, 0.4f, "küçük yaratık");
        Assert.Less((moved - inside).magnitude, 4f, "en yakın uygun yere");
    }

    [Test]
    public void BigBody_KeepsItsWholeRadiusOutOfHousesAndWalls()
    {
        var map = new TerrainMap(W, H);
        BuildHouse(map, 5, 4);
        map.Set(11, 6, TileType.Cactus);

        foreach (Vector2 request in new[] { map.CellCenter(7, 6), map.CellCenter(10, 6), map.CellCenter(11, 6), map.CellCenter(4, 4) })
        {
            Vector2 moved = SpawnPlacement.FindClearSpot(map, request, 1.3f);
            Assert.IsTrue(SpawnPlacement.IsClearSpot(map, moved, 1.3f));
            AssertBodyIsClear(map, moved, 1.3f, $"boss isteği {request}");
        }
    }

    [Test]
    public void HazardsAreAvoided_AndAClearSpotIsUnchanged()
    {
        var map = new TerrainMap(W, H);
        for (int y = 3; y <= 8; y++)
            for (int x = 3; x <= 8; x++)
                map.Set(x, y, TileType.Lava);
        map.Set(14, 6, TileType.Quicksand);

        Vector2 lava = map.CellCenter(5, 5);
        Vector2 moved = SpawnPlacement.FindClearSpot(map, lava, 0.4f);
        Assert.IsTrue(TerrainMap.IsSafeGround(map.At(moved)), "lavdan çıkar");

        AssertBodyIsClear(map, SpawnPlacement.FindClearSpot(map, map.CellCenter(14, 6), 1.3f), 1.3f, "bataklık kumu");

        Vector2 open = map.CellCenter(16, 2);
        Assert.AreEqual(open, SpawnPlacement.FindClearSpot(map, open, 0.4f), "uygun yer olduğu gibi kalır");
        Assert.AreEqual(open, SpawnPlacement.FindClearSpot(null, open, 1.3f), "harita yoksa dokunulmaz");
    }

    [Test]
    public void BodyHangingOffTheMapEdge_IsMovedInside()
    {
        var map = new TerrainMap(W, H);
        Vector2 corner = map.CellCenter(0, 0);
        Assert.IsFalse(SpawnPlacement.IsClearSpot(map, corner, 1.3f), "gövde haritadan taşıyor");
        Vector2 moved = SpawnPlacement.FindClearSpot(map, corner, 1.3f);
        Assert.IsTrue(SpawnPlacement.IsClearSpot(map, moved, 1.3f));
    }

    [Test]
    public void CreatureBuilder_WithMap_NeverSpawnsABossInsideAHouse()
    {
        // Harita neredeyse tamamen ev ve lav: tek açık köşe kalır; boss her tohumda oraya kaydırılır.
        var map = new TerrainMap(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                map.Set(x, y, TileType.Lava);
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                map.Set(x, y, TileType.Grass);
        BuildHouse(map, 8, 4);

        var boss = new WordEntry
        {
            id = "dev", display = "Dev", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget, tags = new[] { "boss" },
            stats = new WordStats { damage = 1, speed = 2f, duration = 40f, radius = 1.3f, hp = 10 },
        };
        var target = new PlayerState(1, "a", 3);
        for (int seed = 1; seed <= 10; seed++)
        {
            List<CreatureModel> models = CreatureBuilder.BuildAttack(boss, target, Vector2.zero, config, arena, new System.Random(seed), map);
            Assert.AreEqual(1, models.Count);
            AssertBodyIsClear(map, models[0].Position, models[0].Radius, $"tohum {seed}");
        }

        CreatureModel unknown = CreatureBuilder.BuildUnknown(config, arena, new System.Random(4), map);
        AssertBodyIsClear(map, unknown.Position, unknown.Radius, "???");
    }
}
