using UnityEngine;

/// <summary>
/// "explosive": gecikmeli alan hasarı. Yaratık koşmaz; hedefin yanına düşer, fitil yanarken ekranda geri sayım görünür,
/// süre dolunca yarıçapındaki herkese hasar verir (yazan dahil). Uyarı süresi ve yarıçap kelimenin değerlerinden
/// ("stats.warningSeconds", "stats.radius") gelir. Fitil süresinde patlama alanının dışına çıkarak kaçılabilir.
/// Yumruktaki (instant) uyarı-vuruş mekanizmasının aynısını kullanır; farkı yerleşimi ve geri sayımıdır.
/// Patlama ateş hasarıdır: hasar alan tutuşur ve kömürleşir (ölürse kömürleşmiş ceset olarak düşer).
/// "trap" etiketiyle birlikte (mayın gibi) fitil kurulmaz; tuzak basılınca patlar ve yine tutuşturur.
/// </summary>
public class ExplosiveTag : ITagBehaviour
{
    public string Tag => "explosive";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        GameConfig config = context.Config;
        if (config == null) return;

        // Patlama ateşi: vurduğunu tutuşturur (haritadaki tahtaları yakmaz).
        creature.IgnitesPlayersOnHit = true;
        creature.BurnSecondsOnHit = config.burnSeconds;
        creature.CharPerHit = config.charPerFireHit;

        // Patlayan tuzak fitil beklemez; basılınca patlar.
        if (creature.Word != null && creature.Word.HasTag("trap")) return;

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
