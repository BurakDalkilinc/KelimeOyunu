using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Koleksiyon defteri testleri: keşfetme/görme kuralları, eş anlamlı → kimlik, "bu maçta yeni" sayımı, ipucu yazısı,
/// kayıt gidiş-dönüş ve bozuk kayıt, sekmeler, ilerleme, etiket adları ve sayfalama.
/// </summary>
public class CollectionTests
{
    /// <summary>Diske dokunmayan kayıt yeri: metni bellekte tutar.</summary>
    class MemoryStore : ICollectionStore
    {
        public string Text;
        public string Read() => Text;
        public bool Write(string text) { Text = text; return true; }
    }

    static WordEntry Word(string id, string display, string category = WordEntry.CategoryAttack, string scope = WordEntry.ScopeTarget, params string[] tags)
    {
        return new WordEntry { id = id, display = display, category = category, scope = scope, tags = tags, description = display + " açıklaması" };
    }

    static List<WordEntry> SampleWords()
    {
        return new List<WordEntry>
        {
            Word("zombi", "Zombi", WordEntry.CategoryAttack, WordEntry.ScopeTarget, "undead", "slow"),
            Word("kalkan", "Kalkan", WordEntry.CategoryDefense, WordEntry.ScopeSelf, "shield"),
            Word("atom_bombasi", "Atom bombası", WordEntry.CategoryThreat, WordEntry.ScopeGlobal, "explosive"),
            Word("meteor", "Meteor", WordEntry.CategoryAttack, WordEntry.ScopeGlobal, "sky"),
        };
    }

    static WordEntry Find(List<WordEntry> words, string id) => words.Find(w => w.id == id);

    // ---- Keşfetme / görme ----

    [Test]
    public void NewBook_EverythingIsLocked()
    {
        var book = new CollectionBook(SampleWords());
        Assert.AreEqual(CollectionStatus.Locked, book.GetStatus("zombi"));
        Assert.AreEqual(0, book.DiscoveredCount);
        Assert.AreEqual(4, book.TotalCount);
    }

    [Test]
    public void Written_IsDiscovered_AndDoesNotCountAsSeen()
    {
        var words = SampleWords();
        var book = new CollectionBook(words);
        book.RecordWritten(Find(words, "zombi"));

        Assert.AreEqual(CollectionStatus.Discovered, book.GetStatus("zombi"));
        Assert.AreEqual(1, book.TimesWritten("zombi"));
        Assert.AreEqual(0, book.TimesSeen("zombi"));
    }

    [Test]
    public void SeenByOthers_IsSeen()
    {
        var words = SampleWords();
        var book = new CollectionBook(words);
        book.RecordSeen(Find(words, "zombi"));
        book.RecordSeen(Find(words, "zombi"));

        Assert.AreEqual(CollectionStatus.Seen, book.GetStatus("zombi"));
        Assert.AreEqual(2, book.TimesSeen("zombi"));
        Assert.AreEqual(0, book.DiscoveredCount);
        Assert.AreEqual(1, book.SeenCount);
    }

    [Test]
    public void Discovered_BeatsSeen_InAnyOrder()
    {
        var words = SampleWords();
        var book = new CollectionBook(words);

        book.RecordSeen(Find(words, "zombi"));
        book.RecordWritten(Find(words, "zombi"));
        Assert.AreEqual(CollectionStatus.Discovered, book.GetStatus("zombi"));

        book.RecordWritten(Find(words, "kalkan"));
        book.RecordSeen(Find(words, "kalkan"));
        Assert.AreEqual(CollectionStatus.Discovered, book.GetStatus("kalkan"));
        Assert.AreEqual(1, book.TimesSeen("kalkan"));
    }

    [Test]
    public void NullWord_IsIgnored()
    {
        var book = new CollectionBook(SampleWords());
        Assert.IsFalse(book.RecordWritten(null));
        Assert.IsFalse(book.RecordSeen(null));
        Assert.AreEqual(0, book.NewThisMatchCount);
        Assert.AreEqual(0, book.ToSaveData().entries.Count);
    }

    [Test]
    public void Alias_CountsTowardsTheWordId()
    {
        const string json = "{\"version\":1,\"words\":[{\"id\":\"zombi\",\"display\":\"Zombi\",\"aliases\":[\"zombie\",\"yürüyen ölü\"],\"category\":\"attack\",\"scope\":\"target\",\"tags\":[\"undead\"]}]}";
        WordDatabase db = WordDatabase.FromJson(json, 50);
        var book = new CollectionBook(db.Words);

        book.RecordWritten(db.Lookup("zombie").Entry);
        book.RecordSeen(db.Lookup("Yürüyen Ölü").Entry);

        Assert.AreEqual(CollectionStatus.Discovered, book.GetStatus("zombi"));
        Assert.AreEqual(1, book.TimesWritten("zombi"));
        Assert.AreEqual(1, book.TimesSeen("zombi"));
        Assert.AreEqual(1, book.ToSaveData().entries.Count);
    }

    // ---- Bu maçta yeni ----

