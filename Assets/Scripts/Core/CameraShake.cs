using UnityEngine;

/// <summary>
/// Kamera sarsıntısı: büyük darbelerde (boss saldırısı, dev adımları) kamerayı kısa süre titretir. Takip eden kamera her karede
/// konumunu kendisi yazdığı için sarsıntı ondan sonra eklenir (birikmez). Yalnızca görüntüdür.
/// </summary>
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    const float MaxOffset = 0.6f;

    float strength;
    float timeLeft;
    float duration;
    float seed;

    /// <summary>
    /// Ana kamerayı <paramref name="amplitude"/> birim kadar, <paramref name="seconds"/> boyunca sarsar. Kaynağı kameradan
    /// uzaksa (<paramref name="source"/> verildiyse) sarsıntı uzaklıkla azalır.
    /// </summary>
    public static void Shake(float amplitude, float seconds, Vector2? source = null)
    {
        Camera cam = Camera.main;
        if (cam == null || amplitude <= 0f || seconds <= 0f) return;

        if (source.HasValue)
        {
            float distance = Vector2.Distance(source.Value, cam.transform.position);
            float falloff = Mathf.Clamp01(1f - (distance - 4f) / 12f); // 4 birime kadar tam, 16 birimde sıfır
            amplitude *= falloff;
            if (amplitude <= 0.001f) return;
        }

        var shake = cam.GetComponent<CameraShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
        shake.Add(amplitude, seconds);
    }

    void Add(float amplitude, float seconds)
    {
        // Güçlüsü baskın: küçük sarsıntı büyüğünü kesmez.
        if (amplitude >= strength * (timeLeft / Mathf.Max(0.001f, duration)))
        {
            strength = Mathf.Min(MaxOffset, amplitude);
            duration = seconds;
            timeLeft = seconds;
            seed = Random.value * 100f;
        }
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f) return;
        timeLeft -= Time.unscaledDeltaTime;
        float fade = Mathf.Clamp01(timeLeft / Mathf.Max(0.001f, duration));
        float t = Time.unscaledTime * 40f;
        Vector2 offset = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 7f, t) - 0.5f) * 2f * strength * fade * fade;
        transform.position += new Vector3(offset.x, offset.y, 0f);
    }
}
