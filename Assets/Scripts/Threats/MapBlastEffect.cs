using UnityEngine;

/// <summary>
/// Tüm haritayı vuran patlama (ör. atom bombası): uyarı bitince çözüm alanında korunmayan herkese kelimenin
/// "stats.damage" kadar hasar verir (genelde elenme). Kalkanı olan bir kez kurtulur. Anında biter.
/// "explosive" etiketli kelimede hasar ateştir: vurulan tutuşur ve kömürleşir (ölürse kömürleşmiş ceset olarak düşer).
/// </summary>
public class MapBlastEffect : IThreatEffect
{
    public bool IsFinished { get; private set; }

    public bool NeedsWarning => true;

    /// <summary>Patlamanın vurduğu oyuncu sayısı (korunmayanlar).</summary>
    public int Affected { get; private set; }

    public void Begin(ThreatContext context)
    {
        WordStats stats = context.Word?.stats;
        int damage = stats != null ? Mathf.Max(1, Mathf.RoundToInt(stats.damage)) : 1;
        bool fiery = context.Word != null && context.Word.HasTag("explosive"); // Patlama ateş hasarı verir

        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive || context.Zones.IsProtected(player.Id)) continue;

            if (fiery) player.TakeFireDamage(damage, context.Config); // Kalkan/dokunulmazlık TakeDamage içinden işler
            else player.TakeDamage(damage);
            Affected++;
        }

        IsFinished = true;
    }

    public void Tick(float deltaSeconds) { }
}
