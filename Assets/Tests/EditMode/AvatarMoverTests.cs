using NUnit.Framework;
using UnityEngine;

/// <summary>AvatarMover testleri: host'un hareket denetimi (hız sınırı, arena sınırı, geçersiz girdi).</summary>
public class AvatarMoverTests
{
    static readonly ArenaBounds Arena = new ArenaBounds(new Vector2(20f, 12f));
    const float Radius = 0.5f;

    [Test]
    public void SanitizeInput_ShortensTooLongVectors_ToLengthOne()
    {
        Vector2 result = AvatarMover.SanitizeInput(new Vector2(100f, 0f));
        Assert.AreEqual(1f, result.magnitude, 0.0001f);

        Vector2 diagonal = AvatarMover.SanitizeInput(new Vector2(1f, 1f));
        Assert.AreEqual(1f, diagonal.magnitude, 0.0001f);
    }

    [Test]
    public void SanitizeInput_KeepsShortVectors()
    {
        Assert.AreEqual(new Vector2(0.3f, 0f), AvatarMover.SanitizeInput(new Vector2(0.3f, 0f)));
    }

    [Test]
    public void SanitizeInput_NaNAndInfinity_BecomeZero()
    {
        Assert.AreEqual(Vector2.zero, AvatarMover.SanitizeInput(new Vector2(float.NaN, 1f)));
        Assert.AreEqual(Vector2.zero, AvatarMover.SanitizeInput(new Vector2(1f, float.PositiveInfinity)));
    }

    [Test]
    public void Step_MovesBySpeedTimesTime()
    {
        Vector2 result = AvatarMover.Step(Vector2.zero, Vector2.right, 5f, 0.2f, Radius, Arena, null);
        Assert.AreEqual(1f, result.x, 0.0001f);
        Assert.AreEqual(0f, result.y, 0.0001f);
    }

    [Test]
    public void Step_CheatingHugeInput_StillMovesAtNormalSpeed()
    {
        Vector2 honest = AvatarMover.Step(Vector2.zero, Vector2.up, 5f, 0.2f, Radius, Arena, null);
        Vector2 cheater = AvatarMover.Step(Vector2.zero, new Vector2(0f, 9999f), 5f, 0.2f, Radius, Arena, null);

        Assert.AreEqual(honest.y, cheater.y, 0.0001f);
    }

    [Test]
    public void Step_StopsAtTheArenaEdge()
    {
        Vector2 result = AvatarMover.Step(new Vector2(9.4f, 0f), Vector2.right, 50f, 1f, Radius, Arena, null);
        Assert.AreEqual(10f - Radius, result.x, 0.0001f);
    }

    [Test]
    public void Step_ZeroInput_DoesNotMove()
    {
        Vector2 start = new Vector2(2f, 3f);
        Assert.AreEqual(start, AvatarMover.Step(start, Vector2.zero, 5f, 0.5f, Radius, Arena, null));
    }

    [Test]
    public void Step_NonPositiveTimeOrSpeed_DoesNotMove()
    {
        Vector2 start = new Vector2(2f, 3f);
        Assert.AreEqual(start, AvatarMover.Step(start, Vector2.right, 5f, 0f, Radius, Arena, null));
        Assert.AreEqual(start, AvatarMover.Step(start, Vector2.right, 5f, -1f, Radius, Arena, null));
        Assert.AreEqual(start, AvatarMover.Step(start, Vector2.right, 0f, 0.5f, Radius, Arena, null));
    }

    [Test]
    public void Step_PositionOutsideArena_IsPulledBackInside()
    {
        Vector2 result = AvatarMover.Step(new Vector2(50f, 0f), Vector2.zero, 5f, 0.1f, Radius, Arena, null);
        Assert.AreEqual(10f - Radius, result.x, 0.0001f);
    }
}
