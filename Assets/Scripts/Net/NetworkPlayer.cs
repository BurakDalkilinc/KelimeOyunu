using System.Collections.Generic;
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

    readonly KnockbackMotion knockback = new KnockbackMotion();

    /// <summary>Yön mesajı hız sınırı: normal oyunda yalnızca tuş değişince gider; seli engeller.</summary>
    readonly RpcRateLimiter moveLimiter = new RpcRateLimiter(30f, 30f);

    SpriteRenderer spriteRenderer;
    ArenaBounds arena;
    BotBrain botBrain;
    NetTurnChannel channel;
    int registeredId = -1;

    int preparedBotId = -1;
    Vector2 serverInput;
    Vector2 lastSentInput;
    Vector2 facing = Vector2.up;

    /// <summary>Bu karakterin maç numarası (insan: bağlantı numarası, bot: 100'den başlar).</summary>
    public int PlayerId => playerId.Value;

    public bool IsBot => isBot.Value;

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
        health.Value = config.startingHealth;
        statusFlags.Value = 0;

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
        spriteRenderer.sortingOrder = 10;

        var animator = gameObject.GetComponent<SpriteAnimator>();
        if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
        animator.SetFrames(SpriteLibrary.GetFrames("char"));

        playerId.OnValueChanged += (_, __) => ApplyLook();
        health.OnValueChanged += (_, __) => ApplyLook();
        statusFlags.OnValueChanged += (_, __) => ApplyLook();
        Everyone.Add(this);

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
    }

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
            IObstacleField obstacles = sim != null ? MovementRules.EffectiveObstacles(state, sim.Walls) : null;

            if (state != null)
            {
                // Fırlatılma ve mıknatıs çekimi, yazarken ya da hareketsizken de olur.
                Vector2 pushed = knockback.Step(dt);
                if (pushed != Vector2.zero) next = MoveBy(next, pushed, obstacles);

                Vector2 pulled = state.Status.GetPullStep(next, dt);
                if (pulled != Vector2.zero) next = MoveBy(next, pulled, obstacles);
            }

            float speedMultiplier = state != null ? state.Status.SpeedMultiplier : 1f;
            Vector2 input = isBot.Value ? BotDirection(dt, next, sim, state) : serverInput;
            if (input != Vector2.zero) facing = input.normalized;

            float speed = (isBot.Value ? config.botMoveSpeed : config.playerMoveSpeed) * speedMultiplier;
            next = AvatarMover.Step(next, input, speed, dt, config.playerRadius, arena, obstacles);

            // Duvar tam üstüne çıktıysa karakter dışarı itilir.
            if (obstacles != null)
            {
                Vector2 resolved = obstacles.Resolve(next, config.playerRadius);
                if (resolved != next) next = arena.Clamp(resolved, config.playerRadius);
            }
        }

        if (next != current) position.Value = next;
        transform.position = next;

        if (state != null)
        {
            if (health.Value != state.Health.Current) health.Value = state.Health.Current;

            int flags = (int)StatusFlagsUtil.Of(state.Status);
            if (statusFlags.Value != flags) statusFlags.Value = flags;
        }
    }

    Vector2 BotDirection(float dt, Vector2 at, NetMatchSim sim, PlayerState state)
    {
        if (botBrain == null) return Vector2.zero;
        if (sim == null || state == null) return botBrain.Think(dt, at, NoDangers);

        // Büyük tehdit varken bot en yakın boş sığınağa koşar; sığındıysa orada kalır.
        IRefugeProvider refugeProvider = sim;
        Vector2? refuge = refugeProvider.TryGetRefuge(state, at, out Vector2 point) ? point : (Vector2?)null;
        bool sheltered = refugeProvider.IsSheltered(state);
        return botBrain.Think(dt, at, sim.Dangers, refuge, sheltered);
    }

    Vector2 MoveBy(Vector2 from, Vector2 delta, IObstacleField obstacles)
    {
        return ObstacleMover.Move(from, from + delta, config.playerRadius, arena, config.playerRadius, obstacles);
    }

    /// <summary>Bu bilgisayardaki oyuncunun tuşlarını okur; değiştiyse host'a bildirir.</summary>
    void SendInput()
    {
        Vector2 input = IsTypingTurn() ? Vector2.zero : ReadMoveInput();

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

    /// <summary>Sahibi olan istemci host'a yönünü söyler. Host değeri her seferinde temizler (<see cref="AvatarMover.SanitizeInput"/>).</summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void MoveInputRpc(Vector2 input)
    {
        if (isBot.Value) return;
        if (!moveLimiter.Allow(0, Time.unscaledTime)) return; // Bu karakterin tek sahibi var; tek kova yeter.
        serverInput = AvatarMover.SanitizeInput(input);
    }
}
