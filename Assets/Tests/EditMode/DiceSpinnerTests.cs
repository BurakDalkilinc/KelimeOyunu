using NUnit.Framework;

/// <summary>DiceSpinner testleri: animasyon her zaman seçilen oyuncuda durur ve isimleri atlamadan döner.</summary>
public class DiceSpinnerTests
{
    [TestCase(2, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(8, 0)]
    [TestCase(8, 5)]
    [TestCase(20, 19)]
    public void AtOrAfterDuration_ShowsTheFinalPlayer(int count, int final)
    {
        Assert.AreEqual(final, DiceSpinner.GetIndex(2f, 2f, count, final));
        Assert.AreEqual(final, DiceSpinner.GetIndex(5f, 2f, count, final));
    }

    [TestCase(2, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 1)]
    [TestCase(8, 3)]
    [TestCase(20, 7)]
    public void JustBeforeEnd_StillLandsOnFinalOrTheOneBeforeIt(int count, int final)
    {
        int index = DiceSpinner.GetIndex(1.9999f, 2f, count, final);
        int before = (final - 1 + count) % count;
        Assert.That(index == final || index == before, $"index={index}, final={final}");
    }

    [TestCase(2, 0)]
    [TestCase(3, 2)]
    [TestCase(8, 5)]
    [TestCase(20, 11)]
    public void AtStart_DoesNotShowTheFinalPlayer(int count, int final)
    {
        Assert.AreNotEqual(final, DiceSpinner.GetIndex(0f, 2f, count, final));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(8)]
    [TestCase(20)]
    public void Spinning_NeverSkipsANameAndStaysInRange(int count)
    {
        const float duration = 2f;
        int final = count - 1;
        int previous = DiceSpinner.GetIndex(0f, duration, count, final);

        for (float t = 0.001f; t <= duration; t += 0.001f)
        {
            int current = DiceSpinner.GetIndex(t, duration, count, final);
            Assert.That(current, Is.InRange(0, count - 1));

            int stepped = (current - previous + count) % count;
            Assert.That(stepped, Is.InRange(0, 1), $"t={t}: {previous} → {current} bir isim atladı");
            previous = current;
        }
    }

    [Test]
    public void Spinning_SlowsDownTowardTheEnd()
    {
        const float duration = 2f;
        const int count = 8;
        int changesInFirstHalf = CountChanges(0f, duration * 0.5f, duration, count);
        int changesInSecondHalf = CountChanges(duration * 0.5f, duration, duration, count);

        Assert.Greater(changesInFirstHalf, changesInSecondHalf);
    }

    static int CountChanges(float from, float to, float duration, int count)
    {
        int changes = 0;
        int previous = DiceSpinner.GetIndex(from, duration, count, 0);
        for (float t = from + 0.001f; t <= to; t += 0.001f)
        {
            int current = DiceSpinner.GetIndex(t, duration, count, 0);
            if (current != previous) changes++;
            previous = current;
        }
        return changes;
    }

    [Test]
    public void SingleCandidate_AlwaysReturnsIt()
    {
        Assert.AreEqual(0, DiceSpinner.GetIndex(0f, 2f, 1, 0));
        Assert.AreEqual(0, DiceSpinner.GetIndex(1f, 2f, 1, 0));
    }

    [Test]
    public void NoCandidates_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, DiceSpinner.GetIndex(1f, 2f, 0, 0));
        Assert.AreEqual(-1, DiceSpinner.GetIndex(1f, 2f, -3, 0));
    }

    [Test]
    public void ZeroOrNegativeDuration_ShowsFinalImmediately()
    {
        Assert.AreEqual(3, DiceSpinner.GetIndex(0f, 0f, 8, 3));
        Assert.AreEqual(3, DiceSpinner.GetIndex(0f, -1f, 8, 3));
    }

    [Test]
    public void OutOfRangeFinalIndex_IsClamped()
    {
        Assert.AreEqual(7, DiceSpinner.GetIndex(5f, 2f, 8, 99));
        Assert.AreEqual(0, DiceSpinner.GetIndex(5f, 2f, 8, -4));
    }

    [Test]
    public void NegativeElapsed_ActsLikeStart()
    {
        Assert.AreEqual(DiceSpinner.GetIndex(0f, 2f, 8, 3), DiceSpinner.GetIndex(-1f, 2f, 8, 3));
    }
}
