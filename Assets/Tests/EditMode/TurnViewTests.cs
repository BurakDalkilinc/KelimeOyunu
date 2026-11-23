using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Ortak tur görünümü testleri: TurnManager ve NetTurnClient aynı arayüzü uygular; durum yazıları; istemci sıfırlama.</summary>
public class TurnViewTests
{
    const string WordsJson = @"{ ""blocked"": [], ""words"": [
        { ""id"": ""kurt"", ""display"": ""Kurt"", ""aliases"": [], ""stats"": { ""duration"": 8 } } ] }";

    GameConfig config;
    WordDatabase words;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.FromJson(WordsJson, config.maxWordLength);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    [Test]
    public void TurnManagerAndNetTurnClient_BothImplementTheSameView()
    {
        var match = new MatchState(new[] { new PlayerState(1, "A", 3), new PlayerState(2, "B", 3) });
        ITurnView local = new TurnManager(config, match, new Dice(new System.Random(1), 0.5f), new MatchWordTracker(words));
        ITurnView net = new NetTurnClient(words);

        Assert.AreEqual(TurnPhase.NotStarted, local.Phase);
        Assert.AreEqual(TurnPhase.NotStarted, net.Phase);
        Assert.IsNull(local.Writer);
        Assert.IsNull(net.Writer);
    }

    // ---- NetTurnClient.Reset ----

    [Test]
    public void Reset_ClearsPlayersWordsAndPhase_ButKeepsSubscriptions()
    {
        var client = new NetTurnClient(words);
        client.SetRoster(new[] { new RosterEntry(1, "A", false), new RosterEntry(2, "B", false) }, 3);
        client.Apply(new TurnMessage { Kind = TurnMessageKind.WordPlayed, Phase = TurnPhase.Result, TurnNumber = 2, WriterId = 1, WordId = "kurt", WinnerId = TurnMessage.NoPlayer });
        var phases = new List<TurnPhase>();
        client.PhaseChanged += p => phases.Add(p);

        client.Reset();

        Assert.AreEqual(TurnPhase.NotStarted, client.Phase);
        Assert.AreEqual(0, client.Players.Count);
        Assert.AreEqual(0, client.PlayedWords.Count);
        Assert.AreEqual(0, client.TurnNumber);
        Assert.IsNull(client.Writer);
        Assert.AreEqual(new[] { TurnPhase.NotStarted }, phases.ToArray()); // Abonelik yaşıyor, ekran haberdar oldu
    }

    [Test]
    public void Reset_WhenAlreadyNotStarted_RaisesNoEvent()
    {
        var client = new NetTurnClient(words);
        int calls = 0;
        client.PhaseChanged += _ => calls++;

        client.Reset();

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Reset_AfterMatchOver_LetsTheEndScreenClose()
    {
        var client = new NetTurnClient(words);
        client.SetRoster(new[] { new RosterEntry(1, "A", false) }, 3);
        client.Apply(new TurnMessage { Kind = TurnMessageKind.MatchEnded, Phase = TurnPhase.MatchOver, WriterId = TurnMessage.NoPlayer, WinnerId = 1 });
        TurnPhase last = TurnPhase.MatchOver;
        client.PhaseChanged += p => last = p;

        client.Reset();

        Assert.AreEqual(TurnPhase.NotStarted, last);
        Assert.IsNull(client.Winner);
    }

    // ---- StatusText ----

    [Test]
    public void StatusText_NoEffects_IsEmpty()
    {
        Assert.AreEqual("", StatusText.Describe(new StatusEffects()));
        Assert.AreEqual("", StatusText.Describe((StatusEffects)null));
        Assert.AreEqual("", StatusText.Describe(StatusFlags.None));
    }

    [Test]
    public void StatusText_WithTimes_ShowsRemainingSeconds()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 2f);
        status.ApplyShield(1, 10f);

        string text = StatusText.Describe(status);

        StringAssert.Contains("Yavaşladın! (2 sn)", text);
        StringAssert.Contains("Kalkan var", text);
    }

    [Test]
    public void StatusText_FromFlags_HasNoSeconds()
    {
        string text = StatusText.Describe(StatusFlags.Slow | StatusFlags.Stealth | StatusFlags.Flight | StatusFlags.Pull | StatusFlags.Shield);

        StringAssert.Contains("Yavaşladın!", text);
        StringAssert.Contains("Görünmezsin", text);
        StringAssert.Contains("Uçuyorsun", text);
        StringAssert.Contains("Çekiliyorsun!", text);
        StringAssert.Contains("Kalkan var", text);
        StringAssert.DoesNotContain("sn", text);
    }

    // ---- Sürüm: oyuncunun yerel olup olmadığı ----

    sealed class FakeSource : TurnViewSource
    {
        public PlayerState Local;
        public List<PlayerState> All = new List<PlayerState>();

        public override ITurnView View => null;
        public override IReadOnlyList<PlayerState> Players => All;
        public override IReadOnlyList<WordEntry> UsedWords => new List<WordEntry>();
        public override PlayerState LocalPlayer => Local;
        public override string DescribeLocalStatus() => "";
        public override void SubmitWord(string text, System.Action<SubmitStatus> onResult) { }
        public override bool CanRestart => false;
        public override void Restart() { }
    }

    [Test]
    public void TurnViewSource_IsLocal_AndAlivePlayers()
    {
        var go = new GameObject("kaynak");
        try
        {
            var source = go.AddComponent<FakeSource>();
            var me = new PlayerState(1, "Ben", 3);
            var other = new PlayerState(2, "Diğer", 3);
            var dead = new PlayerState(3, "Ölü", 3);
            dead.Health.TakeDamage(99);
            source.Local = me;
            source.All.AddRange(new[] { me, other, dead });

            Assert.IsTrue(source.IsLocal(me));
            Assert.IsFalse(source.IsLocal(other));
            Assert.IsFalse(source.IsLocal(null));

            var alive = new List<PlayerState>(source.AlivePlayers);
            Assert.AreEqual(2, alive.Count);
            Assert.IsFalse(alive.Contains(dead));
            Assert.AreEqual(MatchSummary.GetHint(), source.RestartHint);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
