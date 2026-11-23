using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Duvarın testleri: WallSegment (itme), WallField, "wall" etiketi (Duvar kelimesi) ve yaratıkların duvara takılması.</summary>
public class WallTests
{
    const float Radius = 0.5f;

    ArenaBounds arena;
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // önünde 1.8, kalınlık 0.5, uzunluk 4, ömür 10
        config.arenaSize = new Vector2(20f, 12f); // Testlerdeki koordinatlar bu küçük haritaya göre
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    /// <summary>Merkezi (0,0), dikey duran (y ekseni boyunca) 4 uzunluğunda, 0.5 kalınlığında duvar.</summary>
    static WallSegment VerticalWall(float life = 10f) => new WallSegment(Vector2.zero, Vector2.up, 4f, 0.5f, life);

    // ---- WallSegment.PushOut ----

    [Test]
    public void PushOut_FarFromWall_ReturnsTheSamePoint()
    {
        Vector2 p = new Vector2(3f, 0f);
        Assert.AreEqual(p, VerticalWall().PushOut(p, Radius));
    }

    [Test]
    public void PushOut_TouchingExactlyAtRadius_DoesNotMove()
    {
        // Duvar yüzeyi x=0.25; daire merkezi 0.25+0.5 = 0.75'te tam değiyor.
        Vector2 p = new Vector2(0.75f, 0f);
        Assert.AreEqual(p, VerticalWall().PushOut(p, Radius));
    }

    [Test]
    public void PushOut_OverlappingTheSide_IsPushedOutPerpendicular()
    {
        Vector2 result = VerticalWall().PushOut(new Vector2(0.5f, 1f), Radius);

        Assert.AreEqual(0.75f, result.x, 0.001f);  // Yüzeyden tam yarıçap kadar uzak
        Assert.AreEqual(1f, result.y, 0.001f);     // Boyuna konum değişmez
    }

    [Test]
    public void PushOut_OnTheOtherSide_IsPushedTheOtherWay()
    {
        Vector2 result = VerticalWall().PushOut(new Vector2(-0.4f, -1f), Radius);

        Assert.AreEqual(-0.75f, result.x, 0.001f);
        Assert.AreEqual(-1f, result.y, 0.001f);
    }

    [Test]
    public void PushOut_NearTheEndCap_IsPushedRadiallyFromTheCorner()
    {
        // Duvarın üst ucu y=2; köşe (0.25, 2). Nokta köşenin biraz üstünde ve yanında.
        Vector2 result = VerticalWall().PushOut(new Vector2(0.4f, 2.2f), Radius);

        Vector2 corner = new Vector2(0.25f, 2f);
        Assert.AreEqual(Radius, (result - corner).magnitude, 0.001f, "Köşeden tam yarıçap kadar uzakta");
        Assert.Greater(result.y, 2f, "Uç kenarından dışarı");
    }

    [Test]
    public void PushOut_CenterInsideTheWall_IsPushedOutThroughTheNearestFace()
    {
        // Duvar ince (0.5); içeride (0.1, 0.3) → yan yüzeyden çıkmak en yakın.
        Vector2 result = VerticalWall().PushOut(new Vector2(0.1f, 0.3f), Radius);

        Assert.AreEqual(0.25f + Radius, result.x, 0.001f);
        Assert.AreEqual(0.3f, result.y, 0.001f);
    }

    [Test]
    public void PushOut_CenterInsideOnTheLeftHalf_IsPushedLeft()
    {
        Vector2 result = VerticalWall().PushOut(new Vector2(-0.1f, 0.3f), Radius);
        Assert.AreEqual(-(0.25f + Radius), result.x, 0.001f);
    }

    [Test]
    public void PushOut_ExactlyAtTheCenter_StillGivesAFinitePointOutsideTheWall()
    {
        Vector2 result = VerticalWall().PushOut(Vector2.zero, Radius);

        Assert.IsFalse(float.IsNaN(result.x) || float.IsNaN(result.y));
        Assert.GreaterOrEqual(Mathf.Abs(result.x), 0.25f + Radius - 0.001f);
    }

