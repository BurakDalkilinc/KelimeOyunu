using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>İtiş (Boşluk) testleri: kimi vurur, ne kadar savurur, bekleme süresi ve yazılar.</summary>
public class ShoveTests
{
    const float Range = 1.9f;
    const float HalfAngle = 65f;
    const float Distance = 3.6f;

    static List<bool> AllSolid(int n)
    {
        var list = new List<bool>();
        for (int i = 0; i < n; i++) list.Add(true);
        return list;
    }

    static List<ShoveHit> Find(Vector2 facing, List<Vector2> positions, List<bool> solid = null, int shover = 0)
    {
        var results = new List<ShoveHit>();
        ShoveRules.Find(shover, facing, positions, solid ?? AllSolid(positions.Count), Range, HalfAngle, Distance, results);
        return results;
    }

    // ---- ShoveRules ----

    [Test]
    public void SomeoneInFront_IsShoved_ForwardAndFar()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(1f, 0f) };

        List<ShoveHit> hits = Find(Vector2.right, positions);

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(1, hits[0].Index);
        Assert.Greater(hits[0].Push.x, 2.5f);                   // Çarpışma itişinden çok daha güçlü
        Assert.AreEqual(0f, hits[0].Push.y, 0.001f);
        Assert.LessOrEqual(hits[0].Push.magnitude, Distance);
    }

    [Test]
    public void SomeoneBehind_OrTooFar_IsNotHit()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(-1f, 0f), new Vector2(Range + 0.5f, 0f) };

        Assert.AreEqual(0, Find(Vector2.right, positions).Count);
    }

    [Test]
    public void SomeoneToTheSide_InsideTheConeIsHit_OutsideIsNot()
    {
        var inside = new List<Vector2> { Vector2.zero, new Vector2(1.2f, 0.8f) };   // ~34 derece
        var outside = new List<Vector2> { Vector2.zero, new Vector2(0.2f, 1.2f) };  // ~80 derece

        Assert.AreEqual(1, Find(Vector2.right, inside).Count);
        Assert.AreEqual(0, Find(Vector2.right, outside).Count);
    }

    [Test]
    public void ShoveDirection_BlendsFacingAndTarget_SoSideTargetsFlySideways()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(1.2f, 0.8f) };

        ShoveHit hit = Find(Vector2.right, positions)[0];

        Assert.Greater(hit.Push.x, 0f);
        Assert.Greater(hit.Push.y, 0f); // Yukarı yana da savruldu
        Assert.Greater(hit.Push.x, hit.Push.y);
    }

    [Test]
    public void CloserTargets_FlyFurther()
    {
        var near = Find(Vector2.right, new List<Vector2> { Vector2.zero, new Vector2(0.6f, 0f) })[0];
        var far = Find(Vector2.right, new List<Vector2> { Vector2.zero, new Vector2(1.8f, 0f) })[0];

        Assert.Greater(near.Push.magnitude, far.Push.magnitude);
    }

    [Test]
    public void GhostsAndFlyers_AreNeverShoved()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(1f, 0f), new Vector2(1.2f, 0f) };
        var solid = new List<bool> { true, false, true };

        List<ShoveHit> hits = Find(Vector2.right, positions, solid);

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(2, hits[0].Index);
    }

    [Test]
    public void TheShover_NeverHitsItself()
    {
        var positions = new List<Vector2> { Vector2.zero };

        Assert.AreEqual(0, Find(Vector2.right, positions).Count);
    }

    [Test]
    public void ExactlyOnTopOfTheShover_GoesTheFacingWay()
    {
        var positions = new List<Vector2> { Vector2.zero, Vector2.zero };

        ShoveHit hit = Find(new Vector2(0f, 1f), positions)[0];

        Assert.AreEqual(0f, hit.Push.x, 0.001f);
        Assert.Greater(hit.Push.y, 0f);
    }

    [Test]
    public void ZeroFacing_DefaultsToUp()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(0f, 1f) };

        Assert.AreEqual(1, Find(Vector2.zero, positions).Count);
    }

    [Test]
    public void Several_AreAllShoved_InTheirOwnDirections()
    {
        var positions = new List<Vector2> { Vector2.zero, new Vector2(1f, 0.3f), new Vector2(1.2f, -0.3f), new Vector2(-1f, 0f) };

        List<ShoveHit> hits = Find(Vector2.right, positions);

        Assert.AreEqual(2, hits.Count); // Arkadaki etkilenmedi
        Assert.Greater(hits.Find(h => h.Index == 1).Push.y, 0f);
        Assert.Less(hits.Find(h => h.Index == 2).Push.y, 0f);
    }

    [Test]
    public void InvalidShoverIndex_DoesNotThrow_AndHitsNobody()
    {
        var positions = new List<Vector2> { Vector2.zero };

        var results = new List<ShoveHit> { new ShoveHit(5, Vector2.one) };
        Assert.DoesNotThrow(() => ShoveRules.Find(9, Vector2.right, positions, AllSolid(1), Range, HalfAngle, Distance, results));
        Assert.AreEqual(0, results.Count);
    }

    [Test]
    public void ShoveIsMuchStrongerThanTheBodyPush()
    {
        // Çarpışma itişi en çok yarıçap (0,5) kadar; itiş birkaç birim savurur.
        Assert.Greater(Distance, 0.5f * 4f);
    }

    // ---- ShoveCooldown ----

    [Test]
    public void Cooldown_StartsReady_ThenBlocksUntilItRunsOut()
    {
        var cooldown = new ShoveCooldown(3f);
        Assert.IsTrue(cooldown.IsReady);

        Assert.IsTrue(cooldown.TryUse());
        Assert.IsFalse(cooldown.IsReady);
        Assert.IsFalse(cooldown.TryUse());
        Assert.AreEqual(3f, cooldown.Remaining, 0.0001f);

        cooldown.Tick(2.9f);
        Assert.IsFalse(cooldown.TryUse());

        cooldown.Tick(0.2f);
        Assert.IsTrue(cooldown.IsReady);
        Assert.AreEqual(0f, cooldown.Remaining);
        Assert.IsTrue(cooldown.TryUse());
    }

    [Test]
    public void Cooldown_NegativeTime_DoesNothing_AndResetMakesItReady()
    {
        var cooldown = new ShoveCooldown(3f);
        cooldown.TryUse();

        cooldown.Tick(-5f);
        Assert.AreEqual(3f, cooldown.Remaining, 0.0001f);

        cooldown.Reset();
        Assert.IsTrue(cooldown.IsReady);
    }

    [Test]
    public void Cooldown_NegativeSeconds_AreTreatedAsZero()
    {
        var cooldown = new ShoveCooldown(-2f);

        Assert.IsTrue(cooldown.TryUse());
        Assert.IsTrue(cooldown.IsReady);
    }

    [Test]
    public void Describe_ShowsReadyOrRoundedUpSeconds()
    {
        Assert.AreEqual("İtiş (Boşluk): hazır", ShoveCooldown.Describe(0f));
        Assert.AreEqual("İtiş: 3 sn", ShoveCooldown.Describe(2.1f));
        Assert.AreEqual("İtiş: 1 sn", ShoveCooldown.Describe(0.2f));
    }

    // ---- Ayar mantıklı mı? ----

    [Test]
    public void DefaultConfig_ShoveSettingsAreSane()
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        try
        {
            Assert.GreaterOrEqual(config.shoveCooldownSeconds, 1f);
            Assert.Greater(config.shoveDistance, config.playerRadius * 4f);
            Assert.Greater(config.shoveRange, config.playerRadius * 2f);
            Assert.Greater(config.botShoveChancePerSecond, 0f);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }
}
