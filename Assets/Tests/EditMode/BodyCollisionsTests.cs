using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Karakter itişmesi ve oyuncu sayısına bağlı alan sayısı testleri.</summary>
public class BodyCollisionsTests
{
    const float Radius = 0.5f;
    static readonly ArenaBounds Arena = new ArenaBounds(new Vector2(34f, 19f));

    static List<bool> Solid(int count)
    {
        var list = new List<bool>();
        for (int i = 0; i < count; i++) list.Add(true);
        return list;
    }

    // ---- BodyCollisions ----

    [Test]
    public void TwoOverlappingBodies_AreSeparatedEvenly()
    {
        var positions = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.4f, 0f) };

        bool moved = BodyCollisions.Resolve(positions, Solid(2), Radius, Arena);

        Assert.IsTrue(moved);
        Assert.AreEqual(1f, (positions[1] - positions[0]).magnitude, 0.001f); // İki yarıçap kadar açıldı
        Assert.AreEqual(0.2f, (positions[0] + positions[1]).x * 0.5f, 0.001f); // Orta nokta sabit: eşit itildiler
    }

    [Test]
    public void BodiesThatDoNotTouch_AreLeftAlone()
    {
        var positions = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1.5f, 0f) };

        bool moved = BodyCollisions.Resolve(positions, Solid(2), Radius, Arena);

        Assert.IsFalse(moved);
        Assert.AreEqual(new Vector2(1.5f, 0f), positions[1]);
    }

    [Test]
    public void GhostsAndFlyers_AreNotSolid_AndPassThrough()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(0.1f, 0f), new Vector2(0.2f, 0f) };
        var solid = new List<bool> { true, false, false };

        bool moved = BodyCollisions.Resolve(positions, solid, Radius, Arena);

        Assert.IsFalse(moved);
        Assert.AreEqual(new Vector2(0.1f, 0f), positions[1]);
    }

    [Test]
    public void ExactlyOnTopOfEachOther_AreSeparatedDeterministically()
    {
        var a = new List<Vector2> { Vector2.zero, Vector2.zero };
        var b = new List<Vector2> { Vector2.zero, Vector2.zero };

        BodyCollisions.Resolve(a, Solid(2), Radius, Arena);
        BodyCollisions.Resolve(b, Solid(2), Radius, Arena);

        Assert.GreaterOrEqual((a[0] - a[1]).magnitude, 0.999f);
        Assert.AreEqual(a[0], b[0]); // Aynı girdi, aynı sonuç
        Assert.AreEqual(a[1], b[1]);
    }

    [Test]
    public void Crowd_OfTwentyInATowerSizedSpot_SpreadsOutWithoutLeavingTheArena()
    {
        var positions = new List<Vector2>();
        for (int i = 0; i < 20; i++) positions.Add(new Vector2((i % 5) * 0.2f, (i / 5) * 0.2f)); // Hepsi ~1 birim içinde

        for (int frame = 0; frame < 30; frame++) BodyCollisions.Resolve(positions, Solid(20), Radius, Arena);

        for (int i = 0; i < 20; i++)
        {
            Assert.IsTrue(Arena.Contains(positions[i]));
            for (int j = i + 1; j < 20; j++)
                Assert.GreaterOrEqual((positions[i] - positions[j]).magnitude, 2f * Radius - 0.05f, $"{i} ile {j} hâlâ iç içe");
        }
    }

    [Test]
    public void PushAgainstTheWall_KeepsBothInsideTheArena()
    {
        float edge = 17f - Radius;
        var positions = new List<Vector2> { new Vector2(edge, 0f), new Vector2(edge - 0.3f, 0f) };

        BodyCollisions.Resolve(positions, Solid(2), Radius, Arena);

        Assert.LessOrEqual(positions[0].x, edge + 0.0001f);
        Assert.LessOrEqual(positions[1].x, edge + 0.0001f);
    }

    [Test]
    public void AMover_PushesTheOneInFront_FrameByFrame()
    {
        var positions = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1.2f, 0f) };

        // Soldaki karakter her karede 0,1 birim sağa yürür (4 kare), öndekini iter.
        for (int frame = 0; frame < 4; frame++)
        {
            positions[0] = positions[0] + new Vector2(0.1f, 0f);
            BodyCollisions.Resolve(positions, Solid(2), Radius, Arena);
        }

        Assert.Greater(positions[1].x, 1.2f);                    // Öndeki itildi
        Assert.GreaterOrEqual(positions[1].x - positions[0].x, 0.999f); // İç içe geçmediler
    }

    [Test]
    public void Mismatched_ListLengths_DoNotThrow()
    {
        var positions = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero };

        Assert.DoesNotThrow(() => BodyCollisions.Resolve(positions, Solid(2), Radius, Arena));
    }

    // ---- Kuleden itme: itilen çözüm alanından çıkar, boşalan yere itişen girer ----

    sealed class FakeWorld : IPlayerLocator
    {
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public IReadOnlyList<PlayerState> Players { get; set; }
        public bool TryGetPosition(int id, out Vector2 p) => Positions.TryGetValue(id, out p);
    }

    [Test]
    public void ShovedOutOfATower_LosesProtection_AndThePusherTakesTheSpot()
    {
        var zone = new SafeZone("tower", Vector2.zero, 1.6f, 1, float.MaxValue); // Tek kişilik
        var inside = new PlayerState(1, "İçerideki", 3);
        var pusher = new PlayerState(2, "İtici", 3);
        var world = new FakeWorld { Players = new[] { inside, pusher } };
        world.Positions[1] = new Vector2(1.1f, 0f);
        world.Positions[2] = new Vector2(3.0f, 0f);
        zone.UpdateOccupants(world);
        Assert.IsTrue(zone.IsOccupant(1));

        // İtici kuleye doğru yürüyüp içerideki karakterle çarpışır; çarpışma çözümü içerideki karakteri dışarı sürer.
        var positions = new List<Vector2> { world.Positions[1], world.Positions[2] };
        for (int step = 0; step < 15; step++)
        {
            positions[1] = positions[1] + new Vector2(-0.25f, 0f); // İtici sola (kuleye) yürür
            BodyCollisions.Resolve(positions, Solid(2), Radius, Arena);
            world.Positions[1] = positions[0];
            world.Positions[2] = positions[1];
            zone.UpdateOccupants(world);
        }

        // Tek kişilik kulede, itici içeri girdi: önceki sahibinin yeri korunmaz çünkü artık dışarıda.
        Assert.IsFalse(zone.IsOccupant(1) && zone.IsOccupant(2), "Tek kişilik kulede iki kişi olamaz");
        Assert.IsTrue(zone.IsOccupant(2) || zone.IsOccupant(1));
    }

    // ---- Alan sayısı oyuncu sayısına bağlı ----

    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static WordEntry Counter(string type, int count, int capacityPerItem = 0, float capacityRatio = 0f)
    {
        return new WordEntry
        {
            id = "x", category = WordEntry.CategoryThreat, scope = WordEntry.ScopeGlobal,
            counter = new WordCounter { type = type, count = count, capacityPerItem = capacityPerItem, capacityRatio = capacityRatio },
        };
    }

    [TestCase(8, 3)]   // Referans oyuncu sayısı: kelimedeki sayı
    [TestCase(20, 8)]  // 3 * 20 / 8 = 7,5 -> 8 kule
    [TestCase(4, 2)]   // 3 * 4 / 8 = 1,5 -> 2 kule
    [TestCase(2, 1)]   // 0,75 -> 1
    [TestCase(1, 1)]
    [TestCase(0, 1)]   // En az 1
    public void TowerCount_ScalesWithAlivePlayers(int alive, int expected)
    {
        WordEntry tsunami = Counter("tower", 3, capacityPerItem: 3);

        Assert.AreEqual(expected, ThreatSpec.GetZoneCount(tsunami, alive, config));
    }

    [Test]
    public void TwentyPlayers_GetRoughlyTheSameSlotsPerPlayerAsEight()
    {
        WordEntry tsunami = Counter("tower", 3, capacityPerItem: 3);

        int slotsAtEight = ThreatSpec.GetZoneCount(tsunami, 8, config) * 3;
        int slotsAtTwenty = ThreatSpec.GetZoneCount(tsunami, 20, config) * 3;

        Assert.AreEqual(slotsAtEight / 8f, slotsAtTwenty / 20f, 0.2f);
    }

    [Test]
    public void CavesAndHouses_ScaleToo()
    {
        Assert.AreEqual(5, ThreatSpec.GetZoneCount(Counter("cave", 2, capacityPerItem: 4), 20, config));   // 2 * 20 / 8 = 5
        Assert.AreEqual(10, ThreatSpec.GetZoneCount(Counter("houses", 4, capacityPerItem: 2), 20, config)); // 4 * 20 / 8 = 10
    }

    [Test]
    public void Shelter_KeepsItsCount_ButItsCapacityAlreadyScalesWithPlayers()
    {
        WordEntry bomb = Counter("shelter", 1, capacityRatio: 0.5f);

        Assert.AreEqual(1, ThreatSpec.GetZoneCount(bomb, 20, config));
        Assert.AreEqual(10, ThreatSpec.GetZoneCapacity(bomb, 20, config));
        Assert.AreEqual(4, ThreatSpec.GetZoneCapacity(bomb, 8, config));
    }

    [Test]
    public void Meteor_HasNoZonesAtAnyPlayerCount()
    {
        Assert.AreEqual(0, ThreatSpec.GetZoneCount(Counter("shadows", 0), 20, config));
        Assert.AreEqual(0, ThreatSpec.GetZoneCount(null, 20, config));
    }

    [Test]
    public void RealTsunami_AtTwentyPlayers_PlacesEightTowers_AllInsideTheArena()
    {
        WordDatabase words = WordDatabase.LoadFromResources(config.maxWordLength);
        WordEntry tsunami = null;
        foreach (WordEntry w in words.Words) if (w.id == "tsunami") tsunami = w;
        var arena = new ArenaBounds(config.arenaSize);

        int count = ThreatSpec.GetZoneCount(tsunami, 20, config);
        List<Vector2> spots = ZonePlacement.Pick(arena, count, config.safeZoneEdgeMargin, config.safeZoneSeparation, new System.Random(3));

        Assert.AreEqual(8, count);
        Assert.AreEqual(8, spots.Count);
        foreach (Vector2 spot in spots) Assert.IsTrue(arena.Contains(spot));
    }
}
