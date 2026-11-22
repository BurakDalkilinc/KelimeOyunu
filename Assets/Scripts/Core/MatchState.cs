using System;
using System.Collections.Generic;

/// <summary>
/// Bir maçtaki tüm oyuncular ve kimin hayatta olduğu. Maç, 1 ya da 0 oyuncu kalınca biter.
/// Düz C# sınıfı; ağdan bağımsız olduğu için sunucuda da aynen kullanılabilir.
/// </summary>
public class MatchState
{
    readonly List<PlayerState> players;
    readonly List<PlayerState> aliveBuffer = new List<PlayerState>();

    /// <summary>Maçtaki herkes (elenenler dahil), katılma sırasıyla.</summary>
    public IReadOnlyList<PlayerState> Players => players;

    public MatchState(IEnumerable<PlayerState> players)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        this.players = new List<PlayerState>(players);

        var ids = new HashSet<int>();
        foreach (PlayerState player in this.players)
        {
            if (player == null) throw new ArgumentException("Oyuncu listesinde null var.", nameof(players));
            if (!ids.Add(player.Id)) throw new ArgumentException($"{player.Id} numaralı oyuncu iki kez eklenmiş.", nameof(players));
        }
    }

    /// <summary>Hayattaki oyuncular. Her çağrıda güncel liste verir; sonucu saklamayın.</summary>
    public IReadOnlyList<PlayerState> AlivePlayers
    {
        get
        {
            aliveBuffer.Clear();
            foreach (PlayerState player in players)
                if (player.IsAlive) aliveBuffer.Add(player);
            return aliveBuffer;
        }
    }

    public int AliveCount
    {
        get
        {
            int count = 0;
            foreach (PlayerState player in players)
                if (player.IsAlive) count++;
            return count;
        }
    }

    /// <summary>Maç bitti mi? (Hayatta 1 ya da 0 oyuncu kaldı.)</summary>
    public bool IsOver => AliveCount <= 1;

    /// <summary>Kazanan; maç bitmediyse ya da herkes elendiyse null.</summary>
    public PlayerState Winner
    {
        get
        {
            PlayerState winner = null;
            foreach (PlayerState player in players)
            {
                if (!player.IsAlive) continue;
                if (winner != null) return null;
                winner = player;
            }
            return winner;
        }
    }

    /// <summary>Numarasına göre oyuncuyu bulur; yoksa null.</summary>
    public PlayerState GetPlayer(int id)
    {
        foreach (PlayerState player in players)
            if (player.Id == id) return player;
        return null;
    }
}
