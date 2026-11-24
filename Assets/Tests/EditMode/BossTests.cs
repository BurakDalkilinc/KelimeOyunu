using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Boss testleri: can, vuruş açısı/menzili, ödül, en yakını hedefleme ve Godzilla kelimesi.</summary>
public class BossTests
{
    GameConfig config;
    ArenaBounds arena;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(34f, 19f);
        arena = new ArenaBounds(config.arenaSize);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    CreatureModel Boss(int hp, Vector2 at, PlayerState target)
    {
        var word = new WordEntry
        {
            id = "godzilla", display = "Godzilla", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget,
            tags = new[] { "boss" }, stats = new WordStats { damage = 1, speed = 2f, duration = 40f, radius = 1.3f, hp = hp },
        };
        var model = new CreatureModel(word, target, at, 2f, 1, 0.4f, 40f, arena);
        TagRegistry.Apply(model, word.tags, new SpawnContext(0, 1, config));
        return model;
    }

    [Test]
    public void BossTag_GivesHealthFromStats_AndChasesNearest()
    {
        var target = new PlayerState(1, "a", 3);
        CreatureModel boss = Boss(12, Vector2.zero, target);

        Assert.IsTrue(boss.IsBoss);
        Assert.AreEqual(12, boss.BossHealth);
        Assert.AreEqual(12, boss.BossMaxHealth);
        Assert.IsTrue(boss.Persistent);
        Assert.IsTrue(boss.TargetsNearest);
        Assert.AreEqual(1.3f, boss.Radius, 0.001f, "gövde kelimenin yarıçapında");
        Assert.AreEqual(3f, CreatureSpriteScale.ForRadius(boss.Radius), "dev görünür (tam sayı kat)");
        Assert.AreEqual(1f, CreatureSpriteScale.ForRadius(0.4f));
        Assert.AreEqual("Godzilla 12/12", BossRules.Label(boss));
    }

    [Test]
    public void Hitting_InFrontAndInRange_Works_BehindOrFar_DoesNot()
    {
        var hitter = new PlayerState(1, "a", 3);
        CreatureModel boss = Boss(5, new Vector2(2.5f, 0f), hitter);
        var list = new List<CreatureModel> { boss };

        Assert.AreEqual(0, BossRules.HitBosses(list, hitter, Vector2.zero, Vector2.left, config, apply: true), "arkası dönük");
        Assert.AreEqual(5, boss.BossHealth);

        Assert.AreEqual(1, BossRules.HitBosses(list, hitter, Vector2.zero, Vector2.right, config, apply: false));
        Assert.AreEqual(5, boss.BossHealth, "apply false canı düşürmez");

        Assert.AreEqual(1, BossRules.HitBosses(list, hitter, Vector2.zero, Vector2.right, config, apply: true));
        Assert.AreEqual(4, boss.BossHealth);

        Assert.AreEqual(0, BossRules.HitBosses(list, hitter, new Vector2(-6f, 0f), Vector2.right, config, apply: true), "uzakta");
        Assert.AreEqual(1, BossRules.HitBosses(list, hitter, new Vector2(2.4f, 0f), Vector2.left, config, apply: true), "içindeyken her yöne vurur");
    }

    [Test]
    public void LastHitter_KillsBoss_AndGetsHealth_OrShieldIfFull()
    {
        var weak = new PlayerState(1, "a", 3);
        weak.TakeDamage(2);
        var full = new PlayerState(2, "b", 3);
        CreatureModel boss = Boss(2, new Vector2(1.5f, 0f), weak);
        var list = new List<CreatureModel> { boss };

        BossRules.HitBosses(list, full, Vector2.zero, Vector2.right, config, apply: true);
        Assert.IsFalse(boss.IsFinished);
        Assert.IsFalse(full.Status.HasShield);

        BossRules.HitBosses(list, weak, Vector2.zero, Vector2.right, config, apply: true);
        Assert.IsTrue(boss.IsFinished, "can bitince ölür");
        Assert.AreEqual(weak, boss.LastBossHitter);
        Assert.AreEqual(2, weak.Health.Current, "son vuran +1 can");

        CreatureModel second = Boss(1, new Vector2(1.5f, 0f), full);
        BossRules.HitBosses(new List<CreatureModel> { second }, full, Vector2.zero, Vector2.right, config, apply: true);
        Assert.AreEqual(3, full.Health.Current);
        Assert.IsTrue(full.Status.HasShield, "canı tamsa kalkan alır");
    }

    [Test]
    public void DeadBoss_CannotBeHitAgain()
    {
        var hitter = new PlayerState(1, "a", 3);
        CreatureModel boss = Boss(1, new Vector2(1.5f, 0f), hitter);
        var list = new List<CreatureModel> { boss };

        BossRules.HitBosses(list, hitter, Vector2.zero, Vector2.right, config, apply: true);
        Assert.AreEqual(0, BossRules.HitBosses(list, hitter, Vector2.zero, Vector2.right, config, apply: true));
    }

    [Test]
    public void ShoveCooldown_CustomSeconds_ForBossHits()
    {
        var cooldown = new ShoveCooldown(3f);
        Assert.IsTrue(cooldown.TryUse(0.5f));
        Assert.AreEqual(0.5f, cooldown.Remaining, 0.001f);
        cooldown.Tick(0.6f);
        Assert.IsTrue(cooldown.IsReady);
    }

    [Test]
    public void GodzillaWord_IsABoss()
    {
        WordEntry word = WordDatabase.LoadFromResources(50).Lookup("gojira").Entry;
        Assert.IsNotNull(word);
        Assert.AreEqual("godzilla", word.id);
        CollectionAssert.Contains(word.tags, "boss");
        Assert.Greater(word.stats.hp, 0);
    }
}