    [Test]
    public void NewThisMatch_CountsWordsLeavingLockedOnlyOnce()
    {
        var words = SampleWords();
        var book = new CollectionBook(words);

        Assert.IsTrue(book.RecordSeen(Find(words, "zombi")));     // kilitli → görüldü: yeni
        Assert.IsFalse(book.RecordWritten(Find(words, "zombi"))); // görüldü → keşfedildi: aynı kelime, yeniden sayılmaz
        Assert.IsTrue(book.RecordWritten(Find(words, "kalkan"))); // kilitli → keşfedildi: yeni
        Assert.IsFalse(book.RecordWritten(Find(words, "kalkan")));

        Assert.AreEqual(2, book.NewThisMatchCount);
    }

    [Test]
    public void NewThisMatch_ResetsOnBeginMatch_AndIgnoresAlreadyKnownWords()
    {
        var words = SampleWords();
        var saved = new CollectionSaveData();
        saved.entries.Add(new CollectionEntry { id = "zombi", timesSeen = 1 });
        var book = new CollectionBook(words, saved);

        book.RecordWritten(Find(words, "zombi")); // önceki maçta görülmüştü: yeni değil
        Assert.AreEqual(0, book.NewThisMatchCount);

        book.RecordSeen(Find(words, "meteor"));
        Assert.AreEqual(1, book.NewThisMatchCount);

        book.BeginMatch();
        Assert.AreEqual(0, book.NewThisMatchCount);
    }

    [Test]
    public void NewWordsNotice_HiddenWhenZero()
    {
        Assert.AreEqual(string.Empty, CollectionText.NewWordsNotice(0));
        Assert.AreEqual("Deftere 3 yeni kelime eklendi!", CollectionText.NewWordsNotice(3));
    }

    // ---- İpucu ----

    [Test]
    public void LetterHint_FirstLetterAndBlanks()
    {
        Assert.AreEqual("Z _ _ _ _", CollectionText.LetterHint(Word("zombi", "Zombi")));
        Assert.AreEqual(5, CollectionText.LetterCount(Word("zombi", "Zombi")));
    }

    [Test]
    public void LetterHint_SpacesAreGaps_AndNotCounted()
    {
        WordEntry word = Word("muz_kabugu", "Muz kabuğu");
        Assert.AreEqual("M _ _   _ _ _ _ _ _", CollectionText.LetterHint(word));
        Assert.AreEqual(9, CollectionText.LetterCount(word));
    }

    [Test]
    public void LetterHint_UsesTurkishUppercase()
    {
        Assert.AreEqual("İ _ _ _", CollectionText.LetterHint(Word("igne", "iğne")));
    }

    [Test]
    public void KindAndLength_UsesTabRules()
    {
        var words = SampleWords();
        Assert.AreEqual("Yaratık · 5 harf", CollectionText.KindAndLength(Find(words, "zombi")));
        Assert.AreEqual("Savunma · 6 harf", CollectionText.KindAndLength(Find(words, "kalkan")));
        Assert.AreEqual("Büyük tehdit · 11 harf", CollectionText.KindAndLength(Find(words, "atom_bombasi")));
        Assert.AreEqual("Büyük tehdit · 6 harf", CollectionText.KindAndLength(Find(words, "meteor")));
    }

    [Test]
    public void Counts_Text()
    {
        Assert.AreEqual("3 kez yazdın, 1 kez gördün", CollectionText.Counts(3, 1));
    }

    // ---- Kayıt ----

    [Test]
    public void Save_ThenLoad_KeepsCounters()
    {
        var words = SampleWords();
        var book = new CollectionBook(words);
        book.RecordWritten(Find(words, "zombi"));
        book.RecordWritten(Find(words, "zombi"));
        book.RecordSeen(Find(words, "kalkan"));

        var store = new MemoryStore();
        Assert.IsTrue(CollectionStorage.Save(store, book));
        StringAssert.Contains("\"version\": 1", store.Text);

        CollectionBook loaded = CollectionStorage.Load(store, words);
        Assert.AreEqual(CollectionStatus.Discovered, loaded.GetStatus("zombi"));
        Assert.AreEqual(2, loaded.TimesWritten("zombi"));
        Assert.AreEqual(CollectionStatus.Seen, loaded.GetStatus("kalkan"));
        Assert.AreEqual(1, loaded.TimesSeen("kalkan"));
        Assert.AreEqual(CollectionStatus.Locked, loaded.GetStatus("meteor"));
        Assert.AreEqual(0, loaded.NewThisMatchCount);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("bu json değil {{{")]
    [TestCase("{\"entries\": 5")]
    [TestCase("[1,2,3]")]
    public void CorruptOrMissingSave_StartsEmpty(string text)
    {
        CollectionBook book = CollectionStorage.Load(new MemoryStore { Text = text }, SampleWords());
        Assert.AreEqual(0, book.DiscoveredCount);
        Assert.AreEqual(0, book.SeenCount);
        Assert.AreEqual(4, book.TotalCount);
    }

