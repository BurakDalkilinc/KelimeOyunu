/// <summary>
/// "ignite": tutuşturur. Yaratık haritadaki tahtaları yakar: uyarı alanı vuran bir kelimede (Ateş) alan vurunca bir kez tutuşur,
/// yangın oradan komşu tahtalara yayılır. Hasarı ve alanı diğer etiketlerden (instant) gelir.
/// </summary>
public class IgniteTag : ITagBehaviour
{
    public string Tag => "ignite";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.IgnitesTerrain = true;
        if (context.Config != null)
        {
            creature.BurnSecondsOnHit = context.Config.burnSeconds;   // Vurduğunu tutuşturur
            creature.CharPerHit = context.Config.charPerFireHit;
        }
    }
}
