using UnityEngine;

/// <summary>
/// Yazı kutusunun altındaki sayaç çubuğunun düz hesapları: doluluk oranı, kalan süreye göre renk (yeşil → sarı → kırmızı)
/// ve son saniyelerdeki titreme şiddeti. Ekrana çizen <see cref="TypingBoxView"/>'dır.
/// </summary>
public static class TimerBarRules
{
    /// <summary>Çubuğun dolu kısmı (0..1): kalan süre / toplam süre. Toplam 0 ise 0.</summary>
    public static float Fraction(float secondsLeft, float totalSeconds)
    {
        if (totalSeconds <= 0f) return 0f;
        return Mathf.Clamp01(secondsLeft / totalSeconds);
    }

    /// <summary>
    /// Doluluğa göre renk: <paramref name="yellowAt"/> üstünde yeşil, oraya inerken sarıya, <paramref name="redAt"/>'e inerken
    /// kırmızıya döner; altında tam kırmızı.
    /// </summary>
    public static Color ColorFor(float fraction, Color green, Color yellow, Color red, float yellowAt, float redAt)
    {
        fraction = Mathf.Clamp01(fraction);
        yellowAt = Mathf.Clamp01(yellowAt);
        redAt = Mathf.Clamp(redAt, 0f, yellowAt);
        if (fraction >= yellowAt)
        {
            float span = 1f - yellowAt;
            float t = span > 0f ? (fraction - yellowAt) / span : 1f;
            return Color.Lerp(yellow, green, t);
        }
        if (fraction <= redAt) return red;
        return Color.Lerp(red, yellow, (fraction - redAt) / (yellowAt - redAt));
    }

    /// <summary>
    /// Son <paramref name="warnSeconds"/> saniyede titreme şiddeti: uyarı başlarken 0'a yakın, süre bitince <paramref name="maxPixels"/>.
    /// Uyarı dışında 0.
    /// </summary>
    public static float ShakeAmplitude(float secondsLeft, float warnSeconds, float maxPixels)
    {
        if (warnSeconds <= 0f || secondsLeft <= 0f || secondsLeft > warnSeconds) return 0f;
        return maxPixels * (1f - secondsLeft / warnSeconds * 0.6f);
    }
}