    [Test]
    public void BadEntries_AreCleaned()
    {
        const string json = "{\"version\":1,\"entries\":[{\"id\":\"\",\"timesWritten\":3},{\"id\":\"zombi\",\"timesWritten\":-4,\"timesSeen\":2},{\"id\":\"zombi\",\"timesSeen\":1}]}";
        CollectionBook book = CollectionStorage.Load(new MemoryStore { Text = json }, SampleWords());

        Assert.AreEqual(CollectionStatus.Seen, book.GetStatus("zombi"));
        Assert.AreEqual(0, book.TimesWritten("zombi"));
        Assert.AreEqual(3, book.TimesSeen("zombi"));
    }

    [Test]
    public void NullStore_StartsEmpty_AndSaveFails()
    {
        CollectionBook book = CollectionStorage.Load(null, SampleWords());
        Assert.AreEqual(0, book.DiscoveredCount);
        Assert.IsFalse(CollectionStorage.Save(null, book));
    }

    // ---- Sekmeler ve ilerleme ----

    [Test]
    public void Tabs_FilterByCategoryAndScope()
    {
        var book = new CollectionBook(SampleWords());

        Assert.AreEqual(4, book.Filter(CollectionTab.All).Count);
        CollectionAssert.AreEqual(new[] { "zombi" }, book.Filter(CollectionTab.Creatures).ConvertAll(w => w.id));
        CollectionAssert.AreEqual(new[] { "kalkan" }, book.Filter(CollectionTab.Defense).ConvertAll(w => w.id));
        CollectionAssert.AreEqual(new[] { "atom_bombasi", "meteor" }, book.Filter(CollectionTab.Threats).ConvertAll(w => w.id));
    }

    [Test]
    public void Tabs_RealWords_EveryWordHasExactlyOneTab()
    {
        WordDatabase db = WordDatabase.LoadFromResources(50);
        var book = new CollectionBook(db.Words);
        int sum = book.Filter(CollectionTab.Creatures).Count + book.Filter(CollectionTab.Defense).Count + book.Filter(CollectionTab.Threats).Count;
        Assert.AreEqual(book.Filter(CollectionTab.All).Count, sum);
        Assert.AreEqual(db.Words.Count, book.TotalCount);
    }

    [Test]
    public void Progress_CountsOnlyDiscoveredKnownWords()
    {
        var words = SampleWords();
        var saved = new CollectionSaveData();
        saved.entries.Add(new CollectionEntry { id = "silinmis_kelime", timesWritten = 2 }); // artık words.json'da yok
        var book = new CollectionBook(words, saved);

        book.RecordWritten(Find(words, "zombi"));
        book.RecordSeen(Find(words, "kalkan"));

        Assert.AreEqual("1 / 4 keşfedildi", CollectionText.Progress(book));
        // Eski kayıt silinmez, dosyada kalır.
        Assert.IsTrue(book.ToSaveData().entries.Exists(e => e.id == "silinmis_kelime"));
    }

    // ---- Etiket adları ----

    [Test]
    public void TagNames_FromJson_WithFallback()
    {
        TagNames names = TagNames.FromJson("{\"version\":1,\"tags\":[{\"id\":\"fast\",\"name\":\"Hızlı\"},{\"id\":\"flying\",\"name\":\"Uçan\"}]}");
        Assert.AreEqual("Hızlı", names.Get("fast"));
        Assert.AreEqual("yeni_etiket", names.Get("yeni_etiket"));
        Assert.AreEqual("Uçan, Hızlı, swarm", names.Describe(Word("ari", "Arı", WordEntry.CategoryAttack, WordEntry.ScopeTarget, "flying", "fast", "swarm")));
    }

    [Test]
    public void TagNames_Corrupt_IsEmpty()
    {
        TagNames names = TagNames.FromJson("bozuk {");
        Assert.AreEqual("fast", names.Get("fast"));
    }

    [Test]
    public void TagNames_EveryTagInWordsJsonHasATurkishName()
    {
        TagNames names = TagNames.LoadFromResources();
        var missing = new List<string>();
        foreach (WordEntry word in WordDatabase.LoadFromResources(50).Words)
            if (word.tags != null)
                foreach (string tag in word.tags)
                    if (!names.Has(tag) && !missing.Contains(tag)) missing.Add(tag);

        Assert.IsEmpty(missing, "tag_names.json'da adı olmayan etiketler: " + string.Join(", ", missing));
    }

    // ---- Sayfalama ----

    [Test]
    public void Paging_CountsClampsAndSlices()
    {
        var items = new List<int>();
        for (int i = 0; i < 32; i++) items.Add(i);

        Assert.AreEqual(3, CollectionPaging.PageCount(32, 15));
        Assert.AreEqual(1, CollectionPaging.PageCount(0, 15));
        Assert.AreEqual(2, CollectionPaging.ClampPage(9, 32, 15));
        Assert.AreEqual(0, CollectionPaging.ClampPage(-1, 32, 15));
        CollectionAssert.AreEqual(new[] { 30, 31 }, CollectionPaging.Slice(items, 2, 15));
        Assert.AreEqual(15, CollectionPaging.Slice(items, 0, 15).Count);
        Assert.AreEqual("Sayfa 2 / 3", CollectionText.PageLabel(1, 3));
    }
}
