using System;
using System.Collections.Generic;

/// <summary>
/// Ağ maçının istemci tarafı: host'tan gelen <see cref="TurnMessage"/>'larla tur durumunun bir kopyasını tutar ve
/// <see cref="TurnManager"/> ile aynı şekilli olayları yayar. Hiçbir kural işletmez; host ne derse odur.
/// Mesajlar arasında kalan süreyi kendisi geri sayar. Düz C# sınıfı, ağ kütüphanesini bilmez.
/// </summary>
public class NetTurnClient
{
    readonly WordDatabase words;
    readonly Dictionary<int, PlayerState> players = new Dictionary<int, PlayerState>();

    public event Action<TurnPhase> PhaseChanged;
    public event Action<PlayerState> WriterSelected;
    public event Action<PlayerState, WordEntry> WordPlayed;
    public event Action<PlayerState> TurnSkipped;
    public event Action<PlayerState> MatchEnded;

    public TurnPhase Phase { get; private set; } = TurnPhase.NotStarted;
    public PlayerState Writer { get; private set; }
    public WordEntry CurrentWord { get; private set; }
    public float PhaseTimeLeft { get; private set; }
    public int TurnNumber { get; private set; }
    public PlayerState Winner { get; private set; }

    /// <summary>Host'un bildirdiği oyuncular (numaraya göre).</summary>
    public IReadOnlyDictionary<int, PlayerState> Players => players;

    public NetTurnClient(WordDatabase words)
    {
        this.words = words ?? throw new ArgumentNullException(nameof(words));
    }

    /// <summary>Oyuncu listesini kurar (host'tan gelen liste). Eski liste silinir.</summary>
    public void SetRoster(IEnumerable<RosterEntry> roster, int startingHealth)
    {
        players.Clear();
        foreach (RosterEntry entry in roster)
            players[entry.Id] = new PlayerState(entry.Id, entry.Name, startingHealth, entry.IsBot);
    }

    /// <summary>Host'tan gelen bir mesajı uygular: önce fotoğrafı kopyalar, sonra olayı yayar.</summary>
    public void Apply(TurnMessage message)
    {
        TurnPhase previousPhase = Phase;

        TurnNumber = message.TurnNumber;
        PhaseTimeLeft = message.PhaseTimeLeft;
        Writer = Find(message.WriterId);
        CurrentWord = FindWord(message.WordId);
        Winner = Find(message.WinnerId);
        Phase = message.Phase;

        if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);

        switch (message.Kind)
        {
            case TurnMessageKind.WriterSelected: WriterSelected?.Invoke(Writer); break;
            case TurnMessageKind.WordPlayed: WordPlayed?.Invoke(Writer, CurrentWord); break;
            case TurnMessageKind.TurnSkipped: TurnSkipped?.Invoke(Writer); break;
            case TurnMessageKind.MatchEnded: MatchEnded?.Invoke(Winner); break;
        }
    }

    /// <summary>Mesajlar arasında kalan süreyi geri sayar.</summary>
    public void Tick(float deltaSeconds)
    {
        if (Phase == TurnPhase.NotStarted || Phase == TurnPhase.MatchOver) return;
        PhaseTimeLeft = Math.Max(0f, PhaseTimeLeft - deltaSeconds);
    }

    /// <summary>Host oyuncunun bota dönüştüğünü bildirdi (bağlantısı koptu).</summary>
    public void MarkBot(int playerId)
    {
        if (players.TryGetValue(playerId, out PlayerState player)) player.BecomeBot();
    }

    PlayerState Find(int id)
    {
        return id != TurnMessage.NoPlayer && players.TryGetValue(id, out PlayerState player) ? player : null;
    }

    WordEntry FindWord(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (WordEntry entry in words.Words)
            if (entry.id == id) return entry;
        return null;
    }
}
