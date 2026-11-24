using UnityEngine;

/// <summary>
/// Araç görüntüsü: sürücünün altında aracın resmi (motor, araba) belirir, gittiği yöne göre sağa/sola döner ve hafifçe titrer;
/// arkasından egzoz dumanı çıkar. Hangi aracın görüneceğini sahibi <see cref="SpriteName"/> ve <see cref="WorldSize"/> ile söyler.
/// </summary>
public class VehicleVisual : MonoBehaviour
{
    SpriteRenderer car;
    SpriteRenderer body;
    string shownName;
    Vector3 lastPosition;
    float smokeTimer;
    bool facingLeft;

    /// <summary>Aracın resminin adı; boşsa araç yok.</summary>
    public string SpriteName { get; set; }

    /// <summary>Aracın dünyadaki genişliği (birim).</summary>
    public float WorldSize { get; set; } = 1.2f;

    public static VehicleVisual Attach(GameObject target)
    {
        var visual = target.GetComponent<VehicleVisual>();
        if (visual == null) visual = target.AddComponent<VehicleVisual>();
        return visual;
    }

    void LateUpdate()
    {
        if (car == null)
        {
            body = GetComponent<SpriteRenderer>();
            var go = new GameObject("Araç");
            go.transform.SetParent(transform, false);
            car = go.AddComponent<SpriteRenderer>();
            car.sortingOrder = (body != null ? body.sortingOrder : 5) - 1;
            car.enabled = false;
            lastPosition = transform.position;
        }

        if (shownName != SpriteName)
        {
            shownName = SpriteName;
            car.sprite = string.IsNullOrEmpty(shownName) ? null : SpriteLibrary.Get(shownName);
        }

        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;

        car.enabled = car.sprite != null;
        if (!car.enabled) return;

        if (Mathf.Abs(moved.x) > 0.001f) facingLeft = moved.x < 0f;
        car.flipX = facingLeft;

        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        float size = WorldSize / parentScale;
        float rumble = Mathf.Sin(Time.time * 60f) * 0.02f;
        car.transform.localScale = Vector3.one * size;
        car.transform.localPosition = new Vector3(0f, -0.3f + rumble, 0f);

        // Egzoz dumanı: arkadan gri damlalar.
        smokeTimer -= Time.deltaTime;
        if (smokeTimer <= 0f && moved.sqrMagnitude > 0.00001f)
        {
            smokeTimer = 0.06f;
            Vector2 back = -((Vector2)moved).normalized;
            Vector2 at = (Vector2)transform.position + back * (WorldSize * 0.5f) + Vector2.down * 0.3f;
            BloodDrop.Spawn(at, back * 1.5f + Random.insideUnitCircle * 0.5f, 0.3f, 2f, 0.12f, new Color(0.55f, 0.55f, 0.6f, 0.7f), false);
        }
    }
}
