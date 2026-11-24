using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Araç testleri: binme, hiç durmadan sürme, kenardan sekme, ezme, binaya çarpıp patlama.</summary>
public class VehicleTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position)
        {
            var player = new PlayerState(id, "P" + id, 3);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    /// <summary>x &gt;= 3 olan her yer katı (bir evin duvarı gibi).</summary>
    class WallAtX3 : IObstacleField
    {
        public Vector2 Resolve(Vector2 position, float radius)
            => position.x + radius > 3f ? new Vector2(3f - radius, position.y) : position;
    }

    GameConfig config;
    ArenaBounds arena;
    WordDatabase db;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(34f, 19f);
        arena = new ArenaBounds(config.arenaSize);
        db = WordDatabase.LoadFromResources(50);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    PlayerState MountedDriver(FakeWorld world, string word, Vector2 at)
    {
        PlayerState driver = world.Add(1, at);
        WordEntry vehicle = db.Lookup(word).Entry;
        TagRegistry.ApplySelfEffects(vehicle, new SelfEffectContext(driver, vehicle, config));
        return driver;
    }

    [Test]
    public void MotorAndCar_AreExplosiveVehicles_AndMountTheWriter()
    {
        var world = new FakeWorld();
        PlayerState driver = MountedDriver(world, "motosiklet", Vector2.zero);

        Assert.IsTrue(driver.Status.IsDriving);
        Assert.AreEqual("motor", driver.Status.Vehicle.id);
        Assert.IsTrue(VehicleRules.Explodes(driver.Status.Vehicle));
        Assert.IsTrue(VehicleRules.Explodes(db.Lookup("taksi").Entry));
        Assert.Greater(VehicleRules.BodyRadius(db.Lookup("araba").Entry, config), VehicleRules.BodyRadius(driver.Status.Vehicle, config), "araba daha geniş");

        driver.Status.Tick(100f);
        Assert.IsFalse(driver.Status.IsDriving, "süre bitince iner");
    }

    [Test]
    public void Drive_NeverStops_InputOnlySteers_BouncesOffArenaEdge()
    {
        Vector2 heading = Vector2.right;
        Vector2 p = VehicleRules.Drive(Vector2.zero, ref heading, Vector2.zero, 10f, 0.1f, 0.5f, arena, null, out bool crashed);
        Assert.IsFalse(crashed);
        Assert.AreEqual(1f, p.x, 0.001f, "girdi yokken de ileri gider");

        p = VehicleRules.Drive(p, ref heading, Vector2.up, 10f, 0.1f, 0.5f, arena, null, out crashed);
        Assert.AreEqual(Vector2.up, heading, "girdi yön verir");

        heading = Vector2.right;
        Vector2 nearEdge = new Vector2(arena.HalfSize.x - 0.6f, 0f);
        VehicleRules.Drive(nearEdge, ref heading, Vector2.zero, 10f, 0.2f, 0.5f, arena, null, out crashed);
        Assert.IsFalse(crashed, "arena kenarı patlatmaz");
        Assert.Less(heading.x, 0f, "kenardan seker");
    }

    [Test]
    public void Drive_IntoBuilding_Crashes()
    {
        Vector2 heading = Vector2.right;
        Vector2 p = VehicleRules.Drive(new Vector2(1f, 0f), ref heading, Vector2.zero, 10f, 0.5f, 0.5f, arena, new WallAtX3(), out bool crashed);
        Assert.IsTrue(crashed);
        Assert.LessOrEqual(p.x, 2.5f + 0.001f, "duvarın içine girmez");
    }

    [Test]
    public void Crush_KillsOncePerRide_ShieldProtects_DriverSafe()
    {
        var world = new FakeWorld();
        PlayerState driver = MountedDriver(world, "motor", Vector2.zero);
        PlayerState victim = world.Add(2, new Vector2(0.8f, 0f));
        PlayerState shielded = world.Add(3, new Vector2(-0.8f, 0f));
        shielded.Status.ApplyShield(1, 30f);
        PlayerState far = world.Add(4, new Vector2(5f, 0f));

        Assert.AreEqual(2, VehicleRules.Crush(driver, Vector2.zero, world, config));
        Assert.IsFalse(victim.IsAlive, "ezilen elenir");
        Assert.IsTrue(shielded.IsAlive, "kalkan korur");
        Assert.IsTrue(far.IsAlive);
        Assert.IsTrue(driver.IsAlive);

        Assert.AreEqual(0, VehicleRules.Crush(driver, Vector2.zero, world, config), "aynı sürüşte ikinci kez ezilmez");
        Assert.IsTrue(shielded.IsAlive);
    }

    [Test]
    public void Crash_ExplodingVehicle_KillsDriverEvenWithShield_HurtsAndPushesNearby()
    {
        var world = new FakeWorld();
        PlayerState driver = MountedDriver(world, "araba", Vector2.zero);
        driver.Status.ApplyShield(3, 30f);
        PlayerState near = world.Add(2, new Vector2(1f, 0f));
        PlayerState far = world.Add(3, new Vector2(8f, 0f));
        var pushes = new Dictionary<int, Vector2>();

        bool exploded = VehicleRules.Crash(driver, Vector2.zero, world, (id, push) => pushes[id] = push, null, config);

        Assert.IsTrue(exploded);
        Assert.IsFalse(driver.IsAlive, "sürücü ölür (kalkan kurtarmaz)");
        Assert.IsFalse(driver.Status.IsDriving);
        Assert.AreEqual(3 - config.vehicleCrashDamage, near.Health.Current);
        Assert.Greater(pushes[2].x, 0f, "dışarı savrulur");
        Assert.AreEqual(3, far.Health.Current);
        Assert.IsFalse(pushes.ContainsKey(3));
    }

    [Test]
    public void Crash_NonExplodingVehicle_JustStops()
    {
        var world = new FakeWorld();
        PlayerState driver = world.Add(1, Vector2.zero);
        var cart = new WordEntry { id = "bisiklet", display = "Bisiklet", tags = new[] { "vehicle" }, stats = new WordStats { speed = 6f, duration = 5f } };
        driver.Status.Mount(cart, 5f);

        Assert.IsFalse(VehicleRules.Crash(driver, Vector2.zero, world, null, null, config));
        Assert.IsTrue(driver.IsAlive);
        Assert.IsFalse(driver.Status.IsDriving);
    }
}
