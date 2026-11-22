using UnityEngine;

/// <summary>
/// Tüm haritayı vuran patlama (ör. atom bombası): uyarı bitince çözüm alanında korunmayan herkese kelimenin
/// "stats.damage" kadar hasar verir (genelde elenme). Kalkanı olan bir kez kurtulur. Anında biter.
/// </summary>
public class MapBlastEffect : IThreatEffect
{
    public bool IsFinished { get; private set; }

    /// <summary>Patlamanın vurduğu oyuncu sayısı (korunmayanlar).</summary>
    public int Affected { get; private set; }

    public void Begin(ThreatContext context)
    {
        WordStats stats = context.Word?.stats;
        int damage = stats != null ? Mathf.Max(1, Mathf.RoundToInt(stats.damage)) : 1;

        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive || context.Zones.IsProtected(player.Id)) continue;

            player.TakeDamage(damage);
            Affected++;
        }

        IsFinished = true;
    }

    public void Tick(float deltaSeconds) { }
}
