using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Çözüm alanı (mağara) tehdidin bütün hasar türlerinden korur: ejderhanın ateş alanı, yerde kalan ateş ve üstündeki yanma.
/// Alanın dışındaki oyuncu ise hepsinden hasar alır.
/// </summary>
public class ShelterFireTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position, int health = 3)
        {
            var player = new PlayerState(id, "P" + id, health);
            players.Add(player);
            Positions[id] = position;
            return player;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);
    }

    class CollectingSink : ICreatureSink
    {
        public readonly List<CreatureModel> Models = new List<CreatureModel>();
        public void Spawn(CreatureModel model, string label) => Models.Add(model);
    }

    const float PlayerRadius = 0.5f;

    GameConfig config;
    ArenaBounds arena;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        arena = new ArenaBounds(new Vector2(20f, 12f));
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static WordEntry Ejderha() => WordDatabase.LoadFromResources(50).Lookup("ejderha").Entry;

    /// <summary>Tehdidi kurar, ilk mağaraya <paramref name="sheltered"/> oyuncuyu koyar, korunanları günceller.</summary>
    GlobalThreat ThreatWithShelteredPlayer(FakeWorld world, PlayerState sheltered, CollectingSink sink)
    {
        var threat = new GlobalThreat(Ejderha(), world.Players[0], config, arena, world, new Random(3), sink);
        world.Positions[sheltered.Id] = threat.Zones.Zones[0].Position;
        threat.Tick(0.01f);
        Assert.IsTrue(threat.Zones.IsProtected(sheltered.Id), "Mağaradaki korunuyor");
        return threat;
    }

    /// <summary>Noktadan arenanın ortasına doğru birim yön (kenardaki mağaranın yanındaki nokta harita dışına taşmasın).</summary>
    static Vector2 TowardCenter(Vector2 from) => from.sqrMagnitude > 0.0001f ? -from.normalized : Vector2.right;

    [Test]
    public void DragonFireArea_DoesNotHurtTheShelteredPlayer_ButHurtsTheOneOutside()
    {
        var world = new FakeWorld();
        PlayerState inCave = world.Add(1, Vector2.zero);
        PlayerState outside = world.Add(2, Vector2.zero);
        var sink = new CollectingSink();
        GlobalThreat threat = ThreatWithShelteredPlayer(world, inCave, sink);

        // Dışarıdaki mağaranın hemen kenarında (alanın dışında), ejderhanın ateş yarıçapı ikisini de kapsıyor.
        SafeZone cave = threat.Zones.Zones[0];
        world.Positions[outside.Id] = cave.Position + TowardCenter(cave.Position) * (cave.Radius + 0.3f);
        threat.Tick(7.1f);
        Assert.AreEqual(1, sink.Models.Count);
        Assert.IsFalse(threat.Zones.IsProtected(outside.Id));
        CreatureModel dragon = sink.Models[0];
        Vector2 between = (world.Positions[inCave.Id] + world.Positions[outside.Id]) * 0.5f;

        for (int i = 0; i < 30; i++)
        {
            threat.Tick(0.1f);
            dragon.PlaceAt(between); // Ejderha tam ikisinin arasında: ateş yarıçapı ikisini de kapsar
            dragon.TickArea(0.1f, world, PlayerRadius);
        }

        Assert.AreEqual(3, inCave.Health.Current, "Mağaradakine ateş işlemez");
        Assert.IsFalse(inCave.Status.IsBurning);
        Assert.Less(outside.Health.Current, 3, "Dışarıdaki yanar");
        Assert.IsTrue(outside.Status.IsBurning);
    }

    [Test]
    public void BurningPlayer_IsExtinguished_OnEnteringTheCave()
    {
        var world = new FakeWorld();
        PlayerState runner = world.Add(1, new Vector2(-9f, -5f));
        var threat = new GlobalThreat(Ejderha(), runner, config, arena, world, new Random(3), new CollectingSink());
        runner.Status.Ignite(5f, 0.35f);

        threat.Tick(0.1f);
        Assert.IsTrue(runner.Status.IsBurning, "Dışarıdayken yanmaya devam eder");

        world.Positions[runner.Id] = threat.Zones.Zones[0].Position;
        threat.Tick(0.1f);

        Assert.IsFalse(runner.Status.IsBurning, "Mağaraya girince söner");
        Assert.AreEqual(0.35f, runner.Status.CharLevel, 0.001f, "Kömürleşme kalır");
    }

    [Test]
    public void GroundFire_DoesNotHurtShelteredPlayer_ButHurtsTheOneOutside()
    {
        config.fireSpreadChancePerSecond = 0f;
        var map = new TerrainMap(20, 12);
        for (int x = 0; x < 20; x++) for (int y = 0; y < 12; y++) map.Set(x, y, TileType.WoodFloor);
        var terrain = new TerrainSim(config, map, new Random(1));

        var world = new FakeWorld();
        PlayerState inCave = world.Add(1, Vector2.zero);
        PlayerState outside = world.Add(2, Vector2.zero);
        var threats = new List<GlobalThreat>();
        GlobalThreat threat = ThreatWithShelteredPlayer(world, inCave, new CollectingSink());
        threats.Add(threat);
        terrain.Hazards.IsSheltered = player => RefugeFinder.IsSheltered(threats, player);

        SafeZone cave = threat.Zones.Zones[0];
        world.Positions[outside.Id] = cave.Position + TowardCenter(cave.Position) * (cave.Radius + 1f);
        terrain.Fire.IgniteArea(cave.Position, cave.Radius + 2f); // Mağaranın içi de dışı da yanıyor

        Assert.IsFalse(threat.Zones.IsProtected(outside.Id));
        for (int i = 0; i < 30; i++)
        {
            threat.Tick(0.1f);
            terrain.Tick(0.1f, null, world.Players, id => world.Positions[id], (id, to) => world.Positions[id] = to);
        }

        Assert.AreEqual(3, inCave.Health.Current, "Mağaradakine yerdeki ateş işlemez");
        Assert.Less(outside.Health.Current, 3, "Dışarıdaki yanan karoda hasar alır");
    }

    [Test]
    public void GroundFire_HurtsEveryone_WhenNoShelterIsBound()
    {
        var map = new TerrainMap(10, 8);
        for (int x = 0; x < 10; x++) for (int y = 0; y < 8; y++) map.Set(x, y, TileType.WoodFloor);
        var terrain = new TerrainSim(config, map, new Random(1));
        var player = new PlayerState(1, "P", 3);
        terrain.Fire.Ignite(5, 4);

        terrain.Hazards.Tick(0.1f, new List<PlayerState> { player }, id => map.CellCenter(5, 4), (id, to) => { });

        Assert.Less(player.Health.Current, 3);
    }

    [Test]
    public void Ejderha_EntersFromTheEdge_AfterTheWarning()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 8; i++) world.Add(i, new Vector2(-6f + i, 0f));
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Ejderha(), world.Players[0], config, arena, world, new Random(5), sink);

        threat.Tick(ThreatSpec.GetWarningSeconds(Ejderha(), config) + 0.05f);

        Assert.AreEqual(1, sink.Models.Count);
        Vector2 at = sink.Models[0].Position;
        Vector2 half = arena.HalfSize;
        bool onEdge = Mathf.Abs(Mathf.Abs(at.x) - half.x) < 1f || Mathf.Abs(Mathf.Abs(at.y) - half.y) < 1f;
        Assert.IsTrue(onEdge, "Ejderha haritanın kenarından girer: " + at);
    }

    [Test]
    public void Ejderha_CavesFitTheAlivePlayers()
    {
        // Mağaralar hayattaki herkesi alacak kadar olmalı (alan başına kapasite x alan sayısı >= hayattaki).
        foreach (int alive in new[] { 2, 4, 8, 12, 20 })
        {
            int total = ThreatSpec.GetZoneCount(Ejderha(), alive, config) * ThreatSpec.GetZoneCapacity(Ejderha(), alive, config);
            Assert.GreaterOrEqual(total, alive, alive + " oyuncu");
        }
    }
}
