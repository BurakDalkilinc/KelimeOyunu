using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>"trap" etiketinin testleri: Muz kabuğu (yerde bekler, basanı kaydırır, tek kullanımlık).</summary>
public class TrapTagTests
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
        config = ScriptableObject.CreateInstance<GameConfig>(); // tuzak hedefin 2 birim yanına düşer, fırlatma 3 birim
        world = new FakeWorld();
        hits = new List<CreatureHit>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    CreatureModel Banana(PlayerState target, Vector2 at)
    {
        var banana = CreatureFactory.CreateAttack(Real("muz_kabugu"), target, Vector2.zero, config, arena);
        banana.PlaceAt(at);
        return banana;
    }

    // ---- Muz kabuğu (gerçek kelimeden) ----

    [Test]
    public void MuzKabugu_FromRealWords_IsAStationarySlipTrap()
    {
        var target = world.Add(1, Vector2.zero);
        var banana = Banana(target, new Vector2(2f, 0f));

        Assert.IsTrue(banana.IsTrap);
        Assert.AreEqual(0.8f, banana.Radius, 0.001f);                 // words.json: radius 0.8
        Assert.AreEqual(0f, banana.Speed, 0.001f);
        Assert.AreEqual(0, banana.Damage);
        Assert.AreEqual(config.knockbackDistance, banana.KnockbackDistance, 0.001f);
        Assert.AreEqual(config.trapDropDistance, banana.PlacementOffset, 0.001f);
        Assert.IsTrue(banana.PlacesNearTarget);
        Assert.AreEqual(15f, banana.LifeLeft, 0.001f);                // words.json: duration 15
    }

    [Test]
    public void Trap_BotsDoNotSeeIt_SoItIsNotHarmful()
    {
        var target = world.Add(1, Vector2.zero);
        var banana = Banana(target, new Vector2(2f, 0f));

        Assert.IsFalse(banana.IsHarmful, "Botlar tuzaktan kaçmaz");
        Assert.IsFalse(banana.IsDangerous);
    }

    [Test]
    public void Trap_PlayerSteppingOnIt_SlidesAwayFromItsCenter_AndTrapDisappears()
    {
        var target = world.Add(1, new Vector2(5f, 0f));
        var victim = world.Add(2, new Vector2(2.6f, 0.2f)); // Muz (2,0) merkezinin sağında
        var banana = Banana(target, new Vector2(2f, 0f));

        int triggered = banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(1, triggered);
        Assert.AreEqual(1, hits.Count);
        Assert.AreSame(victim, hits[0].Player);
        Assert.AreEqual(config.knockbackDistance, hits[0].Knockback.magnitude, 0.001f);
        Assert.Greater(hits[0].Knockback.x, 0f, "Merkezin sağında basan sağa kayar");
        Assert.AreEqual(3, victim.Health.Current, "Hasar yok");
        Assert.IsTrue(banana.IsFinished, "Tek kullanımlık");
    }

    [Test]
    public void Trap_AffectsAnyoneNotJustTheTarget_IncludingTheWriter()
    {
        var target = world.Add(1, new Vector2(8f, 3f));
        var writer = world.Add(2, new Vector2(-2.6f, 0f)); // Muzun (-2,0) merkezinin solunda
        var banana = Banana(target, new Vector2(-2f, 0f));

        banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(1, hits.Count);
        Assert.AreSame(writer, hits[0].Player, "Yazan da basarsa kayar");
        Assert.Less(hits[0].Knockback.x, 0f);
    }

    [Test]
    public void Trap_DoesNothingWhileNobodyIsNear()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f)); // Hedef 2 uzakta; temas mesafesi 0.8 + 0.5 = 1.3

        for (int i = 0; i < 20; i++)
            Assert.AreEqual(0, banana.TickTrap(0.1f, world, PlayerRadius, hits));

        Assert.IsEmpty(hits);
        Assert.IsFalse(banana.IsFinished);
    }

    [Test]
    public void Trap_FiresWhenTheTargetWalksInto_ItLater()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f));
        banana.TickTrap(1f, world, PlayerRadius, hits);
        Assert.IsFalse(banana.IsFinished);

        world.Move(1, new Vector2(1.0f, 0f)); // Muza yaklaştı (mesafe 1.0 < 1.3)
        int triggered = banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(1, triggered);
        Assert.Less(hits[0].Knockback.x, 0f, "Muzun solundan basan sola kayar");
    }

    [Test]
    public void Trap_TwoPlayersOnTopOfIt_BothSlideInTheirOwnDirections()
    {
        var a = world.Add(1, new Vector2(2.5f, 0f));
        var b = world.Add(2, new Vector2(1.5f, 0f));
        var banana = Banana(a, new Vector2(2f, 0f));

        int triggered = banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(2, triggered);
        Assert.Greater(hits.Find(h => h.Player == a).Knockback.x, 0f);
        Assert.Less(hits.Find(h => h.Player == b).Knockback.x, 0f);
    }

    [Test]
    public void Trap_PlayerExactlyOnTheCenter_StillGetsAFiniteSlide()
    {
        var target = world.Add(1, new Vector2(2f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f));

        banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Vector2 push = hits[0].Knockback;
        Assert.IsFalse(float.IsNaN(push.x) || float.IsNaN(push.y));
        Assert.AreEqual(config.knockbackDistance, push.magnitude, 0.001f);
    }

    [Test]
    public void Trap_SkipsEliminatedPlayers()
    {
        var target = world.Add(1, new Vector2(2f, 0f));
        target.Health.TakeDamage(99);
        var banana = Banana(target, new Vector2(2f, 0f));

        Assert.AreEqual(0, banana.TickTrap(0.1f, world, PlayerRadius, hits));
        Assert.IsFalse(banana.IsFinished);
    }

    [Test]
    public void Trap_StaysEvenIfItsTargetIsEliminated()
    {
        var target = world.Add(1, new Vector2(8f, 0f));
        var bystander = world.Add(2, new Vector2(-8f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f));
        target.Health.TakeDamage(99);

        banana.TickTrap(1f, world, PlayerRadius, hits);
        Assert.IsFalse(banana.IsFinished);

        world.Move(2, new Vector2(2.2f, 0f));
        Assert.AreEqual(1, banana.TickTrap(0.1f, world, PlayerRadius, hits));
        Assert.IsNotNull(bystander);
    }

    [Test]
    public void Trap_DisappearsWhenItsLifeEnds_WithoutTriggering()
    {
        var target = world.Add(1, new Vector2(8f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f)); // Ömür 15 sn

        banana.TickTrap(14.9f, world, PlayerRadius, hits);
        Assert.IsFalse(banana.IsFinished);

        banana.TickTrap(0.2f, world, PlayerRadius, hits);
        Assert.IsTrue(banana.IsFinished);
        Assert.IsEmpty(hits);
    }

    [Test]
    public void Trap_FinishedTrapNeverTriggersAgain()
    {
        var target = world.Add(1, new Vector2(2f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f));
        banana.TickTrap(0.1f, world, PlayerRadius, hits);
        hits.Clear();

        Assert.AreEqual(0, banana.TickTrap(0.1f, world, PlayerRadius, hits));
        Assert.IsEmpty(hits);
    }

    [Test]
    public void Trap_AppliesDamageAndSlowToo_WhenItsWordHasThem()
    {
        var word = new WordEntry
        {
            id = "x", tags = new[] { "trap", "slowdown" },
            stats = new WordStats { damage = 1, radius = 1f, slowSeconds = 2f, duration = 10f }
        };
        var victim = world.Add(1, new Vector2(2f, 0f));
        var banana = CreatureFactory.CreateAttack(word, victim, Vector2.zero, config, arena);
        banana.PlaceAt(new Vector2(2f, 0f));

        banana.TickTrap(0.1f, world, PlayerRadius, hits);

        Assert.AreEqual(2, victim.Health.Current);
        Assert.IsTrue(victim.Status.IsSlowed);
        Assert.IsEmpty(hits, "Fırlatma etiketi yok, kayma da yok");
    }

    [Test]
    public void Trap_NullWorld_DoesNotThrow()
    {
        var target = world.Add(1, Vector2.zero);
        var banana = Banana(target, new Vector2(2f, 0f));
        Assert.DoesNotThrow(() => banana.TickTrap(0.1f, null, PlayerRadius, hits));
    }

    // ---- Diğer tick'ler tuzağı yok sayar, tuzak olmayan TickTrap'te işlem yapmaz ----

    [Test]
    public void Trap_IgnoresNormalTick_StrikeTick_AndAreaTick()
    {
        var target = world.Add(1, new Vector2(2f, 0f));
        var banana = Banana(target, new Vector2(2f, 0f));

        Assert.IsFalse(banana.Tick(1f, new Vector2(2f, 0f), PlayerRadius, 1f));
        Assert.AreEqual(0, banana.TickStrike(1f, world, PlayerRadius, hits));
        Assert.AreEqual(0, banana.TickArea(1f, world, PlayerRadius));
        Assert.IsFalse(banana.IsFinished);
    }

    [Test]
    public void NonTrapCreature_TickTrapDoesNothing()
    {
        var target = world.Add(1, new Vector2(0f, 0f));
        var kurt = CreatureFactory.CreateAttack(Real("kurt"), target, Vector2.zero, config, arena);

        Assert.AreEqual(0, kurt.TickTrap(1f, world, PlayerRadius, hits));
        Assert.IsEmpty(hits);
    }

    [Test]
    public void TrapTag_WordWithoutStats_DoesNotThrow_AndKeepsDefaultRadius()
    {
        var word = new WordEntry { id = "x", tags = new[] { "trap" } };
        var target = world.Add(1, Vector2.zero);

        CreatureModel creature = null;
        Assert.DoesNotThrow(() => creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena));
        Assert.AreEqual(config.creatureRadius, creature.Radius, 0.001f);
    }

    [Test]
    public void PlacesNearTarget_IsTrueForStrikesAndTraps_FalseForChasers()
    {
        var target = world.Add(1, Vector2.zero);

        Assert.IsTrue(CreatureFactory.CreateAttack(Real("yumruk"), target, Vector2.zero, config, arena).PlacesNearTarget);
        Assert.IsTrue(CreatureFactory.CreateAttack(Real("bomba"), target, Vector2.zero, config, arena).PlacesNearTarget);
        Assert.IsTrue(CreatureFactory.CreateAttack(Real("muz_kabugu"), target, Vector2.zero, config, arena).PlacesNearTarget);
        Assert.IsFalse(CreatureFactory.CreateAttack(Real("kurt"), target, Vector2.zero, config, arena).PlacesNearTarget);
        Assert.IsFalse(CreatureFactory.CreateAttack(Real("bulut"), target, Vector2.zero, config, arena).PlacesNearTarget);
    }
}
