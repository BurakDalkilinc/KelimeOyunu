using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Hayalet dirilişi testleri: can, oylama, kelime etiketi, tek kişilik hizmet ve ağ simülasyonu.</summary>
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

    // ---- GhostVote ----

    [Test]
    public void Vote_MajorityWins()
    {
        var a = Dead(1); var b = Dead(2); var c = Dead(3);
        var vote = new GhostVote(new[] { a, b, c }, new[] { 1, 2, 3 }, 5f, new System.Random(1));

        vote.TryVote(1, 2);
        vote.TryVote(2, 2);
        Assert.IsFalse(vote.IsFinished);
        vote.TryVote(3, 1);

        Assert.IsTrue(vote.IsFinished); // Herkes oy verince erken biter
        Assert.AreSame(b, vote.Winner);
        Assert.AreEqual(2, vote.VotesFor(2));
    }

    [Test]
    public void Vote_Tie_PicksOneOfTheTopCandidates()
    {
        var a = Dead(1); var b = Dead(2); var c = Dead(3);
        for (int seed = 0; seed < 20; seed++)
        {
            var vote = new GhostVote(new[] { a, b, c }, new[] { 1, 2 }, 5f, new System.Random(seed));
            vote.TryVote(1, 1);
            vote.TryVote(2, 2);

            Assert.IsTrue(vote.Winner == a || vote.Winner == b);
            Assert.AreNotSame(c, vote.Winner);
        }
    }

    [Test]
    public void Vote_NoVotes_AfterTimeout_PicksRandomly()
    {
        var a = Dead(1); var b = Dead(2);
        var vote = new GhostVote(new[] { a, b }, new[] { 1 }, 5f, new System.Random(3));

        vote.Tick(4f);
        Assert.IsFalse(vote.IsFinished);
        vote.Tick(2f);

        Assert.IsTrue(vote.IsFinished);
        Assert.IsTrue(vote.Winner == a || vote.Winner == b);
    }

    [Test]
    public void Vote_WithoutHumanVoters_FinishesImmediately()
    {
        var bot = Dead(100, bot: true);
        var vote = new GhostVote(new[] { bot }, new int[0], 5f, new System.Random(1));

        Assert.IsTrue(vote.IsFinished);
        Assert.AreSame(bot, vote.Winner);
    }

    [Test]
    public void Vote_RejectsNonVoters_UnknownCandidates_AndLateVotes()
    {
        var a = Dead(1); var b = Dead(2);
        var vote = new GhostVote(new[] { a, b }, new[] { 1, 2 }, 5f, new System.Random(1));

        Assert.IsFalse(vote.TryVote(9, 1));   // Oy hakkı yok
        Assert.IsFalse(vote.TryVote(1, 9));   // Aday değil
        Assert.AreEqual(0, vote.VotesCast);

        Assert.IsTrue(vote.TryVote(1, 1));
        Assert.IsTrue(vote.TryVote(1, 2));    // Fikir değiştirmek serbest
        Assert.AreEqual(1, vote.VotesCast);
        Assert.AreEqual(1, vote.VotesFor(2));

        vote.Tick(10f);
        Assert.IsFalse(vote.TryVote(2, 1));   // Bitti
    }

    [Test]
    public void Vote_RequiresAtLeastOneCandidate()
    {
        Assert.Throws<System.ArgumentException>(() => new GhostVote(new PlayerState[0], new[] { 1 }, 5f, new System.Random(1)));
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

    // ---- Tek kişilik hizmet ----

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

    // ---- Ağ simülasyonu: oylu diriliş ----

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
    public void NetSim_ReviveWord_StartsAVote_AndTheVotedGhostReturns()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out PlayerState humanGhost, out PlayerState botGhost);
        float started = -1f; int ended = -2;
        sim.VoteStarted += s => started = s;
        sim.VoteEnded += id => ended = id;

        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        Assert.IsTrue(sim.IsVoting);
        Assert.AreEqual(config.reviveVoteSeconds, started, 0.0001f);

        Assert.IsTrue(sim.TryVote(2, 100)); // İnsan hayalet, bot hayaleti seçti

        Assert.IsFalse(sim.IsVoting);
        Assert.AreEqual(100, ended);
        Assert.IsFalse(botGhost.IsGhost);
        Assert.AreEqual(config.reviveHealth, botGhost.Health.Current);
        Assert.IsTrue(humanGhost.IsGhost); // Diğeri hâlâ elenmiş
    }

    [Test]
    public void NetSim_Vote_RejectsLivingPlayersAndNonGhostCandidates()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out _, out _);
        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        Assert.IsFalse(sim.TryVote(1, 2));   // Yazan yaşıyor, oy hakkı yok
        Assert.IsFalse(sim.TryVote(2, 3));   // 3 numara hayalet değil
        Assert.IsFalse(sim.TryVote(100, 2)); // Bot oy vermez
        Assert.IsTrue(sim.IsVoting);
    }

    [Test]
    public void NetSim_Vote_TimeoutPicksARandomGhost()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out PlayerState humanGhost, out PlayerState botGhost);
        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        sim.Tick(config.reviveVoteSeconds + 0.1f);

        Assert.IsFalse(sim.IsVoting);
        Assert.AreEqual(1, (humanGhost.IsGhost ? 0 : 1) + (botGhost.IsGhost ? 0 : 1)); // Tam biri döndü
    }

    [Test]
    public void NetSim_OnlyBotGhosts_RevivesImmediately()
    {
        var writer = new PlayerState(1, "Yazan", 3);
        var botGhost = Dead(100, bot: true);
        var avatars = new FakeAvatars();
        avatars.Positions[1] = Vector2.zero; avatars.Positions[100] = Vector2.one;
        var sim = new NetMatchSim(config, new MatchState(new[] { writer, botGhost }), avatars, new System.Random(1));

        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        Assert.IsFalse(sim.IsVoting);
        Assert.IsFalse(botGhost.IsGhost);
    }

    [Test]
    public void NetSim_NoGhosts_ReviveWordFizzles_AndSecondRequestDuringVoteIsIgnored()
    {
        var a = new PlayerState(1, "A", 3);
        var b = new PlayerState(2, "B", 3);
        var avatars = new FakeAvatars();
        avatars.Positions[1] = Vector2.zero; avatars.Positions[2] = Vector2.one;
        var sim = new NetMatchSim(config, new MatchState(new[] { a, b }), avatars, new System.Random(1));
        sim.OnWordPlayed(a, Word("yeniden_canlanma"));
        Assert.IsFalse(sim.IsVoting);

        NetMatchSim sim2 = BuildSim(out PlayerState writer, out _, out _);
        int started = 0;
        sim2.VoteStarted += _ => started++;
        sim2.OnWordPlayed(writer, Word("yeniden_canlanma"));
        sim2.OnWordPlayed(writer, Word("yeniden_canlanma")); // İkincisi boşa gider

        Assert.AreEqual(1, started);
    }

    [Test]
    public void NetSim_MatchOver_CancelsTheVote()
    {
        NetMatchSim sim = BuildSim(out PlayerState writer, out _, out _);
        int ended = -2;
        sim.VoteEnded += id => ended = id;
        sim.OnWordPlayed(writer, Word("yeniden_canlanma"));

        writer.Health.TakeDamage(99);
        // Maç, hayatta tek ya da hiç oyuncu kalınca biter: yalnızca "Diğer" yaşıyor.
        sim.Tick(0.05f);

        Assert.AreEqual(-1, ended);
        Assert.IsFalse(sim.IsVoting);
    }
}
