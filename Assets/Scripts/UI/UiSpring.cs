using UnityEngine;

/// <summary>
/// Yay ile hedefe giden sayı (düğmenin üzerine gelince büyüme gibi): hedefe çekilir, sönümle durulur, az sönümde biraz taşar.
/// Düz hesap; büyük kare sürelerinde de patlamasın diye adım küçük parçalara bölünür.
/// </summary>
public class UiSpring
{
    /// <summary>Tek hesap adımının en uzun süresi (sn).</summary>
    const float MaxStep = 1f / 120f;

    /// <summary>Bir çağrıda işlenecek en uzun süre (sn); oyun takılınca yay uzun süre "yetişmeye" çalışmasın.</summary>
    const float MaxDelta = 0.25f;

    public float Value { get; private set; }
    public float Velocity { get; private set; }

    public UiSpring(float start)
    {
        Value = start;
    }

    /// <summary>Değeri <paramref name="target"/>'a doğru <paramref name="deltaTime"/> kadar ilerletir.</summary>
    public float Step(float target, float deltaTime, float stiffness, float damping)
    {
        float left = Mathf.Clamp(deltaTime, 0f, MaxDelta);
        while (left > 0f)
        {
            float dt = Mathf.Min(MaxStep, left);
            left -= dt;
            float force = (target - Value) * stiffness - Velocity * damping;
            Velocity += force * dt;
            Value += Velocity * dt;
        }
        return Value;
    }

    /// <summary>Hemen değere atlar (hızı sıfırlar).</summary>
    public void Snap(float value)
    {
        Value = value;
        Velocity = 0f;
    }
}
