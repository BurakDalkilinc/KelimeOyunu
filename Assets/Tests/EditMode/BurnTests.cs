using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Yanma testleri: ateş hasarı tutuşturur ve kömürleştirir, kalkan korur, ateşli yaratık ve araç patlaması yakar.</summary>
public class BurnTests
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
    public void FireDamage_Ignites_AndCharsMoreEachHit_UpToBlack()
    {
        var player = new PlayerState(1, "a", 10);

        player.TakeFireDamage(1, config);
        Assert.IsTrue(player.Status.IsBurning);
        Assert.AreEqual(config.burnSeconds, player.Status.BurnTimeLeft, 0.001f);
        Assert.AreEqual(config.charPerFireHit, player.Status.CharLevel, 0.001f);

        for (int i = 0; i < 5; i++) player.TakeFireDamage(1, config);
        Assert.AreEqual(1f, player.Status.CharLevel, 0.001f, "en fazla simsiyah");

        player.Status.Tick(config.burnSeconds + 0.1f);
        Assert.IsFalse(player.Status.IsBurning, "alev söner");
        Assert.AreEqual(1f, player.Status.CharLevel, 0.001f, "kömürleşme kalır");
    }

    [Test]
    public void Shield_BlocksFire_NoBurning()
    {
        var player = new PlayerState(1, "a", 3);
        player.Status.ApplyShield(1, 30f);

        player.TakeFireDamage(1, config);

        Assert.AreEqual(3, player.Health.Current);
        Assert.IsFalse(player.Status.IsBurning);
        Assert.AreEqual(0f, player.Status.CharLevel);
    }

    [Test]
    public void Tint_DarkensWithChar_KeepsAlpha()
    {
        Color baseColor = new Color(0.9f, 0.8f, 0.7f, 0.5f);
        Color burnt = StatusTint.Burnt(baseColor, 1f, false);
        Assert.Less(burnt.r + burnt.g + burnt.b, 0.6f, "kömür rengi koyu");
        Assert.AreEqual(0.5f, burnt.a, 0.001f);
        Assert.AreEqual(baseColor, StatusTint.Burnt(baseColor, 0f, false));
    }

    [Test]
    public void FireStrikeWord_IgnitesWhoItHits()
    {
        WordEntry fire = WordDatabase.LoadFromResources(50).Lookup("ates").Entry;
        var world = new FakeWorld();
        PlayerState target = world.Add(1, Vector2.zero);
        var arena = new ArenaBounds(config.arenaSize);

        CreatureModel strike = CreatureFactory.CreateAttack(fire, target, Vector2.zero, config, arena, 0, 1);
        strike.PlaceAt(Vector2.zero);
        var hits = new List<CreatureHit>();
        for (int i = 0; i < 600 && !strike.HasStruck; i++)
            strike.TickStrike(1f / 60f, world, config.playerRadius, hits);

        Assert.IsTrue(strike.HasStruck);
        Assert.Less(target.Health.Current, 3);
        Assert.IsTrue(target.Status.IsBurning, "ateş vuruşu tutuşturur");
    }

    [Test]
    public void VehicleExplosion_SetsNearbyOnFire()
    {
        var world = new FakeWorld();
        PlayerState driver = world.Add(1, Vector2.zero);
        driver.Status.Mount(new WordEntry { id = "motor", display = "Motor", tags = new[] { "vehicle", "explosive" }, stats = new WordStats() }, 5f);
        PlayerState near = world.Add(2, new Vector2(1f, 0f));

        VehicleRules.Crash(driver, Vector2.zero, world, null, null, config);

        Assert.IsTrue(near.Status.IsBurning);
        Assert.Greater(near.Status.CharLevel, 0f);
    }
}
