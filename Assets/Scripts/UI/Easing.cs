using UnityEngine;

/// <summary>
/// Yumuşatma eğrileri: 0..1 arası ilerlemeyi (t) animasyon eğrisine çevirir. Arayüz animasyonlarının hepsi buradan geçer.
/// Düz hesap, test edilebilir. t her zaman 0..1'e kırpılır; 0'da 0, 1'de 1 döner.
/// </summary>
public static class Easing
{
    /// <summary>OutBack'in varsayılan taşma miktarı (yaklaşık %10 fazla gidip geri gelir).</summary>
    public const float DefaultOvershoot = 1.70158f;

    public static float Linear(float t) => Mathf.Clamp01(t);

    /// <summary>Yavaş başlar, hızlanır (çıkış animasyonları için).</summary>
    public static float InCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    /// <summary>Hızlı başlar, yavaşlayarak durur.</summary>
    public static float OutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    /// <summary>Hedefi biraz aşıp geri gelir ("punch" girişleri). <paramref name="overshoot"/> büyüdükçe taşma artar.</summary>
    public static float OutBack(float t, float overshoot = DefaultOvershoot)
    {
        t = Mathf.Clamp01(t);
        float c3 = overshoot + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + overshoot * u * u;
    }

    /// <summary>Lastik gibi birkaç kez sallanarak oturur.</summary>
    public static float OutElastic(float t)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
}
