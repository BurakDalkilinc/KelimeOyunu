using UnityEngine;

/// <summary>
/// Yazma sayacının son saniyeleri: kalan süre son <c>warnSeconds</c> saniyeye girince her tam saniyede bir kez "tik" der.
/// Düz hesap; sesi ve kırmızı nabzı <see cref="TypingBoxDisplay"/> çalar/çizer.
/// </summary>
public class CountdownTicker
{
    int lastSecond = int.MaxValue;

    /// <summary>Yeni sayım başlarken sıfırlar.</summary>
    public void Reset() => lastSecond = int.MaxValue;

    /// <summary>Kalan süre uyarı aralığında mı (0'dan büyük ve en fazla <paramref name="warnSeconds"/>)?</summary>
    public static bool IsWarning(float secondsLeft, int warnSeconds)
    {
        return secondsLeft > 0f && Mathf.CeilToInt(secondsLeft) <= warnSeconds;
    }

    /// <summary>Kalan süreyi bildirir; uyarı aralığında yeni bir saniyeye girildiyse true (tik sesi zamanı).</summary>
    public bool Update(float secondsLeft, int warnSeconds)
    {
        int second = Mathf.CeilToInt(secondsLeft);
        bool tick = second < lastSecond && IsWarning(secondsLeft, warnSeconds);
        lastSecond = second;
        return tick;
    }
}
