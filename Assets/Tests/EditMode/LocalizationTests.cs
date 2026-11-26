using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Dil desteği testleri: tr/en tablolarında aynı anahtarlar, boş metin yok, yer tutucular iki dilde aynı, koddaki her anahtar
/// tabloda var; eksik anahtarda Türkçe'ye ve anahtara düşme; İngilizce kelime eşleştirme ve kilit; İngilizce ad çakışması yok;
/// dil ayarının kaydı. Dili değiştiren testler sonunda Türkçe'ye döner.
/// </summary>
public class LocalizationTests
{
    const int MaxLength = 30;
    static readonly Regex Placeholder = new Regex(@"\{(\d+)(?:[,:][^}]*)?\}");

    [TearDown]
    public void BackToTurkish() => Loc.SetLanguage(Language.Turkish);

    static LocTable Tr => LocTable.LoadFromResources(Language.Turkish);
    static LocTable En => LocTable.LoadFromResources(Language.English);

    static WordDatabase Words() => WordDatabase.LoadFromResources(MaxLength);

    // ---- Tablolar ----

    [Test]
    public void Tables_LoadAndAreNotEmpty()
    {
        Assert.Greater(Tr.Count, 100);
        Assert.Greater(En.Count, 100);
        Assert.IsEmpty(Tr.DuplicateKeys, "lang_tr.json'da iki kez yazılmış anahtar");
        Assert.IsEmpty(En.DuplicateKeys, "lang_en.json'da iki kez yazılmış anahtar");
    }

    [Test]
    public void Tables_HaveTheSameKeys()
    {
        var tr = new HashSet<string>(Tr.Keys);
        var en = new HashSet<string>(En.Keys);
        Assert.IsEmpty(tr.Except(en).ToList(), "İngilizce'de eksik anahtarlar");
        Assert.IsEmpty(en.Except(tr).ToList(), "Türkçe'de eksik anahtarlar");
    }

    [Test]
    public void Tables_HaveNoEmptyValues()
    {
        foreach (LocTable table in new[] { Tr, En })
            foreach (string key in table.Keys)
                Assert.IsFalse(string.IsNullOrWhiteSpace(table.RawValue(key)), "Boş metin: " + key);
    }

    [Test]
    public void Tables_PlaceholdersMatchInBothLanguages()
    {
        LocTable tr = Tr, en = En;
        foreach (string key in tr.Keys)
        {
            string trText = tr.RawValue(key), enText = en.RawValue(key);
            if (enText == null) continue; // Anahtar eksikliğini başka test yakalar
            CollectionAssert.AreEquivalent(Indices(trText), Indices(enText), "Yer tutucular farklı: " + key);

            // Biçim iki dilde de geçerli olmalı (yer tutucu sayısı kadar değerle çökmemeli).
            object[] args = Enumerable.Range(0, Indices(trText).DefaultIfEmpty(-1).Max() + 1).Select(i => (object)i).ToArray();
            Assert.DoesNotThrow(() => string.Format(trText, args), "Türkçe biçim bozuk: " + key);
            Assert.DoesNotThrow(() => string.Format(enText, args), "İngilizce biçim bozuk: " + key);
        }
    }

    static List<int> Indices(string text)
    {
        return Placeholder.Matches(text).Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(i => i).ToList();
    }

