using UnityEngine;

/// <summary>
/// "instant": anında etki. Yaratık koşmaz; hedefin o anki yerinde uyarı alanı açar, uyarı süresi dolunca
/// alanın içindeki herkese vurur. Alanın yarıçapı ve uyarı süresi kelimenin değerlerinden gelir
/// ("stats.radius", "stats.warningSeconds"). Alan hedefi takip etmez, uyarı süresinde kaçılabilir.
/// </summary>
public class InstantTag : ITagBehaviour
{
    public string Tag => "instant";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        GameConfig config = context.Config;
        if (config == null) return;

        WordStats stats = creature.Word?.stats;
        float warning = Mathf.Max(config.strikeMinWarningSeconds, stats != null ? stats.warningSeconds : 0f);
        float radius = stats != null && stats.radius > 0f ? stats.radius : creature.Radius;

        creature.Radius = radius;
        creature.StrikeWarningSeconds = warning;
        creature.EnsureLifeAtLeast(warning + config.strikeLingerSeconds); // Vuruş ve iz için süre kalsın.
    }
}
