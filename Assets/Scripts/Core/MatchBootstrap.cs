using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maçı sahnede kurar: oyuncuyu ve botları <see cref="MatchState"/> içine koyar, botları arenanın içine dizer.
/// Oyuncu 0 numaradır, botlar 1'den başlar. Tur sistemi (TurnManager) bu maç durumunu kullanacak.
/// </summary>
[DefaultExecutionOrder(-100)] // Diğer her şeyden önce çalışır: maç durumu herkesin Awake/OnEnable'ından önce hazır olsun.
public class MatchBootstrap : MonoBehaviour, IPlayerLocator
{
    [SerializeField] GameConfig config;
    [SerializeField] PlayerController player;

    readonly List<DangerZone> dangers = new List<DangerZone>();
    readonly List<BotController> bots = new List<BotController>();
    readonly Dictionary<int, Transform> avatars = new Dictionary<int, Transform>();

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

    void Update()
    {
        Walls.Tick(Time.deltaTime);
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

        var states = new List<PlayerState>();
        var humanState = new PlayerState(0, "Sen", config.startingHealth);
        states.Add(humanState);
        for (int i = 1; i <= config.botCount; i++)
            states.Add(new PlayerState(i, "Bot " + i, config.startingHealth, isBot: true));

        Match = new MatchState(states);
        player.Bind(humanState);
        player.Obstacles = Walls;
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
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;

            var go = new GameObject(states[i].Name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            Color color = Color.HSVToRGB((i - 1) / (float)Mathf.Max(1, config.botCount), 0.55f, 0.95f);

            // Her bot kendi rastgelelik akışını maçın ana üreticisinden türetir (sıra sabit, seed'li).
            var botRandom = new System.Random(Random.Next());
            var bot = go.AddComponent<BotController>();
            bot.Initialize(states[i], config, arena, botRandom, dangers, color);
            bots.Add(bot);
            bot.Obstacles = Walls;
            avatars[states[i].Id] = go.transform;
        }
    }
}
