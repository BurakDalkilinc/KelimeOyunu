/// <summary>"undead": ölümsüz kovalayıcı. Bir kez vurup kaybolmaz; süresi bitene kadar tekrar tekrar vurur.</summary>
public class UndeadTag : ITagBehaviour
{
    public string Tag => "undead";

    public void Configure(CreatureModel creature)
    {
        creature.Persistent = true;
    }
}
