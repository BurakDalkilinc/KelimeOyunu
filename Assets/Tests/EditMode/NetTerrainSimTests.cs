using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Ağ maçı host simülasyonunda arazi: lav, uçurum, yangın, engeller, kule yerleşimi (ağsız).</summary>
public class NetTerrainSimTests
{
    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public readonly List<(int id, Vector2 to)> Teleports = new List<(int, Vector2)>();

        public bool TryGetPosition(int id, out Vector2 p) => Positions.TryGetValue(id, out p);
        public bool TryApplyKnockback(int id, Vector2 d) => Positions.ContainsKey(id);
        public bool TryGetFacing(int id, out Vector2 d) { d = Vector2.up; return false; }

        public bool TryTeleport(int id, Vector2 to)
        {
            Teleports.Add((id, to));
            Positions[id] = to;
            return true;
        }
    }

    GameConfig config;
    WordDatabase words;
    TerrainMap map;
    TerrainSim terrain;
    FakeAvatars avatars;
    NetMatchSim sim;
    PlayerState writer, victim;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.LoadFromResources(config.maxWordLength);
        config.fireSpreadChancePerSecond = 0f;

        map = new TerrainMap(34, 19);
        terrain = new TerrainSim(config, map, new System.Random(1));

        writer = new PlayerState(1, "Yazan", 3, isBot: true);
        victim = new PlayerState(2, "Kurban", 10);
        avatars = new FakeAvatars();
        avatars.Positions[1] = map.CellCenter(3, 3);
        avatars.Positions[2] = map.CellCenter(20, 10);

        sim = new NetMatchSim(config, new MatchState(new[] { writer, victim }), avatars, new System.Random(3), terrain);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Word(string id)
    {
        foreach (WordEntry w in words.Words) if (w.id == id) return w;
        Assert.Fail("Kelime yok: " + id);
        return null;
    }

    [Test]
    public void FireWord_StrikesTheTargetSpot_AndLightsTheWoodThere()
    {
        for (int x = 17; x <= 23; x++) for (int y = 7; y <= 13; y++) map.Set(x, y, TileType.WoodFloor);

        sim.OnWordPlayed(writer, Word("ates")); // Bot yazar: hedef rastgele rakip (tek rakip: kurban)
        Assert.AreEqual(1, sim.CreatureCount);
        Assert.AreEqual(0, terrain.Fire.BurningCount);

        for (float t = 0f; t < 3f; t += 0.05f) sim.Tick(0.05f);

        Assert.Greater(terrain.Fire.BurningCount + map.Count(TileType.CharredFloor), 3, "Alan tutuşmuş (ya da yanıp kömürleşmiş) olmalı");
        Assert.Less(victim.Health.Current, 10, "Alev hasar da verir");
        Assert.IsTrue(terrain.Dirty);
    }

    [Test]
    public void Obstacles_IncludeTheHouseWalls_AndBotsAvoidHazards()
    {
        map.Set(10, 10, TileType.WoodWall);
        map.Set(12, 10, TileType.Lava);

        Vector2 inWall = map.CellCenter(10, 10);
        Vector2 inLava = map.CellCenter(12, 10);

        Assert.AreNotEqual(inWall, sim.Obstacles.Resolve(inWall, 0.5f));      // Duvar herkesi iter
        Assert.AreEqual(inLava, sim.Obstacles.Resolve(inLava, 0.5f));         // Lava itilen girebilir
        Assert.AreNotEqual(inLava, sim.BotObstacles.Resolve(inLava, 0.5f));   // Bot kendi isteğiyle girmez
    }

    [Test]
    public void FireDangers_AreAddedForBots()
    {
        map.Set(5, 5, TileType.WoodFloor);
        terrain.Fire.Ignite(5, 5);

        sim.Tick(0.05f);

        Assert.AreEqual(1, sim.Dangers.Count);
    }

    [Test]
    public void TowerPlacement_AvoidsLavaChasmAndWalls()
    {
        for (int y = 2; y < 17; y++) for (int x = 2; x < 32; x++)
            if ((x / 3 + y / 3) % 2 == 0) map.Set(x, y, TileType.Lava); // Satranç tahtası gibi yarı yarıya lav

        sim.OnWordPlayed(writer, Word("tsunami"));
        var zones = new List<ZoneSnapshot>();
        sim.SnapshotZones(zones);

        Assert.Greater(zones.Count, 0);
        foreach (ZoneSnapshot z in zones)
            Assert.IsFalse(map.IsBlockedPoint(new Vector2(z.X, z.Y)), "Kule lavın üstüne konmuş");
    }

    [Test]
    public void WithoutTerrain_TheSimStillWorks()
    {
        var plain = new NetMatchSim(config, new MatchState(new[] { writer, victim }), avatars, new System.Random(3));

        Assert.IsNull(plain.Terrain);
        Assert.DoesNotThrow(() => plain.Tick(0.1f));
        Assert.DoesNotThrow(() => plain.OnWordPlayed(writer, Word("ates")));
    }

    // ---- Ağ taşıma: harita durumu metni ----

    [Test]
    public void TerrainState_RoundTrips_WithBurningTiles_AtFullArenaSize()
    {
        TerrainMap source = MapGenerator.Generate(34, 19, 9, MapRecipe.Default);
        string text = source.Encode((x, y) => x == 10 && y == 10);

        var client = new TerrainMap(34, 19);
        var burning = new bool[34 * 19];
        Assert.IsTrue(client.TryApply(text, burning));

        Assert.AreEqual(source.Encode(null), client.Encode(null));
        Assert.IsTrue(burning[client.Index(10, 10)]);
        Assert.AreEqual(34 * 19, text.Length);
    }

    [Test]
    public void TerrainState_IsSmallEnoughToSendOften()
    {
        string text = MapGenerator.Generate(34, 19, 2, MapRecipe.Default).Encode(null);

        Assert.Less(System.Text.Encoding.UTF8.GetByteCount(text), 1024);
    }
}
