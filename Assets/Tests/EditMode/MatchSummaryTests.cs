using System.Collections.Generic;
using NUnit.Framework;

/// <summary>MatchSummary testleri: maç sonu ekranındaki kazanan başlığı ve kelime listesi.</summary>
public class MatchSummaryTests
{
    static List<WordEntry> Words(params string[] displays)
    {
        var list = new List<WordEntry>();
        foreach (string display in displays) list.Add(new WordEntry { id = display.ToLowerInvariant(), display = display });
        return list;
    }

    [Test]
    public void Title_WithWinner_NamesThem()
    {
        Assert.AreEqual("Kazanan: Bot 3", MatchSummary.GetTitle(new PlayerState(3, "Bot 3", 3)));
        Assert.AreEqual("Kazanan: Sen", MatchSummary.GetTitle(new PlayerState(0, "Sen", 3)));
    }

    [Test]
    public void Title_WithoutWinner_SaysSo()
    {
        Assert.AreEqual("Kazanan yok", MatchSummary.GetTitle(null));
    }

    [Test]
    public void Words_None_SaysNoWordsWereWritten()
    {
        Assert.AreEqual("Maçta hiç kelime yazılmadı.", MatchSummary.GetWordsText(new List<WordEntry>()));
        Assert.AreEqual("Maçta hiç kelime yazılmadı.", MatchSummary.GetWordsText(null));
    }

    [Test]
    public void Words_One_HasTheCountAndTheWord()
    {
        Assert.AreEqual("Yazılan kelimeler (1):\nZombi", MatchSummary.GetWordsText(Words("Zombi")));
    }

    [Test]
    public void Words_Several_KeepWritingOrder_SeparatedByCommas()
    {
        string text = MatchSummary.GetWordsText(Words("Zombi", "Atom bombası", "Kurt"));

        Assert.AreEqual("Yazılan kelimeler (3):\nZombi, Atom bombası, Kurt", text);
    }

    [Test]
    public void Words_LongList_WrapsEverySixWords_WithoutTrailingComma()
    {
        var list = Words("A", "B", "C", "D", "E", "F", "G", "H");
        string text = MatchSummary.GetWordsText(list);

        string[] lines = text.Split('\n');
        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual("Yazılan kelimeler (8):", lines[0]);
        Assert.AreEqual("A, B, C, D, E, F,", lines[1]);
        Assert.AreEqual("G, H", lines[2]);
        Assert.IsFalse(text.EndsWith(","));
    }

    [Test]
    public void Words_EntryWithoutDisplay_ShowsAPlaceholder_AndNullEntriesDoNotThrow()
    {
        var list = new List<WordEntry> { new WordEntry { id = "x", display = "" }, null };

        string text = MatchSummary.GetWordsText(list);

        Assert.AreEqual("Yazılan kelimeler (2):\n?, ?", text);
    }

    [Test]
    public void Hint_MentionsTheRestartKey()
    {
        StringAssert.Contains("R", MatchSummary.GetHint());
    }
}
