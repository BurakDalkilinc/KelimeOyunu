using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>MeteorShowerEffect (Meteor yağmuru) testleri: zamanlama, yerleşim, gölge/alan vuruşu, haritada çözüm alanı olmaması.</summary>
public class MeteorShowerTests
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

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    class CollectingSink : ICreatureSink
    {
        public readonly List<CreatureModel> Models = new List<CreatureModel>();
        public readonly List<string> Labels = new List<string>();

        public void Spawn(CreatureModel model, string label)
        {
            Models.Add(model);
            Labels.Add(label);
        }
    }

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // meteor hedefli olasılık 0.4, yayılma 2, iz 0.4 sn
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Meteor() => WordDatabase.LoadFromResources(50).Lookup("meteor_yagmuru").Entry;

    FakeWorld Players(int count)
    {
        var world = new FakeWorld();
        for (int i = 1; i <= count; i++) world.Add(i, new Vector2(-8f + i * 2f, 0f));
        return world;
    }

    MeteorShowerEffect Start(FakeWorld world, CollectingSink sink, int seed = 1)
    {
        var context = new ThreatContext(Meteor(), null, config, arena, world, new Random(seed), new SafeZoneField(), sink);
        var effect = new MeteorShowerEffect();
        effect.Begin(context);
        return effect;
    }

    // ---- Zamanlama ----

    [Test]
    public void Meteor_FromRealWords_Has30MeteorsOver12Seconds()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);

        Assert.AreEqual(30, effect.Total);   // words.json: count 30
        Assert.AreEqual(1, effect.Spawned, "İlk meteor hemen düşer");
        Assert.IsFalse(effect.IsFinished);
    }

    [Test]
    public void Meteors_AreSpacedEvenly_AcrossTheDuration()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);

        effect.Tick(6.01f); // Süre 12 sn → aralık 0.4 sn; 6 sn'de 0..15 → 16 meteor (eşikte yuvarlama olmasın diye küçük pay)
        Assert.AreEqual(16, effect.Spawned);

        effect.Tick(5.9f);  // Toplam ~11.9 sn: eşik 29 × 0.4 = 11.6 → hepsi (30) düştü
        Assert.AreEqual(30, effect.Spawned);
        Assert.IsTrue(effect.IsFinished);
    }

    [Test]
    public void Meteors_HugeTimeStep_SpawnsEveryMeteorExactlyOnce()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);

        effect.Tick(1000f);
        effect.Tick(1000f);

        Assert.AreEqual(30, sink.Models.Count);
        Assert.IsTrue(effect.IsFinished);
    }

    [Test]
    public void Meteors_NegativeDelta_SpawnsNothingExtra()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);
        int before = sink.Models.Count;

        effect.Tick(-5f);

        Assert.AreEqual(before, sink.Models.Count);
    }

    // ---- Her meteorun özellikleri ----

    [Test]
    public void EachMeteor_IsAShadowStrike_WithRadiusWarningAndDamageFromTheWord()
    {
        var sink = new CollectingSink();
        var effect = Start(Players(4), sink);
        effect.Tick(100f);

        foreach (CreatureModel meteor in sink.Models)
        {
            Assert.IsTrue(meteor.IsStrike);
            Assert.IsTrue(meteor.IsSky);
            Assert.AreEqual(1.5f, meteor.Radius, 0.001f);               // words.json: radius 1.5
            Assert.AreEqual(1.5f, meteor.StrikeWarningSeconds, 0.001f); // words.json: warningSeconds 1.5
            Assert.AreEqual(1, meteor.Damage);
            Assert.IsTrue(meteor.IsHarmful, "Botlar gölgeden kaçmalı");
        }
    }

    [Test]
    public void EachMeteor_LivesOnlyWarningPlusLinger_NotTheWholeShowerDuration()
    {
        var sink = new CollectingSink();
        Start(Players(4), sink);

        CreatureModel meteor = sink.Models[0];
        Assert.AreEqual(1.5f + config.strikeLingerSeconds, meteor.LifeLeft, 0.001f);
    }

    [Test]
    public void Meteors_HaveNoLabel_AndFallInsideTheArena()
    {
        var sink = new CollectingSink();
        Start(Players(4), sink).Tick(100f);

        foreach (string label in sink.Labels) Assert.AreEqual(string.Empty, label);
        foreach (CreatureModel meteor in sink.Models) Assert.IsTrue(arena.Contains(meteor.Position), meteor.Position.ToString());
    }

    [Test]
    public void Meteors_SomeAimNearPlayers_AndSomeFallAnywhere()
    {
        int nearPlayer = 0, total = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            var world = Players(3);
            var sink = new CollectingSink();
            Start(world, sink, seed).Tick(100f);

            foreach (CreatureModel meteor in sink.Models)
            {
                total++;
                foreach (PlayerState p in world.Players)
                {
                    world.TryGetPosition(p.Id, out Vector2 pos);
                    if ((pos - meteor.Position).magnitude <= config.meteorAimSpread + 0.001f) { nearPlayer++; break; }
                }
            }
        }

        double ratio = nearPlayer / (double)total;
        Assert.That(ratio, Is.InRange(0.3, 0.7), "Hedefli olasılık 0.4 + rastgele düşenlerin tesadüfen yakın olması");
        Assert.Less(nearPlayer, total, "Hepsi oyuncunun yanına düşmemeli");
    }

    [Test]
    public void Meteors_ZeroAimChance_NeverTargetPlayersOnPurpose_AndFullChanceAlwaysDoes()
    {
        config.meteorAimedChance = 1f;
        var sink = new CollectingSink();
        var world = Players(2);
        Start(world, sink, 3).Tick(100f);

        foreach (CreatureModel meteor in sink.Models)
        {
            bool near = false;
            foreach (PlayerState p in world.Players)
            {
                world.TryGetPosition(p.Id, out Vector2 pos);
                if ((pos - meteor.Position).magnitude <= config.meteorAimSpread + 0.001f) near = true;
            }
            Assert.IsTrue(near, "Şans 1 iken her meteor bir oyuncunun yakınına düşer");
        }
    }

    // ---- Vuruş ----

    [Test]
    public void ASingleMeteor_HurtsEveryoneInsideItsShadow_WhenItLands()
    {
        var world = new FakeWorld();
        var inside = world.Add(1, new Vector2(0f, 0f));
        var edge = world.Add(2, new Vector2(1.6f, 0f));
        var outside = world.Add(3, new Vector2(6f, 0f));
        var meteor = CreatureFactory.CreateAttack(Meteor(), inside, Vector2.zero, config, arena);
        meteor.PlaceAt(Vector2.zero);

        meteor.TickStrike(1.4f, world, 0.5f, new List<CreatureHit>());
        Assert.AreEqual(3, inside.Health.Current, "Gölge görünürken hasar yok");

        meteor.TickStrike(0.2f, world, 0.5f, new List<CreatureHit>());
        Assert.AreEqual(2, inside.Health.Current);
        Assert.AreEqual(2, edge.Health.Current);
        Assert.AreEqual(3, outside.Health.Current);
    }

    [Test]
    public void Meteor_ShieldAbsorbsOneMeteor()
    {
        var world = new FakeWorld();
        var p = world.Add(1, Vector2.zero);
        p.Status.ApplyShield(1, 30f);
        var meteor = CreatureFactory.CreateAttack(Meteor(), p, Vector2.zero, config, arena);
        meteor.PlaceAt(Vector2.zero);

        meteor.TickStrike(2f, world, 0.5f, new List<CreatureHit>());

        Assert.AreEqual(3, p.Health.Current);
        Assert.IsFalse(p.Status.HasShield);
    }

    // ---- Kenar durumları ----

    [Test]
    public void Meteors_WithNoAlivePlayers_DropNothing_ButStillFinish()
    {
        var world = Players(2);
        foreach (PlayerState p in world.Players) p.Health.TakeDamage(99);
        var sink = new CollectingSink();
        var effect = Start(world, sink);

        effect.Tick(100f);

        Assert.AreEqual(0, sink.Models.Count);
        Assert.IsTrue(effect.IsFinished);
    }

    [Test]
    public void Meteors_WithoutASink_DoNotThrow_AndStillFinish()
    {
        var context = new ThreatContext(Meteor(), null, config, arena, Players(2), new Random(1), new SafeZoneField(), null);
        var effect = new MeteorShowerEffect();

        Assert.DoesNotThrow(() => effect.Begin(context));
        Assert.DoesNotThrow(() => effect.Tick(100f));
        Assert.IsTrue(effect.IsFinished);
    }

    [Test]
    public void Meteors_WordWithoutStats_StillRainsAtLeastOne()
    {
        var word = new WordEntry { id = "x", tags = new[] { "sky", "explosive", "map_wide" } };
        var sink = new CollectingSink();
        var context = new ThreatContext(word, null, config, arena, Players(2), new Random(1), new SafeZoneField(), sink);
        var effect = new MeteorShowerEffect();

        effect.Begin(context);

        Assert.AreEqual(1, effect.Total);
        Assert.GreaterOrEqual(sink.Models.Count, 1);
    }

    // ---- GlobalThreat: Meteor yağmuru ----

    [Test]
    public void MeteorShower_AsAGlobalThreat_HasNoZonesAndStartsActiveImmediately()
    {
        var world = Players(4);
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Meteor(), world.Players[0], config, arena, world, new Random(2), sink);

        Assert.AreEqual(0, threat.Zones.Zones.Count);
        Assert.AreEqual(ThreatPhase.Active, threat.Phase);
        Assert.AreEqual(1, threat.Effects.Count);
        Assert.IsInstanceOf<MeteorShowerEffect>(threat.Effects[0]);
        Assert.GreaterOrEqual(sink.Models.Count, 1, "İlk meteor hemen düşer");
    }

    [Test]
    public void MeteorShower_Threat_FinishesAfterAllMeteorsAreDroppedAndTheLinger()
    {
        var world = Players(4);
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Meteor(), world.Players[0], config, arena, world, new Random(2), sink);

        threat.Tick(12.5f);
        Assert.AreEqual(30, sink.Models.Count);
        Assert.IsTrue(threat.EffectsFinished);

        threat.Tick(config.safeZoneLingerSeconds + 0.5f);
        Assert.AreEqual(ThreatPhase.Finished, threat.Phase);
    }

    [Test]
    public void Registry_SkyAndMapWide_ProduceAMeteorShower()
    {
        var effects = TagRegistry.CreateThreatEffects(Meteor());
        Assert.AreEqual(1, effects.Count);
        Assert.IsInstanceOf<MeteorShowerEffect>(effects[0]);
    }

    [Test]
    public void SkyTag_MarksCreaturesAsSky_AndOnlySkyWords()
    {
        var target = new PlayerState(1, "H", 3);
        var db = WordDatabase.LoadFromResources(50);

        Assert.IsTrue(CreatureFactory.CreateAttack(Meteor(), target, Vector2.zero, config, arena).IsSky);
        Assert.IsFalse(CreatureFactory.CreateAttack(db.Lookup("bomba").Entry, target, Vector2.zero, config, arena).IsSky);
    }

    [Test]
    public void LimitLife_OnlyShortens_NeverExtends()
    {
        var target = new PlayerState(1, "H", 3);
        var creature = new CreatureModel(null, target, Vector2.zero, 1f, 1, 0.5f, 10f, arena);

        creature.LimitLife(20f);
        Assert.AreEqual(10f, creature.LifeLeft, 0.001f);

        creature.LimitLife(2f);
        Assert.AreEqual(2f, creature.LifeLeft, 0.001f);
    }
}
