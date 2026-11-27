using UnityEngine;

/// <summary>
/// Vuruş donması (hitstop), yalnızca görüntü: bu bileşenin bulunduğu nesnenin görsel parçaları (kare animasyonu, katmanlı gövdenin
/// dönüşü ve zıplaması, saldırı animasyonu) kısa süre ilerlemez, sonra kaldığı yerden devam eder. Time.timeScale'e dokunmaz;
/// oyun mantığı ve konum etkilenmez (çok oyunculu oyunda herkes kendi ekranında dondurur). Görsel bileşenler
/// <see cref="IsFrozen(GameObject)"/> ile sorar. Süre gerçek zamanla sayılır (ağır çekimden etkilenmez).
/// </summary>
public class HitStop : MonoBehaviour
{
    float frozenUntil = -1f;

    /// <summary>Şu an donuk mu?</summary>
    public bool Frozen => Time.unscaledTime < frozenUntil;

    /// <summary>Kalan donma süresi (sn).</summary>
    public float Remaining => Mathf.Max(0f, frozenUntil - Time.unscaledTime);

    /// <summary>
    /// <paramref name="target"/>'ı <paramref name="seconds"/> saniye dondurur. Zaten donuksa uzun olan kalır (kısa donma uzununu kesmez).
    /// </summary>
    public static HitStop Freeze(GameObject target, float seconds)
    {
        if (target == null) return null;
        var stop = target.GetComponent<HitStop>();
        if (stop == null) stop = target.AddComponent<HitStop>();
        if (seconds > 0f) stop.frozenUntil = Mathf.Max(stop.frozenUntil, Time.unscaledTime + seconds);
        return stop;
    }

    /// <summary>Nesne şu an donuk mu? Bileşen yoksa false.</summary>
    public static bool IsFrozen(GameObject target)
    {
        return target != null && target.TryGetComponent(out HitStop stop) && stop.Frozen;
    }
}
