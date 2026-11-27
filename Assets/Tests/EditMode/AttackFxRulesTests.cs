using NUnit.Framework;
using UnityEngine;

/// <summary>AttackFxRules testleri: etiket → vuruş izi, öncelik, varsayılan ve dev izi; veri dosyası etiketleri ve resimleri.</summary>
public class AttackFxRulesTests
{
    const int FramesPerEffect = 4;

    static AttackFxRules FromFile() => AttackFxRules.LoadFromResources(GameConfigLoader.Get().attackFxPath);

    static WordEntry Word(params string[] tags) => new WordEntry { id = "test", tags = tags };

    [Test]
    public void FirstMatchingTag_Wins_InPriorityOrder()
    {
        AttackFxRules rules = AttackFxRules.FromJson(
            "{\"defaultEffect\":\"fx_hit\",\"rules\":[{\"tag\":\"boss\",\"effect\":\"fx_stomp\"},{\"tag\":\"predator\",\"effect\":\"fx_bite\"}]}");

        Assert.AreEqual("fx_bite", rules.ForCreature(Word("fast", "predator"), false));
        Assert.AreEqual("fx_stomp", rules.ForCreature(Word("predator", "boss"), true)); // Sıra kuraldan gelir, etiket sırasından değil
        Assert.AreEqual("fx_hit", rules.ForCreature(Word("slow"), false));
    }

    [Test]
    public void NoMatch_GiantGetsGiantEffect_UnknownWordGetsDefault()
    {
        AttackFxRules rules = AttackFxRules.FromJson("{\"defaultEffect\":\"fx_hit\",\"giantEffect\":\"fx_stomp\",\"rules\":[]}");

        Assert.AreEqual("fx_stomp", rules.ForCreature(Word("slow"), true));
        Assert.AreEqual("fx_hit", rules.ForCreature(null, false));
        Assert.AreEqual("fx_stomp", rules.ForCreature(null, true));
    }

    [Test]
    public void EmptyOrBrokenJson_FallsBackToHit()
    {
        Assert.AreEqual(AttackFxRules.FallbackEffect, AttackFxRules.FromJson(null).ForCreature(Word("predator"), true));
        Assert.AreEqual(AttackFxRules.FallbackEffect, AttackFxRules.FromJson("{bozuk").ForCreature(Word("predator"), false));
        Assert.AreEqual(AttackFxRules.FallbackEffect, AttackFxRules.LoadFromResources("Data/yok_boyle_dosya").DefaultEffect);
    }

    [Test]
    public void DataFile_MapsExpectedTags()
    {
        AttackFxRules rules = FromFile();
        Assert.Greater(rules.Rules.Count, 0);

        Assert.AreEqual("fx_stomp", rules.ForCreature(Word("boss", "knockback", "armored"), true));
        Assert.AreEqual("fx_slash", rules.ForCreature(Word("weapon", "melee"), false));
        Assert.AreEqual("fx_bite", rules.ForCreature(Word("fast", "predator", "knockback"), false)); // Avcı ısırır
        Assert.AreEqual("fx_claw", rules.ForCreature(Word("undead", "slow"), false));
        Assert.AreEqual("fx_punch", rules.ForCreature(Word("fast", "knockback"), false));
        Assert.AreEqual("fx_hit", rules.ForCreature(Word("flying", "fast", "swarm"), false));
        Assert.AreEqual("fx_hit", rules.DefaultEffect);
    }

    [Test]
    public void DataFile_EveryTag_ExistsInWordDatabase()
    {
        var asset = Resources.Load<TextAsset>("Data/words");
        Assert.IsNotNull(asset);
        var db = JsonUtility.FromJson<WordFile>(asset.text);

        foreach (AttackFxRule rule in FromFile().Rules)
        {
            bool used = false;
            foreach (WordEntry word in db.words)
                if (word.HasTag(rule.tag)) { used = true; break; }
            Assert.IsTrue(used, "attack_fx.json etiketi words.json'da yok: " + rule.tag);
        }
    }

    /// <summary>Resim ajanı fx_*_0..3 karelerini üretene kadar bu test kırılabilir.</summary>
    [Test]
    public void DataFile_EveryEffect_HasFourFramesInSprites()
    {
        AttackFxRules rules = FromFile();
        var effects = new System.Collections.Generic.HashSet<string> { rules.DefaultEffect, rules.GiantEffect };
        foreach (AttackFxRule rule in rules.Rules) effects.Add(rule.effect);

        foreach (string effect in effects)
            for (int i = 0; i < FramesPerEffect; i++)
                Assert.IsNotNull(SpriteLibrary.Get(effect + "_" + i), "Eksik iz karesi: " + effect + "_" + i);
    }

    [Test]
    public void MarkEffect_MissingFrames_SpawnsNothing()
    {
        Assert.AreEqual(0, AttackMarkEffect.FramesFor("fx_yok_boyle").Length);
        Assert.IsNull(AttackMarkEffect.Spawn("fx_yok_boyle", Vector2.zero, 1f, false, 0.05f));
        Assert.IsNull(AttackMarkEffect.Spawn(null, Vector2.zero, 1f, false, 0.05f));
    }
}
