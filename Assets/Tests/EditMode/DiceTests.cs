using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Dice testleri: ağırlıklı seçim, elenenlerin seçilmemesi, seed ile tekrar oynatma.</summary>
public class DiceTests
{
    static PlayerState NewPlayer(int id, int missed = 0)
    {
        return new PlayerState(id, "P" + id, 3) { MissedTurns = missed };
    }

    [Test]
    public void GetWeight_IsOnePlusBonusTimesMissedTurns()
    {
        var dice = new Dice(new Random(1), 0.5f);
        Assert.AreEqual(1f, dice.GetWeight(NewPlayer(1, 0)), 0.0001f);
        Assert.AreEqual(3f, dice.GetWeight(NewPlayer(1, 4)), 0.0001f);
    }

    [Test]
    public void GetWeight_NegativeMissedTurns_CountsAsZero()
    {
        var dice = new Dice(new Random(1), 0.5f);
        Assert.AreEqual(1f, dice.GetWeight(NewPlayer(1, -5)), 0.0001f);
    }

    [Test]
    public void Roll_SingleAlivePlayer_ReturnsThatPlayer()
    {
        var dice = new Dice(new Random(1), 0.5f);
        var only = NewPlayer(1);
        for (int i = 0; i < 20; i++)
            Assert.AreSame(only, dice.Roll(new List<PlayerState> { only }));
    }

    [Test]
    public void Roll_NeverPicksEliminatedPlayers()
    {
        var dice = new Dice(new Random(7), 0.5f);
        var dead = NewPlayer(1, missed: 50); // Çok bekledi ama elendi.
        dead.Health.TakeDamage(99);
        var alive = NewPlayer(2);
        var candidates = new List<PlayerState> { dead, alive };

        for (int i = 0; i < 500; i++)
            Assert.AreSame(alive, dice.Roll(candidates));
    }

    [Test]
    public void Roll_NoAlivePlayers_Throws()
    {
        var dice = new Dice(new Random(1), 0.5f);
        var dead = NewPlayer(1);
        dead.Health.TakeDamage(99);

        Assert.Throws<InvalidOperationException>(() => dice.Roll(new List<PlayerState> { dead }));
        Assert.Throws<InvalidOperationException>(() => dice.Roll(new List<PlayerState>()));
    }

    [Test]
    public void Roll_LongWaitingPlayer_IsPickedMoreOften()
    {
        var dice = new Dice(new Random(42), 0.5f);
        var waiting = NewPlayer(1, missed: 10); // Ağırlık 6
        var fresh = NewPlayer(2, missed: 0);    // Ağırlık 1
        var candidates = new List<PlayerState> { waiting, fresh };

        int waitingPicked = 0;
        const int rolls = 10000;
        for (int i = 0; i < rolls; i++)
            if (ReferenceEquals(dice.Roll(candidates), waiting)) waitingPicked++;

        // Beklenen oran 6/7 ≈ 0,857
        double ratio = waitingPicked / (double)rolls;
        Assert.That(ratio, Is.InRange(0.82, 0.90));
    }

    [Test]
    public void Roll_EqualWeights_IsRoughlyUniform()
    {
        var dice = new Dice(new Random(99), 0.5f);
        var a = NewPlayer(1);
        var b = NewPlayer(2);
        var c = NewPlayer(3);
        var candidates = new List<PlayerState> { a, b, c };

        var counts = new Dictionary<PlayerState, int> { { a, 0 }, { b, 0 }, { c, 0 } };
        const int rolls = 9000;
        for (int i = 0; i < rolls; i++)
            counts[dice.Roll(candidates)]++;

        foreach (int count in counts.Values)
            Assert.That(count / (double)rolls, Is.InRange(0.30, 0.37));
    }

    [Test]
    public void Roll_ZeroBonus_IgnoresWaitingTime()
    {
        var dice = new Dice(new Random(5), 0f);
        var waiting = NewPlayer(1, missed: 100);
        var fresh = NewPlayer(2, missed: 0);
        var candidates = new List<PlayerState> { waiting, fresh };

        int waitingPicked = 0;
        const int rolls = 6000;
        for (int i = 0; i < rolls; i++)
            if (ReferenceEquals(dice.Roll(candidates), waiting)) waitingPicked++;

        Assert.That(waitingPicked / (double)rolls, Is.InRange(0.45, 0.55));
    }

    [Test]
    public void Roll_SameSeed_GivesSameSequence()
    {
        var players = new List<PlayerState> { NewPlayer(1), NewPlayer(2), NewPlayer(3), NewPlayer(4) };
        var first = new Dice(new Random(2024), 0.5f);
        var second = new Dice(new Random(2024), 0.5f);

        for (int i = 0; i < 50; i++)
            Assert.AreEqual(first.Roll(players).Id, second.Roll(players).Id);
    }

    [Test]
    public void Roll_DoesNotChangeMissedTurns()
    {
        var dice = new Dice(new Random(1), 0.5f);
        var player = NewPlayer(1, missed: 3);
        dice.Roll(new List<PlayerState> { player, NewPlayer(2) });
        Assert.AreEqual(3, player.MissedTurns);
    }

    [Test]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Dice(null, 0.5f));
    }
}
