using NUnit.Framework;

/// <summary>Health sınıfının testleri: hasar, iyileşme, ölüm ve olaylar.</summary>
public class HealthTests
{
    [Test]
    public void NewHealth_StartsFull()
    {
        var health = new Health(3);
        Assert.AreEqual(3, health.Current);
        Assert.AreEqual(3, health.Max);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void Constructor_ZeroMax_Throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new Health(0));
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        var health = new Health(3);
        int taken = health.TakeDamage(1);
        Assert.AreEqual(1, taken);
        Assert.AreEqual(2, health.Current);
    }

    [Test]
    public void TakeDamage_MoreThanCurrent_StopsAtZeroAndDies()
    {
        var health = new Health(3);
        int taken = health.TakeDamage(10);
        Assert.AreEqual(3, taken);
        Assert.AreEqual(0, health.Current);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void TakeDamage_NegativeOrZero_DoesNothing()
    {
        var health = new Health(3);
        Assert.AreEqual(0, health.TakeDamage(0));
        Assert.AreEqual(0, health.TakeDamage(-5));
        Assert.AreEqual(3, health.Current);
    }

    [Test]
    public void TakeDamage_WhenDead_DoesNothingAndDiesOnlyOnce()
    {
        var health = new Health(1);
        int diedCount = 0;
        health.Died += () => diedCount++;

        health.TakeDamage(1);
        int extra = health.TakeDamage(1);

        Assert.AreEqual(0, extra);
        Assert.AreEqual(1, diedCount);
    }

    [Test]
    public void Heal_CannotExceedMax()
    {
        var health = new Health(3);
        health.TakeDamage(1);
        int healed = health.Heal(5);
        Assert.AreEqual(1, healed);
        Assert.AreEqual(3, health.Current);
    }

    [Test]
    public void Heal_WhenDead_DoesNothing()
    {
        var health = new Health(2);
        health.TakeDamage(2);
        Assert.AreEqual(0, health.Heal(1));
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void Changed_FiresWithNewValues()
    {
        var health = new Health(3);
        int lastCurrent = -1, lastMax = -1;
        health.Changed += (current, max) => { lastCurrent = current; lastMax = max; };

        health.TakeDamage(2);

        Assert.AreEqual(1, lastCurrent);
        Assert.AreEqual(3, lastMax);
    }
}
