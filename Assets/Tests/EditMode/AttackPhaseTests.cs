using NUnit.Framework;
using UnityEngine;

/// <summary>AttackPhase testleri: geçen süre → aşama + ilerleme, atılma mesafesi ve ezilme/uzama ölçeği.</summary>
public class AttackPhaseTests
{
    static AttackPhase Phase() => new AttackPhase(0.12f, 0.08f, 0.25f);

    [Test]
    public void Evaluate_WalksThroughWindupStrikeRecover_ThenIdle()
    {
        AttackPhase phase = Phase();

        Assert.AreEqual(AttackStage.Windup, phase.Evaluate(0f, out float p0));
        Assert.AreEqual(0f, p0, 0.0001f);

        Assert.AreEqual(AttackStage.Windup, phase.Evaluate(0.06f, out float p1));
        Assert.AreEqual(0.5f, p1, 0.0001f);

        Assert.AreEqual(AttackStage.Strike, phase.Evaluate(0.16f, out float p2));
        Assert.AreEqual(0.5f, p2, 0.0001f);

        Assert.AreEqual(AttackStage.Recover, phase.Evaluate(0.2f + 0.125f, out float p3));
        Assert.AreEqual(0.5f, p3, 0.0001f);

        Assert.AreEqual(AttackStage.Idle, phase.Evaluate(0.45f, out _));
        Assert.AreEqual(AttackStage.Idle, phase.Evaluate(1f, out _));
        Assert.AreEqual(AttackStage.Idle, phase.Evaluate(-0.1f, out _));
    }

    [Test]
    public void TotalAndImpactTime_FollowDurations()
    {
        AttackPhase phase = Phase();
        Assert.AreEqual(0.45f, phase.TotalSeconds, 0.0001f);
        Assert.AreEqual(0.12f, phase.ImpactTime, 0.0001f); // Darbe izi hazırlık bitince
    }

    [Test]
    public void ZeroWindup_StartsDirectlyWithStrike()
    {
        var phase = new AttackPhase(0f, 0.08f, 0.25f);
        Assert.AreEqual(AttackStage.Strike, phase.Evaluate(0f, out _));
        Assert.AreEqual(0f, phase.ImpactTime);
    }

    [Test]
    public void GiantWindup_IsLonger_StrikeComesLater()
    {
        var normal = new AttackPhase(0.12f, 0.08f, 0.25f);
        var giant = new AttackPhase(0.25f, 0.08f, 0.25f);
        Assert.AreEqual(AttackStage.Strike, normal.Evaluate(0.15f, out _));
        Assert.AreEqual(AttackStage.Windup, giant.Evaluate(0.15f, out _));
    }

    [Test]
    public void Lunge_PullsBackInWindup_GoesForwardInStrike_ReturnsInRecover()
    {
        const float pullBack = 0.35f;
        Assert.Less(AttackPhase.LungeFactor(AttackStage.Windup, 0.5f, pullBack), 0f);           // Hedefin tersine
        Assert.AreEqual(-pullBack, AttackPhase.LungeFactor(AttackStage.Windup, 1f, pullBack), 0.0001f);
        Assert.AreEqual(-pullBack, AttackPhase.LungeFactor(AttackStage.Strike, 0f, pullBack), 0.0001f); // Kesintisiz
        Assert.AreEqual(1f, AttackPhase.LungeFactor(AttackStage.Strike, 1f, pullBack), 0.0001f);
        Assert.AreEqual(1f, AttackPhase.LungeFactor(AttackStage.Recover, 0f, pullBack), 0.0001f);
        Assert.AreEqual(0f, AttackPhase.LungeFactor(AttackStage.Recover, 1f, pullBack), 0.0001f);
        Assert.AreEqual(0f, AttackPhase.LungeFactor(AttackStage.Idle, 0.5f, pullBack));
    }

    [Test]
    public void Scale_SquashesInWindup_StretchesInStrike_BackToOne()
    {
        Vector2 squashed = AttackPhase.ScaleFactor(AttackStage.Windup, 1f, 0.15f, 0.2f);
        Assert.Greater(squashed.x, 1f);
        Assert.Less(squashed.y, 1f);

        Vector2 stretched = AttackPhase.ScaleFactor(AttackStage.Strike, 1f, 0.15f, 0.2f);
        Assert.Greater(stretched.y, 1f);
        Assert.Greater(stretched.x, 1f);

        Assert.AreEqual(squashed, AttackPhase.ScaleFactor(AttackStage.Strike, 0f, 0.15f, 0.2f)); // Kesintisiz
        Assert.AreEqual(Vector2.one, AttackPhase.ScaleFactor(AttackStage.Recover, 1f, 0.15f, 0.2f));
        Assert.AreEqual(Vector2.one, AttackPhase.ScaleFactor(AttackStage.Idle, 0.3f, 0.15f, 0.2f));
    }

    [Test]
    public void StrikeFrame_ShownInStrike_AndEarlyRecoverOnly()
    {
        Assert.IsFalse(AttackPhase.ShowsStrikeFrame(AttackStage.Windup, 0.9f, 0.4f));
        Assert.IsTrue(AttackPhase.ShowsStrikeFrame(AttackStage.Strike, 0.1f, 0.4f));
        Assert.IsTrue(AttackPhase.ShowsStrikeFrame(AttackStage.Recover, 0.3f, 0.4f));
        Assert.IsFalse(AttackPhase.ShowsStrikeFrame(AttackStage.Recover, 0.5f, 0.4f));
        Assert.IsFalse(AttackPhase.ShowsStrikeFrame(AttackStage.Idle, 0f, 0.4f));
    }

    [Test]
    public void ConfigDefaults_GiantWindupLongerAndLungeStronger()
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        try
        {
            Assert.Greater(config.creatureAttackGiantWindupSeconds, config.creatureAttackWindupSeconds);
            Assert.Greater(config.creatureAttackGiantLunge, config.creatureAttackLunge);
            Assert.LessOrEqual(config.creatureAttackWindupSeconds, 0.15f); // Görüntü hasarın çok gerisinde kalmasın
        }
        finally { Object.DestroyImmediate(config); }
    }

    [Test]
    public void TargetFinder_PicksNearestWithinReach()
    {
        var points = new[] { new Vector2(3f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f) };
        Assert.AreEqual(new Vector2(1f, 0f), AttackTargetFinder.Nearest(Vector2.zero, points, 2f));
        Assert.IsNull(AttackTargetFinder.Nearest(Vector2.zero, points, 0.5f));
        Assert.IsNull(AttackTargetFinder.Nearest(Vector2.zero, null, 2f));
    }
}
