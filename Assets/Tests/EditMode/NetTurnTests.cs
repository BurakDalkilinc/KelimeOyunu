using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Ağ tur katmanı testleri: host kuralları, istemci kopyası, mesajların uçtan uca akışı (ağsız).</summary>
public class NetTurnTests
{
    const string WordsJson = @"{
  ""blocked"": [],
  ""words"": [
    { ""id"": ""kurt"", ""display"": ""Kurt"", ""aliases"": [""wolf""], ""stats"": { ""duration"": 8 } },
    { ""id"": ""ari"", ""display"": ""Arı"", ""aliases"": [], ""stats"": { ""duration"": 8 } }
  ] }";

    GameConfig config;
    WordDatabase words;
    NetTurnHost host;
    NetTurnClient client;
    List<TurnMessage> sent;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.FromJson(WordsJson, config.maxWordLength);
        sent = new List<TurnMessage>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static List<RosterEntry> Roster(params (int id, bool bot)[] players)
    {
        var list = new List<RosterEntry>();
        foreach (var p in players) list.Add(new RosterEntry(p.id, "P" + p.id, p.bot));
        return list;
    }

    /// <summary>Host ve istemciyi kurar; host'un mesajları istemciye doğrudan verilir (ağ yerine).</summary>
    void Build(List<RosterEntry> roster, int seed = 7)
    {
        host = new NetTurnHost(config, words, new System.Random(seed), roster);
        client = new NetTurnClient(words);
        client.SetRoster(roster, config.startingHealth);
        host.MessageReady += m => { sent.Add(m); client.Apply(m); };
    }

    // ---- RosterCodec ----

    [Test]
    public void RosterCodec_RoundTrips()
    {
        var roster = new List<RosterEntry> { new RosterEntry(1, "Ayşe", false), new RosterEntry(100, "Bot 1", true) };

        Assert.IsTrue(RosterCodec.TryDecode(RosterCodec.Encode(roster), out List<RosterEntry> back));

        Assert.AreEqual(2, back.Count);
        Assert.AreEqual(1, back[0].Id);
        Assert.AreEqual("Ayşe", back[0].Name);
        Assert.IsFalse(back[0].IsBot);
        Assert.AreEqual(100, back[1].Id);
        Assert.IsTrue(back[1].IsBot);
    }

    [Test]
    public void RosterCodec_StripsSeparatorsFromNames()
    {
        var roster = new List<RosterEntry> { new RosterEntry(1, "a;b|c", false) };

        Assert.IsTrue(RosterCodec.TryDecode(RosterCodec.Encode(roster), out List<RosterEntry> back));
        Assert.AreEqual(1, back.Count);
        Assert.AreEqual("a b c", back[0].Name);
    }

    [TestCase("1|x")]
    [TestCase("a|x|0")]
    [TestCase("1|x|2")]
    [TestCase("1|x|0;1|y|0")]
    public void RosterCodec_RejectsBrokenText(string text)
    {
        Assert.IsFalse(RosterCodec.TryDecode(text, out List<RosterEntry> entries));
        Assert.AreEqual(0, entries.Count);
    }

    [Test]
    public void RosterCodec_EmptyText_IsEmptyList()
    {
        Assert.IsTrue(RosterCodec.TryDecode("", out List<RosterEntry> entries));
        Assert.AreEqual(0, entries.Count);
    }

    // ---- Host ----

    [Test]
    public void Host_Start_PublishesStateThenWriterSelected()
    {
        Build(Roster((1, false), (2, false), (3, false)));

        host.Start();

        Assert.AreEqual(TurnMessageKind.State, sent[0].Kind);
        Assert.AreEqual(TurnPhase.Dice, sent[0].Phase);
        Assert.AreEqual(TurnMessageKind.WriterSelected, sent[1].Kind);
        Assert.AreEqual(host.Turns.Writer.Id, sent[1].WriterId);
        Assert.AreEqual(1, sent[1].TurnNumber);
    }

    [Test]
    public void Host_RejectsWordFromPlayerWhoIsNotTheWriter()
    {
        Build(Roster((1, false), (2, false), (3, false)));
        host.Start();
        host.Tick(config.diceSeconds); // Yazma aşaması

        int writer = host.Turns.Writer.Id;
        int other = writer == 1 ? 2 : 1;

        Assert.AreEqual(SubmitStatus.NotWriter, host.Submit(other, "kurt"));
        Assert.AreEqual(TurnPhase.Typing, host.Turns.Phase); // Tur bozulmadı
        Assert.AreEqual(SubmitStatus.Accepted, host.Submit(writer, "kurt"));
    }

    [Test]
    public void Host_RejectsWordOutsideTypingPhase()
    {
        Build(Roster((1, false), (2, false)));
        host.Start(); // Zar aşaması

        Assert.AreEqual(SubmitStatus.WrongPhase, host.Submit(host.Turns.Writer.Id, "kurt"));
    }

    [Test]
    public void Host_UsedWordCannotBePlayedAgain_EvenBySynonym()
    {
        Build(Roster((1, false), (2, false)));
        host.Start();
        host.Tick(config.diceSeconds);
        host.Submit(host.Turns.Writer.Id, "kurt");

        host.Tick(config.resultSecondsMax + config.diceSeconds); // Yeni tur, yazma aşaması
        Assert.AreEqual(TurnPhase.Typing, host.Turns.Phase);

        Assert.AreEqual(SubmitStatus.AlreadyUsed, host.Submit(host.Turns.Writer.Id, "wolf"));
    }

    [Test]
    public void Host_BotWriter_SubmitsByItself()
    {
        Build(Roster((100, true), (101, true))); // İkisi de bot

        host.Start();
        host.Tick(config.diceSeconds);
        Assert.AreEqual(TurnPhase.Typing, host.Turns.Phase);

        host.Tick(config.botTypingDelayMax + 0.1f);

        Assert.AreEqual(TurnPhase.Result, host.Turns.Phase);
        Assert.IsTrue(sent.Exists(m => m.Kind == TurnMessageKind.WordPlayed));
    }

    [Test]
    public void Host_SendsHeartbeatState_WhileRunning()
    {
        Build(Roster((1, false), (2, false)));
        host.Start();
        sent.Clear();

        host.Tick(NetTurnHost.HeartbeatSeconds * 0.5f); // Henüz erken (zar süresi içinde)
        int before = sent.Count;
        host.Tick(NetTurnHost.HeartbeatSeconds * 0.6f);

        Assert.AreEqual(0, before);
        Assert.AreEqual(1, sent.Count);
        Assert.AreEqual(TurnMessageKind.State, sent[0].Kind);
    }

    // ---- İstemci kopyası ----

    [Test]
    public void Client_MirrorsHostTurnState()
    {
        Build(Roster((1, false), (2, false), (3, false)));

        host.Start();
        host.Tick(config.diceSeconds);

        Assert.AreEqual(host.Turns.Phase, client.Phase);
        Assert.AreEqual(host.Turns.TurnNumber, client.TurnNumber);
        Assert.AreEqual(host.Turns.Writer.Id, client.Writer.Id);
    }

    [Test]
    public void Client_RaisesTheSameEventsAsTheHost()
    {
        Build(Roster((1, false), (2, false)));

        var phases = new List<TurnPhase>();
        PlayerState selected = null;
        WordEntry played = null;
        client.PhaseChanged += p => phases.Add(p);
        client.WriterSelected += w => selected = w;
        client.WordPlayed += (w, entry) => played = entry;

        host.Start();
        host.Tick(config.diceSeconds);
        host.Submit(host.Turns.Writer.Id, "wolf");

        Assert.AreEqual(new[] { TurnPhase.Dice, TurnPhase.Typing, TurnPhase.Result }, phases.ToArray());
        Assert.AreEqual(host.Turns.Writer.Id, selected.Id);
        Assert.AreEqual("kurt", played.id);
        Assert.AreEqual(TurnPhase.Result, client.Phase);
    }

    [Test]
    public void Client_UnknownWord_ArrivesAsNullEntry()
    {
        Build(Roster((1, false), (2, false)));
        bool called = false;
        WordEntry played = words.Words[0];
        client.WordPlayed += (w, entry) => { called = true; played = entry; };

        host.Start();
        host.Tick(config.diceSeconds);
        Assert.AreEqual(SubmitStatus.AcceptedUnknown, host.Submit(host.Turns.Writer.Id, "zzzzqq"));

        Assert.IsTrue(called);
        Assert.IsNull(played);
        Assert.IsNull(client.CurrentWord);
    }

    [Test]
    public void Client_CountsDownBetweenMessages_AndNeverGoesNegative()
    {
        Build(Roster((1, false), (2, false)));
        host.Start();
        float start = client.PhaseTimeLeft;

        client.Tick(0.5f);
        Assert.AreEqual(start - 0.5f, client.PhaseTimeLeft, 0.0001f);

        client.Tick(100f);
        Assert.AreEqual(0f, client.PhaseTimeLeft);
    }

    [Test]
    public void Client_ReportsWinner_WhenHostMatchEnds()
    {
        Build(Roster((1, false), (2, false)));
        PlayerState winner = null;
        bool ended = false;
        client.MatchEnded += w => { ended = true; winner = w; };

        host.Start();
        host.Match.Players[1].Health.TakeDamage(999); // 2 numara elendi
        host.Tick(0.1f);

        Assert.IsTrue(ended);
        Assert.AreEqual(1, winner.Id);
        Assert.AreEqual(TurnPhase.MatchOver, client.Phase);
    }

    [Test]
    public void Client_IgnoresUnknownPlayerIds_WithoutThrowing()
    {
        client = new NetTurnClient(words);
        client.SetRoster(Roster((1, false)), config.startingHealth);

        Assert.DoesNotThrow(() => client.Apply(new TurnMessage
        {
            Kind = TurnMessageKind.WriterSelected, Phase = TurnPhase.Dice, TurnNumber = 1,
            WriterId = 99, PhaseTimeLeft = 2f, WordId = "yok", WinnerId = TurnMessage.NoPlayer,
        }));
        Assert.IsNull(client.Writer);
        Assert.IsNull(client.CurrentWord);
    }
}
