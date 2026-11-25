using System;
using UnityEngine;

/// <summary>
/// Tur döngüsü: zar → yazma → sonuç → eleme kontrolü → tekrar zar, tek kişi kalana kadar.
/// Düz C# sınıfı; zamanı dışarıdan <see cref="Tick"/> ile alır, bu yüzden testlerde saniyeleri elle ilerletebiliriz.
/// Kelime doğrulaması burada (sunucu tarafında) yapılır; istemci yalnızca "şunu yazdım" der.
/// </summary>
public class TurnManager : ITurnView
{
    readonly GameConfig config;
    readonly MatchState match;
    readonly Dice dice;
    readonly MatchWordTracker words;

    /// <summary>Aşama her değiştiğinde çağrılır.</summary>
    public event Action<TurnPhase> PhaseChanged;

    /// <summary>Zar atıldı ve yazacak oyuncu belli oldu.</summary>
    public event Action<PlayerState> WriterSelected;

    /// <summary>Kelime kabul edildi. Bilinmeyen kelimede <c>WordEntry</c> null'dır ("???" yaratığı).</summary>
    public event Action<PlayerState, WordEntry> WordPlayed;

    /// <summary>Yazan oyuncu süreyi kaçırdı ya da yazarken elendi; tur yandı.</summary>
    public event Action<PlayerState> TurnSkipped;

    /// <summary>Maç bitti. Kazanan yoksa (herkes aynı anda elendi) null.</summary>
    public event Action<PlayerState> MatchEnded;

    public TurnPhase Phase { get; private set; } = TurnPhase.NotStarted;

    /// <summary>Bu turda yazacak oyuncu (zar atıldıktan sonra dolu).</summary>
    public PlayerState Writer { get; private set; }

    /// <summary>Bu turda oynanan kelime; bilinmeyen kelime ya da kelime yoksa null.</summary>
    public WordEntry CurrentWord { get; private set; }

    /// <summary>Şu anki aşamanın bitmesine kalan süre (sn).</summary>
    public float PhaseTimeLeft { get; private set; }

    /// <summary>Kaçıncı tur (ilk zar atılınca 1 olur).</summary>
    public int TurnNumber { get; private set; }

    /// <summary>Maç bittiğinde kazanan.</summary>
    public PlayerState Winner { get; private set; }

    bool IsRunning => Phase == TurnPhase.Dice || Phase == TurnPhase.Typing || Phase == TurnPhase.Result;

