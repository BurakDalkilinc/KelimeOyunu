using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>"fire" etiketinin ve Ejderha tehdidinin testleri: ateş alanı, yanma aralığı, hedef değiştirme, mağaralar, uyarıdan sonra gelme.</summary>
public class DragonFireTests
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
        public readonly List<string> Labels = new List<string>();

        public void Spawn(CreatureModel model, string label)
        {
            Models.Add(model);
            Labels.Add(label);
        }
    }

    class FakeProtection : IProtection
    {
        public readonly HashSet<int> Protected = new HashSet<int>();
        public bool IsProtected(int playerId) => Protected.Contains(playerId);
    }

    const float PlayerRadius = 0.5f;

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // yanma aralığı 1 sn
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Ejderha() => WordDatabase.LoadFromResources(50).Lookup("ejderha").Entry;

    CreatureModel Dragon(PlayerState target, Vector2 at)
    {
        var dragon = CreatureFactory.CreateAttack(Ejderha(), target, at, config, arena);
        dragon.PlaceAt(at);
        return dragon;
    }

    // ---- Ejderha (gerçek kelimeden) ----

    [Test]
    public void Ejderha_FromRealWords_IsAFlyingFireAreaThatBurnsEverySecond()
    {
        var target = new PlayerState(1, "H", 3);
        var dragon = Dragon(target, new Vector2(5f, 0f));

        Assert.IsTrue(dragon.IsFlying);
        Assert.IsTrue(dragon.IsArea);
        Assert.IsTrue(dragon.BurnsArea);
        Assert.IsTrue(dragon.RetargetsWhenTargetDies);
        Assert.AreEqual(2f, dragon.Radius, 0.001f);     // words.json: radius 2.0
        Assert.AreEqual(3.6f, dragon.Speed, 0.001f);    // words.json: speed 3.6 (oyuncudan belirgin yavaş)
        Assert.Less(dragon.Speed, config.playerMoveSpeed * 0.8f, "Koşan oyuncu ejderhadan uzaklaşabilmeli");
        Assert.AreEqual(1, dragon.Damage);
        Assert.AreEqual(10f, dragon.LifeLeft, 0.001f);  // words.json: duration 10
        Assert.AreEqual(config.fireDamageIntervalSeconds, dragon.AreaDamageIntervalSeconds, 0.001f);
        Assert.IsTrue(dragon.IsHarmful, "Botlar ejderhadan kaçar");
    }

    [Test]
    public void Fire_BurnsOncePerInterval_NotEveryFrame()
    {
        var world = new FakeWorld();
        var victim = world.Add(1, new Vector2(0f, 0f), health: 10);
        var dragon = Dragon(victim, Vector2.zero);

        dragon.TickArea(0.1f, world, PlayerRadius);
        Assert.AreEqual(9, victim.Health.Current, "İlk yanma hemen");

        for (int i = 0; i < 9; i++) dragon.TickArea(0.1f, world, PlayerRadius); // 1.0 sn'ye kadar
        Assert.AreEqual(9, victim.Health.Current, "Aralık dolana kadar tekrar yanmaz");

        dragon.TickArea(0.2f, world, PlayerRadius);
        Assert.AreEqual(8, victim.Health.Current, "1 sn sonra ikinci yanma");
    }

    [Test]
    public void Fire_BurnsEveryoneInsideTheRadius_ButNotOutsiders()
    {
        var world = new FakeWorld();
        var target = world.Add(1, new Vector2(0f, 0f));
        var bystander = world.Add(2, new Vector2(2.3f, 0f));   // yarıçap 2 + 0.5 içinde
        var outsider = world.Add(3, new Vector2(5f, 0f));
        var dragon = Dragon(target, Vector2.zero);

        int inside = dragon.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(2, inside);
        Assert.AreEqual(2, target.Health.Current);
        Assert.AreEqual(2, bystander.Health.Current);
        Assert.AreEqual(3, outsider.Health.Current);
    }

    [Test]
    public void Fire_DoesNotBurnProtectedPlayers_InACave()
    {
        var world = new FakeWorld();
        var cave = world.Add(1, new Vector2(0f, 0f));
        var exposed = world.Add(2, new Vector2(0.5f, 0f));
        var protection = new FakeProtection();
        protection.Protected.Add(cave.Id);
        var dragon = Dragon(cave, Vector2.zero);
        dragon.Protection = protection;

        dragon.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(3, cave.Health.Current);
        Assert.AreEqual(2, exposed.Health.Current);
    }

    [Test]
    public void Fire_BurnsFlyingPlayersToo_AndShieldAbsorbsOneBurn()
    {
        var world = new FakeWorld();
        var flyer = world.Add(1, Vector2.zero);
        flyer.Status.ApplyFlight(10f, 1.2f);
        var shielded = world.Add(2, new Vector2(0.4f, 0f));
        shielded.Status.ApplyShield(1, 30f);
        var dragon = Dragon(flyer, Vector2.zero);

        dragon.TickArea(0.1f, world, PlayerRadius);

        Assert.AreEqual(2, flyer.Health.Current, "Ateş yerdeki temas değil, uçmak korumaz");
        Assert.AreEqual(3, shielded.Health.Current);
        Assert.IsFalse(shielded.Status.HasShield);
    }

    [Test]
    public void Dragon_FliesToItsTarget_AndIgnoresWalls()
    {
        var field = new WallField();
        field.Add(new WallSegment(Vector2.zero, Vector2.up, 4f, 0.5f, 60f));
        var world = new FakeWorld();
        var target = world.Add(1, new Vector2(6f, 0f));
        var dragon = Dragon(target, new Vector2(-6f, 0f));
        dragon.Obstacles = field;

        for (int i = 0; i < 40; i++) dragon.TickArea(0.1f, world, PlayerRadius); // 4 sn × 3,6 = 14,4 birim

        Assert.Greater(dragon.Position.x, 5f, "Duvarın üstünden uçup hedefe ulaştı");
    }

    // ---- Hedef değiştirme ----

    [Test]
    public void Dragon_WhenTargetDies_PicksTheNearestAlivePlayer_AndVanishesWhenNobodyIsLeft()
    {
        var world = new FakeWorld();
        var first = world.Add(1, new Vector2(0f, 0f));
        var near = world.Add(2, new Vector2(3f, 0f));
        var far = world.Add(3, new Vector2(8f, 0f));
        var dragon = Dragon(first, Vector2.zero);

        first.Health.TakeDamage(99);
        dragon.TickArea(0.1f, world, PlayerRadius);
        Assert.AreSame(near, dragon.Target, "En yakın hayattakine geçti");
        Assert.IsFalse(dragon.IsFinished);

        near.Health.TakeDamage(99);
        dragon.TickArea(0.1f, world, PlayerRadius);
        Assert.AreSame(far, dragon.Target);

        far.Health.TakeDamage(99);
        dragon.TickArea(0.1f, world, PlayerRadius);
        Assert.IsTrue(dragon.IsFinished);
    }

    [Test]
    public void Dragon_DoesNotRetargetToAStealthedPlayer()
    {
        var world = new FakeWorld();
        var first = world.Add(1, new Vector2(0f, 0f));
        var hidden = world.Add(2, new Vector2(1f, 0f));
        hidden.Status.ApplyStealth(10f);
        var visible = world.Add(3, new Vector2(7f, 0f));
        var dragon = Dragon(first, Vector2.zero);

        first.Health.TakeDamage(99);
        dragon.TickArea(0.1f, world, PlayerRadius);

        Assert.AreSame(visible, dragon.Target);
    }

    [Test]
    public void Cloud_StillVanishesWhenItsTargetDies_ItDoesNotRetarget()
    {
        var world = new FakeWorld();
        var target = world.Add(1, Vector2.zero);
        world.Add(2, new Vector2(3f, 0f));
        var cloud = CreatureFactory.CreateAttack(WordDatabase.LoadFromResources(50).Lookup("bulut").Entry, target, Vector2.zero, config, arena);
        target.Health.TakeDamage(99);

        cloud.TickArea(0.1f, world, PlayerRadius);

        Assert.IsTrue(cloud.IsFinished);
    }

    [Test]
    public void FireDamage_RespectsTheDamageOfTheWord_AndZeroDamageDoesNotBurn()
    {
        var world = new FakeWorld();
        var victim = world.Add(1, Vector2.zero, health: 10);
        var word = new WordEntry { id = "x", tags = new[] { "fire", "area" }, stats = new WordStats { damage = 3, radius = 2f, duration = 5f, speed = 1f } };
        var strong = CreatureFactory.CreateAttack(word, victim, Vector2.zero, config, arena);
        strong.TickArea(0.1f, world, PlayerRadius);
        Assert.AreEqual(7, victim.Health.Current);

        var harmless = new WordEntry { id = "y", tags = new[] { "fire", "area" }, stats = new WordStats { damage = 0, radius = 2f, duration = 5f, speed = 1f } };
        var none = CreatureFactory.CreateAttack(harmless, victim, Vector2.zero, config, arena);
        none.TickArea(0.1f, world, PlayerRadius);
        Assert.AreEqual(7, victim.Health.Current);
    }

    // ---- Etiket ve tehdit akışı ----

    [Test]
    public void FireTag_ProducesAHordeEffect_ForThreatWords_ButNotWhenMapWideAlreadyDoes()
    {
        var effects = TagRegistry.CreateThreatEffects(Ejderha());
        Assert.AreEqual(1, effects.Count);
        Assert.IsInstanceOf<HordeEffect>(effects[0]);

        var both = new WordEntry { id = "x", tags = new[] { "fire", "map_wide" } };
        Assert.AreEqual(1, TagRegistry.CreateThreatEffects(both).Count, "map_wide'ın etkisi var, fire ikinci bir etki eklemez");
    }

    [Test]
    public void Ejderha_AsAGlobalThreat_WarnsFirst_ThenOneDragonWithItsName_AndThreeCaves()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 8; i++) world.Add(i, new Vector2(-8f + i * 2f, 0f));
        var sink = new CollectingSink();
        var threat = new GlobalThreat(Ejderha(), world.Players[0], config, arena, world, new Random(2), sink);

        Assert.AreEqual(ThreatPhase.Warning, threat.Phase, "Önce uyarı: mağaraya koşacak süre");
        Assert.AreEqual(7f, threat.WarningLeft, 0.001f, "words.json: warningSeconds 7");
        Assert.AreEqual(0, sink.Models.Count, "Uyarı bitmeden ejderha gelmez");

        threat.Tick(6.9f);
        Assert.AreEqual(0, sink.Models.Count);
        threat.Tick(0.2f);
        Assert.AreEqual(ThreatPhase.Active, threat.Phase);
        Assert.AreEqual(1, sink.Models.Count);
        // Ad dilden bağımsız bir belirteçle gelir; ekrana basan taraf seçili dildeki adı yazar.
        Assert.AreEqual(CreatureLabel.NameToken, sink.Labels[0], "Tek yaratıkta adı yazılır");
        Assert.AreEqual(Ejderha().DisplayFor(Language.Turkish), CreatureLabel.Resolve(sink.Labels[0], sink.Models[0].Word));
        Assert.AreEqual(3, threat.Zones.Zones.Count, "8 oyuncu: 3 mağara x 4 kişi = 12 yer");
        foreach (SafeZone cave in threat.Zones.Zones)
        {
            Assert.AreEqual("cave", cave.Type);
            Assert.AreEqual(4, cave.Capacity);
        }

        CreatureModel dragon = sink.Models[0];
        Assert.IsTrue(dragon.TargetsNearest);
        Assert.AreSame(threat.Zones, dragon.Protection);
    }

    [Test]
    public void Ejderha_Flow_EndsAfterWarningPlus10Seconds_AndCavesDisappearAfterTheLinger()
    {
        var world = new FakeWorld();
        for (int i = 1; i <= 4; i++) world.Add(i, new Vector2(-4f + i * 2f, 0f));
        var threat = new GlobalThreat(Ejderha(), world.Players[0], config, arena, world, new Random(2), new CollectingSink());

        threat.Tick(7.1f);  // Uyarı (7 sn) bitti, ejderha geldi
        threat.Tick(9.8f);
        // 4 oyuncu: mağara sayısı oyuncu sayısına göre (3 * 4 / 8 = 1,5 → 2).
        Assert.AreEqual(ThreatSpec.GetZoneCount(Ejderha(), 4, config), threat.Zones.Zones.Count);
        Assert.AreEqual(2, threat.Zones.Zones.Count);
        Assert.AreEqual(ThreatPhase.Active, threat.Phase);

        threat.Tick(0.2f);
        threat.Tick(config.safeZoneLingerSeconds + 0.5f);
        Assert.AreEqual(ThreatPhase.Finished, threat.Phase);
        Assert.AreEqual(0, threat.Zones.Zones.Count);
    }

    [Test]
    public void Dragon_TargetsTheNearestPlayerToWhereItEntered()
    {
        var world = new FakeWorld();
        world.Add(1, new Vector2(-9f, 0f));
        world.Add(2, new Vector2(9f, 0f));
        var sink = new CollectingSink();
        var context = new ThreatContext(Ejderha(), null, config, arena, world, new Random(1), new SafeZoneField(), sink);
        new HordeEffect().Begin(context);

        CreatureModel dragon = sink.Models[0];
        world.TryGetPosition(dragon.Target.Id, out Vector2 targetPosition);
        foreach (PlayerState other in world.Players)
        {
            world.TryGetPosition(other.Id, out Vector2 position);
            Assert.LessOrEqual((targetPosition - dragon.Position).magnitude, (position - dragon.Position).magnitude + 0.0001f);
        }
    }
}
