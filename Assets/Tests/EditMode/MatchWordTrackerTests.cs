using NUnit.Framework;

/// <summary>MatchWordTracker testleri: her kelime (ve eş anlamlıları) maçta bir kez.</summary>
public class MatchWordTrackerTests
{
    const string Json = @"{
  ""blocked"": [""yasakli""],
  ""words"": [
    { ""id"": ""zombi"", ""display"": ""Zombi"", ""aliases"": [""zombie"", ""yürüyen ölü""] },
    { ""id"": ""kurt"", ""display"": ""Kurt"", ""aliases"": [""wolf""] }
  ] }";

    static MatchWordTracker NewTracker() => new MatchWordTracker(WordDatabase.FromJson(Json, 50));

    [Test]
    public void Use_FirstTime_IsFoundAndLocked()
    {
        var tracker = NewTracker();
        var result = tracker.Use("zombi");
        Assert.AreEqual(WordLookupStatus.Found, result.Status);
        Assert.IsTrue(tracker.IsUsed("zombi"));
    }

    [Test]
    public void Use_SameWordTwice_SecondIsAlreadyUsed()
    {
        var tracker = NewTracker();
        tracker.Use("zombi");
        var second = tracker.Use("zombi");
        Assert.AreEqual(WordLookupStatus.AlreadyUsed, second.Status);
        Assert.AreEqual("zombi", second.Entry.id);
    }

    [TestCase("zombie")]
    [TestCase("ZOMBİ")]
    [TestCase("yuruyen olu")]
    [TestCase("Yürüyen Ölü")]
    public void Use_SynonymOrSpellingOfUsedWord_IsAlreadyUsed(string second)
    {
        var tracker = NewTracker();
        tracker.Use("zombi");
        Assert.AreEqual(WordLookupStatus.AlreadyUsed, tracker.Use(second).Status);
    }

    [Test]
    public void Use_DifferentWord_IsStillAllowed()
    {
        var tracker = NewTracker();
        tracker.Use("zombi");
        Assert.AreEqual(WordLookupStatus.Found, tracker.Use("kurt").Status);
    }

    [Test]
    public void Use_UnknownWord_IsNotLocked()
    {
        var tracker = NewTracker();
        Assert.AreEqual(WordLookupStatus.Unknown, tracker.Use("blabla").Status);
        Assert.AreEqual(WordLookupStatus.Unknown, tracker.Use("blabla").Status);
        Assert.AreEqual(0, tracker.UsedWords.Count);
    }

    [Test]
    public void Use_EmptyBlockedAndTooLong_AreNotLocked()
    {
        var tracker = NewTracker();
        Assert.AreEqual(WordLookupStatus.Empty, tracker.Use("  ").Status);
        Assert.AreEqual(WordLookupStatus.Blocked, tracker.Use("yasaklı").Status);
        Assert.AreEqual(WordLookupStatus.TooLong, tracker.Use(new string('a', 51)).Status);
        Assert.AreEqual(0, tracker.UsedWords.Count);
    }

    [Test]
    public void UsedWords_KeepsOrderAndNoDuplicates()
    {
        var tracker = NewTracker();
        tracker.Use("kurt");
        tracker.Use("zombi");
        tracker.Use("wolf");

        Assert.AreEqual(2, tracker.UsedWords.Count);
        Assert.AreEqual("kurt", tracker.UsedWords[0].id);
        Assert.AreEqual("zombi", tracker.UsedWords[1].id);
    }

    [Test]
    public void Reset_UnlocksEverything()
    {
        var tracker = NewTracker();
        tracker.Use("zombi");
        tracker.Reset();

        Assert.IsFalse(tracker.IsUsed("zombi"));
        Assert.AreEqual(0, tracker.UsedWords.Count);
        Assert.AreEqual(WordLookupStatus.Found, tracker.Use("zombie").Status);
    }

    [Test]
    public void TwoTrackers_DoNotShareLocks()
    {
        var db = WordDatabase.FromJson(Json, 50);
        var matchA = new MatchWordTracker(db);
        var matchB = new MatchWordTracker(db);

        matchA.Use("zombi");
        Assert.AreEqual(WordLookupStatus.Found, matchB.Use("zombi").Status);
    }
}
