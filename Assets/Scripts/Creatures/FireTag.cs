/// <summary>
/// "fire": ateş. Yaratığın alanı (area) süre boyunca ateş püskürtür: yarıçapındaki herkese, her
/// <see cref="GameConfig.fireDamageIntervalSeconds"/> saniyede bir kelimenin "stats.damage" kadar hasar verir.
/// Hedefi elenirse başka bir oyuncuya geçer. Tek başına (map_wide olmadan) bir tehdit kelimesinde (ör. Ejderha) etiket,
/// yaratığı kenardan salan bir tehdit etkisi de üretir. Bu etki uyarılıdır: yaratık uyarı süresi ("stats.warningSeconds") bitince
/// gelir, oyuncular o sürede mağaraya koşar. Çözüm alanındaki (mağara) korunana ateşin hiçbir hasarı işlemez.
/// </summary>
public class FireTag : ITagBehaviour, IThreatEffectTag
{
    public string Tag => "fire";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.BurnsArea = true;
        creature.IgnitesTerrain = true;
        if (context.Config != null)
        {
            creature.BurnSecondsOnHit = context.Config.burnSeconds;   // Vurduğunu tutuşturur
            creature.CharPerHit = context.Config.charPerFireHit;
        } // Ateş püskürten yaratık altındaki tahtaları da tutuşturur
        creature.RetargetsWhenTargetDies = true;
        if (context.Config != null) creature.AreaDamageIntervalSeconds = context.Config.fireDamageIntervalSeconds;
    }

    public IThreatEffect CreateEffect(WordEntry word)
    {
        // "map_wide" olan kelimelerde etkiyi o etiket üretir; çift etki olmasın.
        if (word != null && word.HasTag("map_wide")) return null;

        // Bir yaratığı (ejderha) uyarı bitince kenardan salar ve yaşadığı sürece tehdit sürer.
        return new HordeEffect(needsWarning: true);
    }
}
