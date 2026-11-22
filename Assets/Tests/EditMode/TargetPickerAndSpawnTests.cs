using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

/// <summary>TargetPicker ve ArenaSpawn testleri: hedef seçimi ve yaratığın doğma noktası.</summary>
public class TargetPickerAndSpawnTests
{
    static PlayerState NewPlayer(int id) => new PlayerState(id, "P" + id, 3);

    // ---- TargetPicker ----

    [Test]
    public void PickRandomOpponent_NeverPicksTheWriter()
    {
        var writer = NewPlayer(1);
        var players = new List<PlayerState> { writer, NewPlayer(2), NewPlayer(3) };
        var random = new Random(1);

        for (int i = 0; i < 300; i++)
            Assert.AreNotSame(writer, TargetPicker.PickRandomOpponent(players, writer, random));
    }

    [Test]
    public void PickRandomOpponent_NeverPicksEliminatedPlayers()
    {
        var writer = NewPlayer(1);
        var dead = NewPlayer(2);
        dead.Health.TakeDamage(99);
        var alive = NewPlayer(3);
        var players = new List<PlayerState> { writer, dead, alive };

        for (int i = 0; i < 100; i++)
            Assert.AreSame(alive, TargetPicker.PickRandomOpponent(players, writer, new Random(i)));
    }

    [Test]
    public void PickRandomOpponent_NoOpponent_ReturnsNull()
    {
        var writer = NewPlayer(1);
        var dead = NewPlayer(2);
        dead.Health.TakeDamage(99);

        Assert.IsNull(TargetPicker.PickRandomOpponent(new List<PlayerState> { writer }, writer, new Random(1)));
        Assert.IsNull(TargetPicker.PickRandomOpponent(new List<PlayerState> { writer, dead }, writer, new Random(1)));
        Assert.IsNull(TargetPicker.PickRandomOpponent(new List<PlayerState>(), writer, new Random(1)));
    }

    [Test]
    public void PickRandomOpponent_EventuallyPicksEveryOpponent()
    {
        var writer = NewPlayer(1);
        var players = new List<PlayerState> { writer, NewPlayer(2), NewPlayer(3), NewPlayer(4) };
        var seen = new HashSet<int>();
        var random = new Random(7);

        for (int i = 0; i < 200; i++)
            seen.Add(TargetPicker.PickRandomOpponent(players, writer, random).Id);

        CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seen);
    }

    [Test]
    public void PickRandomOpponent_SameSeed_SameSequence()
    {
        var writer = NewPlayer(1);
        var players = new List<PlayerState> { writer, NewPlayer(2), NewPlayer(3), NewPlayer(4) };
        var a = new Random(5);
        var b = new Random(5);

        for (int i = 0; i < 40; i++)
            Assert.AreEqual(TargetPicker.PickRandomOpponent(players, writer, a).Id, TargetPicker.PickRandomOpponent(players, writer, b).Id);
    }

    [Test]
    public void PickRandomOpponent_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => TargetPicker.PickRandomOpponent(null, null, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => TargetPicker.PickRandomOpponent(new List<PlayerState>(), null, null));
    }

    // ---- ArenaSpawn ----

    [Test]
    public void PickPoint_IsInsideArenaAndFarEnoughFromTarget()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        var random = new Random(3);
        var awayFrom = new Vector2(1f, 1f);

        for (int i = 0; i < 300; i++)
        {
            Vector2 point = ArenaSpawn.PickPoint(arena, awayFrom, 6f, random);
            Assert.IsTrue(arena.Contains(point), point.ToString());
            Assert.GreaterOrEqual((point - awayFrom).magnitude, 6f - 0.0001f);
        }
    }

    [Test]
    public void PickPoint_RespectsEdgeMargin()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        var random = new Random(9);

        for (int i = 0; i < 300; i++)
        {
            Vector2 point = ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, random, edgeMargin: 1f);
            Assert.LessOrEqual(Mathf.Abs(point.x), 9f + 0.0001f);
            Assert.LessOrEqual(Mathf.Abs(point.y), 5f + 0.0001f);
        }
    }

    [Test]
    public void PickPoint_ImpossibleDistance_FallsBackToFarthestCorner()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        Vector2 point = ArenaSpawn.PickPoint(arena, new Vector2(9f, 5f), 1000f, new Random(1), edgeMargin: 0.5f);

        Assert.AreEqual(new Vector2(-9.5f, -5.5f), point); // Hedefe en uzak köşe
        Assert.IsTrue(arena.Contains(point));
    }

    [Test]
    public void PickPoint_SameSeed_SamePoint()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        Assert.AreEqual(
            ArenaSpawn.PickPoint(arena, Vector2.zero, 4f, new Random(42)),
            ArenaSpawn.PickPoint(arena, Vector2.zero, 4f, new Random(42)));
    }

    [Test]
    public void PickPoint_MarginBiggerThanArena_DoesNotThrow()
    {
        var arena = new ArenaBounds(new Vector2(2f, 2f));
        Assert.DoesNotThrow(() => ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, new Random(1), edgeMargin: 50f));
    }

    [Test]
    public void PickPoint_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ArenaSpawn.PickPoint(null, Vector2.zero, 1f, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => ArenaSpawn.PickPoint(new ArenaBounds(Vector2.one * 10f), Vector2.zero, 1f, null));
    }
}
