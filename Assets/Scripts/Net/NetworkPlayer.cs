using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ağdaki bir karakter (insan ya da bot). Hareketi, canı ve etkileri yalnızca host hesaplar:
/// sahibi olan istemci sadece "şu yöne basıyorum" der (<see cref="MoveInputRpc"/>), host hızı, yavaşlamayı, fırlatmayı
/// ve arena sınırını uygular; konumu ve canı herkese ağ değişkenleriyle yayar. Botları da host sürer.
/// Hesap düz C# sınıflarındadır (<see cref="AvatarMover"/>, <see cref="BotBrain"/>, <see cref="PlayerState"/>); burası taşıyıcı ve görüntüdür.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>İnsan oyuncuların başlangıç halkası (botların halkasının içinde).</summary>
    const float HumanRingFraction = 0.4f;

    static readonly IReadOnlyList<DangerZone> NoDangers = new List<DangerZone>();

    /// <summary>Host'taki karakterler (numarasına göre). Yaratık simülasyonu konum ve fırlatma için bunu kullanır.</summary>
    static readonly Dictionary<int, NetworkPlayer> ServerAvatars = new Dictionary<int, NetworkPlayer>();

    /// <summary>Bu bilgisayarda görünen bütün karakterler (fareyle hedef seçmek için).</summary>
    static readonly List<NetworkPlayer> Everyone = new List<NetworkPlayer>();

    /// <summary>Uzak karakterin hedef konuma yaklaşma sertliği (büyük = daha sıkı takip).</summary>
    const float FollowSharpness = 18f;

    [SerializeField] GameConfig config;

    readonly NetworkVariable<int> playerId = new NetworkVariable<int>(-1);
    readonly NetworkVariable<bool> isBot = new NetworkVariable<bool>(false);
    readonly NetworkVariable<Vector2> position = new NetworkVariable<Vector2>();
    readonly NetworkVariable<int> health = new NetworkVariable<int>(1);
    readonly NetworkVariable<int> statusFlags = new NetworkVariable<int>(0);

    /// <summary>İtişin kalan bekleme süresi (host yazar, yaklaşık; arayüz kendi geri sayar).</summary>
    readonly NetworkVariable<float> shoveLeft = new NetworkVariable<float>(0f);

    /// <summary>Her itişte artar; herkes artışı görünce halka efekti çıkarır.</summary>
    readonly NetworkVariable<int> shoveCount = new NetworkVariable<int>(0);

    /// <summary>Sürülen aracın resmi ("" ise araç yok) ve genişliği; her istemci aracı çizer.</summary>
    readonly NetworkVariable<FixedString32Bytes> vehicleSprite = new NetworkVariable<FixedString32Bytes>(default);
    readonly NetworkVariable<float> vehicleSize = new NetworkVariable<float>(1.2f);

    /// <summary>Her araç patlamasında artar; istemciler patlama görüntüsünü oynatır.</summary>
    readonly NetworkVariable<int> crashCount = new NetworkVariable<int>(0);

    /// <summary>Kamuflaj kılığının resmi ("" ise kılık yok); her istemci karakteri o resme (çalı) büründürür.</summary>
    readonly NetworkVariable<FixedString32Bytes> disguiseSprite = new NetworkVariable<FixedString32Bytes>(default);

    /// <summary>Kılıç/sopa savuruşu: her vuruşta sayaç artar; silahın resmi ve savurma yönü istemcilerde görüntüyü çizer.</summary>
    readonly NetworkVariable<int> swingCount = new NetworkVariable<int>(0);
    readonly NetworkVariable<FixedString32Bytes> swingSprite = new NetworkVariable<FixedString32Bytes>(default);
    readonly NetworkVariable<Vector2> swingDirection = new NetworkVariable<Vector2>(Vector2.up);
    readonly NetworkVariable<float> swingReach = new NetworkVariable<float>(1.6f);

    /// <summary>Kömürleşme (0..1); her istemci karakteri o kadar karartır.</summary>
    readonly NetworkVariable<float> charLevel = new NetworkVariable<float>(0f);

    readonly KnockbackMotion knockback = new KnockbackMotion();
    readonly GroundMotion groundMotion = new GroundMotion();

    /// <summary>Yön mesajı hız sınırı: normal oyunda yalnızca tuş değişince gider; seli engeller.</summary>
    readonly RpcRateLimiter moveLimiter = new RpcRateLimiter(30f, 30f);

    // İtiş (Boşluk): bekleme süresi ve hesap host'tadır; istemci yalnızca "bastım" der.
    readonly RpcRateLimiter shoveLimiter = new RpcRateLimiter(3f, 1f);
    readonly List<PlayerState> shoveStates = new List<PlayerState>();
    readonly List<NetworkPlayer> shoveAvatars = new List<NetworkPlayer>();
    readonly List<Vector2> shovePositions = new List<Vector2>();
    readonly List<bool> shoveSolid = new List<bool>();
    readonly List<ShoveHit> shoveHits = new List<ShoveHit>();
    ShoveCooldown shoveCooldown;
    System.Random shoveRandom;
    float shoveThinkTimer;
    float shoveSyncTimer;
    float shoveLeftChangedAt;
    PressEdge shoveEdge;

    SpriteRenderer spriteRenderer;
    ArenaBounds arena;
    BotBrain botBrain;
    NetTurnChannel channel;
    int registeredId = -1;

    int preparedBotId = -1;
    Vector2 serverInput;
    Vector2 lastSentInput;
    Vector2 facing = Vector2.up;
    Vector2? botBossFacing;

    /// <summary>Bu karakterin maç numarası (insan: bağlantı numarası, bot: 100'den başlar).</summary>
    public int PlayerId => playerId.Value;

    public bool IsBot => isBot.Value;

    /// <summary>İtişin kalan bekleme süresi (sn), son host mesajından bu yana geri sayılarak; 0 ise hazır.</summary>
    public float ShoveLeft => Mathf.Max(0f, shoveLeft.Value - (Time.unscaledTime - shoveLeftChangedAt));

    /// <summary>Şu anki can (herkes görür).</summary>
    public int Health => health.Value;

    /// <summary>Elendi mi? Elenen hayalet olur: gezer ama hiçbir şey ona işlemez.</summary>
    public bool IsGhost => health.Value <= 0;

    /// <summary>Üzerindeki etkiler (yavaş, kalkan, görünmez, uçan, çekilen).</summary>
    public StatusFlags Status => (StatusFlags)statusFlags.Value;

    /// <summary>Host'ta: son yürüdüğü yön (duvar önünde belirsin diye). Henüz yürümediyse yukarı.</summary>
    public Vector2 FacingDirection => facing;

    /// <summary>Bu bilgisayarda görünen karakterler.</summary>
    public static IReadOnlyList<NetworkPlayer> All => Everyone;

    /// <summary>Host'ta numarasına göre karakteri bulur.</summary>
    public static bool TryFind(int playerId, out NetworkPlayer avatar)
    {
        return ServerAvatars.TryGetValue(playerId, out avatar) && avatar != null;
    }

    /// <summary>Doğmadan önce çağrılır: bu karakter bir bot olacak. Yalnızca host'ta.</summary>
    public void PrepareAsBot(int botPlayerId)
    {
        preparedBotId = botPlayerId;
    }

    /// <summary>
    /// Sahibinin bağlantısı koptu: karakter olduğu yerde kalır ve bundan sonra bot gibi gezer (yalnızca host'ta).
    /// </summary>
    public void BecomeBotOnServer()
    {
        if (!IsServer || isBot.Value || config == null) return;

        isBot.Value = true;
        serverInput = Vector2.zero;
        botBrain = new BotBrain(config, arena ?? new ArenaBounds(config.arenaSize), new System.Random(playerId.Value * 7919 + 1));
    }

    /// <summary>
    /// Yeni maç için sıfırlar: can tam, etkiler ve fırlatma silinir, karakter başlangıç yerine döner (yalnızca host'ta, insan karakterler).
    /// </summary>
    public void ResetOnServer()
    {
        if (!IsServer || isBot.Value || config == null) return;

        knockback.Start(Vector2.zero, 0.01f);
        serverInput = Vector2.zero;
        shoveCooldown?.Reset();
        shoveLeft.Value = 0f;
        health.Value = config.startingHealth;
        statusFlags.Value = 0;
        charLevel.Value = 0f;
        vehicleSprite.Value = default;
        disguiseSprite.Value = default;

        Vector2 start = HumanStart();
        position.Value = start;
        transform.position = start;
    }

    /// <summary>Host'ta karakterin şu anki konumu.</summary>
    public Vector2 ServerPosition => position.Value;

    /// <summary>Host'ta karakteri verilen konuma koyar (itişme çözümü). İstemcide bir şey yapmaz.</summary>
    public void ServerSetPosition(Vector2 newPosition)
    {
        if (!IsServer) return;
        position.Value = newPosition;
        transform.position = newPosition;
    }

    /// <summary>Karakteri kısa bir süreye yayarak kaydırır (bir yaratık fırlattığında). Yalnızca host'ta.</summary>
    public void ApplyKnockback(Vector2 distance)
    {
        if (config == null) return;
        knockback.Start(distance, config.knockbackSeconds);
    }

    public override void OnNetworkSpawn()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteLibrary.Get("char") ?? SpriteFactory.Circle();
        spriteRenderer.sortingOrder = CreatureSpriteScale.EntityOrder;

        var animator = gameObject.GetComponent<SpriteAnimator>();
        if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
        animator.SetFrames(SpriteLibrary.GetFrames("char"));
        CharacterStackVisual.Attach(gameObject); // 3D piksel (katmanlı) gövde
        NameTagView.Attach(gameObject, config, NameTag); // Başın üstünde adı (kadrodan)

        playerId.OnValueChanged += (_, __) => ApplyLook();
        health.OnValueChanged += (_, __) => ApplyLook();
        statusFlags.OnValueChanged += (_, __) => ApplyLook();
        charLevel.OnValueChanged += (_, __) => ApplyLook();
        shoveLeft.OnValueChanged += (_, __) => shoveLeftChangedAt = Time.unscaledTime;
        shoveCount.OnValueChanged += (_, __) => { if (config != null) ShoveEffect.Spawn(transform.position, config.shoveRange); };
        crashCount.OnValueChanged += (_, __) => { if (config != null) ExplosionEffect.Spawn(transform.position, config.vehicleCrashRadius); };
        swingCount.OnValueChanged += (_, __) =>
            MeleeSwingEffect.Spawn(transform.position, swingDirection.Value, swingReach.Value, SpriteLibrary.Get(swingSprite.Value.ToString()));
        shoveLeftChangedAt = Time.unscaledTime;
        Everyone.Add(this);
        GoreEmitter.Attach(gameObject, () => health.Value,
                           () => (Status & StatusFlags.FallLava) != 0 ? FallKind.Lava : (Status & StatusFlags.FallChasm) != 0 ? FallKind.Chasm : FallKind.None) // Kan ve parçalanma (her istemcide)
                   .Charred = () => (Status & StatusFlags.Burning) != 0 || charLevel.Value >= 0.5f;

        if (IsServer) InitOnServer();

        transform.position = (Vector3)position.Value;
        ApplyLook();
        channel = FindAnyObjectByType<NetTurnChannel>();
    }

    public override void OnNetworkDespawn()
    {
        Everyone.Remove(this);
        if (registeredId >= 0 && ServerAvatars.TryGetValue(registeredId, out NetworkPlayer current) && current == this)
            ServerAvatars.Remove(registeredId);
        registeredId = -1;
    }

    void InitOnServer()
    {
        if (config == null) { Debug.LogError("NetworkPlayer: GameConfig atanmamış.", this); return; }

        arena = new ArenaBounds(config.arenaSize);
        health.Value = config.startingHealth;
        shoveCooldown = new ShoveCooldown(config.shoveCooldownSeconds);
        shoveRandom = new System.Random(unchecked((int)OwnerClientId * 7919 + 13));

        if (preparedBotId >= 0)
        {
            isBot.Value = true;
            playerId.Value = preparedBotId;
            botBrain = new BotBrain(config, arena, new System.Random(preparedBotId * 7919 + 1));
            position.Value = transform.position;
        }
        else
        {
            // İnsan: bağlantı numarasıyla aynı. Oyuncular yan yana dizilir.
            // Bağlantısı koparsa karakter silinmesin (sahipliği host'a geçer), bot devralsın.
            NetworkObject.DontDestroyWithOwner = true;
            playerId.Value = (int)OwnerClientId;
            position.Value = HumanStart();
        }

        registeredId = playerId.Value;
        ServerAvatars[registeredId] = this;
    }

    /// <summary>İnsan oyuncunun başlangıç yeri: bağlantı numarasına göre iç halkada bir nokta.</summary>
    Vector2 HumanStart()
    {
        return SpawnRing.Point(arena ?? new ArenaBounds(config.arenaSize), (int)OwnerClientId, RosterBuilder.MaxPlayers, HumanRingFraction);
    }

    /// <summary>
    /// İsim etiketinin bilgisi: ad host'un gönderdiği kadrodan (lobi adı), kadro henüz gelmediyse "Bot N" / "Oyuncu N".
    /// Kendi karakterin sarı; başkasının hayaleti çizilmediği için adı da görünmez; görünmezlik/kılıkta ad gizlenir.
    /// </summary>
    NameTagInfo NameTag()
    {
        int id = playerId.Value;
        string name = null;
        if (channel != null && channel.Client != null && channel.Client.Players.TryGetValue(id, out PlayerState state)) name = state.Name;
        if (string.IsNullOrEmpty(name) && id >= 0) name = NameTagRules.DefaultName(id, isBot.Value, RosterBuilder.FirstBotId);

        return new NameTagInfo
        {
            Name = name,
            IsLocal = IsOwner && !isBot.Value,
            IsAlive = !IsGhost,
            BodyShown = spriteRenderer != null && spriteRenderer.enabled,
            IsStealthed = (Status & StatusFlags.Stealth) != 0 || !disguiseSprite.Value.IsEmpty,
        };
    }

    void ApplyLook()
    {
        if (spriteRenderer == null) return;
        Color color = PlayerColors.For(playerId.Value);
        spriteRenderer.color = IsGhost ? StatusTint.Ghost(color) : StatusFlagsUtil.Tint(color, Status);
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer) ServerStep(Time.deltaTime);
        else transform.position = Vector2.Lerp(transform.position, position.Value, 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime));

        if (IsOwner && !isBot.Value) SendInput();

        // Düşme animasyonu (herkeste, bayraktan).
        if (config != null)
        {
            if (fall == null) fall = FallAnimation.Attach(gameObject, config.fallSeconds);
            fall.Kind = (Status & StatusFlags.FallChasm) != 0 ? FallKind.Chasm : (Status & StatusFlags.FallLava) != 0 ? FallKind.Lava : FallKind.None;
        }
        FlightVisual.Attach(gameObject).Flying = !IsGhost && (Status & StatusFlags.Flight) != 0; // Uçarken gölge ve alev
        if (spriteRenderer != null) spriteRenderer.enabled = !IsGhost || (IsOwner && !isBot.Value); // Başkalarının hayaleti görünmez
        string disguiseName = disguiseSprite.Value.ToString();
        DisguiseVisual.Attach(gameObject).SpriteName = IsGhost || string.IsNullOrEmpty(disguiseName) ? null : disguiseName; // Kamuflaj: çalı
        bool burning = !IsGhost && (Status & StatusFlags.Burning) != 0;
        BurnVisual.Attach(gameObject).Burning = burning; // Ateş aldı: alevler
        if (spriteRenderer != null && !IsGhost && (burning || charLevel.Value > 0f))
            spriteRenderer.color = StatusTint.Burnt(StatusFlagsUtil.Tint(PlayerColors.For(playerId.Value), Status), charLevel.Value, burning);
        VehicleVisual car = VehicleVisual.Attach(gameObject);
        car.SpriteName = IsGhost ? null : vehicleSprite.Value.ToString();
        car.WorldSize = vehicleSize.Value;
    }

    FallAnimation fall;

    void ServerStep(float dt)
    {
        if (arena == null) return;

        NetMatchSim sim = channel != null ? channel.Sim : null;
        PlayerState state = sim != null ? sim.FindPlayer(playerId.Value) : null;
        bool frozenBot = isBot.Value && state != null && !state.IsAlive; // Elenen bot durur; elenen insan hayalet olup gezer

        Vector2 current = position.Value;
        Vector2 next = current;

        if (!frozenBot)
        {
            // Hayalet ve uçan karakter duvarlardan geçer; diğerleri takılır.
            IObstacleField obstacles = sim != null ? MovementRules.EffectiveObstacles(state, sim.Obstacles) : null;

            // Botlar kendi isteğiyle lava, uçuruma ve yanan karolara yürümez; itilen ya da fırlatılan girebilir.
            IObstacleField voluntary = sim != null ? MovementRules.EffectiveObstacles(state, isBot.Value ? sim.BotObstacles : sim.Obstacles) : null;

            if (state != null)
            {
                // Fırlatılma ve mıknatıs çekimi, yazarken ya da hareketsizken de olur.
                Vector2 pushed = knockback.Step(dt);
                if (pushed != Vector2.zero)
                {
                    Vector2 before = next;
                    next = MoveBy(next, pushed, obstacles);

                    // Yolda lav/uçuruma değdiyse orada dur (ince uçurumun üstünden atlayıp geçme).
                    if (sim != null && sim.Terrain != null && !state.Status.IsFlying)
                    {
                        next = sim.Terrain.FirstHazardOnPath(before, next, out bool hitHazard);
                        if (hitHazard)
                        {
                            knockback.Cancel();
                            sim.Terrain.TryStartFall(state, next); // Lava/uçurumda kaldıysa düşme animasyonu, sonra ölüm
                        }
                    }
                }

                Vector2 pulled = state.Status.GetPullStep(next, dt);
                if (pulled != Vector2.zero) next = MoveBy(next, pulled, obstacles);
            }

            float speedMultiplier = state != null ? state.Status.SpeedMultiplier : 1f;
            bool falling = state != null && state.Falling != FallKind.None; // Lava/uçuruma düşüyor: kımıldamaz

            // Savrulan bot kendi yürümez; yürüseydi "lav/uçuruma girme" engeli onu kenara geri iterdi.
            bool stunnedBot = isBot.Value && knockback.IsActive;
            Vector2 input = falling || stunnedBot ? Vector2.zero : isBot.Value ? BotDirection(dt, next, sim, state) : serverInput;
            if (input != Vector2.zero) facing = input.normalized;
            if (isBot.Value && botBossFacing.HasValue) facing = botBossFacing.Value; // Saldırdığı boss'a bakar

            if (state != null && state.IsAlive && state.Status.IsDriving && !falling && sim != null)
            {
                // Araç: hiç durmaz, girdi yalnızca yön verir; bot en yakın rakibin üstüne sürer.
                if (isBot.Value)
                    input = MeleeRules.TryFindPrey(state, next, sim, config.botVehiclePreyRange, out Vector2 prey) ? (prey - next).normalized : Vector2.zero;
                groundMotion.Stop();
                Vector2 before = next;
                next = sim.DriveVehicle(state, next, ref facing, input, dt, obstacles, out bool exploded);
                if (exploded) crashCount.Value++;
                if (sim.Terrain != null && !state.Status.IsFlying)
                {
                    next = sim.Terrain.FirstHazardOnPath(before, next, out bool hitHazard);
                    if (hitHazard) sim.Terrain.TryStartFall(state, next);
                }
            }
            else
            {
                // Zemine göre yürüyüş: su ve kumda yavaş, buzda kayarak. Düşen ya da savrulan karakterin kayması durur.
                if (falling || stunnedBot) groundMotion.Stop();
                float speed = (isBot.Value ? config.botMoveSpeed : config.playerMoveSpeed) * speedMultiplier;
                TileType ground = GroundRules.GroundUnder(state, sim != null && sim.Terrain != null ? sim.Terrain.Map : null, next);
                Vector2 wanted = groundMotion.Step(AvatarMover.SanitizeInput(input), speed, ground, config, dt);
                Vector2 before = next;
                next = AvatarMover.Displace(next, wanted, config.playerRadius, arena, voluntary);
                groundMotion.Absorb(wanted, next - before, dt);
            }

            // Duvar tam üstüne çıktıysa karakter dışarı itilir.
            if (obstacles != null)
            {
                Vector2 resolved = obstacles.Resolve(next, config.playerRadius);
                if (resolved != next) next = arena.Clamp(resolved, config.playerRadius);
            }
        }

        if (next != current) position.Value = next;
        transform.position = next;

        TickShove(dt, state);

        if (state != null)
        {
            if (health.Value != state.Health.Current) health.Value = state.Health.Current;

            int flags = (int)StatusFlagsUtil.Of(state.Status);
            if (state.Status.HasWeapon) flags |= (int)StatusFlags.Weapon;
            if (state.Status.IsDriving) flags |= (int)StatusFlags.Vehicle;
            if (state.Status.IsBurning) flags |= (int)StatusFlags.Burning;
            string disguise = state.IsAlive ? state.Status.StealthDisguise ?? string.Empty : string.Empty;
            if (disguiseSprite.Value.ToString() != disguise) disguiseSprite.Value = new FixedString32Bytes(disguise);
            if (Mathf.Abs(charLevel.Value - state.Status.CharLevel) > 0.05f) charLevel.Value = state.Status.CharLevel;
            string carSprite = state.IsAlive && state.Status.IsDriving ? SpriteLibrary.NameFor(state.Status.Vehicle) : string.Empty;
            if (vehicleSprite.Value.ToString() != carSprite) vehicleSprite.Value = new FixedString32Bytes(carSprite);
            if (state.Status.IsDriving) vehicleSize.Value = VehicleRules.BodyRadius(state.Status.Vehicle, config) * 2f;
            if (state.Falling == FallKind.Chasm) flags |= (int)StatusFlags.FallChasm;
            else if (state.Falling == FallKind.Lava) flags |= (int)StatusFlags.FallLava;
            if (statusFlags.Value != flags) statusFlags.Value = flags;
        }
    }

    /// <summary>Host'ta itişin bekleme süresini ilerletir, arayüze seyrek bildirir ve botlara ara sıra itiş attırır.</summary>
    void TickShove(float dt, PlayerState state)
    {
        if (shoveCooldown == null) return;

        shoveCooldown.Tick(dt);
        shoveSyncTimer += dt;
        if (shoveSyncTimer >= 0.25f)
        {
            shoveSyncTimer = 0f;
            if (Mathf.Abs(shoveLeft.Value - shoveCooldown.Remaining) > 0.2f || (shoveCooldown.IsReady && shoveLeft.Value > 0f))
                shoveLeft.Value = shoveCooldown.Remaining;
        }

        if (!isBot.Value || state == null || !state.IsAlive) return;

        shoveThinkTimer += dt;
        if (shoveThinkTimer < 0.25f) return;
        shoveThinkTimer = 0f;

        // Boss'a saldıran bot her düşünüşte vurmayı dener (önünde boss yoksa hak harcanmaz).
        if (botBossFacing.HasValue) { ServerTryShove(true); return; }

        if (shoveRandom.NextDouble() < config.botShoveChancePerSecond * 0.25f) ServerTryShove(true);
    }

    /// <summary>
    /// İtiş (host'ta): bekleme süresi doluysa, karakter yaşıyor ve uçmuyorsa bakılan yönde yakındakileri güçlü savurur.
    /// <paramref name="onlyIfVictims"/> true ise (botlar) önünde kimse yoksa hak harcanmaz.
    /// </summary>
    bool ServerTryShove(bool onlyIfVictims)
    {
        NetMatchSim sim = channel != null ? channel.Sim : null;
        PlayerState me = sim != null ? sim.FindPlayer(playerId.Value) : null;

        if (me != null && me.Status.IsDriving) return false; // Araç sürerken Boşluk bir şey yapmaz

        // Silahı olan Boşluk'la itmez, ateş eder.
        if (me != null && me.IsAlive && me.Status.HasWeapon)
        {
            WordEntry weapon = me.Status.Weapon;
            bool used = sim.TryFireWeapon(me, facing, onlyIfVictims);
            if (used && MeleeRules.IsMelee(weapon))
            {
                // Kılıç/sopa savuruşunu herkes görsün.
                swingSprite.Value = new FixedString32Bytes(SpriteLibrary.NameFor(weapon));
                swingDirection.Value = facing;
                swingReach.Value = MeleeRules.Reach(weapon, config);
                swingCount.Value++;
            }
            return used;
        }

        if (me == null || !me.IsAlive || me.Status.IsFlying || shoveCooldown == null || !shoveCooldown.IsReady) return false;

        shoveStates.Clear(); shoveAvatars.Clear(); shovePositions.Clear(); shoveSolid.Clear();
        int myIndex = -1;
        foreach (NetworkPlayer avatar in All)
        {
            if (avatar == null || !avatar.IsSpawned) continue;

            PlayerState state = sim.FindPlayer(avatar.PlayerId);
            if (avatar == this) myIndex = shoveAvatars.Count;
            shoveStates.Add(state);
            shoveAvatars.Add(avatar);
            shovePositions.Add(avatar.ServerPosition);
            shoveSolid.Add(state != null && state.IsAlive && !state.Status.IsFlying);
        }
        if (myIndex < 0) return false;

        ShoveRules.Find(myIndex, facing, shovePositions, shoveSolid, config.shoveRange, config.shoveHalfAngleDegrees, config.shoveDistance, shoveHits);

        // Önündeki boss'a da vurur (boss dövmek için bekleme kısa).
        int bosses = BossRules.HitBosses(sim.CreatureModels, me, shovePositions[myIndex], facing, config, apply: false);
        if (onlyIfVictims && shoveHits.Count == 0 && bosses == 0) return false;

        if (bosses > 0)
        {
            shoveCooldown.TryUse(BossRules.HitCooldown(me, config));
            BossRules.HitBosses(sim.CreatureModels, me, shovePositions[myIndex], facing, config, apply: true);
        }
        else shoveCooldown.TryUse();
        shoveLeft.Value = shoveCooldown.Remaining;
        shoveCount.Value++;
        foreach (ShoveHit hit in shoveHits) shoveAvatars[hit.Index].ApplyKnockback(hit.Push);
        return true;
    }

    Vector2 BotDirection(float dt, Vector2 at, NetMatchSim sim, PlayerState state)
    {
        if (botBrain == null) return Vector2.zero;
        if (sim == null || state == null) return botBrain.Think(dt, at, NoDangers);

        // Gezinme hedefi lav/uçurum/duvar (ve kenarları) olmasın.
        if (sim.Terrain != null && botBrain.IsBadTarget == null) botBrain.IsBadTarget = sim.Terrain.IsBadWanderTarget;

        // Büyük tehdit varken bot en yakın boş sığınağa koşar; sığındıysa orada kalır.
        IRefugeProvider refugeProvider = sim;
        Vector2? refuge = refugeProvider.TryGetRefuge(state, at, out Vector2 point) ? point : (Vector2?)null;
        bool sheltered = refugeProvider.IsSheltered(state);

        // Elinde kılıç/sopa varsa en yakın rakibi kovalar.
        botBossFacing = null;
        if (!refuge.HasValue && !sheltered && state.Status.HasWeapon && MeleeRules.IsMelee(state.Status.Weapon)
            && MeleeRules.TryFindPrey(state, at, sim, config.botBossAggroRange, out Vector2 prey))
        {
            Vector2 chase = botBrain.ThinkBossAttack(at, sim.Dangers, prey, 0f, out Vector2 faceToPrey);
            botBossFacing = faceToPrey;
            return chase;
        }

        // Büyük tehdit yoksa ve canı yetiyorsa yakındaki boss'a saldırır.
        CreatureModel boss = refuge.HasValue || sheltered ? null : BossRules.PickBossToAttack(sim.CreatureModels, state, at, config);
        if (boss != null)
        {
            Vector2 move = botBrain.ThinkBossAttack(at, sim.Dangers, boss.Position, boss.Radius, out Vector2 face);
            botBossFacing = face;
            return move;
        }
        return botBrain.Think(dt, at, sim.Dangers, refuge, sheltered);
    }

    Vector2 MoveBy(Vector2 from, Vector2 delta, IObstacleField obstacles)
    {
        return ObstacleMover.Move(from, from + delta, config.playerRadius, arena, config.playerRadius, obstacles);
    }

    /// <summary>Bu bilgisayardaki oyuncunun tuşlarını okur; değiştiyse host'a bildirir.</summary>
    void SendInput()
    {
        bool typing = IsTypingTurn();
        Vector2 input = typing ? Vector2.zero : ReadMoveInput();

        // Boşluk: itiş. Yazma sırasında ve elenince çalışmaz.
        var keyboard = Keyboard.current;
        if (shoveEdge.Update(keyboard != null && keyboard.spaceKey.isPressed) && !typing && !IsGhost)
        {
            if (IsServer) ServerTryShove(false);
            else ShoveRpc();
        }

        if (IsServer)
        {
            serverInput = input; // Host kendi karakterini doğrudan sürer.
            return;
        }

        if (input == lastSentInput) return;
        lastSentInput = input;
        MoveInputRpc(input);
    }

    /// <summary>Sıra bu oyuncudaysa (zar atıldığından yazma bitene kadar) karakter durur; WASD kutuya harf olarak girmesin.</summary>
    bool IsTypingTurn()
    {
        if (channel == null) channel = FindAnyObjectByType<NetTurnChannel>();
        NetTurnClient client = channel != null ? channel.Client : null;
        if (client == null || client.Writer == null) return false;

        bool phase = client.Phase == TurnPhase.Dice || client.Phase == TurnPhase.Typing;
        return phase && client.Writer.Id == playerId.Value;
    }

    static Vector2 ReadMoveInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

        return new Vector2(x, y).normalized;
    }

    /// <summary>Sahibi olan istemci "itiş attım" der; bekleme süresini ve etkiyi host hesaplar.</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void ShoveRpc()
    {
        if (isBot.Value) return;
        if (!shoveLimiter.Allow(0, Time.unscaledTime)) return;
        ServerTryShove(false);
    }

    /// <summary>Sahibi olan istemci host'a yönünü söyler. Host değeri her seferinde temizler (<see cref="AvatarMover.SanitizeInput"/>).</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void MoveInputRpc(Vector2 input)
    {
        if (isBot.Value) return;
        if (!moveLimiter.Allow(0, Time.unscaledTime)) return; // Bu karakterin tek sahibi var; tek kova yeter.
        serverInput = AvatarMover.SanitizeInput(input);
    }
}
