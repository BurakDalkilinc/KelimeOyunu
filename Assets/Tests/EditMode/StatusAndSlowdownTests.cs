using NUnit.Framework;
using UnityEngine;

/// <summary>StatusEffects (yavaşlama etkisi) ve "slowdown" etiketinin testleri.</summary>
public class StatusAndSlowdownTests
{
    const float PlayerRadius = 0.5f;
    const float Cooldown = 1f;

    ArenaBounds arena;
    GameConfig config;
    PlayerState target;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // yavaşlatma çarpanı 0.5, varsayılan süre 3 sn
        target = new PlayerState(1, "Hedef", 3);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    // ---- StatusEffects ----

    [Test]
    public void NewStatus_IsNotSlowed()
    {
        var status = new StatusEffects();
        Assert.IsFalse(status.IsSlowed);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
        Assert.AreEqual(0f, status.SlowTimeLeft, 0.0001f);
    }

    [Test]
    public void ApplySlow_SetsMultiplierAndTime()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);

        Assert.IsTrue(status.IsSlowed);
        Assert.AreEqual(0.5f, status.SpeedMultiplier, 0.0001f);
        Assert.AreEqual(3f, status.SlowTimeLeft, 0.0001f);
    }

    [Test]
    public void Tick_EndsTheSlowWhenTimeRunsOut()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);

        status.Tick(2.9f);
        Assert.IsTrue(status.IsSlowed);

        status.Tick(0.2f);
        Assert.IsFalse(status.IsSlowed);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
        Assert.AreEqual(0f, status.SlowTimeLeft, 0.0001f);
    }

    [Test]
    public void ApplySlow_Twice_DoesNotStack_ButRefreshesTime()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);
        status.Tick(2f);          // 1 sn kaldı
        status.ApplySlow(0.5f, 3f); // Tekrar yavaşlatıldı

        Assert.AreEqual(0.5f, status.SpeedMultiplier, 0.0001f, "Çarpanlar çarpılmaz (0.25 olmaz)");
        Assert.AreEqual(3f, status.SlowTimeLeft, 0.0001f, "Süre tazelenir");
    }

    [Test]
    public void ApplySlow_StrongerWins_WeakerDoesNotWeakenIt()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);
        status.ApplySlow(0.8f, 5f); // Daha hafif ama daha uzun

        Assert.AreEqual(0.5f, status.SpeedMultiplier, 0.0001f, "Güçlü olan korunur");
        Assert.AreEqual(5f, status.SlowTimeLeft, 0.0001f, "Süre uzun olana tazelenir");

        status.ApplySlow(0.25f, 1f);
        Assert.AreEqual(0.25f, status.SpeedMultiplier, 0.0001f, "Daha güçlüsü gelince o geçerli olur");
        Assert.AreEqual(5f, status.SlowTimeLeft, 0.0001f);
    }

    [Test]
    public void ApplySlow_AfterItEnded_StartsFresh()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.25f, 1f);
        status.Tick(2f);

        status.ApplySlow(0.8f, 2f);

        Assert.AreEqual(0.8f, status.SpeedMultiplier, 0.0001f, "Eski güçlü çarpan geri gelmemeli");
    }

    [TestCase(0.5f, 0f)]
    [TestCase(0.5f, -2f)]
    [TestCase(1f, 3f)]
    [TestCase(2f, 3f)]
    public void ApplySlow_NoOpCases_DoNothing(float multiplier, float seconds)
    {
        var status = new StatusEffects();
        status.ApplySlow(multiplier, seconds);
        Assert.IsFalse(status.IsSlowed);
    }

    [Test]
    public void ApplySlow_TooSmallMultiplier_IsClampedSoPlayerIsNeverFrozen()
    {
        var status = new StatusEffects();
        status.ApplySlow(0f, 3f);
        Assert.Greater(status.SpeedMultiplier, 0f);
    }

    [Test]
    public void Tick_NegativeDelta_DoesNothing()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);
        status.Tick(-5f);
        Assert.AreEqual(3f, status.SlowTimeLeft, 0.0001f);
    }

    [Test]
    public void Clear_RemovesEverything()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 3f);
        status.Clear();
        Assert.IsFalse(status.IsSlowed);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void PlayerState_HasItsOwnStatus()
    {
        var a = new PlayerState(1, "A", 3);
        var b = new PlayerState(2, "B", 3);

        a.Status.ApplySlow(0.5f, 3f);

        Assert.IsTrue(a.Status.IsSlowed);
        Assert.IsFalse(b.Status.IsSlowed);
    }

    // ---- slowdown etiketi ----

    [Test]
    public void Fare_FromRealWords_SlowsForTheWordsSlowSeconds_WithoutDamage()
    {
        var fare = CreatureFactory.CreateAttack(Real("fare"), target, Vector2.zero, config, arena);

        Assert.AreEqual(0, fare.Damage);
        Assert.AreEqual(3f, fare.SlowSeconds, 0.001f);                  // words.json: slowSeconds 3
        Assert.AreEqual(config.slowdownSpeedMultiplier, fare.SlowMultiplier, 0.001f);
        Assert.AreEqual(7f, fare.Speed, 0.001f);
        Assert.IsFalse(fare.IsDangerous, "Hasarı yok");
        Assert.IsTrue(fare.IsHarmful, "Ama zararlı: botlar kaçar");
    }

    [Test]
    public void Slowdown_OnContact_SlowsTargetWithoutHurtingAndCreatureFinishes()
    {
        var fare = CreatureFactory.CreateAttack(Real("fare"), target, new Vector2(0f, 0f), config, arena);

        bool hit = fare.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown);

        Assert.IsTrue(hit);
        Assert.AreEqual(3, target.Health.Current);
        Assert.IsTrue(target.Status.IsSlowed);
        Assert.AreEqual(0.5f, target.Status.SpeedMultiplier, 0.001f);
        Assert.AreEqual(3f, target.Status.SlowTimeLeft, 0.001f);
        Assert.IsTrue(fare.IsFinished, "Fare tek vuruşluk");
    }

    [Test]
    public void Slowdown_FarAway_DoesNotSlow()
    {
        var fare = CreatureFactory.CreateAttack(Real("fare"), target, new Vector2(-9f, 0f), config, arena);
        fare.Tick(0.1f, new Vector2(9f, 0f), PlayerRadius, Cooldown);
        Assert.IsFalse(target.Status.IsSlowed);
    }

    [Test]
    public void Slowdown_WordWithoutSlowSeconds_UsesDefaultSeconds()
    {
        var word = new WordEntry { id = "x", tags = new[] { "slowdown" }, stats = new WordStats { speed = 1f, duration = 5f } };
        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(config.slowdownDefaultSeconds, creature.SlowSeconds, 0.001f);
    }

    [Test]
    public void CreatureWithoutSlowdownTag_DoesNotSlow()
    {
        var kurt = CreatureFactory.CreateAttack(Real("kurt"), target, new Vector2(0f, 0f), config, arena);
        kurt.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown);

        Assert.AreEqual(0f, kurt.SlowSeconds, 0.0001f);
        Assert.IsFalse(target.Status.IsSlowed);
    }

    [Test]
    public void Slowdown_AreaStrike_SlowsEveryoneInside()
    {
        // Alan vuruşu (instant) etiketiyle birleşince alandaki herkesi yavaşlatır.
        var word = new WordEntry
        {
            id = "x", tags = new[] { "instant", "slowdown" },
            stats = new WordStats { damage = 0, radius = 2f, warningSeconds = 1f, slowSeconds = 2f, duration = 3f }
        };
        var a = new PlayerState(1, "A", 3);
        var b = new PlayerState(2, "B", 3);
        var outsider = new PlayerState(3, "C", 3);

        var creature = CreatureFactory.CreateAttack(word, a, Vector2.zero, config, arena);
        creature.PlaceAt(Vector2.zero);

        var world = new SimpleWorld();
        world.Add(a, new Vector2(0.5f, 0f));
        world.Add(b, new Vector2(-1f, 0.5f));
        world.Add(outsider, new Vector2(8f, 0f));

        creature.TickStrike(1f, world, PlayerRadius, new System.Collections.Generic.List<CreatureHit>());

        Assert.IsTrue(a.Status.IsSlowed);
        Assert.IsTrue(b.Status.IsSlowed);
        Assert.IsFalse(outsider.Status.IsSlowed);
        Assert.AreEqual(3, a.Health.Current, "Hasar yok");
    }

    [Test]
    public void IsHarmful_FalseWhenFinishedOrTargetless()
    {
        var harmless = new CreatureModel(null, null, Vector2.zero, 1f, 5, 0.5f, 10f, arena);
        Assert.IsFalse(harmless.IsHarmful);

        var finished = new CreatureModel(null, target, Vector2.zero, 1f, 1, 0.5f, 0f, arena);
        Assert.IsFalse(finished.IsHarmful);
    }

    class SimpleWorld : IPlayerLocator
    {
        readonly System.Collections.Generic.List<PlayerState> players = new System.Collections.Generic.List<PlayerState>();
        readonly System.Collections.Generic.Dictionary<int, Vector2> positions = new System.Collections.Generic.Dictionary<int, Vector2>();

        public System.Collections.Generic.IReadOnlyList<PlayerState> Players => players;

        public void Add(PlayerState player, Vector2 position)
        {
            players.Add(player);
            positions[player.Id] = position;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }
}