    [Test]
    public void PushOut_RotatedWall_WorksAlongItsOwnAxis()
    {
        // 45° duran duvar: yönü (1,1). Normali (-1,1)/√2 doğrultusunda itmeli.
        var wall = new WallSegment(Vector2.zero, new Vector2(1f, 1f), 6f, 0.5f, 10f);
        Vector2 normal = new Vector2(-1f, 1f).normalized;

        Vector2 result = wall.PushOut(normal * 0.4f, Radius);

        Assert.AreEqual(0.25f + Radius, Vector2.Dot(result, normal), 0.001f);
        Assert.AreEqual(0f, Vector2.Dot(result, new Vector2(1f, 1f).normalized), 0.001f);
    }

    [Test]
    public void PushOut_ExpiredWall_DoesNothing()
    {
        var wall = VerticalWall(life: 1f);
        wall.Tick(2f);

        Vector2 p = new Vector2(0.1f, 0f);
        Assert.IsTrue(wall.IsExpired);
        Assert.AreEqual(p, wall.PushOut(p, Radius));
    }

    [Test]
    public void PushOut_IsIdempotent_PushedPointIsNotPushedAgain()
    {
        var wall = VerticalWall();
        Vector2 once = wall.PushOut(new Vector2(0.3f, 0.5f), Radius);
        Vector2 twice = wall.PushOut(once, Radius);

        Assert.AreEqual(once.x, twice.x, 0.0001f);
        Assert.AreEqual(once.y, twice.y, 0.0001f);
    }

    [Test]
    public void Segment_NormalizesDirectionAndClampsSizes()
    {
        var wall = new WallSegment(Vector2.zero, new Vector2(0f, 10f), -3f, 0f, -5f);

        Assert.AreEqual(Vector2.up, wall.Direction);
        Assert.GreaterOrEqual(wall.Length, 0.1f);
        Assert.GreaterOrEqual(wall.Thickness, 0.1f);
        Assert.IsTrue(wall.IsExpired);

        Assert.AreEqual(Vector2.right, new WallSegment(Vector2.zero, Vector2.zero, 1f, 1f, 1f).Direction);
    }

    [Test]
    public void Segment_Tick_CountsDownAndNeverGoesNegative_NegativeDeltaIgnored()
    {
        var wall = VerticalWall(life: 2f);
        wall.Tick(-5f);
        Assert.AreEqual(2f, wall.LifeLeft, 0.0001f);

        wall.Tick(5f);
        Assert.AreEqual(0f, wall.LifeLeft, 0.0001f);
        Assert.IsTrue(wall.IsExpired);
    }

    // ---- WallField ----

    [Test]
    public void Field_AddRaisesEvent_AndTickRemovesExpiredWalls()
    {
        var field = new WallField();
        WallSegment announced = null;
        field.Added += w => announced = w;

        var wall = field.Add(VerticalWall(life: 2f));

        Assert.AreSame(wall, announced);
        Assert.AreEqual(1, field.Walls.Count);

        field.Tick(1f);
        Assert.AreEqual(1, field.Walls.Count);

        field.Tick(1.1f);
        Assert.AreEqual(0, field.Walls.Count);
    }

    [Test]
    public void Field_ExpiredWallIsNotAdded_AndNullThrows()
    {
        var field = new WallField();
        field.Add(VerticalWall(life: 0f));
        Assert.AreEqual(0, field.Walls.Count);
        Assert.Throws<System.ArgumentNullException>(() => field.Add(null));
    }

    [Test]
    public void Field_Resolve_EmptyFieldReturnsTheSamePoint()
    {
        var field = new WallField();
        Vector2 p = new Vector2(1f, 2f);
        Assert.AreEqual(p, field.Resolve(p, Radius));
    }

    [Test]
    public void Field_Resolve_BlocksAWalkerTryingToPassThrough()
    {
        var field = new WallField();
        field.Add(VerticalWall());

        // Soldan sağa yürüyen: duvarın soluna dayanır, karşıya geçemez.
        Vector2 position = new Vector2(-3f, 0f);
        for (int i = 0; i < 100; i++)
            position = field.Resolve(position + new Vector2(0.1f, 0f), Radius);

        Assert.AreEqual(-(0.25f + Radius), position.x, 0.01f);
    }

