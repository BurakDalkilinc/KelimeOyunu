using NUnit.Framework;
using UnityEngine;

/// <summary>ArenaBounds sınıfının testleri: sınırlar içinde tutma.</summary>
public class ArenaBoundsTests
{
    [Test]
    public void Clamp_InsidePoint_StaysSame()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        Assert.AreEqual(new Vector2(3f, -2f), arena.Clamp(new Vector2(3f, -2f), 0.5f));
    }

    [Test]
    public void Clamp_OutsidePoint_IsPulledToEdgeMinusMargin()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        Assert.AreEqual(new Vector2(9.5f, 5.5f), arena.Clamp(new Vector2(100f, 100f), 0.5f));
        Assert.AreEqual(new Vector2(-9.5f, -5.5f), arena.Clamp(new Vector2(-100f, -100f), 0.5f));
    }

    [Test]
    public void Clamp_MarginBiggerThanArena_ReturnsCenter()
    {
        var arena = new ArenaBounds(new Vector2(2f, 2f));
        Assert.AreEqual(Vector2.zero, arena.Clamp(new Vector2(5f, 5f), 10f));
    }

    [Test]
    public void Contains_ChecksEdges()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        Assert.IsTrue(arena.Contains(new Vector2(10f, 6f)));
        Assert.IsFalse(arena.Contains(new Vector2(10.1f, 0f)));
    }

    [Test]
    public void Constructor_ZeroSize_IsMadePositive()
    {
        var arena = new ArenaBounds(Vector2.zero);
        Assert.Greater(arena.Size.x, 0f);
        Assert.Greater(arena.Size.y, 0f);
    }
}
