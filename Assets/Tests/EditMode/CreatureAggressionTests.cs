using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Yaratık tehlikesi kuralları: hız çarpanı ve "slow" sınırları, etiket çarpanları, ömür çarpanı, atılma (menzil, hazırlık,
/// hız, bekleme), vuruşta sendeleme, vuramadığı hedeften dönme, botların tehlike alanı ve ağ fotoğrafındaki bayraklar.
/// </summary>
public class CreatureAggressionTests
{
    const float Dt = 1f / 60f;

    class FakeWorld : ICreatureWorld
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position, int health = 10)
        {
            var player = new PlayerState(id, "P" + id, health);
            players.Add(player);
            Positions[id] = position;
            return player;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);
        public bool TryApplyKnockback(int playerId, Vector2 distance) => false;
    }

    /// <summary>Sağa doğru duran, kalınlığı 1 olan dikey duvar (x = wallX).</summary>
    class WallAt : IObstacleField
    {
        readonly float wallX;
        public WallAt(float x) { wallX = x; }

        public Vector2 Resolve(Vector2 position, float radius)
        {
            float limit = wallX - radius;
            return position.x > limit ? new Vector2(limit, position.y) : position;
        }
    }

    GameConfig config;
    ArenaBounds arena;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        arena = new ArenaBounds(config.arenaSize);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(config);

    static WordEntry Word(float speed, float duration, params string[] tags)
    {
        return new WordEntry
        {
            id = "deneme", display = "Deneme", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget, tags = tags,
            stats = new WordStats { damage = 1, speed = speed, duration = duration },
        };
    }

    CreatureModel Build(WordEntry word, PlayerState target, Vector2 targetAt, Vector2 creatureAt, FakeWorld world)
    {
        List<CreatureModel> models = CreatureBuilder.BuildAttack(word, target, targetAt, config, arena, new System.Random(1));
        CreatureModel model = models[0];
        model.PlaceAt(creatureAt);
        model.Locator = world;
        return model;
    }

    // ---- Hız ----

    [Test]
    public void Speed_IsMultiplied()
    {
        config.creatureSpeedMultiplier = 1.25f;
        Assert.AreEqual(4f * 1.25f, CreatureAggression.SpeedFor(4f, Word(4f, 8f), config), 0.001f);
    }

    [Test]
    public void SlowTag_HasFloorAndStaysSlowerThanPlayer()
    {
        config.playerMoveSpeed = 5f;
        config.creatureSlowMinSpeedFraction = 0.55f;
        config.creatureSlowMaxSpeedFraction = 0.9f;

        Assert.AreEqual(5f * 0.55f, CreatureAggression.SpeedFor(0.8f, Word(0.8f, 8f, "slow"), config), 0.001f, "alt sınır");
        Assert.AreEqual(5f * 0.9f, CreatureAggression.SpeedFor(6f, Word(6f, 8f, "slow"), config), 0.001f, "oyuncudan yavaş kalır");
        Assert.Less(CreatureAggression.SpeedFor(3.5f, Word(3.5f, 8f, "slow"), config), config.playerMoveSpeed);
    }

    [Test]
    public void SpeedCap_LimitsBoostButNeverSlowsBelowWordSpeed()
    {
        config.playerMoveSpeed = 5f;
        config.creatureSpeedMultiplier = 1.25f;
        config.creatureMaxSpeedFraction = 1.4f;
        config.creatureTagTunings = new CreatureTagTuning[0];

        Assert.AreEqual(7f, CreatureAggression.SpeedFor(6f, Word(6f, 8f), config), 0.001f, "6×1.25 = 7.5 → sınır 7");
        Assert.AreEqual(8f, CreatureAggression.SpeedFor(8f, Word(8f, 8f), config), 0.001f, "kelimedeki 8 korunur");
    }

    [Test]
    public void TagTuning_MultipliesSpeedPounceAndDuration()
    {
        config.creatureSpeedMultiplier = 1f;
        config.creatureMaxSpeedFraction = 0f;
        config.creaturePounceRange = 2f;
        config.creatureDurationMultiplier = 1f;
        config.creatureTagTunings = new[]
        {
            new CreatureTagTuning { tag = "fast", pounceRangeMultiplier = 1.5f, pounceSpeedMultiplier = 1.2f, speedMultiplier = 0.5f, durationMultiplier = 2f },
            new CreatureTagTuning { tag = "swarm", pounceRangeMultiplier = 0f },
        };

        WordEntry fast = Word(4f, 8f, "fast");
        Assert.AreEqual(2f, CreatureAggression.SpeedFor(4f, fast, config), 0.001f);
        Assert.AreEqual(3f, CreatureAggression.PounceRangeFor(fast, config), 0.001f);
        Assert.AreEqual(config.creaturePounceSpeedMultiplier * 1.2f, CreatureAggression.PounceSpeedFor(fast, config), 0.001f);
        Assert.AreEqual(2f, CreatureAggression.DurationMultiplierFor(fast, config), 0.001f);
        Assert.AreEqual(0f, CreatureAggression.PounceRangeFor(Word(4f, 8f, "fast", "swarm"), config), "0 çarpan atılmayı kapatır");
    }

    // ---- Ömür ----

    [Test]
    public void Duration_IsMultiplied_ForChasersOnly()
    {
        config.creatureDurationMultiplier = 1.4f;
        config.creatureTagTunings = new CreatureTagTuning[0];
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(10f, 0f));

        CreatureModel chaser = Build(Word(3f, 10f), target, new Vector2(10f, 0f), Vector2.zero, world);
        Assert.AreEqual(14f, chaser.LifeLeft, 0.001f);

        WordEntry punch = Word(0f, 10f, "instant");
        CreatureModel strike = CreatureBuilder.BuildAttack(punch, target, new Vector2(10f, 0f), config, arena, new System.Random(1))[0];
        Assert.IsTrue(strike.IsStrike);
        Assert.AreEqual(0f, strike.PounceRange, "yumruk atılmaz");
        Assert.LessOrEqual(strike.LifeLeft, 10f, "yumruğun ömrü uzamaz");
    }

    // ---- Atılma ----

    [Test]
    public void Pounce_WindupStops_ThenLungesFaster_ThenCoolsDown()
    {
        config.creatureSpeedMultiplier = 1f;
        config.creatureTagTunings = new CreatureTagTuning[0];
        config.creaturePounceRange = 2.5f;
        config.creaturePounceWindupSeconds = 0.2f;
        config.creaturePounceSeconds = 0.35f;
        config.creaturePounceSpeedMultiplier = 2f;
        config.creaturePounceCooldownSeconds = 2.5f;
        config.creaturePounceOvershoot = 0.6f;

        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(2.8f, 0f)); // Kenardan 2.3: menzilde
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(2.8f, 0f), Vector2.zero, world);
        float r = config.playerRadius;

        wolf.Tick(Dt, new Vector2(2.8f, 0f), r, 1f);
        Assert.AreEqual(PounceStage.Windup, wolf.Pounce, "menzilde hazırlık başlar");
        Assert.IsTrue(wolf.IsPounceWindup);

        Vector2 before = wolf.Position;
        for (int i = 0; i < 10; i++) wolf.Tick(Dt, new Vector2(2.8f, 0f), r, 1f); // 0.17 sn
        Assert.AreEqual(before, wolf.Position, "hazırlıkta durur");

        for (int i = 0; i < 4; i++) wolf.Tick(Dt, new Vector2(2.8f, 0f), r, 1f);
        Assert.AreEqual(PounceStage.Lunge, wolf.Pounce, "hazırlık bitince atılır");

        Vector2 start = wolf.Position;
        wolf.Tick(Dt, new Vector2(2.8f, 0f), r, 1f);
        Assert.AreEqual(3f * 2f * Dt, (wolf.Position - start).magnitude, 0.001f, "atılma hızı = hız × çarpan");
    }

    [Test]
    public void Pounce_DodgedSideways_EndsAndWaitsForCooldown()
    {
        config.creatureSpeedMultiplier = 1f;
        config.creatureTagTunings = new CreatureTagTuning[0];
        config.creaturePounceRange = 2.5f;
        config.creaturePounceWindupSeconds = 0.1f;
        config.creaturePounceSeconds = 0.3f;
        config.creaturePounceCooldownSeconds = 2f;

        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(2.5f, 0f));
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(2.5f, 0f), Vector2.zero, world);
        float r = config.playerRadius;

        for (int i = 0; i < 8; i++) wolf.Tick(Dt, new Vector2(2.5f, 0f), r, 1f); // Hazırlık → atılma
        Assert.AreEqual(PounceStage.Lunge, wolf.Pounce);

        // Hedef yana kaçtı: atılma kilitli yönde gider, vurmaz, süre dolunca biter.
        Vector2 dodged = new Vector2(2.5f, 3f);
        world.Positions[1] = dodged;
        for (int i = 0; i < 30; i++) wolf.Tick(Dt, dodged, r, 1f);
        Assert.AreEqual(10, target.Health.Current, "yana kaçan kurtulur");
        Assert.AreEqual(PounceStage.None, wolf.Pounce, "atılma biter");

        // Hedef yine menzilde ama bekleme sürüyor: hemen tekrar atılmaz.
        Vector2 near = wolf.Position + new Vector2(2f, 0f);
        world.Positions[1] = near;
        wolf.Tick(Dt, near, r, 1f);
        Assert.AreEqual(PounceStage.None, wolf.Pounce, "bekleme bitmeden atılmaz");
    }

    [Test]
    public void Pounce_FarTarget_NoWindup()
    {
        config.creaturePounceRange = 2.5f;
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(8f, 0f));
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(8f, 0f), Vector2.zero, world);

        wolf.Tick(Dt, new Vector2(8f, 0f), config.playerRadius, 1f);
        Assert.AreEqual(PounceStage.None, wolf.Pounce);
        Assert.Greater(wolf.Position.x, 0f, "kovalar");
    }

    [Test]
    public void Pounce_Disabled_WhenRangeZero()
    {
        config.creaturePounceRange = 0f;
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(2f, 0f));
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(2f, 0f), Vector2.zero, world);

        wolf.Tick(Dt, new Vector2(2f, 0f), config.playerRadius, 1f);
        Assert.AreEqual(PounceStage.None, wolf.Pounce);
    }

    // ---- Sendeleme ----

    [Test]
    public void Hit_StaggersTarget()
    {
        config.creatureHitSlowMultiplier = 0.5f;
        config.creatureHitSlowSeconds = 0.6f;
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(0.5f, 0f));
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(0.5f, 0f), Vector2.zero, world);

        Assert.IsTrue(wolf.Tick(Dt, new Vector2(0.5f, 0f), config.playerRadius, 1f));
        Assert.AreEqual(9, target.Health.Current);
        Assert.AreEqual(0.5f, target.Status.SpeedMultiplier, 0.001f, "sendeler");

        target.Status.Tick(0.7f);
        Assert.AreEqual(1f, target.Status.SpeedMultiplier, 0.001f, "süre bitince geçer");
    }

    [Test]
    public void ShieldBlockedHit_DoesNotStagger()
    {
        config.creatureHitSlowSeconds = 0.6f;
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(0.5f, 0f));
        target.Status.ApplyShield(1, 30f);
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(0.5f, 0f), Vector2.zero, world);

        wolf.Tick(Dt, new Vector2(0.5f, 0f), config.playerRadius, 1f);
        Assert.AreEqual(10, target.Health.Current, "kalkan tuttu");
        Assert.AreEqual(1f, target.Status.SpeedMultiplier, 0.001f, "kalkan varken sendelemez");
    }

    // ---- Hedef değiştirme ----

    [Test]
    public void FlyingTarget_AfterRetargetSeconds_SwitchesToHittablePlayer()
    {
        config.creatureRetargetSeconds = 1.5f;
        config.creaturePounceRange = 0f;
        var world = new FakeWorld();
        PlayerState flyer = world.Add(1, new Vector2(3f, 0f));
        PlayerState walker = world.Add(2, new Vector2(-8f, 0f));
        CreatureModel wolf = Build(Word(0.5f, 30f), flyer, new Vector2(3f, 0f), Vector2.zero, world);
        flyer.Status.ApplyFlight(10f, 1f);

        for (int i = 0; i < 60; i++) wolf.Tick(Dt, world.Positions[wolf.Target.Id], config.playerRadius, 1f); // 1 sn
        Assert.AreSame(flyer, wolf.Target, "hemen bırakmaz");

        for (int i = 0; i < 40; i++) wolf.Tick(Dt, world.Positions[wolf.Target.Id], config.playerRadius, 1f); // 1.67 sn
        Assert.AreSame(walker, wolf.Target, "vurulabilir oyuncuya döner");

        for (int i = 0; i < 60; i++) wolf.Tick(Dt, world.Positions[wolf.Target.Id], config.playerRadius, 1f);
        Assert.AreSame(walker, wolf.Target, "uçana geri dönüp gidip gelmez");
    }

    [Test]
    public void StealthedTarget_IsDropped()
    {
        config.creatureRetargetSeconds = 1f;
        var world = new FakeWorld();
        PlayerState hidden = world.Add(1, new Vector2(6f, 0f));
        PlayerState other = world.Add(2, new Vector2(-6f, 0f));
        CreatureModel wolf = Build(Word(0.5f, 30f), hidden, new Vector2(6f, 0f), Vector2.zero, world);
        hidden.Status.ApplyStealth(10f);

        for (int i = 0; i < 70; i++) wolf.Tick(Dt, world.Positions[wolf.Target.Id], config.playerRadius, 1f);
        Assert.AreSame(other, wolf.Target);
    }

    [Test]
    public void BlockedByWall_SwitchesTarget()
    {
        config.creatureRetargetSeconds = 1.5f;
        config.creaturePounceRange = 0f;
        var world = new FakeWorld();
        PlayerState behindWall = world.Add(1, new Vector2(4f, 0f));
        PlayerState open = world.Add(2, new Vector2(-7f, 0f));
        CreatureModel wolf = Build(Word(3f, 30f), behindWall, new Vector2(4f, 0f), Vector2.zero, world);
        wolf.Obstacles = new WallAt(2f);

        for (int i = 0; i < 150; i++) wolf.Tick(Dt, world.Positions[wolf.Target.Id], config.playerRadius, 1f);
        Assert.AreSame(open, wolf.Target, "duvarın önünde beklemez");
    }

    [Test]
    public void Unreachable_NoOneElse_KeepsTarget()
    {
        config.creatureRetargetSeconds = 0.5f;
        var world = new FakeWorld();
        PlayerState flyer = world.Add(1, new Vector2(3f, 0f));
        CreatureModel wolf = Build(Word(1f, 30f), flyer, new Vector2(3f, 0f), Vector2.zero, world);
        flyer.Status.ApplyFlight(10f, 1f);

        for (int i = 0; i < 120; i++) wolf.Tick(Dt, world.Positions[1], config.playerRadius, 1f);
        Assert.AreSame(flyer, wolf.Target);
        Assert.IsFalse(wolf.IsFinished);
    }

    // ---- Botlar ve ağ ----

    [Test]
    public void DangerZone_IncludesPartOfPounceRange_FullDuringWindup()
    {
        config.creaturePounceRange = 2f;
        config.creaturePounceWindupSeconds = 1f;
        config.botPounceAwareness = 0.5f;
        config.creatureTagTunings = new CreatureTagTuning[0];
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(10f, 0f));
        CreatureModel wolf = Build(Word(3f, 20f), target, new Vector2(10f, 0f), Vector2.zero, world);

        Assert.AreEqual(wolf.Radius + 1f, CreatureAggression.DangerZoneFor(wolf, config).Radius, 0.001f);

        world.Positions[1] = new Vector2(2f, 0f);
        wolf.Tick(Dt, new Vector2(2f, 0f), config.playerRadius, 1f);
        Assert.IsTrue(wolf.IsPounceWindup);
        Assert.AreEqual(wolf.Radius + 2f, CreatureAggression.DangerZoneFor(wolf, config).Radius, 0.001f, "kırmızı yanandan tam kaçar");
    }

    [Test]
    public void HordeCreatures_AreNotAffected()
    {
        // Global tehditler (zombi sürüsü, meteor) CreatureFactory ile kurulur, bu kurallar onlara uygulanmaz.
        var world = new FakeWorld();
        PlayerState target = world.Add(1, new Vector2(5f, 0f));
        WordEntry zombie = Word(2f, 10f, "undead", "slow");
        CreatureModel member = CreatureFactory.CreateAttack(zombie, target, Vector2.zero, config, arena);
        Assert.AreEqual(2f, member.Speed, 0.001f);
        Assert.AreEqual(0f, member.PounceRange);
        Assert.AreEqual(0f, member.RetargetUnreachableSeconds);
        Assert.AreEqual(10f, member.LifeLeft, 0.001f);
    }

    [Test]
    public void SnapshotCodec_CarriesPounceFlagsAndTarget()
    {
        var list = new List<CreatureSnapshot>
        {
            new CreatureSnapshot
            {
                Id = 4, WordId = "kurt", X = 1f, Y = 2f, Radius = 0.5f, Label = "Kurt", Attacks = 2,
                Flags = CreatureSnapshot.FlagPounceWindup | CreatureSnapshot.FlagHarmful, TargetId = 7,
            },
        };
        Assert.IsTrue(CreatureSnapshotCodec.TryDecode(CreatureSnapshotCodec.Encode(list), out List<CreatureSnapshot> back));
        Assert.IsTrue(back[0].IsPounceWindup);
        Assert.IsTrue(back[0].IsHarmful);
        Assert.AreEqual(7, back[0].TargetId);
    }
}
