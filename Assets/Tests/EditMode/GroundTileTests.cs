using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Yeni karoların kuralları: buz kayar, dondurucu su ve bataklık kumu yavaşlatır ve hasar verir, kaktüs batar, taş yanmaz.</summary>
public class GroundTileTests
{
    GameConfig config;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.arenaSize = new Vector2(12f, 12f);
    }

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    // ---- Özellikler ----

    [Test]
    public void Properties_OfNewTiles()
    {
        Assert.IsTrue(TerrainMap.IsSolid(TileType.StoneWall));
        Assert.IsTrue(TerrainMap.IsSolid(TileType.AdobeWall));
        Assert.IsTrue(TerrainMap.IsSolid(TileType.Cactus));
        Assert.IsTrue(TerrainMap.IsHouseWall(TileType.StoneWall));
        Assert.IsTrue(TerrainMap.IsHouseWall(TileType.AdobeWall));
        Assert.IsFalse(TerrainMap.IsHouseWall(TileType.Cactus));
        Assert.IsTrue(TerrainMap.IsPrickly(TileType.Cactus));

        foreach (TileType t in new[] { TileType.StoneWall, TileType.StoneFloor, TileType.AdobeWall, TileType.AdobeFloor,
                                       TileType.Ice, TileType.FreezingWater, TileType.Quicksand, TileType.Cactus })
            Assert.IsFalse(TerrainMap.IsFlammable(t), t + " yanmamalı");

        Assert.IsTrue(TerrainMap.IsSafeGround(TileType.StoneFloor));
        Assert.IsTrue(TerrainMap.IsSafeGround(TileType.AdobeFloor));

        Assert.IsTrue(TerrainMap.IsWalkable(TileType.Ice));
        Assert.IsTrue(TerrainMap.IsSafeGround(TileType.Ice));
        Assert.IsTrue(TerrainMap.IsSlippery(TileType.Ice));
        Assert.IsFalse(TerrainMap.IsSlippery(TileType.Grass));

        foreach (TileType t in new[] { TileType.FreezingWater, TileType.Quicksand })
        {
            Assert.IsTrue(TerrainMap.IsWalkable(t), t + " yürünür");
            Assert.IsTrue(TerrainMap.IsDamaging(t));
            Assert.IsFalse(TerrainMap.IsSafeGround(t));
            Assert.IsTrue(TerrainMap.IsDangerous(t));
            Assert.IsFalse(TerrainMap.IsHazard(t), t + " düşülen tehlike değil");
            Assert.IsFalse(TerrainMap.CanHoldFire(t));
        }

        // Eski türler aynı kalır.
        Assert.IsTrue(TerrainMap.IsHouseWall(TileType.WoodWall));
        Assert.IsTrue(TerrainMap.CanHoldFire(TileType.WoodWall));
        Assert.IsTrue(TerrainMap.CanHoldFire(TileType.Grass));
        Assert.IsFalse(TerrainMap.CanHoldFire(TileType.Lava));
        Assert.IsFalse(TerrainMap.IsDangerous(TileType.Grass));
    }

    [Test]
    public void Ground_SpeedMultipliers_ComeFromConfig()
    {
        config.freezingWaterSpeedMultiplier = 0.6f;
        config.quicksandSpeedMultiplier = 0.4f;
        Assert.AreEqual(0.6f, GroundRules.SpeedMultiplier(TileType.FreezingWater, config), 1e-5f);
        Assert.AreEqual(0.4f, GroundRules.SpeedMultiplier(TileType.Quicksand, config), 1e-5f);
        Assert.AreEqual(1f, GroundRules.SpeedMultiplier(TileType.Ice, config), 1e-5f);
        Assert.AreEqual(1f, GroundRules.SpeedMultiplier(TileType.Grass, config), 1e-5f);
    }

    [Test]
    public void GroundUnder_FlyingOrGhost_IgnoresTheGround()
    {
        var map = new TerrainMap(4, 4);
        map.Set(1, 1, TileType.Quicksand);
        var player = new PlayerState(1, "A", 3);

        Assert.AreEqual(TileType.Quicksand, GroundRules.GroundUnder(player, map, map.CellCenter(1, 1)));
        Assert.AreEqual(TileType.Grass, GroundRules.GroundUnder(player, null, map.CellCenter(1, 1)));

        player.Eliminate();
        Assert.AreEqual(TileType.Grass, GroundRules.GroundUnder(player, map, map.CellCenter(1, 1)), "Hayalet zeminden etkilenmez");
    }

    // ---- Hareket ----

    [Test]
    public void Motion_OnGrass_IsInstant_LikeBefore()
    {
        var motion = new GroundMotion();
        Vector2 step = motion.Step(Vector2.right, 5f, TileType.Grass, config, 0.1f);
        Assert.AreEqual(0.5f, step.x, 1e-5f);

        Assert.AreEqual(Vector2.zero, motion.Step(Vector2.zero, 5f, TileType.Grass, config, 0.1f), "Tuş bırakınca hemen durur");
    }

    [Test]
    public void Motion_OnIce_AcceleratesSlowly_AndSlidesAfterRelease()
    {
        config.iceAcceleration = 5f;
        var motion = new GroundMotion();

        Vector2 first = motion.Step(Vector2.right, 5f, TileType.Ice, config, 0.1f);
        Assert.Less(first.x, 0.5f * 0.5f, "Buzda hemen tam hıza çıkılmaz");
        Assert.Greater(first.x, 0f);

        for (int i = 0; i < 20; i++) motion.Step(Vector2.right, 5f, TileType.Ice, config, 0.1f);
        Assert.AreEqual(5f, motion.Velocity.x, 1e-4f, "Sonunda tam hıza ulaşır");

        Vector2 slide = motion.Step(Vector2.zero, 5f, TileType.Ice, config, 0.1f);
        Assert.Greater(slide.x, 0.4f, "Tuş bırakılınca kaymaya devam eder");

        float travelled = 0f;
        for (int i = 0; i < 40; i++) travelled += motion.Step(Vector2.zero, 5f, TileType.Ice, config, 0.1f).x;
        Assert.Greater(travelled, 1f);
        Assert.IsFalse(motion.IsMoving, "Sonunda durur");
    }

    [Test]
    public void Motion_WallStopsTheSlide()
    {
        var motion = new GroundMotion();
        for (int i = 0; i < 30; i++) motion.Step(Vector2.right, 5f, TileType.Ice, config, 0.1f);

        Vector2 wanted = motion.Step(Vector2.zero, 5f, TileType.Ice, config, 0.1f);
        motion.Absorb(wanted, Vector2.zero, 0.1f); // Duvara çarptı, hiç gidemedi
        Assert.IsFalse(motion.IsMoving);
    }

    [Test]
    public void Motion_InQuicksandAndWater_IsSlower()
    {
        config.quicksandSpeedMultiplier = 0.5f;
        config.freezingWaterSpeedMultiplier = 0.6f;
        var motion = new GroundMotion();
        Assert.AreEqual(0.25f, motion.Step(Vector2.right, 5f, TileType.Quicksand, config, 0.1f).x, 1e-5f);
        Assert.AreEqual(0.3f, motion.Step(Vector2.right, 5f, TileType.FreezingWater, config, 0.1f).x, 1e-5f);
    }

    [Test]
    public void AvatarMover_Displace_MatchesStep()
    {
        var arena = new ArenaBounds(new Vector2(12f, 12f));
        Vector2 a = AvatarMover.Step(Vector2.zero, Vector2.right, 5f, 0.1f, 0.5f, arena, null);
        Vector2 b = AvatarMover.Displace(Vector2.zero, new Vector2(0.5f, 0f), 0.5f, arena, null);
        Assert.AreEqual(a.x, b.x, 1e-5f);
        Assert.AreEqual(a.y, b.y, 1e-5f);
    }

    // ---- Hasar ----

    TerrainHazards Hazards(TerrainMap map) => new TerrainHazards(map, new FireSim(map, config, new System.Random(1)), config);

    [Test]
    public void FreezingWater_HurtsOnEntry_ThenEveryInterval_WithoutFire()
    {
        config.freezingWaterDamage = 1;
        config.freezingWaterDamageIntervalSeconds = 1f;
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.FreezingWater);
        var fire = new FireSim(map, config, new System.Random(1));
        var hazards = new TerrainHazards(map, fire, config);
        var player = new PlayerState(1, "A", 10);
        var players = new List<PlayerState> { player };
        Vector2 at = map.CellCenter(3, 3);

        hazards.Tick(0.1f, players, _ => at, null);
        Assert.AreEqual(9, player.Health.Current, "Girişte hemen");
        Assert.AreEqual(FallKind.None, player.Falling, "Suya düşülmez");

        hazards.Tick(0.5f, players, _ => at, null);
        Assert.AreEqual(9, player.Health.Current);
        hazards.Tick(0.6f, players, _ => at, null);
        Assert.AreEqual(8, player.Health.Current);

        Assert.IsFalse(player.Status.IsBurning, "Su tutuşturmaz");
        Assert.AreEqual(0, fire.BurningCount);
        Assert.IsFalse(fire.Ignite(3, 3), "Su alev almaz");
    }

    [Test]
    public void Quicksand_HurtsSlowly_AfterAnInterval()
    {
        config.quicksandDamage = 1;
        config.quicksandDamageIntervalSeconds = 2f;
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.Quicksand);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        var players = new List<PlayerState> { player };
        Vector2 at = map.CellCenter(3, 3);

        hazards.Tick(0.1f, players, _ => at, null);
        Assert.AreEqual(10, player.Health.Current, "Kum hemen vurmaz");
        hazards.Tick(1.5f, players, _ => at, null);
        Assert.AreEqual(10, player.Health.Current);
        hazards.Tick(0.5f, players, _ => at, null);
        Assert.AreEqual(9, player.Health.Current);

        // Çıkıp girince sayaç baştan başlar.
        Vector2 outside = map.CellCenter(6, 6);
        hazards.Tick(0.1f, players, _ => outside, null);
        hazards.Tick(1.9f, players, _ => at, null);
        Assert.AreEqual(9, player.Health.Current);
    }

    [Test]
    public void Ice_DoesNotHurt()
    {
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.Ice);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        for (int i = 0; i < 50; i++) hazards.Tick(0.1f, new List<PlayerState> { player }, _ => map.CellCenter(3, 3), null);
        Assert.AreEqual(10, player.Health.Current);
    }

    [Test]
    public void Cactus_HurtsWhoTouchesIt_WithCooldown()
    {
        config.cactusDamage = 1;
        config.cactusDamageCooldownSeconds = 1f;
        config.playerRadius = 0.5f;
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.Cactus);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        var players = new List<PlayerState> { player };

        Vector2 touching = map.CellCenter(3, 3) + new Vector2(1.0f, 0f); // Kenarı kaktüse değiyor
        Vector2 apart = map.CellCenter(6, 6);
        hazards.Tick(0.1f, players, _ => touching, null);
        Assert.AreEqual(9, player.Health.Current);
        for (int i = 0; i < 50; i++) hazards.Tick(0.1f, players, _ => touching, null);
        Assert.AreEqual(9, player.Health.Current, "Değmeye devam etmek tekrar batmaz");

        hazards.Tick(0.1f, players, _ => apart, null);   // Ayrıldı
        hazards.Tick(0.1f, players, _ => touching, null); // Yeniden değdi, bekleme çoktan bitti
        Assert.AreEqual(8, player.Health.Current);

        hazards.Tick(0.1f, players, _ => apart, null);
        hazards.Tick(0.1f, players, _ => touching, null); // Bekleme süresinde yeniden değdi
        Assert.AreEqual(8, player.Health.Current, "Bekleme süresinde tekrar batmaz");

        var far = new PlayerState(2, "B", 10);
        Vector2 away = map.CellCenter(6, 6);
        for (int i = 0; i < 20; i++) hazards.Tick(0.1f, new List<PlayerState> { far }, _ => away, null);
        Assert.AreEqual(10, far.Health.Current);
    }

    [Test]
    public void Cactus_PushesTheVictimBack_ToSafeGround()
    {
        config.cactusPushDistance = 0.4f;
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.Cactus);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        Vector2 at = map.CellCenter(3, 3) + new Vector2(1.0f, 0f);
        Vector2? pushedTo = null;

        hazards.Tick(0.1f, new List<PlayerState> { player }, _ => at, (id, to) => pushedTo = to);

        Assert.IsTrue(pushedTo.HasValue);
        Assert.AreEqual(at.x + 0.4f, pushedTo.Value.x, 1e-4f, "Kaktüsten uzağa (sağa) itilir");
        Assert.AreEqual(at.y, pushedTo.Value.y, 1e-4f);
    }

    [Test]
    public void Cactus_RubbingAlongItWhileWalking_HurtsAtMostOnce()
    {
        // Kaktüs sırasının hemen yanında 10 saniye aşağı yukarı yürüyen karakter (itme kapalı: hep değiyor).
        config.cactusPushDistance = 0f;
        var map = new TerrainMap(12, 12);
        for (int y = 2; y <= 9; y++) map.Set(5, y, TileType.Cactus);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        var players = new List<PlayerState> { player };
        float x = map.CellCenter(5, 5).x + 1.0f;

        for (float time = 0f; time < 10f; time += 0.05f)
        {
            float y = map.CellCenter(5, 2).y + Mathf.PingPong(time * 3f, 7f);
            hazards.Tick(0.05f, players, _ => new Vector2(x, y), null);
        }
        Assert.GreaterOrEqual(player.Health.Current, 9, "Sürtünerek yürümek en çok bir kez batar");
    }

    [Test]
    public void BotObstacles_KeepBotsClearOfCactusTouchRange()
    {
        config.botCactusClearance = 0.3f;
        config.cactusTouchDistance = 0.08f;
        var map = new TerrainMap(12, 12);
        map.Set(5, 5, TileType.Cactus);
        var sim = new TerrainSim(config, map, new System.Random(1));

        // Bot kaktüse doğru yürüyüp tam dibine gelmek istese de dokunma mesafesine girmez.
        Vector2 wanted = map.CellCenter(5, 5) + new Vector2(1.0f, 0f);
        Vector2 resolved = sim.AvoidingObstacles.Resolve(wanted, config.playerRadius);
        float gap = resolved.x - (map.CellCenter(5, 5).x + 0.5f) - config.playerRadius;
        Assert.Greater(gap, config.cactusTouchDistance);
    }

    [Test]
    public void Spawn_IsNeverNextToACactus()
    {
        var map = new TerrainMap(12, 12);
        map.Set(5, 5, TileType.Cactus);
        var sim = new TerrainSim(config, map, new System.Random(1));

        Vector2Int spawn = map.CellOf(sim.NearestSafe(map.CellCenter(6, 5)));
        Assert.IsFalse(sim.IsNextToPrickly(spawn.x, spawn.y));
        Assert.IsTrue(TerrainMap.IsSafeGround(map.At(spawn.x, spawn.y)));
    }

    [Test]
    public void Spawn_OnDesertMaps_IsNeverNextToACactus()
    {
        for (int seed = 1; seed <= 10; seed++)
        {
            TerrainMap map = MapGenerator.Generate(34, 19, seed, MapRecipe.Desert);
            var sim = new TerrainSim(config, map, new System.Random(1));
            for (int y = 0; y < 19; y++)
            {
                for (int x = 0; x < 34; x++)
                {
                    Vector2Int cell = map.CellOf(sim.NearestSafe(map.CellCenter(x, y)));
                    Assert.IsFalse(sim.IsNextToPrickly(cell.x, cell.y), $"tohum {seed}: ({x},{y}) kaktüs dibinde doğdu");
                }
            }
        }
    }

    [Test]
    public void Cactus_WithZeroDamage_IsOnlyAnObstacle()
    {
        config.cactusDamage = 0;
        var map = new TerrainMap(8, 8);
        map.Set(3, 3, TileType.Cactus);
        TerrainHazards hazards = Hazards(map);
        var player = new PlayerState(1, "A", 10);
        hazards.Tick(0.1f, new List<PlayerState> { player }, _ => map.CellCenter(3, 3) + new Vector2(1f, 0f), null);
        Assert.AreEqual(10, player.Health.Current);

        var obstacles = new TerrainObstacles(map, null, blockHazards: false);
        Vector2 pushed = obstacles.Resolve(map.CellCenter(3, 3) + new Vector2(0.6f, 0f), 0.5f);
        Assert.Greater(pushed.x - map.CellCenter(3, 3).x, 0.99f, "Kaktüsün içinden geçilmez");
    }

    // ---- Botlar ----

    [Test]
    public void Bots_TreatWaterQuicksandAndCactusAsDanger()
    {
        var map = new TerrainMap(12, 12);
        map.Set(2, 2, TileType.FreezingWater);
        map.Set(6, 6, TileType.Quicksand);
        map.Set(9, 9, TileType.Cactus);
        map.Set(9, 2, TileType.Ice);
        var sim = new TerrainSim(config, map, new System.Random(1));

        Assert.IsTrue(sim.IsBadWanderTarget(map.CellCenter(2, 2)));
        Assert.IsTrue(sim.IsBadWanderTarget(map.CellCenter(6, 6)));
        Assert.IsTrue(sim.IsBadWanderTarget(map.CellCenter(9, 9)));
        Assert.IsTrue(sim.IsDangerousAt(map.CellCenter(6, 6)));
        Assert.IsFalse(sim.IsBadWanderTarget(map.CellCenter(9, 2)), "Buz tehlikeli değil");
        Assert.IsFalse(sim.IsHazardousAt(map.CellCenter(6, 6)), "Kum savrulan karakteri durdurmaz (düşülmez)");
        Assert.IsFalse(sim.TryStartFall(new PlayerState(1, "A", 3), map.CellCenter(6, 6)));
    }

    [Test]
    public void BotObstacles_KeepBotsOutOfQuicksand_ButLetThemWalkOut()
    {
        var map = new TerrainMap(12, 12);
        for (int x = 4; x <= 7; x++) for (int y = 4; y <= 7; y++) map.Set(x, y, TileType.Quicksand);
        var sim = new TerrainSim(config, map, new System.Random(1));

        // Dışarıdan kumun kenarına yürüyen bot itilir.
        Vector2 edge = map.CellCenter(3, 5) + new Vector2(0.3f, 0f);
        Vector2 resolved = sim.AvoidingObstacles.Resolve(edge, 0.5f);
        Assert.Less(resolved.x, edge.x, "Bot kendi isteğiyle kuma girmez");

        // İçindeki bot kumu engel saymaz (çakılı kalmaz).
        Vector2 inside = map.CellCenter(5, 5);
        Assert.AreEqual(inside, sim.AvoidingObstacles.Resolve(inside, 0.5f));

        // İtilen karakter için kum engel değildir.
        Assert.AreEqual(edge, sim.SolidObstacles.Resolve(edge, 0.5f));
    }

    [Test]
    public void StoneAndAdobe_DoNotSpreadFire()
    {
        config.fireSpreadChancePerSecond = 100f;
        var map = new TerrainMap(8, 8);
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++) map.Set(x, y, x < 4 ? TileType.StoneFloor : TileType.AdobeFloor);
        map.Set(3, 3, TileType.StoneWall);
        map.Set(5, 5, TileType.AdobeWall);
        var fire = new FireSim(map, config, new System.Random(1));

        Assert.IsTrue(fire.Ignite(1, 1), "Taş zeminde kısa alev olabilir");
        Assert.IsFalse(fire.Ignite(3, 3), "Taş duvar alev almaz");
        Assert.IsFalse(fire.Ignite(5, 5), "Kerpiç duvar alev almaz");
        for (int i = 0; i < 100; i++) fire.Tick(0.1f);

        Assert.AreEqual(0, fire.BurningCount);
        Assert.AreEqual(0, map.Count(TileType.CharredFloor));
    }
}
