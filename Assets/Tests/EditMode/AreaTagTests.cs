using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>"area" etiketinin testleri: Bulut gibi sürekli alan yaratıkları.</summary>
public class AreaTagTests
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

        public void Move(int id, Vector2 position) => positions[id] = position;

        public void Forget(int id) => positions.Remove(id);

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    const float PlayerRadius = 0.5f;

    ArenaBounds arena;
    GameConfig config;
    FakeWorld world;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // yavaşlatma ×0.5, alandan çıkınca 0.6 sn kalır
        world = new FakeWorld();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    CreatureModel Cloud(PlayerState target, Vector2 at)
    {
        return CreatureFactory.CreateAttack(Real("bulut"), target, at, config, arena);
    }

    // ---- Bulut (gerçek kelimeden) ----

    [Test]
    public void Bulut_FromRealWords_IsAContinuousAreaThatSlows()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(-6f, 0f));

        Assert.IsTrue(cloud.IsArea);
        Assert.IsFalse(cloud.IsStrike);
        Assert.AreEqual(2f, cloud.Radius, 0.001f);   // words.json: radius 2.0
        Assert.AreEqual(3f, cloud.Speed, 0.001f);    // words.json: speed 3.0
        Assert.AreEqual(0, cloud.Damage);
        Assert.Greater(cloud.SlowSeconds, 0f);
        Assert.AreEqual(config.areaLingerSeconds, cloud.AreaLingerSeconds, 0.0001f);
        Assert.IsTrue(cloud.IsHarmful, "Botlar bulutun alanından kaçmalı");
        Assert.IsFalse(cloud.IsDangerous, "Hasarı yok");
    }

    [Test]
    public void Cloud_MovesTowardItsTargetAtItsSpeed_AndStopsOnTop()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(-6f, 0f));

        cloud.TickArea(1f, world, PlayerRadius);
        Assert.AreEqual(-3f, cloud.Position.x, 0.001f);

        for (int i = 0; i < 100; i++) cloud.TickArea(0.1f, world, PlayerRadius);
        Assert.AreEqual(0f, cloud.Position.x, 0.2f, "Hedefin üstünde durur");
    }

    [Test]
    public void Cloud_SlowsEveryoneInside_NotOnlyTheTarget()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var bystander = world.Add(2, new Vector2(1.5f, 0.5f));
        var outsider = world.Add(3, new Vector2(6f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));

        int inside = cloud.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(2, inside);
        Assert.IsTrue(target.Status.IsSlowed);
        Assert.IsTrue(bystander.Status.IsSlowed);
        Assert.IsFalse(outsider.Status.IsSlowed);
        Assert.AreEqual(config.slowdownSpeedMultiplier, target.Status.SpeedMultiplier, 0.001f);
    }

    [Test]
    public void Cloud_DoesNotHurt()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));

        for (int i = 0; i < 50; i++) cloud.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(3, target.Health.Current);
    }

    [Test]
    public void Cloud_SlowEndsShortlyAfterLeavingTheArea()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));
        cloud.TickArea(0.1f, world, PlayerRadius);
        Assert.IsTrue(target.Status.IsSlowed);

        // Oyuncu alandan çıkar; bulut onu yavaşça kovalar ama yetişemez (oyuncu bu testte sabit uzakta).
        world.Move(1, new Vector2(8f, 0f));
        cloud.TickArea(0.1f, world, PlayerRadius); // Bulut biraz yaklaştı, hâlâ uzakta
        for (int i = 0; i < 8; i++) target.Status.Tick(0.1f); // 0.8 sn geçti (> 0.6 sn kalıntı)

        Assert.IsFalse(target.Status.IsSlowed, "Alandan çıkan kısa sürede kurtulmalı");
    }

    [Test]
    public void Cloud_KeepsRefreshingSlowWhileInside()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));

        for (int i = 0; i < 30; i++) // 3 sn içeride kal
        {
            cloud.TickArea(0.1f, world, PlayerRadius);
            target.Status.Tick(0.1f);
        }

        Assert.IsTrue(target.Status.IsSlowed);
        Assert.Greater(target.Status.SlowTimeLeft, 0.4f);
    }

    [Test]
    public void Cloud_SkipsEliminatedPlayers()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var dead = world.Add(2, new Vector2(0.5f, 0f));
        dead.Health.TakeDamage(99);
        var cloud = Cloud(target, new Vector2(0f, 0f));

        int inside = cloud.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(1, inside);
        Assert.IsFalse(dead.Status.IsSlowed);
    }

    [Test]
    public void Cloud_FinishesWhenTargetIsEliminated()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));
        target.Health.TakeDamage(99);

        cloud.TickArea(0.1f, world, PlayerRadius);

        Assert.IsTrue(cloud.IsFinished);
    }

    [Test]
    public void Cloud_FinishesWhenLifeEnds()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f)); // Ömür 10 sn

        cloud.TickArea(9.9f, world, PlayerRadius);
        Assert.IsFalse(cloud.IsFinished);

        cloud.TickArea(0.2f, world, PlayerRadius);
        Assert.IsTrue(cloud.IsFinished);
    }

    [Test]
    public void Cloud_TargetPositionUnknown_StaysPutButStillAffectsPlayersInside()
    {
        var target = world.Add(1, new Vector2(5f, 0f));
        var near = world.Add(2, new Vector2(-6f, 0f));
        var cloud = Cloud(target, new Vector2(-6f, 0f));
        world.Forget(1);

        cloud.TickArea(1f, world, PlayerRadius);

        Assert.AreEqual(-6f, cloud.Position.x, 0.001f);
        Assert.IsTrue(near.Status.IsSlowed);
    }

    [Test]
    public void Cloud_NullWorld_DoesNotThrow()
    {
        var target = world.Add(1, Vector2.zero);
        var cloud = Cloud(target, Vector2.zero);
        Assert.DoesNotThrow(() => cloud.TickArea(0.1f, null, PlayerRadius));
    }

    // ---- Genel kurallar ----

    [Test]
    public void AreaCreature_IgnoresNormalTick()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var cloud = Cloud(target, new Vector2(0f, 0f));

        Assert.IsFalse(cloud.Tick(1f, Vector2.zero, PlayerRadius, 1f));
        Assert.IsFalse(target.Status.IsSlowed);
    }

    [Test]
    public void NonAreaCreature_TickAreaDoesNothing()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var fare = CreatureFactory.CreateAttack(Real("fare"), target, new Vector2(0f, 0f), config, arena);

        Assert.AreEqual(0, fare.TickArea(1f, world, PlayerRadius));
        Assert.IsFalse(target.Status.IsSlowed);
    }

    [TestCase("instant", "area")]
    [TestCase("area", "instant")]
    public void AreaWithInstant_IsAStrikeNotAContinuousZone_RegardlessOfTagOrder(string first, string second)
    {
        var word = new WordEntry
        {
            id = "x", tags = new[] { first, second },
            stats = new WordStats { damage = 1, radius = 2f, warningSeconds = 1f, duration = 3f }
        };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.IsTrue(creature.IsStrike);
        Assert.IsFalse(creature.IsArea);
    }

    [Test]
    public void Bomba_FromRealWords_IsAStrikeNotAContinuousZone()
    {
        // Bomba "explosive" + "area" etiketli. Alan vuruşu zaten alan etkisi olduğu için sürekli alana dönüşmez.
        var target = world.Add(1, Vector2.zero);
        var bomba = CreatureFactory.CreateAttack(Real("bomba"), target, Vector2.zero, config, arena);

        Assert.IsTrue(bomba.AreaRequested);
        Assert.IsTrue(bomba.IsStrike);
        Assert.IsFalse(bomba.IsArea);
        Assert.AreEqual(2.5f, bomba.Radius, 0.001f);
        Assert.AreEqual(1, bomba.Damage);
    }

    [Test]
    public void AreaWithoutAnyAreaEffect_IsNotAZone()
    {
        var word = new WordEntry { id = "x", tags = new[] { "area" }, stats = new WordStats { damage = 1, radius = 2f, duration = 5f } };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.IsFalse(creature.IsArea);
    }

    [TestCase("area", "slowdown")]
    [TestCase("slowdown", "area")]
    public void AreaWithSlowdown_IsAZone_RegardlessOfTagOrder(string first, string second)
    {
        var word = new WordEntry { id = "x", tags = new[] { first, second }, stats = new WordStats { radius = 2f, speed = 1f, duration = 5f } };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.IsTrue(creature.IsArea);
    }

    [Test]
    public void AreaTag_WordWithoutRadius_KeepsDefaultRadius()
    {
        var word = new WordEntry { id = "x", tags = new[] { "area", "slowdown" }, stats = new WordStats { speed = 1f, duration = 5f } };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(config.creatureRadius, creature.Radius, 0.001f);
    }
}