    [Test]
    public void Field_Resolve_WalkingAroundTheWallEndIsPossible()
    {
        var field = new WallField();
        field.Add(VerticalWall());

        // Duvarın üst ucunun (y=2) yukarısından dolaşıp karşıya geçilebilir.
        Vector2 position = new Vector2(-3f, 3f);
        for (int i = 0; i < 80; i++)
            position = field.Resolve(position + new Vector2(0.1f, 0f), Radius);

        Assert.Greater(position.x, 3f, "Duvarın üstünden dolaşan karşıya geçti");
    }

    [Test]
    public void Field_Resolve_TwoWallsMeetingInACorner_EndsOutsideBoth()
    {
        var field = new WallField();
        field.Add(new WallSegment(new Vector2(0f, 0f), Vector2.up, 4f, 0.5f, 10f));       // Dikey
        field.Add(new WallSegment(new Vector2(1.8f, -1.8f), Vector2.right, 4f, 0.5f, 10f)); // Yatay, alt köşede birleşiyor

        Vector2 resolved = field.Resolve(new Vector2(0.6f, -1.6f), Radius);

        foreach (WallSegment wall in field.Walls)
            Assert.AreEqual(resolved, wall.PushOut(resolved, Radius), "Her iki duvarın da dışında olmalı");
    }

    // ---- "wall" etiketi / Duvar kelimesi ----

    [Test]
    public void Duvar_FromRealWords_BuildsAWallInFrontOfTheWriter()
    {
        var writer = new PlayerState(1, "Yazan", 3);
        var field = new WallField();
        var context = new SelfEffectContext(writer, Real("duvar"), config, new Vector2(-4f, 0f), Vector2.right, field);

        int applied = TagRegistry.ApplySelfEffects(Real("duvar"), context);

        Assert.AreEqual(1, applied);
        Assert.AreEqual(1, field.Walls.Count);
        WallSegment wall = field.Walls[0];
        Assert.AreEqual(-4f + config.wallDistanceInFront, wall.Center.x, 0.001f);
        Assert.AreEqual(0f, wall.Center.y, 0.001f);
        Assert.AreEqual(4f, wall.Length, 0.001f);       // words.json: length 4
        Assert.AreEqual(10f, wall.TotalLife, 0.001f);   // words.json: duration 10
        Assert.AreEqual(config.wallThickness, wall.Thickness, 0.001f);
        Assert.AreEqual(0f, Mathf.Abs(Vector2.Dot(wall.Direction, Vector2.right)), 0.001f, "Duvar baktığı yöne dik uzanır");
    }

    [Test]
    public void Duvar_FacingUp_IsAHorizontalWallAbove()
    {
        var field = new WallField();
        var context = new SelfEffectContext(new PlayerState(1, "Y", 3), Real("duvar"), config, Vector2.zero, Vector2.up, field);

        TagRegistry.ApplySelfEffects(Real("duvar"), context);

        WallSegment wall = field.Walls[0];
        Assert.AreEqual(config.wallDistanceInFront, wall.Center.y, 0.001f);
        Assert.AreEqual(1f, Mathf.Abs(Vector2.Dot(wall.Direction, Vector2.right)), 0.001f);
    }

    [Test]
    public void Duvar_WithoutFacing_DefaultsToUp_AndNearTheWallItStaysInsideTheArena()
    {
        var field = new WallField();
        var context = new SelfEffectContext(new PlayerState(1, "Y", 3), Real("duvar"), config, new Vector2(0f, 5.8f), Vector2.zero, field);

        TagRegistry.ApplySelfEffects(Real("duvar"), context);

        Assert.IsTrue(arena.Contains(field.Walls[0].Center));
        Assert.LessOrEqual(field.Walls[0].Center.y, 6f);
    }

