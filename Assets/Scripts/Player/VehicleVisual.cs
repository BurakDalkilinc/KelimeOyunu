using UnityEngine;

/// <summary>
/// Araç görüntüsü: aracın resmi (motor, araba) belirir, gittiği yöne döner ve hafifçe titrer; arkasından egzoz dumanı çıkar.
/// Katmanlı (3D piksel) araçta karakterin ayakta duran resmi gizlenir, yerine kabinde/selede yukarıdan görünen küçük bir kafa
/// (karakterin renginde) çizilir: sürücü aracın içinde görünür. Hangi aracın görüneceğini sahibi <see cref="SpriteName"/> ve <see cref="WorldSize"/> ile söyler.
/// </summary>
public class VehicleVisual : MonoBehaviour
{
    SpriteRenderer car;
    SpriteRenderer body;
    string shownName;
    Vector3 lastPosition;
    float smokeTimer;
    bool facingLeft;
    SpriteStack stack;
    float heading;
    SpriteRenderer head;
    SpriteRenderer headOutline;

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

        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        if (shownName != SpriteName)
        {
            shownName = SpriteName;
            if (stack != null) { Destroy(stack.gameObject); stack = null; }

            // Katmanlı (3D piksel) çizimi varsa onu kullan, yoksa düz yan görünüş resmi.
            Sprite[] slices = string.IsNullOrEmpty(shownName) ? null : StackLibrary.Get(shownName);
            if (slices != null)
            {
                stack = SpriteStack.Create(transform, "Araç (katmanlı)", slices, (body != null ? body.sortingOrder : 5) - 1,
                                           WorldSize * 1.1f / parentScale);
                car.sprite = null;
            }
            else car.sprite = string.IsNullOrEmpty(shownName) ? null : SpriteLibrary.Get(shownName);
        }

        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;

        // Katmanlı araçta sürücü aracın içindedir: ayakta duran resmi çizme (başka kodların enabled/renk ayarına dokunmadan).
        if (body != null) body.forceRenderingOff = stack != null;
        UpdateHead();

        float rumble = Mathf.Sin(Time.time * 60f) * 0.02f;
        if (stack != null)
        {
            // Gittiği yöne döner (yumuşak dönüş), motor titrer.
            if (moved.sqrMagnitude > 0.000001f)
                heading = Mathf.MoveTowardsAngle(heading, Mathf.Atan2(moved.y, moved.x) * Mathf.Rad2Deg, 900f * Time.deltaTime);
            stack.Angle = heading;
            stack.transform.localPosition = new Vector3(0f, rumble, 0f);
            car.enabled = false;
        }
        else
        {
            car.enabled = car.sprite != null;
            if (!car.enabled) return;

            if (Mathf.Abs(moved.x) > 0.001f) facingLeft = moved.x < 0f;
            car.flipX = facingLeft;
            car.transform.localScale = Vector3.one * (WorldSize / parentScale);
            car.transform.localPosition = new Vector3(0f, -0.3f + rumble, 0f);
        }

        // Egzoz dumanı: arkadan gri damlalar.
        EmitSmoke(moved);
    }

    /// <summary>Kabindeki/seledeki sürücünün kafası: aracın tepesinde, koltuğun yerinde, karakterin renginde.</summary>
    void UpdateHead()
    {
        if (head == null)
        {
            headOutline = CreateHeadPart("Sürücü kafası çizgisi");
            head = CreateHeadPart("Sürücü kafası");
        }

        bool show = stack != null;
        head.enabled = show;
        headOutline.enabled = show;
        if (!show) return;

        int order = body != null ? body.sortingOrder : 5;
        head.sortingOrder = order + 1;
        headOutline.sortingOrder = order;

        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        Vector2 forward = new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad));
        Vector3 seat = (Vector3)(-forward * (WorldSize * 0.12f)) + Vector3.up * (stack.Height + 0.04f) + stack.transform.localPosition * parentScale;
        float size = Mathf.Clamp(WorldSize * 0.3f, 0.3f, 0.5f);

        head.transform.position = transform.position + seat;
        headOutline.transform.position = head.transform.position;
        head.transform.localScale = Vector3.one * (size / parentScale);
        headOutline.transform.localScale = Vector3.one * (size * 1.3f / parentScale);

        Color c = body != null ? body.color : Color.white;
        c.a = 1f;
        head.color = c;
        headOutline.color = new Color(0.1f, 0.11f, 0.17f, 1f);
    }

    SpriteRenderer CreateHeadPart(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.enabled = false;
        return sr;
    }

    void EmitSmoke(Vector3 moved)
    {
        smokeTimer -= Time.deltaTime;
        if (smokeTimer <= 0f && moved.sqrMagnitude > 0.00001f)
        {
            smokeTimer = 0.06f;
            Vector2 back = -((Vector2)moved).normalized;
            Vector2 at = (Vector2)transform.position + back * (WorldSize * 0.5f) + (stack != null ? Vector2.zero : Vector2.down * 0.3f);
            BloodDrop.Spawn(at, back * 1.5f + Random.insideUnitCircle * 0.5f, 0.3f, 2f, 0.12f, new Color(0.55f, 0.55f, 0.6f, 0.7f), false);
        }
    }
}
