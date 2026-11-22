using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maçı sahnede kurar: oyuncuyu ve botları <see cref="MatchState"/> içine koyar, botları arenanın içine dizer.
/// Oyuncu 0 numaradır, botlar 1'den başlar. Tur sistemi (TurnManager) bu maç durumunu kullanacak.
/// </summary>
public class MatchBootstrap : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] PlayerController player;

    readonly List<DangerZone> dangers = new List<DangerZone>();
    readonly List<BotController> bots = new List<BotController>();

    /// <summary>Maçın tüm oyuncuları.</summary>
    public MatchState Match { get; private set; }

    /// <summary>Maçın tek rastgelelik kaynağı (zar ve botlar bundan beslenir).</summary>
    public System.Random Random { get; private set; }

    /// <summary>Botların kaçacağı alanlar. Yaratıklar eklenince burayı onlar dolduracak.</summary>
    public IList<DangerZone> Dangers => dangers;

    public IReadOnlyList<BotController> Bots => bots;

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
        }
    }
}
