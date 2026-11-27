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

    [Test]
    public void AttackFrame_ExistsForCreatures_AndIsMissingForPlayer()
    {
        Sprite attack = SpriteLibrary.GetAttackFrame("aslan");

        Assert.IsNotNull(attack);
        Assert.AreEqual("aslan" + SpriteLibrary.AttackSuffix, attack.name);
        Assert.AreEqual(SpriteLibrary.Get("aslan").rect.size, attack.rect.size); // Aynı tuval: oyunda boyu değişmez
        Assert.IsNull(SpriteLibrary.GetAttackFrame("char"));
        Assert.IsNull(SpriteLibrary.GetAttackFrame(null));
    }

    [Test]
    public void EveryAnimatedCreature_HasAttackFrame_WithSameVoxelSliceCount()
    {
        var asset = Resources.Load<TextAsset>("Data/words");
        var db = JsonUtility.FromJson<WordFile>(asset.text);

        foreach (WordEntry word in db.words)
        {
            string name = SpriteLibrary.NameFor(word);
            Sprite[] walk = StackLibrary.Get(name + "_b");
            if (walk == null) continue; // Hareketli voxel yaratık değil (araç, simge, alan)

            Assert.IsNotNull(SpriteLibrary.GetAttackFrame(name), word.id);
            Sprite[] attack = StackLibrary.Get(name + SpriteLibrary.AttackSuffix);
            Assert.IsNotNull(attack, word.id);
            Assert.AreEqual(StackLibrary.Get(name).Length, attack.Length, word.id); // Dilimler birebir değişebilsin
        }
    }

    [Test]
    public void SpriteAnimator_Hold_ShowsGivenFrameImmediately()
    {
        var go = new GameObject("test");
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            var animator = go.AddComponent<SpriteAnimator>();
            animator.SetFrames(SpriteLibrary.GetFrames("aslan"));
            Assert.AreEqual("aslan", animator.BaseFrame.name);

            animator.Hold(SpriteLibrary.GetAttackFrame("aslan"), 0.3f);
            Assert.AreEqual("aslan" + SpriteLibrary.AttackSuffix, renderer.sprite.name);

            animator.Hold(null, 0.3f); // Saldırı karesi yoksa hiçbir şey değişmez
            Assert.AreEqual("aslan" + SpriteLibrary.AttackSuffix, renderer.sprite.name);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [System.Serializable]
    class WordFile
    {
        public WordEntry[] words;
    }
}
