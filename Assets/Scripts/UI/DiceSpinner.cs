using UnityEngine;

/// <summary>
/// Zar animasyonunun matematiği: oyuncu isimleri arasında dönen seçim, sonunda tam olarak
/// seçilen oyuncuda durur. Başta hızlı döner, sona doğru yavaşlar. Düz hesap; Unity nesnesi gerektirmez.
/// Kimin seçileceğine <see cref="Dice"/> karar verir, burada yalnızca gösterim vardır.
/// </summary>
public static class DiceSpinner
{
    /// <summary>Seçim durmadan önce listenin etrafında kaç tam tur atar.</summary>
    public const int DefaultLaps = 3;

    /// <summary>
    /// O anda ekranda gösterilecek adayın sırası. <paramref name="elapsed"/> süresi
    /// <paramref name="duration"/>'a ulaşınca her zaman <paramref name="finalIndex"/> döner.
    /// Aday yoksa -1 döner. Tek adayda animasyon yoktur.
    /// </summary>
    public static int GetIndex(float elapsed, float duration, int candidateCount, int finalIndex, int laps = DefaultLaps)
    {
        if (candidateCount <= 0) return -1;

        finalIndex = Mathf.Clamp(finalIndex, 0, candidateCount - 1);
        if (candidateCount == 1 || duration <= 0f || elapsed >= duration) return finalIndex;

        // Başlangıç noktası, son durak ile aynı olmasın diye tam turlara yarım tur eklenir.
        int totalSteps = Mathf.Max(0, laps) * candidateCount + Mathf.Max(1, candidateCount / 2);
        int startIndex = (((finalIndex - totalSteps) % candidateCount) + candidateCount) % candidateCount;

        float progress = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - Mathf.Pow(1f - progress, 3f); // Başta hızlı, sonda yavaş.
        int step = Mathf.Min(totalSteps, Mathf.FloorToInt(eased * totalSteps));

        return (startIndex + step) % candidateCount;
    }
}