    [Test]
    public void EveryKeyUsedInCode_ExistsInTheTables()
    {
        // Tablodaki ön eklerle ("settings.", "steam."...) başlayan her metin sabiti bir anahtar sayılır
        // (Loc.T("..."), Loc.F("..."), SetStatus("...") ve koşullu ifadelerdeki anahtarlar da yakalanır).
        LocTable tr = Tr;
        string prefixes = string.Join("|", tr.Keys.Select(k => Regex.Escape(k.Substring(0, k.IndexOf('.')))).Distinct());
        var used = new Regex("\"((?:" + prefixes + ")\\.(?!json\")[a-z_]+)\""); // "settings.json" gibi dosya adları anahtar değil
        var missing = new List<string>();
        foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories))
            foreach (Match match in used.Matches(File.ReadAllText(file)))
                if (tr.RawValue(match.Groups[1].Value) == null) missing.Add(match.Groups[1].Value + " (" + Path.GetFileName(file) + ")");
        Assert.IsEmpty(missing, "Kodda kullanılıp tabloda olmayan anahtarlar");
    }

    // ---- Düşme davranışı ----

    static Localizer Fake(out List<string> warnings)
    {
        var tr = LocTable.FromPairs(new Dictionary<string, string> { { "a", "Türkçe A" }, { "b", "Türkçe B" }, { "f", "Kazanan: {0}" } });
        var en = LocTable.FromPairs(new Dictionary<string, string> { { "a", "English A" }, { "f", "Winner: {0}" }, { "bad", "{0} {1}" } });
        var list = new List<string>();
        warnings = list;
        return new Localizer(code => code == Language.English ? en : tr) { Warn = list.Add };
    }

    [Test]
    public void Missing_FallsBackToTurkish_ThenToKey_WarningOnce()
    {
        Localizer loc = Fake(out List<string> warnings);
        loc.SetLanguage(Language.English);

        Assert.AreEqual("English A", loc.T("a"));
        Assert.AreEqual("Türkçe B", loc.T("b"), "İngilizce'de yoksa Türkçe");
        Assert.AreEqual("yok.boyle", loc.T("yok.boyle"), "Hiçbir dilde yoksa anahtarın kendisi");
        loc.T("b");
        loc.T("yok.boyle");
        Assert.AreEqual(2, warnings.Count, "Her eksik anahtar için bir kez uyarı");
        Assert.AreEqual(string.Empty, loc.T(null));
    }

    [Test]
    public void Format_UsesSelectedLanguage_AndBadFormatDoesNotThrow()
    {
        Localizer loc = Fake(out _);
        Assert.AreEqual("Kazanan: Bot 3", loc.F("f", "Bot 3"));
        loc.SetLanguage(Language.English);
        Assert.AreEqual("Winner: Bot 3", loc.F("f", "Bot 3"));
        Assert.AreEqual("{0} {1}", loc.F("bad", "tek"), "Eksik argümanda ham metin");
    }

    [Test]
    public void SetLanguage_RaisesEventOnlyOnRealChange()
    {
        Localizer loc = Fake(out _);
        var changes = new List<string>();
        loc.LanguageChanged += changes.Add;

        Assert.IsFalse(loc.SetLanguage(Language.Turkish), "Zaten Türkçe");
        Assert.IsFalse(loc.SetLanguage("de"), "Desteklenmeyen dil yok sayılır");
        Assert.IsTrue(loc.SetLanguage(" EN "));
        Assert.AreEqual(Language.English, loc.Current);
        CollectionAssert.AreEqual(new[] { Language.English }, changes);
    }

    [Test]
    public void Language_Helpers()
    {
        Assert.AreEqual(Language.Turkish, Language.FromSystem(SystemLanguage.Turkish));
        Assert.AreEqual(Language.English, Language.FromSystem(SystemLanguage.English));
        Assert.AreEqual(Language.English, Language.FromSystem(SystemLanguage.German));
        Assert.AreEqual(Language.English, Language.Next(Language.Turkish));
        Assert.AreEqual(Language.Turkish, Language.Next(Language.English));
        Assert.AreEqual("Türkçe", Language.NativeName(Language.Turkish));
        Assert.AreEqual("English", Language.NativeName(Language.English));
    }

    // ---- Oyundaki metinler İngilizce'de ----

    [Test]
    public void GameTexts_SwitchToEnglish()
    {
        var winner = new PlayerState(3, "Bot 3", 3, true);
        Assert.AreEqual("Kazanan: Bot 3", MatchSummary.GetTitle(winner));
        Assert.AreEqual("%80", SettingsText.Percent(0.8f));

        Loc.SetLanguage(Language.English);
        Assert.AreEqual("Winner: Bot 3", MatchSummary.GetTitle(winner));
        Assert.AreEqual("80%", SettingsText.Percent(0.8f));
        Assert.AreEqual("Tower 2/3", ThreatText.GetZoneLabel("tower", 2, 3));
        Assert.AreEqual("Player 2", NameTagRules.DefaultName(2, false, 100));
        Assert.AreEqual("Bot 1", NameTagRules.DefaultName(100, true, 100));
    }

    [Test]
    public void WordNames_FollowLanguage_InSummaryAlarmAndBook()
    {
        WordDatabase db = Words();
        WordEntry zombi = db.FindById("zombi");
        WordEntry threat = db.Words.First(w => w.IsThreat && w.HasCounter);

        Loc.SetLanguage(Language.English);
        StringAssert.Contains("Zombie", MatchSummary.GetWordsText(new[] { zombi }));
        StringAssert.StartsWith(threat.displayEn.ToUpper(Language.Culture(Language.English)) + "!", ThreatText.GetAlarm(threat, 5));
        Assert.AreEqual("Z _ _ _ _ _", CollectionText.LetterHint(zombi));
        Assert.AreEqual(zombi.descriptionEn, zombi.LocalizedDescription);
        Assert.AreEqual("Zombie", CreatureLabel.Resolve(CreatureLabel.NameToken, zombi));

        Loc.SetLanguage(Language.Turkish);
        Assert.AreEqual("Z _ _ _ _", CollectionText.LetterHint(zombi));
        Assert.AreEqual("Zombi 5/12", CreatureLabel.Resolve(CreatureLabel.NameToken + " 5/12", zombi));
        Assert.AreEqual("???", CreatureLabel.Resolve("???", zombi), "Belirteçsiz yazı aynen kalır");
    }

    [Test]
    public void DisplayFor_FallsBackToTurkishThenId()
    {
        var word = new WordEntry { id = "x", display = "İks", description = "Türkçe" };
        Assert.AreEqual("İks", word.DisplayFor(Language.English));
        Assert.AreEqual("Türkçe", word.DescriptionFor(Language.English));
        word.displayEn = "Ex";
        word.descriptionEn = "English";
        Assert.AreEqual("Ex", word.DisplayFor(Language.English));
        Assert.AreEqual("İks", word.DisplayFor(Language.Turkish));
        Assert.AreEqual("English", word.DescriptionFor(Language.English));
        Assert.AreEqual("x", new WordEntry { id = "x" }.DisplayFor(Language.Turkish));
    }

    [Test]
    public void EveryWord_HasEnglishNameAndDescription()
    {
        foreach (WordEntry word in Words().Words)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(word.displayEn), "displayEn yok: " + word.id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(word.descriptionEn), "descriptionEn yok: " + word.id);
        }
    }

    [Test]
    public void EveryTag_HasEnglishName()
    {
        TagNames names = TagNames.LoadFromResources();
        var missing = new SortedSet<string>();
        foreach (WordEntry word in Words().Words)
            foreach (string tag in word.tags ?? Array.Empty<string>())
                if (!names.HasEnglish(tag)) missing.Add(tag);
        Assert.IsEmpty(missing, "tag_names.json'da İngilizce adı (nameEn) olmayan etiketler");
        Assert.AreEqual("Fast", names.Get("fast", Language.English));
        Assert.AreEqual("Hızlı", names.Get("fast", Language.Turkish));
    }

    // ---- İngilizce yazarak oynama ----

    [Test]
    public void EnglishName_FindsTheSameWord()
    {
        WordDatabase db = Words();
        Assert.AreEqual("zombi", db.Lookup("zombie").Entry.id);
        Assert.AreEqual("gorunmezlik", db.Lookup("INVISIBILITY").Entry.id);
        Assert.AreEqual("buz", db.Lookup("Ice").Entry.id);

        foreach (WordEntry word in db.Words)
        {
            WordLookupResult result = db.Lookup(word.displayEn);
            Assert.AreEqual(WordLookupStatus.Found, result.Status, word.displayEn);
            Assert.AreEqual(word.id, result.Entry.id, "İngilizce ad başka kelimeyi buluyor: " + word.displayEn);
        }
    }

    [Test]
    public void SelectedLanguage_DoesNotChangeMatching()
    {
        Loc.SetLanguage(Language.English);
        WordDatabase db = Words();
        Assert.AreEqual("zombi", db.Lookup("Zombi").Entry.id);
        Assert.AreEqual("zombi", db.Lookup("Zombie").Entry.id);
    }

    [Test]
    public void Normalizer_EnglishCapitalI_Matches()
    {
        Assert.AreEqual("invisibility", WordNormalizer.ToMatchKey("INVISIBILITY"));
        Assert.AreEqual("ice", WordNormalizer.ToMatchKey("Ice"));
        Assert.AreEqual("ice", WordNormalizer.ToMatchKey("ICE"));
        Assert.AreEqual(WordNormalizer.ToMatchKey("Ice wall"), WordNormalizer.ToMatchKey("ICE WALL"));
    }

    [Test]
    public void SameMatch_TurkishThenEnglish_IsRejected()
    {
        var tracker = new MatchWordTracker(Words());
        Assert.AreEqual(WordLookupStatus.Found, tracker.Use("zombi").Status);
        Assert.AreEqual(WordLookupStatus.AlreadyUsed, tracker.Use("zombie").Status);
        Assert.AreEqual(WordLookupStatus.AlreadyUsed, tracker.Use("ZOMBIE").Status);

        Assert.AreEqual(WordLookupStatus.Found, tracker.Use("Invisibility").Status);
        Assert.AreEqual(WordLookupStatus.AlreadyUsed, tracker.Use("görünmezlik").Status);
    }

    [Test]
    public void EnglishNames_DoNotCollideWithOtherWords()
    {
        WordDatabase db = Words();
        Assert.IsEmpty(db.EnglishNameConflicts);

        // Ayrıca bağımsız kontrol: her İngilizce ad, başka hiçbir kelimenin id/ad/eş anlamlısı değil.
        var owner = new Dictionary<string, string>();
        foreach (WordEntry word in db.Words)
        {
            owner[WordNormalizer.ToMatchKey(word.id)] = word.id;
            owner[WordNormalizer.ToMatchKey(word.display)] = word.id;
            foreach (string alias in word.aliases ?? Array.Empty<string>()) owner[WordNormalizer.ToMatchKey(alias)] = word.id;
        }
        var conflicts = new List<string>();
        foreach (WordEntry word in db.Words)
            if (owner.TryGetValue(WordNormalizer.ToMatchKey(word.displayEn), out string other) && other != word.id)
                conflicts.Add($"{word.displayEn} ({word.id}) = {other}");
        Assert.IsEmpty(conflicts);
    }

    [Test]
    public void EnglishNameCollision_DoesNotBreakLoading()
    {
        const string json = "{\"words\":[" +
            "{\"id\":\"kedi\",\"display\":\"Kedi\",\"aliases\":[\"cat\"],\"displayEn\":\"Cat\"}," +
            "{\"id\":\"kaplan\",\"display\":\"Kaplan\",\"aliases\":[],\"displayEn\":\"Cat\"}]}";
        WordDatabase db = null;
        Assert.DoesNotThrow(() => db = WordDatabase.FromJson(json, MaxLength));
        Assert.AreEqual(1, db.EnglishNameConflicts.Count);
        Assert.AreEqual("kedi", db.Lookup("cat").Entry.id, "Çakışmada anahtar ilk sahibinde kalır");
    }

    // ---- Dil ayarı ----

    class MemoryStore : ISettingsStore
    {
        public string Text;
        public string Read() => Text;
        public bool Write(string text) { Text = text; return true; }
    }

    [Test]
    public void LanguageSetting_RoundTrip()
    {
        var store = new MemoryStore();
        Assert.IsTrue(SettingsStorage.Save(store, new GameSettings { language = Language.English }));
        Assert.AreEqual(Language.English, SettingsStorage.Load(store, Language.Turkish).language, "Kayıt varken sistem dili kullanılmaz");

        Assert.IsTrue(SettingsStorage.Save(store, new GameSettings { language = Language.Turkish }));
        Assert.AreEqual(Language.Turkish, SettingsStorage.Load(store, Language.English).language);
    }

    [Test]
    public void LanguageSetting_FirstRunUsesGivenLanguage_UnknownBecomesTurkish()
    {
        Assert.AreEqual(Language.English, SettingsStorage.Load(new MemoryStore(), Language.English).language);
        Assert.AreEqual(Language.Turkish, SettingsStorage.Load(new MemoryStore(), Language.Turkish).language);
        Assert.AreEqual(Language.Turkish, SettingsStorage.Load(new MemoryStore()).language);
        Assert.AreEqual(Language.Turkish, SettingsStorage.FromJson("{\"version\":1,\"language\":\"de\"}").language);
        Assert.AreEqual(Language.English, SettingsStorage.FromJson("{\"version\":1,\"language\":\"EN\"}").language);
    }
}
