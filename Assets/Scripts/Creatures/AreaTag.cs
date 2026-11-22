/// <summary>
/// "area": alan etkisi. Etki, yaratığa temas eden tek hedefe değil, yarıçapındaki herkese uygulanır.
/// Koşan yaratıkta (ör. yağmur bulutu) sürekli bir alana dönüşür: hedefi yavaşça takip eder, içindeki herkesi etkiler.
/// Alan vuruşlarında (instant) zaten alan etkisi olduğu için bunlarda bir şey değiştirmez (bkz. CreatureModel.IsArea).
/// Yarıçap kelimenin "stats.radius" değerinden, alandan çıkınca etkinin kalma süresi GameConfig'ten gelir.
/// </summary>
public class AreaTag : ITagBehaviour
{
    public string Tag => "area";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.AreaRequested = true;

        float radius = creature.Word?.stats != null ? creature.Word.stats.radius : 0f;
        if (radius > 0f) creature.Radius = radius;

        if (context.Config != null) creature.AreaLingerSeconds = context.Config.areaLingerSeconds;
    }
}
