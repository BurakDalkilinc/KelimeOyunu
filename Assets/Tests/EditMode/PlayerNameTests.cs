using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Ağ maçında oyuncu adları: sunucu tarafı temizlik ve kadroya yazılması.</summary>
public class PlayerNameTests
{
    const int Max = 24;

    // ---- PlayerNameSanitizer ----

    [Test]
    public void Sanitize_KeepsNormalName_AndTurkishLetters()
    {
        Assert.AreEqual("Çağrı İşık", PlayerNameSanitizer.Sanitize("Çağrı İşık", 1, Max));
    }

    [Test]
    public void Sanitize_TrimsAndCollapsesSpaces()
    {
        Assert.AreEqual("Ali Veli", PlayerNameSanitizer.Sanitize("   Ali     Veli  ", 1, Max));
    }

    [Test]
    public void Sanitize_LongName_IsCutAtMax()
    {
        string name = PlayerNameSanitizer.Sanitize(new string('a', 500), 1, Max);
        Assert.AreEqual(Max, name.Length);
    }

    [Test]
    public void Sanitize_DoesNotSplitSurrogatePair()
    {
        // 23 harf + emoji (2 karakter): emoji yarıdan bölünmez, atılır.
        string name = PlayerNameSanitizer.Sanitize(new string('a', 23) + "\U0001F600", 1, Max);
        Assert.AreEqual(new string('a', 23), name);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("     ")]
    [TestCase("\n\r\t")]
    [TestCase("​‮")]
    public void Sanitize_EmptyOrInvisible_BecomesDefaultName(string raw)
    {
        Assert.AreEqual("Oyuncu 3", PlayerNameSanitizer.Sanitize(raw, 3, Max));
    }

    [Test]
    public void Sanitize_RemovesControlAndFormatCharacters()
    {
        Assert.AreEqual("AliVeli", PlayerNameSanitizer.Sanitize("A\u0000li\u0007‮Veli​", 1, Max));
    }

    [Test]
    public void Sanitize_LineBreaksBecomeOneSpace()
    {
        Assert.AreEqual("Ali Veli", PlayerNameSanitizer.Sanitize("Ali\r\n\r\nVeli\n", 1, Max));
    }

    [Test]
    public void Sanitize_RemovesCodecSeparators()
    {
        string name = PlayerNameSanitizer.Sanitize("Ali|1;Veli", 1, Max);
        Assert.IsFalse(name.Contains("|") || name.Contains(";"));
        Assert.AreEqual("Ali 1 Veli", name);
    }

    [Test]
    public void MakeUnique_AddsNumberSuffix_IgnoringCase()
    {
        var taken = new List<string> { "Ali", "ali (2)" };
        Assert.AreEqual("Ali (3)", PlayerNameSanitizer.MakeUnique("Ali", taken, Max));
        Assert.AreEqual("Veli", PlayerNameSanitizer.MakeUnique("Veli", taken, Max));
    }

    [Test]
    public void MakeUnique_StaysWithinMaxLength()
    {
        string longName = new string('b', Max);
        string unique = PlayerNameSanitizer.MakeUnique(longName, new List<string> { longName }, Max);
        Assert.LessOrEqual(unique.Length, Max);
        Assert.IsTrue(unique.EndsWith(" (2)"));
    }

    // ---- RosterBuilder adları ----

    [Test]
    public void Roster_UsesReportedNames_AndDefaultsForMissing()
    {
        var names = new Dictionary<ulong, string> { { 0, "Burak" } };

        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0, 1 }, 3, names, Max);

        Assert.AreEqual("Burak", roster[0].Name);
        Assert.AreEqual("Oyuncu 1", roster[1].Name);
        Assert.AreEqual("Bot 1", roster[2].Name);
    }

    [Test]
    public void Roster_SanitizesNames_EvenIfServerStoredRawText()
    {
        var names = new Dictionary<ulong, string> { { 0, "  Kötü\nAd|;" + new string('x', 300) } };

        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0 }, 2, names, Max);

        Assert.LessOrEqual(roster[0].Name.Length, Max);
        Assert.IsTrue(roster[0].Name.StartsWith("Kötü Ad"));
        Assert.IsTrue(RosterCodec.TryDecode(RosterCodec.Encode(roster), out List<RosterEntry> back));
        Assert.AreEqual(roster[0].Name, back[0].Name);
    }

    [Test]
    public void Roster_DuplicateNames_GetSuffix_FirstKeepsName()
    {
        var names = new Dictionary<ulong, string> { { 0, "Ayşe" }, { 1, "Ayşe" }, { 2, "ayşe" } };

        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0, 1, 2 }, 3, names, Max);

        Assert.AreEqual("Ayşe", roster[0].Name);
        Assert.AreEqual("Ayşe (2)", roster[1].Name);
        Assert.AreEqual("ayşe (3)", roster[2].Name);
    }

    [Test]
    public void Roster_HumanNamedLikeBot_BotGetsSuffix()
    {
        var names = new Dictionary<ulong, string> { { 0, "Bot 1" } };

        List<RosterEntry> roster = RosterBuilder.Build(new List<ulong> { 0 }, 2, names, Max);

        Assert.AreEqual("Bot 1", roster[0].Name);
        Assert.AreEqual("Bot 1 (2)", roster[1].Name);
        Assert.IsTrue(roster[1].IsBot);
    }

    [Test]
    public void BuildHumans_HasNoBots()
    {
        List<RosterEntry> humans = RosterBuilder.BuildHumans(new List<ulong> { 0, 4 }, null, Max);

        Assert.AreEqual(2, humans.Count);
        Assert.IsTrue(humans.TrueForAll(h => !h.IsBot));
        Assert.AreEqual("Oyuncu 4", humans[1].Name);
    }
}
