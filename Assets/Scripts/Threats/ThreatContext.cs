/// <summary>
/// Bir global tehdidin etkilerine verilen bilgi: ayarlar, arena, oyuncular, çözüm alanları ve rastgelelik.
/// Her tehdit kendi bağlamını ve kendi çözüm alanlarını taşır.
/// </summary>
public class ThreatContext
{
    public WordEntry Word { get; }
    public PlayerState Writer { get; }
    public GameConfig Config { get; }
    public ArenaBounds Arena { get; }
    public IPlayerLocator World { get; }
    public System.Random Random { get; }

    /// <summary>Bu tehdidin çözüm alanları ve kimin korunduğu.</summary>
    public SafeZoneField Zones { get; }

    public ThreatContext(WordEntry word, PlayerState writer, GameConfig config, ArenaBounds arena,
                         IPlayerLocator world, System.Random random, SafeZoneField zones)
    {
        Word = word;
        Writer = writer;
        Config = config;
        Arena = arena;
        World = world;
        Random = random;
        Zones = zones;
    }
}
