using UnityEngine;

/// <summary>
/// Darbe görüntüsü: yerde genişleyip sönen bir toz halkası, kısa beyaz parlama ve etrafa saçılan toprak/taş parçaları.
/// Yaratık saldırılarında ve devlerin adımlarında kullanılır. Yalnızca görüntüdür.
/// </summary>
public class ImpactEffect : MonoBehaviour
{
    const float Seconds = 0.4f;

    static readonly Color[] Dirt =
    {
        new Color(0.55f, 0.43f, 0.3f, 0.95f), new Color(0.42f, 0.33f, 0.24f, 0.95f), new Color(0.68f, 0.6f, 0.48f, 0.9f),
    };

    float age;
    float size;
    SpriteRenderer ring;
    SpriteRenderer flash;

    /// <summary><paramref name="at"/> noktasında <paramref name="size"/> büyüklüğünde bir darbe (1 = normal, 3 = dev).</summary>
    public static void Spawn(Vector2 at, float size, bool withFlash = true)
    {
        var go = new GameObject("Darbe");
        go.transform.position = at;
        var effect = go.AddComponent<ImpactEffect>();
        effect.size = Mathf.Max(0.3f, size);

        effect.ring = go.AddComponent<SpriteRenderer>();
        effect.ring.sprite = SpriteFactory.Circle();
        effect.ring.sortingOrder = Gore.StainOrder + 3;

        if (withFlash)
        {
            var flashGo = new GameObject("Parlama");
            flashGo.transform.SetParent(go.transform, false);
            effect.flash = flashGo.AddComponent<SpriteRenderer>();
            effect.flash.sprite = SpriteFactory.Circle();
            effect.flash.sortingOrder = Gore.AirOrder + 1;
        }

        // Saçılan toprak ve taş parçaları.
        int count = Mathf.RoundToInt(8 * effect.size);
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            BloodDrop.Spawn(at, dir * Random.Range(1.5f, 4f) * Mathf.Sqrt(effect.size), Random.Range(0.1f, 0.4f),
                            Random.Range(2f, 6f) * Mathf.Sqrt(effect.size), Random.Range(0.08f, 0.16f) * Mathf.Sqrt(effect.size),
                            Dirt[Random.Range(0, Dirt.Length)], false);
        }
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
        float radius = Mathf.Lerp(0.3f, 1.4f, Mathf.Sqrt(t)) * size;
        transform.localScale = new Vector3(radius * 2f, radius, 1f); // Yerde yatık halka
        ring.color = new Color(0.75f, 0.66f, 0.5f, 0.6f * (1f - t));
        if (flash != null)
        {
            flash.transform.localScale = new Vector3(0.5f, 1f, 1f) * Mathf.Lerp(0.6f, 0.1f, t);
            flash.color = new Color(1f, 1f, 0.9f, Mathf.Clamp01(1f - t * 3f));
        }
    }
}
