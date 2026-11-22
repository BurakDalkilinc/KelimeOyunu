using NUnit.Framework;
using UnityEngine;

/// <summary>"predator" (vur-kaç, sıkılıp gitme) ve "swarm" (sürü) etiketlerinin testleri.</summary>
public class PredatorSwarmTests
{
    const float PlayerRadius = 0.5f;
    const float Cooldown = 1f;

    ArenaBounds arena;
    GameConfig config;
    PlayerState target;

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaBounds(new Vector2(20f, 12f));
        config = ScriptableObject.CreateInstance<GameConfig>(); // predator: geri çekilme 2 sn, gitme 1.5 sn; sürü aralığı 0.8 sn
        target = new PlayerState(1, "Hedef", 30);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    WordEntry Real(string id) => WordDatabase.LoadFromResources(50).Lookup(id).Entry;

    /// <summary>Hedefin hemen yanında doğan, çok hızlı bir avcı (temas anında vurur).</summary>
    CreatureModel Predator(float life = 30f)
    {
        var creature = new CreatureModel(null, target, new Vector2(4f, 0f), 5f, 1, 0.5f, life, arena);
        TagRegistry.Apply(creature, new[] { "predator" }, new SpawnContext(0, 1, config));
        return creature;
    }

    // ---- predator ----

    [Test]
    public void Predator_TagSetsPersistentRetreatAndLeave()
    {
        var creature = Predator();

        Assert.IsTrue(creature.Persistent);
        Assert.AreEqual(config.predatorRetreatSeconds, creature.RetreatAfterHitSeconds, 0.0001f);
        Assert.AreEqual(config.predatorLeaveSeconds, creature.LeaveBeforeEndSeconds, 0.0001f);
    }

    [Test]
    public void Predator_AfterBiting_RetreatsAndDoesNotBiteUntilItReturns()
    {
        var creature = Predator();
        var targetPosition = new Vector2(4f, 0f);

        Assert.IsTrue(creature.Tick(0.1f, targetPosition, PlayerRadius, Cooldown)); // ısırdı
        Assert.IsTrue(creature.IsRetreating);
        float distanceAfterBite = (creature.Position - targetPosition).magnitude;

        // Geri çekilirken uzaklaşır ve ısırmaz.
        for (int i = 0; i < 10; i++)
            Assert.IsFalse(creature.Tick(0.1f, targetPosition, PlayerRadius, Cooldown));

        Assert.Greater((creature.Position - targetPosition).magnitude, distanceAfterBite + 2f);
        Assert.AreEqual(29, target.Health.Current);
    }

    [Test]
    public void Predator_AfterRetreat_ComesBackAndBitesAgain()
    {
        var creature = Predator();
        var targetPosition = new Vector2(4f, 0f);
        int bites = 0;

        for (int i = 0; i < 100; i++) // 10 saniye
            if (creature.Tick(0.1f, targetPosition, PlayerRadius, Cooldown)) bites++;

        Assert.GreaterOrEqual(bites, 3, "Avcı geri çekilip tekrar saldırmalı");
        Assert.Less(bites, 6, "Her vuruş arasında en az geri çekilme süresi kadar ara olmalı");
    }

    [Test]
    public void Predator_BitesLessOftenThanUndeadWithSameStats()
    {
        int predatorBites = CountBites(Predator(), 10f);

        var undead = new CreatureModel(null, target, new Vector2(4f, 0f), 5f, 1, 0.5f, 30f, arena);
        TagRegistry.Apply(undead, new[] { "undead" }, new SpawnContext(0, 1, config));
        int undeadBites = CountBites(undead, 10f);

        Assert.Less(predatorBites, undeadBites, "Geri çekilme, kaçmak için fırsat bırakmalı");
    }

    int CountBites(CreatureModel creature, float seconds)
    {
        int bites = 0;
        for (int i = 0; i < Mathf.RoundToInt(seconds * 10f); i++)
            if (creature.Tick(0.1f, new Vector2(4f, 0f), PlayerRadius, Cooldown)) bites++;
        return bites;
    }

