using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Kelime paketi 3 testleri: kılıç/sopa (yakın vuruş), tabanca, kamuflaj kılığı, seçilme şansı ve avcı yaratıklar.</summary>
public class BrutalPackTests
{
    class FakeWorld : ICreatureWorld
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position)
        {
            var player = new PlayerState(id, "P" + id, 3);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public void Move(int id, Vector2 position) => positions[id] = position;
        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
        public bool TryApplyKnockback(int playerId, Vector2 distance) => false;
    }

    GameConfig config;
    ArenaBounds arena;
    WordDatabase db;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(34f, 19f);
        arena = new ArenaBounds(config.arenaSize);
        db = WordDatabase.LoadFromResources(50);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    [Test]
    public void SwordAndBat_AreMeleeWeapons_PistolShoots()
    {
        WordEntry sword = db.Lookup("kılıç").Entry;
        WordEntry bat = db.Lookup("beyzbol sopası").Entry;
        WordEntry pistol = db.Lookup("tabanca").Entry;

        Assert.AreEqual("kilic", sword.id);
        Assert.AreEqual("sopa", bat.id);
        Assert.AreEqual("tabanca", pistol.id, "tabanca artık Silah'ın eş anlamlısı değil, kendi kelimesi");
        Assert.IsTrue(MeleeRules.IsMelee(sword));
        Assert.IsTrue(MeleeRules.IsMelee(bat));
        Assert.IsFalse(MeleeRules.IsMelee(pistol));
        Assert.Greater(pistol.stats.count, 1, "tabanca birkaç mermili");
        Assert.AreEqual(1, MeleeRules.Damage(pistol));
    }

    [Test]
    public void Sword_Deals2InFront_TwoHitsEliminate_NotBehindOrFar_ShieldBlocks()
    {
        WordEntry sword = db.Lookup("kilic").Entry;
        var world = new FakeWorld();
        PlayerState me = world.Add(1, Vector2.zero);
        PlayerState front = world.Add(2, new Vector2(1.2f, 0f));
        PlayerState behind = world.Add(3, new Vector2(-1.2f, 0f));
        PlayerState far = world.Add(4, new Vector2(6f, 0f));
        PlayerState shielded = world.Add(5, new Vector2(1f, 0.3f));
        shielded.Status.ApplyShield(1, 30f);

        var victims = new List<PlayerState>();
        Assert.AreEqual(2, MeleeRules.Strike(me, Vector2.zero, Vector2.right, sword, world, config, false, victims));
        Assert.IsTrue(front.IsAlive, "apply false vurmaz");

        MeleeRules.Strike(me, Vector2.zero, Vector2.right, sword, world, config, true, victims);
        Assert.AreEqual(1, front.Health.Current, "tek vuruş 2 hasar: 3 canlı oyuncu ayakta kalır");
        MeleeRules.Strike(me, Vector2.zero, Vector2.right, sword, world, config, true, victims);
        Assert.IsFalse(front.IsAlive, "ikinci vuruşta elenir");
        Assert.IsTrue(behind.IsAlive);
        Assert.IsTrue(far.IsAlive);
        Assert.IsTrue(shielded.IsAlive, "kalkan korur");
        Assert.IsTrue(me.IsAlive, "kendine vurmaz");
    }

    [Test]
    public void MeleeBot_FindsNearestVisiblePrey()
    {
        var world = new FakeWorld();
        PlayerState bot = world.Add(1, Vector2.zero);
        world.Add(2, new Vector2(8f, 0f));
        PlayerState hidden = world.Add(3, new Vector2(2f, 0f));
        hidden.Status.ApplyStealth(5f);

        Assert.IsTrue(MeleeRules.TryFindPrey(bot, Vector2.zero, world, 14f, out Vector2 prey));
        Assert.AreEqual(new Vector2(8f, 0f), prey, "görünmez olanı kovalamaz");
        Assert.IsFalse(MeleeRules.TryFindPrey(bot, Vector2.zero, world, 3f, out _), "menzil dışı");
    }

    [Test]
    public void Camouflage_DisguisesAsBush_InvisibilityDoesNot()
    {
        var writer = new PlayerState(1, "a", 3);
        TagRegistry.ApplySelfEffects(db.Lookup("kamuflaj").Entry,
            new SelfEffectContext(writer, db.Lookup("kamuflaj").Entry, config, Vector2.zero, Vector2.up, null, null));
        Assert.IsTrue(writer.Status.IsStealthed);
        Assert.AreEqual("cali", writer.Status.StealthDisguise);

        var other = new PlayerState(2, "b", 3);
        WordEntry invisible = db.Lookup("görünmezlik").Entry;
        TagRegistry.ApplySelfEffects(invisible, new SelfEffectContext(other, invisible, config, Vector2.zero, Vector2.up, null, null));
        Assert.IsTrue(other.Status.IsStealthed);
        Assert.IsNull(other.Status.StealthDisguise);

        // Kamuflaj sürerken düz görünmezlik çalı kılığını silmez.
        writer.Status.ApplyStealth(3f);
        Assert.AreEqual("cali", writer.Status.StealthDisguise);

        writer.Status.Clear();
        Assert.IsNull(writer.Status.StealthDisguise);
    }

    [Test]
    public void SelectionChance_FollowsDiceWeights()
    {
        var dice = new Dice(new System.Random(1), 0.5f);
        var a = new PlayerState(1, "a", 3);
        var b = new PlayerState(2, "b", 3);
        var c = new PlayerState(3, "c", 3);
        var all = new List<PlayerState> { a, b, c };

        Assert.AreEqual(1f / 3f, dice.ChanceOf(a, all), 0.001f);

        a.MissedTurns = 2; // ağırlık 2, diğerleri 1 → 2/4
        Assert.AreEqual(0.5f, dice.ChanceOf(a, all), 0.001f);

        c.TakeDamage(3); // ölü sayılmaz → 2/3
        Assert.AreEqual(2f / 3f, dice.ChanceOf(a, all), 0.001f);
        Assert.AreEqual(0f, dice.ChanceOf(c, all));
    }

    [Test]
    public void ChasingCreature_HuntsEveryone_DoesNotVanishAfterOneKill()
    {
        var world = new FakeWorld();
        PlayerState first = world.Add(1, new Vector2(1f, 0f));
        PlayerState second = world.Add(2, new Vector2(4f, 0f));
        first.TakeDamage(2); // tek can kaldı

        var word = new WordEntry
        {
            id = "kurt", display = "Kurt", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget,
            tags = new string[0], stats = new WordStats { damage = 1, speed = 4f, duration = 20f, radius = 0.4f },
        };
        CreatureModel wolf = CreatureFactory.CreateAttack(word, first, Vector2.zero, config, arena, 0, 1);
        wolf.Locator = world;
        CreatureBuilder.MakeHunter(wolf);
        Assert.IsTrue(wolf.Persistent && wolf.TargetsNearest && wolf.RetargetsWhenTargetDies);

        var hits = new List<CreatureHit>();
        for (int i = 0; i < 600 && second.Health.Current == 3; i++)
            CreatureDriver.Advance(wolf, 1f / 60f, world, config, hits, null);

        Assert.IsFalse(first.IsAlive, "ilk kurban elendi");
        Assert.Less(second.Health.Current, 3, "sonra sıradakine geçip ona da saldırdı");
        Assert.IsFalse(wolf.IsFinished);
    }

    [Test]
    public void StrikeAndTrap_StayAsTheyWere()
    {
        WordEntry punch = db.Lookup("yumruk").Entry;
        var target = new PlayerState(1, "a", 3);
        CreatureModel model = CreatureFactory.CreateAttack(punch, target, Vector2.zero, config, arena, 0, 1);
        bool persistentBefore = model.Persistent;
        CreatureBuilder.MakeHunter(model);
        Assert.AreEqual(persistentBefore, model.Persistent, "yerinde vuran yaratık avcı olmaz");
    }
}
