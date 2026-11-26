using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maçı sahnede kurar: oyuncuyu ve botları <see cref="MatchState"/> içine koyar, botları arenanın içine dizer.
/// Oyuncu 0 numaradır, botlar 1'den başlar. Tur sistemi (TurnManager) bu maç durumunu kullanacak.
/// </summary>
[DefaultExecutionOrder(-100)] // Diğer her şeyden önce çalışır: maç durumu herkesin Awake/OnEnable'ından önce hazır olsun.
public class MatchBootstrap : MonoBehaviour, ICreatureWorld
{
    [SerializeField] GameConfig config;
    [SerializeField] PlayerController player;

    readonly List<DangerZone> dangers = new List<DangerZone>();
    readonly List<BotController> bots = new List<BotController>();
    readonly Dictionary<int, Transform> avatars = new Dictionary<int, Transform>();

    /// <summary>Maçın arazisi (harita, yangın, lav, uçurum). Awake'te kurulur.</summary>
    public TerrainSim Terrain { get; private set; }

    /// <summary>Herkesin (karakterler, itilenler, yaratıklar) içinden geçemediği engeller: oyuncu duvarları + haritadaki tahta duvarlar.</summary>
    public IObstacleField Obstacles { get; private set; }

    /// <summary>Botların kendi isteğiyle yürürken kaçındığı engeller: <see cref="Obstacles"/> + lav, uçurum, yanan kareler.</summary>
    public IObstacleField BotObstacles { get; private set; }

    /// <summary>Şu anki yaratık modelleri (arazi bunlara bakıp tutuşturur). <see cref="CreatureSpawner"/> atar.</summary>
    public Func<IEnumerable<CreatureModel>> CreatureSource { get; set; }

    /// <summary>Haritadaki duvarlar. Karakterler ve yaratıklar bunların içinden geçemez (uçanlar hariç).</summary>
    public WallField Walls { get; } = new WallField();

    /// <summary>Maçın tüm oyuncuları.</summary>
    public MatchState Match { get; private set; }

    IReadOnlyList<PlayerState> IPlayerLocator.Players => Match.Players;

    /// <summary>Maçın tek rastgelelik kaynağı (zar ve botlar bundan beslenir).</summary>
    public System.Random Random { get; private set; }

    /// <summary>Botların kaçacağı alanlar. Yaratıklar eklenince burayı onlar dolduracak.</summary>
    public IList<DangerZone> Dangers => dangers;

    public IReadOnlyList<BotController> Bots => bots;

