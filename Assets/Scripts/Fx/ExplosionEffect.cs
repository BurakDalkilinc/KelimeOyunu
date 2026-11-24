using UnityEngine;

/// <summary>
/// Patlama görüntüsü: büyüyüp sönen sarı-turuncu ateş topu, etrafa saçılan alev kıvılcımları ve kara dumanlar, yerde kalıcı
/// bir is lekesi. Yalnızca görüntüdür (hasar ve yangını <see cref="VehicleRules"/> verir).
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    const float Seconds = 0.55f;

    float age;
    float radius;
    SpriteRenderer core;
    SpriteRenderer ring;

    public static void Spawn(Vector2 at, float radius)
    {
        var go = new GameObject("Patlama");
        go.transform.position = at;
        var effect = go.AddComponent<ExplosionEffect>();
        effect.radius = Mathf.Max(0.5f, radius);

        effect.ring = go.AddComponent<SpriteRenderer>();
        effect.ring.sprite = SpriteFactory.Circle();
        effect.ring.sortingOrder = Gore.AirOrder + 1;

        var coreGo = new GameObject("Ateş topu");
        coreGo.transform.SetParent(go.transform, false);
        effect.core = coreGo.AddComponent<SpriteRenderer>();
        effect.core.sprite = SpriteFactory.Circle();
        effect.core.sortingOrder = Gore.AirOrder + 2;

        // Kıvılcımlar ve duman.
        Color[] fire = { new Color(1f, 0.85f, 0.25f), new Color(1f, 0.5f, 0.1f), new Color(0.95f, 0.25f, 0.05f) };
        for (int i = 0; i < 40; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            BloodDrop.Spawn(at, dir * Random.Range(2f, 7f), Random.Range(0.2f, 0.8f), Random.Range(3f, 9f),
                            Random.Range(0.1f, 0.2f), fire[Random.Range(0, fire.Length)], false);
        }
        for (int i = 0; i < 18; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            BloodDrop.Spawn(at, dir * Random.Range(1f, 3f), Random.Range(0.5f, 1.2f), Random.Range(4f, 8f),
                            Random.Range(0.25f, 0.45f), new Color(0.15f, 0.15f, 0.17f, 0.8f), false);
        }

        // Yerde is lekesi.
        var scorch = new GameObject("İs");
        scorch.transform.position = at;
        scorch.transform.localScale = new Vector3(radius * 1.6f, radius * 1.2f, 1f);
        var sr = scorch.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = new Color(0.08f, 0.07f, 0.07f, 0.55f);
        sr.sortingOrder = Gore.StainOrder;
        Gore.RegisterStain(scorch);

        effect.Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Seconds) { Destroy(gameObject); return; }
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(age / Seconds);
        float size = Mathf.Lerp(0.4f, radius * 2.2f, Mathf.Sqrt(t));
        transform.localScale = Vector3.one * size;
        ring.color = new Color(1f, 0.45f, 0.1f, 0.8f * (1f - t));
        core.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 0.3f, t);
        core.color = new Color(1f, 0.95f, 0.6f, 1f - t);
    }
}
