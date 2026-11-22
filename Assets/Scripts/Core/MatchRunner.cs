using UnityEngine;

/// <summary>
/// Tur sistemini sahnede çalıştırır: kelime veritabanını yükler, zarı ve <see cref="TurnManager"/>'ı kurar,
/// her karede zamanı ilerletir. Oyun mantığının kendisi TurnManager'dadır; burası yalnızca bağlayıcıdır.
/// </summary>
[DefaultExecutionOrder(-50)] // MatchBootstrap'tan sonra, ekran bileşenlerinden önce.
public class MatchRunner : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchBootstrap bootstrap;

    /// <summary>Tur döngüsü. Awake'ten sonra hazırdır.</summary>
    public TurnManager Turns { get; private set; }

    /// <summary>Yüklenen kelimeler.</summary>
    public WordDatabase Words { get; private set; }

    /// <summary>Bu maçta kullanılan kelimelerin kilidi.</summary>
    public MatchWordTracker WordTracker { get; private set; }

    public MatchState Match => bootstrap.Match;

    /// <summary>Maçın ortak rastgelelik kaynağı.</summary>
    public System.Random Random => bootstrap.Random;

    void Awake()
    {
        if (config == null || bootstrap == null)
        {
            Debug.LogError("MatchRunner: config ya da bootstrap atanmamış.", this);
            enabled = false;
            return;
        }

        Words = WordDatabase.LoadFromResources(config.maxWordLength);
        WordTracker = new MatchWordTracker(Words);

        var dice = new Dice(bootstrap.Random, config.diceBonusPerMissedTurn);
        Turns = new TurnManager(config, bootstrap.Match, dice, WordTracker);
    }

    void Start()
    {
        // Ekran bileşenleri OnEnable'da olaylara abone oldu; şimdi ilk zar atılabilir.
        Turns.Start();
    }

    void Update()
    {
        Turns.Tick(Time.deltaTime);
    }
}
