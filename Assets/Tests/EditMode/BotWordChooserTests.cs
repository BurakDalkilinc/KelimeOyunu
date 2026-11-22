using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>BotWordChooser testleri: bot yalnızca kullanılmamış kelime seçer, hepsi bitince bilinmeyen kelime yazar.</summary>
public class BotWordChooserTests
{
    static List<WordEntry> Words(params string[] ids)
    {
        var list = new List<WordEntry>();
        foreach (string id in ids)
            list.Add(new WordEntry { id = id, display = id.ToUpperInvariant() });
        return list;
    }

    [Test]
    public void Choose_NeverReturnsUsedWord()
    {
        var words = Words("a", "b", "c");
        var random = new Random(1);

        for (int i = 0; i < 300; i++)
        {
            string chosen = BotWordChooser.Choose(words, id => id == "a" || id == "b", random);
            Assert.AreEqual("C", chosen);
        }
    }

    [Test]
    public void Choose_AllUsed_ReturnsFallbackUnknownWord()
    {
        var words = Words("a", "b");
        Assert.AreEqual(BotWordChooser.FallbackUnknownWord, BotWordChooser.Choose(words, id => true, new Random(1)));
    }

    [Test]
    public void Choose_EmptyList_ReturnsFallback()
    {
        Assert.AreEqual(BotWordChooser.FallbackUnknownWord, BotWordChooser.Choose(new List<WordEntry>(), id => false, new Random(1)));
    }

    [Test]
    public void Choose_PicksAllAvailableWordsOverTime()
    {
        var words = Words("a", "b", "c", "d");
        var seen = new HashSet<string>();
        var random = new Random(3);

        for (int i = 0; i < 200; i++)
            seen.Add(BotWordChooser.Choose(words, id => false, random));

        Assert.AreEqual(4, seen.Count);
    }

    [Test]
    public void Choose_SameSeed_GivesSameSequence()
    {
        var words = Words("a", "b", "c", "d", "e");
        var first = new Random(9);
        var second = new Random(9);

        for (int i = 0; i < 50; i++)
            Assert.AreEqual(BotWordChooser.Choose(words, id => false, first), BotWordChooser.Choose(words, id => false, second));
    }

    [Test]
    public void Choose_EntryWithoutDisplay_FallsBackToId()
    {
        var words = new List<WordEntry> { new WordEntry { id = "kurt", display = "" } };
        Assert.AreEqual("kurt", BotWordChooser.Choose(words, id => false, new Random(1)));
    }

    [Test]
    public void Choose_SkipsNullEntries()
    {
        var words = new List<WordEntry> { null, new WordEntry { id = "a", display = "A" } };
        Assert.AreEqual("A", BotWordChooser.Choose(words, id => false, new Random(1)));
    }

    [Test]
    public void Choose_ChosenTextIsFoundByTheDatabase_WhenUsedWithRealWords()
    {
        var db = WordDatabase.LoadFromResources(50);
        var tracker = new MatchWordTracker(db);
        var random = new Random(5);

        // Bot, tüm kelimeleri sırayla yazar: her biri kabul edilmeli, sonunda bilinmeyen kelimeye düşmeli.
        for (int i = 0; i < db.Words.Count; i++)
        {
            string text = BotWordChooser.Choose(db.Words, tracker.IsUsed, random);
            Assert.AreEqual(WordLookupStatus.Found, tracker.Use(text).Status, text);
        }

        string last = BotWordChooser.Choose(db.Words, tracker.IsUsed, random);
        Assert.AreEqual(BotWordChooser.FallbackUnknownWord, last);
        Assert.AreEqual(WordLookupStatus.Unknown, tracker.Use(last).Status);
    }

    [Test]
    public void Choose_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BotWordChooser.Choose(null, id => false, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => BotWordChooser.Choose(Words("a"), null, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => BotWordChooser.Choose(Words("a"), id => false, null));
    }
}
