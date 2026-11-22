/// <summary>
/// "water": su. Tek başına bir şey yapmaz; "map_wide" ile birlikte tehdidi bir dev dalgaya çevirir
/// (bkz. <see cref="MapWideTag"/> ve <see cref="WaveEffect"/>). Etiketin var olması "bu tehdit su" demektir.
/// </summary>
public class WaterTag : ITagBehaviour
{
    public string Tag => "water";

    public void Configure(CreatureModel creature, SpawnContext context) { }
}
