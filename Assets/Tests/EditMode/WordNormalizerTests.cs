using NUnit.Framework;

/// <summary>WordNormalizer testleri: Türkçe I/ı/İ/i, boşluk, işaret ve aksansız eşleşme.</summary>
public class WordNormalizerTests
{
    [TestCase("IŞIK", "ışık")]
    [TestCase("İSTANBUL", "istanbul")]
    [TestCase("Iğdır", "ığdır")]
    [TestCase("ZOMBİ", "zombi")]
    [TestCase("KIRMIZI", "kırmızı")]
    public void Normalize_UsesTurkishLowercase(string input, string expected)
    {
        Assert.AreEqual(expected, WordNormalizer.Normalize(input));
    }

    [Test]
    public void Normalize_KeepsDottedAndDotlessIDistinct()
    {
        Assert.AreNotEqual(WordNormalizer.Normalize("ışık"), WordNormalizer.Normalize("isik"));
    }

    [Test]
    public void Normalize_TrimsAndCollapsesSpaces()
    {
        Assert.AreEqual("el bombası", WordNormalizer.Normalize("   el    bombası \t"));
    }

    [TestCase("meteor_yagmuru", "meteor yagmuru")]
    [TestCase("muz-kabuğu", "muz kabuğu")]
    public void Normalize_TreatsUnderscoreAndDashAsSpace(string input, string expected)
    {
        Assert.AreEqual(expected, WordNormalizer.Normalize(input));
    }

    [TestCase("zombi!!!", "zombi")]
    [TestCase("zombi.", "zombi")]
    [TestCase("(kurt)", "kurt")]
    public void Normalize_DropsPunctuation(string input, string expected)
    {
        Assert.AreEqual(expected, WordNormalizer.Normalize(input));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("     ")]
    [TestCase("\t\n")]
    [TestCase("😀")]
    [TestCase("!?*")]
    public void Normalize_EmptyLikeInput_ReturnsEmpty(string input)
    {
        Assert.AreEqual(string.Empty, WordNormalizer.Normalize(input));
        Assert.AreEqual(string.Empty, WordNormalizer.ToMatchKey(input));
    }

    [Test]
    public void Normalize_EmojiInsideWord_IsDroppedWordRemains()
    {
        Assert.AreEqual("zombi", WordNormalizer.Normalize("zom😀bi"));
    }

    [TestCase("ÖLÜ", "olu")]
    [TestCase("ölü", "olu")]
    [TestCase("Yürüyen Ölü", "yuruyen olu")]
    [TestCase("IŞIK", "isik")]
    [TestCase("ışık", "isik")]
    [TestCase("İSTANBUL", "istanbul")]
    [TestCase("Görünmezlik", "gorunmezlik")]
    [TestCase("Kaplumbağa", "kaplumbaga")]
    [TestCase("Mıknatıs", "miknatis")]
    public void ToMatchKey_FoldsTurkishLetters(string input, string expected)
    {
        Assert.AreEqual(expected, WordNormalizer.ToMatchKey(input));
    }

    [Test]
    public void ToMatchKey_AccentedAndPlainSpellingsMatch()
    {
        Assert.AreEqual(WordNormalizer.ToMatchKey("ŞEKER"), WordNormalizer.ToMatchKey("seker"));
        Assert.AreEqual(WordNormalizer.ToMatchKey("ÇİÇEK"), WordNormalizer.ToMatchKey("cicek"));
    }
}