    [Test]
    public void Predator_InLastSeconds_LeavesAndNeverBites()
    {
        var creature = Predator(life: 10f);
        var targetPosition = new Vector2(4f, 0f);

        creature.Tick(8.6f, targetPosition, PlayerRadius, Cooldown); // Ömrün son 1.4 sn'si (< 1.5) → sıkıldı
        Assert.IsTrue(creature.IsLeaving);

        int health = target.Health.Current;
        for (int i = 0; i < 10; i++)
            Assert.IsFalse(creature.Tick(0.1f, targetPosition, PlayerRadius, Cooldown));

        Assert.AreEqual(health, target.Health.Current);
    }

    [Test]
    public void Predator_WhileLeaving_MovesAwayFromTarget()
    {
        // Ömrü 1.4 sn kalmış (< 1.5) avcı baştan "sıkılmış" durumda; hedef (0,0), avcı (4,0)'da.
        var creature = Predator(life: 1.4f);
        var targetPosition = new Vector2(0f, 0f);
        Assert.IsTrue(creature.IsLeaving);
        float before = (creature.Position - targetPosition).magnitude;

        creature.Tick(0.2f, targetPosition, PlayerRadius, Cooldown);

        Assert.Greater((creature.Position - targetPosition).magnitude, before);
    }

    [Test]
    public void NonPredator_NeverRetreatsOrLeaves()
    {
        var creature = new CreatureModel(null, target, new Vector2(4f, 0f), 5f, 1, 0.5f, 3f, arena) { Persistent = true };

        creature.Tick(0.1f, new Vector2(4f, 0f), PlayerRadius, Cooldown); // vurdu
        Assert.IsFalse(creature.IsRetreating);

        creature.Tick(1.7f, new Vector2(4f, 0f), PlayerRadius, Cooldown); // ömrün sonuna yaklaştı
        Assert.IsFalse(creature.IsLeaving);
    }

    [Test]
    public void Kurt_FromRealWords_IsAPredator()
    {
        var creature = CreatureFactory.CreateAttack(Real("kurt"), target, Vector2.zero, config, arena);

        Assert.IsTrue(creature.Persistent);
        Assert.Greater(creature.RetreatAfterHitSeconds, 0f);
        Assert.Greater(creature.LeaveBeforeEndSeconds, 0f);
        Assert.AreEqual(5.5f, creature.Speed, 0.001f);
        Assert.AreEqual(8f, creature.LifeLeft, 0.001f);
    }

    [Test]
    public void Predator_TargetStandingStillOnTopOfWall_DoesNotCrashWhenCornered()
    {
        // Köşeye sıkışan avcı uzaklaşamaz ama hata vermemeli ve arenanın içinde kalmalı.
        var creature = new CreatureModel(null, target, new Vector2(10f, 6f), 5f, 1, 0.5f, 30f, arena);
        TagRegistry.Apply(creature, new[] { "predator" }, new SpawnContext(0, 1, config));

        for (int i = 0; i < 200; i++)
            creature.Tick(0.1f, new Vector2(9.5f, 5.5f), PlayerRadius, Cooldown);

        Assert.IsTrue(arena.Contains(creature.Position));
    }

    // ---- swarm ----

    [Test]
    public void Swarm_Ari_SpawnsFiveMembers()
    {
        Assert.AreEqual(5, TagRegistry.GetSpawnCount(Real("ari")));
    }

