using UnityEngine;

/// <summary>
/// Sönümlü yay: değer hedefe doğru çekilir, hız taşır; sönüm azsa hedefi aşıp geri gelir (yaylanma, "overshoot").
/// Vuruşta ezilip geri dönen gövde ve kameranın yön itmesi bununla hesaplanır. Düz C#, test edilebilir; kare hızından bağımsızdır
/// (büyük adımlar küçük alt adımlara bölünür).
/// </summary>
public class SpringValue
{
    /// <summary>Alt adımın en uzun süresi (sn): ağır karede bile yay patlamasın.</summary>
    const float MaxSubStep = 1f / 120f;

    /// <summary>Şu anki değer.</summary>
    public float Value { get; private set; }

    /// <summary>Şu anki hız (birim/sn).</summary>
    public float Velocity { get; private set; }

    /// <summary>Yayın çektiği değer.</summary>
    public float Target { get; set; }

    /// <summary>Salınım sıklığı (saniyede tur). Büyük = sert ve hızlı yay.</summary>
    public float FrequencyHz { get; set; }

    /// <summary>Sönüm oranı: 1 = aşmadan durur, 1'den küçük = aşıp geri gelir, 0 = hiç durmaz.</summary>
    public float DampingRatio { get; set; }

    public SpringValue(float frequencyHz, float dampingRatio, float value = 0f)
    {
        FrequencyHz = frequencyHz;
        DampingRatio = dampingRatio;
        Value = value;
    }

    /// <summary>Değeri hemen verir ve hızı sıfırlar (ör. vuruş anında tam ezilme).</summary>
    public void Set(float value)
    {
        Value = value;
        Velocity = 0f;
    }

    /// <summary>Hıza ekleme yapar (darbe): değer o yöne fırlar, sonra yay geri çeker.</summary>
    public void Kick(float velocity) => Velocity += velocity;

    /// <summary>Hedefte ve neredeyse durgun mu?</summary>
    public bool IsSettled(float epsilon = 0.001f) =>
        Mathf.Abs(Value - Target) <= epsilon && Mathf.Abs(Velocity) <= epsilon * 10f;

    /// <summary>Yayı <paramref name="deltaSeconds"/> kadar ilerletir ve yeni değeri döndürür.</summary>
    public float Step(float deltaSeconds)
    {
        float left = Mathf.Max(0f, deltaSeconds);
        float omega = 2f * Mathf.PI * Mathf.Max(0f, FrequencyHz);
        float damping = Mathf.Max(0f, DampingRatio);

        while (left > 0f)
        {
            float dt = Mathf.Min(MaxSubStep, left);
            left -= dt;

            // Yarı örtük Euler: önce hız, sonra konum (kararlı).
            float acceleration = -omega * omega * (Value - Target) - 2f * damping * omega * Velocity;
            Velocity += acceleration * dt;
            Value += Velocity * dt;
        }
        return Value;
    }
}
