using System;
using System.Collections.Generic;

/// <summary>
/// Zar: bu turda kimin kelime yazacağını seçer. Kelimenin gücüyle ilgisi yoktur.
/// Ağırlık = 1 + bonus × (üst üste seçilmediği tur sayısı); böylece uzun süredir seçilmeyenin şansı artar.
/// Elenenler hiç seçilmez. Rastgelelik tek bir <see cref="Random"/> örneğinden gelir (seed'li, tekrar oynatılabilir).
/// </summary>
public class Dice
{
    readonly Random random;
    readonly float bonusPerMissedTurn;

    public Dice(Random random, float bonusPerMissedTurn)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        this.bonusPerMissedTurn = Math.Max(0f, bonusPerMissedTurn);
    }

    /// <summary>Oyuncunun bu atıştaki seçilme ağırlığı.</summary>
    public float GetWeight(PlayerState player)
    {
        return 1f + Math.Max(0, player.MissedTurns) * bonusPerMissedTurn;
    }

    /// <summary>
    /// Hayattaki adaylar arasından ağırlıklı rastgele birini seçer. Sayaçları değiştirmez
    /// (bunu <see cref="TurnManager"/> yapar). Hayatta aday yoksa hata fırlatır.
    /// </summary>
    public PlayerState Roll(IReadOnlyList<PlayerState> candidates)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));

        double total = 0;
        PlayerState lastAlive = null;
        foreach (PlayerState player in candidates)
        {
            if (!player.IsAlive) continue;
            total += GetWeight(player);
            lastAlive = player;
        }

        if (lastAlive == null)
            throw new InvalidOperationException("Zar atılamaz: hayatta oyuncu yok.");

        double target = random.NextDouble() * total;
        foreach (PlayerState player in candidates)
        {
            if (!player.IsAlive) continue;
            target -= GetWeight(player);
            if (target < 0) return player;
        }

        return lastAlive; // Yuvarlama hatası güvencesi.
    }
}
