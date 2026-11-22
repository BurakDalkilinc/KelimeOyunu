using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>"explosive" etiketinin testleri: Bomba (gecikmeli alan hasarı, yanına düşme, geri sayım).</summary>
public class ExplosiveTagTests
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

    const float PlayerRadius = 0.5f;

    ArenaBounds arena;
    GameConfig config;
    FakeWorld world;
    List<CreatureHit> hits;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // yanına düşme 1.5 birim
        world = new FakeWorld();
        hits = new List<CreatureHit>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    CreatureModel Bomb(PlayerState target, Vector2 at)
    {
        var bomb = CreatureFactory.CreateAttack(Real("bomba"), target, Vector2.zero, config, arena);
        bomb.PlaceAt(at);
        return bomb;
    }

    // ---- Bomba (gerçek kelimeden) ----

    [Test]
    public void Bomba_FromRealWords_IsADelayedAreaStrikeWithCountdown()
    {
        var target = world.Add(1, Vector2.zero);
        var bomb = Bomb(target, Vector2.zero);

        Assert.IsTrue(bomb.IsStrike);
        Assert.AreEqual(3f, bomb.StrikeWarningSeconds, 0.001f);  // words.json: warningSeconds 3
        Assert.AreEqual(2.5f, bomb.Radius, 0.001f);              // words.json: radius 2.5
        Assert.AreEqual(1, bomb.Damage);
        Assert.AreEqual(0f, bomb.KnockbackDistance, 0.0001f, "Patlama fırlatmaz");
        Assert.AreEqual(config.explosiveDropDistance, bomb.PlacementOffset, 0.0001f);
        Assert.IsTrue(bomb.ShowsCountdown);
        Assert.IsTrue(bomb.IsHarmful, "Botlar patlama alanından kaçmalı");
    }

    [Test]
    public void Yumruk_FromRealWords_DropsOnTargetWithoutCountdown()
    {
        var target = world.Add(1, Vector2.zero);
        var fist = CreatureFactory.CreateAttack(Real("yumruk"), target, Vector2.zero, config, arena);

        Assert.AreEqual(0f, fist.PlacementOffset, 0.0001f);
        Assert.IsFalse(fist.ShowsCountdown);
    }

    [Test]
    public void Bomb_DoesNothingDuringTheFuse()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var bomb = Bomb(target, new Vector2(1.5f, 0f));

        Assert.AreEqual(0, bomb.TickStrike(2.9f, world, PlayerRadius, hits));

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(bomb.HasStruck);
        Assert.AreEqual(0.1f, bomb.StrikeWarningLeft, 0.001f);
    }

    [Test]
    public void Bomb_Explodes_HurtingEveryoneInsideTheRadius_WithoutKnockback()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var nearby = world.Add(2, new Vector2(2.0f, 1.0f));
        var farAway = world.Add(3, new Vector2(7f, 0f));
        var bomb = Bomb(target, new Vector2(1.5f, 0f));

        int affected = bomb.TickStrike(3f, world, PlayerRadius, hits);

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, target.Health.Current);
        Assert.AreEqual(2, nearby.Health.Current);
        Assert.AreEqual(3, farAway.Health.Current);
        Assert.IsEmpty(hits, "Patlamada fırlatma olmaz");
        Assert.IsTrue(bomb.HasStruck);
        Assert.IsFalse(bomb.IsHarmful, "Patladıktan sonra artık zararlı değil");
    }

    [Test]
    public void Bomb_TargetWhoRanOutOfTheRadiusDuringTheFuse_Survives()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var bomb = Bomb(target, new Vector2(1.5f, 0f)); // Merkez 1.5 uzakta, yarıçap 2.5 (+0.5) = 3.0

        bomb.TickStrike(1.5f, world, PlayerRadius, hits);
        world.Move(1, new Vector2(-3.0f, 0f));             // Merkezden 4.5 uzağa kaçtı
        bomb.TickStrike(1.5f, world, PlayerRadius, hits);

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsTrue(bomb.HasStruck);
    }

    [Test]
    public void Bomb_TargetNeedsToMoveOnlyAFewUnitsToEscape()
    {
        // Oyuncu hızı 5: patlama alanından (merkezden 3.0) çıkmak için ~1.5 birim, yani 0.3 sn yeter. Fitil 3 sn.
        float needed = (config.creatureRadius + 2.5f + PlayerRadius) - config.explosiveDropDistance;
        Assert.Less(needed / config.playerMoveSpeed, 3f - 1f, "Kaçmak için bolca zaman olmalı");
    }

    [Test]
    public void Bomb_ExplosionIsFollowedByALingeringMark_ThenFinishes()
    {
        var target = world.Add(1, Vector2.zero);
        var bomb = Bomb(target, new Vector2(1.5f, 0f));

        bomb.TickStrike(3f, world, PlayerRadius, hits);
        Assert.IsFalse(bomb.IsFinished);

        bomb.TickStrike(config.strikeLingerSeconds + 0.5f, world, PlayerRadius, hits);
        Assert.IsTrue(bomb.IsFinished);
    }

    [Test]
    public void StrikeWarningLeft_CountsDown_AndIsZeroAfterTheBlastOrForNonStrikes()
    {
        var target = world.Add(1, Vector2.zero);
        var bomb = Bomb(target, new Vector2(1.5f, 0f));

        Assert.AreEqual(3f, bomb.StrikeWarningLeft, 0.001f);
        bomb.TickStrike(1f, world, PlayerRadius, hits);
        Assert.AreEqual(2f, bomb.StrikeWarningLeft, 0.001f);

        bomb.TickStrike(2f, world, PlayerRadius, hits);
        Assert.AreEqual(0f, bomb.StrikeWarningLeft, 0.001f);

        var chaser = new CreatureModel(null, target, Vector2.zero, 1f, 1, 0.5f, 5f, arena);
        Assert.AreEqual(0f, chaser.StrikeWarningLeft, 0.001f);
    }

    [Test]
    public void ExplosiveTag_UsesAtLeastTheMinimumFuse_AndExtendsLife()
    {
        var word = new WordEntry
        {
            id = "x", tags = new[] { "explosive" },
            stats = new WordStats { damage = 1, radius = 2f, warningSeconds = 0.1f, duration = 0.2f }
        };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(config.strikeMinWarningSeconds, creature.StrikeWarningSeconds, 0.001f);
        Assert.GreaterOrEqual(creature.LifeLeft, creature.StrikeWarningSeconds + config.strikeLingerSeconds - 0.001f);
    }

    [Test]
    public void ExplosiveTag_WordWithoutStats_DoesNotThrow()
    {
        var word = new WordEntry { id = "x", tags = new[] { "explosive" } };
        var target = world.Add(1, Vector2.zero);

        Assert.DoesNotThrow(() => CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena));
    }

    // ---- ArenaSpawn.PointBeside ----

    [Test]
    public void PointBeside_IsExactlyTheGivenDistanceFromTheCenter()
    {
        var random = new Random(3);
        var center = new Vector2(2f, -1f);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(1.5f, (ArenaSpawn.PointBeside(center, 1.5f, random) - center).magnitude, 0.001f);
    }

    [Test]
    public void PointBeside_CoversDifferentDirections()
    {
        var random = new Random(5);
        bool right = false, left = false, up = false, down = false;

        for (int i = 0; i < 200; i++)
        {
            Vector2 offset = ArenaSpawn.PointBeside(Vector2.zero, 1f, random);
            right |= offset.x > 0.5f;
            left |= offset.x < -0.5f;
            up |= offset.y > 0.5f;
            down |= offset.y < -0.5f;
        }

        Assert.IsTrue(right && left && up && down);
    }

    [Test]
    public void PointBeside_SameSeed_SamePoint_AndZeroDistanceIsTheCenter()
    {
        Assert.AreEqual(ArenaSpawn.PointBeside(Vector2.zero, 2f, new Random(9)), ArenaSpawn.PointBeside(Vector2.zero, 2f, new Random(9)));
        Assert.AreEqual(new Vector2(4f, 4f), ArenaSpawn.PointBeside(new Vector2(4f, 4f), 0f, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => ArenaSpawn.PointBeside(Vector2.zero, 1f, null));
    }
}
