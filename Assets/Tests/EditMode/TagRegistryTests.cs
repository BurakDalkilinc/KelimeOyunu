using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>TagRegistry ve CreatureFactory testleri: etiketler yaratığı değiştirir, kelimeden doğru yaratık kurulur.</summary>
public class TagRegistryTests
{
    // Docs/GDD.md "Etiket listesi (v1)" ile aynı olmalı.
    static readonly string[] GddTags =
    {
        "slow", "fast", "flying", "swarm", "undead", "predator", "instant", "knockback", "trap",
        "slowdown", "pull", "armored", "explosive", "area", "water", "fire", "sky", "map_wide",
        "shield", "heal", "wall", "escape", "stealth", "revive"
    };

    ArenaBounds arena;
    GameConfig config;
    PlayerState target;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>();
        target = new PlayerState(1, "Hedef", 3);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    CreatureModel Blank() => new CreatureModel(null, target, Vector2.zero, 1f, 1, 0.5f, 10f, arena);

    SpawnContext Ctx() => new SpawnContext(0, 1, config);

    // ---- Etiketler ----

    [Test]
    public void Undead_MakesCreaturePersistent()
    {
        var creature = Blank();
        Assert.IsFalse(creature.Persistent);

        TagRegistry.Apply(creature, new[] { "undead", "slow" }, Ctx());

        Assert.IsTrue(creature.Persistent);
    }

    [Test]
    public void LabelOnlyAndPendingTags_DoNotChangeCreature()
    {
        var creature = Blank();
        TagRegistry.Apply(creature, new[] { "slow", "fast", "armored", "totallyUnknown", null }, Ctx());

        Assert.IsFalse(creature.Persistent);
        Assert.AreEqual(1f, creature.Speed, 0.0001f);
        Assert.AreEqual(1, creature.Damage);
    }

    [Test]
    public void Apply_NullArguments_DoNotThrow()
    {
        Assert.DoesNotThrow(() => TagRegistry.Apply(null, new[] { "undead" }, Ctx()));
        Assert.DoesNotThrow(() => TagRegistry.Apply(Blank(), null, Ctx()));
    }

    [Test]
    public void EveryGddTag_IsInExactlyOneGroup()
    {
        foreach (string tag in GddTags)
        {
            int groups = 0;
            if (new List<string>(TagRegistry.ImplementedTags).Contains(tag)) groups++;
            if (new List<string>(TagRegistry.LabelOnlyTags).Contains(tag)) groups++;
            if (new List<string>(TagRegistry.PendingTags).Contains(tag)) groups++;

            Assert.AreEqual(1, groups, tag + ": tam olarak bir grupta olmalı (uygulanmış / etiket-yalnız / bekleyen)");
        }
    }

    [Test]
    public void RegistryGroups_ContainNoTagOutsideGddList()
    {
        var all = new List<string>(TagRegistry.ImplementedTags);
        all.AddRange(TagRegistry.LabelOnlyTags);
        all.AddRange(TagRegistry.PendingTags);

        Assert.AreEqual(GddTags.Length, all.Count, "Gruplardaki etiket sayısı GDD listesiyle aynı olmalı");
        foreach (string tag in all)
            CollectionAssert.Contains(GddTags, tag);
    }

    [Test]
    public void IsKnown_WorksForAllGroupsAndRejectsGarbage()
    {
        Assert.IsTrue(TagRegistry.IsKnown("undead"));
        Assert.IsTrue(TagRegistry.IsKnown("slow"));
        Assert.IsTrue(TagRegistry.IsKnown("predator"));
        Assert.IsFalse(TagRegistry.IsKnown("nonsense"));
        Assert.IsFalse(TagRegistry.IsKnown(""));
        Assert.IsFalse(TagRegistry.IsKnown(null));
    }

    [Test]
    public void EveryTagUsedInRealWordsFile_IsKnownToTheRegistry()
    {
        var db = WordDatabase.LoadFromResources(50);
        foreach (WordEntry entry in db.Words)
            foreach (string tag in entry.tags)
                Assert.IsTrue(TagRegistry.IsKnown(tag), entry.id + ": '" + tag + "' etiketi kayıt defterinde yok");
    }

    // ---- CreatureFactory (gerçek kelimelerle) ----

    WordEntry Real(string id)
    {
        var db = WordDatabase.LoadFromResources(50);
        return db.Lookup(id).Entry;
    }

    [Test]
    public void Factory_Zombi_IsPersistentWithStatsFromJson()
    {
        var creature = CreatureFactory.CreateAttack(Real("zombi"), target, Vector2.zero, config, arena);

        Assert.IsTrue(creature.Persistent);
        Assert.AreEqual(2f, creature.Speed, 0.001f);
        Assert.AreEqual(1, creature.Damage);
        Assert.AreEqual(12f, creature.LifeLeft, 0.001f);
        Assert.AreSame(target, creature.Target);
    }

    [Test]
    public void Factory_Ari_IsOneShotAndFast()
    {
        var creature = CreatureFactory.CreateAttack(Real("ari"), target, Vector2.zero, config, arena);

        Assert.IsFalse(creature.Persistent);
        Assert.AreEqual(6f, creature.Speed, 0.001f);
        Assert.AreEqual(1, creature.Damage);
        Assert.AreEqual(8f, creature.LifeLeft, 0.001f);
    }

    [Test]
    public void Factory_Kaplumbaga_HitsHardButSlow()
    {
        var creature = CreatureFactory.CreateAttack(Real("kaplumbaga"), target, Vector2.zero, config, arena);

        Assert.AreEqual(2, creature.Damage);
        Assert.AreEqual(0.8f, creature.Speed, 0.001f);
    }

    [Test]
    public void Factory_Bulut_HasNoDamage()
    {
        var creature = CreatureFactory.CreateAttack(Real("bulut"), target, Vector2.zero, config, arena);

        Assert.AreEqual(0, creature.Damage);
        Assert.IsFalse(creature.IsDangerous);
    }

    [Test]
    public void Factory_WordWithoutStats_UsesDefaultsAndMinResultSeconds()
    {
        var word = new WordEntry { id = "x", tags = new string[0] };
        var creature = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, arena);

        Assert.AreEqual(0, creature.Damage);
        Assert.AreEqual(config.resultSecondsMin, creature.LifeLeft, 0.001f);
    }

    [Test]
    public void Factory_Unknown_IsHarmlessAndTargetless()
    {
        var creature = CreatureFactory.CreateUnknown(new Vector2(2f, 2f), config, arena);

        Assert.IsNull(creature.Word);
        Assert.IsNull(creature.Target);
        Assert.AreEqual(0, creature.Damage);
        Assert.AreEqual(config.unknownCreatureSpeed, creature.Speed, 0.001f);
        Assert.AreEqual(config.resultSecondsMin, creature.LifeLeft, 0.001f);
    }

    [Test]
    public void Factory_NullArguments_Throw()
    {
        var word = Real("zombi");
        Assert.Throws<System.ArgumentNullException>(() => CreatureFactory.CreateAttack(null, target, Vector2.zero, config, arena));
        Assert.Throws<System.ArgumentNullException>(() => CreatureFactory.CreateAttack(word, null, Vector2.zero, config, arena));
        Assert.Throws<System.ArgumentNullException>(() => CreatureFactory.CreateAttack(word, target, Vector2.zero, null, arena));
        Assert.Throws<System.ArgumentNullException>(() => CreatureFactory.CreateUnknown(Vector2.zero, null, arena));
    }
}
