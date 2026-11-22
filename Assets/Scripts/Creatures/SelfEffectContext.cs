using UnityEngine;

/// <summary>
/// Yazanın kendine uyguladığı (self) bir kelimenin etkilerine verilen bilgi: kim yazdı, hangi kelime, ayarlar;
/// duvar gibi yer isteyen etkiler için yazanın konumu, baktığı yön ve duvar alanı.
/// </summary>
public readonly struct SelfEffectContext
{
    public PlayerState Writer { get; }
    public WordEntry Word { get; }
    public GameConfig Config { get; }

    /// <summary>Yazanın sahnedeki konumu (bilinmiyorsa sıfır).</summary>
    public Vector2 WriterPosition { get; }

    /// <summary>Yazanın baktığı (son yürüdüğü) yön. Sıfırsa yukarı sayılır.</summary>
    public Vector2 Facing { get; }

    /// <summary>Duvar eklenebilen alan; null ise duvar kelimeleri bir şey yapmaz.</summary>
    public WallField Walls { get; }

    public SelfEffectContext(PlayerState writer, WordEntry word, GameConfig config,
                             Vector2 writerPosition = default, Vector2 facing = default, WallField walls = null)
    {
        Writer = writer;
        Word = word;
        Config = config;
        WriterPosition = writerPosition;
        Facing = facing;
        Walls = walls;
    }
}
