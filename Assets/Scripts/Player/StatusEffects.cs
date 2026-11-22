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

    /// <summary>Şu an yavaşlatılmış mı?</summary>
    public bool IsSlowed => slowTimeLeft > 0f;

    /// <summary>Yavaşlamanın bitmesine kalan süre (sn). Yavaşlamıyorsa 0.</summary>
    public float SlowTimeLeft => slowTimeLeft;

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

    /// <summary>Zamanı ilerletir; süre dolan etkiler biter.</summary>
    public void Tick(float deltaSeconds)
    {
        if (slowTimeLeft <= 0f) return;

        slowTimeLeft -= Mathf.Max(0f, deltaSeconds);
        if (slowTimeLeft <= 0f) Clear();
    }

    /// <summary>Tüm etkileri hemen kaldırır.</summary>
    public void Clear()
    {
        slowTimeLeft = 0f;
        slowMultiplier = 1f;
    }
}
