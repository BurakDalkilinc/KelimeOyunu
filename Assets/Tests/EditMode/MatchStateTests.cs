using System;
using NUnit.Framework;

/// <summary>MatchState testleri: hayatta kalanlar, maç sonu ve kazanan.</summary>
public class MatchStateTests
{
    static PlayerState NewPlayer(int id) => new PlayerState(id, "P" + id, 3);

    [Test]
    public void AliveCount_CountsOnlyLivingPlayers()
    {
        var match = new MatchState(new[] { NewPlayer(1), NewPlayer(2), NewPlayer(3) });
        match.GetPlayer(2).Health.TakeDamage(99);

        Assert.AreEqual(2, match.AliveCount);
        Assert.AreEqual(2, match.AlivePlayers.Count);
        Assert.AreEqual(3, match.Players.Count);
    }

    [Test]
    public void IsOver_FalseWithTwoAlive_TrueWithOneOrZero()
    {
        var match = new MatchState(new[] { NewPlayer(1), NewPlayer(2), NewPlayer(3) });
        Assert.IsFalse(match.IsOver);

        match.GetPlayer(1).Health.TakeDamage(99);
        Assert.IsFalse(match.IsOver);

        match.GetPlayer(2).Health.TakeDamage(99);
        Assert.IsTrue(match.IsOver);

        match.GetPlayer(3).Health.TakeDamage(99);
        Assert.IsTrue(match.IsOver);
    }

    [Test]
    public void Winner_IsLastAlivePlayer()
    {
        var match = new MatchState(new[] { NewPlayer(1), NewPlayer(2), NewPlayer(3) });
        Assert.IsNull(match.Winner);

        match.GetPlayer(1).Health.TakeDamage(99);
        match.GetPlayer(3).Health.TakeDamage(99);

        Assert.AreEqual(2, match.Winner.Id);
    }

    [Test]
    public void Winner_IsNullWhenEveryoneIsEliminated()
    {
        var match = new MatchState(new[] { NewPlayer(1), NewPlayer(2) });
        match.GetPlayer(1).Health.TakeDamage(99);
        match.GetPlayer(2).Health.TakeDamage(99);

        Assert.IsNull(match.Winner);
    }

    [Test]
    public void GetPlayer_UnknownId_ReturnsNull()
    {
        var match = new MatchState(new[] { NewPlayer(1) });
        Assert.IsNull(match.GetPlayer(99));
    }

    [Test]
    public void Constructor_DuplicateId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MatchState(new[] { NewPlayer(1), NewPlayer(1) }));
    }

    [Test]
    public void Constructor_NullPlayerOrList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MatchState(null));
        Assert.Throws<ArgumentException>(() => new MatchState(new PlayerState[] { null }));
    }
}
