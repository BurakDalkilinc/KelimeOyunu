/// <summary>
/// "map_wide": bütün haritayı etkileyen tehdit. Ne yaptığı kelimenin diğer etiketlerine bağlıdır:
/// "water" dalga, "sky" meteor yağmuru, "swarm" sürü saldırısı; hiçbiri yoksa (ör. "explosive" ile) tüm haritayı
/// vuran bir patlama olur. Çözüm alanları kelimenin "counter" bilgisinden kurulur.
/// </summary>
public class MapWideTag : ITagBehaviour, IThreatEffectTag
{
    public string Tag => "map_wide";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public IThreatEffect CreateEffect(WordEntry word)
    {
        // Diğer etiketlere göre etki seçilir; yeni türler sırayla eklenir.
        if (word != null && word.HasTag("water")) return new WaveEffect();
        if (word != null && word.HasTag("sky")) return new MeteorShowerEffect();

        return new MapBlastEffect();
    }
}
