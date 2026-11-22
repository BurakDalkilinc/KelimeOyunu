using UnityEngine;

/// <summary>
/// Bir oyuncunun üzerindeki süreli etkiler. Şimdilik yavaşlama; ileride çekilme gibi etkiler de buraya eklenecek.
/// Düz C# sınıfı. Süreyi karakter her karede <see cref="Tick"/> ile ilerletir, hızını
/// <see cref="SpeedMultiplier"/> ile çarpar.
/// </summary>
public class StatusEffects
{
    const float MinMultiplier = 0.05f;

    float slowMultiplier = 1f;
    float slowTimeLeft;
    Vector2 pullPoint;
    float pullForce;
    float pullTimeLeft;

    /// <summary>Şu an yavaşlatılmış mı?</summary>
    public bool IsSlowed => slowTimeLeft > 0f;

    /// <summary>Yavaşlamanın bitmesine kalan süre (sn). Yavaşlamıyorsa 0.</summary>
    public float SlowTimeLeft => slowTimeLeft;

    /// <summary>Şu an bir noktaya çekiliyor mu?</summary>
    public bool IsPulled => pullTimeLeft > 0f;

    /// <summary>Çekmenin bitmesine kalan süre (sn). Çekilmiyorsa 0.</summary>
    public float PullTimeLeft => pullTimeLeft;

    /// <summary>Hız çarpanı: yavaşlamıyorsa 1, yavaşlıyorsa (ör.) 0.5.</summary>
    public float SpeedMultiplier => IsSlowed ? slowMultiplier : 1f;

    /// <summary>
    /// Yavaşlatır. İki yavaşlatma üst üste binmez: en güçlü (en küçük) çarpan geçerli olur, süre en uzun olana tazelenir.
    /// Süresi 0 ya da çarpanı 1 (yavaşlatmayan) ise bir şey yapmaz.
    /// </summary>
    public void ApplySlow(float multiplier, float seconds)
    {
        if (seconds <= 0f) return;

        multiplier = Mathf.Clamp(multiplier, MinMultiplier, 1f);
        if (multiplier >= 1f) return;

        slowMultiplier = IsSlowed ? Mathf.Min(slowMultiplier, multiplier) : multiplier;
        slowTimeLeft = Mathf.Max(slowTimeLeft, seconds);
    }

    /// <summary>
    /// Bir noktaya doğru çekmeyi başlatır ya da tazeler. Güç saniyedeki birimdir; oyuncunun kendi hızından düşükse
    /// yürüyerek direnebilir. Güç ya da süre 0'sa bir şey yapmaz.
    /// </summary>
    public void ApplyPull(Vector2 point, float forcePerSecond, float seconds)
    {
        if (seconds <= 0f || forcePerSecond <= 0f) return;

        pullPoint = point;
        pullForce = forcePerSecond;
        pullTimeLeft = Mathf.Max(pullTimeLeft, seconds);
    }

    /// <summary>
    /// Bu karede çekmenin getireceği kayma. Çekilmiyorsa sıfır; noktayı aşmaz (noktaya varınca tam orada durur).
    /// </summary>
    public Vector2 GetPullStep(Vector2 position, float deltaSeconds)
    {
        if (!IsPulled) return Vector2.zero;

        Vector2 toPoint = pullPoint - position;
        float distance = toPoint.magnitude;
        if (distance < 0.0001f) return Vector2.zero;

        float step = Mathf.Min(pullForce * Mathf.Max(0f, deltaSeconds), distance);
        return toPoint / distance * step;
    }

    /// <summary>Zamanı ilerletir; süre dolan etkiler biter.</summary>
    public void Tick(float deltaSeconds)
    {
        float dt = Mathf.Max(0f, deltaSeconds);

        if (slowTimeLeft > 0f)
        {
            slowTimeLeft -= dt;
            if (slowTimeLeft <= 0f)
            {
                slowTimeLeft = 0f;
                slowMultiplier = 1f;
            }
        }

        if (pullTimeLeft > 0f)
        {
            pullTimeLeft -= dt;
            if (pullTimeLeft <= 0f) pullTimeLeft = 0f;
        }
    }

    /// <summary>Tüm etkileri hemen kaldırır.</summary>
    public void Clear()
    {
        slowTimeLeft = 0f;
        slowMultiplier = 1f;
        pullTimeLeft = 0f;
        pullForce = 0f;
    }
}