    /// <summary>Oyuncunun (insan ya da bot) ekrandaki konumunu verir; oyuncu yoksa false.</summary>
    public bool TryGetPosition(int playerId, out Vector2 position)
    {
        if (avatars.TryGetValue(playerId, out Transform avatar) && avatar != null)
        {
            position = avatar.position;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    /// <summary>Oyuncunun baktığı (son yürüdüğü) yönü verir; karakter yönü bilmiyorsa false.</summary>
    public bool TryGetFacing(int playerId, out Vector2 direction)
    {
        if (avatars.TryGetValue(playerId, out Transform avatar) && avatar != null && avatar.TryGetComponent(out IFacing facing))
        {
            direction = facing.FacingDirection;
            return true;
        }

        direction = Vector2.up;
        return false;
    }

    /// <summary>Oyuncunun sahne karakterini fırlatır (kısa bir süreye yayılarak kayar). Oyuncu itilemiyorsa false.</summary>
    public bool TryApplyKnockback(int playerId, Vector2 distance)
    {
        if (avatars.TryGetValue(playerId, out Transform avatar) && avatar != null && avatar.TryGetComponent(out IKnockable knockable))
        {
            knockable.ApplyKnockback(distance);
            return true;
        }

        return false;
    }

    readonly Dictionary<int, ShoveCooldown> shoveCooldowns = new Dictionary<int, ShoveCooldown>();
    readonly List<PlayerState> shoveStates = new List<PlayerState>();
    readonly List<Vector2> shovePositions = new List<Vector2>();
    readonly List<bool> shoveSolid = new List<bool>();
    readonly List<ShoveHit> shoveHits = new List<ShoveHit>();

    void Update()
    {
        Walls.Tick(Time.deltaTime);
        foreach (ShoveCooldown cooldown in shoveCooldowns.Values) cooldown.Tick(Time.deltaTime);
        TickTerrain(Time.deltaTime);
    }

    /// <summary>Yangını, yaratıkların tutuşturmasını ve lav/ateş/uçurum hasarını ilerletir.</summary>
    void TickTerrain(float dt)
    {
        if (Terrain == null || Match == null) return;

        Terrain.Tick(dt, CreatureSource != null ? CreatureSource() : null, Match.Players,
            id => avatars.TryGetValue(id, out Transform t) && t != null ? (Vector2?)(Vector2)t.position : null,
            (id, to) =>
            {
                if (avatars.TryGetValue(id, out Transform t) && t != null) t.position = new Vector3(to.x, to.y, t.position.z);
            });
    }

    /// <summary>Yanan karoları botların kaçacağı alanlar olarak listeye ekler (en çok 60).</summary>
    public void AppendFireDangers(IList<DangerZone> into)
    {
        if (Terrain != null) Terrain.AppendFireDangers(into);
    }

    ShoveCooldown ShoveCooldownOf(int playerId)
    {
        if (!shoveCooldowns.TryGetValue(playerId, out ShoveCooldown cooldown))
        {
            cooldown = new ShoveCooldown(config.shoveCooldownSeconds);
            shoveCooldowns[playerId] = cooldown;
        }
        return cooldown;
    }

    /// <summary>Oyuncunun itişinin kalan bekleme süresi (sn); 0 ise hazır.</summary>
    public float GetShoveCooldownLeft(int playerId)
    {
        return config == null ? 0f : ShoveCooldownOf(playerId).Remaining;
    }

    /// <summary>
    /// İtiş: bakılan yönde yakındakileri güçlü savurur (<see cref="ShoveRules"/>, savurma mevcut fırlatma sisteminde).
    /// Bekleme süresi dolmadıysa, oyuncu elenmiş ya da uçuyorsa yapılmaz. <paramref name="onlyIfVictims"/> true ise
    /// (botlar) önünde kimse yoksa hak harcanmaz; insan boşa da atabilir (bekleme süresi başlar).
    /// </summary>
    public bool TryShove(int playerId, bool onlyIfVictims)
    {
        if (Match == null || config == null) return false;

        // Silahı olan Boşluk'la itmez, ateş eder.
        PlayerState armed = FindPlayer(playerId);
        if (armed != null && armed.Status.IsDriving) return false; // Araç sürerken Boşluk bir şey yapmaz
        if (armed != null && armed.IsAlive && armed.Status.HasWeapon) return TryFireWeapon(armed, onlyIfVictims);

        ShoveCooldown cooldown = ShoveCooldownOf(playerId);
        if (!cooldown.IsReady) return false;

        shoveStates.Clear(); shovePositions.Clear(); shoveSolid.Clear();
        PlayerState shover = null;
        int shoverIndex = -1;
        foreach (PlayerState state in Match.Players)
        {
            if (!avatars.TryGetValue(state.Id, out Transform avatar) || avatar == null) continue;

            if (state.Id == playerId) { shover = state; shoverIndex = shoveStates.Count; }
            shoveStates.Add(state);
            shovePositions.Add(avatar.position);
            shoveSolid.Add(state.IsAlive && !state.Status.IsFlying);
        }

        if (shover == null || !shover.IsAlive || shover.Status.IsFlying) return false;

        Vector2 facing = TryGetFacing(playerId, out Vector2 f) ? f : Vector2.up;
        ShoveRules.Find(shoverIndex, facing, shovePositions, shoveSolid, config.shoveRange, config.shoveHalfAngleDegrees, config.shoveDistance, shoveHits);

        // Önündeki boss'a da vurur (boss dövmek için bekleme kısa).
        IEnumerable<CreatureModel> creatures = CreatureSource != null ? CreatureSource() : null;
        int bosses = BossRules.HitBosses(creatures, shover, shovePositions[shoverIndex], facing, config, apply: false);
        if (onlyIfVictims && shoveHits.Count == 0 && bosses == 0) return false;

        if (bosses > 0)
        {
            cooldown.TryUse(BossRules.HitCooldown(shover, config));
            BossRules.HitBosses(creatures, shover, shovePositions[shoverIndex], facing, config, apply: true);
        }
        else cooldown.TryUse();
        foreach (ShoveHit hit in shoveHits) TryApplyKnockback(shoveStates[hit.Index].Id, hit.Push);
        ShoveEffect.Spawn(shovePositions[shoverIndex], config.shoveRange);
        return true;
    }

    /// <summary>
    /// Sahneye yaratık koyan kanca (mermiler için). <see cref="CreatureSpawner"/> bağlar; boşsa silah ateş etmez.
    /// </summary>
    public System.Action<CreatureModel> SpawnCreature { get; set; }

    PlayerState FindPlayer(int playerId)
    {
        foreach (PlayerState player in Match.Players)
            if (player.Id == playerId) return player;
        return null;
    }

    /// <summary>
    /// Silahla ateş: insan baktığı yöne, bot menzildeki en yakın rakibe nişan alır (kimse yoksa atış harcamaz).
    /// </summary>
    bool TryFireWeapon(PlayerState shooter, bool onlyIfVictims)
    {
        if (!TryGetPosition(shooter.Id, out Vector2 from)) return false;

        // Kılıç/sopa: ateş etmez, önündekini biçer.
        if (MeleeRules.IsMelee(shooter.Status.Weapon))
        {
            Vector2 facing = TryGetFacing(shooter.Id, out Vector2 face) ? face : Vector2.up;
            WordEntry melee = shooter.Status.Weapon;
            if (onlyIfVictims && MeleeRules.Strike(shooter, from, facing, melee, this, config, false, meleeVictims) == 0) return false;
            if (!shooter.Status.TryUseWeaponShot(config.weaponShotIntervalSeconds, out WordEntry used)) return false;

            MeleeRules.Strike(shooter, from, facing, used, this, config, true, meleeVictims);
            MeleeSwingEffect.Spawn(from, facing, MeleeRules.Reach(used, config), SpriteLibrary.Get(SpriteLibrary.NameFor(used)));
            return true;
        }

        if (SpawnCreature == null) return false;

        Vector2 direction;
        if (onlyIfVictims)
        {
            if (!WeaponFire.TryAimAtNearest(shooter, from, this, config.weaponRange * 0.8f, out direction)) return false;
        }
        else direction = TryGetFacing(shooter.Id, out Vector2 f) ? f : Vector2.up;

        if (!shooter.Status.TryUseWeaponShot(config.weaponShotIntervalSeconds, out WordEntry weapon)) return false;

        SpawnCreature(WeaponFire.Build(shooter, weapon, from, direction, config, new ArenaBounds(config.arenaSize)));
        return true;
    }

    readonly List<Vector2> bodyPositions = new List<Vector2>();
    readonly List<PlayerState> meleeVictims = new List<PlayerState>();

    /// <summary>
    /// Sürücünün aracını bir kare sürer: yön verir, ilerletir, değdiğini ezer; binaya çarparsa patlatır (görüntüsüyle).
    /// Yeni konumu döner.
    /// </summary>
    ThreatDirector threats;

    public Vector2 DriveVehicle(PlayerState driver, Vector2 from, ref Vector2 heading, Vector2 input, float dt, IObstacleField obstacles)
    {
        WordEntry vehicle = driver.Status.Vehicle;
        Vector2 next = VehicleRules.Drive(from, ref heading, input, VehicleRules.Speed(vehicle, config), dt,
                                          config.playerRadius, new ArenaBounds(config.arenaSize), obstacles, out bool crashed);
        if (crashed)
        {
            Vector2 at = next + heading * config.playerRadius;
            if (VehicleRules.Crash(driver, at, this, (id, push) => TryApplyKnockback(id, push), Terrain != null ? Terrain.Fire : null, config))
                ExplosionEffect.Spawn(at, config.vehicleCrashRadius);
            return next;
        }

        if (threats == null) threats = FindAnyObjectByType<ThreatDirector>();
        VehicleRules.Crush(driver, next, this, config, null, threats != null ? (Func<PlayerState, bool>)threats.IsSheltered : null);
        return next;
    }
    readonly List<bool> bodySolid = new List<bool>();
    readonly List<Transform> bodyTransforms = new List<Transform>();
    readonly List<PlayerState> bodyStates = new List<PlayerState>();

    /// <summary>
    /// Karakterlerin birbirinin içinden geçmemesi (<see cref="BodyCollisions"/>): hareket eden öndekini iter.
    /// Hayalet ve uçan karakterler katı değildir. Duvar ve arena sınırı ayrıca korunur.
    /// </summary>
    void LateUpdate()
    {
        if (config == null || !config.charactersPushEachOther || Match == null) return;

        bodyPositions.Clear(); bodySolid.Clear(); bodyTransforms.Clear(); bodyStates.Clear();
        foreach (PlayerState state in Match.Players)
        {
            if (!avatars.TryGetValue(state.Id, out Transform avatar) || avatar == null) continue;

            bodyTransforms.Add(avatar);
            bodyStates.Add(state);
            bodyPositions.Add(avatar.position);
            bodySolid.Add(state.IsAlive && !state.Status.IsFlying);
        }

        var arena = new ArenaBounds(config.arenaSize);
        if (!BodyCollisions.Resolve(bodyPositions, bodySolid, config.playerRadius, arena)) return;

        for (int i = 0; i < bodyTransforms.Count; i++)
        {
            if (!bodySolid[i]) continue;

            Vector2 resolved = bodyPositions[i];
            IObstacleField obstacles = MovementRules.EffectiveObstacles(bodyStates[i], Obstacles);
            if (obstacles != null) resolved = arena.Clamp(obstacles.Resolve(resolved, config.playerRadius), config.playerRadius);

            bodyTransforms[i].position = new Vector3(resolved.x, resolved.y, bodyTransforms[i].position.z);
        }
    }

    void Awake()
    {
        if (config == null || player == null)
        {
            Debug.LogError("MatchBootstrap: config ya da player atanmamış.", this);
            enabled = false;
            return;
        }

        Random = config.CreateRandom();
        var arena = new ArenaBounds(config.arenaSize);

        // Arazi: haritayı üret, engelleri (oyuncu duvarları + haritadaki duvarlar) birleştir.
        int terrainSeed = config.terrainSeed != 0 ? config.terrainSeed : Random.Next(1, int.MaxValue);
        Terrain = new TerrainSim(config, terrainSeed, TerrainSim.ThemeFor(config, terrainSeed), new System.Random(Random.Next()));
        Obstacles = new CompositeObstacles(Walls, Terrain.SolidObstacles);
        BotObstacles = new CompositeObstacles(Walls, Terrain.AvoidingObstacles);

        var states = new List<PlayerState>();
        var humanState = new PlayerState(0, "Sen", config.startingHealth);
        states.Add(humanState);
        for (int i = 1; i <= config.botCount; i++)
            states.Add(new PlayerState(i, "Bot " + i, config.startingHealth, isBot: true));

        Match = new MatchState(states);
        player.Bind(humanState);
        player.Bootstrap = this;
        player.Obstacles = Obstacles;
        player.transform.position = Terrain.NearestSafe(player.transform.position);
        avatars[humanState.Id] = player.transform;

        SpawnBots(states, arena);
    }

    void SpawnBots(List<PlayerState> states, ArenaBounds arena)
    {
        // Botlar arenanın içinde bir halka üzerine, eşit aralıklarla dizilir.
        float ringRadius = Mathf.Min(arena.HalfSize.x, arena.HalfSize.y) * 0.6f;

        for (int i = 1; i < states.Count; i++)
        {
            float angle = Mathf.PI * 2f * (i - 1) / Mathf.Max(1, config.botCount);
            Vector2 position = Terrain.NearestSafe(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius);

            var go = new GameObject(states[i].Name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            Color color = Color.HSVToRGB((i - 1) / (float)Mathf.Max(1, config.botCount), 0.55f, 0.95f);

            // Her bot kendi rastgelelik akışını maçın ana üreticisinden türetir (sıra sabit, seed'li).
            var botRandom = new System.Random(Random.Next());
            var bot = go.AddComponent<BotController>();
            bot.Initialize(states[i], config, arena, botRandom, dangers, color);
            bot.Bootstrap = this;
            bots.Add(bot);
            bot.Obstacles = Obstacles;
            bot.VoluntaryObstacles = BotObstacles;
            avatars[states[i].Id] = go.transform;
        }
    }
}
