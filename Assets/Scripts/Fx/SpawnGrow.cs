using UnityEngine;

/// <summary>
/// Kelime büyüsüyle doğan yaratığın görüntüsünü 0'dan asıl boyuna büyütür (yaylı); büyüme bitince kendini siler.
/// Yalnızca görüntü: yaratığın kuralı (konum, hasar) hemen işler. Süre ölçeğinden bağımsızdır.
/// </summary>
public class SpawnGrow : MonoBehaviour
{
    float startAt;
    float endAt;
    Vector3 baseScale = Vector3.one;

    /// <summary>
    /// <paramref name="visual"/>'ı şimdi 0 ölçekten başlatıp <paramref name="endAt"/> (unscaled saniye) anında tam boya getirir.
    /// Zaten büyüyorsa yalnızca bitiş anı güncellenir.
    /// </summary>
    public static void Begin(Transform visual, float endAt)
    {
        if (visual == null) return;
        var grow = visual.GetComponent<SpawnGrow>();
        if (grow == null)
        {
            grow = visual.gameObject.AddComponent<SpawnGrow>();
            grow.baseScale = visual.localScale;
            grow.startAt = Time.unscaledTime;
        }
        grow.endAt = Mathf.Max(endAt, grow.startAt);
        grow.Apply();
    }

    void LateUpdate() => Apply();

    void Apply()
    {
        float span = endAt - startAt;
        float t = span > 0f ? (Time.unscaledTime - startAt) / span : 1f;
        if (t >= 1f)
        {
            transform.localScale = baseScale;
            Destroy(this);
            return;
        }
        transform.localScale = baseScale * Mathf.Max(0f, Easing.OutBack(t));
    }

    void OnDestroy()
    {
        transform.localScale = baseScale; // Yaratık erken silinse de ölçek bozuk kalmasın
    }
}
