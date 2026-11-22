/// <summary>
/// "predator": avcı. Hedefi kovalar, vurup geri çekilir, sonra tekrar saldırır; ömrünün sonunda sıkılıp gider.
/// Geri çekilme ve gitme süreleri GameConfig'ten gelir. Geri çekilme aralarda kaçmak için fırsat verir.
/// </summary>
public class PredatorTag : ITagBehaviour
{
    public string Tag => "predator";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.Persistent = true; // Vurup kaybolmaz; geri çekilip yeniden saldırır.

        if (context.Config == null) return;
        creature.RetreatAfterHitSeconds = context.Config.predatorRetreatSeconds;
        creature.LeaveBeforeEndSeconds = context.Config.predatorLeaveSeconds;
    }
}
