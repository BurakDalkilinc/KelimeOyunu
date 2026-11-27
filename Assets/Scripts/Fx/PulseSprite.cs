using UnityEngine;

/// <summary>
/// Basit yedek efekt (resim yoksa): dünyada bir daire belirir, büyür ve söner (çağırma halkası, beyaz flaş, puf).
/// Yalnızca görüntüdür, süre ölçeğinden bağımsızdır.
/// </summary>
public class PulseSprite : MonoBehaviour
{
    float age;
    float life;
    float fromSize;
    float toSize;
    Color color;
    SpriteRenderer sr;

    /// <summary><paramref name="at"/> noktasında <paramref name="fromSize"/>'dan <paramref name="toSize"/>'a büyüyüp sönen daire.</summary>
    public static PulseSprite Spawn(Vector2 at, Color color, float fromSize, float toSize, float life, int sortingOrder)
    {
        var go = new GameObject("Efekt halkası");
        go.transform.position = at;
        var pulse = go.AddComponent<PulseSprite>();
        pulse.sr = go.AddComponent<SpriteRenderer>();
        pulse.sr.sprite = SpriteFactory.Circle();
        pulse.sr.sortingOrder = sortingOrder;
        pulse.color = color;
        pulse.fromSize = fromSize;
        pulse.toSize = toSize;
        pulse.life = Mathf.Max(0.02f, life);
        pulse.Apply();
        return pulse;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(age / life);
        transform.localScale = Vector3.one * Mathf.Lerp(fromSize, toSize, Easing.OutCubic(t));
        sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
    }
}
