using UnityEngine;

/// <summary>
/// Fırlatma hareketi: toplam mesafeyi kısa bir süreye yayar; başta hızlı, sonda yavaş kayar.
/// Düz C# sınıfı. Karakter her karede <see cref="Step"/> ile o karenin kayma miktarını alır.
/// Yeni bir fırlatma gelirse eskisinin kalanını bırakıp yenisi başlar.
/// </summary>
public class KnockbackMotion
{
    Vector2 total;
    float duration;
    float elapsed;

    /// <summary>Kayma sürüyor mu?</summary>
    public bool IsActive => total != Vector2.zero && elapsed < duration;

    public void Start(Vector2 distance, float seconds)
    {
        total = distance;
        duration = Mathf.Max(0.01f, seconds);
        elapsed = 0f;
    }

    /// <summary>Kaymayı hemen keser (ör. lava ya da uçuruma düşünce).</summary>
    public void Cancel()
    {
        total = Vector2.zero;
    }

    /// <summary>Bu karede kayılacak miktar. Fırlatma yoksa ya da bittiyse sıfır.</summary>
    public Vector2 Step(float deltaSeconds)
    {
        if (!IsActive) return Vector2.zero;

        float before = Ease(elapsed / duration);
        elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, deltaSeconds));
        float after = Ease(elapsed / duration);

        return total * (after - before);
    }

    /// <summary>Başta hızlı, sonda yavaşlayan eğri (0 → 1).</summary>
    static float Ease(float x)
    {
        float inverse = 1f - Mathf.Clamp01(x);
        return 1f - inverse * inverse;
    }
}
