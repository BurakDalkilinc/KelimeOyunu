using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>Hayalet modunun testleri: elenen oyuncu hayalet olur, hiçbir şeye takılmaz, hiçbir şey ona işlemez, hedef alınamaz.</summary>
public class GhostTests
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

    static PlayerState Dead(int id = 1)
    {
        var player = new PlayerState(id, "P" + id, 3);
        player.Health.TakeDamage(99);
        return player;
    }

    // ---- Hayalet durumu ----

    [Test]
    public void IsGhost_IsTrueOnlyAfterElimination()
    {
        var player = new PlayerState(1, "P", 3);
        Assert.IsFalse(player.IsGhost);
        Assert.IsTrue(player.IsAlive);

        player.TakeDamage(99);

        Assert.IsTrue(player.IsGhost);
        Assert.IsFalse(player.IsAlive);
    }

    // ---- Hareket kuralları ----

    [Test]
    public void Obstacles_NormalPlayer_IsBlocked_GhostAndFlyerAreNot()
    {
        var field = new WallField();
        var alive = new PlayerState(1, "A", 3);
        var flyer = new PlayerState(2, "F", 3);
        flyer.Status.ApplyFlight(5f, 1.2f);

        Assert.AreSame(field, MovementRules.EffectiveObstacles(alive, field));
        Assert.IsNull(MovementRules.EffectiveObstacles(flyer, field));
        Assert.IsNull(MovementRules.EffectiveObstacles(Dead(), field));
    }

    [Test]
    public void Obstacles_NullPlayerOrField_ReturnsNull()
    {
        Assert.IsNull(MovementRules.EffectiveObstacles(null, new WallField()));
        Assert.IsNull(MovementRules.EffectiveObstacles(new PlayerState(1, "A", 3), null));
    }

    [Test]
    public void Ghost_WalksThroughAWall_WhileAnAlivePlayerCannot()
    {
        var field = new WallField();
        field.Add(new WallSegment(Vector2.zero, Vector2.up, 4f, 0.5f, 60f));
        var alive = new PlayerState(1, "A", 3);
        var ghost = Dead(2);

        Vector2 from = new Vector2(-1.5f, 0f);
        Vector2 to = new Vector2(2.5f, 0f);

        Vector2 aliveResult = ObstacleMover.Move(from, to, 0.5f, arena, 0.5f, MovementRules.EffectiveObstacles(alive, field));
        Vector2 ghostResult = ObstacleMover.Move(from, to, 0.5f, arena, 0.5f, MovementRules.EffectiveObstacles(ghost, field));

        Assert.LessOrEqual(aliveResult.x, -0.7f);
        Assert.AreEqual(2.5f, ghostResult.x, 0.001f);
    }

    // ---- Hiçbir şey hayalete işlemez ----

    [Test]
    public void Creatures_DoNotHurtGhosts()
    {
        var ghost = Dead();
        var creature = new CreatureModel(null, ghost, Vector2.zero, 100f, 1, 0.5f, 10f, arena);

        Assert.IsFalse(creature.Tick(1f, new Vector2(3f, 0f), 0.5f, 1f));
        Assert.IsTrue(creature.IsFinished, "Hedefi hayalet olunca yaratık kaybolur");
    }

    [Test]
    public void AreaEffects_Strikes_Traps_AndWaves_SkipGhosts()
    {
        var world = new FakeWorld();
        var ghost = world.Add(1, Vector2.zero);
        ghost.Health.TakeDamage(99);
        var db = WordDatabase.LoadFromResources(50);

        var fist = CreatureFactory.CreateAttack(db.Lookup("yumruk").Entry, ghost, Vector2.zero, config, arena);
        fist.PlaceAt(Vector2.zero);
        Assert.AreEqual(0, fist.TickStrike(2f, world, 0.5f, new List<CreatureHit>()));

        var banana = CreatureFactory.CreateAttack(db.Lookup("muz_kabugu").Entry, ghost, Vector2.zero, config, arena);
        banana.PlaceAt(Vector2.zero);
        Assert.AreEqual(0, banana.TickTrap(0.1f, world, 0.5f, new List<CreatureHit>()));
        Assert.IsFalse(banana.IsFinished, "Hayalet tuzağa basmaz");

        var cloud = CreatureFactory.CreateAttack(db.Lookup("bulut").Entry, ghost, Vector2.zero, config, arena);
        Assert.AreEqual(0, cloud.TickArea(0.1f, world, 0.5f));
    }

    [Test]
    public void GlobalThreats_DoNotCountGhosts()
    {
        var world = new FakeWorld();
        var ghost = world.Add(1, new Vector2(30f, 30f));
        ghost.Health.TakeDamage(99);
        var alive = world.Add(2, new Vector2(30f, 30f));
        var db = WordDatabase.LoadFromResources(50);
        var threat = new GlobalThreat(db.Lookup("atom_bombasi").Entry, alive, config, arena, world, new Random(1));

        // Sığınak kapasitesi yalnızca hayattakilere göre: 1 hayatta → en az 1.
        Assert.AreEqual(1, threat.Zones.Zones[0].Capacity);

        threat.Tick(8.1f);
        Assert.IsFalse(alive.IsAlive);
    }

    // ---- Hedef alınamaz ----

    [Test]
    public void Ghosts_CannotBeTargeted_ByPickerHitTesterOrSelection()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        var ghost = world.Add(2, new Vector2(4f, 0f));
        ghost.Health.TakeDamage(99);
        var living = world.Add(3, new Vector2(-4f, 0f));

        for (int i = 0; i < 40; i++)
            Assert.AreSame(living, TargetPicker.PickRandomOpponent(world.Players, writer, new Random(i)));

        Assert.IsNull(TargetHitTester.FindAt(new Vector2(4f, 0f), world, writer, 0.9f));
        Assert.IsFalse(new TargetSelection(writer, 3f).TrySelect(ghost));
    }

    [Test]
    public void Dice_NeverSelectsGhosts()
    {
        var ghost = Dead(1);
        ghost.MissedTurns = 99;
        var alive = new PlayerState(2, "B", 3);
        var dice = new Dice(new Random(1), 0.5f);

        for (int i = 0; i < 100; i++)
            Assert.AreSame(alive, dice.Roll(new List<PlayerState> { ghost, alive }));
    }

    // ---- Görünüm ----

    [Test]
    public void GhostTint_IsFaintAndPaleComparedToTheBaseColor()
    {
        var baseColor = new Color(0.3f, 0.85f, 0.6f, 1f);
        Color ghost = StatusTint.Ghost(baseColor);

        Assert.AreEqual(StatusTint.GhostAlpha, ghost.a, 0.0001f);
        Assert.Greater(ghost.r, baseColor.r, "Beyaza doğru solar");
        Assert.Less(ghost.a, baseColor.a);
    }

    [Test]
    public void DeathClearsStatusEffects_SoAGhostIsNotStillSlowedOrShielded()
    {
        var player = new PlayerState(1, "P", 3);
        player.Status.ApplySlow(0.5f, 10f);
        player.Status.ApplyShield(1, 30f);

        player.Status.Clear(); // PlayerController.OnDied bunu çağırır

        Assert.IsFalse(player.Status.IsSlowed);
        Assert.IsFalse(player.Status.HasShield);
        Assert.AreEqual(1f, player.Status.SpeedMultiplier, 0.0001f);
    }
}
