using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ağdaki bir karakter (insan ya da bot). Hareketi yalnızca host hesaplar:
/// sahibi olan istemci sadece "şu yöne basıyorum" der (<see cref="MoveInputRpc"/>), host hızı ve arena sınırını uygular,
/// konumu herkese <see cref="position"/> ile yayar. Botları da host sürer.
/// Hesap düz C# sınıflarındadır (<see cref="AvatarMover"/>, <see cref="BotBrain"/>); burası taşıyıcı ve görüntüdür.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    static readonly Color[] Palette =
    {
        new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.7f, 0.95f), new Color(0.55f, 0.9f, 0.55f), new Color(0.95f, 0.85f, 0.4f),
        new Color(0.75f, 0.55f, 0.95f), new Color(0.95f, 0.65f, 0.35f), new Color(0.5f, 0.9f, 0.85f), new Color(0.95f, 0.6f, 0.8f),
    };

    static readonly IReadOnlyList<DangerZone> NoDangers = new List<DangerZone>();

    /// <summary>Uzak karakterin hedef konuma yaklaşma sertliği (büyük = daha sıkı takip).</summary>
    const float FollowSharpness = 18f;

    [SerializeField] GameConfig config;

    readonly NetworkVariable<int> playerId = new NetworkVariable<int>(-1);
    readonly NetworkVariable<bool> isBot = new NetworkVariable<bool>(false);
    readonly NetworkVariable<Vector2> position = new NetworkVariable<Vector2>();

    SpriteRenderer spriteRenderer;
    ArenaBounds arena;
    BotBrain botBrain;
    NetTurnChannel channel;

    int preparedBotId = -1;
    Vector2 serverInput;
    Vector2 lastSentInput;

    /// <summary>Bu karakterin maç numarası (insan: bağlantı numarası, bot: 100'den başlar).</summary>
    public int PlayerId => playerId.Value;

    public bool IsBot => isBot.Value;

    /// <summary>Doğmadan önce çağrılır: bu karakter bir bot olacak. Yalnızca host'ta.</summary>
    public void PrepareAsBot(int botPlayerId)
    {
        preparedBotId = botPlayerId;
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

        if (IsServer) InitOnServer();

        transform.position = (Vector3)position.Value;
        ApplyLook();
        channel = FindAnyObjectByType<NetTurnChannel>();
    }

    void InitOnServer()
    {
        if (config == null) { Debug.LogError("NetworkPlayer: GameConfig atanmamış.", this); return; }

        arena = new ArenaBounds(config.arenaSize);

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
            playerId.Value = (int)OwnerClientId;
            int slot = (int)(OwnerClientId % (ulong)Palette.Length);
            position.Value = new Vector2((slot - 3.5f) * 1.5f, 0f);
        }
    }

    void ApplyLook()
    {
        if (spriteRenderer == null) return;
        int id = Mathf.Max(0, playerId.Value);
        spriteRenderer.color = Palette[id % Palette.Length];
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

        Vector2 current = position.Value;
        Vector2 next = current;

        if (isBot.Value)
        {
            if (botBrain != null)
            {
                Vector2 direction = botBrain.Think(dt, current, NoDangers);
                next = AvatarMover.Step(current, direction, config.botMoveSpeed, dt, config.playerRadius, arena, null);
            }
        }
        else
        {
            next = AvatarMover.Step(current, serverInput, config.playerMoveSpeed, dt, config.playerRadius, arena, null);
        }

        if (next != current) position.Value = next;
        transform.position = next;
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
        serverInput = AvatarMover.SanitizeInput(input);
    }
}
