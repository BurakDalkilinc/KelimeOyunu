using System.Collections.Generic;

/// <summary>
/// Efekt çalma izni: aynı ses kısa süre içinde (bekleme süresi) ikinci kez çalmaz, aynı anda en fazla N efekt çalar.
/// Zamanı dışarıdan alan düz C# sınıfı; Unity gerektirmez, testte saat elle ilerletilir.
/// </summary>
public class SfxGate
{
    readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    /// <summary>Aynı anda çalabilecek en fazla efekt sayısı.</summary>
    public int MaxSimultaneous { get; set; }

    public SfxGate(int maxSimultaneous)
    {
        MaxSimultaneous = maxSimultaneous;
    }

    /// <summary>
    /// Ses şimdi çalınabilir mi? Evetse çalındı sayılır (bekleme süresi başlar).
    /// <paramref name="playingNow"/>: şu an çalmakta olan efekt sayısı.
    /// </summary>
    public bool TryPlay(string id, float now, float cooldown, int playingNow)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (playingNow >= MaxSimultaneous) return false;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < cooldown) return false;

        lastPlayed[id] = now;
        return true;
    }

    /// <summary>Bekleme sürelerini unutur (ör. yeni sahne).</summary>
    public void Reset() => lastPlayed.Clear();
}
