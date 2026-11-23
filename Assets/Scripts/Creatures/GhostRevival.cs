using System;
using System.Collections.Generic;

/// <summary>
/// Hayaleti yeniden doğurmanın ortak hesabı ve tek kişilik sahnenin diriliş hizmeti.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public class GhostRevival : IRevivalService
{
    readonly IReadOnlyList<PlayerState> players;
    readonly GameConfig config;
    readonly Random random;

    public GhostRevival(IReadOnlyList<PlayerState> players, GameConfig config, Random random)
    {
        this.players = players ?? throw new ArgumentNullException(nameof(players));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>Tek kişilikte oylayacak insan hayalet olmadığı için rastgele bir hayalet dirilir.</summary>
    public void RequestRevival(PlayerState writer)
    {
        PlayerState chosen = Pick(players, random);
        if (chosen != null) Revive(chosen, config);
    }

    /// <summary>Elenmişlerden rastgele biri; kimse yoksa null.</summary>
    public static PlayerState Pick(IReadOnlyList<PlayerState> players, Random random)
    {
        var ghosts = new List<PlayerState>();
        foreach (PlayerState player in players)
            if (player != null && player.IsGhost) ghosts.Add(player);
        return ghosts.Count == 0 ? null : ghosts[random.Next(ghosts.Count)];
    }

    /// <summary>Oyuncuyu <see cref="GameConfig.reviveHealth"/> canla geri getirir; üzerindeki eski etkiler silinir.</summary>
    public static void Revive(PlayerState player, GameConfig config)
    {
        if (player == null || !player.IsGhost) return;

        player.Status.Clear();
        player.Health.Revive(config.reviveHealth);
    }
}
