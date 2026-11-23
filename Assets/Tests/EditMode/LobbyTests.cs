using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>4-20 kişilik lobi testleri: kadro, renkler, doğma halkası.</summary>
public class LobbyTests
{
    // ---- RosterBuilder ----

    [Test]
    public void Roster_FillsMissingSlotsWithBots_HumansFirst()
    {
        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0, 3 }, 6);

        Assert.AreEqual(6, roster.Count);
        Assert.AreEqual(0, roster[0].Id);
        Assert.AreEqual(3, roster[1].Id);
        Assert.IsFalse(roster[0].IsBot);
        Assert.IsFalse(roster[1].IsBot);
        for (int i = 2; i < 6; i++) Assert.IsTrue(roster[i].IsBot);
        Assert.AreEqual(RosterBuilder.FirstBotId, roster[2].Id);
        Assert.AreEqual("Bot 1", roster[2].Name);
    }

    [Test]
    public void Roster_NeverHasFewerPlayersThanHumans()
    {
        var humans = new List<ulong> { 0, 1, 2, 3, 4, 5 };

        List<RosterEntry> roster = RosterBuilder.Build(humans, 4);

        Assert.AreEqual(6, roster.Count);
        Assert.IsTrue(roster.TrueForAll(r => !r.IsBot));
    }

    [TestCase(-5, 2)]
    [TestCase(0, 2)]
    [TestCase(1, 2)]
    [TestCase(7, 7)]
    [TestCase(20, 20)]
    [TestCase(21, 20)]
    [TestCase(999, 20)]
    public void ClampTarget_KeepsBetweenTwoAndTwenty(int target, int expected)
    {
        Assert.AreEqual(expected, RosterBuilder.ClampTarget(target));
    }

    [Test]
    public void Roster_TwentyPlayers_HasUniqueIds()
    {
        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0, 1 }, 20);

        Assert.AreEqual(20, roster.Count);
        var ids = new HashSet<int>();
        foreach (RosterEntry entry in roster) Assert.IsTrue(ids.Add(entry.Id), "Aynı numara iki kez: " + entry.Id);
    }

    [Test]
    public void Roster_MoreThanTwentyHumans_IsCappedAtTwenty()
    {
        var humans = new List<ulong>();
        for (ulong i = 0; i < 25; i++) humans.Add(i);

        Assert.AreEqual(20, RosterBuilder.Build(humans, 4).Count);
    }

    [Test]
    public void Roster_RoundTripsThroughTheNetworkCodec_AtTwentyPlayers()
    {
        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0 }, 20);

        Assert.IsTrue(RosterCodec.TryDecode(RosterCodec.Encode(roster), out List<RosterEntry> back));
        Assert.AreEqual(20, back.Count);
    }

    // ---- PlayerColors ----

    [Test]
    public void Colors_AreDeterministic_AndDistinctForTwentyPlayers()
    {
        var seen = new List<Color>();
        for (int id = 0; id < 20; id++)
        {
            Color color = PlayerColors.For(id);
            Assert.AreEqual(color, PlayerColors.For(id));

            foreach (Color other in seen)
            {
                float distance = Mathf.Abs(color.r - other.r) + Mathf.Abs(color.g - other.g) + Mathf.Abs(color.b - other.b);
                Assert.Greater(distance, 0.12f, $"{id}. renk bir öncekine çok yakın");
            }
            seen.Add(color);
        }
    }

    [Test]
    public void Colors_BotIdsWork_AndNegativeIsSafe()
    {
        Assert.AreNotEqual(PlayerColors.For(100), PlayerColors.For(101));
        Assert.DoesNotThrow(() => PlayerColors.For(-1));
    }

    // ---- SpawnRing ----

    [Test]
    public void SpawnRing_PointsAreInsideTheArena_AndEvenlySpaced()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));
        var points = new List<Vector2>();
        for (int i = 0; i < 20; i++)
        {
            Vector2 p = SpawnRing.Point(arena, i, 20, 0.4f);
            Assert.IsTrue(arena.Contains(p));
            points.Add(p);
        }

        // Komşu noktalar oyuncu çapından (1 birim) daha uzak olmalı: yan yana binmesinler.
        for (int i = 0; i < 20; i++)
            Assert.Greater((points[i] - points[(i + 1) % 20]).magnitude, 0.7f);
    }

    [Test]
    public void SpawnRing_WrapsTheIndex_AndHandlesZeroCount()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));

        Assert.AreEqual(SpawnRing.Point(arena, 1, 8, 0.5f), SpawnRing.Point(arena, 9, 8, 0.5f));
        Assert.DoesNotThrow(() => SpawnRing.Point(arena, 0, 0, 0.5f));
    }

    [Test]
    public void SpawnRing_RadiusFraction_IsClamped()
    {
        var arena = new ArenaBounds(new Vector2(20f, 12f));

        Vector2 p = SpawnRing.Point(arena, 0, 4, 5f); // 5 -> 1'e çekilir

        Assert.AreEqual(6f, p.magnitude, 0.0001f); // Kısa yarı kenar
    }
}
