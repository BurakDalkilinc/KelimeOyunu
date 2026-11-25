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
    public void Bot_PicksNearestBoss_OnlyWhenHealthyUnarmedAndInRange()
    {
        var bot = new PlayerState(1, "bot", 3);
        CreatureModel far = Boss(5, new Vector2(10f, 0f), bot);
        CreatureModel near = Boss(5, new Vector2(4f, 0f), bot);
        var list = new List<CreatureModel> { far, near };

        Assert.AreSame(near, BossRules.PickBossToAttack(list, bot, Vector2.zero, config));

        config.botBossAggroRange = 1f;
        Assert.IsNull(BossRules.PickBossToAttack(list, bot, Vector2.zero, config), "menzil dışı");
        config.botBossAggroRange = 14f;

        bot.TakeDamage(2);
        Assert.IsNull(BossRules.PickBossToAttack(list, bot, Vector2.zero, config), "canı az: kaçar");

        var armed = new PlayerState(2, "b", 3);
        armed.Status.ArmWeapon(new WordEntry { id = "silah", display = "Silah", stats = new WordStats() }, 1, 10f);
        Assert.IsNull(BossRules.PickBossToAttack(list, armed, Vector2.zero, config), "silahlı bot boss'a vurmaz");
    }

    [Test]
    public void BotBrain_BossAttack_ApproachesHoldsAndBacksOff_FacingBoss()
    {
        var brain = new BotBrain(config, arena, new System.Random(1));
        Vector2 bossAt = new Vector2(6f, 0f);
        var dangers = new List<DangerZone> { new DangerZone(bossAt, 1.3f) };
        float stand = 1.3f + config.playerRadius + config.botBossStandoff;

        Vector2 move = brain.ThinkBossAttack(Vector2.zero, dangers, bossAt, 1.3f, out Vector2 face);
        Assert.Greater(move.x, 0.9f, "uzaktan boss'a yaklaşır (kendi tehlike alanından kaçmaz)");
        Assert.Greater(face.x, 0.99f);

        Vector2 holdAt = bossAt - new Vector2(stand, 0f);
        Assert.AreEqual(Vector2.zero, brain.ThinkBossAttack(holdAt, dangers, bossAt, 1.3f, out face), "menzilde durur");
        Assert.Greater(face.x, 0.99f, "yüzü boss'a dönük");
        Assert.LessOrEqual(stand - 1.3f, config.shoveRange, "durduğu yerden vurabilir");

        Vector2 tooClose = bossAt - new Vector2(1.4f, 0f);
        Assert.Less(brain.ThinkBossAttack(tooClose, dangers, bossAt, 1.3f, out face).x, -0.9f, "çok yakın: geri çekilir");

        dangers.Add(new DangerZone(new Vector2(1f, 0f), 1f)); // Başka bir tehlike (örn. ateş)
        Assert.Less(brain.ThinkBossAttack(Vector2.zero, dangers, bossAt, 1.3f, out face).x, 0f, "başka tehlikeden yine kaçar");
    }

    [Test]
    public void BossHealth_GrowsWithAlivePlayers_BotsHitSlower()
    {
        var target = new PlayerState(1, "a", 3);
        CreatureModel boss = Boss(12, Vector2.zero, target);
        BossRules.ScaleForPlayers(boss, 20, config);
        Assert.AreEqual(12 + 19 * config.bossHealthPerExtraPlayer, boss.BossMaxHealth);
        Assert.AreEqual(boss.BossMaxHealth, boss.BossHealth);

        CreatureModel solo = Boss(12, Vector2.zero, target);
        BossRules.ScaleForPlayers(solo, 1, config);
        Assert.AreEqual(12, solo.BossMaxHealth, "tek kişide değişmez");

        var bot = new PlayerState(2, "bot", 3, isBot: true);
        Assert.Greater(BossRules.HitCooldown(bot, config), BossRules.HitCooldown(target, config), "bot daha seyrek vurur");
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
