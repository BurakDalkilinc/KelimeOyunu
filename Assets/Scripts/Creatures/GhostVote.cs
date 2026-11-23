using System;
using System.Collections.Generic;

/// <summary>
/// Hayaletlerin "kim yeniden doğsun?" oylaması. Adaylar elenmiş oyuncular; oy verebilenler elenmiş insan oyuncular.
/// En çok oyu alan kazanır; eşitlikte eşit olanlardan biri, hiç oy yoksa adaylardan biri rastgele seçilir.
/// Düz C# sınıfı; süre ve oy doğrulaması burada, ekran ve fare bağlantısı ağ katmanındadır.
/// </summary>
public class GhostVote
{
    readonly List<PlayerState> candidates;
    readonly HashSet<int> voterIds;
    readonly Dictionary<int, int> votes = new Dictionary<int, int>(); // oy veren → aday

    public PlayerState Winner { get; private set; }
    public float TimeLeft { get; private set; }

    /// <summary>Oylama bitti mi? (Süre doldu, herkes oy verdi ya da hiç oy veren yok.)</summary>
    public bool IsFinished => Winner != null;

    public IReadOnlyList<PlayerState> Candidates => candidates;

    /// <summary>Oy verebilecek hayaletlerin sayısı.</summary>
    public int VoterCount => voterIds.Count;

    public int VotesCast => votes.Count;

    /// <param name="candidates">Dirilebilecek hayaletler (en az biri).</param>
    /// <param name="voterIds">Oy verebilecek hayaletlerin numaraları (botlar oy vermez). Boşsa oylama hemen rastgele biter.</param>
    public GhostVote(IEnumerable<PlayerState> candidates, IEnumerable<int> voterIds, float seconds, Random random)
    {
        this.candidates = new List<PlayerState>(candidates ?? throw new ArgumentNullException(nameof(candidates)));
        if (this.candidates.Count == 0) throw new ArgumentException("En az bir aday gerekir.", nameof(candidates));

        this.voterIds = new HashSet<int>(voterIds ?? throw new ArgumentNullException(nameof(voterIds)));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        TimeLeft = Math.Max(0f, seconds);

        if (this.voterIds.Count == 0 || TimeLeft <= 0f) Finish();
    }

    readonly Random random;

    /// <summary>
    /// Bir hayaletin oyunu kaydeder (değiştirmek serbest). Oy veren oy hakkına sahip değilse, aday listede değilse
    /// ya da oylama bittiyse reddedilir. Herkes oy verince oylama biter.
    /// </summary>
    public bool TryVote(int voterId, int candidateId)
    {
        if (IsFinished || !voterIds.Contains(voterId) || !IsCandidate(candidateId)) return false;

        votes[voterId] = candidateId;
        if (votes.Count == voterIds.Count) Finish();
        return true;
    }

    /// <summary>Süreyi ilerletir; dolunca oylama biter.</summary>
    public void Tick(float deltaSeconds)
    {
        if (IsFinished) return;

        TimeLeft -= Math.Max(0f, deltaSeconds);
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            Finish();
        }
    }

    /// <summary>Adayın şu anki oy sayısı.</summary>
    public int VotesFor(int candidateId)
    {
        int count = 0;
        foreach (int chosen in votes.Values)
            if (chosen == candidateId) count++;
        return count;
    }

    bool IsCandidate(int id)
    {
        foreach (PlayerState candidate in candidates)
            if (candidate.Id == id) return true;
        return false;
    }

    void Finish()
    {
        int best = -1;
        var top = new List<PlayerState>();

        foreach (PlayerState candidate in candidates)
        {
            int count = VotesFor(candidate.Id);
            if (count > best)
            {
                best = count;
                top.Clear();
            }
            if (count == best) top.Add(candidate);
        }

        // Hiç oy yoksa herkes eşit ("en çok oy 0"): adaylardan biri rastgele seçilir.
        Winner = top[random.Next(top.Count)];
        TimeLeft = 0f;
    }
}
