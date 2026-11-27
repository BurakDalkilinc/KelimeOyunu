using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Bot adları: tek kişilikte takma adlar (tohumla aynı, tekrarsız, yerel adla çakışmaz, liste bozuksa "Bot N"),
/// bot_names.json'un kendisi (temiz, 3-16 harf, kelime değil, "bot" geçmiyor) ve ağdaki [BOT] rozeti.
/// </summary>
public class BotNameTests
{
    static GameConfig Config => GameConfigLoader.Get();

    static BotNameGenerator FromFile() =>
        BotNameGenerator.LoadFromResources(Config.botNamesPath, Config.botNameMinLength, Config.botNameMaxLength);

    static BotNameGenerator FromList(params string[] names)
    {
        string json = JsonUtility.ToJson(new NameList { names = names });
        return BotNameGenerator.FromJson(json, 3, 16);
    }

    [System.Serializable]
    class NameList { public string[] names; }

    // ---- Seçim ----

    [Test]
    public void SameSeed_GivesSameNames_DifferentSeed_Differs()
    {
        BotNameGenerator generator = FromFile();
        CollectionAssert.AreEqual(generator.Pick(19, 1234, null), generator.Pick(19, 1234, null));
        CollectionAssert.AreNotEqual(generator.Pick(19, 1234, null), generator.Pick(19, 98765, null));
    }

    [Test]
    public void Names_AreUnique_WithinAMatch()
    {
        BotNameGenerator generator = FromFile();
        for (int seed = 0; seed < 50; seed++)
        {
            List<string> names = generator.Pick(RosterBuilder.MaxPlayers - 1, seed, null);
            var seen = new HashSet<string>();
            foreach (string name in names) Assert.IsTrue(seen.Add(name.ToLowerInvariant()), "tekrar eden ad: " + name);
        }
    }

    [Test]
    public void Names_NeverCollideWithLocalPlayer_IgnoringCase()
    {
        BotNameGenerator generator = FromList("Sen", "KaraKartal", "pixelpanda");
        for (int seed = 0; seed < 30; seed++)
        {
            List<string> names = generator.Pick(2, seed, new[] { "sen" });
            CollectionAssert.DoesNotContain(names, "Sen");
            CollectionAssert.AreEquivalent(new[] { "KaraKartal", "pixelpanda" }, names);
        }
    }

    [Test]
    public void TakenList_IsNotModified()
    {
        var taken = new List<string> { "Sen" };
        FromList("KaraKartal", "pixelpanda").Pick(2, 7, taken);
        CollectionAssert.AreEqual(new[] { "Sen" }, taken);
    }

    [Test]
    public void EmptyOrBrokenList_FallsBackToBotN()
    {
        foreach (string json in new[] { null, "", "   ", "{ bozuk json", "{\"names\": []}", "{\"version\": 1}" })
        {
            BotNameGenerator generator = BotNameGenerator.FromJson(json, 3, 16);
            Assert.AreEqual(0, generator.Names.Count, "metin: " + json);
            CollectionAssert.AreEqual(new[] { "Bot 1", "Bot 2", "Bot 3" }, generator.Pick(3, 5, new[] { "Sen" }));
        }
    }

    [Test]
    public void ShortList_FillsTheRestWithBotN_StillUnique()
    {
        List<string> names = FromList("KaraKartal").Pick(3, 1, null);
        Assert.AreEqual("KaraKartal", names[0]);
        CollectionAssert.AreEqual(new[] { "Bot 2", "Bot 3" }, names.GetRange(1, 2));

        List<string> clash = BotNameGenerator.FromJson(null, 3, 16).Pick(1, 1, new[] { "Bot 1" });
        Assert.AreEqual("Bot 1 (2)", clash[0], "yerel ad 'Bot 1' ise yedek ad da çakışmaz");
    }

    [Test]
    public void InvalidEntries_AreSkipped()
    {
        BotNameGenerator generator = FromList("ab", "Bu ad on altıdan uzun", " Bosluklu", "A;B;C", "Iki  bosluk", "KaraKartal", "karakartal", null, "");
        CollectionAssert.AreEqual(new[] { "KaraKartal" }, generator.Names);
    }

    [Test]
    public void FakeNamesOff_GivesBotN()
    {
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
        config.singlePlayerFakeBotNames = false;
        CollectionAssert.AreEqual(new[] { "Bot 1", "Bot 2" }, BotNameGenerator.ForSinglePlayer(config, 2, 3, null));

        config.singlePlayerFakeBotNames = true;
        List<string> fake = BotNameGenerator.ForSinglePlayer(config, 7, 3, new[] { "Sen" });
        Assert.AreEqual(7, fake.Count);
        foreach (string name in fake) StringAssert.DoesNotContain("Bot", name);
        Object.DestroyImmediate(config);
    }

    // ---- bot_names.json ----

