using UnityEngine;

/// <summary>
/// Kalan süreden doluluk oranı çıkarır: toplam süre bilinmediği için başlangıçtaki (en yüksek) kalan süreyi toplam sayar;
/// süre yeniden uzarsa (tazelendi) yeni toplam o olur. İtiş bekleme çubuğu ve etki süre çubukları kullanır. Düz hesap, test edilebilir.
/// </summary>
public class DurationGauge
{
    float last;

    /// <summary>Geçerli toplam süre (sn); etki yokken 0.</summary>
    public float Total { get; private set; }

    /// <summary>Son verilen kalan süre (sn).</summary>
    public float Remaining => last;

    /// <summary>
    /// Bu karenin kalan süresini verir. <paramref name="minimumTotal"/> bilinen en kısa toplamdır (ör. ayardaki itiş süresi);
    /// gösterge oyun ortasında açılırsa çubuk yanlış "dolu" başlamasın diye.
    /// </summary>
    public void Update(float remaining, float minimumTotal = 0f)
    {
        remaining = Mathf.Max(0f, remaining);
        if (remaining <= 0f) Total = 0f;
        else if (remaining > last + 0.0001f || Total <= 0f) Total = Mathf.Max(remaining, minimumTotal);
        last = remaining;
    }

    /// <summary>Kalan oran (1 = yeni başladı, 0 = bitti).</summary>
    public float RemainingFraction => HudRules.CooldownFraction(last, Total);

    /// <summary>Dolan oran (0 = yeni başladı, 1 = hazır).</summary>
    public float FilledFraction => 1f - RemainingFraction;
}
