using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>WaveEffect (Tsunami) testleri: dalganın ilerlemesi, kuledekilerin korunması, bir kez vurma, görünüm bilgisi.</summary>
public class WaveEffectTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position, int health = 3)
        {
            var player = new PlayerState(id, "P" + id, health);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public void Move(int id, Vector2 position) => positions[id] = position;

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Tsunami() => WordDatabase.LoadFromResources(50).Lookup("tsunami").Entry;

    ThreatContext Context(FakeWorld world, SafeZoneField zones, int seed = 1)
    {
        return new ThreatContext(Tsunami(), null, config, arena, world, new Random(seed), zones);
    }

    /// <summary>Dalganın ilerleme ekseninde, perpendicular=0 çizgisinde bir nokta: Direction * along.</summary>
    static Vector2 OnAxis(WaveEffect wave, float along) => wave.Direction * along;

    // ---- Kurulum ----

    [Test]
    public void Begin_PicksOneOfFourAxisDirections_AndStartsAtTheNearEdge()
    {
        var seen = new HashSet<Vector2>();
        for (int seed = 0; seed < 60; seed++)
        {
            var wave = new WaveEffect();
            wave.Begin(Context(new FakeWorld(), new SafeZoneField(), seed));

            seen.Add(wave.Direction);
            float halfExtent = Mathf.Abs(wave.Direction.x) * 10f + Mathf.Abs(wave.Direction.y) * 6f;
            Assert.AreEqual(-halfExtent, wave.Front, 0.001f);
            Assert.AreEqual(1f, wave.Direction.magnitude, 0.001f);
        }

        Assert.AreEqual(4, seen.Count, "Dört kenardan da gelebilir");
    }

    [Test]
    public void Tsunami_FromRealWords_UsesSpeedFromTheWord()
    {
        var wave = new WaveEffect();
        wave.Begin(Context(new FakeWorld(), new SafeZoneField()));

        Assert.AreEqual(4f, wave.Speed, 0.001f); // words.json: speed 4
        Assert.IsTrue(wave.IsActive);
        Assert.IsFalse(wave.IsFinished);
    }

    [Test]
    public void CrossingSeconds_MatchesArenaSizeAndSpeed()
    {
        var wave = new WaveEffect();
        wave.Begin(Context(new FakeWorld(), new SafeZoneField()));

        float halfExtent = Mathf.Abs(wave.Direction.x) * 10f + Mathf.Abs(wave.Direction.y) * 6f;
        Assert.AreEqual((2f * halfExtent + WaveEffect.Thickness) / 4f, wave.CrossingSeconds, 0.001f);
    }

    // ---- Vuruş ----

    [Test]
    public void Wave_HitsPlayersOnlyWhenItsFrontReachesThem_InOrder()
    {
        var world = new FakeWorld();
        var first = world.Add(1, Vector2.zero);
        var second = world.Add(2, Vector2.zero);
        var third = world.Add(3, Vector2.zero);
        var wave = new WaveEffect();
        wave.Begin(Context(world, new SafeZoneField()));

        world.Move(1, OnAxis(wave, -5f));
        world.Move(2, OnAxis(wave, 0f));
        world.Move(3, OnAxis(wave, 5f));
        float halfExtent = -wave.Front;

        // Ön yüz -5'e varana kadar kimse vurulmaz.
        wave.Tick((halfExtent - 5.2f) / wave.Speed);
        Assert.IsTrue(first.IsAlive && second.IsAlive && third.IsAlive);

        wave.Tick(0.3f / wave.Speed * 2f);  // -5'i geçti
        Assert.IsFalse(first.IsAlive);
        Assert.IsTrue(second.IsAlive);

        wave.Tick(6f / wave.Speed);          // 0'ı geçti
        Assert.IsFalse(second.IsAlive);
        Assert.IsTrue(third.IsAlive);

        wave.Tick(6f / wave.Speed);          // 5'i geçti
        Assert.IsFalse(third.IsAlive);
    }

    [Test]
    public void Wave_SparesThoseProtectedInATower_AtTheMomentItPasses()
    {
        var world = new FakeWorld();
        var safe = world.Add(1, Vector2.zero);
        var exposed = world.Add(2, Vector2.zero);
        var zones = new SafeZoneField();
        var wave = new WaveEffect();
        wave.Begin(Context(world, zones));

        Vector2 towerPosition = OnAxis(wave, 2f);
        zones.Add(new SafeZone("tower", towerPosition, 1.6f, 3, 100f));
        world.Move(1, towerPosition);
        world.Move(2, OnAxis(wave, 6f));
        zones.Update(world, 0f);

        wave.Tick(100f);

        Assert.IsTrue(safe.IsAlive, "Kuledeki kurtulur");
        Assert.IsFalse(exposed.IsAlive);
        Assert.AreEqual(1, wave.Affected);
    }

    [Test]
    public void Wave_HitsEachPlayerOnlyOnce()
    {
        var world = new FakeWorld();
        var player = world.Add(1, Vector2.zero, health: 100);
        var wave = new WaveEffect();
        wave.Begin(Context(world, new SafeZoneField()));
        world.Move(1, OnAxis(wave, 0f));

        wave.Tick(100f);
        int healthAfterFirst = player.Health.Current;
        wave.Tick(1f);

        Assert.AreEqual(1, wave.Affected);
        Assert.AreEqual(healthAfterFirst, player.Health.Current);
    }

    [Test]
    public void Wave_ShieldAbsorbsTheWaveOnce()
    {
        var world = new FakeWorld();
        var shielded = world.Add(1, Vector2.zero);
        shielded.Status.ApplyShield(1, 30f);
        var wave = new WaveEffect();
        wave.Begin(Context(world, new SafeZoneField()));
        world.Move(1, OnAxis(wave, 0f));

        wave.Tick(100f);

        Assert.IsTrue(shielded.IsAlive);
        Assert.IsFalse(shielded.Status.HasShield);
    }

    [Test]
    public void Wave_FlyingPlayersAreNotImmune_ButEliminatedPlayersAreIgnored()
    {
        var world = new FakeWorld();
        var flyer = world.Add(1, Vector2.zero);
        flyer.Status.ApplyFlight(10f, 1.2f);
        var dead = world.Add(2, Vector2.zero);
        dead.Health.TakeDamage(99);
        var wave = new WaveEffect();
        wave.Begin(Context(world, new SafeZoneField()));
        world.Move(1, OnAxis(wave, 1f));
        world.Move(2, OnAxis(wave, 1f));

        wave.Tick(100f);

        Assert.IsFalse(flyer.IsAlive, "Dalga yerdeki saldırı değil, uçmak korumaz");
        Assert.AreEqual(1, wave.Affected);
    }

    [Test]
    public void Wave_PlayerWhoRanBehindTheFront_IsNotHit()
    {
        var world = new FakeWorld();
        var runner = world.Add(1, Vector2.zero);
        var wave = new WaveEffect();
        wave.Begin(Context(world, new SafeZoneField()));
        float halfExtent = -wave.Front;

        // Oyuncu dalganın hâlâ ulaşmadığı yerde. Dalga yarı yola geldi, oyuncu hâlâ önünde, tekrar ilerledi.
        world.Move(1, OnAxis(wave, 8f));
        wave.Tick((halfExtent + 4f) / wave.Speed); // Ön yüz 4'te
        Assert.IsTrue(runner.IsAlive);

        wave.Tick((10f - 4f) / wave.Speed);        // Ön yüz 10'a vardı: 8'i geçti
        Assert.IsFalse(runner.IsAlive);
    }

    // ---- Bitiş ----

    [Test]
    public void Wave_FinishesAfterCrossingTheWholeArena()
    {
        var wave = new WaveEffect();
        wave.Begin(Context(new FakeWorld(), new SafeZoneField()));

        wave.Tick(wave.CrossingSeconds * 0.5f);
        Assert.IsTrue(wave.IsActive);
        Assert.IsFalse(wave.IsFinished);

        wave.Tick(wave.CrossingSeconds * 0.6f);
        Assert.IsTrue(wave.IsFinished);
        Assert.IsFalse(wave.IsActive);
    }

    [Test]
    public void Wave_NegativeDelta_DoesNotMoveBackward()
    {
        var wave = new WaveEffect();
        wave.Begin(Context(new FakeWorld(), new SafeZoneField()));
        float start = wave.Front;

        wave.Tick(-5f);

        Assert.AreEqual(start, wave.Front, 0.0001f);
    }

    // ---- GlobalThreat: gerçek Tsunami ----

    [Test]
    public void Tsunami_AsAGlobalThreat_HasThreeTowersAndAWaveEffect()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 8; i++) world.Add(i, new Vector2(30f, 30f));
        var threat = new GlobalThreat(Tsunami(), world.Players[0], config, arena, world, new Random(4));

        Assert.AreEqual(3, threat.Zones.Zones.Count);
        foreach (SafeZone zone in threat.Zones.Zones)
        {
            Assert.AreEqual("tower", zone.Type);
            Assert.AreEqual(3, zone.Capacity);
        }
        Assert.AreEqual(1, threat.Effects.Count);
        Assert.IsInstanceOf<WaveEffect>(threat.Effects[0]);
        Assert.AreEqual(5f, threat.WarningLeft, 0.001f);
    }

    [Test]
    public void Tsunami_FullFlow_OnlyTowerOccupantsSurvive()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 8; i++) world.Add(i, new Vector2(30f, 30f));
        var threat = new GlobalThreat(Tsunami(), world.Players[0], config, arena, world, new Random(4));

        // 1-3 numaralı oyuncular ilk kuleye, 4. oyuncu ikinci kuleye girer; geri kalanı açıkta.
        SafeZone towerA = threat.Zones.Zones[0];
        SafeZone towerB = threat.Zones.Zones[1];
        for (int i = 1; i <= 3; i++) world.Move(i, towerA.Position + new Vector2(0.1f * i, 0f));
        world.Move(4, towerB.Position);
        for (int i = 5; i <= 8; i++) world.Move(i, new Vector2(-9f + i, -5.5f)); // Arenanın içinde ama kulelerin dışında

        threat.Tick(5.1f);                 // Uyarı bitti, dalga başladı
        threat.Tick(30f);                  // Dalga her yeri geçti

        for (int i = 1; i <= 4; i++) Assert.IsTrue(world.Players[i - 1].IsAlive, "Kuledeki " + i + " kurtulmalı");
        for (int i = 5; i <= 8; i++) Assert.IsFalse(world.Players[i - 1].IsAlive, "Açıktaki " + i + " elenmeli");
    }
}
