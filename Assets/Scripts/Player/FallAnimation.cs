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
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f - 0.7f * p), baseScale.z) * (1f - 0.3f * p);
            if (sprite != null) sprite.color = Color.Lerp(sprite.color, new Color(1f, 0.25f, 0.05f, 1f - p * 0.8f), 0.5f);
        }
    }

    void Restore()
    {
        active = false;
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
    }
}
