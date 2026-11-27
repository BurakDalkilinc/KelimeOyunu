using UnityEngine;

/// <summary>
/// Arayüzün ortak animasyon hesapları: içeri "punch" (küçükten taşarak büyüme), kısa sıçrama, sarsıntı, kayarak çıkış,
/// nabız ve sırayla belirme (stagger). Düz hesap; süreler ve ölçekler <see cref="GameConfig"/>'ten verilir.
/// </summary>
public static class UiTween
{
    /// <summary>Girişte ölçek: <paramref name="startScale"/>'den 1'e, hedefi biraz aşarak (OutBack).</summary>
    public static float PunchIn(float elapsed, float duration, float startScale, float overshoot)
    {
        if (duration <= 0f || elapsed >= duration) return 1f;
        return Mathf.LerpUnclamped(startScale, 1f, Easing.OutBack(Mathf.Max(0f, elapsed) / duration, overshoot));
    }

    /// <summary>Kısa sıçrama: ölçek 1 + <paramref name="amount"/>'tan lastik gibi sallanarak 1'e iner (zarın duruşu).</summary>
    public static float Bump(float elapsed, float duration, float amount)
    {
        if (duration <= 0f || elapsed < 0f || elapsed >= duration) return 1f;
        return 1f + amount * (1f - Easing.OutElastic(elapsed / duration));
    }

    /// <summary>Azalarak biten sarsıntı (piksel). Süre dışında 0. Aynı anda hep aynı değeri verir (rastgele değil).</summary>
    public static float Shake(float elapsed, float duration, float amplitude, float frequency)
    {
        if (duration <= 0f || elapsed < 0f || elapsed >= duration) return 0f;
        float fade = 1f - elapsed / duration;
        return amplitude * fade * Mathf.Sin(elapsed * frequency * 2f * Mathf.PI);
    }

    /// <summary>Kayarak çıkış: ne kadar kaydı (0 → <paramref name="distance"/>, gittikçe hızlanır).</summary>
    public static float SlideOut(float elapsed, float duration, float distance)
    {
        if (duration <= 0f) return distance;
        return distance * Easing.InCubic(elapsed / duration);
    }

    /// <summary>Doğrusal görünürlük: girişte 0 → 1 (<paramref name="fadeIn"/> true), çıkışta 1 → 0.</summary>
    public static float Fade(float elapsed, float duration, bool fadeIn)
    {
        float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        return fadeIn ? t : 1f - t;
    }

    /// <summary>0..1 arası sürekli nabız (saniyede <paramref name="frequency"/> kez).</summary>
    public static float Pulse(float time, float frequency)
    {
        return 0.5f - 0.5f * Mathf.Cos(time * frequency * 2f * Mathf.PI);
    }

    /// <summary>Geri sayımda her tam saniyede tepe yapan nabız: saniye yeni döndüğünde 1, sonraki saniyeye doğru 0'a iner.</summary>
    public static float SecondTick(float secondsLeft)
    {
        if (secondsLeft <= 0f) return 0f;
        float sinceTick = Mathf.Ceil(secondsLeft) - secondsLeft; // 0 = saniye az önce döndü
        return 1f - Easing.OutCubic(sinceTick);
    }

    /// <summary>Sırayla belirme: <paramref name="index"/>. satır <c>index × delay</c> saniye sonra başlar, <paramref name="duration"/>'da tamamlanır (0..1).</summary>
    public static float StaggerProgress(float elapsed, int index, float delay, float duration)
    {
        float start = Mathf.Max(0, index) * Mathf.Max(0f, delay);
        if (elapsed < start) return 0f;
        if (duration <= 0f) return 1f;
        return Mathf.Clamp01((elapsed - start) / duration);
    }

    /// <summary>Şu ana kadar belirmeye başlamış satır sayısı (en fazla <paramref name="count"/>).</summary>
    public static int StaggerStarted(float elapsed, int count, float delay)
    {
        if (count <= 0 || elapsed < 0f) return 0;
        if (delay <= 0f) return count;
        return Mathf.Min(count, Mathf.FloorToInt(elapsed / delay) + 1);
    }
}
