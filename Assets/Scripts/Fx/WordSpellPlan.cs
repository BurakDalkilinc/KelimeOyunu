using UnityEngine;

/// <summary>
/// Kelime büyüsünün zaman çizelgesi (düz C#): harfler kutudan sırayla kopar ve kutunun üstünde bekler; hedef (doğan yaratık,
/// yazan ya da ekran tepesi) belli olunca sırayla uçar. Hedef belli olduktan sonra son harf en geç
/// <see cref="SpellTimings.MaxArriveSeconds"/> içinde varır; yaratık görüntüsü o ana kadar 0'dan büyür.
/// Hedef hiç gelmezse <see cref="SpellTimings.HoverMaxSeconds"/> sonra büyü söner. Zamanlar mutlak saniyedir.
/// </summary>
public class WordSpellPlan
{
    readonly SpellTimings timings;
    readonly int count;
    float flightStagger;
    float flightSeconds;

    /// <summary>Büyünün başladığı an.</summary>
    public float StartTime { get; }

    /// <summary>Harf sayısı (en az 1).</summary>
    public int LetterCount => count;

    /// <summary>Hedefin belli olduğu an; belli değilse null.</summary>
    public float? TargetTime { get; private set; }

    public WordSpellPlan(int letterCount, float startTime, SpellTimings timings)
    {
        count = Mathf.Max(1, letterCount);
        StartTime = startTime;
        this.timings = timings;
    }

    /// <summary>Hedef belli oldu. Yalnızca ilk çağrı sayılır. Uçuş aralığı gerekirse sıkıştırılır ki son harf geç kalmasın.</summary>
    public void SetTarget(float now)
    {
        if (TargetTime.HasValue) return;
        TargetTime = Mathf.Max(now, StartTime);

        float total = Mathf.Min(timings.FlightSeconds + (count - 1) * timings.FlightStagger, timings.MaxArriveSeconds);
        total = Mathf.Max(0.01f, total);
        flightStagger = count > 1 ? Mathf.Min(timings.FlightStagger, total * 0.5f / (count - 1)) : 0f;
        flightSeconds = Mathf.Max(0.01f, total - (count - 1) * flightStagger);
    }

    /// <summary>Harfin kutudan koptuğu an.</summary>
    public float LeaveTime(int index) => StartTime + Mathf.Clamp(index, 0, count - 1) * timings.LeaveStagger;

    /// <summary>Harfin uçuşa kalktığı an; hedef belli değilse sonsuz.</summary>
    public float DepartTime(int index)
    {
        if (!TargetTime.HasValue) return float.PositiveInfinity;
        return TargetTime.Value + Mathf.Clamp(index, 0, count - 1) * flightStagger;
    }

    /// <summary>Harfin vardığı an; hedef belli değilse sonsuz.</summary>
    public float ArriveTime(int index) => DepartTime(index) + flightSeconds;

    /// <summary>İlk harfin vardığı an (çağırma çemberi açılır).</summary>
    public float FirstArrival => ArriveTime(0);

    /// <summary>Son harfin vardığı an (flaş, kamera itmesi; yaratık tam boyuna ulaşır).</summary>
    public float LastArrival => ArriveTime(count - 1);

    /// <summary>Harfin kutudan kopup bekleme sırasına yükselişi (0..1; yumuşak).</summary>
    public float RiseProgress(int index, float now)
    {
        if (timings.RiseSeconds <= 0f) return now >= LeaveTime(index) ? 1f : 0f;
        return Easing.OutCubic((now - LeaveTime(index)) / timings.RiseSeconds);
    }

    /// <summary>Harfin uçuşu (0..1); hedef yoksa ya da kalkmadıysa 0. Önce yavaş, sonra hızlanarak (büyü "çekilir").</summary>
    public float FlightProgress(int index, float now)
    {
        float depart = DepartTime(index);
        if (now <= depart) return 0f;
        float u = Mathf.Clamp01((now - depart) / flightSeconds);
        return u * u * (2f - u) * 0.5f + u * 0.5f * u; // Ease-in ağırlıklı, sonunda tam 1
    }

    /// <summary>Harf uçuşta mı?</summary>
    public bool IsFlying(int index, float now) => now > DepartTime(index) && now < ArriveTime(index);

    /// <summary>Harf vardı mı (artık çizilmez)?</summary>
    public bool HasArrived(int index, float now) => now >= ArriveTime(index);

    /// <summary>Hedef gelmeden bekleme süresi doldu mu (harfler söner)?</summary>
    public bool HasExpired(float now) => !TargetTime.HasValue && now - StartTime >= timings.HoverMaxSeconds;

    /// <summary>
    /// Yaratık görüntüsünün büyümesi: hedef belli olduğu anda 0, son harf vardığında 1. Hedef yoksa 0.
    /// </summary>
    public float GrowProgress(float now)
    {
        if (!TargetTime.HasValue) return 0f;
        float span = LastArrival - TargetTime.Value;
        if (span <= 0f) return 1f;
        return Mathf.Clamp01((now - TargetTime.Value) / span);
    }

    /// <summary>Büyü bitti mi (bütün harfler vardı ya da bekleme süresi doldu)?</summary>
    public bool IsDone(float now) => HasExpired(now) || (TargetTime.HasValue && now >= LastArrival);
}
