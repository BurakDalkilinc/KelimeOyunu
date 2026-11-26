using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Patlama ateşi testleri: "explosive" etiketli kaynağın (bomba, patlayan tuzak, harita çapında patlama) hasarı oyuncuyu
/// tutuşturur ve kömürleştirir; kalkan ve dokunulmazlık korur; patlayıcı olmayan kaynak tutuşturmaz.
/// </summary>
public class ExplosionFireTests
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

    GameConfig config;
    ArenaBounds arena;
    FakeWorld world;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(34f, 19f);
        arena = new ArenaBounds(config.arenaSize);
        world = new FakeWorld();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static WordEntry Word(string id, int damage, params string[] tags) => new WordEntry
    {
        id = id,
        display = id,
        category = "attack",
        scope = "target",
        tags = tags,
        stats = new WordStats { damage = damage, duration = 5f, radius = 2f, warningSeconds = 1f },
    };

    /// <summary>Kelimeden saldırıyı kurar, (0,0)'a koyar ve uyarısı dolup vurana kadar ilerletir.</summary>
    CreatureModel StrikeAtOrigin(WordEntry word, PlayerState target)
    {
        CreatureModel strike = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);
        strike.PlaceAt(Vector2.zero);
        var hits = new List<CreatureHit>();
        for (int i = 0; i < 600 && !strike.HasStruck; i++)
            strike.TickStrike(1f / 60f, world, config.playerRadius, hits);
        Assert.IsTrue(strike.HasStruck, "vuruş yapılmalı");
        return strike;
    }

    // ---- Bomba (explosive) ----

    [Test]
    public void ExplosiveStrike_IgnitesAndCharsWhoItHits()
    {
        PlayerState target = world.Add(1, Vector2.zero);

        StrikeAtOrigin(Word("test_bomba", 1, "explosive", "area"), target);

        Assert.AreEqual(2, target.Health.Current);
        Assert.IsTrue(target.Status.IsBurning, "patlama tutuşturur");
        Assert.AreEqual(config.charPerFireHit, target.Status.CharLevel, 0.001f, "patlama kömürleştirir");
    }

    [Test]
    public void ExplosiveStrike_Kill_LeavesCharredDeadPlayer()
    {
        PlayerState target = world.Add(1, Vector2.zero, health: 1);

        StrikeAtOrigin(Word("test_bomba", 5, "explosive"), target);

        Assert.IsFalse(target.IsAlive);
        Assert.IsTrue(target.Status.IsBurning, "ölüm anında yanıyor: kömürleşmiş ceset düşer");
        Assert.Greater(target.Status.CharLevel, 0f);
    }

    [Test]
    public void ExplosiveStrike_ShieldBlocks_NoBurning()
    {
        PlayerState target = world.Add(1, Vector2.zero);
        target.Status.ApplyShield(1, 30f);

        StrikeAtOrigin(Word("test_bomba", 1, "explosive"), target);

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(target.Status.IsBurning, "kalkan hasarı ve ateşi engeller");
        Assert.AreEqual(0f, target.Status.CharLevel);
    }

    [Test]
    public void ExplosiveStrike_Invulnerable_NoBurning()
    {
        PlayerState target = world.Add(1, Vector2.zero);
        target.MakeInvulnerable();

        StrikeAtOrigin(Word("test_bomba", 1, "explosive"), target);

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(target.Status.IsBurning);
    }

    [Test]
    public void NonExplosiveStrike_DoesNotIgnite()
    {
        PlayerState target = world.Add(1, Vector2.zero);

        StrikeAtOrigin(Word("test_yumruk", 1, "instant"), target);

        Assert.AreEqual(2, target.Health.Current, "hasar yine işler");
        Assert.IsFalse(target.Status.IsBurning, "patlayıcı olmayan vuruş tutuşturmaz");
        Assert.AreEqual(0f, target.Status.CharLevel);
    }

    [Test]
    public void RealBombWord_IgnitesWhoItHits()
    {
        WordEntry bomb = WordDatabase.LoadFromResources(50).Lookup("bomba").Entry;
        PlayerState target = world.Add(1, Vector2.zero);

        StrikeAtOrigin(bomb, target);

        Assert.IsTrue(target.Status.IsBurning);
        Assert.Greater(target.Status.CharLevel, 0f);
    }

    // ---- Patlayan tuzak (trap + explosive) ----

    [Test]
    public void ExplosiveTrap_StaysTrap_AndIgnitesWhoStepsOnIt()
    {
        PlayerState target = world.Add(1, Vector2.zero);
        CreatureModel mine = CreatureFactory.CreateAttack(Word("test_mayin", 1, "trap", "explosive"), target, Vector2.zero, config, arena);
        mine.PlaceAt(Vector2.zero);

        Assert.IsTrue(mine.IsTrap);
        Assert.IsFalse(mine.IsStrike, "tuzakta fitil kurulmaz");

        int triggered = mine.TickTrap(1f / 60f, world, config.playerRadius, new List<CreatureHit>());

        Assert.AreEqual(1, triggered);
        Assert.IsTrue(target.Status.IsBurning);
        Assert.Greater(target.Status.CharLevel, 0f);
    }

    [Test]
    public void NonExplosiveTrap_DoesNotIgnite()
    {
        PlayerState target = world.Add(1, Vector2.zero);
        CreatureModel nail = CreatureFactory.CreateAttack(Word("test_civi", 1, "trap"), target, Vector2.zero, config, arena);
        nail.PlaceAt(Vector2.zero);

        nail.TickTrap(1f / 60f, world, config.playerRadius, new List<CreatureHit>());

        Assert.AreEqual(2, target.Health.Current);
        Assert.IsFalse(target.Status.IsBurning);
    }

    // ---- Harita çapında patlama (map_wide + explosive: atom bombası gibi) ----

    MapBlastEffect Blast(WordEntry word)
    {
        var effect = new MapBlastEffect();
        effect.Begin(new ThreatContext(word, null, config, arena, world, new System.Random(1), new SafeZoneField()));
        return effect;
    }

    [Test]
    public void ExplosiveMapBlast_KillsCharred_ShieldedSurvivesUnburnt()
    {
        PlayerState outside = world.Add(1, Vector2.zero);
        PlayerState shielded = world.Add(2, new Vector2(3f, 0f));
        shielded.Status.ApplyShield(1, 30f);

        Blast(Word("test_atom", 99, "explosive", "map_wide"));

        Assert.IsFalse(outside.IsAlive);
        Assert.IsTrue(outside.Status.IsBurning, "patlamada ölen kömürleşmiş düşer");
        Assert.Greater(outside.Status.CharLevel, 0f);

        Assert.IsTrue(shielded.IsAlive, "kalkan bir kez kurtarır");
        Assert.IsFalse(shielded.Status.IsBurning);
        Assert.AreEqual(0f, shielded.Status.CharLevel);
    }

    [Test]
    public void NonExplosiveMapBlast_DoesNotIgnite()
    {
        PlayerState player = world.Add(1, Vector2.zero);

        Blast(Word("test_dalga", 1, "map_wide"));

        Assert.AreEqual(2, player.Health.Current);
        Assert.IsFalse(player.Status.IsBurning);
    }
}
