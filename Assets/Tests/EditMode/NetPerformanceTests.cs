using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 20 oyunculu ağır maç yükünün host maliyeti: simülasyon adımı ve yaratık fotoğrafı. Sınırlar bilerek cömert (yavaş makinede de geçsin);
/// asıl amaç, bir değişiklik bunu birden on katına çıkarırsa testin kırılması ve gerçek sayıların çıktıya yazılmasıdır.
/// </summary>
public class NetPerformanceTests
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

    WordEntry Word(string id)
    {
        foreach (WordEntry entry in words.Words)
            if (entry.id == id) return entry;
        Assert.Fail("Kelime yok: " + id);
        return null;
    }

    /// <summary>20 oyuncu (ilk ikisi insan), hepsi arenada dağınık.</summary>
    NetMatchSim BuildTwentyPlayerSim(out List<PlayerState> players)
    {
        players = new List<PlayerState>();
        var avatars = new FakeAvatars();
        var arena = new ArenaBounds(config.arenaSize);

        for (int i = 0; i < 20; i++)
        {
            int id = i < 2 ? i : RosterBuilder.FirstBotId + i;
            players.Add(new PlayerState(id, "P" + id, 1000, isBot: i >= 2)); // Çok canlı: ölüm yükü bozmasın
            avatars.Positions[id] = SpawnRing.Point(arena, i, 20, 0.7f);
        }

        return new NetMatchSim(config, new MatchState(players), avatars, new System.Random(8));
    }

    [Test]
    public void SimTick_WithTwentyPlayersAndAHeavyLoad_StaysCheap()
    {
        NetMatchSim sim = BuildTwentyPlayerSim(out List<PlayerState> players);
        PlayerState writer = players[5];

        // Ağır yük: zombi sürüsü (20 yaratık), meteor yağmuru (30 meteor), arılar (sürü), bulut, yumruk, bomba, duvar, atom bombası.
        foreach (string id in new[] { "zombi_suru", "meteor_yagmuru", "ari", "bulut", "yumruk", "bomba", "kurt", "tsunami" })
            sim.OnWordPlayed(writer, Word(id));

        int ticks = 600;
        float step = 1f / 60f;
        var snapshot = new List<CreatureSnapshot>();
        int peakCreatures = 0;

        var watch = Stopwatch.StartNew();
        for (int i = 0; i < ticks; i++)
        {
            sim.Tick(step);
            sim.Snapshot(snapshot);
            peakCreatures = Mathf.Max(peakCreatures, snapshot.Count);
        }
        watch.Stop();

        double perTick = watch.Elapsed.TotalMilliseconds / ticks;
        TestContext.WriteLine($"Simülasyon adımı + fotoğraf: {perTick:0.000} ms (yaratık tepe: {peakCreatures}, 20 oyuncu)");

        Assert.Greater(peakCreatures, 25, "Yük gerçekten ağır olmalı");
        Assert.Less(perTick, 8.0, "Bir adım 60 Hz bütçesinin (16,6 ms) yarısını geçmemeli");
    }

    [Test]
    public void CreatureSnapshot_AtTheHeaviestLoad_IsSmallEnoughToBroadcastOften()
    {
        NetMatchSim sim = BuildTwentyPlayerSim(out List<PlayerState> players);
        foreach (string id in new[] { "zombi_suru", "meteor_yagmuru", "ari", "ejderha" }) sim.OnWordPlayed(players[5], Word(id));
        sim.Tick(1f); // Meteorlardan birkaçı daha doğsun

        var snapshot = new List<CreatureSnapshot>();
        sim.Snapshot(snapshot);

        var watch = Stopwatch.StartNew();
        string packed = null;
        for (int i = 0; i < 200; i++) packed = CreatureSnapshotCodec.Encode(snapshot);
        watch.Stop();

        double encodeMs = watch.Elapsed.TotalMilliseconds / 200;
        int bytes = System.Text.Encoding.UTF8.GetByteCount(packed);
        double perSecondKb = bytes * (1f / 0.08f) / 1024.0;
        TestContext.WriteLine($"{snapshot.Count} yaratık: {bytes} bayt/paket, kodlama {encodeMs:0.000} ms, istemci başına ~{perSecondKb:0.0} KB/sn");

        Assert.Less(bytes, 16 * 1024, "Tek paket 16 KB'ı aşmamalı");
        Assert.Less(perSecondKb, 200, "İstemci başına yaratık trafiği 200 KB/sn'yi aşmamalı");
        Assert.Less(encodeMs, 2.0);
        Assert.IsTrue(CreatureSnapshotCodec.TryDecode(packed, out List<CreatureSnapshot> back));
        Assert.AreEqual(snapshot.Count, back.Count);
    }

    [Test]
    public void TurnHost_WithTwentyPlayers_RunsAFullTurnCycle_QuicklyAndWithoutStalls()
    {
        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0, 1 }, 20);
        var host = new NetTurnHost(config, words, new System.Random(2), roster);
        int messages = 0;
        host.MessageReady += _ => messages++;
        host.Start();

        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 60 * 120; i++) host.Tick(1f / 60f); // 2 dakikalık maç
        watch.Stop();

        TestContext.WriteLine($"2 dk, 20 oyunculu tur akışı: {watch.Elapsed.TotalMilliseconds:0} ms, {messages} mesaj, tur {host.Turns.TurnNumber}");

        Assert.Greater(host.Turns.TurnNumber, 1, "Tur akışı ilerlemeli");
        Assert.Less(watch.Elapsed.TotalMilliseconds, 2000);
        Assert.Less(messages, 2000, "Mesaj sayısı kontrolsüz artmamalı");
    }

    [Test]
    public void BotBrain_TwentyBots_ThinkQuickly()
    {
        var arena = new ArenaBounds(config.arenaSize);
        var brains = new List<BotBrain>();
        for (int i = 0; i < 20; i++) brains.Add(new BotBrain(config, arena, new System.Random(i)));
        var dangers = new List<DangerZone>();
        for (int i = 0; i < 40; i++) dangers.Add(new DangerZone(SpawnRing.Point(arena, i, 40, 0.8f), 1.5f));

        var watch = Stopwatch.StartNew();
        for (int frame = 0; frame < 600; frame++)
            for (int b = 0; b < brains.Count; b++)
                brains[b].Think(1f / 60f, SpawnRing.Point(arena, b, 20, 0.5f), dangers);
        watch.Stop();

        double perFrame = watch.Elapsed.TotalMilliseconds / 600;
        TestContext.WriteLine($"20 bot, 40 tehlike alanı: kare başına {perFrame:0.000} ms");
        Assert.Less(perFrame, 3.0);
    }
}
