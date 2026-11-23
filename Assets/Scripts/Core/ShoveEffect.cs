using UnityEngine;

/// <summary>İtişin kısa görsel izi: karakterin çevresinde hızla büyüyüp sönen beyaz bir halka. Oyun mantığı değil, yalnızca görüntü.</summary>
public class ShoveEffect : MonoBehaviour
{
    const float Seconds = 0.22f;

    float age;
    float maxScale;
    SpriteRenderer sr;

    /// <summary>Verilen noktada, <paramref name="radius"/> yarıçapına kadar büyüyen bir halka çıkarır.</summary>
    public static void Spawn(Vector2 position, float radius)
    {
        var go = new GameObject("İtiş izi");
        go.transform.position = new Vector3(position.x, position.y, 0f);

        var effect = go.AddComponent<ShoveEffect>();
        effect.maxScale = Mathf.Max(0.5f, radius * 2f);
        effect.sr = go.AddComponent<SpriteRenderer>();
        effect.sr.sprite = SpriteFactory.Circle();
        effect.sr.sortingOrder = 12;
        effect.Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Seconds)
        {
            Destroy(gameObject);
            return;
        }
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(age / Seconds);
        transform.localScale = Vector3.one * Mathf.Lerp(0.6f, maxScale, t);
        sr.color = new Color(1f, 1f, 1f, 0.55f * (1f - t));
    }
}
