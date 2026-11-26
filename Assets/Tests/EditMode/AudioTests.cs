using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Ses sistemi testleri: etiket → ses kuralları (tehdit → siren, etiketsiz → varsayılan, bilinmeyen kelime), gönderme ve
/// maç sonu sesleri, aynı sesin üst üste binmemesi ve eşzaman sınırı, müzik seçimi, veri dosyası ile ses dosyalarının tutarlılığı.
/// </summary>
public class AudioTests
{
    const string RulesJson = @"{
        ""version"": 1, ""threatSound"": ""sfx_siren"", ""defaultSound"": ""sfx_spawn"", ""unknownSound"": ""sfx_unknown"",
        ""rules"": [
            { ""tag"": ""explosive"", ""sound"": ""sfx_explosion"" },
            { ""tag"": ""fire"", ""sound"": ""sfx_fire"" },
            { ""tag"": ""ignite"", ""sound"": ""sfx_fire"" },
            { ""tag"": ""water"", ""sound"": ""sfx_water"" },
            { ""tag"": ""shield"", ""sound"": ""sfx_shield"" },
            { ""tag"": ""heal"", ""sound"": ""sfx_heal"" },
            { ""tag"": ""flying"", ""sound"": ""sfx_fly"" },
            { ""tag"": ""sky"", ""sound"": ""sfx_fly"" }
        ] }";

    static WordEntry Word(string scope, params string[] tags)
    {
        return new WordEntry { id = "deneme", display = "Deneme", category = WordEntry.CategoryAttack, scope = scope, tags = tags };
    }

    static SfxRules Rules() => SfxRules.FromJson(RulesJson);

    // ---- Etiket → ses ----

    [TestCase("explosive", SoundId.Explosion)]
    [TestCase("fire", SoundId.Fire)]
    [TestCase("ignite", SoundId.Fire)]
    [TestCase("water", SoundId.Water)]
    [TestCase("shield", SoundId.Shield)]
    [TestCase("heal", SoundId.Heal)]
    [TestCase("flying", SoundId.Fly)]
    [TestCase("sky", SoundId.Fly)]
    public void TaggedWord_PlaysTagSound(string tag, string expected)
    {
        Assert.AreEqual(expected, Rules().ForWord(Word(WordEntry.ScopeTarget, "fast", tag)));
    }

    [Test]
    public void GlobalThreat_PlaysSiren_EvenWithOtherTags()
    {
        WordEntry threat = Word(WordEntry.ScopeGlobal, "fire", "flying");
        threat.category = WordEntry.CategoryThreat;
        Assert.AreEqual(SoundId.Siren, Rules().ForWord(threat));
    }

    [Test]
    public void WordWithoutMatchingTag_PlaysDefault()
    {
        Assert.AreEqual(SoundId.Spawn, Rules().ForWord(Word(WordEntry.ScopeTarget, "slow", "undead")));
        Assert.AreEqual(SoundId.Spawn, Rules().ForWord(Word(WordEntry.ScopeTarget)));
        Assert.AreEqual(SoundId.Spawn, Rules().ForWord(new WordEntry { id = "etiketsiz", scope = WordEntry.ScopeTarget }));
    }

    [Test]
    public void UnknownWord_PlaysUnknownSound()
    {
        Assert.AreEqual("sfx_unknown", Rules().ForWord(null));
    }

    [Test]
    public void FirstMatchingRule_Wins()
    {
        // explosive kuralı fire'dan önce yazılı
        Assert.AreEqual(SoundId.Explosion, Rules().ForWord(Word(WordEntry.ScopeTarget, "fire", "explosive")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("bozuk { json")]
    [TestCase("{}")]
    public void BrokenOrEmptyRules_FallBackToDefaults(string json)
    {
        SfxRules rules = SfxRules.FromJson(json);
        Assert.AreEqual(SoundId.Spawn, rules.ForWord(Word(WordEntry.ScopeTarget, "explosive")));
        Assert.AreEqual(SoundId.Siren, rules.ForWord(Word(WordEntry.ScopeGlobal)));
        Assert.AreEqual(SoundId.Spawn, rules.ForWord(null));
    }

    // ---- Gönderme ve maç sonu ----

    [TestCase(SubmitStatus.Accepted, SoundId.WordOk)]
    [TestCase(SubmitStatus.AcceptedUnknown, SoundId.WordOk)]
    [TestCase(SubmitStatus.Empty, SoundId.WordBad)]
    [TestCase(SubmitStatus.TooLong, SoundId.WordBad)]
    [TestCase(SubmitStatus.Blocked, SoundId.WordBad)]
    [TestCase(SubmitStatus.AlreadyUsed, SoundId.WordBad)]
    [TestCase(SubmitStatus.WrongPhase, null)]
    [TestCase(SubmitStatus.NotWriter, null)]
    public void SubmitStatus_PicksSound(SubmitStatus status, string expected)
    {
        Assert.AreEqual(expected, SfxRules.ForSubmit(status));
    }

    [Test]
    public void MatchEnd_VictoryForLocalWinner_DefeatOtherwise()
    {
        var me = new PlayerState(1, "Ben", 3);
        var other = new PlayerState(2, "Bot", 3, true);
        Assert.AreEqual(SoundId.Victory, SfxRules.ForMatchEnd(me, me));
        Assert.AreEqual(SoundId.Victory, SfxRules.ForMatchEnd(new PlayerState(1, "Ben (yeniden kuruldu)", 3), me)); // kimlikle
        Assert.AreEqual(SoundId.Defeat, SfxRules.ForMatchEnd(other, me));
        Assert.AreEqual(SoundId.Defeat, SfxRules.ForMatchEnd(null, me));
        Assert.AreEqual(SoundId.Victory, SfxRules.ForMatchEnd(other, null)); // izleyici
    }

    // ---- Üst üste binme ve eşzaman sınırı ----

    [Test]
    public void Gate_SameSoundWithinCooldown_IsSkipped()
    {
        var gate = new SfxGate(8);
        Assert.IsTrue(gate.TryPlay(SoundId.Hit, 0f, 0.1f, 0));
        Assert.IsFalse(gate.TryPlay(SoundId.Hit, 0f, 0.1f, 1), "aynı karede ikinci kez");
        Assert.IsFalse(gate.TryPlay(SoundId.Hit, 0.05f, 0.1f, 1));
        Assert.IsTrue(gate.TryPlay(SoundId.Hit, 0.11f, 0.1f, 1));
    }

    [Test]
    public void Gate_DifferentSounds_DoNotBlockEachOther()
    {
        var gate = new SfxGate(8);
        Assert.IsTrue(gate.TryPlay(SoundId.Hit, 0f, 0.1f, 0));
        Assert.IsTrue(gate.TryPlay(SoundId.Explosion, 0f, 0.1f, 1));
    }

    [Test]
    public void Gate_RespectsMaxSimultaneous()
    {
        var gate = new SfxGate(2);
        Assert.IsTrue(gate.TryPlay("a", 0f, 0f, 1));
        Assert.IsFalse(gate.TryPlay("b", 0f, 0f, 2), "havuz dolu");
        Assert.IsTrue(gate.TryPlay("b", 0f, 0f, 1));
    }

    [Test]
    public void Gate_SkippedPlay_DoesNotStartCooldown()
    {
        var gate = new SfxGate(1);
        Assert.IsFalse(gate.TryPlay("a", 0f, 1f, 1)); // havuz doluydu, çalmadı
        Assert.IsTrue(gate.TryPlay("a", 0.01f, 1f, 0));
        Assert.IsFalse(gate.TryPlay(null, 0f, 0f, 0));
    }

    // ---- Müzik seçimi ----

    [Test]
    public void Music_OutsideMatch_IsMenu()
    {
        Assert.AreEqual(MusicTrack.Menu, MusicDirector.Choose(false, TurnPhase.Typing, null));
        Assert.AreEqual(MusicTrack.Menu, MusicDirector.Choose(true, TurnPhase.NotStarted, null));
        Assert.AreEqual(MusicTrack.Menu, MusicDirector.Choose(true, TurnPhase.MatchOver, null));
    }

    [Test]
    public void Music_DuringTurns_IsMatch()
    {
        Assert.AreEqual(MusicTrack.Match, MusicDirector.Choose(true, TurnPhase.Dice, null));
        Assert.AreEqual(MusicTrack.Match, MusicDirector.Choose(true, TurnPhase.Typing, null));
        Assert.AreEqual(MusicTrack.Match, MusicDirector.Choose(true, TurnPhase.Result, Word(WordEntry.ScopeTarget, "fast")));
        Assert.AreEqual(MusicTrack.Match, MusicDirector.Choose(true, TurnPhase.Result, null), "bilinmeyen kelime");
    }

    [Test]
    public void Music_GlobalThreatResult_IsThreat()
    {
        Assert.AreEqual(MusicTrack.Threat, MusicDirector.Choose(true, TurnPhase.Result, Word(WordEntry.ScopeGlobal)));
        // Tehdit kelimesi yazılırken değil, ortaya çıkınca gerilim başlar
        Assert.AreEqual(MusicTrack.Match, MusicDirector.Choose(true, TurnPhase.Typing, Word(WordEntry.ScopeGlobal)));
    }

    [Test]
    public void Music_ClipNames()
    {
        Assert.AreEqual(SoundId.MusicMenu, MusicDirector.ClipName(MusicTrack.Menu));
        Assert.AreEqual(SoundId.MusicMatch, MusicDirector.ClipName(MusicTrack.Match));
        Assert.AreEqual(SoundId.MusicThreat, MusicDirector.ClipName(MusicTrack.Threat));
        Assert.IsNull(MusicDirector.ClipName(MusicTrack.None));
    }

    // ---- Veri ve ses dosyaları ----

    /// <summary>GameConfig'teki varsayılan ses klasörü ve eşleme dosyası yolu (asset'e dokunmadan).</summary>
    static void ConfigPaths(out string folder, out string tagsPath)
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        folder = config.audioFolder;
        tagsPath = config.audioSfxTagsPath;
        Object.DestroyImmediate(config);
    }

    static SfxRules ProjectRules()
    {
        ConfigPaths(out _, out string tagsPath);
        return SfxRules.LoadFromResources(tagsPath);
    }

    [Test]
    public void ProjectRules_FileExistsAndHasRules()
    {
        ConfigPaths(out _, out string tagsPath);
        Assert.IsNotNull(Resources.Load<TextAsset>(tagsPath), "sfx_tags.json yok");
        Assert.Greater(ProjectRules().Rules.Count, 0);
    }

    [Test]
    public void ProjectRules_EveryTagExistsInWords()
    {
        WordDatabase words = WordDatabase.LoadFromResources(50);
        var known = new HashSet<string>();
        foreach (WordEntry word in words.Words)
            if (word.tags != null) known.UnionWith(word.tags);

        foreach (SfxTagRule rule in ProjectRules().Rules)
            Assert.IsTrue(known.Contains(rule.tag), $"sfx_tags.json'daki '{rule.tag}' etiketi words.json'da hiçbir kelimede yok");
    }

    [Test]
    public void ProjectRules_EverySoundHasClip()
    {
        SfxRules rules = ProjectRules();
        var names = new List<string> { rules.ThreatSound, rules.DefaultSound, rules.UnknownSound };
        foreach (SfxTagRule rule in rules.Rules) names.Add(rule.sound);

        ConfigPaths(out string folder, out _);
        foreach (string name in names)
            Assert.IsNotNull(Resources.Load<AudioClip>(folder + "/" + name), $"Resources/{folder}/{name} ses dosyası yok");
    }

    [Test]
    public void EveryNamedSound_HasClip()
    {
        ConfigPaths(out string folder, out _);
        var library = new SoundLibrary(folder);
        foreach (string name in SoundId.AllEffects) Assert.IsNotNull(library.Get(name), name + " yok");
        foreach (string name in SoundId.AllMusic) Assert.IsNotNull(library.Get(name), name + " yok");
    }

    [Test]
    public void Library_MissingSound_IsNullNotError()
    {
        var library = new SoundLibrary("Audio");
        Assert.IsNull(library.Get("boyle_bir_ses_yok"));
        Assert.IsNull(library.Get(null));
        Assert.IsNull(library.Get(""));
    }
}
