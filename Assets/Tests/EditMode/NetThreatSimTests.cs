using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Ağ maçı büyük tehdit testleri: çözüm alanları, uyarı, etki, sığınma, botların sığınağa koşması (ağsız).</summary>
public class NetThreatSimTests
{
    sealed class FakeAvatars : INetAvatars
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);
        public bool TryApplyKnockback(int playerId, Vector2 distance) => Positions.ContainsKey(playerId);
        public bool TryGetFacing(int playerId, out Vector2 direction) { direction = Vector2.up; return false; }
    }

    GameConfig config;
    WordDatabase words;
    FakeAvatars avatars;
    NetMatchSim sim;
    PlayerState writer;
    PlayerState[] others;
    readonly List<ZoneSnapshot> zoneBuffer = new List<ZoneSnapshot>();

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        words = WordDatabase.LoadFromResources(config.maxWordLength);

        writer = new PlayerState(1, "Yazan", config.startingHealth, isBot: true);
        others = new[]
        {
            new PlayerState(2, "A", config.startingHealth),
            new PlayerState(3, "B", config.startingHealth),
            new PlayerState(4, "C", config.startingHealth),
        };

        var all = new List<PlayerState> { writer };
        all.AddRange(others);
        var match = new MatchState(all);

        avatars = new FakeAvatars();
        avatars.Positions[1] = new Vector2(-9f, 5f);
        avatars.Positions[2] = new Vector2(8f, -5f);
        avatars.Positions[3] = new Vector2(8f, 5f);
        avatars.Positions[4] = new Vector2(-8f, -5f);

        sim = new NetMatchSim(config, match, avatars, new System.Random(11));
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

    List<ZoneSnapshot> Zones()
    {
        sim.SnapshotZones(zoneBuffer);
        return new List<ZoneSnapshot>(zoneBuffer);
    }

    // ---- Çözüm alanları ve alarm ----

    [Test]
    public void Tsunami_PlacesThreeTowers_AndRaisesTheAlarm()
    {
        sim.OnWordPlayed(writer, Word("tsunami"));

        List<ZoneSnapshot> zones = Zones();
        Assert.AreEqual(2, zones.Count); // 4 oyuncu: 3 * 4 / 8 = 1,5 -> 2 kule (oyuncu sayısına bağlı)
        Assert.IsTrue(zones.TrueForAll(z => z.Type == "tower" && z.Capacity == 3));

        ThreatStatus status = sim.GetThreatStatus();
        Assert.AreEqual("tsunami", status.AlarmWordId);
        Assert.AreEqual(5, status.AlarmSeconds);
        Assert.IsFalse(status.WaveActive);
    }

    [Test]
    public void Alarm_CountsDown_DuringTheWarning()
    {
        sim.OnWordPlayed(writer, Word("tsunami"));

        Run(2f);

        Assert.AreEqual(3, sim.GetThreatStatus().AlarmSeconds);
    }

    [Test]
    public void ZoneIds_StayTheSame_WhileTheThreatLives()
    {
        sim.OnWordPlayed(writer, Word("tsunami"));
        List<ZoneSnapshot> first = Zones();
        sim.Tick(1f);
        List<ZoneSnapshot> second = Zones();

        for (int i = 0; i < first.Count; i++) Assert.AreEqual(first[i].Id, second[i].Id);
    }

    // ---- Atom bombası ----

    [Test]
    public void AtomBomb_EliminatesThoseOutside_AndSparesTheOneInTheShelter()
    {
        sim.OnWordPlayed(writer, Word("atom_bombasi"));
        List<ZoneSnapshot> zones = Zones();
        Assert.AreEqual(1, zones.Count);
        Assert.AreEqual("shelter", zones[0].Type);
        Assert.AreEqual(2, zones[0].Capacity); // Hayattaki 4 kişinin yarısı

        avatars.Positions[2] = new Vector2(zones[0].X, zones[0].Y); // A sığınakta, B ve C dışarıda

        Run(9f); // 8 sn uyarı

        Assert.IsTrue(others[0].IsAlive);
        Assert.IsTrue(others[1].IsGhost);
        Assert.IsTrue(others[2].IsGhost);
        Assert.AreEqual(1, sim.GetThreatStatus().ImpactCount);
    }

    [Test]
    public void ThreatEnds_AndTheZonesAreRemoved_AfterTheLingerTime()
    {
        sim.OnWordPlayed(writer, Word("atom_bombasi"));

        Run(8f + config.safeZoneLingerSeconds + 1f);

        Assert.AreEqual(0, Zones().Count);
        Assert.AreEqual(0, sim.Threats.Count);
        Assert.AreEqual("", sim.GetThreatStatus().AlarmWordId);
    }

    // ---- Tsunami ----

    [Test]
    public void Tsunami_WaveAppears_AndOnlyHitsThoseOutsideTheTowers()
    {
        sim.OnWordPlayed(writer, Word("tsunami"));
        List<ZoneSnapshot> zones = Zones();
        avatars.Positions[2] = new Vector2(zones[0].X, zones[0].Y); // A kulede

        Run(5.5f); // Uyarı bitti, dalga başladı
        ThreatStatus status = sim.GetThreatStatus();
        Assert.IsTrue(status.WaveActive);
        Assert.AreEqual(1f, Mathf.Abs(status.WaveDirX) + Mathf.Abs(status.WaveDirY), 0.0001f);

        Run(15f); // Dalga arenayı geçsin

        Assert.IsTrue(others[0].IsAlive);
        Assert.AreEqual(others[0].Health.Max, others[0].Health.Current);
        Assert.IsTrue(others[1].IsGhost || others[2].IsGhost); // Dışarıdakilerden en az biri dalgada elendi
        Assert.IsFalse(sim.GetThreatStatus().WaveActive);
    }

    // ---- Yaratık tehditleri ----

    [Test]
    public void MeteorShower_HasNoZones_AndRainsCreaturesOverTime()
    {
        sim.OnWordPlayed(writer, Word("meteor_yagmuru"));

        Assert.AreEqual(0, Zones().Count);
        Run(3f);

        Assert.Greater(sim.CreatureCount, 0);
        ThreatStatus status = sim.GetThreatStatus();
        Assert.AreEqual("meteor_yagmuru", status.AlarmWordId);
        Assert.AreEqual(0, status.ImpactCount); // Meteorda ekran parlamaz
    }

    [Test]
    public void ZombieHorde_SpawnsAtOnce_WithHouses()
    {
        sim.OnWordPlayed(writer, Word("zombi_suru"));

        Assert.AreEqual(20, sim.CreatureCount);
        List<ZoneSnapshot> zones = Zones();
        Assert.AreEqual(2, zones.Count); // 4 oyuncu: 4 * 4 / 8 = 2 ev
        Assert.IsTrue(zones.TrueForAll(z => z.Type == "houses"));
    }

    [Test]
    public void Dragon_IsOneFlyingFireCreature_WithCaves()
    {
        sim.OnWordPlayed(writer, Word("ejderha"));
        Assert.AreEqual(0, sim.CreatureCount, "Uyarı bitmeden ejderha gelmez");
        sim.Tick(7.1f);

        Assert.AreEqual(1, sim.CreatureCount);
        var creatures = new List<CreatureSnapshot>();
        sim.Snapshot(creatures);
        Assert.AreEqual(CreatureKind.FireArea, creatures[0].Kind);

        List<ZoneSnapshot> zones = Zones();
        Assert.AreEqual(2, zones.Count); // 4 oyuncu: 3 * 4 / 8 = 1,5 → 2 mağara
        Assert.IsTrue(zones.TrueForAll(z => z.Type == "cave"));
    }

    // ---- Botlar sığınağa koşar ----

    [Test]
    public void Refuge_PointsToTheNearestFreeZone_AndShelteredPlayersStay()
    {
        sim.OnWordPlayed(writer, Word("tsunami"));
        List<ZoneSnapshot> zones = Zones();
        IRefugeProvider refuge = sim;

        Vector2 near = new Vector2(zones[0].X, zones[0].Y);
        Assert.IsTrue(refuge.TryGetRefuge(others[0], near + new Vector2(0.1f, 0f), out Vector2 point));
        Assert.AreEqual(near.x, point.x, 0.0001f);
        Assert.AreEqual(near.y, point.y, 0.0001f);
        Assert.IsFalse(refuge.IsSheltered(others[0]));

        avatars.Positions[2] = near;
        sim.Tick(0.1f); // Alan içindekileri günceller

        Assert.IsTrue(refuge.IsSheltered(others[0]));
        Assert.IsTrue(refuge.TryGetRefuge(others[0], near + new Vector2(5f, 5f), out Vector2 stay));
        Assert.AreEqual(near.x, stay.x, 0.0001f); // Zaten korunuyorsa orada kalır
    }

    [Test]
    public void Refuge_WithoutAnyThreat_IsNone()
    {
        IRefugeProvider refuge = sim;
        Assert.IsFalse(refuge.TryGetRefuge(others[0], Vector2.zero, out _));
        Assert.IsFalse(refuge.IsSheltered(others[0]));
    }

    [Test]
    public void DefenseAndAttackWords_DoNotStartThreats()
    {
        sim.OnWordPlayed(writer, Word("kurt"));
        sim.OnWordPlayed(writer, Word("kalkan"));

        Assert.AreEqual(0, sim.Threats.Count);
        Assert.AreEqual("", sim.GetThreatStatus().AlarmWordId);
    }

    // ---- Kodlayıcılar ----

    [Test]
    public void ZoneCodec_RoundTrips()
    {
        var list = new List<ZoneSnapshot>
        {
            new ZoneSnapshot { Id = 2, Type = "tower", X = -3.5f, Y = 1.25f, Radius = 1.5f, Capacity = 3, Occupied = 2 },
        };

        Assert.IsTrue(ZoneSnapshotCodec.TryDecode(ZoneSnapshotCodec.Encode(list), out List<ZoneSnapshot> back));

        Assert.AreEqual(1, back.Count);
        Assert.AreEqual("tower", back[0].Type);
        Assert.AreEqual(-3.5f, back[0].X, 0.001f);
        Assert.AreEqual(3, back[0].Capacity);
        Assert.AreEqual(2, back[0].Occupied);
    }

    [TestCase("1|tower|0|0|1|3")]
    [TestCase("x|tower|0|0|1|3|0")]
    [TestCase("1|tower|NaN|0|1|3|0")]
    public void ZoneCodec_RejectsBrokenText(string text)
    {
        Assert.IsFalse(ZoneSnapshotCodec.TryDecode(text, out List<ZoneSnapshot> list));
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void ThreatStatusCodec_RoundTrips()
    {
        var status = new ThreatStatus
        {
            AlarmWordId = "tsunami", AlarmSeconds = 4, ImpactCount = 2,
            WaveActive = true, WaveDirX = -1f, WaveDirY = 0f, WaveFront = 3.25f,
        };

        Assert.IsTrue(ThreatStatusCodec.TryDecode(ThreatStatusCodec.Encode(status), out ThreatStatus back));

        Assert.AreEqual("tsunami", back.AlarmWordId);
        Assert.AreEqual(4, back.AlarmSeconds);
        Assert.AreEqual(2, back.ImpactCount);
        Assert.IsTrue(back.WaveActive);
        Assert.AreEqual(-1f, back.WaveDirX, 0.001f);
        Assert.AreEqual(3.25f, back.WaveFront, 0.001f);
    }

    [TestCase("")]
    [TestCase("a|1|2|0|0|0|0")]
    [TestCase("a|x|2|0|0|0|0|v1")]
    [TestCase("a|1|2|5|0|0|0|v1")]
    public void ThreatStatusCodec_RejectsBrokenText(string text)
    {
        Assert.IsFalse(ThreatStatusCodec.TryDecode(text, out ThreatStatus status));
        Assert.AreEqual("", status.AlarmWordId);
    }

    [Test]
    public void ThreatStatusCodec_EmptyAlarm_RoundTrips()
    {
        Assert.IsTrue(ThreatStatusCodec.TryDecode(ThreatStatusCodec.Encode(new ThreatStatus { AlarmWordId = "" }), out ThreatStatus back));
        Assert.AreEqual("", back.AlarmWordId);
    }

    [Test]
    public void ZoneLabel_FromNumbers_MatchesTheZoneOverload()
    {
        var zone = new SafeZone("tower", Vector2.zero, 1.5f, 3, float.MaxValue);
        Assert.AreEqual(ThreatText.GetZoneLabel(zone), ThreatText.GetZoneLabel("tower", 0, 3));
        Assert.AreEqual("Kule 2/3", ThreatText.GetZoneLabel("tower", 2, 3));
    }
}
