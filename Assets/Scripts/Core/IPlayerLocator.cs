using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maçtaki oyuncuları ve sahnedeki konumlarını veren kaynak. Alan etkili yaratıklar (anında vuran yumruk gibi)
/// kimin alanın içinde olduğunu bundan öğrenir. Gerçek uygulaması <see cref="MatchBootstrap"/>; testlerde sahte bir tane kullanılır.
/// </summary>
public interface IPlayerLocator
{
    /// <summary>Maçtaki herkes (elenenler dahil).</summary>
    IReadOnlyList<PlayerState> Players { get; }

    /// <summary>Oyuncunun konumunu verir; oyuncu yoksa false.</summary>
    bool TryGetPosition(int playerId, out Vector2 position);
}
