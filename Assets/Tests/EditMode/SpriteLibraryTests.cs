using NUnit.Framework;
using UnityEngine;

/// <summary>SpriteLibrary testleri: piksel sanat dosyaları bulunuyor mu, kelime için doğru sprite seçiliyor mu.</summary>
public class SpriteLibraryTests
{
    [Test]
    public void Get_UnknownName_ReturnsNull_AndEmptyNameToo()
    {
        Assert.IsNull(SpriteLibrary.Get("yok_boyle_bir_sprite"));
        Assert.IsNull(SpriteLibrary.Get(""));
        Assert.IsNull(SpriteLibrary.Get(null));
    }

    [Test]
    public void Char_HasTwoFrames_At16PixelsPerUnit()
    {
        Sprite[] frames = SpriteLibrary.GetFrames("char");

        Assert.AreEqual(2, frames.Length);
        Assert.AreEqual(16f, frames[0].pixelsPerUnit);
        Assert.AreEqual(FilterMode.Point, frames[0].texture.filterMode);
        Assert.AreEqual(1f, frames[0].bounds.size.x, 0.001f); // 16 piksel = 1 birim = oyuncu çapı
    }

    [Test]
    public void GetFrames_MissingName_IsEmpty()
    {
        Assert.AreEqual(0, SpriteLibrary.GetFrames("yok_boyle_bir_sprite").Length);
    }

    [Test]
    public void NameFor_UsesWordId_ThenSpriteField_ThenUnknown()
    {
        Assert.AreEqual("kurt", SpriteLibrary.NameFor(new WordEntry { id = "kurt" }));
        Assert.AreEqual("zombi", SpriteLibrary.NameFor(new WordEntry { id = "zombi_suru", sprite = "zombi" }));
        Assert.AreEqual("unknown", SpriteLibrary.NameFor(new WordEntry { id = "olmayan_kelime" }));
        Assert.AreEqual("unknown", SpriteLibrary.NameFor(null));
    }

    [Test]
    public void EveryWordInTheDatabase_ResolvesToAnExistingSprite()
    {
        var asset = Resources.Load<TextAsset>("Data/words");
        Assert.IsNotNull(asset);
        var db = JsonUtility.FromJson<WordFile>(asset.text);

        foreach (WordEntry word in db.words)
            Assert.IsNotNull(SpriteLibrary.Get(SpriteLibrary.NameFor(word)), word.id);
    }

    [System.Serializable]
    class WordFile
    {
        public WordEntry[] words;
    }
}
