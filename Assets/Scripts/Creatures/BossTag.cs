/// <summary>
/// "boss": yaratık can barlı bir dev olur (Godzilla gibi). Hep en yakın oyuncuya saldırır, hedefi ölünce başkasına geçer.
/// Oyuncular yanına gidip Boşluk ile vurarak canını eritir (bkz. <see cref="BossRules"/>); son vuran ödül alır.
/// Dev gövdesiyle yürürken değdiği ev duvarlarını yıkar (bkz. <see cref="HouseDamage"/>).
/// Canı kelimenin "stats.hp" değeri (yazmıyorsa GameConfig'teki varsayılan), gövde büyüklüğü "stats.radius".
/// </summary>
public class BossTag : ITagBehaviour
{
    public string Tag => "boss";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        WordStats stats = creature.Word?.stats;
        int health = stats != null && stats.hp > 0 ? stats.hp : context.Config != null ? context.Config.bossDefaultHealth : 10;

        creature.MakeBoss(health);
        if (stats != null && stats.radius > 0f) creature.Radius = stats.radius; // Dev gövde: kelimenin yarıçapı
        creature.Persistent = true;
        creature.TargetsNearest = true;
        creature.RetargetsWhenTargetDies = true;
        creature.BreaksHouses = true; // Evlere takılmaz: duvarı yıkıp geçer (sığınak evleri hariç)
    }
}
