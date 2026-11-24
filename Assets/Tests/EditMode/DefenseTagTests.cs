using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>Koruma etiketlerinin testleri: shield, heal, stealth, escape ve flying; StatusTint.</summary>
public class DefenseTagTests
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

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    const float PlayerRadius = 0.5f;

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

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    static PlayerState NewPlayer(int id = 1, int health = 3) => new PlayerState(id, "P" + id, health);

    void ApplyWord(string id, PlayerState writer) => TagRegistry.ApplySelfEffects(Real(id), new SelfEffectContext(writer, Real(id), config));

    // ---- Kalkan ----

    [Test]
    public void Shield_AbsorbsOneHit_ThenIsGone()
    {
        var status = new StatusEffects();
        status.ApplyShield(1, 30f);

        Assert.IsTrue(status.HasShield);
        Assert.IsTrue(status.TryAbsorbHit());
        Assert.IsFalse(status.HasShield);
        Assert.IsFalse(status.TryAbsorbHit());
    }

    [Test]
    public void Shield_WithSeveralBlocks_AbsorbsThatMany()
    {
        var status = new StatusEffects();
        status.ApplyShield(3, 30f);

        Assert.IsTrue(status.TryAbsorbHit());
        Assert.IsTrue(status.TryAbsorbHit());
        Assert.IsTrue(status.TryAbsorbHit());
        Assert.IsFalse(status.TryAbsorbHit());
    }

    [Test]
    public void Shield_ExpiresWithTime()
    {
        var status = new StatusEffects();
        status.ApplyShield(1, 5f);

        status.Tick(4.9f);
        Assert.IsTrue(status.HasShield);

        status.Tick(0.2f);
        Assert.IsFalse(status.HasShield);
        Assert.IsFalse(status.TryAbsorbHit());
    }

    [Test]
    public void Shield_AppliedTwice_KeepsTheStrongerAndDoesNotStack()
    {
        var status = new StatusEffects();
        status.ApplyShield(1, 10f);
        status.ApplyShield(1, 30f);

        Assert.AreEqual(30f, status.ShieldTimeLeft, 0.001f);
        Assert.IsTrue(status.TryAbsorbHit());
        Assert.IsFalse(status.TryAbsorbHit(), "İki kalkan iki hak vermez");
    }

    [TestCase(0, 5f)]
    [TestCase(1, 0f)]
    [TestCase(-1, 5f)]
    public void Shield_NoOpCases(int blocks, float seconds)
    {
        var status = new StatusEffects();
        status.ApplyShield(blocks, seconds);
        Assert.IsFalse(status.HasShield);
    }

    [Test]
    public void PlayerState_TakeDamage_ShieldBlocksFirstHitOnly()
    {
        var player = NewPlayer();
        player.Status.ApplyShield(1, 30f);

        Assert.AreEqual(0, player.TakeDamage(2));
        Assert.AreEqual(3, player.Health.Current);

        Assert.AreEqual(2, player.TakeDamage(2));
        Assert.AreEqual(1, player.Health.Current);
    }

    [Test]
    public void PlayerState_TakeDamage_ZeroNegativeOrDead_DoesNothingAndKeepsTheShield()
    {
        var player = NewPlayer();
        player.Status.ApplyShield(1, 30f);

        Assert.AreEqual(0, player.TakeDamage(0));
        Assert.AreEqual(0, player.TakeDamage(-3));
        Assert.IsTrue(player.Status.HasShield, "Hasar olmayan vuruş kalkanı harcamaz");

        var dead = NewPlayer(2);
        dead.Health.TakeDamage(99);
        dead.Status.ApplyShield(1, 30f);
        Assert.AreEqual(0, dead.TakeDamage(1));
        Assert.IsTrue(dead.Status.HasShield);
    }

    [Test]
    public void Creature_HittingAShieldedTarget_IsAbsorbed_AndTheCreatureIsSpent()
    {
        var target = NewPlayer();
        target.Status.ApplyShield(1, 30f);
        var creature = new CreatureModel(null, target, Vector2.zero, 100f, 2, 0.5f, 10f, arena);

        bool hit = creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, 1f);

        Assert.IsTrue(hit);
        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(target.Status.HasShield);
        Assert.IsTrue(creature.IsFinished);
    }

    [Test]
    public void Strike_AbsorbedByShield_ForShieldedPlayersOnly()
    {
        var world = new FakeWorld();
        var shielded = world.Add(1, new Vector2(1f, 0f));
        var plain = world.Add(2, new Vector2(-1f, 0f));
        shielded.Status.ApplyShield(1, 30f);
        var fist = CreatureFactory.CreateAttack(Real("yumruk"), shielded, Vector2.zero, config, arena);
        fist.PlaceAt(Vector2.zero);

        fist.TickStrike(1.5f, world, PlayerRadius, new List<CreatureHit>());

        Assert.AreEqual(3, shielded.Health.Current);
        Assert.AreEqual(2, plain.Health.Current);
    }

    [Test]
    public void Kalkan_FromRealWords_GivesShieldToTheWriter()
    {
        var writer = NewPlayer();
        ApplyWord("kalkan", writer);

        Assert.IsTrue(writer.Status.HasShield);
        Assert.AreEqual(30f, writer.Status.ShieldTimeLeft, 0.001f); // words.json: duration 30
        Assert.IsTrue(writer.Status.TryAbsorbHit());
        Assert.IsFalse(writer.Status.TryAbsorbHit(), "blocks: 1");
    }

    // ---- Can ----

    [Test]
    public void Can_FromRealWords_HealsOneAndNeverAboveMax()
    {
        var writer = NewPlayer(health: 3);
        ApplyWord("can", writer);
        Assert.AreEqual(3, writer.Health.Current, "Zaten tam can");

        writer.Health.TakeDamage(2);
        ApplyWord("can", writer);
        Assert.AreEqual(2, writer.Health.Current);

        ApplyWord("can", writer);
        ApplyWord("can", writer);
        Assert.AreEqual(3, writer.Health.Current);
    }

    [Test]
    public void Can_DoesNotReviveAnEliminatedWriter()
    {
        var writer = NewPlayer();
        writer.Health.TakeDamage(99);
        ApplyWord("can", writer);
        Assert.IsTrue(writer.Health.IsDead);
    }

    // ---- Görünmezlik ----

    [Test]
    public void Gorunmezlik_FromRealWords_MakesTheWriterStealthedForItsDuration()
    {
        var writer = NewPlayer();
        ApplyWord("gorunmezlik", writer);

        Assert.IsTrue(writer.Status.IsStealthed);
        Assert.AreEqual(6f, writer.Status.StealthTimeLeft, 0.001f); // words.json: duration 6

        writer.Status.Tick(6.1f);
        Assert.IsFalse(writer.Status.IsStealthed);
    }

    [Test]
    public void Stealthed_PlayerCannotBeTargeted_ByPickerHitTesterOrSelection()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        var hidden = world.Add(2, new Vector2(4f, 0f));
        var visible = world.Add(3, new Vector2(-4f, 0f));
        hidden.Status.ApplyStealth(6f);

        // Rastgele seçim: hep görünür olan
        for (int i = 0; i < 50; i++)
            Assert.AreSame(visible, TargetPicker.PickRandomOpponent(world.Players, writer, new Random(i)));

        // Tıklama: görünmeze tıklanınca hiçbir şey seçilmez
        Assert.IsNull(TargetHitTester.FindAt(new Vector2(4f, 0f), world, writer, 0.9f));
        Assert.AreSame(visible, TargetHitTester.FindAt(new Vector2(-4f, 0f), world, writer, 0.9f));

        // Seçim işlemi
        var selection = new TargetSelection(writer, 3f);
        Assert.IsFalse(selection.TrySelect(hidden));
        Assert.IsTrue(selection.TrySelect(visible));
    }

    [Test]
    public void Stealthed_AfterItEnds_CanBeTargetedAgain()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        var other = world.Add(2, new Vector2(4f, 0f));
        other.Status.ApplyStealth(2f);
        Assert.IsNull(TargetPicker.PickRandomOpponent(world.Players, writer, new Random(1)));

        other.Status.Tick(2.1f);

        Assert.AreSame(other, TargetPicker.PickRandomOpponent(world.Players, writer, new Random(1)));
    }

    // ---- Uçma (kanat) ----

    [Test]
    public void Flight_RaisesSpeed_AndEndsWithTime()
    {
        var status = new StatusEffects();
        status.ApplyFlight(5f, 1.2f);

        Assert.IsTrue(status.IsFlying);
        Assert.AreEqual(1.2f, status.SpeedMultiplier, 0.0001f);

        status.Tick(5.1f);
        Assert.IsFalse(status.IsFlying);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void Flight_AndSlow_MultiplyTogether()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 5f);
        status.ApplyFlight(5f, 1.2f);

        Assert.AreEqual(0.6f, status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void Flight_MultiplierBelowOne_NeverSlowsDown()
    {
        var status = new StatusEffects();
        status.ApplyFlight(5f, 0.5f);

        Assert.IsTrue(status.IsFlying);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void Flight_NoOpForZeroSeconds()
    {
        var status = new StatusEffects();
        status.ApplyFlight(0f, 2f);
        Assert.IsFalse(status.IsFlying);
    }

    [Test]
    public void Kanat_FromRealWords_GivesFlightWithSpeedRatio()
    {
        var writer = NewPlayer();
        ApplyWord("kanat", writer);

        Assert.IsTrue(writer.Status.IsFlying);
        Assert.AreEqual(8f, writer.Status.FlightTimeLeft, 0.001f);                              // words.json: duration 8
        Assert.AreEqual(6f / config.playerMoveSpeed, writer.Status.SpeedMultiplier, 0.001f);   // words.json: speed 6
    }

    [Test]
    public void GroundCreature_CannotHitAFlyingPlayer_ButHitsAfterFlightEnds()
    {
        var target = NewPlayer();
        target.Status.ApplyFlight(5f, 1.2f);
        var wolf = new CreatureModel(null, target, new Vector2(4f, 0f), 5f, 1, 0.5f, 30f, arena) { Persistent = true };

        Assert.IsFalse(wolf.Tick(0.5f, new Vector2(4f, 0f), PlayerRadius, 1f));
        Assert.AreEqual(3, target.Health.Current);

        target.Status.Tick(5.1f);
        Assert.IsTrue(wolf.Tick(0.1f, new Vector2(4f, 0f), PlayerRadius, 1f));
        Assert.AreEqual(2, target.Health.Current);
    }

    [Test]
    public void FlyingCreature_HitsAFlyingPlayer()
    {
        var target = NewPlayer();
        target.Status.ApplyFlight(5f, 1.2f);
        var ari = CreatureFactory.CreateAttack(Real("ari"), target, new Vector2(4f, 0f), config, arena);

        Assert.IsTrue(ari.IsFlying);
        Assert.IsTrue(ari.Tick(0.1f, new Vector2(4f, 0f), PlayerRadius, 1f));
        Assert.AreEqual(2, target.Health.Current);
    }

    [Test]
    public void FlyingTag_MarksOnlyFlyingWords()
    {
        var target = NewPlayer();
        Assert.IsTrue(CreatureFactory.CreateAttack(Real("ari"), target, Vector2.zero, config, arena).IsFlying);
        Assert.IsFalse(CreatureFactory.CreateAttack(Real("kurt"), target, Vector2.zero, config, arena).IsFlying);
        Assert.IsFalse(CreatureFactory.CreateAttack(Real("zombi"), target, Vector2.zero, config, arena).IsFlying);
    }

    [Test]
    public void Trap_FlyingPlayerDoesNotTriggerIt_ButGroundPlayerDoes()
    {
        var world = new FakeWorld();
        var flyer = world.Add(1, new Vector2(2f, 0f));
        flyer.Status.ApplyFlight(5f, 1.2f);
        var banana = CreatureFactory.CreateAttack(Real("muz_kabugu"), flyer, Vector2.zero, config, arena);
        banana.PlaceAt(new Vector2(2f, 0f));
        var hits = new List<CreatureHit>();

        Assert.AreEqual(0, banana.TickTrap(0.1f, world, PlayerRadius, hits));
        Assert.IsFalse(banana.IsFinished);

        flyer.Status.Tick(5.1f);
        Assert.AreEqual(1, banana.TickTrap(0.1f, world, PlayerRadius, hits));
    }

    // ---- Self etkileri ----

    [Test]
    public void ApplySelfEffects_AttackWords_DoNothing()
    {
        var writer = NewPlayer();
        Assert.AreEqual(0, TagRegistry.ApplySelfEffects(Real("kurt"), new SelfEffectContext(writer, Real("kurt"), config)));
        Assert.IsFalse(writer.Status.HasShield || writer.Status.IsStealthed || writer.Status.IsFlying);
    }

    [Test]
    public void ApplySelfEffects_NullWordOrWriter_ReturnsZero()
    {
        var writer = NewPlayer();
        Assert.AreEqual(0, TagRegistry.ApplySelfEffects(null, new SelfEffectContext(writer, null, config)));
        Assert.AreEqual(0, TagRegistry.ApplySelfEffects(Real("kalkan"), new SelfEffectContext(null, Real("kalkan"), config)));
    }

    [Test]
    public void SelfTags_WordsWithoutStats_UseConfigDefaults()
    {
        var writer = NewPlayer();
        var word = new WordEntry { id = "x", tags = new[] { "shield", "stealth", "escape" } };

        int applied = TagRegistry.ApplySelfEffects(word, new SelfEffectContext(writer, word, config));

        Assert.AreEqual(3, applied);
        Assert.AreEqual(config.shieldDefaultSeconds, writer.Status.ShieldTimeLeft, 0.001f);
        Assert.AreEqual(config.stealthDefaultSeconds, writer.Status.StealthTimeLeft, 0.001f);
        Assert.AreEqual(config.flightDefaultSeconds, writer.Status.FlightTimeLeft, 0.001f);
    }

    [Test]
    public void RealDefenseWords_AreAllSelfScopedAndEachAppliesSomething()
    {
        foreach (string id in new[] { "kalkan", "can", "gorunmezlik", "kanat" })
        {
            var writer = NewPlayer();
            writer.Health.TakeDamage(1);
            Assert.AreEqual(WordEntry.ScopeSelf, Real(id).scope, id);
            Assert.GreaterOrEqual(TagRegistry.ApplySelfEffects(Real(id), new SelfEffectContext(writer, Real(id), config)), 1, id);
        }
    }

    // ---- StatusTint ----

    [Test]
    public void Tint_NoStatus_ReturnsTheBaseColor()
    {
        var baseColor = new Color(0.3f, 0.85f, 0.6f, 1f);
        Assert.AreEqual(baseColor, StatusTint.Apply(baseColor, new StatusEffects()));
        Assert.AreEqual(baseColor, StatusTint.Apply(baseColor, null));
    }

    [Test]
    public void Tint_EachEffectChangesTheColor()
    {
        var baseColor = new Color(0.3f, 0.85f, 0.6f, 1f);

        var slowed = new StatusEffects(); slowed.ApplySlow(0.5f, 3f);
        var shielded = new StatusEffects(); shielded.ApplyShield(1, 30f);
        var flying = new StatusEffects(); flying.ApplyFlight(5f, 1.2f);

        Assert.AreNotEqual(baseColor, StatusTint.Apply(baseColor, slowed));
        Assert.AreNotEqual(baseColor, StatusTint.Apply(baseColor, shielded));
        Assert.AreNotEqual(baseColor, StatusTint.Apply(baseColor, flying));
    }

    [Test]
    public void Tint_Stealth_MakesTheColorFaint_AndKeepsOtherTints()
    {
        var baseColor = new Color(0.3f, 0.85f, 0.6f, 1f);
        var stealthed = new StatusEffects();
        stealthed.ApplyStealth(6f);

        Assert.LessOrEqual(StatusTint.Apply(baseColor, stealthed).a, StatusTint.StealthAlpha + 0.0001f);

        stealthed.ApplyShield(1, 30f);
        Color both = StatusTint.Apply(baseColor, stealthed);
        Assert.LessOrEqual(both.a, StatusTint.StealthAlpha + 0.0001f);
        Assert.AreNotEqual(baseColor.g, both.g, "Kalkan tonu da uygulanır");
    }
}
