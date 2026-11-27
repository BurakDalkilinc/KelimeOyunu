using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Korku ipuçlarının hesabı: vinyet gücü (yakınlık, atılma hazırlığı) ve ekran dışı okun kenardaki yeri.</summary>
public class DangerSenseTests
{
    static SensedCreature At(float x, bool harmful = true, bool windup = false)
    {
        return new SensedCreature { Position = new Vector2(x, 0f), Radius = 0.5f, Harmful = harmful, PounceWindup = windup, TargetId = 1 };
    }

    [Test]
    public void Intensity_ZeroWhenFarOrHarmless()
    {
        Assert.AreEqual(0f, DangerSense.Intensity(Vector2.zero, new List<SensedCreature> { At(10f) }, 3.5f));
        Assert.AreEqual(0f, DangerSense.Intensity(Vector2.zero, new List<SensedCreature> { At(1f, harmful: false) }, 3.5f));
        Assert.AreEqual(0f, DangerSense.Intensity(Vector2.zero, null, 3.5f));
    }

    [Test]
    public void Intensity_GrowsAsCreatureApproaches_FullOnWindup()
    {
        float far = DangerSense.Intensity(Vector2.zero, new List<SensedCreature> { At(3.8f) }, 3.5f);
        float near = DangerSense.Intensity(Vector2.zero, new List<SensedCreature> { At(1f) }, 3.5f);
        Assert.Greater(far, 0f);
        Assert.Greater(near, far);
        Assert.AreEqual(1f, DangerSense.Intensity(Vector2.zero, new List<SensedCreature> { At(3.8f, windup: true) }, 3.5f));
    }

    [Test]
    public void EdgeArrow_NoneWhenOnScreen()
    {
        Assert.IsFalse(DangerSense.TryEdgeArrow(new Vector2(0.4f, 0.7f), Vector2.zero, out _, out _));
    }

    [Test]
    public void EdgeArrow_SitsOnEdgeAndPointsTowardCreature()
    {
        Assert.IsTrue(DangerSense.TryEdgeArrow(new Vector2(2f, 0.5f), new Vector2(0.05f, 0.05f), out Vector2 right, out float rightAngle));
        Assert.AreEqual(0.95f, right.x, 0.001f);
        Assert.AreEqual(0.5f, right.y, 0.001f);
        Assert.AreEqual(0f, rightAngle, 0.01f);

        Assert.IsTrue(DangerSense.TryEdgeArrow(new Vector2(0.5f, -1f), Vector2.zero, out Vector2 below, out float belowAngle));
        Assert.AreEqual(0f, below.y, 0.001f);
        Assert.AreEqual(-90f, belowAngle, 0.01f);
    }

    [Test]
    public void Pulse_StaysBetweenHalfAndOne()
    {
        for (float t = 0f; t < 2f; t += 0.05f)
        {
            float p = DangerSense.Pulse(t, 2f);
            Assert.GreaterOrEqual(p, 0.549f);
            Assert.LessOrEqual(p, 1.001f);
        }
    }
}
