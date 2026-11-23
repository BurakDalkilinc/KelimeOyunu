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

        writer = new PlayerState(1, "Yazan", config.startingHealth);
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
    public void DefenseAndThreatWords_DoNotSpawnCreatures_YetInThisStep()
    {
        sim.OnWordPlayed(writer, Word("kalkan"));
        sim.OnWordPlayed(writer, Word("tsunami"));

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
}
