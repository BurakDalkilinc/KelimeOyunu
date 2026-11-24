using UnityEngine;

/// <summary>
/// Düşme animasyonu: uçuruma düşen küçülerek dönüp kaybolur, lavda batan kızarıp küçülür. Yalnızca görüntüdür; ne zaman başlayacağını
/// sahibi (<see cref="Kind"/>) söyler, bitince ölüm kuralı <see cref="TerrainHazards"/> tarafından işletilir.
/// </summary>
public class FallAnimation : MonoBehaviour
{
    float duration = 0.9f;
    float elapsed;
    bool active;
    Vector3 baseScale;
    Quaternion baseRotation;
    SpriteRenderer sprite;

    /// <summary>Düşme türü; <see cref="FallKind.None"/> ise animasyon yok ve karakter normal görünür.</summary>
    public FallKind Kind { get; set; }

    public static FallAnimation Attach(GameObject target, float seconds)
    {
        var anim = target.GetComponent<FallAnimation>();
        if (anim == null) anim = target.AddComponent<FallAnimation>();
        anim.duration = Mathf.Max(0.1f, seconds);
        return anim;
    }

    void LateUpdate()
    {
        if (Kind == FallKind.None)
        {
            if (active) Restore();
            return;
        }

        if (!active)
        {
            active = true;
            elapsed = 0f;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            sprite = GetComponent<SpriteRenderer>();
        }

        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / duration);

        if (Kind == FallKind.Chasm)
        {
            transform.localScale = baseScale * (1f - p);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, p * 540f);
        }
        else
        {
            // Lavda batış: kızarır, yanıp titrer, yassılıp saydamlaşır.
            float flicker = 0.5f + 0.5f * Mathf.Sin(elapsed * 40f);
            transform.localScale = new Vector3(baseScale.x * (1f - 0.35f * p), baseScale.y * (1f - 0.9f * p), baseScale.z);
            if (sprite != null)
            {
                Color hot = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.15f, 0f), flicker);
                hot.a = 1f - p;
                sprite.color = hot;
            }
        }
    }

    void Restore()
    {
        active = false;
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
    }
}
