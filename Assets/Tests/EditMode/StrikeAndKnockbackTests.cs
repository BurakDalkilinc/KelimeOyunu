using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>"instant" (anında vuran alan), "knockback" (fırlatma) ve KnockbackMotion testleri.</summary>
public class StrikeAndKnockbackTests
{
    /// <summary>Testler için sahte dünya: oyuncular ve konumları elle verilir.</summary>
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

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    const float PlayerRadius = 0.5f;
    const float Cooldown = 1f;

    ArenaBounds arena;
    GameConfig config;
    FakeWorld world;
    List<CreatureHit> hits;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // fırlatma 3 birim / 0.25 sn, en az uyarı 0.5 sn, iz 0.4 sn
        world = new FakeWorld();
        hits = new List<CreatureHit>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    /// <summary>Gerçek "yumruk" kelimesinden vuruş alanı kurar ve verilen noktaya koyar.</summary>
    CreatureModel Fist(PlayerState target, Vector2 at)
    {
        var creature = CreatureFactory.CreateAttack(Real("yumruk"), target, Vector2.zero, config, arena);
        creature.PlaceAt(at);
        return creature;
    }

    // ---- Yumruk (instant + knockback), gerçek kelimeden ----

    [Test]
    public void Fist_FromRealWords_IsAStrikeWithWarningRadiusAndKnockback()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        Assert.IsTrue(fist.IsStrike);
        Assert.AreEqual(1.5f, fist.StrikeWarningSeconds, 0.001f); // words.json: warningSeconds 1.5
        Assert.AreEqual(1.5f, fist.Radius, 0.001f);               // words.json: radius 1.5
        Assert.AreEqual(1, fist.Damage);
        Assert.AreEqual(config.knockbackDistance, fist.KnockbackDistance, 0.001f);
        Assert.GreaterOrEqual(fist.LifeLeft, 1.5f + config.strikeLingerSeconds - 0.001f);
    }

    [Test]
    public void Strike_DoesNothingDuringWarning()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        Assert.AreEqual(0, fist.TickStrike(1.0f, world, PlayerRadius, hits));

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(fist.HasStruck);
        Assert.IsTrue(fist.IsDangerous);
        Assert.AreEqual(1.0f / 1.5f, fist.StrikeProgress, 0.001f);
        Assert.IsEmpty(hits);
    }

    [Test]
    public void Strike_WhenWarningEnds_HitsEveryoneInsideTheArea()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var bystander = world.Add(2, new Vector2(3.8f, 0.5f));
        var fist = Fist(target, new Vector2(3f, 0f));

        int affected = fist.TickStrike(1.5f, world, PlayerRadius, hits);

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, target.Health.Current);
        Assert.AreEqual(2, bystander.Health.Current, "Alandaki başkaları da vurulur");
        Assert.IsTrue(fist.HasStruck);
        Assert.IsFalse(fist.IsDangerous, "Vurduktan sonra artık tehlike değil");
        Assert.AreEqual(1f, fist.StrikeProgress, 0.0001f);
    }

    [Test]
    public void Strike_DoesNotHitPlayersOutsideTheArea()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var far = world.Add(2, new Vector2(8f, 0f));
        var justOutside = world.Add(3, new Vector2(3f + 1.5f + PlayerRadius + 0.2f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        fist.TickStrike(1.5f, world, PlayerRadius, hits);

        Assert.AreEqual(2, target.Health.Current);
        Assert.AreEqual(3, far.Health.Current);
        Assert.AreEqual(3, justOutside.Health.Current);
    }

    [Test]
    public void Strike_PlayerWhoMovedAwayDuringWarning_Dodges()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f)); // Alan, hedefin ilk bulunduğu yere kilitlendi

        fist.TickStrike(1.0f, world, PlayerRadius, hits);
        var moved = new FakeWorld(); // Aynı oyuncu, uyarı sırasında başka yere gitmiş.
        var dodger = moved.Add(1, new Vector2(7f, 0f)); // Hedef uyarı sırasında kaçtı
        fist.TickStrike(0.6f, moved, PlayerRadius, hits);

        Assert.AreEqual(3, dodger.Health.Current);
        Assert.IsTrue(fist.HasStruck);
    }

    [Test]
    public void Strike_SkipsEliminatedPlayers()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var dead = world.Add(2, new Vector2(3f, 0.2f));
        dead.Health.TakeDamage(99);
        var fist = Fist(target, new Vector2(3f, 0f));

        int affected = fist.TickStrike(1.5f, world, PlayerRadius, hits);

        Assert.AreEqual(1, affected);
        Assert.IsFalse(hits.Exists(h => h.Player == dead));
    }

    [Test]
    public void Strike_HitsOnlyOnce()
    {
        var target = world.Add(1, new Vector2(3f, 0f), health: 10);
        var fist = Fist(target, new Vector2(3f, 0f));

        fist.TickStrike(1.5f, world, PlayerRadius, hits);
        int second = fist.TickStrike(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(0, second);
        Assert.AreEqual(9, target.Health.Current);
    }

    [Test]
    public void Strike_FinishesAfterWarningPlusLinger()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        fist.TickStrike(1.5f, world, PlayerRadius, hits);
        Assert.IsFalse(fist.IsFinished, "Darbe izi bir süre kalır");

        fist.TickStrike(config.strikeLingerSeconds + 0.5f, world, PlayerRadius, hits);
        Assert.IsTrue(fist.IsFinished);
    }

    [Test]
    public void Strike_HugeTimeStep_StillStrikes()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        fist.TickStrike(100f, world, PlayerRadius, hits);

        Assert.IsTrue(fist.HasStruck);
        Assert.AreEqual(2, target.Health.Current);
        Assert.IsTrue(fist.IsFinished);
    }

    [Test]
    public void Strike_KnocksPlayersAwayFromTheCenter()
    {
        var right = world.Add(1, new Vector2(4f, 0f));
        var left = world.Add(2, new Vector2(2f, 0f));
        var fist = Fist(right, new Vector2(3f, 0f));

        fist.TickStrike(1.5f, world, PlayerRadius, hits);

        var rightHit = hits.Find(h => h.Player == right);
        var leftHit = hits.Find(h => h.Player == left);
        Assert.AreEqual(config.knockbackDistance, rightHit.Knockback.magnitude, 0.001f);
        Assert.Greater(rightHit.Knockback.x, 0f);  // Sağdaki sağa
        Assert.Less(leftHit.Knockback.x, 0f);      // Soldaki sola
    }

    [Test]
    public void Strike_PlayerExactlyAtCenter_GetsAFiniteKnockback()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        fist.TickStrike(1.5f, world, PlayerRadius, hits);

        Vector2 push = hits[0].Knockback;
        Assert.IsFalse(float.IsNaN(push.x) || float.IsNaN(push.y));
        Assert.AreEqual(config.knockbackDistance, push.magnitude, 0.001f);
    }

    [Test]
    public void StrikeOnlyCreature_IgnoresNormalTick()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        Assert.IsFalse(fist.Tick(5f, new Vector2(3f, 0f), PlayerRadius, Cooldown));
        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(fist.IsFinished);
    }

    [Test]
    public void NormalCreature_TickStrikeDoesNothing()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var creature = new CreatureModel(null, target, new Vector2(3f, 0f), 1f, 1, 0.5f, 10f, arena);

        Assert.AreEqual(0, creature.TickStrike(5f, world, PlayerRadius, hits));
        Assert.AreEqual(3, target.Health.Current);
    }

    [Test]
    public void Strike_NullWorld_DoesNotThrow()
    {
        var target = world.Add(1, new Vector2(3f, 0f));
        var fist = Fist(target, new Vector2(3f, 0f));

        Assert.DoesNotThrow(() => fist.TickStrike(2f, null, PlayerRadius, hits));
    }

    [Test]
    public void InstantTag_UsesAtLeastTheConfiguredMinimumWarning()
    {
        var word = new WordEntry { id = "x", tags = new[] { "instant" }, stats = new WordStats { damage = 1, radius = 2f, warningSeconds = 0.1f } };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(config.strikeMinWarningSeconds, creature.StrikeWarningSeconds, 0.001f);
    }

    [Test]
    public void InstantTag_LongWarningExtendsLifeSoTheStrikeStillHappens()
    {
        var word = new WordEntry { id = "x", tags = new[] { "instant" }, stats = new WordStats { damage = 1, radius = 2f, warningSeconds = 6f, duration = 1f } };
        var target = world.Add(1, Vector2.zero);

        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);
        creature.TickStrike(6f, world, PlayerRadius, hits);

        Assert.IsTrue(creature.HasStruck);
        Assert.AreEqual(2, target.Health.Current);
    }

    // ---- knockback (temaslı yaratık) ----

    [Test]
    public void Knockback_CreatureWithoutDamage_StillPushesOnContact()
    {
        var target = new PlayerState(1, "H", 3);
        var banana = new CreatureModel(null, target, new Vector2(2f, 0f), 0f, 0, 0.5f, 10f, arena) { KnockbackDistance = 3f };

        bool hit = banana.Tick(0.1f, new Vector2(2.6f, 0f), PlayerRadius, Cooldown); // Üstüne bastı

        Assert.IsTrue(hit);
        Assert.AreEqual(3, target.Health.Current, "Hasar yok");
        Assert.AreEqual(target, banana.LastHit.Player);
        Assert.AreEqual(3f, banana.LastHit.Knockback.magnitude, 0.001f);
        Assert.Greater(banana.LastHit.Knockback.x, 0f); // Hedef sağda, yani sağa fırlar
    }

    [Test]
    public void Knockback_CreatureWithNeitherDamageNorKnockback_NeverHits()
    {
        var target = new PlayerState(1, "H", 3);
        var cloud = new CreatureModel(null, target, new Vector2(2f, 0f), 0f, 0, 0.5f, 10f, arena);

        Assert.IsFalse(cloud.Tick(0.1f, new Vector2(2.2f, 0f), PlayerRadius, Cooldown));
    }

    [Test]
    public void Knockback_ChasingCreatureWithDamage_DamagesAndPushes()
    {
        var target = new PlayerState(1, "H", 3);
        var creature = new CreatureModel(null, target, new Vector2(0f, 0f), 100f, 1, 0.5f, 10f, arena) { KnockbackDistance = 2f };

        Assert.IsTrue(creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown));

        Assert.AreEqual(2, target.Health.Current);
        Assert.AreEqual(2f, creature.LastHit.Knockback.magnitude, 0.001f);
    }

    [Test]
    public void Knockback_WithoutTag_LastHitHasNoPush()
    {
        var target = new PlayerState(1, "H", 3);
        var creature = new CreatureModel(null, target, new Vector2(0f, 0f), 100f, 1, 0.5f, 10f, arena);

        creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown);

        Assert.AreEqual(Vector2.zero, creature.LastHit.Knockback);
    }

    [Test]
    public void MuzKabugu_FromRealWords_KnocksBackWithoutDamage()
    {
        var target = new PlayerState(1, "H", 3);
        var banana = CreatureFactory.CreateAttack(Real("muz_kabugu"), target, new Vector2(2f, 0f), config, arena);

        Assert.AreEqual(0, banana.Damage);
        Assert.AreEqual(config.knockbackDistance, banana.KnockbackDistance, 0.001f);
        Assert.AreEqual(0f, banana.Speed, 0.001f);
        Assert.IsFalse(banana.IsDangerous);
    }

    // ---- KnockbackMotion ----

    [Test]
    public void Motion_SumsToTheFullDistance()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(3f, 0f), 0.25f);

        Vector2 sum = Vector2.zero;
        for (int i = 0; i < 100; i++) sum += motion.Step(0.01f);

        Assert.AreEqual(3f, sum.x, 0.001f);
        Assert.AreEqual(0f, sum.y, 0.001f);
        Assert.IsFalse(motion.IsActive);
    }

    [Test]
    public void Motion_HugeStep_DeliversFullDistanceOnce()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(0f, -4f), 0.25f);

        Vector2 first = motion.Step(10f);
        Vector2 second = motion.Step(10f);

        Assert.AreEqual(-4f, first.y, 0.001f);
        Assert.AreEqual(Vector2.zero, second);
    }

    [Test]
    public void Motion_StartsFastAndSlowsDown()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(4f, 0f), 0.4f);

        float early = motion.Step(0.1f).x;
        motion.Step(0.1f);
        motion.Step(0.1f);
        float late = motion.Step(0.1f).x;

        Assert.Greater(early, late);
    }

    [Test]
    public void Motion_IdleOrZeroDistance_ReturnsZero()
    {
        var motion = new KnockbackMotion();
        Assert.IsFalse(motion.IsActive);
        Assert.AreEqual(Vector2.zero, motion.Step(1f));

        motion.Start(Vector2.zero, 0.25f);
        Assert.IsFalse(motion.IsActive);
        Assert.AreEqual(Vector2.zero, motion.Step(1f));
    }

    [Test]
    public void Motion_NewKnockback_ReplacesTheOldOne()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(3f, 0f), 0.25f);
        motion.Step(0.05f);

        motion.Start(new Vector2(0f, 2f), 0.25f);
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < 40; i++) sum += motion.Step(0.01f);

        Assert.AreEqual(0f, sum.x, 0.001f);
        Assert.AreEqual(2f, sum.y, 0.001f);
    }

    [Test]
    public void Motion_NegativeDelta_DoesNotMoveBackward()
    {
        var motion = new KnockbackMotion();
        motion.Start(new Vector2(3f, 0f), 0.25f);
        Assert.AreEqual(Vector2.zero, motion.Step(-1f));
    }
}
