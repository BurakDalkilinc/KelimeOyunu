/// <summary>
/// "flying": uçan yaratık (ör. Arı). Uçan yaratık, uçan (kanat) oyunculara da vurabilir.
/// Duvarlardan geçmesi duvar adımında eklenecek ("Engellerin üstünden geçer").
/// </summary>
public class FlyingTag : ITagBehaviour
{
    public string Tag => "flying";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.IsFlying = true;
    }
}
