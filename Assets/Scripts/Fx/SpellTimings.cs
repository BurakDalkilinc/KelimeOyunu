/// <summary>Kelime büyüsünün süreleri (GameConfig "Kelime büyüsü" başlığından kopyalanır; test kendi değerlerini verebilir).</summary>
public struct SpellTimings
{
    /// <summary>Harflerin kutudan sırayla kopma aralığı (sn).</summary>
    public float LeaveStagger;

    /// <summary>Kopan harfin kutunun üstündeki bekleme sırasına yükselme süresi (sn).</summary>
    public float RiseSeconds;

    /// <summary>Hedef belliyken bir harfin uçuş süresi (sn).</summary>
    public float FlightSeconds;

    /// <summary>Harflerin sırayla uçuşa kalkma aralığı (sn).</summary>
    public float FlightStagger;

    /// <summary>Hedef belli olduktan sonra son harfin en geç varış süresi (sn).</summary>
    public float MaxArriveSeconds;

    /// <summary>Hedef hiç belli olmazsa harflerin bekleyip söneceği süre (sn).</summary>
    public float HoverMaxSeconds;

    public static SpellTimings From(GameConfig config)
    {
        return new SpellTimings
        {
            LeaveStagger = config.spellLeaveStagger,
            RiseSeconds = config.spellRiseSeconds,
            FlightSeconds = config.spellFlightSeconds,
            FlightStagger = config.spellFlightStagger,
            MaxArriveSeconds = config.spellMaxArriveSeconds,
            HoverMaxSeconds = config.spellHoverMaxSeconds,
        };
    }
}
