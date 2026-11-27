using UnityEngine;

/// <summary>
/// "Travma" tabanlı ekran sarsıntısı hesabı: darbeler 0..1 arası travma ekler, travma zamanla düz bir hızla azalır,
/// sarsıntının şiddeti travmanın karesidir (küçük darbeler hafif, büyükler sert hissettirir). Titreme Perlin gürültüsüyle
/// yumuşak ve rastgele görünür. Düz C#, test edilebilir; kameraya uygulamayı <see cref="CameraShake"/> yapar.
/// </summary>
public class Trauma
{
    /// <summary>En kısa sönme süresi (sn); sıfıra bölmeyi önler.</summary>
    const float MinSeconds = 0.02f;

    float decayPerSecond;

    /// <summary>Şu anki travma (0..1).</summary>
    public float Value { get; private set; }

    /// <summary>Şiddet üssü: 2 = travmanın karesi.</summary>
    public float Exponent { get; set; } = 2f;

    /// <summary>Şu anki sarsıntı şiddeti (0..1) = travma ^ üs.</summary>
    public float Shake => Mathf.Pow(Value, Mathf.Max(0.01f, Exponent));

    /// <summary>Şu anki sönme hızı (saniyede travma).</summary>
    public float DecayPerSecond => decayPerSecond;

    /// <summary>
    /// Travma ekler (toplam en fazla 1). Sönme hızı, hem önceki sarsıntının kalanı hem bu yeninin <paramref name="seconds"/>
    /// süresi bitene kadar sürecek şekilde ayarlanır: küçük bir sarsıntı büyüğünü kısaltmaz.
    /// </summary>
    public void Add(float amount, float seconds)
    {
        if (amount <= 0f) return;

        float remainingOld = Value > 0f && decayPerSecond > 0f ? Value / decayPerSecond : 0f;
        Value = Mathf.Clamp01(Value + amount);
        float lasting = Mathf.Max(remainingOld, Mathf.Max(MinSeconds, seconds));
        decayPerSecond = Value / lasting;
    }

    /// <summary>Travmayı zamanla azaltır.</summary>
    public void Step(float deltaSeconds)
    {
        if (Value <= 0f) return;
        Value = Mathf.Max(0f, Value - decayPerSecond * Mathf.Max(0f, deltaSeconds));
        if (Value <= 0f) decayPerSecond = 0f;
    }

    /// <summary>Hepsini sıfırlar.</summary>
    public void Clear()
    {
        Value = 0f;
        decayPerSecond = 0f;
    }

    /// <summary>
    /// Eski "genlik" API'sinden travmaya çeviri: <paramref name="amplitude"/> birimlik en büyük kayma için gereken travma
    /// (<paramref name="maxOffset"/> en büyük kayma, şiddet = travma ^ üs).
    /// </summary>
    public static float AmountFor(float amplitude, float maxOffset, float exponent)
    {
        if (amplitude <= 0f || maxOffset <= 0f) return 0f;
        return Mathf.Pow(Mathf.Clamp01(amplitude / maxOffset), 1f / Mathf.Max(0.01f, exponent));
    }

    /// <summary>
    /// Bu anki kamera kayması: iki ayrı Perlin gürültüsü (x, y) -1..1, şiddet ve <paramref name="maxOffset"/> ile çarpılır.
    /// <paramref name="frequency"/> saniyedeki gürültü hızı, <paramref name="seed"/> her kameraya ayrı desen.
    /// </summary>
    public Vector2 Offset(float time, float seed, float frequency, float maxOffset)
    {
        float shake = Shake;
        if (shake <= 0f) return Vector2.zero;
        float t = time * frequency;
        float x = Mathf.PerlinNoise(seed, t) * 2f - 1f;
        float y = Mathf.PerlinNoise(seed + 17.3f, t) * 2f - 1f;
        return new Vector2(x, y) * maxOffset * shake;
    }
}
