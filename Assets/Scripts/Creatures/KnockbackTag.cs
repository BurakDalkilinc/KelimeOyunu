/// <summary>
/// "knockback": vurduğu oyuncuyu fırlatır (kendinden uzağa). Mesafe GameConfig'ten gelir.
/// Hasarı olmayan yaratık da (ör. muz kabuğu) bununla oyuncuyu fırlatabilir.
/// </summary>
public class KnockbackTag : ITagBehaviour
{
    public string Tag => "knockback";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        if (context.Config == null) return;
        creature.KnockbackDistance = context.Config.knockbackDistance;
    }
}
