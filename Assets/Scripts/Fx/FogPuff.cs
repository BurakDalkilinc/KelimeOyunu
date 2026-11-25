using UnityEngine;

/// <summary>Süzülen yarı saydam sis bulutu (zehirli tehditler): yavaşça belirir, sürüklenir, söner. Yalnızca görüntüdür.</summary>
public class FogPuff : MonoBehaviour
{
    float age;
    float life;
    float maxAlpha;
    Vector2 drift;
    SpriteRenderer sr;
    Color color;

    public static void Spawn(Vector2 at, Color color, float size, float life)
    {
        var go = new GameObject("Sis");
        go.transform.position = at;
        go.transform.localScale = new Vector3(size * 1.6f, size, 1f);
        var puff = go.AddComponent<FogPuff>();
        puff.sr = go.AddComponent<SpriteRenderer>();
        puff.sr.sprite = SpriteFactory.Circle();
        puff.sr.sortingOrder = Gore.AirOrder + 5;
        puff.color = color;
        puff.maxAlpha = color.a;
        puff.life = Mathf.Max(0.5f, life);
        puff.drift = Random.insideUnitCircle * 0.6f;
        puff.Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life) { Destroy(gameObject); return; }
        transform.position += (Vector3)(drift * Time.deltaTime);
        Apply();
    }

    void Apply()
    {
        float t = age / life;
        float fade = t < 0.3f ? t / 0.3f : t > 0.7f ? (1f - t) / 0.3f : 1f;
        Color c = color;
        c.a = maxAlpha * fade;
        sr.color = c;
    }
}
