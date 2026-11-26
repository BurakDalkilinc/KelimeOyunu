using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Ayar testleri: kayıt gidiş-dönüş, bozuk/boş kayıt → varsayılan, ses düzeyi sınırları (0..1), dil varsayılanı,
/// çözünürlük listesi (tekrarsız, sıralı, en yakın, uçlarda durma).
/// </summary>
public class SettingsTests
{
    /// <summary>Diske dokunmayan kayıt yeri: metni bellekte tutar.</summary>
    class MemoryStore : ISettingsStore
    {
        public string Text;
        public string Read() => Text;
        public bool Write(string text) { Text = text; return true; }
    }

    [Test]
    public void SaveAndLoad_RoundTrip()
    {
        var store = new MemoryStore();
        var settings = new GameSettings
        {
            masterVolume = 0.25f, musicVolume = 0f, sfxVolume = 1f, fullscreen = false,
            resolutionWidth = 1280, resolutionHeight = 720, vSync = false, showNameTags = false, language = "tr"
        };

        Assert.IsTrue(SettingsStorage.Save(store, settings));
        GameSettings loaded = SettingsStorage.Load(store);

        Assert.AreEqual(0.25f, loaded.masterVolume, 1e-5f);
        Assert.AreEqual(0f, loaded.musicVolume, 1e-5f);
        Assert.AreEqual(1f, loaded.sfxVolume, 1e-5f);
        Assert.IsFalse(loaded.fullscreen);
        Assert.AreEqual(1280, loaded.resolutionWidth);
        Assert.AreEqual(720, loaded.resolutionHeight);
        Assert.IsFalse(loaded.vSync);
        Assert.IsFalse(loaded.showNameTags);
        Assert.AreEqual("tr", loaded.language);
        Assert.AreEqual(GameSettings.CurrentVersion, loaded.version);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("bozuk { json")]
    [TestCase("{}")]
    [TestCase("[1, 2, 3]")]
    [TestCase("{\"masterVolume\": 0.1}")] // sürüm alanı yok: bizim dosyamız değil
    public void BrokenOrMissing_GivesDefaults(string json)
    {
        GameSettings loaded = SettingsStorage.FromJson(json);
        var defaults = new GameSettings();
        Assert.AreEqual(defaults.masterVolume, loaded.masterVolume);
        Assert.AreEqual(defaults.musicVolume, loaded.musicVolume);
        Assert.AreEqual(defaults.sfxVolume, loaded.sfxVolume);
        Assert.AreEqual(defaults.fullscreen, loaded.fullscreen);
        Assert.AreEqual(defaults.showNameTags, loaded.showNameTags);
        Assert.AreEqual(GameSettings.DefaultLanguage, loaded.language);
    }

    [Test]
    public void StoreThatThrows_GivesDefaults()
    {
        GameSettings loaded = SettingsStorage.Load(new ThrowingStore());
        Assert.AreEqual(new GameSettings().masterVolume, loaded.masterVolume);
        Assert.IsFalse(SettingsStorage.Save(new ThrowingStore(), loaded));
        Assert.IsFalse(SettingsStorage.Save(null, loaded));
    }

    class ThrowingStore : ISettingsStore
    {
        public string Read() => throw new System.IO.IOException("disk yok");
        public bool Write(string text) => throw new System.IO.IOException("disk yok");
    }

    [Test]
    public void Volumes_AreClampedOnLoad()
    {
        string json = "{\"version\":1,\"masterVolume\":3.5,\"musicVolume\":-2,\"sfxVolume\":0.5,\"language\":\"\"}";
        GameSettings loaded = SettingsStorage.FromJson(json);
        Assert.AreEqual(1f, loaded.masterVolume);
        Assert.AreEqual(0f, loaded.musicVolume);
        Assert.AreEqual(0.5f, loaded.sfxVolume, 1e-5f);
        Assert.AreEqual(GameSettings.DefaultLanguage, loaded.language);
    }

    [Test]
    public void ClampVolume_Bounds()
    {
        Assert.AreEqual(0f, GameSettings.ClampVolume(-0.1f));
        Assert.AreEqual(1f, GameSettings.ClampVolume(1.1f));
        Assert.AreEqual(0.4f, GameSettings.ClampVolume(0.4f), 1e-6f);
        Assert.AreEqual(0f, GameSettings.ClampVolume(float.NaN));
    }

    [Test]
    public void SavedJson_IsClamped()
    {
        var store = new MemoryStore();
        SettingsStorage.Save(store, new GameSettings { masterVolume = 9f, resolutionWidth = -5, resolutionHeight = 600 });
        GameSettings loaded = SettingsStorage.Load(store);
        Assert.AreEqual(1f, loaded.masterVolume);
        Assert.AreEqual(0, loaded.resolutionWidth, "yarım çözünürlük → ekranın kendisi");
        Assert.AreEqual(0, loaded.resolutionHeight);
    }

    [Test]
    public void Clone_IsIndependent()
    {
        var original = new GameSettings { musicVolume = 0.3f };
        GameSettings copy = original.Clone();
        copy.musicVolume = 0.9f;
        Assert.AreEqual(0.3f, original.musicVolume, 1e-6f);
    }

    // ---- Çözünürlük listesi ----

    static ResolutionList SampleList()
    {
        return new ResolutionList(new[]
        {
            new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), // tekrar (farklı Hz)
            new Vector2Int(2560, 1440), new Vector2Int(0, 0)
        });
    }

    [Test]
    public void Resolutions_AreDistinctAndSorted()
    {
        ResolutionList list = SampleList();
        Assert.AreEqual(3, list.Sizes.Count);
        Assert.AreEqual(new Vector2Int(1280, 720), list.Sizes[0]);
        Assert.AreEqual(new Vector2Int(2560, 1440), list.Sizes[2]);
    }

    [Test]
    public void Resolutions_IndexOf_ExactNearestAndNative()
    {
        ResolutionList list = SampleList();
        Assert.AreEqual(1, list.IndexOf(1920, 1080, Vector2Int.zero));
        Assert.AreEqual(0, list.IndexOf(1366, 768, Vector2Int.zero), "en yakın alan");
        Assert.AreEqual(2, list.IndexOf(0, 0, new Vector2Int(2560, 1440)), "0 → ekranın kendisi");
        Assert.AreEqual(-1, new ResolutionList(null).IndexOf(1920, 1080, Vector2Int.zero));
    }

    [Test]
    public void Resolutions_StepStopsAtEnds()
    {
        ResolutionList list = SampleList();
        Assert.AreEqual(0, list.Step(0, -1));
        Assert.AreEqual(1, list.Step(0, 1));
        Assert.AreEqual(2, list.Step(2, 1));
        Assert.AreEqual(-1, new ResolutionList(null).Step(0, 1));
    }

    [Test]
    public void SettingsText_Formats()
    {
        Assert.AreEqual("%80", SettingsText.Percent(0.8f));
        Assert.AreEqual("%0", SettingsText.Percent(0f));
        Assert.AreEqual("1920 × 1080", SettingsText.Size(1920, 1080));
        Assert.AreEqual(SettingsText.NativeResolution, SettingsText.Size(0, 0));
        Assert.AreEqual(SettingsText.On, SettingsText.OnOff(true));
    }
}
