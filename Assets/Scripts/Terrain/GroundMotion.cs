using UnityEngine;

/// <summary>
/// Bir karakterin yürüme hızını zemine göre hesaplar. Normal zeminde hız hemen istenen değer olur (eski davranış);
/// buzda hız <see cref="GameConfig.iceAcceleration"/> ile yavaşça değişir: karakter zor hızlanır, tuşu bırakınca kayar.
/// Her karakterin kendi örneği vardır. Düz C# sınıfı, test edilebilir.
/// </summary>
public class GroundMotion
{
    /// <summary>Şu anki yürüme hızı (birim/sn).</summary>
    public Vector2 Velocity { get; private set; }

    /// <summary>Karakter şu an kayıyor mu (girdi olmasa da hareket edecek mi)?</summary>
    public bool IsMoving => Velocity.sqrMagnitude > 0.0001f;

    /// <summary>
    /// Bir kare ilerletir ve bu karedeki yer değiştirmeyi verir. <paramref name="direction"/> uzunluğu en çok 1 olan yön,
    /// <paramref name="speed"/> karakterin (etkilerle çarpılmış) yürüme hızı, <paramref name="ground"/> bastığı karo.
    /// </summary>
    public Vector2 Step(Vector2 direction, float speed, TileType ground, GameConfig config, float deltaSeconds)
    {
        float dt = Mathf.Max(0f, deltaSeconds);
        if (direction.sqrMagnitude > 1f) direction = direction.normalized;
        Vector2 wanted = direction * (Mathf.Max(0f, speed) * GroundRules.SpeedMultiplier(ground, config));

        if (TerrainMap.IsSlippery(ground) && config != null)
            Velocity = Vector2.MoveTowards(Velocity, wanted, config.iceAcceleration * dt);
        else
            Velocity = wanted;

        return Velocity * dt;
    }

    /// <summary>
    /// Duvara çarpınca hızı gerçekte gidilen yola indirir (buzda duvara sürtünen karakter hızını kaybeder).
    /// <paramref name="wanted"/> <see cref="Step"/>'in verdiği, <paramref name="actual"/> engellerden sonra gerçekleşen yer değiştirme.
    /// </summary>
    public void Absorb(Vector2 wanted, Vector2 actual, float deltaSeconds)
    {
        if (deltaSeconds <= 0f || (wanted - actual).sqrMagnitude < 0.000001f) return;
        Velocity = actual / deltaSeconds;
    }

    /// <summary>Hızı sıfırlar (düştü, savruldu, araca bindi).</summary>
    public void Stop() => Velocity = Vector2.zero;
}
