/// <summary>
/// Yazanın kendine uyguladığı (self) bir kelimenin etkilerine verilen bilgi: kim yazdı, hangi kelime, ayarlar.
/// </summary>
public readonly struct SelfEffectContext
{
    public PlayerState Writer { get; }
    public WordEntry Word { get; }
    public GameConfig Config { get; }

    public SelfEffectContext(PlayerState writer, WordEntry word, GameConfig config)
    {
        Writer = writer;
        Word = word;
        Config = config;
    }
}
