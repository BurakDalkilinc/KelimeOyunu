using System;
using System.Collections.Generic;

/// <summary>
/// Saldırı kelimesinin hedefini seçer: hayattaki rakiplerden biri. Yazan kişi kendini hedefleyemez.
/// (İnsan oyuncunun fareyle hedef seçmesi sonraki adımda eklenecek; botlar ve süre dolunca bu kullanılır.)
/// </summary>
public static class TargetPicker
{
    /// <summary>Yazan dışındaki hayattaki oyunculardan rastgele birini verir; kimse yoksa null.</summary>
    public static PlayerState PickRandomOpponent(IReadOnlyList<PlayerState> players, PlayerState writer, Random random)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        if (random == null) throw new ArgumentNullException(nameof(random));

        var candidates = new List<PlayerState>();
        foreach (PlayerState player in players)
            if (player != null && player.IsAlive && player != writer) candidates.Add(player);

        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
    }
}