    [Test]
    public void Duvar_WithoutWallField_DoesNothingAndDoesNotThrow()
    {
        var context = new SelfEffectContext(new PlayerState(1, "Y", 3), Real("duvar"), config, Vector2.zero, Vector2.up, null);
        Assert.DoesNotThrow(() => TagRegistry.ApplySelfEffects(Real("duvar"), context));
    }

    [Test]
    public void WallTag_WordWithoutStats_UsesConfigDefaults()
    {
        var word = new WordEntry { id = "x", tags = new[] { "wall" } };
        var field = new WallField();

        TagRegistry.ApplySelfEffects(word, new SelfEffectContext(new PlayerState(1, "Y", 3), word, config, Vector2.zero, Vector2.up, field));

        Assert.AreEqual(config.wallDefaultLength, field.Walls[0].Length, 0.001f);
        Assert.AreEqual(config.wallDefaultSeconds, field.Walls[0].TotalLife, 0.001f);
    }

    // ---- Yaratıkların duvara takılması ----

    [Test]
    public void GroundCreature_ChasingThroughAWall_IsStoppedByIt()
    {
        var field = new WallField();
        field.Add(VerticalWall());
        var target = new PlayerState(1, "H", 3);
        var wolf = new CreatureModel(null, target, new Vector2(-5f, 0f), 5f, 1, 0.5f, 60f, arena) { Obstacles = field };

        for (int i = 0; i < 100; i++)
            wolf.Tick(0.1f, new Vector2(5f, 0f), Radius, 1f);

        Assert.LessOrEqual(wolf.Position.x, -(0.25f + 0.5f) + 0.01f, "Duvarın bu yanında kaldı");
        Assert.AreEqual(3, target.Health.Current, "Hedefe ulaşamadı");
    }

    [Test]
    public void FlyingCreature_PassesOverTheWall()
    {
        var field = new WallField();
        field.Add(VerticalWall());
        var target = new PlayerState(1, "H", 3);
        var ari = CreatureFactory.CreateAttack(Real("ari"), target, new Vector2(-5f, 0f), config, arena);
        ari.Obstacles = field;

        for (int i = 0; i < 100; i++)
            ari.Tick(0.1f, new Vector2(5f, 0f), Radius, 1f);

        Assert.IsTrue(ari.IsFlying);
        Assert.IsTrue(ari.IsFinished || ari.Position.x > 1f, "Uçan arı duvardan geçip hedefe ulaştı");
        Assert.Less(target.Health.Current, 3);
    }

    [Test]
    public void ChasingCreature_DoesNotPathfindAroundTheWall_KnownLimit()
    {
        // Hedef duvarın hemen arkasında ama duvar kısa; yaratık uçtan dolaşabilir mi? (Kaba bir yol bulma yok:
        // doğrudan koşar, duvara takılır. Bu test bilinen sınırı belgeler.)
        var field = new WallField();
        field.Add(VerticalWall());
        var target = new PlayerState(1, "H", 3);
        var wolf = new CreatureModel(null, target, new Vector2(-5f, 0f), 5f, 1, 0.5f, 60f, arena) { Obstacles = field };

        for (int i = 0; i < 100; i++)
            wolf.Tick(0.1f, new Vector2(5f, 0f), Radius, 1f);

        Assert.AreEqual(3, target.Health.Current, "Basit kovalayan yaratık duvarın çevresinden dolaşmaz (bilinen sınır)");
    }

    [Test]
    public void CloudArea_IgnoresWalls()
    {
        var field = new WallField();
        field.Add(VerticalWall());
        var target = new PlayerState(1, "H", 3);
        var cloud = CreatureFactory.CreateAttack(Real("bulut"), target, new Vector2(-5f, 0f), config, arena);
        cloud.Obstacles = field;
        var world = new SimpleWorld();
        world.Add(target, new Vector2(5f, 0f));

        for (int i = 0; i < 60; i++) cloud.TickArea(0.1f, world, Radius);

        Assert.Greater(cloud.Position.x, 0.5f, "Bulut duvardan geçip hedefin üstüne gitti");
    }

    class SimpleWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public void Add(PlayerState player, Vector2 position)
        {
            players.Add(player);
            positions[player.Id] = position;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }
}
