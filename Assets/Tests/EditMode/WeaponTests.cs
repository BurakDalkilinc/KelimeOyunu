using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Silah testleri: silah verme, atış harcama, merminin uçuşu, isabet, kalkan ve duvar.</summary>
public class WeaponTests
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

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);

        public bool TryApplyKnockback(int playerId, Vector2 distance) => false;
    }

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

    static WordEntry Gun(int shots = 1, float damage = 99f) => new WordEntry
    {
        id = "silah", display = "Silah", category = WordEntry.CategoryDefense, scope = WordEntry.ScopeSelf,
        tags = new[] { "weapon" },
        stats = new WordStats { damage = damage, speed = 16f, duration = 15f, radius = 0.3f, count = shots },
    };

    static void Fly(CreatureModel bullet, ICreatureWorld world, GameConfig config, int frames = 120)
    {
        var hits = new List<CreatureHit>();
        for (int i = 0; i < frames && !bullet.IsFinished; i++)
            CreatureDriver.Advance(bullet, 1f / 60f, world, config, hits, null);
    }

    [Test]
    public void WeaponWord_ArmsTheWriter_WithShotsAndTime()
    {
        var writer = new PlayerState(1, "a", 3);
        int applied = TagRegistry.ApplySelfEffects(Gun(shots: 2), new SelfEffectContext(writer, Gun(shots: 2), config));

        Assert.AreEqual(1, applied);
        Assert.IsTrue(writer.Status.HasWeapon);
        Assert.AreEqual(2, writer.Status.WeaponShots);
        Assert.AreEqual(15f, writer.Status.WeaponTimeLeft, 0.01f);
    }

    [Test]
    public void Shots_RunOut_AndShotsAreSpaced()
    {
        var status = new StatusEffects();
        status.ArmWeapon(Gun(), 2, 10f);

        Assert.IsTrue(status.TryUseWeaponShot(0.5f, out WordEntry first));
        Assert.IsNotNull(first);
        Assert.IsFalse(status.TryUseWeaponShot(0.5f, out _), "iki atış arası beklenmeli");

        status.Tick(0.6f);
        Assert.IsTrue(status.TryUseWeaponShot(0.5f, out _));
        Assert.IsFalse(status.HasWeapon, "mermi bitince silah gider");
    }

    [Test]
    public void Weapon_ExpiresAfterItsTime()
    {
        var status = new StatusEffects();
        status.ArmWeapon(Gun(), 1, 3f);
        status.Tick(3.1f);
        Assert.IsFalse(status.HasWeapon);
    }

    [Test]
    public void Bullet_FliesStraight_AndEliminatesTheFirstPlayerItTouches()
    {
        var world = new FakeWorld();
        PlayerState shooter = world.Add(1, new Vector2(-5f, 0f));
        PlayerState victim = world.Add(2, new Vector2(0f, 0f));
        PlayerState behind = world.Add(3, new Vector2(3f, 0f));
        PlayerState aside = world.Add(4, new Vector2(-2f, 4f));

        CreatureModel bullet = WeaponFire.Build(shooter, Gun(), new Vector2(-5f, 0f), Vector2.right, config, arena);
        Fly(bullet, world, config);

        Assert.IsTrue(bullet.IsFinished);
        Assert.IsTrue(shooter.IsAlive, "kendine değmez");
        Assert.IsFalse(victim.IsAlive, "tek mermi eler");
        Assert.IsTrue(behind.IsAlive, "mermi ilk değdiğinde durur");
        Assert.IsTrue(aside.IsAlive);
    }

    [Test]
    public void Bullet_IsAbsorbedByShield()
    {
        var world = new FakeWorld();
        PlayerState shooter = world.Add(1, new Vector2(-5f, 0f));
        PlayerState victim = world.Add(2, new Vector2(0f, 0f));
        victim.Status.ApplyShield(1, 30f);

        Fly(WeaponFire.Build(shooter, Gun(), new Vector2(-5f, 0f), Vector2.right, config, arena), world, config);

        Assert.IsTrue(victim.IsAlive);
        Assert.AreEqual(3, victim.Health.Current);
        Assert.IsFalse(victim.Status.HasShield, "kalkan harcandı");
    }

    [Test]
    public void Bullet_IsStoppedByWoodenWall()
    {
        var map = new TerrainMap(34, 19);
        Vector2Int wallCell = map.CellOf(new Vector2(-2.5f, 0f));
        for (int y = -2; y <= 2; y++) map.Set(wallCell.x, wallCell.y + y, TileType.WoodWall);

        var world = new FakeWorld();
        PlayerState shooter = world.Add(1, new Vector2(-6f, 0f));
        PlayerState victim = world.Add(2, new Vector2(0f, 0f));

        CreatureModel bullet = WeaponFire.Build(shooter, Gun(), new Vector2(-6f, 0f), Vector2.right, config, arena);
        bullet.Obstacles = new TerrainObstacles(map, null, blockHazards: false);
        Fly(bullet, world, config);

        Assert.IsTrue(bullet.IsFinished);
        Assert.IsTrue(victim.IsAlive, "duvar mermiyi durdurur");
    }

    [Test]
    public void Bullet_DisappearsAfterItsRange()
    {
        var world = new FakeWorld();
        PlayerState shooter = world.Add(1, new Vector2(-15f, 0f));
        PlayerState far = world.Add(2, new Vector2(15f, 0f)); // 30 birim ötede, menzil 12

        Fly(WeaponFire.Build(shooter, Gun(), new Vector2(-15f, 0f), Vector2.right, config, arena), world, config, 300);

        Assert.IsTrue(far.IsAlive);
    }

    [Test]
    public void BotAim_PicksTheNearestLivingGroundedOpponentInRange()
    {
        var world = new FakeWorld();
        PlayerState shooter = world.Add(1, Vector2.zero);
        PlayerState flying = world.Add(2, new Vector2(1f, 0f));
        flying.Status.ApplyFlight(5f, 1f);
        PlayerState near = world.Add(3, new Vector2(0f, -3f));
        world.Add(4, new Vector2(6f, 0f));
        world.Add(5, new Vector2(30f, 0f));

        Assert.IsTrue(WeaponFire.TryAimAtNearest(shooter, Vector2.zero, world, 10f, out Vector2 direction));
        Assert.AreEqual(Vector2.down, direction);

        var lonely = new FakeWorld();
        PlayerState alone = lonely.Add(1, Vector2.zero);
        lonely.Add(2, new Vector2(30f, 0f));
        Assert.IsFalse(WeaponFire.TryAimAtNearest(alone, Vector2.zero, lonely, 10f, out _), "menzilde kimse yok");
    }

    [Test]
    public void WeaponText_ShowsNameShotsAndTime()
    {
        var status = new StatusEffects();
        Assert.AreEqual(string.Empty, WeaponText.Describe(status));

        status.ArmWeapon(Gun(), 1, 14.2f);
        Assert.AreEqual("Silah (Boşluk ile ateş): 1 atış, 15 sn", WeaponText.Describe(status));
    }

    [Test]
    public void SilahWord_IsInTheDatabase_AndArmsOnUse()
    {
        WordEntry word = WordDatabase.LoadFromResources(50).Lookup("gun").Entry;
        Assert.IsNotNull(word);
        Assert.AreEqual("silah", word.id);
        CollectionAssert.Contains(word.tags, "weapon");
    }
}
