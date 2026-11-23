using System;
using NUnit.Framework;

/// <summary>WordDatabase testleri: arama, hata durumları ve gerçek words.json'un tutarlılığı.</summary>
public class WordDatabaseTests
{
    const int MaxLength = 50;

    const string SmallJson = @"{
  ""version"": 1, ""language"": ""tr"", ""blocked"": [""yasakli kelime""],
  ""words"": [
    { ""id"": ""zombi"", ""display"": ""Zombi"", ""aliases"": [""zombie"", ""yürüyen ölü""], ""category"": ""attack"", ""scope"": ""target"", ""tags"": [""undead"", ""slow""], ""stats"": { ""damage"": 1, ""speed"": 2.0, ""duration"": 12, ""radius"": 0 }, ""counter"": null, ""description"": ""x"" },
    { ""id"": ""atom_bombasi"", ""display"": ""Atom bombası"", ""aliases"": [""nuke""], ""category"": ""threat"", ""scope"": ""global"", ""tags"": [""explosive"", ""map_wide""], ""stats"": { ""damage"": 99, ""warningSeconds"": 8 }, ""counter"": { ""type"": ""shelter"", ""count"": 1, ""capacityRatio"": 0.5 }, ""description"": ""y"" }
  ]
}";

    static WordDatabase Small() => WordDatabase.FromJson(SmallJson, MaxLength);

    [Test]
    public void Lookup_FindsById()
    {
        var result = Small().Lookup("zombi");
        Assert.AreEqual(WordLookupStatus.Found, result.Status);
        Assert.AreEqual("zombi", result.Entry.id);
    }

    [Test]
    public void Lookup_FindsByAlias_AndDisplay()
    {
        var db = Small();
        Assert.AreEqual("zombi", db.Lookup("zombie").Entry.id);
        Assert.AreEqual("atom_bombasi", db.Lookup("Atom bombası").Entry.id);
        Assert.AreEqual("atom_bombasi", db.Lookup("atom_bombasi").Entry.id);
    }

    [TestCase("ZOMBİ")]
    [TestCase("  zombi  ")]
    [TestCase("Zombi!!!")]
    [TestCase("ZOMBIE")]
    public void Lookup_IgnoresCaseSpacesAndPunctuation(string input)
    {
        Assert.AreEqual(WordLookupStatus.Found, Small().Lookup(input).Status);
    }

    [TestCase("yürüyen ölü")]
    [TestCase("YÜRÜYEN ÖLÜ")]
    [TestCase("yuruyen olu")]
    [TestCase("Yuruyen   Olu")]
    public void Lookup_AccentlessSpellingMatches(string input)
    {
        var result = Small().Lookup(input);
        Assert.AreEqual(WordLookupStatus.Found, result.Status);
        Assert.AreEqual("zombi", result.Entry.id);
    }

    [Test]
    public void Lookup_UnknownWord_ReturnsUnknown()
    {
        var result = Small().Lookup("ejderhaaa");
        Assert.AreEqual(WordLookupStatus.Unknown, result.Status);
        Assert.IsNull(result.Entry);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("    ")]
    [TestCase("😀😀")]
    public void Lookup_EmptyLikeInput_ReturnsEmpty(string input)
    {
        Assert.AreEqual(WordLookupStatus.Empty, Small().Lookup(input).Status);
    }

    [Test]
    public void Lookup_TooLongInput_ReturnsTooLong()
    {
        string longInput = new string('a', MaxLength + 1);
        Assert.AreEqual(WordLookupStatus.TooLong, Small().Lookup(longInput).Status);
    }

    [Test]
    public void Lookup_ExactlyMaxLength_IsNotTooLong()
    {
        string input = new string('a', MaxLength);
        Assert.AreEqual(WordLookupStatus.Unknown, Small().Lookup(input).Status);
    }

    [Test]
    public void Lookup_BlockedWord_ReturnsBlocked()
    {
        Assert.AreEqual(WordLookupStatus.Blocked, Small().Lookup("YASAKLI KELİME").Status);
    }

    [Test]
    public void Lookup_ThreatEntry_HasCounter()
    {
        var entry = Small().Lookup("nuke").Entry;
        Assert.IsTrue(entry.IsThreat);
        Assert.IsTrue(entry.HasCounter);
        Assert.AreEqual("shelter", entry.counter.type);
        Assert.AreEqual(0.5f, entry.counter.capacityRatio, 0.0001f);
        Assert.AreEqual(8f, entry.stats.warningSeconds, 0.0001f);
    }

    [Test]
    public void Lookup_NonThreatEntry_HasNoCounter()
    {
        var entry = Small().Lookup("zombi").Entry;
        Assert.IsFalse(entry.IsThreat);
        Assert.IsFalse(entry.HasCounter);
        Assert.IsTrue(entry.HasTag("undead"));
        Assert.IsFalse(entry.HasTag("fire"));
    }

    [Test]
    public void FromJson_SameAliasInTwoEntries_Throws()
    {
        const string json = @"{ ""words"": [
            { ""id"": ""a"", ""display"": ""A"", ""aliases"": [""ortak""] },
            { ""id"": ""b"", ""display"": ""B"", ""aliases"": [""Ortak""] } ] }";
        var ex = Assert.Throws<InvalidOperationException>(() => WordDatabase.FromJson(json, MaxLength));
        StringAssert.Contains("'a'", ex.Message);
        StringAssert.Contains("'b'", ex.Message);
    }

    [Test]
    public void FromJson_AliasesDifferingOnlyByAccents_CountAsDuplicates()
    {
        const string json = @"{ ""words"": [
            { ""id"": ""a"", ""display"": ""A"", ""aliases"": [""ölü""] },
            { ""id"": ""b"", ""display"": ""B"", ""aliases"": [""olu""] } ] }";
        Assert.Throws<InvalidOperationException>(() => WordDatabase.FromJson(json, MaxLength));
    }

    [Test]
    public void FromJson_DuplicateId_Throws()
    {
        const string json = @"{ ""words"": [
            { ""id"": ""a"", ""display"": ""A"" },
            { ""id"": ""a"", ""display"": ""B"" } ] }";
        Assert.Throws<InvalidOperationException>(() => WordDatabase.FromJson(json, MaxLength));
    }

    [Test]
    public void FromJson_MissingId_Throws()
    {
        const string json = @"{ ""words"": [ { ""display"": ""A"" } ] }";
        Assert.Throws<InvalidOperationException>(() => WordDatabase.FromJson(json, MaxLength));
    }

    [Test]
    public void FromJson_EmptyText_Throws()
    {
        Assert.Throws<ArgumentException>(() => WordDatabase.FromJson("  ", MaxLength));
    }

    [Test]
    public void FromJson_EntryWithoutAliases_StillLoads()
    {
        const string json = @"{ ""words"": [ { ""id"": ""a"", ""display"": ""A"" } ] }";
        var db = WordDatabase.FromJson(json, MaxLength);
        Assert.AreEqual("a", db.Lookup("a").Entry.id);
    }

    // ---- Gerçek words.json dosyasının tutarlılığı ----

    static readonly string[] AllowedTags =
    {
        "slow", "fast", "flying", "swarm", "undead", "predator", "instant", "knockback", "trap",
        "slowdown", "pull", "armored", "explosive", "area", "water", "fire", "sky", "map_wide",
        "shield", "heal", "wall", "escape", "stealth", "revive"
    };

    static readonly string[] AllowedCounters = { "shelter", "tower", "cave", "shadows", "houses" };

    [Test]
    public void RealWordsFile_LoadsWithoutErrors()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        Assert.GreaterOrEqual(db.Words.Count, 20);
    }

    [Test]
    public void RealWordsFile_EveryThreatIsGlobalAndHasCounter()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        foreach (WordEntry entry in db.Words)
        {
            if (!entry.IsThreat) continue;
            Assert.AreEqual(WordEntry.ScopeGlobal, entry.scope, entry.id + ": tehdit global olmalı");
            Assert.IsTrue(entry.HasCounter, entry.id + ": tehdidin çözümü (counter) olmalı");
            CollectionAssert.Contains(AllowedCounters, entry.counter.type, entry.id + ": bilinmeyen counter türü");
        }
    }

    [Test]
    public void RealWordsFile_OnlyThreatsAreGlobal()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        foreach (WordEntry entry in db.Words)
            Assert.AreEqual(entry.IsThreat, entry.scope == WordEntry.ScopeGlobal, entry.id + ": global kapsam yalnızca tehdit için");
    }

    [Test]
    public void RealWordsFile_AllTagsAreInGddList()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        foreach (WordEntry entry in db.Words)
        {
            Assert.IsNotNull(entry.tags, entry.id + ": etiket listesi yok");
            foreach (string tag in entry.tags)
                CollectionAssert.Contains(AllowedTags, tag, entry.id + ": GDD'de olmayan etiket");
        }
    }

    [Test]
    public void RealWordsFile_EveryWordFindsItselfAndItsAliases()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        foreach (WordEntry entry in db.Words)
        {
            Assert.AreEqual(entry.id, db.Lookup(entry.display).Entry.id, entry.id + ": görünen ad bulunamadı");
            foreach (string alias in entry.aliases)
                Assert.AreEqual(entry.id, db.Lookup(alias).Entry.id, entry.id + ": '" + alias + "' bulunamadı");
        }
    }

    [TestCase("dragon", "ejderha")]
    [TestCase("DRAGON", "ejderha")]
    [TestCase("Wyvern", "ejderha")]
    [TestCase("nuke", "atom_bombasi")]
    [TestCase("Nuclear Bomb", "atom_bombasi")]
    [TestCase("A-Bomb", "atom_bombasi")]
    [TestCase("atomic bomb", "atom_bombasi")]
    [TestCase("tidal wave", "tsunami")]
    [TestCase("flood", "tsunami")]
    [TestCase("asteroid", "meteor_yagmuru")]
    [TestCase("Meteor Shower", "meteor_yagmuru")]
    [TestCase("zombie", "zombi")]
    [TestCase("ghoul", "zombi")]
    [TestCase("zombie horde", "zombi_suru")]
    [TestCase("zombies", "zombi_suru")]
    [TestCase("wasp", "ari")]
    [TestCase("hornet", "ari")]
    [TestCase("rat", "fare")]
    [TestCase("banana peel", "muz_kabugu")]
    [TestCase("turtle", "kaplumbaga")]
    [TestCase("tortoise", "kaplumbaga")]
    [TestCase("magnet", "miknatis")]
    [TestCase("grenade", "bomba")]
    [TestCase("dynamite", "bomba")]
    [TestCase("potion", "can")]
    [TestCase("healing", "can")]
    [TestCase("barricade", "duvar")]
    [TestCase("wings", "kanat")]
    [TestCase("invisibility", "gorunmezlik")]
    [TestCase("INVISIBLE", "gorunmezlik")]
    [TestCase("force field", "kalkan")]
    [TestCase("punch", "yumruk")]
    [TestCase("wolf", "kurt")]
    [TestCase("storm cloud", "bulut")]
    public void RealWordsFile_EnglishWordsAndSynonymsWork(string typed, string expectedId)
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        var result = db.Lookup(typed);

        Assert.AreEqual(WordLookupStatus.Found, result.Status, typed + " bulunamadı");
        Assert.AreEqual(expectedId, result.Entry.id);
    }

    [Test]
    public void RealWordsFile_EveryWordHasAtLeastOneEnglishStyleAlias()
    {
        // Her kelimenin eş anlamlıları arasında, kendi id/görünen adından farklı en az 3 yazım olmalı.
        var db = WordDatabase.LoadFromResources(MaxLength);
        foreach (WordEntry entry in db.Words)
            Assert.GreaterOrEqual(entry.aliases.Length, 3, entry.id + ": eş anlamlı az");
    }

    [Test]
    public void RealWordsFile_ZombiAndZombiSurusuAreDifferentWords()
    {
        var db = WordDatabase.LoadFromResources(MaxLength);
        Assert.AreEqual("zombi", db.Lookup("zombi").Entry.id);
        Assert.AreEqual("zombi_suru", db.Lookup("Zombi sürüsü").Entry.id);
    }
}
