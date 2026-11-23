using NUnit.Framework;
using UnityEngine;

/// <summary>Takip kamerası hesabı testleri: arena dışına taşmama, yumuşak takip, görüş boyutu.</summary>
public class CameraFollowMathTests
{
    static readonly Vector2 ArenaHalf = new Vector2(17f, 9.5f); // 34 x 19
    static readonly Vector2 ViewHalf = new Vector2(12f, 6.75f); // 384 x 216 piksel, 16 piksel/birim

    [Test]
    public void ViewHalf_FromReferenceResolution()
    {
        Vector2 half = CameraFollowMath.ViewHalf(384, 216, 16);

        Assert.AreEqual(12f, half.x, 0.0001f);
        Assert.AreEqual(6.75f, half.y, 0.0001f);
    }

    [Test]
    public void Clamp_TargetInTheMiddle_IsFollowedExactly()
    {
        Vector2 result = CameraFollowMath.Clamp(new Vector2(2f, -1f), ArenaHalf, ViewHalf, 1f);

        Assert.AreEqual(2f, result.x, 0.0001f);
        Assert.AreEqual(-1f, result.y, 0.0001f);
    }

    [Test]
    public void Clamp_NearTheEdge_NeverShowsBeyondTheMargin()
    {
        Vector2 result = CameraFollowMath.Clamp(new Vector2(16f, 9f), ArenaHalf, ViewHalf, 1f);

        // Merkez en çok arenaHalf + margin - viewHalf kadar gidebilir.
        Assert.AreEqual(17f + 1f - 12f, result.x, 0.0001f);
        Assert.AreEqual(9.5f + 1f - 6.75f, result.y, 0.0001f);

        Vector2 other = CameraFollowMath.Clamp(new Vector2(-16f, -9f), ArenaHalf, ViewHalf, 1f);
        Assert.AreEqual(-(17f + 1f - 12f), other.x, 0.0001f);
        Assert.AreEqual(-(9.5f + 1f - 6.75f), other.y, 0.0001f);
    }

    [Test]
    public void Clamp_WhenTheViewIsLargerThanTheArena_CentersOnThatAxis()
    {
        Vector2 overview = CameraFollowMath.ViewHalf(640, 360, 16); // 20 x 11,25 > arena

        Vector2 result = CameraFollowMath.Clamp(new Vector2(15f, -8f), ArenaHalf, overview, 1f);

        Assert.AreEqual(0f, result.x, 0.0001f); // 17 + 1 - 20 < 0: ortala
        Assert.AreEqual(0f, result.y, 0.0001f);
    }

    [Test]
    public void Clamp_OnlyTheTooLargeAxisIsCentered()
    {
        var wide = new Vector2(30f, 5f); // yatayda arenadan geniş, dikeyde dar

        Vector2 result = CameraFollowMath.Clamp(new Vector2(10f, 7f), ArenaHalf, wide, 1f);

        Assert.AreEqual(0f, result.x, 0.0001f);
        Assert.AreEqual(5.5f, result.y, 0.0001f); // 9,5 + 1 - 5
    }

    [Test]
    public void Smooth_MovesTowardTheTarget_WithoutOvershoot()
    {
        Vector2 position = Vector2.zero;
        Vector2 target = new Vector2(10f, 0f);

        for (int i = 0; i < 20; i++)
        {
            Vector2 next = CameraFollowMath.Smooth(position, target, 1f / 60f, 8f);
            Assert.GreaterOrEqual(next.x, position.x);
            Assert.LessOrEqual(next.x, target.x);
            position = next;
        }

        Assert.Greater(position.x, 3f);
        Assert.Less(position.x, 10f);
    }

    [Test]
    public void Smooth_IsFrameRateIndependent()
    {
        Vector2 a = Vector2.zero, b = Vector2.zero, target = new Vector2(10f, 4f);

        for (int i = 0; i < 60; i++) a = CameraFollowMath.Smooth(a, target, 1f / 60f, 6f);
        for (int i = 0; i < 30; i++) b = CameraFollowMath.Smooth(b, target, 1f / 30f, 6f);

        Assert.AreEqual(a.x, b.x, 0.001f);
        Assert.AreEqual(a.y, b.y, 0.001f);
    }

    [Test]
    public void Smooth_ZeroTimeOrNegativeSharpness_DoesNotMove()
    {
        Vector2 start = new Vector2(1f, 2f);

        Assert.AreEqual(start, CameraFollowMath.Smooth(start, new Vector2(9f, 9f), 0f, 8f));
        Assert.AreEqual(start, CameraFollowMath.Smooth(start, new Vector2(9f, 9f), 1f, -3f));
    }

    [Test]
    public void FullFollowView_AlwaysContainsTheArenaEdgeNearThePlayer()
    {
        // Oyuncu arenanın köşesindeyken (16, 9) kamera sınırlanır ama oyuncu görüş içinde kalır.
        Vector2 player = new Vector2(16.5f, 9f);
        Vector2 camera = CameraFollowMath.Clamp(player, ArenaHalf, ViewHalf, 1.2f);

        Assert.LessOrEqual(Mathf.Abs(player.x - camera.x), ViewHalf.x);
        Assert.LessOrEqual(Mathf.Abs(player.y - camera.y), ViewHalf.y);
    }
}