    [Test]
    public void File_HasEnoughNames_AllKept()
    {
        TextAsset asset = Resources.Load<TextAsset>(Config.botNamesPath);
        Assert.IsNotNull(asset, "bot_names.json bulunamadı");
        int inFile = JsonUtility.FromJson<NameList>(asset.text).names.Length;

        BotNameGenerator generator = FromFile();
        Assert.GreaterOrEqual(generator.Names.Count, 150);
        Assert.AreEqual(inFile, generator.Names.Count, "dosyadaki her ad geçerli ve tekrarsız olmalı");
    }

    [Test]
    public void EveryName_PassesSanitizerUnchanged_AndIs3To16Letters()
    {
        foreach (string name in FromFile().Names)
        {
            Assert.That(name.Length, Is.InRange(3, 16), name);
            Assert.AreEqual(name, PlayerNameSanitizer.Sanitize(name, 1, Config.playerNameMaxLength), name);
            Assert.AreEqual(name, PlayerNameSanitizer.Clean(name, Config.botNameMaxLength), name);
        }
    }

    [Test]
    public void EveryName_FitsTheNameTag_WithoutShortening()
    {
        // Kısaltılmış ad ("SozcukSihirba.") yapay görünür; listedeki adlar etikete olduğu gibi sığmalı.
        foreach (string name in FromFile().Names)
            Assert.AreEqual(name, NameTagRules.Shorten(name, Config.nameTagMaxLength), name);
    }

    [Test]
    public void NoName_IsAWordOrAlias()
    {
        WordDatabase words = WordDatabase.LoadFromResources(100);
        foreach (string name in FromFile().Names)
            Assert.AreNotEqual(WordLookupStatus.Found, words.Lookup(name).Status, "ad bir kelimeyle karışır: " + name);
    }

    [Test]
    public void NoName_RevealsItIsABot()
    {
        foreach (string name in FromFile().Names)
        {
            StringAssert.DoesNotContain("bot", name.ToLowerInvariant(), name);
            Assert.AreNotEqual("sen", name.ToLowerInvariant());
            Assert.AreNotEqual("you", name.ToLowerInvariant());
        }
    }

    // ---- İsim etiketi rozeti ----

    static NameTagInfo Tag(string name, bool badge) => new NameTagInfo
    {
        Name = name, IsAlive = true, BodyShown = true, ShowBotBadge = badge,
    };

    [Test]
    public void BotBadge_OnlyOnlineAndOnlyForBots()
    {
        Assert.IsTrue(NameTagRules.ShowsBotBadge(true, true, true), "ağda bot rozetli");
        Assert.IsFalse(NameTagRules.ShowsBotBadge(true, false, true), "tek kişilikte rozet yok");
        Assert.IsFalse(NameTagRules.ShowsBotBadge(false, true, true), "insan rozetsiz");
        Assert.IsFalse(NameTagRules.ShowsBotBadge(true, true, false), "ayarda kapalıysa rozet yok");
    }

    [Test]
    public void Label_OnlineBot_HasBadge_OfflineBotDoesNot()
    {
        Assert.AreEqual("Bot 3 [BOT]", NameTagRules.Label(Tag("Bot 3", true), "[BOT]", 14));
        Assert.AreEqual("KaraKartal", NameTagRules.Label(Tag("KaraKartal", false), "[BOT]", 14));
        Assert.AreEqual("Bot 3", NameTagRules.Label(Tag("Bot 3", true), "", 14), "rozet metni boşsa yalnızca ad");
        Assert.AreEqual("", NameTagRules.Label(Tag("", true), "[BOT]", 14), "ad yoksa rozet de yok");
    }

    [Test]
    public void Label_BadgeIsNotCutByShortening()
    {
        Assert.AreEqual("Çok uzun bir. [BOT]", NameTagRules.Label(Tag("Çok uzun bir bot adı", true), "[BOT]", 13));
    }

    [Test]
    public void BadgeText_ComesFromLanguageFiles()
    {
        Assert.AreEqual("[BOT]", Loc.T("nametag.bot_badge"));
    }

    [Test]
    public void Color_OnlineBotIsGray_OthersUnchanged()
    {
        Color gray = new Color(0.5f, 0.5f, 0.5f, 1f);
        Assert.AreEqual(gray, NameTagRules.ColorFor(Tag("Bot 3", true), Color.yellow, Color.white, gray, 0.4f));
        Assert.AreEqual(Color.white, NameTagRules.ColorFor(Tag("KaraKartal", false), Color.yellow, Color.white, gray, 0.4f));

        NameTagInfo own = Tag("Sen", true);
        own.IsLocal = true;
        Assert.AreEqual(Color.yellow, NameTagRules.ColorFor(own, Color.yellow, Color.white, gray, 0.4f), "kendi adın hep sarı");
    }
}
