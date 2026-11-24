using UnityEngine;

/// <summary>
/// Yanma görüntüsü: karakter yanarken üstünden alev dilleri ve kıvılcımlar yükselir, arada kara duman çıkar.
/// Kömürleşme (karakterin kararması) rengin içinde yapılır (bkz. <see cref="StatusTint"/>); burası yalnızca alevleri çizer.
/// Yanıp yanmadığını sahibi <see cref="Burning"/> ile söyler.
/// </summary>
public class BurnVisual : MonoBehaviour
{
    const float FlameInterval = 0.035f;
    const float SmokeInterval = 0.18f;

    static readonly Color[] FlameColors =
    {
        new Color(1f, 0.9f, 0.3f, 0.95f), new Color(1f, 0.6f, 0.1f, 0.95f), new Color(0.95f, 0.3f, 0.05f, 0.95f),
    };

    float flameTimer;
    float smokeTimer;

    /// <summary>Karakter şu an yanıyor mu?</summary>
    public bool Burning { get; set; }

    public static BurnVisual Attach(GameObject target)
    {
        var visual = target.GetComponent<BurnVisual>();
        if (visual == null) visual = target.AddComponent<BurnVisual>();
        return visual;
    }

    void LateUpdate()
    {
        if (!Burning) return;

        float size = Mathf.Max(0.3f, transform.lossyScale.x);
        Vector2 at = transform.position;

        flameTimer -= Time.deltaTime;
        while (flameTimer <= 0f)
        {
            flameTimer += FlameInterval;
            // Alev dili: gövdenin üstünden yukarı fırlar, kısa sürede söner (iz bırakmaz).
            Vector2 from = at + new Vector2(Random.Range(-0.35f, 0.35f) * size, Random.Range(-0.3f, 0.2f) * size);
            BloodDrop.Spawn(from, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.6f)), Random.Range(0.1f, 0.4f),
                            Random.Range(3f, 6f), Random.Range(0.08f, 0.18f) * size, FlameColors[Random.Range(0, FlameColors.Length)], false);
        }

        smokeTimer -= Time.deltaTime;
        if (smokeTimer <= 0f)
        {
            smokeTimer = SmokeInterval;
            BloodDrop.Spawn(at + Vector2.up * 0.4f * size, new Vector2(Random.Range(-0.3f, 0.3f), 1.2f), 0.6f, 4f,
                            Random.Range(0.2f, 0.32f) * size, new Color(0.18f, 0.18f, 0.2f, 0.6f), false);
        }
    }
}
