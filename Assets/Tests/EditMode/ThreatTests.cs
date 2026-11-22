using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>Global tehdit altyapısının testleri: çözüm alanları, kapasite, yerleştirme, tehdit akışı, Atom bombası, botların sığınması.</summary>
public class ThreatTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position, int health = 3)
        {
            var player = new PlayerState(id, "P" + id, health);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public void Move(int id, Vector2 position) => positions[id] = position;

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // alan yarıçapı 1.6, en az uyarı 5 sn, toparlanma 2 sn
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    /// <summary>n oyuncu, hepsi (0,0) çevresinde, arenanın ortasında.</summary>
    FakeWorld Crowd(int count)
    {
        var world = new FakeWorld();
        for (int i = 1; i <= count; i++) world.Add(i, new Vector2(30f, 30f)); // Hepsi başta çok uzakta
        return world;
    }

    GlobalThreat NewThreat(string wordId, FakeWorld world, int seed = 1)
    {
        return new GlobalThreat(Real(wordId), world.Players.Count > 0 ? world.Players[0] : null, config, arena, world, new Random(seed));
    }

    // ---- SafeZone ----

    [Test]
    public void Zone_FillsToCapacity_ClosestToCenterFirst()
    {
        var world = new FakeWorld();
        var far = world.Add(1, new Vector2(1.2f, 0f));
        var near = world.Add(2, new Vector2(0.1f, 0f));
        var mid = world.Add(3, new Vector2(0.6f, 0f));
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 2, 100f);

        zone.UpdateOccupants(world);

        Assert.AreEqual(2, zone.Occupants.Count);
        Assert.IsTrue(zone.IsOccupant(near.Id));
        Assert.IsTrue(zone.IsOccupant(mid.Id));
        Assert.IsFalse(zone.IsOccupant(far.Id), "Kapasite doldu, en uzaktaki giremedi");
        Assert.AreEqual(0, zone.Free);
    }

    [Test]
    public void Zone_OutsidePlayersAreNotProtected()
    {
        var world = new FakeWorld();
        var outside = world.Add(1, new Vector2(5f, 0f));
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 4, 100f);

        zone.UpdateOccupants(world);

        Assert.AreEqual(0, zone.Occupants.Count);
        Assert.IsFalse(zone.IsOccupant(outside.Id));
    }

    [Test]
    public void Zone_PlayerWhoLeaves_FreesTheirPlace_AndAWaitingPlayerTakesIt()
    {
        var world = new FakeWorld();
        var a = world.Add(1, new Vector2(0.2f, 0f));
        var b = world.Add(2, new Vector2(1.0f, 0f));
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 1, 100f);

        zone.UpdateOccupants(world);
        Assert.IsTrue(zone.IsOccupant(a.Id));
        Assert.IsFalse(zone.IsOccupant(b.Id));

        world.Move(1, new Vector2(6f, 0f)); // a çıktı
        zone.UpdateOccupants(world);

        Assert.IsFalse(zone.IsOccupant(a.Id));
        Assert.IsTrue(zone.IsOccupant(b.Id));
    }

    [Test]
    public void Zone_OccupantKeepsTheirSpot_EvenIfSomeoneCloserArrivesLater()
    {
        var world = new FakeWorld();
        var first = world.Add(1, new Vector2(1.2f, 0f));
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 1, 100f);
        zone.UpdateOccupants(world);

        var late = world.Add(2, new Vector2(0.0f, 0f)); // Tam merkeze geldi ama geç kaldı
        zone.UpdateOccupants(world);

        Assert.IsTrue(zone.IsOccupant(first.Id));
        Assert.IsFalse(zone.IsOccupant(late.Id), "Önce giren yerini korur");
    }

    [Test]
    public void Zone_EliminatedOccupant_IsRemoved()
    {
        var world = new FakeWorld();
        var p = world.Add(1, new Vector2(0.2f, 0f));
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 2, 100f);
        zone.UpdateOccupants(world);

        p.Health.TakeDamage(99);
        zone.UpdateOccupants(world);

        Assert.AreEqual(0, zone.Occupants.Count);
    }

    [Test]
    public void Zone_NullWorld_DoesNotThrow_AndCapacityIsAtLeastOne()
    {
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 0, 100f);
        Assert.DoesNotThrow(() => zone.UpdateOccupants(null));
        Assert.AreEqual(1, zone.Capacity);
    }

    [Test]
    public void Zone_ExpiresWithTime()
    {
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 2, 3f);
        zone.Tick(2.9f);
        Assert.IsFalse(zone.IsExpired);
        zone.Tick(0.2f);
        Assert.IsTrue(zone.IsExpired);
    }

    // ---- SafeZoneField ----

    [Test]
    public void Field_IsProtected_OnlyForOccupantsOfAnyZone()
    {
        var world = new FakeWorld();
        var inA = world.Add(1, new Vector2(-5f, 0f));
        var inB = world.Add(2, new Vector2(5f, 0f));
        var nowhere = world.Add(3, new Vector2(0f, 4f));
        var field = new SafeZoneField();
        field.Add(new SafeZone("shelter", new Vector2(-5f, 0f), 1.6f, 1, 100f));
        field.Add(new SafeZone("shelter", new Vector2(5f, 0f), 1.6f, 1, 100f));

        field.Update(world, 0.1f);

        Assert.IsTrue(field.IsProtected(inA.Id));
        Assert.IsTrue(field.IsProtected(inB.Id));
        Assert.IsFalse(field.IsProtected(nowhere.Id));
    }

    [Test]
    public void Field_RaisesAddedAndRemoved_AndClearRemovesEverything()
    {
        var field = new SafeZoneField();
        int added = 0, removed = 0;
        field.Added += z => added++;
        field.Removed += z => removed++;

        field.Add(new SafeZone("tower", Vector2.zero, 1f, 1, 100f));
        field.Add(new SafeZone("tower", Vector2.one, 1f, 1, 0.5f));
        field.Update(new FakeWorld(), 1f); // İkincinin ömrü bitti

        Assert.AreEqual(2, added);
        Assert.AreEqual(1, removed);
        Assert.AreEqual(1, field.Zones.Count);

        field.Clear();
        Assert.AreEqual(0, field.Zones.Count);
        Assert.AreEqual(2, removed);
    }

    // ---- ZonePlacement ----

    [Test]
    public void Placement_ReturnsRequestedCount_InsideMargins_AndSeparated()
    {
        var points = ZonePlacement.Pick(arena, 4, 2.5f, 5f, new Random(7));

        Assert.AreEqual(4, points.Count);
        foreach (Vector2 p in points)
        {
            Assert.LessOrEqual(Mathf.Abs(p.x), 10f - 2.5f + 0.001f);
            Assert.LessOrEqual(Mathf.Abs(p.y), 6f - 2.5f + 0.001f);
        }
        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
                Assert.GreaterOrEqual((points[i] - points[j]).magnitude, 5f * 0.5f, "Birbirine yapışık olmamalı");
    }

    [Test]
    public void Placement_ImpossibleSeparation_StillReturnsEveryZone()
    {
        var points = ZonePlacement.Pick(new ArenaBounds(new Vector2(6f, 6f)), 10, 1f, 50f, new Random(1));
        Assert.AreEqual(10, points.Count);
    }

    [Test]
    public void Placement_SameSeed_SamePoints_AndZeroCountIsEmpty()
    {
        var a = ZonePlacement.Pick(arena, 3, 2f, 4f, new Random(5));
        var b = ZonePlacement.Pick(arena, 3, 2f, 4f, new Random(5));
        CollectionAssert.AreEqual(a, b);
        Assert.IsEmpty(ZonePlacement.Pick(arena, 0, 2f, 4f, new Random(5)));
        Assert.IsEmpty(ZonePlacement.Pick(arena, -2, 2f, 4f, new Random(5)));
    }

    // ---- ThreatSpec (gerçek kelimeler) ----

    [Test]
    public void Spec_AtomBombasi_OneShelterWithHalfTheAlivePlayers()
    {
        var atom = Real("atom_bombasi");

        Assert.IsTrue(ThreatSpec.HasZones(atom));
        Assert.AreEqual(1, ThreatSpec.GetZoneCount(atom));
        Assert.AreEqual(4, ThreatSpec.GetZoneCapacity(atom, 8, config));
        Assert.AreEqual(1, ThreatSpec.GetZoneCapacity(atom, 3, config), "floor(1.5) = 1");
        Assert.AreEqual(1, ThreatSpec.GetZoneCapacity(atom, 1, config), "En az 1");
        Assert.AreEqual(10, ThreatSpec.GetZoneCapacity(atom, 20, config));
    }

    [Test]
    public void Spec_OtherThreats_UseCountAndCapacityPerItem()
    {
        Assert.AreEqual(3, ThreatSpec.GetZoneCount(Real("tsunami")));
        Assert.AreEqual(3, ThreatSpec.GetZoneCapacity(Real("tsunami"), 8, config));
        Assert.AreEqual(2, ThreatSpec.GetZoneCount(Real("ejderha")));
        Assert.AreEqual(4, ThreatSpec.GetZoneCapacity(Real("ejderha"), 8, config));
        Assert.AreEqual(4, ThreatSpec.GetZoneCount(Real("zombi_suru")));
        Assert.AreEqual(2, ThreatSpec.GetZoneCapacity(Real("zombi_suru"), 8, config));
    }

    [Test]
    public void Spec_MeteorShadows_HasNoZonesAndNoGlobalWarning()
    {
        var meteor = Real("meteor_yagmuru");

        Assert.IsFalse(ThreatSpec.HasZones(meteor));
        Assert.AreEqual(0, ThreatSpec.GetZoneCount(meteor));
        Assert.AreEqual(0f, ThreatSpec.GetWarningSeconds(meteor, config), 0.001f);
    }

    [Test]
    public void Spec_Warning_IsAtLeastTheConfiguredMinimum()
    {
        Assert.AreEqual(8f, ThreatSpec.GetWarningSeconds(Real("atom_bombasi"), config), 0.001f); // words.json: 8
        Assert.AreEqual(5f, ThreatSpec.GetWarningSeconds(Real("tsunami"), config), 0.001f);

        var quick = new WordEntry
        {
            id = "x", category = WordEntry.CategoryThreat, scope = WordEntry.ScopeGlobal,
            tags = new[] { "map_wide" }, stats = new WordStats { warningSeconds = 1f },
            counter = new WordCounter { type = "shelter", count = 1, capacityRatio = 0.5f }
        };
        Assert.AreEqual(config.globalThreatWarningSeconds, ThreatSpec.GetWarningSeconds(quick, config), 0.001f, "GDD: en az 5 sn");
    }

    [Test]
    public void Spec_WordWithoutCounter_HasNoZones()
    {
        Assert.IsFalse(ThreatSpec.HasZones(null));
        Assert.IsFalse(ThreatSpec.HasZones(Real("kurt")));
        Assert.AreEqual(0, ThreatSpec.GetZoneCapacity(Real("kurt"), 8, config));
    }

    // ---- GlobalThreat: Atom bombası ----

    [Test]
    public void Atom_StartsInWarning_WithOneShelterOfHalfCapacity()
    {
        var world = Crowd(8);
        var threat = NewThreat("atom_bombasi", world);

        Assert.AreEqual(ThreatPhase.Warning, threat.Phase);
        Assert.AreEqual(8f, threat.WarningLeft, 0.001f);
        Assert.AreEqual(1, threat.Zones.Zones.Count);
        Assert.AreEqual(4, threat.Zones.Zones[0].Capacity);
        Assert.AreEqual("shelter", threat.Zones.Zones[0].Type);
        Assert.IsTrue(arena.Contains(threat.Zones.Zones[0].Position));
    }

    [Test]
    public void Atom_DoesNothingDuringTheWarning()
    {
        var world = Crowd(4);
        var threat = NewThreat("atom_bombasi", world);

        threat.Tick(7.9f);

        foreach (PlayerState p in world.Players) Assert.IsTrue(p.IsAlive);
        Assert.AreEqual(ThreatPhase.Warning, threat.Phase);
        Assert.AreEqual(0.1f, threat.WarningLeft, 0.001f);
    }

    [Test]
    public void Atom_Blast_EliminatesEveryoneOutsideTheShelter_AndSparesTheProtected()
    {
        var world = new FakeWorld();
        var safe = world.Add(1, new Vector2(30f, 30f));
        var unsafe1 = world.Add(2, new Vector2(30f, 30f));
        var threat = new GlobalThreat(Real("atom_bombasi"), safe, config, arena, world, new Random(3));
        world.Move(1, threat.Zones.Zones[0].Position); // 1 numara sığınağa girdi

        threat.Tick(8.1f);

        Assert.AreEqual(ThreatPhase.Active, threat.Phase);
        Assert.IsTrue(safe.IsAlive, "Sığınaktaki kurtuldu");
        Assert.IsFalse(unsafe1.IsAlive, "Dışarıdaki elendi");
    }

    [Test]
    public void Atom_ShelterCapacityIsLimited_SoOnlyTheFirstOnesSurvive()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 8; i++) world.Add(i, new Vector2(30f, 30f));
        var threat = new GlobalThreat(Real("atom_bombasi"), world.Players[0], config, arena, world, new Random(3));
        Vector2 shelter = threat.Zones.Zones[0].Position;

        // Hepsi sığınağa koşar, ama 8 kişiden yalnızca 4'ü sığar (1-8 sırasıyla merkeze yaklaşıyor).
        for (int i = 1; i <= 8; i++) world.Move(i, shelter + new Vector2(0.1f * i, 0f));

        threat.Tick(8.1f);

        int alive = 0;
        foreach (PlayerState p in world.Players) if (p.IsAlive) alive++;
        Assert.AreEqual(4, alive);
        Assert.IsTrue(world.Players[0].IsAlive, "Merkeze en yakın olanlar kurtulur");
        Assert.IsFalse(world.Players[7].IsAlive, "En uzaktaki sığmadı");
    }

    [Test]
    public void Atom_PlayerWhoEntersJustBeforeTheBlast_IsSaved()
    {
        var world = new FakeWorld();
        var runner = world.Add(1, new Vector2(30f, 30f));
        var threat = new GlobalThreat(Real("atom_bombasi"), runner, config, arena, world, new Random(2));

        threat.Tick(7.9f);
        world.Move(1, threat.Zones.Zones[0].Position); // Son anda girdi
        threat.Tick(0.2f);

        Assert.IsTrue(runner.IsAlive);
    }

    [Test]
    public void Atom_ShieldAbsorbsTheBlastOnce()
    {
        var world = new FakeWorld();
        var shielded = world.Add(1, new Vector2(30f, 30f));
        shielded.Status.ApplyShield(1, 30f);
        var threat = new GlobalThreat(Real("atom_bombasi"), shielded, config, arena, world, new Random(2));

        threat.Tick(8.1f);

        Assert.IsTrue(shielded.IsAlive, "Kalkan atom bombasını bir kez engeller");
        Assert.IsFalse(shielded.Status.HasShield);
    }

    [Test]
    public void Atom_RaisesImpactedOnce_AndEliminatedPlayersAreNotHitAgain()
    {
        var world = Crowd(3);
        var threat = NewThreat("atom_bombasi", world);
        int impacts = 0;
        threat.Impacted += () => impacts++;

        threat.Tick(8.1f);
        threat.Tick(1f);
        threat.Tick(1f);

        Assert.AreEqual(1, impacts);
    }

    [Test]
    public void Atom_FinishesAfterLinger_AndClearsItsZones()
    {
        var world = Crowd(4);
        var threat = NewThreat("atom_bombasi", world);
        int removed = 0;
        threat.Zones.Removed += z => removed++;

        threat.Tick(8.1f);
        Assert.AreEqual(ThreatPhase.Active, threat.Phase);
        Assert.AreEqual(1, threat.Zones.Zones.Count, "Alan toparlanma süresince ekranda kalır");

        threat.Tick(config.safeZoneLingerSeconds + 0.5f);

        Assert.AreEqual(ThreatPhase.Finished, threat.Phase);
        Assert.AreEqual(0, threat.Zones.Zones.Count);
        Assert.AreEqual(1, removed);
    }

    [Test]
    public void Atom_HugeTimeStep_StillBlastsOnce()
    {
        var world = Crowd(3);
        var threat = NewThreat("atom_bombasi", world);

        threat.Tick(1000f);

        foreach (PlayerState p in world.Players) Assert.IsFalse(p.IsAlive);
        Assert.AreNotEqual(ThreatPhase.Warning, threat.Phase);
    }

    [Test]
    public void GlobalThreat_NullArguments_Throw()
    {
        var world = Crowd(2);
        Assert.Throws<System.ArgumentNullException>(() => new GlobalThreat(null, null, config, arena, world, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => new GlobalThreat(Real("atom_bombasi"), null, null, arena, world, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => new GlobalThreat(Real("atom_bombasi"), null, config, null, world, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => new GlobalThreat(Real("atom_bombasi"), null, config, arena, null, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => new GlobalThreat(Real("atom_bombasi"), null, config, arena, world, null));
    }

    // ---- Etiketten etki kurma ----

    [Test]
    public void Registry_MapWideThreat_GetsABlastEffect_OthersGetNone()
    {
        var effects = TagRegistry.CreateThreatEffects(Real("atom_bombasi"));
        Assert.AreEqual(1, effects.Count);
        Assert.IsInstanceOf<MapBlastEffect>(effects[0]);

        Assert.IsEmpty(TagRegistry.CreateThreatEffects(Real("kurt")));
        Assert.IsEmpty(TagRegistry.CreateThreatEffects(null));
    }

    [Test]
    public void MapBlast_WordWithoutDamage_StillHitsForOne()
    {
        var world = Crowd(2);
        var word = new WordEntry { id = "x", tags = new[] { "map_wide" } };
        var zones = new SafeZoneField();
        var context = new ThreatContext(word, null, config, arena, world, new Random(1), zones);
        var blast = new MapBlastEffect();

        blast.Begin(context);

        Assert.IsTrue(blast.IsFinished);
        Assert.AreEqual(2, blast.Affected);
        Assert.AreEqual(2, world.Players[0].Health.Current);
    }

    // ---- Yazılar ----

    [Test]
    public void Text_Alarm_IsUppercaseTurkishWithHintAndSeconds()
    {
        Assert.AreEqual("ATOM BOMBASI! Sığınağa gir! (5 sn)", ThreatText.GetAlarm(Real("atom_bombasi"), 5));
        Assert.AreEqual("TSUNAMİ! Kuleye çık! (3 sn)", ThreatText.GetAlarm(Real("tsunami"), 3), "Türkçe büyük harf: i → İ");
        Assert.AreEqual("EJDERHA! Mağaraya gir!", ThreatText.GetAlarm(Real("ejderha"), 0));
        Assert.AreEqual("ZOMBİ SÜRÜSÜ! Eve gir! (4 sn)", ThreatText.GetAlarm(Real("zombi_suru"), 4), "Türkçe büyük harf: i → İ");
        Assert.AreEqual("METEOR YAĞMURU! Gölgelere dikkat!", ThreatText.GetAlarm(Real("meteor_yagmuru"), 0));
    }

    [Test]
    public void Text_ZoneLabelAndNames()
    {
        var zone = new SafeZone("shelter", Vector2.zero, 1.6f, 4, 100f);
        Assert.AreEqual("Sığınak 0/4", ThreatText.GetZoneLabel(zone));
        Assert.AreEqual("Kule", ThreatText.GetZoneName("tower"));
        Assert.AreEqual("Mağara", ThreatText.GetZoneName("cave"));
        Assert.AreEqual("Ev", ThreatText.GetZoneName("houses"));
        Assert.AreEqual("Alan", ThreatText.GetZoneName("whatever"));
        Assert.AreEqual("Kaç!", ThreatText.GetHint("whatever"));
        Assert.Greater(ThreatText.GetZoneColor("tower").a, 0f);
    }

    // ---- Botların sığınması ----

    [Test]
    public void Bot_WithRefuge_WalksTowardItAndStopsWhenThere()
    {
        var brain = new BotBrain(config, arena, new Random(1));
        Vector2 refuge = new Vector2(5f, 0f);

        Vector2 toward = brain.Think(0.1f, new Vector2(0f, 0f), new List<DangerZone>(), refuge);
        Assert.AreEqual(1f, toward.x, 0.001f);

        Vector2 arrived = brain.Think(0.1f, new Vector2(4.9f, 0f), new List<DangerZone>(), refuge);
        Assert.AreEqual(Vector2.zero, arrived, "Merkeze yaklaşınca durur");
    }

    [Test]
    public void Bot_FleeingDanger_BeatsGoingToRefuge()
    {
        var brain = new BotBrain(config, arena, new Random(1));
        var dangers = new List<DangerZone> { new DangerZone(new Vector2(1f, 0f), 1f) };

        // Sığınak sağda ama tehlike de sağda ve çok yakın: kaçar (sola).
        Vector2 direction = brain.Think(0.1f, Vector2.zero, dangers, new Vector2(8f, 0f));

        Assert.Less(direction.x, 0f);
    }

    [Test]
    public void Bot_AlreadyShelteredInAZone_StaysPut_EvenWithDangerRightNextToIt()
    {
        var brain = new BotBrain(config, arena, new Random(1));
        var dangers = new List<DangerZone> { new DangerZone(new Vector2(1f, 0f), 1f) }; // Çok yakın tehlike
        Vector2 refuge = new Vector2(0.5f, 0f);

        // Korunmuyorsa kaçardı (sola); korunuyorsa sığınağın merkezine kalır.
        Vector2 flee = brain.Think(0.1f, Vector2.zero, dangers, refuge, false);
        Assert.Less(flee.x, 0f);

        Vector2 stays = brain.Think(0.1f, new Vector2(0.45f, 0f), dangers, refuge, true);
        Assert.AreEqual(Vector2.zero, stays, "Sığınağın merkezine yakınsa durur");

        Vector2 returns = brain.Think(0.1f, new Vector2(-0.5f, 0f), dangers, refuge, true);
        Assert.Greater(returns.x, 0f, "Merkezden uzaklaştıysa geri döner, kaçmaz");
    }

    [Test]
    public void Bot_ShelteredButWithNoRefugePoint_FallsBackToNormalBehaviour()
    {
        var brain = new BotBrain(config, arena, new Random(1));
        var dangers = new List<DangerZone> { new DangerZone(new Vector2(1f, 0f), 1f) };

        Vector2 direction = brain.Think(0.1f, Vector2.zero, dangers, null, true);

        Assert.Less(direction.x, 0f, "Sığınak noktası yoksa normal kaçma");
    }

    [Test]
    public void Bot_WithoutRefuge_StillWanders()
    {
        var brain = new BotBrain(config, arena, new Random(1));
        Vector2 direction = brain.Think(0.1f, Vector2.zero, new List<DangerZone>(), null);
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }
}
