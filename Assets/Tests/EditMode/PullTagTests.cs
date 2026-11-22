using NUnit.Framework;
using UnityEngine;

/// <summary>"pull" etiketinin ve StatusEffects çekme etkisinin testleri: Mıknatıs.</summary>
public class PullTagTests
{
    ArenaBounds arena;
    GameConfig config;
    PlayerState target;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>();
        target = new PlayerState(1, "Hedef", 3);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    CreatureModel Magnet()
    {
        var magnet = CreatureFactory.CreateAttack(Real("miknatis"), target, Vector2.zero, config, arena);
        magnet.PlaceAt(Vector2.zero);
        return magnet;
    }

    // ---- StatusEffects: çekme ----

    [Test]
    public void NewStatus_IsNotPulled()
    {
        var status = new StatusEffects();
        Assert.IsFalse(status.IsPulled);
        Assert.AreEqual(Vector2.zero, status.GetPullStep(new Vector2(5f, 5f), 1f));
    }

    [Test]
    public void GetPullStep_MovesTowardThePointAtTheForce()
    {
        var status = new StatusEffects();
        status.ApplyPull(Vector2.zero, 4f, 2f);

        Vector2 step = status.GetPullStep(new Vector2(8f, 0f), 0.5f);

        Assert.AreEqual(-2f, step.x, 0.001f); // 4 birim/sn × 0.5 sn
        Assert.AreEqual(0f, step.y, 0.001f);
    }

    [Test]
    public void GetPullStep_DoesNotOvershootThePoint()
    {
        var status = new StatusEffects();
        status.ApplyPull(Vector2.zero, 100f, 2f);

        Vector2 step = status.GetPullStep(new Vector2(1f, 0f), 1f);

        Assert.AreEqual(-1f, step.x, 0.001f, "Noktaya varınca tam orada durur");
    }

    [Test]
    public void GetPullStep_AlreadyAtThePoint_IsZero()
    {
        var status = new StatusEffects();
        status.ApplyPull(new Vector2(3f, 3f), 4f, 2f);
        Assert.AreEqual(Vector2.zero, status.GetPullStep(new Vector2(3f, 3f), 1f));
    }

    [Test]
    public void Pull_EndsWhenTimeRunsOut()
    {
        var status = new StatusEffects();
        status.ApplyPull(Vector2.zero, 4f, 1f);

        status.Tick(0.9f);
        Assert.IsTrue(status.IsPulled);

        status.Tick(0.2f);
        Assert.IsFalse(status.IsPulled);
        Assert.AreEqual(Vector2.zero, status.GetPullStep(new Vector2(5f, 0f), 1f));
    }

    [TestCase(0f, 1f)]
    [TestCase(4f, 0f)]
    [TestCase(-1f, 1f)]
    [TestCase(4f, -1f)]
    public void ApplyPull_NoOpCases_DoNothing(float force, float seconds)
    {
        var status = new StatusEffects();
        status.ApplyPull(Vector2.zero, force, seconds);
        Assert.IsFalse(status.IsPulled);
    }

