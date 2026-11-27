using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>HordeEffect (Zombi sürüsü) testleri: kenarlardan salma, korunanlara vurmama, süre, evler, GlobalThreat akışı.</summary>
public class HordeEffectTests
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

    class CollectingSink : ICreatureSink
    {
        public readonly List<CreatureModel> Models = new List<CreatureModel>();
        public void Spawn(CreatureModel model, string label) => Models.Add(model);
    }

    class FakeProtection : IProtection
    {
        public readonly HashSet<int> Protected = new HashSet<int>();
        public bool IsProtected(int playerId) => Protected.Contains(playerId);
    }

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>();
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Horde() => WordDatabase.LoadFromResources(50).Lookup("zombi_suru").Entry;

    FakeWorld Players(int count)
    {
        var world = new FakeWorld();
        for (int i = 1; i <= count; i++) world.Add(i, new Vector2(-8f + i * 2f, 0f));
        return world;
    }

    HordeEffect Start(FakeWorld world, CollectingSink sink, SafeZoneField zones = null, int seed = 1)
    {
        var context = new ThreatContext(Horde(), null, config, arena, world, new Random(seed), zones ?? new SafeZoneField(), sink);
        var effect = new HordeEffect();
        effect.Begin(context);
        return effect;
    }

    // ---- Salma ----

    [Test]
    public void Horde_FromRealWords_Spawns20Zombies_AtTheArenaEdges()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(5), sink);

        Assert.AreEqual(20, effect.Spawned);   // words.json: count 20
        Assert.AreEqual(20, sink.Models.Count);
        foreach (CreatureModel zombie in sink.Models)
        {
            bool onEdge = Mathf.Abs(Mathf.Abs(zombie.Position.x) - 9.5f) < 0.01f || Mathf.Abs(Mathf.Abs(zombie.Position.y) - 5.5f) < 0.01f;
            Assert.IsTrue(onEdge, "Kenardan girer: " + zombie.Position);
            Assert.IsTrue(arena.Contains(zombie.Position));
        }
    }

    [Test]
    public void EachZombie_ComesFromTheWordsTags_UndeadPersistentWithStatsFromJson()
    {
        var sink = new CollectingSink();
        Start(Players(5), sink);

        foreach (CreatureModel zombie in sink.Models)
        {
            Assert.IsTrue(zombie.Persistent, "undead: kalıcı vuruş");
            Assert.AreEqual(2.5f, zombie.Speed, 0.001f);   // words.json: speed 2.5
            Assert.AreEqual(1, zombie.Damage);
            Assert.AreEqual(10f, zombie.LifeLeft, 0.001f); // words.json: duration 10
            Assert.IsNotNull(zombie.Target);
        }
    }

    [Test]
    public void Zombies_EachTargetTheNearestAlivePlayerToWhereTheyEntered()
    {
        var world = Players(5);
        world.Players[0].Health.TakeDamage(99); // Elenmiş: hedef olamaz
        var sink = new CollectingSink();
        Start(world, sink, seed: 4);

        var targets = new HashSet<int>();
        foreach (CreatureModel zombie in sink.Models)
        {
            Assert.IsTrue(zombie.Target.IsAlive);
            targets.Add(zombie.Target.Id);

            // Doğduğu yere en yakın hayattaki oyuncu olmalı.
            PlayerState expected = null;
            float best = float.MaxValue;
            foreach (PlayerState p in world.Players)
            {
                if (!p.IsAlive) continue;
                world.TryGetPosition(p.Id, out Vector2 pos);
                float d = (pos - zombie.Position).magnitude;
                if (d < best) { best = d; expected = p; }
            }
            Assert.AreSame(expected, zombie.Target);
        }
        Assert.GreaterOrEqual(targets.Count, 2, "Kenarın farklı yerlerinden girenler farklı oyunculara gider");
    }

    [Test]
    public void Zombies_IgnoreStealthedPlayersWhenChoosingATarget()
    {
        var world = Players(3);
        world.Players[0].Status.ApplyStealth(10f);
        var sink = new CollectingSink();
        Start(world, sink);

        foreach (CreatureModel zombie in sink.Models)
            Assert.AreNotSame(world.Players[0], zombie.Target, "Görünmezler hedef alınamaz");
    }

    [Test]
    public void Zombie_WhenItsTargetDies_SwitchesToTheNextNearest_AndWhenSomeoneGetsMuchCloser_SwitchesToThem()
    {
        var world = new FakeWorld();
        var first = world.Add(1, new Vector2(2f, 0f));
        var second = world.Add(2, new Vector2(8f, 0f));
        var zombie = CreatureFactory.CreateAttack(Horde(), first, new Vector2(0f, 0f), config, arena);
        zombie.Locator = world;
        zombie.TargetsNearest = true;
        zombie.RetargetsWhenTargetDies = true;

        zombie.Tick(0.1f, new Vector2(2f, 0f), 0.5f, 1f);
        Assert.AreSame(first, zombie.Target);

        // İkinci oyuncu çok daha yakına geldi (hedeften 1 birimden fazla yakın): ona geçer.
        world.Move(2, new Vector2(0.2f, 0f));
        zombie.Tick(0.1f, new Vector2(2f, 0f), 0.5f, 1f);
        Assert.AreSame(second, zombie.Target);

        // Hedef ölünce, kalan en yakına geçer.
        second.Health.TakeDamage(99);
        zombie.Tick(0.1f, new Vector2(0.2f, 0f), 0.5f, 1f);
        Assert.AreSame(first, zombie.Target);
        Assert.IsFalse(zombie.IsFinished);

        // Kimse kalmayınca kaybolur.
        first.Health.TakeDamage(99);
        zombie.Tick(0.1f, new Vector2(2f, 0f), 0.5f, 1f);
        Assert.IsTrue(zombie.IsFinished);
    }

    [Test]
    public void Zombie_DoesNotFlipFlopBetweenAlmostEquallyCloseTargets()
    {
        var world = new FakeWorld();
        var a = world.Add(1, new Vector2(3f, 0f));
        world.Add(2, new Vector2(3f, 0.4f)); // Neredeyse aynı uzaklık
        var zombie = CreatureFactory.CreateAttack(Horde(), a, new Vector2(0f, 0f), config, arena);
        zombie.Locator = world;
        zombie.TargetsNearest = true;

        for (int i = 0; i < 10; i++) zombie.Tick(0.01f, new Vector2(3f, 0f), 0.5f, 1f);

        Assert.AreSame(a, zombie.Target, "Marj (1 birim) içinde kalan fark hedefi değiştirmez");
    }

    [Test]
    public void Swarm_StaggerIsCappedSoLateZombiesDoNotWaitForever()
    {
        var sink = new CollectingSink();
        Start(Players(5), sink);
        var victim = new PlayerState(99, "V", 1000);

        // Son üyenin ilk vuruşu (20. zombi) en çok swarmMaxStaggerSeconds kadar ertelenir (19 × 0.8 = 15.2 → 4 sn'ye iner).
        CreatureModel last = CreatureFactory.CreateAttack(Horde(), victim, new Vector2(0f, 0f), config, arena, 19, 20);
        last.Tick(0.05f, new Vector2(0f, 0f), 0.5f, 1f);
        Assert.AreEqual(1000, victim.Health.Current, "Henüz bekliyor");

        for (int i = 0; i < 80; i++) last.Tick(0.05f, new Vector2(0f, 0f), 0.5f, 1f); // 4 sn
        Assert.Less(victim.Health.Current, 1000, "4 sn sonunda vurmaya başlar");
    }

    // ---- Korunanlar ----

    [Test]
    public void Zombie_CannotHurtAPlayerInsideAHouse_ButHurtsOthers()
    {
        var protection = new FakeProtection();
        var housed = new PlayerState(1, "Evde", 3);
        var outside = new PlayerState(2, "Dışarıda", 3);
        protection.Protected.Add(housed.Id);

        CreatureModel ZombieAt(PlayerState target)
        {
            var z = CreatureFactory.CreateAttack(Horde(), target, new Vector2(4f, 0f), config, arena);
            z.Protection = protection;
            return z;
        }

        var onHoused = ZombieAt(housed);
        var onOutside = ZombieAt(outside);
        for (int i = 0; i < 100; i++)
        {
            onHoused.Tick(0.1f, new Vector2(4f, 0f), 0.5f, 1f);
            onOutside.Tick(0.1f, new Vector2(4f, 0f), 0.5f, 1f);
        }

        Assert.AreEqual(3, housed.Health.Current, "Evdekine vurulamaz");
        Assert.Less(outside.Health.Current, 3);
    }

    [Test]
    public void Zombie_ThatCouldNotHurtAProtectedTarget_StaysAround_AndHurtsOnceTheyLeave()
    {
        var protection = new FakeProtection();
        var target = new PlayerState(1, "H", 3);
        protection.Protected.Add(target.Id);
        var zombie = CreatureFactory.CreateAttack(Horde(), target, new Vector2(4f, 0f), config, arena);
        zombie.Protection = protection;

        for (int i = 0; i < 50; i++) zombie.Tick(0.1f, new Vector2(4f, 0f), 0.5f, 1f);
        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(zombie.IsFinished);

        protection.Protected.Clear(); // Evden çıktı
        for (int i = 0; i < 30; i++) zombie.Tick(0.1f, new Vector2(4f, 0f), 0.5f, 1f);
        Assert.Less(target.Health.Current, 3);
    }

    [Test]
    public void Strike_AndTrap_AlsoSkipProtectedPlayers()
    {
        var world = new FakeWorld();
        var housed = world.Add(1, Vector2.zero);
        var other = world.Add(2, new Vector2(0.5f, 0f));
        var protection = new FakeProtection();
        protection.Protected.Add(housed.Id);

        var fist = CreatureFactory.CreateAttack(WordDatabase.LoadFromResources(50).Lookup("yumruk").Entry, housed, Vector2.zero, config, arena);
        fist.PlaceAt(Vector2.zero);
        fist.Protection = protection;
        fist.TickStrike(2f, world, 0.5f, new List<CreatureHit>());

        Assert.AreEqual(3, housed.Health.Current);
        Assert.AreEqual(2, other.Health.Current);
    }

    // ---- Süre ve bitiş ----

    [Test]
    public void Horde_EffectLastsAsLongAsTheZombies_ThenFinishes()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);

        effect.Tick(9.9f);
        Assert.IsFalse(effect.IsFinished);

        effect.Tick(0.2f);
        Assert.IsTrue(effect.IsFinished);
    }

    [Test]
    public void Horde_WithoutSinkOrAlivePlayers_FinishesImmediately_WithoutThrowing()
    {
        var context = new ThreatContext(Horde(), null, config, arena, Players(3), new Random(1), new SafeZoneField(), null);
        var noSink = new HordeEffect();
        Assert.DoesNotThrow(() => noSink.Begin(context));
        Assert.IsTrue(noSink.IsFinished);

        var dead = Players(2);
        foreach (PlayerState p in dead.Players) p.Health.TakeDamage(99);
        var sink = new CollectingSink();
        var effect = Start(dead, sink);
        Assert.IsTrue(effect.IsFinished);
        Assert.AreEqual(0, sink.Models.Count);
    }

    // ---- GlobalThreat: gerçek Zombi sürüsü ----

    [Test]
    public void ZombiSuru_AsAGlobalThreat_StartsImmediately_WithOptionalHouses()
    {
        var world = Players(8);
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Horde(), world.Players[0], config, arena, world, new Random(3), sink);

        Assert.AreEqual(4, threat.Zones.Zones.Count, "Evler yine çıkar (isteğe bağlı sığınak)");
        foreach (SafeZone house in threat.Zones.Zones)
        {
            Assert.AreEqual("houses", house.Type);
            Assert.AreEqual(2, house.Capacity);
        }
        Assert.IsInstanceOf<HordeEffect>(threat.Effects[0]);

        Assert.AreEqual(ThreatPhase.Active, threat.Phase, "Uyarı beklenmez: zombiler hemen görünür");
        Assert.AreEqual(0f, threat.WarningLeft, 0.001f);
        Assert.AreEqual(20, sink.Models.Count, "Zombiler hemen sahnede");
    }

    [Test]
    public void ZombiSuru_Flow_ZombiesAreProtectedFromByHouses_AndEverythingEndsAfter10Seconds()
    {
        var world = Players(8);
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Horde(), world.Players[0], config, arena, world, new Random(3), sink);

        foreach (CreatureModel zombie in sink.Models)
            Assert.AreSame(threat.Zones, zombie.Protection, "İsteğe bağlı sığınaktakilere vurmazlar");

        threat.Tick(5f);
        Assert.AreEqual(4, threat.Zones.Zones.Count, "Zombiler yaşarken evler ayakta");

        threat.Tick(5.5f);   // 10 sn doldu
        threat.Tick(config.safeZoneLingerSeconds + 0.5f);
        Assert.AreEqual(ThreatPhase.Finished, threat.Phase);
        Assert.AreEqual(0, threat.Zones.Zones.Count);
    }

    [Test]
    public void NeedsWarning_BlastAndWaveDo_HordeAndMeteorDoNot()
    {
        Assert.IsTrue(new MapBlastEffect().NeedsWarning);
        Assert.IsTrue(new WaveEffect().NeedsWarning);
        Assert.IsFalse(new HordeEffect().NeedsWarning);
        Assert.IsTrue(new HordeEffect(needsWarning: true).NeedsWarning, "Ateşli tek yaratık (ejderha) uyarıdan sonra gelir");
        Assert.IsFalse(new MeteorShowerEffect().NeedsWarning);
    }

    // ---- Yardımcılar ----

    [Test]
    public void PickEdgePoint_IsAlwaysOnTheBorder_InsideTheArena_AndCoversAllSides()
    {
        var random = new Random(8);
        bool left = false, right = false, top = false, bottom = false;

        for (int i = 0; i < 200; i++)
        {
            Vector2 p = ArenaSpawn.PickEdgePoint(arena, random);
            Assert.IsTrue(arena.Contains(p));
            Assert.IsTrue(Mathf.Abs(Mathf.Abs(p.x) - 9.5f) < 0.001f || Mathf.Abs(Mathf.Abs(p.y) - 5.5f) < 0.001f);
            left |= p.x < -9f; right |= p.x > 9f; top |= p.y > 5f; bottom |= p.y < -5f;
        }
        Assert.IsTrue(left && right && top && bottom);
    }

    [Test]
    public void PickEdgePoint_NullArguments_Throw_AndSameSeedSamePoint()
    {
        Assert.Throws<System.ArgumentNullException>(() => ArenaSpawn.PickEdgePoint(null, new Random(1)));
        Assert.Throws<System.ArgumentNullException>(() => ArenaSpawn.PickEdgePoint(arena, null));
        Assert.AreEqual(ArenaSpawn.PickEdgePoint(arena, new Random(5)), ArenaSpawn.PickEdgePoint(arena, new Random(5)));
    }

    [Test]
    public void Registry_SwarmAndMapWide_ProduceAHorde_ButAriStillSpawnsNormally()
    {
        var effects = TagRegistry.CreateThreatEffects(Horde());
        Assert.AreEqual(1, effects.Count);
        Assert.IsInstanceOf<HordeEffect>(effects[0]);

        // Arı da swarm etiketli ama map_wide değil: tehdit etkisi üretmez, sürü doğurma yolundan çalışır.
        var ari = WordDatabase.LoadFromResources(50).Lookup("ari").Entry;
        Assert.IsEmpty(TagRegistry.CreateThreatEffects(ari));
        Assert.AreEqual(5, TagRegistry.GetSpawnCount(ari));
    }
}
