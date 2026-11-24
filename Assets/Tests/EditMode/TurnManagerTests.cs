using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>TurnManager testleri: tur akışı, kelime doğrulama, süre dolması, eleme ve maç sonu.</summary>
public class TurnManagerTests
{
    // Süreler GameConfig varsayılanlarından: zar 2 sn, yazma 10 sn, sonuç 15-20 sn.
    const string WordsJson = @"{
  ""blocked"": [""yasakli""],
  ""words"": [
    { ""id"": ""kurt"", ""display"": ""Kurt"", ""aliases"": [""wolf""], ""stats"": { ""duration"": 8 } },
    { ""id"": ""orta"", ""display"": ""Orta"", ""aliases"": [], ""stats"": { ""duration"": 17 } },
    { ""id"": ""kaplumbaga"", ""display"": ""Kaplumbağa"", ""aliases"": [], ""stats"": { ""duration"": 30 } },
    { ""id"": ""ucuncu"", ""display"": ""Üçüncü"", ""aliases"": [], ""stats"": { ""duration"": 16 } }
  ] }";

    GameConfig config;
    MatchState match;
    MatchWordTracker words;
    TurnManager turns;

    [SetUp]
    public void SetUp()
    {
        Build(playerCount: 4, seed: 1234);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) UnityEngine.Object.DestroyImmediate(config);
    }

    void Build(int playerCount, int seed)
    {
        if (config != null) UnityEngine.Object.DestroyImmediate(config);
        config = ScriptableObject.CreateInstance<GameConfig>();

        var players = new List<PlayerState>();
        for (int i = 1; i <= playerCount; i++)
            players.Add(new PlayerState(i, "P" + i, config.startingHealth));

        match = new MatchState(players);
        words = new MatchWordTracker(WordDatabase.FromJson(WordsJson, config.maxWordLength));
        turns = new TurnManager(config, match, new Dice(new System.Random(seed), config.diceBonusPerMissedTurn), words);
    }

    /// <summary>Zarı geçip yazma aşamasına gelir.</summary>
    void GoToTyping()
    {
        turns.Start();
        turns.Tick(config.diceSeconds);
        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
    }

    static void Kill(PlayerState player) => player.Health.TakeDamage(999);

    // ---- Deneme paneli ----

    [Test]
    public void DebugPlay_PlaysForAnyAlivePlayer_WithoutLockingTheWord()
    {
        GoToTyping();
        PlayerState other = match.Players[3];
        WordEntry wolf = WordDatabase.FromJson(WordsJson, config.maxWordLength).Lookup("kurt").Entry;
        PlayerState playedBy = null;
        turns.WordPlayed += (writer, word) => playedBy = writer;

        Assert.IsTrue(turns.DebugPlay(other, wolf));
        Assert.AreEqual(other, playedBy);
        Assert.AreEqual(TurnPhase.Result, turns.Phase);
        Assert.AreEqual(0, words.UsedWords.Count, "kilitlenmez: maçta yine yazılabilir");

        Kill(match.Players[0]);
        Assert.IsFalse(turns.DebugPlay(match.Players[0], wolf), "elenen yazamaz");
    }

    // ---- Başlangıç ----

    [Test]
    public void Start_BeginsDicePhase_WithAliveWriter()
    {
        turns.Start();

        Assert.AreEqual(TurnPhase.Dice, turns.Phase);
        Assert.AreEqual(1, turns.TurnNumber);
        Assert.IsNotNull(turns.Writer);
        Assert.IsTrue(turns.Writer.IsAlive);
        Assert.AreEqual(config.diceSeconds, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void Start_FiresWriterSelectedAndPhaseChanged()
    {
        PlayerState selected = null;
        var phases = new List<TurnPhase>();
        turns.WriterSelected += p => selected = p;
        turns.PhaseChanged += p => phases.Add(p);

        turns.Start();

        Assert.AreSame(turns.Writer, selected);
        CollectionAssert.AreEqual(new[] { TurnPhase.Dice }, phases);
    }

    [Test]
    public void Start_WithOnlyOnePlayer_EndsMatchImmediately()
    {
        Build(playerCount: 1, seed: 1);
        PlayerState winner = null;
        bool ended = false;
        turns.MatchEnded += w => { ended = true; winner = w; };

        turns.Start();

        Assert.AreEqual(TurnPhase.MatchOver, turns.Phase);
        Assert.IsTrue(ended);
        Assert.AreSame(match.Players[0], winner);
    }

    [Test]
    public void Start_Twice_IsIgnored()
    {
        turns.Start();
        var writer = turns.Writer;
        turns.Start();

        Assert.AreEqual(1, turns.TurnNumber);
        Assert.AreSame(writer, turns.Writer);
    }

    [Test]
    public void Tick_BeforeStart_DoesNothing()
    {
        turns.Tick(100f);
        Assert.AreEqual(TurnPhase.NotStarted, turns.Phase);
        Assert.AreEqual(0, turns.TurnNumber);
    }

    // ---- Aşama geçişleri ----

    [Test]
    public void Tick_AfterDiceSeconds_MovesToTyping()
    {
        turns.Start();
        turns.Tick(config.diceSeconds);

        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
        Assert.AreEqual(config.typingSeconds, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void Tick_PartialTime_KeepsPhaseAndReducesTimeLeft()
    {
        turns.Start();
        turns.Tick(0.5f);

        Assert.AreEqual(TurnPhase.Dice, turns.Phase);
        Assert.AreEqual(config.diceSeconds - 0.5f, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void Tick_LeftoverTimeCarriesIntoNextPhase()
    {
        turns.Start();
        turns.Tick(config.diceSeconds + 3f); // Zarı geçer, yazmada 3 sn harcar.

        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
        Assert.AreEqual(config.typingSeconds - 3f, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void Tick_NegativeDelta_IsTreatedAsZero()
    {
        turns.Start();
        turns.Tick(-5f);
        Assert.AreEqual(config.diceSeconds, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void PhaseChanged_FollowsDiceTypingResultOrder()
    {
        var phases = new List<TurnPhase>();
        turns.PhaseChanged += p => phases.Add(p);

        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "kurt");

        CollectionAssert.AreEqual(new[] { TurnPhase.Dice, TurnPhase.Typing, TurnPhase.Result }, phases);
    }

    // ---- Kelime gönderme ----

    [Test]
    public void SubmitWord_DuringDice_IsWrongPhase()
    {
        turns.Start();
        Assert.AreEqual(SubmitStatus.WrongPhase, turns.SubmitWord(turns.Writer.Id, "kurt"));
    }

    [Test]
    public void SubmitWord_FromPlayerWhoIsNotWriter_IsRejected()
    {
        GoToTyping();
        PlayerState other = null;
        foreach (PlayerState p in match.Players)
            if (p != turns.Writer) { other = p; break; }

        Assert.AreEqual(SubmitStatus.NotWriter, turns.SubmitWord(other.Id, "kurt"));
        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
        Assert.IsFalse(words.IsUsed("kurt"));
    }

    [Test]
    public void SubmitWord_UnknownPlayerId_IsRejected()
    {
        GoToTyping();
        Assert.AreEqual(SubmitStatus.NotWriter, turns.SubmitWord(9999, "kurt"));
    }

    [Test]
    public void SubmitWord_KnownWord_StartsResultAndFiresEvent()
    {
        GoToTyping();
        PlayerState who = null;
        WordEntry played = null;
        turns.WordPlayed += (p, e) => { who = p; played = e; };

        var status = turns.SubmitWord(turns.Writer.Id, "KURT");

        Assert.AreEqual(SubmitStatus.Accepted, status);
        Assert.AreEqual(TurnPhase.Result, turns.Phase);
        Assert.AreEqual("kurt", turns.CurrentWord.id);
        Assert.AreSame(turns.Writer, who);
        Assert.AreEqual("kurt", played.id);
        Assert.IsTrue(words.IsUsed("kurt"));
    }

    [TestCase("kurt", 15f)]        // Süre 8 → alt sınır 15'e yükselir
    [TestCase("orta", 17f)]        // 17 → aynen
    [TestCase("kaplumbaga", 20f)]  // 30 → üst sınır 20'ye iner
    public void SubmitWord_ResultSeconds_AreClampedToConfigRange(string word, float expected)
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, word);
        Assert.AreEqual(expected, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void SubmitWord_UnknownWord_IsAcceptedWithMinResultTime_AndNoEntry()
    {
        GoToTyping();
        bool fired = false;
        WordEntry played = new WordEntry();
        turns.WordPlayed += (p, e) => { fired = true; played = e; };

        var status = turns.SubmitWord(turns.Writer.Id, "blablabla");

        Assert.AreEqual(SubmitStatus.AcceptedUnknown, status);
        Assert.AreEqual(TurnPhase.Result, turns.Phase);
        Assert.IsTrue(fired);
        Assert.IsNull(played);
        Assert.IsNull(turns.CurrentWord);
        Assert.AreEqual(config.resultSecondsMin, turns.PhaseTimeLeft, 0.0001f);
    }

    [TestCase("", SubmitStatus.Empty)]
    [TestCase("   ", SubmitStatus.Empty)]
    [TestCase("😀", SubmitStatus.Empty)]
    [TestCase("yasaklı", SubmitStatus.Blocked)]
    public void SubmitWord_InvalidInput_IsRejectedAndTypingContinues(string text, SubmitStatus expected)
    {
        GoToTyping();
        var timeBefore = turns.PhaseTimeLeft;

        Assert.AreEqual(expected, turns.SubmitWord(turns.Writer.Id, text));
        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
        Assert.AreEqual(timeBefore, turns.PhaseTimeLeft, 0.0001f);
    }

    [Test]
    public void SubmitWord_TooLongInput_IsRejected()
    {
        GoToTyping();
        Assert.AreEqual(SubmitStatus.TooLong, turns.SubmitWord(turns.Writer.Id, new string('a', config.maxWordLength + 1)));
        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
    }

    [Test]
    public void SubmitWord_RejectedWordCanBeRetried()
    {
        GoToTyping();
        Assert.AreEqual(SubmitStatus.Empty, turns.SubmitWord(turns.Writer.Id, ""));
        Assert.AreEqual(SubmitStatus.Accepted, turns.SubmitWord(turns.Writer.Id, "kurt"));
    }

    [Test]
    public void SubmitWord_DuringResult_IsWrongPhase()
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "kurt");
        Assert.AreEqual(SubmitStatus.WrongPhase, turns.SubmitWord(turns.Writer.Id, "orta"));
    }

    [Test]
    public void SubmitWord_WordUsedInEarlierTurn_IsAlreadyUsed_IncludingSynonym()
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "kurt");
        turns.Tick(turns.PhaseTimeLeft); // Sonuç biter → yeni tur, zar aşaması
        turns.Tick(config.diceSeconds);  // Yeni tur, yazma aşaması

        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
        Assert.AreEqual(2, turns.TurnNumber);
        Assert.AreEqual(SubmitStatus.AlreadyUsed, turns.SubmitWord(turns.Writer.Id, "kurt"));
        Assert.AreEqual(SubmitStatus.AlreadyUsed, turns.SubmitWord(turns.Writer.Id, "WOLF"));
        Assert.AreEqual(TurnPhase.Typing, turns.Phase);
    }

    // ---- Süre dolması ----

    [Test]
    public void Typing_TimeRunsOut_SkipsTurnAndStartsNextDice()
    {
        GoToTyping();
        var firstWriter = turns.Writer;
        PlayerState skipped = null;
        turns.TurnSkipped += p => skipped = p;

        turns.Tick(config.typingSeconds);

        Assert.AreSame(firstWriter, skipped);
        Assert.AreEqual(TurnPhase.Dice, turns.Phase);
        Assert.AreEqual(2, turns.TurnNumber);
        Assert.IsNull(turns.CurrentWord);
    }

    [Test]
    public void Result_TimeRunsOut_StartsNextTurn()
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "orta");
        turns.Tick(17f);

        Assert.AreEqual(TurnPhase.Dice, turns.Phase);
        Assert.AreEqual(2, turns.TurnNumber);
    }

    [Test]
    public void Tick_HugeDelta_DoesNotHangAndKeepsRunning()
    {
        turns.Start();
        turns.Tick(100000f);

        Assert.IsTrue(turns.Phase == TurnPhase.Dice || turns.Phase == TurnPhase.Typing);
        Assert.Greater(turns.TurnNumber, 1);
        Assert.LessOrEqual(turns.TurnNumber, 101);
    }

    // ---- Zar sayaçları ----

    [Test]
    public void Dice_WriterMissedTurnsReset_OthersIncrease()
    {
        turns.Start();

        foreach (PlayerState p in match.Players)
            Assert.AreEqual(p == turns.Writer ? 0 : 1, p.MissedTurns);
    }

    [Test]
    public void Dice_MissedTurnsAccumulateOverTurns()
    {
        turns.Start();
        var firstWriter = turns.Writer;
        turns.Tick(config.diceSeconds + config.typingSeconds); // Yazılmadı, tur yandı, ikinci zar.
        var secondWriter = turns.Writer;

        int expected = secondWriter == firstWriter ? 0 : 1;
        Assert.AreEqual(expected, firstWriter.MissedTurns);
    }

    [Test]
    public void Dice_NeverSelectsEliminatedPlayers()
    {
        Kill(match.Players[0]);
        turns.Start();

        for (int i = 0; i < 60; i++)
        {
            Assert.AreNotSame(match.Players[0], turns.Writer);
            turns.Tick(config.diceSeconds + config.typingSeconds);
        }
    }

    // ---- Eleme ve maç sonu ----

    [Test]
    public void Typing_WriterEliminatedWhileTyping_TurnIsSkippedOnNextTick()
    {
        GoToTyping();
        var writer = turns.Writer;
        PlayerState skipped = null;
        turns.TurnSkipped += p => skipped = p;

        Kill(writer);
        turns.Tick(0.1f);

        Assert.AreSame(writer, skipped);
        Assert.AreEqual(TurnPhase.Dice, turns.Phase);
        Assert.AreNotSame(writer, turns.Writer);
    }

    [Test]
    public void SubmitWord_FromEliminatedWriter_IsRejected()
    {
        GoToTyping();
        var writer = turns.Writer;
        Kill(writer);

        Assert.AreEqual(SubmitStatus.WrongPhase, turns.SubmitWord(writer.Id, "kurt"));
        Assert.IsFalse(words.IsUsed("kurt"));
    }

    [Test]
    public void MatchEnds_WhenOnlyOnePlayerRemains_DuringResult()
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "kurt");

        PlayerState survivor = match.Players[2];
        foreach (PlayerState p in match.Players)
            if (p != survivor) Kill(p);

        PlayerState announced = null;
        turns.MatchEnded += w => announced = w;
        turns.Tick(0.1f);

        Assert.AreEqual(TurnPhase.MatchOver, turns.Phase);
        Assert.AreSame(survivor, turns.Winner);
        Assert.AreSame(survivor, announced);
    }

    [Test]
    public void MatchEnds_WithNoWinner_WhenEveryoneIsEliminatedTogether()
    {
        GoToTyping();
        turns.SubmitWord(turns.Writer.Id, "kurt");
        foreach (PlayerState p in match.Players) Kill(p);

        bool ended = false;
        PlayerState announced = match.Players[0];
        turns.MatchEnded += w => { ended = true; announced = w; };
        turns.Tick(0.1f);

        Assert.AreEqual(TurnPhase.MatchOver, turns.Phase);
        Assert.IsTrue(ended);
        Assert.IsNull(announced);
        Assert.IsNull(turns.Winner);
    }

    [Test]
    public void MatchEnds_OnlyOnce_AndStopsTicking()
    {
        turns.Start();
        int endCount = 0;
        turns.MatchEnded += w => endCount++;

        foreach (PlayerState p in match.Players)
            if (p != match.Players[0]) Kill(p);

        turns.Tick(1f);
        turns.Tick(1f);
        turns.Tick(50f);

        Assert.AreEqual(1, endCount);
        Assert.AreEqual(TurnPhase.MatchOver, turns.Phase);
        Assert.AreSame(match.Players[0], turns.Winner);
    }

    [Test]
    public void SubmitWord_AfterMatchOver_IsWrongPhase()
    {
        turns.Start();
        foreach (PlayerState p in match.Players)
            if (p != match.Players[0]) Kill(p);
        turns.Tick(1f);

        Assert.AreEqual(SubmitStatus.WrongPhase, turns.SubmitWord(match.Players[0].Id, "kurt"));
    }

    [Test]
    public void SameSeed_GivesSameWriterSequence()
    {
        var first = new List<int>();
        Build(4, seed: 77);
        turns.Start();
        for (int i = 0; i < 20; i++) { first.Add(turns.Writer.Id); turns.Tick(config.diceSeconds + config.typingSeconds); }

        var second = new List<int>();
        Build(4, seed: 77);
        turns.Start();
        for (int i = 0; i < 20; i++) { second.Add(turns.Writer.Id); turns.Tick(config.diceSeconds + config.typingSeconds); }

        CollectionAssert.AreEqual(first, second);
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var dice = new Dice(new System.Random(1), 0.5f);
        Assert.Throws<ArgumentNullException>(() => new TurnManager(null, match, dice, words));
        Assert.Throws<ArgumentNullException>(() => new TurnManager(config, null, dice, words));
        Assert.Throws<ArgumentNullException>(() => new TurnManager(config, match, null, words));
        Assert.Throws<ArgumentNullException>(() => new TurnManager(config, match, dice, null));
    }
}