    public TurnManager(GameConfig config, MatchState match, Dice dice, MatchWordTracker words)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.match = match ?? throw new ArgumentNullException(nameof(match));
        this.dice = dice ?? throw new ArgumentNullException(nameof(dice));
        this.words = words ?? throw new ArgumentNullException(nameof(words));
    }

    /// <summary>Maçı başlatır ve ilk zarı atar. Baştan 2'den az oyuncu varsa maç hemen biter.</summary>
    public void Start()
    {
        if (Phase != TurnPhase.NotStarted) return;

        if (match.AliveCount < 2)
        {
            EndMatch();
            return;
        }

        BeginDice();
    }

    /// <summary>
    /// Zamanı ilerletir. Büyük bir adım birden fazla aşamayı geçebilir; artan süre bir sonraki aşamaya aktarılır.
    /// Hayatta tek kişi kalınca ya da yazan oyuncu yazarken elenince burada fark edilir.
    /// </summary>
    public void Tick(float deltaSeconds)
    {
        float remaining = Mathf.Max(0f, deltaSeconds);

        // Sonsuz döngüye karşı güvence: tek Tick'te en fazla bu kadar aşama geçilir.
        for (int guard = 0; guard < 100 && IsRunning; guard++)
        {
            if (match.IsOver)
            {
                EndMatch();
                return;
            }

            if (Phase == TurnPhase.Typing && !Writer.IsAlive)
            {
                SkipTurn();
                continue;
            }

            if (remaining < PhaseTimeLeft)
            {
                PhaseTimeLeft -= remaining;
                return;
            }

            remaining -= PhaseTimeLeft;
            AdvancePhase();
        }
    }

    /// <summary>
    /// Oyuncunun yazdığı kelimeyi dener. Yalnızca yazma aşamasında ve yalnızca seçilen oyuncudan kabul edilir.
    /// Boş/uzun/yasaklı/kullanılmış kelime reddedilir, süre işler ve oyuncu tekrar deneyebilir.
    /// </summary>
    public SubmitStatus SubmitWord(int playerId, string text)
    {
        if (Phase != TurnPhase.Typing) return SubmitStatus.WrongPhase;
        if (Writer == null || Writer.Id != playerId) return SubmitStatus.NotWriter;
        if (!Writer.IsAlive) return SubmitStatus.WrongPhase;

        WordLookupResult result = words.Use(text);
        switch (result.Status)
        {
            case WordLookupStatus.Empty: return SubmitStatus.Empty;
            case WordLookupStatus.TooLong: return SubmitStatus.TooLong;
            case WordLookupStatus.Blocked: return SubmitStatus.Blocked;
            case WordLookupStatus.AlreadyUsed: return SubmitStatus.AlreadyUsed;
            case WordLookupStatus.Unknown:
                BeginResult(null);
                return SubmitStatus.AcceptedUnknown;
            default:
                BeginResult(result.Entry);
                return SubmitStatus.Accepted;
        }
    }

    void AdvancePhase()
    {
        switch (Phase)
        {
            case TurnPhase.Dice:
                BeginTyping();
                break;
            case TurnPhase.Typing:
                SkipTurn(); // Süre doldu, tur yandı.
                break;
            case TurnPhase.Result:
                EndTurn();
                break;
        }
    }

    void BeginDice()
    {
        TurnNumber++;
        CurrentWord = null;

        Writer = dice.Roll(match.AlivePlayers);
        foreach (PlayerState player in match.AlivePlayers)
            player.MissedTurns = player == Writer ? 0 : player.MissedTurns + 1;

        SetPhase(TurnPhase.Dice, config.diceSeconds);
        WriterSelected?.Invoke(Writer);
    }

    void BeginTyping()
    {
        SetPhase(TurnPhase.Typing, config.typingSeconds);
    }

    /// <summary>
    /// Deneme aracı için: sırayı beklemeden ve kelimeyi kilitlemeden, <paramref name="writer"/> yazmış gibi kelimeyi oynatır
    /// (yaratıklar, tehditler, kendine etkiler normal yoldan çıkar). Maç sürmüyorsa ya da yazan elenmişse bir şey yapmaz.
    /// </summary>
    public bool DebugPlay(PlayerState writer, WordEntry entry)
    {
        if (!IsRunning || writer == null || !writer.IsAlive || entry == null) return false;
        Writer = writer;
        BeginResult(entry);
        return true;
    }

    void BeginResult(WordEntry entry)
    {
        CurrentWord = entry;
        SetPhase(TurnPhase.Result, GetResultSeconds(entry));
        WordPlayed?.Invoke(Writer, entry);
    }

    /// <summary>Sonuç süresi kelimenin süresine göre, GameConfig'teki alt ve üst sınır arasında tutulur.</summary>
    float GetResultSeconds(WordEntry entry)
    {
        if (entry == null || entry.stats == null) return config.resultSecondsMin;
        return Mathf.Clamp(entry.stats.duration, config.resultSecondsMin, config.resultSecondsMax);
    }

    void SkipTurn()
    {
        TurnSkipped?.Invoke(Writer);
        EndTurn();
    }

    void EndTurn()
    {
        if (match.IsOver)
            EndMatch();
        else
            BeginDice();
    }

    void EndMatch()
    {
        Winner = match.Winner;
        Winner?.MakeInvulnerable(); // Maç bitti: kazanan artık ölemez
        SetPhase(TurnPhase.MatchOver, 0f);
        MatchEnded?.Invoke(Winner);
    }

    void SetPhase(TurnPhase phase, float seconds)
    {
        Phase = phase;
        PhaseTimeLeft = seconds;
        PhaseChanged?.Invoke(phase);
    }
}
