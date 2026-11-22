using NUnit.Framework;
using UnityEngine;

/// <summary>ObstacleMover testleri: duvarın içinden geçmeden hareket; ağır kare (büyük adım) tünellemesi.</summary>
public class ObstacleMoverTests
{
    const float Radius = 0.5f;

    ArenaBounds arena;
    WallField field;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        field = new WallField();
        field.Add(new WallSegment(Vector2.zero, Vector2.up, 4f, 0.5f, 60f)); // Dikey duvar, x ekseninde 0.25 kalınlık yarısı
    }

    [Test]
    public void FreeMove_WithNoObstaclesInTheWay_ReachesTheTarget()
    {
        Vector2 result = ObstacleMover.Move(new Vector2(-8f, 4f), new Vector2(-5f, 4f), Radius, arena, Radius, field);
        Assert.AreEqual(-5f, result.x, 0.001f);
        Assert.AreEqual(4f, result.y, 0.001f);
    }

    [Test]
    public void NullObstacles_OnlyClampsToTheArena()
    {
        Vector2 result = ObstacleMover.Move(new Vector2(0f, 0f), new Vector2(100f, 0f), Radius, arena, Radius, null);
        Assert.AreEqual(9.5f, result.x, 0.001f);
    }

    [Test]
    public void SmallStep_IntoTheWall_StopsAtTheSurface()
    {
        Vector2 result = ObstacleMover.Move(new Vector2(-1.0f, 0f), new Vector2(-0.2f, 0f), Radius, arena, Radius, field);
        Assert.AreEqual(-(0.25f + Radius), result.x, 0.01f);
    }

    [TestCase(4f)]
    [TestCase(10f)]
    [TestCase(30f)]
    public void HugeSingleStep_AcrossTheWall_IsStillBlocked(float stepLength)
    {
        // Ağır bir karede ya da çok hızlı fırlatmada bütün hareket tek adımda gelebilir (gerçek bir hata buydu).
        Vector2 from = new Vector2(-1.5f, 0f);
        Vector2 to = from + new Vector2(stepLength, 0f);

        Vector2 result = ObstacleMover.Move(from, to, Radius, arena, Radius, field);

        Assert.LessOrEqual(result.x, -(0.25f + Radius) + 0.01f, "Duvarın karşı tarafına geçemez");
    }

    [Test]
    public void HugeSingleStep_FromTheOtherSide_IsBlockedToo()
    {
        Vector2 from = new Vector2(1.5f, 0.5f);
        Vector2 result = ObstacleMover.Move(from, from + new Vector2(-12f, 0f), Radius, arena, Radius, field);

        Assert.GreaterOrEqual(result.x, 0.25f + Radius - 0.01f);
    }

    [Test]
    public void FlyingMove_WithNullObstacles_PassesStraightOver()
    {
        Vector2 from = new Vector2(-1.5f, 0f);
        Vector2 result = ObstacleMover.Move(from, from + new Vector2(4f, 0f), Radius, arena, Radius, null);

        Assert.AreEqual(2.5f, result.x, 0.001f);
    }

    [Test]
    public void MoveAlongTheWall_SlidesAndKeepsGoing()
    {
        // Duvara dayanıp yukarı doğru yürümek: duvarın yüzeyi boyunca kayar.
        Vector2 from = new Vector2(-0.75f, -1f);
        Vector2 result = ObstacleMover.Move(from, new Vector2(-0.4f, 1f), Radius, arena, Radius, field);

        Assert.AreEqual(1f, result.y, 0.05f, "Yüzey boyunca ilerler");
        Assert.LessOrEqual(result.x, -(0.25f + Radius) + 0.01f);
    }

    [Test]
    public void ZeroMove_ReturnsTheSamePointOrResolvesIt()
    {
        Vector2 p = new Vector2(5f, 3f);
        Assert.AreEqual(p, ObstacleMover.Move(p, p, Radius, arena, Radius, field));
    }

    [Test]
    public void StartingInsideTheWall_IsPushedOutAtTheFirstStep()
    {
        Vector2 result = ObstacleMover.Move(new Vector2(0.1f, 0.5f), new Vector2(0.1f, 0.5f), Radius, arena, Radius, field);
        Assert.GreaterOrEqual(Mathf.Abs(result.x), 0.25f + Radius - 0.01f);
    }

    [Test]
    public void Move_NullArena_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => ObstacleMover.Move(Vector2.zero, Vector2.one, Radius, null, Radius, field));
    }

    // ---- Yaratık da ağır karede duvardan geçmemeli ----

    [Test]
    public void Creature_WithAHugeTimeStep_DoesNotTunnelThroughTheWall()
    {
        var target = new PlayerState(1, "H", 3);
        var wolf = new CreatureModel(null, target, new Vector2(-2f, 0f), 7f, 1, 0.5f, 60f, arena) { Obstacles = field };

        // 0.5 sn'lik tek bir ağır kare: 7 × 0.5 = 3.5 birim, duvarı aşardı.
        wolf.Tick(0.5f, new Vector2(5f, 0f), Radius, 1f);

        Assert.LessOrEqual(wolf.Position.x, -(0.25f + 0.5f) + 0.01f);
        Assert.AreEqual(3, target.Health.Current);
    }

    [Test]
    public void FlyingCreature_WithAHugeTimeStep_StillFliesOver()
    {
        var target = new PlayerState(1, "H", 3);
        var bat = new CreatureModel(null, target, new Vector2(-2f, 0f), 7f, 1, 0.5f, 60f, arena) { Obstacles = field, IsFlying = true };

        bat.Tick(0.5f, new Vector2(5f, 0f), Radius, 1f);

        Assert.Greater(bat.Position.x, 0.25f);
    }
}
