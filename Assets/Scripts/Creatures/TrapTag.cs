/// <summary>
/// "trap": tuzak. Yaratık koşmaz; hedefin yakınına düşüp yerinde bekler. Yarıçapına kim girerse (hedef olsun olmasın,
/// yazan dahil) kelimenin diğer etiketlerinin etkisi uygulanır (örn. "knockback": kayar) ve tuzak kaybolur.
/// Yarıçap kelimenin "stats.radius" değerinden, düşme mesafesi GameConfig'ten gelir.
/// Botlar tuzakları görmez, onlardan kaçmaz.
/// </summary>
public class TrapTag : ITagBehaviour
{
    public string Tag => "trap";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.IsTrap = true;

        float radius = creature.Word?.stats != null ? creature.Word.stats.radius : 0f;
        if (radius > 0f) creature.Radius = radius;

        if (context.Config != null) creature.PlacementOffset = context.Config.trapDropDistance;
    }
}
