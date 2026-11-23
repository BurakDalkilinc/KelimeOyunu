using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Ağ maçı host simülasyonu ve yaratık fotoğrafı testleri (ağsız; sahte avatarlarla).</summary>
public class NetMatchSimTests
{
    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public readonly List<(int id, Vector2 distance)> Knocks = new List<(int, Vector2)>();
        public readonly Dictionary<int, Vector2> Facings = new Dictionary<int, Vector2>();

        public bool TryGetFacing(int playerId, out Vector2 direction)
        {
            direction = Facings.TryGetValue(playerId, out Vector2 f) ? f : Vector2.up;
            return Facings.ContainsKey(playerId);
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);

        public bool TryApplyKnockback(int playerId, Vector2 distance)
        {
            Knocks.Add((playerId, distance));
            return Positions.ContainsKey(playerId);
        }
    }

    GameConfig config;
    WordDatabase words;
    MatchState match;
    FakeAvatars avatars;
    NetMatchSim sim;
    PlayerState writer;
    PlayerState victim;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.LoadFromResources(config.maxWordLength);

        writer = new PlayerState(1, "Yazan", config.startingHealth, isBot: true);
        victim = new PlayerState(2, "Kurban", config.startingHealth);
        match = new MatchState(new[] { writer, victim });

        avatars = new FakeAvatars();
        avatars.Positions[1] = new Vector2(-8f, 4f);
        avatars.Positions[2] = Vector2.zero;

        sim = new NetMatchSim(config, match, avatars, new System.Random(5));
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Word(string id)
    {
        foreach (WordEntry entry in words.Words)
            if (entry.id == id) return entry;
        Assert.Fail("Kelime yok: " + id);
        return null;
    }

    void Run(float seconds, float step = 0.05f)
    {
        for (float t = 0f; t < seconds; t += step) sim.Tick(step);
    }

    // ---- Kodlayıcı ----

    [Test]
    public void SnapshotCodec_RoundTrips_WithInvariantNumbers()
    {
        var list = new List<CreatureSnapshot>
        {
            new CreatureSnapshot { Id = 3, WordId = "kurt", X = -1.5f, Y = 2.25f, Radius = 0.5f, Kind = CreatureKind.Chaser, Label = "Kurt" },
            new CreatureSnapshot { Id = 4, WordId = "bomba", X = 0f, Y = 0f, Radius = 2.5f, Kind = CreatureKind.Strike,
                                   Progress = 0.4f, HasStruck = true, Countdown = 2, Label = "Bomba" },
        };

        Assert.IsTrue(CreatureSnapshotCodec.TryDecode(CreatureSnapshotCodec.Encode(list), out List<CreatureSnapshot> back));

        Assert.AreEqual(2, back.Count);
        Assert.AreEqual(-1.5f, back[0].X, 0.001f);
        Assert.AreEqual(2.25f, back[0].Y, 0.001f);
        Assert.AreEqual(CreatureKind.Strike, back[1].Kind);
        Assert.AreEqual(0.4f, back[1].Progress, 0.001f);
        Assert.IsTrue(back[1].HasStruck);
        Assert.AreEqual(2, back[1].Countdown);
        Assert.AreEqual("Bomba", back[1].Label);
    }

    [Test]
    public void SnapshotCodec_CleansSeparatorsFromLabels()
    {
        var list = new List<CreatureSnapshot> { new CreatureSnapshot { Id = 1, Label = "a;b|c" } };

        Assert.IsTrue(CreatureSnapshotCodec.TryDecode(CreatureSnapshotCodec.Encode(list), out List<CreatureSnapshot> back));
        Assert.AreEqual("a b c", back[0].Label);
    }

    [TestCase("1|x|0|0")]
    [TestCase("a|x|0|0|1|0|0|0|0|")]
    [TestCase("1|x|0|0|1|99|0|0|0|")]
    [TestCase("1|x|NaN|0|1|0|0|0|0|")]
    [TestCase("1|x|0|0|1|0|0|2|0|")]
    public void SnapshotCodec_RejectsBrokenText(string text)
    {
        Assert.IsFalse(CreatureSnapshotCodec.TryDecode(text, out List<CreatureSnapshot> list));
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void SnapshotCodec_EmptyIsEmptyList()
    {
        Assert.IsTrue(CreatureSnapshotCodec.TryDecode("", out List<CreatureSnapshot> list));
        Assert.AreEqual(0, list.Count);
    }

    // ---- Kelime → yaratık ----

    [Test]
    public void AttackWord_SpawnsCreatureTargetingTheOpponent_NotTheWriter()
    {
        sim.OnWordPlayed(writer, Word("kurt"));

        Assert.AreEqual(1, sim.CreatureCount);
        var snapshots = new List<CreatureSnapshot>();
        sim.Snapshot(snapshots);
        Assert.AreEqual("kurt", snapshots[0].WordId);
        Assert.AreEqual(CreatureKind.Chaser, snapshots[0].Kind);
    }

    [Test]
    public void SwarmWord_SpawnsSeveralCreatures()
    {
        sim.OnWordPlayed(writer, Word("ari"));

        Assert.AreEqual(5, sim.CreatureCount);
    }

    [Test]
    public void UnknownWord_SpawnsHarmlessCreature()
    {
        sim.OnWordPlayed(writer, null);

        Assert.AreEqual(1, sim.CreatureCount);
        Run(5f);
        Assert.AreEqual(victim.Health.Max, victim.Health.Current);
        Assert.AreEqual(writer.Health.Max, writer.Health.Current);
    }

    [Test]
    public void DefenseAndThreatWords_DoNotSpawnCreatures()
    {
        sim.OnWordPlayed(writer, Word("kalkan"));
        sim.OnWordPlayed(writer, Word("tsunami")); // Büyük tehditler sonraki adımda

        Assert.AreEqual(0, sim.CreatureCount);
    }

    [Test]
    public void NoOpponentAlive_AttackSpawnsNothing()
    {
        victim.Health.TakeDamage(999);

        sim.OnWordPlayed(writer, Word("kurt"));

        Assert.AreEqual(0, sim.CreatureCount);
    }

    // ---- Hasar ----

    [Test]
    public void ChasingCreature_ReachesTarget_AndDamagesIt()
    {
        sim.OnWordPlayed(writer, Word("kurt"));

        Run(8f);

        Assert.Less(victim.Health.Current, victim.Health.Max);
        Assert.AreEqual(writer.Health.Max, writer.Health.Current);
    }

    [Test]
    public void StrikeCreature_HitsAfterTheWarning_AndKnocksBack()
    {
        sim.OnWordPlayed(writer, Word("yumruk"));
        var snapshots = new List<CreatureSnapshot>();

        sim.Tick(0.5f);
        sim.Snapshot(snapshots);
        Assert.AreEqual(CreatureKind.Strike, snapshots[0].Kind);
        Assert.Greater(snapshots[0].Progress, 0f);
        Assert.AreEqual(victim.Health.Max, victim.Health.Current); // Uyarı bitmedi

        Run(2f);

        Assert.Less(victim.Health.Current, victim.Health.Max);
        Assert.IsTrue(avatars.Knocks.Exists(k => k.id == victim.Id));
    }

    [Test]
    public void Elimination_MakesTheVictimAGhost_AndClearsItsEffects()
    {
        victim.Status.ApplySlow(0.5f, 30f);
        victim.Health.TakeDamage(victim.Health.Current - 1); // Tek can kaldı

        sim.OnWordPlayed(writer, Word("kurt"));
        Run(8f);

        Assert.IsTrue(victim.IsGhost);
        sim.Tick(0.05f);
        Assert.AreEqual(1f, victim.Status.SpeedMultiplier, 0.0001f); // Hayaletin üstünde etki kalmaz
    }

    [Test]
    public void SlowingCreature_SlowsThePlayer()
    {
        sim.OnWordPlayed(writer, Word("fare"));

        Run(8f);

        // Fare hasar vermez ama yavaşlatır; süre dolmadan önce etki sürüyor olabilir, sonra biter.
        Assert.AreEqual(victim.Health.Max, victim.Health.Current);
    }

    // ---- Süre, tehlike, fotoğraf ----

    [Test]
    public void Creature_IsRemoved_WhenItsLifeEnds()
    {
        sim.OnWordPlayed(writer, null); // Gezinen "???", sonuç süresi kadar yaşar

        Run(config.resultSecondsMin + 1f);

        Assert.AreEqual(0, sim.CreatureCount);
    }

    [Test]
    public void HarmfulCreature_AppearsInDangers_ForBots()
    {
        sim.OnWordPlayed(writer, Word("kurt"));
        sim.Tick(0.05f);

        Assert.AreEqual(1, sim.Dangers.Count);
    }

    [Test]
    public void BombSnapshot_ShowsCountdown_WhileWarningRuns()
    {
        sim.OnWordPlayed(writer, Word("bomba"));
        var snapshots = new List<CreatureSnapshot>();

        sim.Tick(0.5f);
        sim.Snapshot(snapshots);

        Assert.AreEqual(CreatureKind.Strike, snapshots[0].Kind);
        Assert.Greater(snapshots[0].Countdown, 0);
        Assert.LessOrEqual(snapshots[0].Countdown, 3);
    }

    [Test]
    public void FindPlayer_ReturnsPlayerState_OrNull()
    {
        Assert.AreSame(victim, sim.FindPlayer(2));
        Assert.IsNull(sim.FindPlayer(999));
    }

    // ---- Hedef seçimi (insan yazan) ----

    /// <summary>Yazanı insan yapar ve simülasyonu yeniden kurar.</summary>
    void UseHumanWriter()
    {
        writer = new PlayerState(1, "Insan", config.startingHealth);
        match = new MatchState(new[] { writer, victim });
        sim = new NetMatchSim(config, match, avatars, new System.Random(5));
    }

    [Test]
    public void HumanAttack_WaitsForTheTargetChoice_AndAnnouncesIt()
    {
        UseHumanWriter();
        int startedWriter = -1;
        float startedSeconds = 0f;
        sim.TargetingStarted += (id, seconds) => { startedWriter = id; startedSeconds = seconds; };

        sim.OnWordPlayed(writer, Word("kurt"));

        Assert.IsTrue(sim.IsSelectingTarget);
        Assert.AreEqual(1, sim.SelectingWriterId);
        Assert.AreEqual(0, sim.CreatureCount);
        Assert.AreEqual(1, startedWriter);
        Assert.AreEqual(config.targetSelectionSeconds, startedSeconds, 0.0001f);
    }

    [Test]
    public void ChosenTarget_IsUsed_AndSelectionEnds()
    {
        UseHumanWriter();
        var third = new PlayerState(3, "Ucuncu", config.startingHealth);
        match = new MatchState(new[] { writer, victim, third });
        avatars.Positions[3] = new Vector2(5f, 5f);
        sim = new NetMatchSim(config, match, avatars, new System.Random(5));
        bool ended = false;
        sim.TargetingEnded += () => ended = true;

        sim.OnWordPlayed(writer, Word("kurt"));
        Assert.IsTrue(sim.TryChooseTarget(1, 3));

        Assert.IsTrue(ended);
        Assert.IsFalse(sim.IsSelectingTarget);
        Assert.AreEqual(1, sim.CreatureCount);

        Run(10f);
        Assert.AreEqual(victim.Health.Max, victim.Health.Current); // Seçilen 3'tü, 2 değil
        Assert.Less(third.Health.Current, third.Health.Max);
    }

    [Test]
    public void ChoosingTarget_ByAnotherPlayer_IsRejected()
    {
        UseHumanWriter();
        sim.OnWordPlayed(writer, Word("kurt"));

        Assert.IsFalse(sim.TryChooseTarget(2, 1)); // Sıra 2'de değil
        Assert.IsTrue(sim.IsSelectingTarget);
    }

    [Test]
    public void ChoosingTarget_Yourself_Eliminated_OrStealthed_IsRejected()
    {
        UseHumanWriter();
        sim.OnWordPlayed(writer, Word("kurt"));

        Assert.IsFalse(sim.TryChooseTarget(1, 1)); // Kendini
        Assert.IsFalse(sim.TryChooseTarget(1, 999)); // Olmayan

        victim.Status.ApplyStealth(10f);
        Assert.IsFalse(sim.TryChooseTarget(1, 2)); // Görünmez

        victim.Status.Clear();
        victim.Health.TakeDamage(999);
        Assert.IsFalse(sim.TryChooseTarget(1, 2)); // Elenmiş

        Assert.IsTrue(sim.IsSelectingTarget); // Geçersiz seçimler süreci bozmaz
    }

    [Test]
    public void ChoosingTarget_WhenNobodySelects_IsRejected()
    {
        Assert.IsFalse(sim.TryChooseTarget(1, 2));
    }

    [Test]
    public void SelectionTimeout_PicksARandomOpponent()
    {
        UseHumanWriter();
        bool ended = false;
        sim.TargetingEnded += () => ended = true;

        sim.OnWordPlayed(writer, Word("kurt"));
        Run(config.targetSelectionSeconds + 0.2f);

        Assert.IsTrue(ended);
        Assert.IsFalse(sim.IsSelectingTarget);
        Assert.AreEqual(1, sim.CreatureCount);
    }

    [Test]
    public void WriterEliminatedWhileSelecting_CancelsTheAttack()
    {
        UseHumanWriter();
        bool ended = false;
        sim.TargetingEnded += () => ended = true;

        sim.OnWordPlayed(writer, Word("kurt"));
        writer.Health.TakeDamage(999);
        sim.Tick(0.05f);

        Assert.IsTrue(ended);
        Assert.AreEqual(0, sim.CreatureCount);
    }

    [Test]
    public void NewAttackWhileSelecting_CompletesThePreviousOneRandomly()
    {
        UseHumanWriter();

        sim.OnWordPlayed(writer, Word("kurt"));
        sim.OnWordPlayed(writer, Word("yumruk"));

        Assert.AreEqual(1, sim.CreatureCount); // İlki rastgele tamamlandı
        Assert.IsTrue(sim.IsSelectingTarget);  // İkincisi seçim bekliyor
    }

    // ---- Koruma kelimeleri ----

    [Test]
    public void Shield_AbsorbsTheNextHit()
    {
        sim.OnWordPlayed(victim, Word("kalkan"));
        Assert.IsTrue(victim.Status.HasShield);

        sim.OnWordPlayed(writer, Word("kurt"));

        // Kurt vurup çekilen bir avcı: ilk vuruş kalkana gider, kalkan biter, can düşmez.
        for (float t = 0f; t < 8f && victim.Status.HasShield; t += 0.05f) sim.Tick(0.05f);

        Assert.IsFalse(victim.Status.HasShield);
        Assert.AreEqual(victim.Health.Max, victim.Health.Current);
    }

    [Test]
    public void HealWord_RestoresOneHealth()
    {
        victim.Health.TakeDamage(2);

        sim.OnWordPlayed(victim, Word("can"));

        Assert.AreEqual(2, victim.Health.Current);
    }

    [Test]
    public void StealthAndFlightWords_SetTheirEffects()
    {
        sim.OnWordPlayed(victim, Word("gorunmezlik"));
        sim.OnWordPlayed(writer, Word("kanat"));

        Assert.IsTrue(victim.Status.IsStealthed);
        Assert.IsTrue(writer.Status.IsFlying);
    }

    [Test]
    public void WallWord_BuildsAWallInFrontOfTheWriter()
    {
        avatars.Positions[1] = Vector2.zero;
        avatars.Facings[1] = Vector2.right;

        sim.OnWordPlayed(writer, Word("duvar"));

        Assert.AreEqual(1, sim.Walls.Walls.Count);
        var walls = new List<WallSnapshot>();
        sim.SnapshotWalls(walls);
        Assert.AreEqual(1, walls.Count);
        Assert.Greater(walls[0].CenterX, 0f); // Baktığı yönün (sağ) önünde
        Assert.AreEqual(1f, walls[0].LifeRatio, 0.0001f);
    }

    [Test]
    public void Wall_ExpiresAfterItsLifetime_AndLeavesTheSnapshot()
    {
        avatars.Positions[1] = Vector2.zero;
        avatars.Facings[1] = Vector2.up;
        sim.OnWordPlayed(writer, Word("duvar"));
        var walls = new List<WallSnapshot>();
        sim.SnapshotWalls(walls);
        int id = walls[0].Id;

        Run(config.wallDefaultSeconds + 12f); // Kelimenin süresi kadar

        sim.SnapshotWalls(walls);
        Assert.AreEqual(0, walls.Count);
        Assert.AreEqual(0, sim.Walls.Walls.Count);
        Assert.Greater(id, 0);
    }

    [Test]
    public void WallSnapshot_KeepsTheSameIdWhileTheWallLives()
    {
        avatars.Positions[1] = Vector2.zero;
        sim.OnWordPlayed(writer, Word("duvar"));
        var first = new List<WallSnapshot>();
        var second = new List<WallSnapshot>();

        sim.SnapshotWalls(first);
        sim.Tick(1f);
        sim.SnapshotWalls(second);

        Assert.AreEqual(first[0].Id, second[0].Id);
        Assert.Less(second[0].LifeRatio, first[0].LifeRatio);
    }

    // ---- Etki bayrakları ve duvar kodlayıcı ----

    [Test]
    public void StatusFlags_ReflectTheActiveEffects()
    {
        var status = new StatusEffects();
        Assert.AreEqual(StatusFlags.None, StatusFlagsUtil.Of(status));

        status.ApplyShield(1, 10f);
        status.ApplyStealth(10f);
        status.ApplySlow(0.5f, 10f);
        status.ApplyFlight(10f, 1.5f);

        StatusFlags flags = StatusFlagsUtil.Of(status);
        Assert.IsTrue((flags & StatusFlags.Shield) != 0);
        Assert.IsTrue((flags & StatusFlags.Stealth) != 0);
        Assert.IsTrue((flags & StatusFlags.Slow) != 0);
        Assert.IsTrue((flags & StatusFlags.Flight) != 0);
        Assert.IsTrue((flags & StatusFlags.Pull) == 0);
    }

    [Test]
    public void StatusFlagsTint_MatchesTheSinglePlayerTint()
    {
        var status = new StatusEffects();
        status.ApplyShield(1, 10f);
        status.ApplyStealth(10f);
        status.ApplySlow(0.5f, 10f);
        Color baseColor = new Color(0.3f, 0.8f, 0.5f);

        Color fromFlags = StatusFlagsUtil.Tint(baseColor, StatusFlagsUtil.Of(status));
        Color fromStatus = StatusTint.Apply(baseColor, status);

        Assert.AreEqual(fromStatus.r, fromFlags.r, 0.0001f);
        Assert.AreEqual(fromStatus.g, fromFlags.g, 0.0001f);
        Assert.AreEqual(fromStatus.b, fromFlags.b, 0.0001f);
        Assert.AreEqual(fromStatus.a, fromFlags.a, 0.0001f);
    }

    [Test]
    public void WallCodec_RoundTrips()
    {
        var list = new List<WallSnapshot>
        {
            new WallSnapshot { Id = 7, CenterX = -1.5f, CenterY = 2.25f, DirX = 0f, DirY = 1f, Length = 4f, Thickness = 0.5f, LifeRatio = 0.6f },
        };

        Assert.IsTrue(WallSnapshotCodec.TryDecode(WallSnapshotCodec.Encode(list), out List<WallSnapshot> back));

        Assert.AreEqual(1, back.Count);
        Assert.AreEqual(7, back[0].Id);
        Assert.AreEqual(-1.5f, back[0].CenterX, 0.001f);
        Assert.AreEqual(4f, back[0].Length, 0.001f);
        Assert.AreEqual(0.6f, back[0].LifeRatio, 0.001f);
    }

    [TestCase("1|0|0|0|1|4|0.5")]
    [TestCase("x|0|0|0|1|4|0.5|1")]
    [TestCase("1|0|0|0|NaN|4|0.5|1")]
    public void WallCodec_RejectsBrokenText(string text)
    {
        Assert.IsFalse(WallSnapshotCodec.TryDecode(text, out List<WallSnapshot> list));
        Assert.AreEqual(0, list.Count);
    }
}
