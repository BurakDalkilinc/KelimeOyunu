using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Kopan oyuncunun bota devredilmesi testleri (ağsız).</summary>
public class NetBotTakeoverTests
{
    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public bool TryGetPosition(int id, out Vector2 p) => Positions.TryGetValue(id, out p);
        public bool TryApplyKnockback(int id, Vector2 d) => Positions.ContainsKey(id);
        public bool TryGetFacing(int id, out Vector2 d) { d = Vector2.up; return false; }
    }

    GameConfig config;
    WordDatabase words;
    NetTurnHost host;
    NetTurnClient client;
    List<TurnMessage> sent;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.LoadFromResources(config.maxWordLength);
        sent = new List<TurnMessage>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    /// <summary>İki insan (1, 2) ve bir bot (100); host mesajları istemciye doğrudan verilir.</summary>
    void Build(int seed = 3)
    {
        var roster = new List<RosterEntry>
        {
            new RosterEntry(1, "A", false), new RosterEntry(2, "B", false), new RosterEntry(100, "Bot", true),
        };
        host = new NetTurnHost(config, words, new System.Random(seed), roster);
        client = new NetTurnClient(words);
        client.SetRoster(roster, config.startingHealth);
        host.MessageReady += m => { sent.Add(m); client.Apply(m); };
    }

    /// <summary>Sırası belli bir insana gelene kadar tur atlatır (zar tohumuna bağlı olmasın diye).</summary>
    void AdvanceUntilHumanWriter(int playerId)
    {
        host.Start();
        for (int guard = 0; guard < 200 && !(host.Turns.Writer.Id == playerId && host.Turns.Phase == TurnPhase.Typing); guard++)
        {
            if (host.Turns.Phase == TurnPhase.Typing) host.Turns.SubmitWord(host.Turns.Writer.Id, "zzzz" + guard); // Sırayı geçir
            host.Tick(0.5f);
        }
        Assert.AreEqual(playerId, host.Turns.Writer.Id);
        Assert.AreEqual(TurnPhase.Typing, host.Turns.Phase);
    }

    [Test]
    public void PlayerState_BecomeBot_FlipsTheFlag()
    {
        var player = new PlayerState(1, "A", 3);
        Assert.IsFalse(player.IsBot);

        player.BecomeBot();

        Assert.IsTrue(player.IsBot);
    }

    [Test]
    public void ReplaceWithBot_UnknownOrAlreadyBot_ReturnsFalse()
    {
        Build();

        Assert.IsFalse(host.ReplaceWithBot(999));
        Assert.IsFalse(host.ReplaceWithBot(100));
    }

    [Test]
    public void ReplaceWithBot_TurnsTheHumanIntoABot()
    {
        Build();
        host.Start();

        Assert.IsTrue(host.ReplaceWithBot(1));

        Assert.IsTrue(host.Match.Players[0].IsBot);
        Assert.IsFalse(host.ReplaceWithBot(1)); // İkinci kez bir şey yapmaz
    }

    [Test]
    public void DisconnectedWriter_MidTyping_TheBotTypesTheWordForThem()
    {
        Build();
        AdvanceUntilHumanWriter(1);
        sent.Clear();

        host.ReplaceWithBot(1); // Yazarken koptu
        host.Tick(config.botTypingDelayMax + 0.1f);

        Assert.AreEqual(TurnPhase.Result, host.Turns.Phase); // Tur yanmadı, bot kelimeyi yazdı
        Assert.IsTrue(sent.Exists(m => m.Kind == TurnMessageKind.WordPlayed));
    }

    [Test]
    public void DisconnectedPlayer_WhoLaterGetsTheTurn_IsTypedForByTheBot()
    {
        Build();
        host.Start();
        host.ReplaceWithBot(1);
        host.ReplaceWithBot(2);

        // Üç botla maç kendi kendine akar: yazma aşamasında takılmadan sonuç aşamasına gelir.
        bool reachedResult = false;
        for (int i = 0; i < 400 && !reachedResult; i++)
        {
            host.Tick(0.1f);
            reachedResult = host.Turns.Phase == TurnPhase.Result;
        }

        Assert.IsTrue(reachedResult);
    }

    [Test]
    public void ReplaceWithBot_DuringTheDicePhase_StillTypesWhenTheTypingStarts()
    {
        // İlk yazan insan olan bir tohum bul (zar rastgele).
        int writerId = 100;
        for (int seed = 1; seed < 60 && writerId == 100; seed++)
        {
            sent.Clear();
            Build(seed);
            host.Start();
            writerId = host.Turns.Writer.Id;
        }
        Assert.AreNotEqual(100, writerId);

        host.ReplaceWithBot(writerId); // Zar atılırken koptu
        host.Tick(config.diceSeconds + config.botTypingDelayMax + 0.2f);

        Assert.AreEqual(TurnPhase.Result, host.Turns.Phase);
    }

    [Test]
    public void ReplaceWithBot_AnnouncesAStateMessage()
    {
        Build();
        host.Start();
        sent.Clear();

        host.ReplaceWithBot(2);

        Assert.AreEqual(1, sent.Count);
        Assert.AreEqual(TurnMessageKind.State, sent[0].Kind);
    }

    [Test]
    public void Client_MarkBot_UpdatesThePlayer_AndIgnoresUnknownIds()
    {
        Build();

        client.MarkBot(2);
        client.MarkBot(999);

        Assert.IsTrue(client.Players[2].IsBot);
        Assert.IsFalse(client.Players[1].IsBot);
    }

    // ---- Hedef seçerken kopma ----

    [Test]
    public void DisconnectWhileChoosingATarget_CompletesTheAttackRandomly()
    {
        var human = new PlayerState(1, "A", config.startingHealth);
        var other = new PlayerState(2, "B", config.startingHealth);
        var avatars = new FakeAvatars();
        avatars.Positions[1] = new Vector2(-5f, 0f);
        avatars.Positions[2] = Vector2.zero;
        var sim = new NetMatchSim(config, new MatchState(new[] { human, other }), avatars, new System.Random(2));
        WordEntry kurt = null;
        foreach (WordEntry w in words.Words) if (w.id == "kurt") kurt = w;

        sim.OnWordPlayed(human, kurt);
        Assert.IsTrue(sim.IsSelectingTarget);
        bool ended = false;
        sim.TargetingEnded += () => ended = true;

        human.BecomeBot();
        sim.OnPlayerBecameBot(1);

        Assert.IsTrue(ended);
        Assert.IsFalse(sim.IsSelectingTarget);
        Assert.AreEqual(1, sim.CreatureCount);
    }

    [Test]
    public void DisconnectOfSomeoneElse_DoesNotTouchTheSelection()
    {
        var human = new PlayerState(1, "A", config.startingHealth);
        var other = new PlayerState(2, "B", config.startingHealth);
        var avatars = new FakeAvatars();
        avatars.Positions[1] = new Vector2(-5f, 0f);
        avatars.Positions[2] = Vector2.zero;
        var sim = new NetMatchSim(config, new MatchState(new[] { human, other }), avatars, new System.Random(2));
        WordEntry kurt = null;
        foreach (WordEntry w in words.Words) if (w.id == "kurt") kurt = w;

        sim.OnWordPlayed(human, kurt);
        sim.OnPlayerBecameBot(2);

        Assert.IsTrue(sim.IsSelectingTarget);
        Assert.AreEqual(0, sim.CreatureCount);
    }

    // ---- Maç sonu özeti ----

    [Test]
    public void Client_KeepsThePlayedWords_ForTheMatchSummary()
    {
        Build();
        AdvanceUntilHumanWriter(1);

        host.Submit(1, "kurt");

        Assert.AreEqual(1, client.PlayedWords.Count);
        Assert.AreEqual("kurt", client.PlayedWords[0].id);
        StringAssert.Contains("Kurt", MatchSummary.GetWordsText(client.PlayedWords));
    }

    [Test]
    public void Client_UnknownWords_AreNotInTheSummary()
    {
        Build();
        AdvanceUntilHumanWriter(1);

        host.Submit(1, "zzzzqqq");

        Assert.AreEqual(0, client.PlayedWords.Count);
    }
}
