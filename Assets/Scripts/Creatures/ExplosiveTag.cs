using UnityEngine;

/// <summary>
/// "explosive": gecikmeli alan hasarı. Yaratık koşmaz; hedefin yanına düşer, fitil yanarken ekranda geri sayım görünür,
/// süre dolunca yarıçapındaki herkese hasar verir (yazan dahil). Uyarı süresi ve yarıçap kelimenin değerlerinden
/// ("stats.warningSeconds", "stats.radius") gelir. Fitil süresinde patlama alanının dışına çıkarak kaçılabilir.
/// Yumruktaki (instant) uyarı-vuruş mekanizmasının aynısını kullanır; farkı yerleşimi ve geri sayımıdır.
/// </summary>
public class ExplosiveTag : ITagBehaviour
{
    public string Tag => "explosive";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        GameConfig config = context.Config;
        if (config == null) return;

        WordStats stats = creature.Word?.stats;
        float fuse = Mathf.Max(config.strikeMinWarningSeconds, stats != null ? stats.warningSeconds : 0f);
        float radius = stats != null && stats.radius > 0f ? stats.radius : creature.Radius;

        creature.Radius = radius;
        creature.StrikeWarningSeconds = fuse;
        creature.PlacementOffset = config.explosiveDropDistance;
        creature.ShowsCountdown = true;
        creature.EnsureLifeAtLeast(fuse + config.strikeLingerSeconds); // Patlama ve iz için süre kalsın.
    }
}
