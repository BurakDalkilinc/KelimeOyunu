using NUnit.Framework;
using UnityEngine;

/// <summary>CreatureModel testleri: kovalama, vuruş, kalıcılık, ömür, hedef elenmesi ve arena sınırı.</summary>
public class CreatureModelTests
{
    const float PlayerRadius = 0.5f;
    const float Cooldown = 1f;

    ArenaBounds arena;
    PlayerState target;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        target = new PlayerState(1, "Hedef", 3);
    }

    CreatureModel New(Vector2 position, float speed = 2f, int damage = 1, float life = 10f, bool persistent = false)
    {
        return new CreatureModel(null, target, position, speed, damage, 0.5f, life, arena) { Persistent = persistent };
    }

    // ---- Hareket ----

    [Test]
    public void Tick_MovesTowardTargetAtSpeedTimesDelta()
    {
        var creature = New(new Vector2(-8f, 0f), speed: 2f);
        creature.Tick(1f, new Vector2(8f, 0f), PlayerRadius, Cooldown);

        Assert.AreEqual(-6f, creature.Position.x, 0.001f);
        Assert.AreEqual(0f, creature.Position.y, 0.001f);
    }

    [Test]
    public void Tick_DoesNotOvershootTarget()
    {
        var creature = New(new Vector2(0f, 0f), speed: 1000f);
        creature.Tick(1f, new Vector2(5f, 0f), PlayerRadius, Cooldown);

        // Hedefin merkezini aşmaz: temas mesafesinin (0.8 × 1.0) hemen yakınında durur.
        Assert.AreEqual(5f - 0.8f, creature.Position.x, 0.001f);
    }

    [Test]
    public void Tick_StationaryCreature_DoesNotMove()
    {
        var creature = New(new Vector2(3f, 3f), speed: 0f);
        creature.Tick(5f, new Vector2(-5f, -3f), PlayerRadius, Cooldown);
        Assert.AreEqual(new Vector2(3f, 3f), creature.Position);
    }

    [Test]
    public void Constructor_PositionOutsideArena_IsClamped()
    {
        var creature = New(new Vector2(500f, -500f));
        Assert.IsTrue(arena.Contains(creature.Position));
    }

    // ---- Vuruş ----

    [Test]
    public void Tick_OnContact_HitsOnceAndFinishesWhenNotPersistent()
    {
        var creature = New(new Vector2(0f, 0f), speed: 100f, damage: 1);
        bool hit = creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown);

        Assert.IsTrue(hit);
        Assert.AreEqual(2, target.Health.Current);
        Assert.IsTrue(creature.IsFinished);
    }

    [Test]
    public void Tick_FarFromTarget_DoesNotHit()
    {
        var creature = New(new Vector2(-8f, 0f), speed: 1f);
        bool hit = creature.Tick(0.5f, new Vector2(8f, 0f), PlayerRadius, Cooldown);

        Assert.IsFalse(hit);
        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(creature.IsFinished);
    }

    [Test]
    public void Tick_Persistent_HitsAgainOnlyAfterCooldown()
    {
        var creature = New(new Vector2(0f, 0f), speed: 100f, damage: 1, life: 30f, persistent: true);
        var targetPosition = new Vector2(4f, 0f);

        Assert.IsTrue(creature.Tick(1f, targetPosition, PlayerRadius, Cooldown));   // 1. vuruş
        Assert.AreEqual(2, target.Health.Current);
        Assert.IsFalse(creature.IsFinished);

        Assert.IsFalse(creature.Tick(0.4f, targetPosition, PlayerRadius, Cooldown)); // bekleme sürüyor
        Assert.IsFalse(creature.Tick(0.4f, targetPosition, PlayerRadius, Cooldown));
        Assert.AreEqual(2, target.Health.Current);

        Assert.IsTrue(creature.Tick(0.4f, targetPosition, PlayerRadius, Cooldown));  // 1.2 sn geçti → 2. vuruş
        Assert.AreEqual(1, target.Health.Current);
    }

    [Test]
    public void Tick_ZeroDamage_NeverHitsOrFinishesByHitting()
    {
        var creature = New(new Vector2(0f, 0f), speed: 100f, damage: 0, life: 30f);

        for (int i = 0; i < 20; i++)
            Assert.IsFalse(creature.Tick(0.5f, new Vector2(4f, 0f), PlayerRadius, Cooldown));

        Assert.AreEqual(3, target.Health.Current);
        Assert.IsFalse(creature.IsDangerous);
    }

    [Test]
    public void Tick_StationaryCreature_HitsOnlyWhenTargetOverlaps()
    {
        var creature = New(new Vector2(0f, 0f), speed: 0f, damage: 1, life: 30f, persistent: true);

        Assert.IsFalse(creature.Tick(0.1f, new Vector2(5f, 0f), PlayerRadius, Cooldown)); // uzakta
        Assert.IsTrue(creature.Tick(0.1f, new Vector2(0.6f, 0f), PlayerRadius, Cooldown)); // üstüne bastı
    }

    [Test]
    public void Tick_CanEliminateTarget_AndNextTickFinishesPersistentCreature()
    {
        target = new PlayerState(1, "Hedef", 1);
        var creature = New(new Vector2(0f, 0f), speed: 100f, damage: 2, life: 30f, persistent: true);

        Assert.IsTrue(creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown));
        Assert.IsFalse(target.IsAlive);

        creature.Tick(0.1f, new Vector2(4f, 0f), PlayerRadius, Cooldown);
        Assert.IsTrue(creature.IsFinished);
    }

    [Test]
    public void Tick_TargetAlreadyEliminated_FinishesWithoutHitting()
    {
        target.Health.TakeDamage(99);
        var creature = New(new Vector2(0f, 0f));

        Assert.IsFalse(creature.Tick(0.1f, new Vector2(1f, 0f), PlayerRadius, Cooldown));
        Assert.IsTrue(creature.IsFinished);
    }

    // ---- Ömür ----

    [Test]
    public void Tick_WhenLifeRunsOut_Finishes()
    {
        var creature = New(new Vector2(-8f, 0f), speed: 0.1f, life: 2f);
        creature.Tick(1f, new Vector2(8f, 0f), PlayerRadius, Cooldown);
        Assert.IsFalse(creature.IsFinished);

        creature.Tick(1.01f, new Vector2(8f, 0f), PlayerRadius, Cooldown);
        Assert.IsTrue(creature.IsFinished);
    }

    [Test]
    public void Constructor_ZeroLife_IsFinishedImmediately()
    {
        Assert.IsTrue(New(Vector2.zero, life: 0f).IsFinished);
    }

    [Test]
    public void Tick_AfterFinished_DoesNothing()
    {
        var creature = New(new Vector2(0f, 0f), speed: 100f, damage: 1);
        creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown); // vurup bitti
        int health = target.Health.Current;

        Assert.IsFalse(creature.Tick(1f, new Vector2(4f, 0f), PlayerRadius, Cooldown));
        Assert.AreEqual(health, target.Health.Current);
    }

    [Test]
    public void Tick_NegativeDelta_IsTreatedAsZero()
    {
        var creature = New(new Vector2(-8f, 0f), speed: 5f, life: 10f);
        creature.Tick(-3f, new Vector2(8f, 0f), PlayerRadius, Cooldown);

        Assert.AreEqual(-8f, creature.Position.x, 0.001f);
        Assert.AreEqual(10f, creature.LifeLeft, 0.001f);
    }

    // ---- Hedefsiz ("???") yaratık ----

    [Test]
    public void Wander_MovesInGivenDirectionAndStaysInArena()
    {
        var creature = new CreatureModel(null, null, new Vector2(8f, 0f), 2f, 0, 0.5f, 100f, arena);

        creature.Wander(Vector2.right, 1f);
        Assert.AreEqual(10f, creature.Position.x, 0.001f); // 8 + 2 = 10 → arena kenarı

        creature.Wander(Vector2.right, 5f);
        Assert.IsTrue(arena.Contains(creature.Position));
    }

    [Test]
    public void Wander_FinishesWhenLifeEnds()
    {
        var creature = new CreatureModel(null, null, Vector2.zero, 1f, 0, 0.5f, 1f, arena);
        creature.Wander(Vector2.up, 1.5f);
        Assert.IsTrue(creature.IsFinished);
    }

    [Test]
    public void TargetlessCreature_IsHarmless()
    {
        var creature = new CreatureModel(null, null, Vector2.zero, 1f, 5, 0.5f, 10f, arena);

        Assert.IsFalse(creature.IsDangerous);
        Assert.IsFalse(creature.Tick(1f, Vector2.zero, PlayerRadius, Cooldown));
    }

    [Test]
    public void Constructor_NullArena_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new CreatureModel(null, target, Vector2.zero, 1f, 1, 0.5f, 1f, null));
    }
}
