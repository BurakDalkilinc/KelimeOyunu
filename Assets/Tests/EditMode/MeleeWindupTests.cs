using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Yakın dövüş silahının (melee) hazırlık ve bekleme kuralları: savuruş önce parlar, süre dolunca vurur; hazırlıkta yaydan çıkan
/// kurtulur; iki savuruş arasında bekleme vardır; Kılıç 2 hasar verir, 3 canlı oyuncu iki vuruşta elenir.
/// </summary>
public class MeleeWindupTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position)
        {
            var player = new PlayerState(id, "P" + id, 3);
            players.Add(player);
            Positions[id] = position;
            return player;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);
    }

    GameConfig config;
    WordEntry sword;
    readonly List<PlayerState> victims = new List<PlayerState>();

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        sword = WordDatabase.LoadFromResources(50).FindById("kilic");
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    PlayerState Armed(FakeWorld world)
    {
        PlayerState me = world.Add(1, Vector2.zero);
        TagRegistry.ApplySelfEffects(sword, new SelfEffectContext(me, sword, config, Vector2.zero, Vector2.right, null, null));
        Assert.IsTrue(me.Status.HasWeapon);
        return me;
    }

    [Test]
    public void Sword_FromRealWords_Has2Damage_ShortReach_TwoSwings_10Seconds()
    {
        Assert.IsTrue(MeleeRules.IsMelee(sword));
        Assert.AreEqual(2, MeleeRules.Damage(sword));
        Assert.AreEqual(1.3f, MeleeRules.Reach(sword, config), 0.001f);
        Assert.AreEqual(2, sword.stats.count);
        Assert.AreEqual(10f, sword.stats.duration, 0.001f);
        Assert.Less(MeleeRules.Damage(sword), config.startingHealth, "Tek vuruş eleyemez");
    }

    [Test]
    public void Swing_HitsOnlyAfterTheWindup()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));

        Assert.IsTrue(MeleeRules.TryBeginSwing(me, Vector2.right, config));
        Assert.IsTrue(me.Status.Swing.IsWindingUp);
        Assert.IsFalse(MeleeRules.TickSwing(me, config.swordWindupSeconds * 0.5f, world, config, victims));
        Assert.AreEqual(3, front.Health.Current, "Hazırlıkta vurmaz");

        Assert.IsTrue(MeleeRules.TickSwing(me, config.swordWindupSeconds, world, config, victims));
        Assert.AreEqual(1, front.Health.Current, "Hazırlık bitince 2 hasar");
        CollectionAssert.Contains(victims, front);
    }

    [Test]
    public void TargetThatLeavesTheArc_DuringTheWindup_Escapes()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));

        MeleeRules.TryBeginSwing(me, Vector2.right, config);
        world.Positions[front.Id] = new Vector2(4f, 0f); // Kaçtı
        MeleeRules.TickSwing(me, config.swordWindupSeconds + 0.01f, world, config, victims);

        Assert.AreEqual(3, front.Health.Current);
    }

    [Test]
    public void SecondSwing_WaitsForTheCooldown_AndTwoHitsEliminateAThreeHealthPlayer()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));

        MeleeRules.TryBeginSwing(me, Vector2.right, config);
        Assert.IsFalse(MeleeRules.TryBeginSwing(me, Vector2.right, config), "Hazırlık sürerken yeniden savrulmaz");
        MeleeRules.TickSwing(me, config.swordWindupSeconds + 0.01f, world, config, victims);

        me.Status.Tick(config.swordWindupSeconds + config.swordSwingCooldownSeconds * 0.5f);
        Assert.IsFalse(MeleeRules.TryBeginSwing(me, Vector2.right, config), "Bekleme dolmadı");

        me.Status.Tick(config.swordSwingCooldownSeconds);
        Assert.IsTrue(MeleeRules.TryBeginSwing(me, Vector2.right, config));
        MeleeRules.TickSwing(me, config.swordWindupSeconds + 0.01f, world, config, victims);

        Assert.IsFalse(front.IsAlive, "İki vuruşta elenir");
        Assert.IsFalse(me.Status.HasWeapon, "2 vuruşluk kılıç bitti");
    }

    [Test]
    public void LastSwing_StillHits_AfterTheWeaponIsUsedUp()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));
        me.Status.ArmWeapon(sword, 1, 10f); // Tek atış kaldı

        Assert.IsTrue(MeleeRules.TryBeginSwing(me, Vector2.right, config));
        Assert.IsFalse(me.Status.HasWeapon, "Atış harcandı");
        Assert.IsTrue(MeleeRules.TickSwing(me, config.swordWindupSeconds + 0.01f, world, config, victims));
        Assert.AreEqual(1, front.Health.Current);
    }

    [Test]
    public void ZeroWindup_HitsAtOnce_AndClearCancelsAPendingSwing()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));

        config.swordWindupSeconds = 0f;
        MeleeRules.TryBeginSwing(me, Vector2.right, config);
        Assert.IsTrue(MeleeRules.TickSwing(me, 0f, world, config, victims), "Hazırlık 0: hemen vurur");
        Assert.AreEqual(1, front.Health.Current);

        config.swordWindupSeconds = 0.2f;
        me.Status.Tick(5f);
        Assert.IsTrue(MeleeRules.TryBeginSwing(me, Vector2.right, config));
        me.Status.Clear(); // Elendi / etkiler silindi
        Assert.IsFalse(MeleeRules.TickSwing(me, 1f, world, config, victims));
        Assert.AreEqual(1, front.Health.Current);
    }

    [Test]
    public void EliminatedAttacker_DoesNotHit()
    {
        var world = new FakeWorld();
        PlayerState me = Armed(world);
        PlayerState front = world.Add(2, new Vector2(1f, 0f));

        MeleeRules.TryBeginSwing(me, Vector2.right, config);
        me.Eliminate();
        MeleeRules.TickSwing(me, 1f, world, config, victims);

        Assert.AreEqual(3, front.Health.Current);
    }
}
