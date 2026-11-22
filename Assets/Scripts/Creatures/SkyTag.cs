/// <summary>
/// "sky": gökten düşen. Yaratık (ör. meteor) bir gölge olarak görünür ve süre dolunca düşer; geri sayım yazısı gösterilmez,
/// çünkü çözüm gölgeyi görüp kaçmaktır. Tek başına bir tehdit etkisi üretmez; "map_wide" ile birlikte tehdidi bir meteor
/// yağmuruna çevirir (bkz. <see cref="MapWideTag"/> ve <see cref="MeteorShowerEffect"/>).
/// </summary>
public class SkyTag : ITagBehaviour
{
    public string Tag => "sky";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.IsSky = true;
    }
}
