using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Hayalet dirilişi testleri: can, kelime etiketi, rastgele diriliş hizmeti ve ağ simülasyonu.</summary>
public class RevivalTests
{
    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public bool TryGetPosition(int id, out Vector2 p) => Positions.TryGetValue(id, out p);
        public bool TryApplyKnockback(int id, Vector2 d) => Positions.ContainsKey(id);
        public bool TryGetFacing(int id, out Vector2 d) { d = Vector2.up; return false; }
    }

    sealed class CountingRevival : IRevivalService
    {
        public int Calls;
        public PlayerState LastWriter;
        public void RequestRevival(PlayerState writer) { Calls++; LastWriter = writer; }
    }

    GameConfig config;
    WordDatabase words;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.LoadFromResources(config.maxWordLength);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static PlayerState Dead(int id, bool bot = false)
    {
        var player = new PlayerState(id, "P" + id, 3, bot);
        player.Health.TakeDamage(99);
        return player;
    }

    WordEntry Word(string id)
    {
        foreach (WordEntry entry in words.Words)
            if (entry.id == id) return entry;
        Assert.Fail("Kelime yok: " + id);
        return null;
    }

    // ---- Health.Revive ----

    [Test]
    public void Health_Revive_BringsTheDeadBackWithTheGivenHealth()
    {
        var health = new Health(3);
        health.TakeDamage(3);
        int changes = 0;
        health.Changed += (_, __) => changes++;

        Assert.AreEqual(2, health.Revive(2));

        Assert.IsFalse(health.IsDead);
        Assert.AreEqual(2, health.Current);
        Assert.AreEqual(1, changes);
    }

    [Test]
    public void Health_Revive_ClampsBetweenOneAndMax()
    {
        var a = new Health(3); a.TakeDamage(3);
        var b = new Health(3); b.TakeDamage(3);

        Assert.AreEqual(1, a.Revive(0));
        Assert.AreEqual(3, b.Revive(99));
    }

    [Test]
    public void Health_Revive_DoesNothingToTheLiving()
    {
        var health = new Health(3);
        health.TakeDamage(1);

        Assert.AreEqual(0, health.Revive(3));
        Assert.AreEqual(2, health.Current);
    }

    [Test]
    public void Health_CanDieAgain_AfterRevive()
    {
        var health = new Health(3);
        int died = 0;
        health.Died += () => died++;
        health.TakeDamage(3);
        health.Revive(1);

        health.TakeDamage(1);

        Assert.AreEqual(2, died);
        Assert.IsTrue(health.IsDead);
    }

    // ---- Etiket, kelime ----

    [Test]
    public void ReviveTag_CallsTheServiceWithTheWriter_AndIsRegistered()
    {
        Assert.IsTrue(TagRegistry.IsKnown("revive"));
        var service = new CountingRevival();
        var writer = new PlayerState(1, "Yazan", 3);

        TagRegistry.ApplySelfEffects(Word("yeniden_canlanma"), new SelfEffectContext(writer, Word("yeniden_canlanma"), config, revival: service));

        Assert.AreEqual(1, service.Calls);
        Assert.AreSame(writer, service.LastWriter);
    }

    [Test]
    public void ReviveTag_WithoutService_DoesNothing()
    {
        var writer = new PlayerState(1, "Yazan", 3);
        Assert.DoesNotThrow(() => TagRegistry.ApplySelfEffects(Word("yeniden_canlanma"), new SelfEffectContext(writer, Word("yeniden_canlanma"), config)));
    }

    [TestCase("yeniden canlanma")]
    [TestCase("revive")]
    [TestCase("review")]
    [TestCase("diriliş")]
    [TestCase("resurrect")]
    public void ReviveWord_IsFoundByItsNamesAndSynonyms(string text)
    {
        WordLookupResult result = words.Lookup(text);

        Assert.AreEqual(WordLookupStatus.Found, result.Status);
        Assert.AreEqual("yeniden_canlanma", result.Entry.id);
    }

    // ---- Diriliş hizmeti (tek kişilik ve ağ aynısını kullanır) ----

    [Test]
    public void GhostRevival_RevivesARandomGhost_WithConfiguredHealth_AndClearsEffects()
    {
        var alive = new PlayerState(1, "A", 3);
        var ghost = Dead(2);
        ghost.Status.ApplySlow(0.5f, 30f);
        var service = new GhostRevival(new[] { alive, ghost }, config, new System.Random(1));

        service.RequestRevival(alive);

        Assert.IsFalse(ghost.IsGhost);
        Assert.AreEqual(config.reviveHealth, ghost.Health.Current);
        Assert.AreEqual(1f, ghost.Status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void GhostRevival_WithoutGhosts_DoesNothing()
    {
        var alive = new PlayerState(1, "A", 3);
        var service = new GhostRevival(new[] { alive }, config, new System.Random(1));

        Assert.DoesNotThrow(() => service.RequestRevival(alive));
        Assert.IsNull(GhostRevival.Pick(new[] { alive }, new System.Random(1)));
    }

    [Test]
    public void GhostRevival_NeverRevivesTheLiving()
    {
        var alive = new PlayerState(1, "A", 3);
        alive.Health.TakeDamage(1);

        GhostRevival.Revive(alive, config);

        Assert.AreEqual(2, alive.Health.Current); // Yaşayanın canı değişmez
    }

    // ---- Ağ simülasyonu: rastgele diriliş ----

    NetMatchSim BuildSim(out PlayerState writer, out PlayerState humanGhost, out PlayerState botGhost)
    {
        writer = new PlayerState(1, "Yazan", config.startingHealth);
        humanGhost = Dead(2);
        botGhost = Dead(100, bot: true);
        var other = new PlayerState(3, "Diğer", config.startingHealth);

        var avatars = new FakeAvatars();
        foreach (int id in new[] { 1, 2, 3, 100 }) avatars.Positions[id] = new Vector2(id * 0.1f, 0f);

        return new NetMatchSim(config, new MatchState(new[] { writer, humanGhost, other, botGhost }), avatars, new System.Random(4));
    }

    [Test]
    public void NetSim_ReviveWord_RevivesExactlyOneRandomGhost_Immediately()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out PlayerState humanGhost, out PlayerState botGhost);

        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        int revived = (humanGhost.IsGhost ? 0 : 1) + (botGhost.IsGhost ? 0 : 1);
        Assert.AreEqual(1, revived);
        PlayerState back = humanGhost.IsGhost ? botGhost : humanGhost;
        Assert.AreEqual(config.reviveHealth, back.Health.Current);
    }

    [Test]
    public void NetSim_SecondReviveWord_BringsBackTheOtherGhost()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out PlayerState humanGhost, out PlayerState botGhost);

        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));
        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        Assert.IsFalse(humanGhost.IsGhost);
        Assert.IsFalse(botGhost.IsGhost);
    }

    [Test]
    public void NetSim_EveryGhostCanBeTheOneWhoReturns()
    {
        var seen = new HashSet<int>();
        for (int seed = 0; seed < 40; seed++)
        {
            var writer = new PlayerState(1, "Yazan", 3);
            PlayerState a = Dead(2), b = Dead(3), c = Dead(4);
            var avatars = new FakeAvatars();
            foreach (int id in new[] { 1, 2, 3, 4 }) avatars.Positions[id] = Vector2.zero;
            var sim = new NetMatchSim(config, new MatchState(new[] { writer, a, b, c }), avatars, new System.Random(seed));

            sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

            foreach (PlayerState ghost in new[] { a, b, c })
                if (!ghost.IsGhost) seen.Add(ghost.Id);
        }

        Assert.AreEqual(3, seen.Count, "Rastgele seçim hayaletlerin hepsine şans vermeli");
    }

    [Test]
    public void NetSim_NoGhosts_ReviveWordFizzles()
    {
        var a = new PlayerState(1, "A", 3);
        var b = new PlayerState(2, "B", 3);
        var avatars = new FakeAvatars();
        avatars.Positions[1] = Vector2.zero; avatars.Positions[2] = Vector2.one;
        var sim = new NetMatchSim(config, new MatchState(new[] { a, b }), avatars, new System.Random(1));

        Assert.DoesNotThrow(() => sim.OnWordPlayed(a, Word("yeniden_canlanma")));
        Assert.AreEqual(3, a.Health.Current);
        Assert.AreEqual(3, b.Health.Current);
    }
}
