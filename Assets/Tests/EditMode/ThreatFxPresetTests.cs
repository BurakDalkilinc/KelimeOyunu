using NUnit.Framework;

public class ThreatFxPresetTests
{
    [Test]
    public void EveryFxInWordsFile_IsAKnownTheme()
    {
        WordDatabase db = WordDatabase.LoadFromResources(50);
        foreach (WordEntry word in db.Words)
        {
            if (string.IsNullOrEmpty(word.fx)) continue;
            Assert.IsNotNull(ThreatFxPreset.For(word.fx), word.id + " bilinmeyen tema: " + word.fx);
        }
    }

    [Test]
    public void UnknownTheme_ReturnsNull()
    {
        Assert.IsNull(ThreatFxPreset.For("yok_boyle_tema"));
        Assert.IsNull(ThreatFxPreset.For(null));
    }
}