    [Test]
    public void Pull_AndSlow_AreIndependent()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 1f);
        status.ApplyPull(Vector2.zero, 4f, 3f);

        status.Tick(1.5f);

        Assert.IsFalse(status.IsSlowed);
        Assert.IsTrue(status.IsPulled);
        Assert.AreEqual(1f, status.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void Clear_RemovesPullToo()
    {
        var status = new StatusEffects();
        status.ApplyPull(Vector2.zero, 4f, 3f);
        status.Clear();
        Assert.IsFalse(status.IsPulled);
    }

    [Test]
    public void ApplyPull_Again_RefreshesTheTimeAndUsesTheNewPoint()
    {
        var status = new StatusEffects();
        status.ApplyPull(new Vector2(-5f, 0f), 4f, 1f);
        status.Tick(0.9f);
        status.ApplyPull(new Vector2(5f, 0f), 4f, 1f);

        Assert.AreEqual(1f, status.PullTimeLeft, 0.001f);
        Assert.Greater(status.GetPullStep(Vector2.zero, 0.1f).x, 0f, "Yeni noktaya doğru çeker");
    }

    // ---- Mıknatıs (gerçek kelimeden) ----

    [Test]
    public void Miknatis_FromRealWords_IsAStationaryPuller()
    {
        var magnet = Magnet();

        Assert.IsTrue(magnet.IsPull);
        Assert.AreEqual(4f, magnet.PullForce, 0.001f);        // words.json: pullForce 4.0
        Assert.AreEqual(4f, magnet.LifeLeft, 0.001f);         // words.json: duration 4
        Assert.AreEqual(0f, magnet.Speed, 0.001f);
        Assert.AreEqual(0, magnet.Damage);
        Assert.AreEqual(config.pullLingerSeconds, magnet.PullLingerSeconds, 0.0001f);
        Assert.IsFalse(magnet.IsHarmful, "Etkisi yalnızca hedefe; botlar kaçmaz");
    }

    [Test]
    public void Magnet_PullsItsTargetTowardItsOwnPosition_EveryTick()
    {
        var magnet = Magnet();

        Assert.IsTrue(magnet.TickPull(0.1f));

        Assert.IsTrue(target.Status.IsPulled);
        Vector2 step = target.Status.GetPullStep(new Vector2(6f, 0f), 0.5f);
        Assert.AreEqual(-2f, step.x, 0.001f); // Merkeze (0,0) doğru, 4 × 0.5
    }

    [Test]
    public void Magnet_KeepsPulling_ForItsWholeLife_ThenLeavesAShortLinger()
    {
        var magnet = Magnet();

        for (int i = 0; i < 39; i++) // 3.9 sn
        {
            Assert.IsTrue(magnet.TickPull(0.1f));
            target.Status.Tick(0.1f);
        }
        Assert.IsTrue(target.Status.IsPulled);

        Assert.IsFalse(magnet.TickPull(0.2f)); // Ömür bitti
        Assert.IsTrue(magnet.IsFinished);

        for (int i = 0; i < 4; i++) target.Status.Tick(0.1f); // 0.4 sn > 0.3 sn kalıntı
        Assert.IsFalse(target.Status.IsPulled);
    }

    [Test]
    public void Magnet_FinishesWhenTargetIsEliminated()
    {
        var magnet = Magnet();
        target.Health.TakeDamage(99);

        Assert.IsFalse(magnet.TickPull(0.1f));
        Assert.IsTrue(magnet.IsFinished);
        Assert.IsFalse(target.Status.IsPulled);
    }

    [Test]
    public void Magnet_IgnoresOtherTicks_AndOtherCreaturesIgnoreTickPull()
    {
        var magnet = Magnet();
        Assert.IsFalse(magnet.Tick(1f, new Vector2(3f, 0f), 0.5f, 1f));
        Assert.IsFalse(target.Status.IsPulled);

        var kurt = CreatureFactory.CreateAttack(Real("kurt"), target, Vector2.zero, config, arena);
        Assert.IsFalse(kurt.TickPull(1f));
        Assert.IsFalse(target.Status.IsPulled);
    }

    [Test]
    public void PullTag_WordWithoutForce_UsesTheDefault()
    {
        var word = new WordEntry { id = "x", tags = new[] { "pull" }, stats = new WordStats { duration = 3f } };
        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(config.pullDefaultForce, creature.PullForce, 0.001f);
    }

    [Test]
    public void PullTag_WordWithoutStats_DoesNotThrow()
    {
        var word = new WordEntry { id = "x", tags = new[] { "pull" } };
        Assert.DoesNotThrow(() => CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena));
    }

    [Test]
    public void PullIsWeakerThanPlayerSpeed_SoWalkingAwayWins()
    {
        // Oyuncu mıknatısa ters yönde yürürse net olarak uzaklaşır: hız (5) > çekme (4).
        Assert.Less(Real("miknatis").stats.pullForce, config.playerMoveSpeed);
    }
}
