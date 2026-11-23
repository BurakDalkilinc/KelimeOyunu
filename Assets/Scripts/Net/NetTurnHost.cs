using System;
using System.Collections.Generic;

/// <summary>
/// Ağ maçının host tarafı: gerçek <see cref="TurnManager"/>'ı çalıştırır, olaylarını <see cref="TurnMessage"/>'a çevirip yayar.
/// Zar, tur, kelime doğrulaması ve bot yazımı yalnızca burada olur; istemci sadece "şunu yazdım" der (<see cref="Submit"/>).
/// Düz C# sınıfı: zamanı dışarıdan <see cref="Tick"/> ile alır, ağ kütüphanesini bilmez.
/// </summary>
public class NetTurnHost
{
    /// <summary>Durum mesajının tekrar gönderilme aralığı (sn). Kaybolan/geç gelen mesajı telafi eder.</summary>
    public const float HeartbeatSeconds = 1f;

    readonly GameConfig config;
    readonly WordDatabase words;
    readonly MatchWordTracker tracker;
    readonly Random botRandom;
    readonly List<RosterEntry> roster;

    float heartbeatLeft;
    bool botWaiting;
    float botWaitLeft;

    /// <summary>Yayılacak her mesaj için çağrılır (ağ katmanı buraya bağlanır).</summary>
    public event Action<TurnMessage> MessageReady;

    public TurnManager Turns { get; }
    public MatchState Match { get; }
    public IReadOnlyList<RosterEntry> Roster => roster;

    public NetTurnHost(GameConfig config, WordDatabase words, Random random, IEnumerable<RosterEntry> roster)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.words = words ?? throw new ArgumentNullException(nameof(words));
        if (random == null) throw new ArgumentNullException(nameof(random));
        if (roster == null) throw new ArgumentNullException(nameof(roster));

        this.roster = new List<RosterEntry>(roster);
        var states = new List<PlayerState>();
        foreach (RosterEntry entry in this.roster)
            states.Add(new PlayerState(entry.Id, entry.Name, config.startingHealth, entry.IsBot));

        Match = new MatchState(states);
        tracker = new MatchWordTracker(words);
        botRandom = new Random(random.Next());
        Turns = new TurnManager(config, Match, new Dice(random, config.diceBonusPerMissedTurn), tracker);

        Turns.PhaseChanged += OnPhaseChanged;
        Turns.WriterSelected += _ => Publish(TurnMessageKind.WriterSelected);
        Turns.WordPlayed += (_, __) => Publish(TurnMessageKind.WordPlayed);
        Turns.TurnSkipped += _ => Publish(TurnMessageKind.TurnSkipped);
        Turns.MatchEnded += _ => Publish(TurnMessageKind.MatchEnded);
    }

    /// <summary>Maçı başlatır (ilk zar atılır). Mesajlar bu çağrıdan itibaren yayılır, o yüzden önce olaya bağlanın.</summary>
    public void Start()
    {
        Turns.Start();
    }

    /// <summary>Zamanı ilerletir: tur döngüsü, bot yazımı ve düzenli durum mesajı.</summary>
    public void Tick(float deltaSeconds)
    {
        Turns.Tick(deltaSeconds);
        TickBotTyping(deltaSeconds);

        heartbeatLeft -= deltaSeconds;
        if (heartbeatLeft <= 0f && Turns.Phase != TurnPhase.NotStarted && Turns.Phase != TurnPhase.MatchOver)
            Publish(TurnMessageKind.State);
    }

    /// <summary>
    /// Bir oyuncunun yazdığı kelimeyi dener. <paramref name="senderPlayerId"/> istemcinin söylediği değil,
    /// ağ katmanının bağlantıdan bulduğu numaradır; sırası olmayan oyuncu reddedilir (<see cref="SubmitStatus.NotWriter"/>).
    /// </summary>
    public SubmitStatus Submit(int senderPlayerId, string text)
    {
        return Turns.SubmitWord(senderPlayerId, text);
    }

    /// <summary>
    /// Bağlantısı kopan insan oyuncuyu bota çevirir. Sırası o oyuncudaysa ve yazma aşamasındaysa kelimeyi bot yazar.
    /// Oyuncu yoksa ya da zaten botsa false.
    /// </summary>
    public bool ReplaceWithBot(int playerId)
    {
        PlayerState player = null;
        foreach (PlayerState candidate in Match.Players)
            if (candidate.Id == playerId) player = candidate;

        if (player == null || player.IsBot) return false;

        player.BecomeBot();
        if (Turns.Phase == TurnPhase.Typing && Turns.Writer == player) ArmBotTyping();

        Publish(TurnMessageKind.State);
        return true;
    }

    void OnPhaseChanged(TurnPhase phase)
    {
        botWaiting = false;
        if (phase == TurnPhase.Typing && Turns.Writer != null && Turns.Writer.IsBot) ArmBotTyping();

        Publish(TurnMessageKind.State);
    }

    void ArmBotTyping()
    {
        float min = Math.Min(config.botTypingDelayMin, config.botTypingDelayMax);
        float max = Math.Max(config.botTypingDelayMin, config.botTypingDelayMax);
        botWaiting = true;
        botWaitLeft = min + (float)botRandom.NextDouble() * (max - min);
    }

    void TickBotTyping(float deltaSeconds)
    {
        if (!botWaiting) return;

        botWaitLeft -= deltaSeconds;
        if (botWaitLeft > 0f) return;

        botWaiting = false;
        string text = BotWordChooser.Choose(words.Words, tracker.IsUsed, botRandom);
        Turns.SubmitWord(Turns.Writer.Id, text);
    }

    void Publish(TurnMessageKind kind)
    {
        heartbeatLeft = HeartbeatSeconds;

        MessageReady?.Invoke(new TurnMessage
        {
            Kind = kind,
            Phase = Turns.Phase,
            TurnNumber = Turns.TurnNumber,
            WriterId = Turns.Writer != null ? Turns.Writer.Id : TurnMessage.NoPlayer,
            PhaseTimeLeft = Turns.PhaseTimeLeft,
            WordId = Turns.CurrentWord != null ? Turns.CurrentWord.id : "",
            WinnerId = Turns.Winner != null ? Turns.Winner.Id : TurnMessage.NoPlayer,
        });
    }
}