    [Test]
    public void SpawnCount_WordsWithoutSwarm_IsOne()
    {
        // Zombi sürüsünün "count" değeri 20 ama o kelimede sürü etiketi de var; tek yaratıklılar ise 1 olmalı.
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(Real("zombi")));
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(Real("kurt")));
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(Real("meteor_yagmuru"))); // count: 30 ama swarm etiketi yok
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(null));
    }

    [Test]
    public void SpawnCount_SwarmWithoutOrInvalidCount_IsAtLeastOne()
    {
        var noStats = new WordEntry { id = "x", tags = new[] { "swarm" } };
        var zeroCount = new WordEntry { id = "y", tags = new[] { "swarm" }, stats = new WordStats { count = 0 } };
        var negative = new WordEntry { id = "z", tags = new[] { "swarm" }, stats = new WordStats { count = -4 } };

        Assert.AreEqual(1, TagRegistry.GetSpawnCount(noStats));
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(zeroCount));
        Assert.AreEqual(1, TagRegistry.GetSpawnCount(negative));
    }

    [Test]
    public void SpawnCount_HugeCount_IsCapped()
    {
        var word = new WordEntry { id = "x", tags = new[] { "swarm" }, stats = new WordStats { count = 100000 } };
        Assert.LessOrEqual(TagRegistry.GetSpawnCount(word), 100);
    }

    [Test]
    public void Zombi_Suru_FromRealWords_SpawnsTwentyOnceItsTagIsImplemented()
    {
        Assert.AreEqual(20, TagRegistry.GetSpawnCount(Real("zombi_suru")));
    }

    [Test]
    public void SwarmMembers_FirstHitIsStaggeredByTheirIndex()
    {
        var ari = Real("ari");
        var targetPosition = new Vector2(4f, 0f);

        // Hepsi hedefin tam üstünde doğuyor; ilk vuruş sırasına göre ertelenir (0, 0.8, 1.6, ... sn).
        for (int index = 0; index < 5; index++)
        {
            var member = CreatureFactory.CreateAttack(ari, new PlayerState(100 + index, "H", 30), targetPosition, config, arena, index, 5);
            float firstHitAt = FirstHitTime(member, targetPosition);

            Assert.AreEqual(index * config.swarmHitStaggerSeconds, firstHitAt, 0.11f, "Üye " + index);
        }
    }

    static float FirstHitTime(CreatureModel member, Vector2 targetPosition)
    {
        for (int i = 1; i <= 100; i++)
            if (member.Tick(0.05f, targetPosition, PlayerRadius, Cooldown)) return i * 0.05f - 0.05f;
        return -1f;
    }

    [Test]
    public void Swarm_FiveBeesOnOneTarget_DoNotKillInstantly()
    {
        // 3 canlı hedefe 5 arı: ilk vuruşlar 0.8 sn arayla geldiği için ilk 1 saniyede en çok 2 hasar.
        var victim = new PlayerState(50, "Kurban", 3);
        var ari = Real("ari");
        var targetPosition = new Vector2(4f, 0f);
        var bees = new CreatureModel[5];
        for (int i = 0; i < 5; i++)
            bees[i] = CreatureFactory.CreateAttack(ari, victim, targetPosition, config, arena, i, 5);

        for (int step = 0; step < 20; step++) // 1 saniye
            foreach (var bee in bees) bee.Tick(0.05f, targetPosition, PlayerRadius, Cooldown);

        Assert.IsTrue(victim.IsAlive);
        Assert.GreaterOrEqual(victim.Health.Current, 1);
    }

    [Test]
    public void DelayFirstHit_NegativeOrZero_DoesNothing()
    {
        var creature = new CreatureModel(null, target, new Vector2(4f, 0f), 5f, 1, 0.5f, 30f, arena);
        creature.DelayFirstHit(-5f);
        creature.DelayFirstHit(0f);

        Assert.IsTrue(creature.Tick(0.05f, new Vector2(4f, 0f), PlayerRadius, Cooldown));
    }

    [Test]
    public void SpawnContext_ClampsNegativeIndexAndZeroCount()
    {
        var context = new SpawnContext(-3, 0, config);
        Assert.AreEqual(0, context.Index);
        Assert.AreEqual(1, context.Count);
    }
}
