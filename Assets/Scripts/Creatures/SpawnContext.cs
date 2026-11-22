/// <summary>
/// Bir yaratık doğarken etiketlere verilen bilgi: sürüde kaçıncı üye olduğu, sürü kaç kişi olduğu ve ayarlar.
/// Tek yaratıkta Index 0, Count 1'dir.
/// </summary>
public readonly struct SpawnContext
{
    /// <summary>Sürüdeki sırası (0'dan başlar).</summary>
    public int Index { get; }

    /// <summary>Bu kelimeden toplam kaç yaratık doğuyor.</summary>
    public int Count { get; }

    public GameConfig Config { get; }

    public SpawnContext(int index, int count, GameConfig config)
    {
        Index = index < 0 ? 0 : index;
        Count = count < 1 ? 1 : count;
        Config = config;
    }
}
