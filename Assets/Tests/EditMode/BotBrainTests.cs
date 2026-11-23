using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>BotBrain testleri: gezinme, bekleme, tehlikeden kaçma, duvar kenarı ve tekrar oynatılabilirlik.</summary>
public class BotBrainTests
{
    GameConfig config;
    ArenaBounds arena;

    static readonly IReadOnlyList<DangerZone> NoDanger = new List<DangerZone>();

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>(); // Oyuncu yarıçapı 0.5
        config.arenaSize = new Vector2(20f, 12f); // Testlerdeki koordinatlar bu küçük haritaya göre; oyunun gerçek haritası daha büyük
        arena = new ArenaBounds(config.arenaSize);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) UnityEngine.Object.DestroyImmediate(config);
    }

    BotBrain NewBrain(int seed = 1) => new BotBrain(config, arena, new System.Random(seed));

    static List<DangerZone> Dangers(params DangerZone[] zones) => new List<DangerZone>(zones);

    // ---- Gezinme ----

    [Test]
    public void Wander_FirstThink_ReturnsUnitDirection()
    {
        var direction = NewBrain().Think(0.016f, new Vector2(0f, 0f), NoDanger);
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    [Test]
    public void Wander_TargetsAlwaysStayInsideArenaMinusMargin()
    {
        var brain = NewBrain(3);
        Vector2 limit = arena.HalfSize - Vector2.one * config.botWanderMargin;

        for (int i = 0; i < 500; i++)
        {
            brain.Think(0.016f, Vector2.zero, NoDanger);  // Hedef seçilir.
            Vector2 target = brain.Target;
            Assert.LessOrEqual(Mathf.Abs(target.x), limit.x + 0.0001f);
            Assert.LessOrEqual(Mathf.Abs(target.y), limit.y + 0.0001f);

            brain.Think(0.016f, target, NoDanger);        // Vardı → bekleme başlar.
            brain.Think(100f, target, NoDanger);          // Beklemeyi bitir.
        }
    }

    [Test]
    public void Wander_OnArrival_StopsAndPauses()
    {
        var brain = NewBrain();
        brain.Think(0.016f, Vector2.zero, NoDanger);
        Vector2 target = brain.Target;

        Assert.AreEqual(Vector2.zero, brain.Think(0.016f, target, NoDanger));
        Assert.AreEqual(Vector2.zero, brain.Think(0.05f, target, NoDanger)); // Bekleme sürüyor (en az 0.3 sn).
    }

    [Test]
    public void Wander_AfterPause_PicksNewTargetAndMovesAgain()
    {
        var brain = NewBrain();
        brain.Think(0.016f, Vector2.zero, NoDanger);
        Vector2 firstTarget = brain.Target;
        brain.Think(0.016f, firstTarget, NoDanger);     // Vardı, bekle.
        brain.Think(100f, firstTarget, NoDanger);       // Bekleme biter.

        Vector2 direction = brain.Think(0.016f, firstTarget, NoDanger);
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
        Assert.AreNotEqual(firstTarget, brain.Target);
    }

    [Test]
    public void Wander_NullDangerList_IsFine()
    {
        var direction = NewBrain().Think(0.016f, Vector2.zero, null);
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    // ---- Tehlikeden kaçma ----

    [Test]
    public void Flee_DangerOnRight_RunsLeft()
    {
        var direction = NewBrain().Think(0.016f, Vector2.zero, Dangers(new DangerZone(new Vector2(1f, 0f), 1f)));
        Assert.AreEqual(-1f, direction.x, 0.001f);
        Assert.AreEqual(0f, direction.y, 0.001f);
    }

    [Test]
    public void Flee_DangerAbove_RunsDown()
    {
        var direction = NewBrain().Think(0.016f, Vector2.zero, Dangers(new DangerZone(new Vector2(0f, 1f), 1f)));
        Assert.AreEqual(-1f, direction.y, 0.001f);
    }

    [Test]
    public void Flee_StartsWhenInsideRadiusPlusMargin()
    {
        // Yarıçap 1 + marj 2 = 3. Mesafe 2.9 → kaçar, 3.1 → kaçmaz (gezinir).
        var near = NewBrain().Think(0.016f, new Vector2(-2.9f, 0f), Dangers(new DangerZone(Vector2.zero, 1f)));
        Assert.Less(near.x, -0.99f);

        var far = NewBrain().Think(0.016f, new Vector2(-3.1f, 0f), Dangers(new DangerZone(Vector2.zero, 1f)));
        Assert.AreEqual(1f, far.magnitude, 0.001f);
        Assert.AreNotEqual(-1f, far.x); // Rastgele hedefe gidiyor, kaçmıyor.
    }

    [Test]
    public void Flee_FarAwayDanger_IsIgnored()
    {
        var brain = NewBrain();
        brain.Think(0.016f, new Vector2(-5f, 0f), Dangers(new DangerZone(new Vector2(9f, 0f), 0.5f)));
        // Gezinme hedefi seçildi demek ki kaçma devreye girmedi.
        Assert.AreNotEqual(new Vector2(-5f, 0f), brain.Target);
    }

    [Test]
    public void Flee_BotInsideDangerRadius_StillRunsAway()
    {
        var direction = NewBrain().Think(0.016f, new Vector2(2f, 0f), Dangers(new DangerZone(new Vector2(2.2f, 0f), 3f)));
        Assert.Less(direction.x, -0.99f);
    }

    [Test]
    public void Flee_TwoDangers_RunsAwayFromBoth()
    {
        var dangers = Dangers(new DangerZone(new Vector2(1f, 0f), 1f), new DangerZone(new Vector2(0f, 1f), 1f));
        var direction = NewBrain().Think(0.016f, Vector2.zero, dangers);

        Assert.Less(direction.x, 0f);
        Assert.Less(direction.y, 0f);
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    [Test]
    public void Flee_OpposingDangers_PicksPerpendicularOrFiniteDirection()
    {
        // Sağda ve solda eşit tehlike: yönler birbirini götürür, sonuç yine de geçerli bir yön olmalı.
        var dangers = Dangers(new DangerZone(new Vector2(1f, 0f), 1f), new DangerZone(new Vector2(-1f, 0f), 1f));
        var direction = NewBrain().Think(0.016f, Vector2.zero, dangers);

        Assert.IsFalse(float.IsNaN(direction.x) || float.IsNaN(direction.y));
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    [Test]
    public void Flee_DangerExactlyOnTopOfBot_ReturnsFiniteUnitDirection()
    {
        var direction = NewBrain().Think(0.016f, new Vector2(3f, 2f), Dangers(new DangerZone(new Vector2(3f, 2f), 1f)));

        Assert.IsFalse(float.IsNaN(direction.x) || float.IsNaN(direction.y));
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    [Test]
    public void Flee_AtWall_SlidesAlongWallInsteadOfPushingIntoIt()
    {
        // Sağ duvara dayalı (9.5 = 10 - yarıçap), tehlike solunda ve biraz yukarıda.
        var direction = NewBrain().Think(0.016f, new Vector2(9.5f, 0f), Dangers(new DangerZone(new Vector2(8.5f, 0.5f), 1f)));

        Assert.LessOrEqual(direction.x, 0f);   // Duvarın içine doğru gitmiyor.
        Assert.Less(direction.y, 0f);          // Tehlikeden uzağa (aşağı) kayıyor.
        Assert.AreEqual(1f, direction.magnitude, 0.001f);
    }

    [Test]
    public void Flee_InCorner_TurnsTowardCenter()
    {
        var direction = NewBrain().Think(0.016f, new Vector2(9.5f, 5.5f), Dangers(new DangerZone(new Vector2(8.5f, 4.5f), 1f)));

        Assert.Less(direction.x, 0f);
        Assert.Less(direction.y, 0f);
    }

    [Test]
    public void AfterFleeing_BotPicksFreshWanderTarget()
    {
        var brain = NewBrain();
        brain.Think(0.016f, Vector2.zero, NoDanger);
        Vector2 oldTarget = brain.Target;

        brain.Think(0.016f, Vector2.zero, Dangers(new DangerZone(new Vector2(1f, 0f), 1f)));  // Kaçtı.
        brain.Think(0.016f, Vector2.zero, NoDanger);                                          // Tehlike geçti.

        Assert.AreNotEqual(oldTarget, brain.Target);
    }

    // ---- Simülasyon ----

    static Vector2 Step(BotBrain brain, ArenaBounds arena, GameConfig config, Vector2 position, float dt, IReadOnlyList<DangerZone> dangers)
    {
        Vector2 direction = brain.Think(dt, position, dangers);
        return arena.Clamp(position + direction * (config.botMoveSpeed * dt), config.playerRadius);
    }

    [Test]
    public void Simulation_BotStaysInsideArena_AndKeepsMoving()
    {
        var brain = NewBrain(11);
        Vector2 position = Vector2.zero;
        float traveled = 0f;
        int pauses = 0;

        for (int i = 0; i < 3000; i++)
        {
            Vector2 next = Step(brain, arena, config, position, 0.05f, NoDanger);
            if (next == position) pauses++;
            traveled += (next - position).magnitude;
            position = next;
            Assert.IsTrue(arena.Contains(position), "Bot arenanın dışına çıktı: " + position);
        }

        Assert.Greater(traveled, 100f, "Bot yerinde saydı.");
        Assert.Greater(pauses, 0, "Bot hiç beklemedi.");
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void Simulation_BotEscapesFromChasingDanger_WithoutLeavingArena(int seed)
    {
        var brain = NewBrain(seed);
        Vector2 position = new Vector2(5f, 0f);
        Vector2 dangerPosition = Vector2.zero;
        float closest = float.MaxValue;

        for (int i = 0; i < 400; i++)
        {
            // Tehlike botu saniyede 3 birim hızla kovalar (bot 4 hızla kaçabilir).
            Vector2 toBot = position - dangerPosition;
            dangerPosition += toBot.normalized * (3f * 0.05f);

            position = Step(brain, arena, config, position, 0.05f, Dangers(new DangerZone(dangerPosition, 0.5f)));

            Assert.IsTrue(arena.Contains(position));
            closest = Mathf.Min(closest, (position - dangerPosition).magnitude);
        }

        Assert.Greater(closest, 0.5f, "Bot tehlikenin içine yakalandı.");
    }

    [Test]
    public void SameSeed_GivesSameMovement()
    {
        var first = NewBrain(77);
        var second = NewBrain(77);
        Vector2 a = Vector2.zero, b = Vector2.zero;

        for (int i = 0; i < 300; i++)
        {
            a = Step(first, arena, config, a, 0.05f, NoDanger);
            b = Step(second, arena, config, b, 0.05f, NoDanger);
            Assert.AreEqual(a, b);
        }
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new BotBrain(null, arena, new System.Random(1)));
        Assert.Throws<ArgumentNullException>(() => new BotBrain(config, null, new System.Random(1)));
        Assert.Throws<ArgumentNullException>(() => new BotBrain(config, arena, null));
    }
}
