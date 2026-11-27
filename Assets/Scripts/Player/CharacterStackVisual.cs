using UnityEngine;

/// <summary>
/// Karakterin 3D piksel (sprite stacking) görüntüsü: düz resmin yerine katmanlı yuvarlak bir gövde çizer. Gövde yürüdüğü yöne
/// döner (gözler öne bakar), yürürken hafifçe zıplar. Rengi (oyuncu rengi, kalkan, yanma, görünmezlik, hayalet) her kare düz
/// resmin renginden alınır; düz resim gizlenir ama yerinde kalır (kan/parçalanma onu kullanır). Araç sürerken, ölü bot gizliyken
/// ya da katmanlı çizim yoksa gövde çizilmez. Yalnızca görüntüdür; tek kişilik sahnede ve ağda aynı çalışır.
/// </summary>
public class CharacterStackVisual : MonoBehaviour
{
    const string StackName = "char";
    const float TurnDegreesPerSecond = 720f;
    const float BobHeight = 0.06f;
    const float BobSpeed = 14f;

    SpriteRenderer body;
    SpriteStack stack;
    VehicleVisual vehicle;
    DisguiseVisual disguise;
    Vector3 lastPosition;
    float angle = -90f; // Başta kameraya (aşağı) bakar
    float bobPhase;
    float moving;

    public static CharacterStackVisual Attach(GameObject target)
    {
        var visual = target.GetComponent<CharacterStackVisual>();
        if (visual == null) visual = target.AddComponent<CharacterStackVisual>();
        return visual;
    }

    /// <summary>Katmanlı gövde (vuruş tepkisi rengini ve ölçeğini buna uygular); yoksa null.</summary>
    public SpriteStack Stack => stack;

    /// <summary>Katmanlı çizim varsa düz resmi gizler (araç görüntüsü de bunu bilir).</summary>
    public bool Active => stack != null;

    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        vehicle = GetComponent<VehicleVisual>();
        lastPosition = transform.position;

        Sprite[] slices = StackLibrary.Get(StackName);
        if (body == null || slices == null) return;

        stack = SpriteStack.Create(transform, "Gövde (katmanlı)", slices, body.sortingOrder, 1.1f, 1f, 0.7f);
    }

    void LateUpdate()
    {
        if (stack == null || body == null) return;
        if (vehicle == null) vehicle = GetComponent<VehicleVisual>();

        if (disguise == null) disguise = GetComponent<DisguiseVisual>();

        bool driving = vehicle != null && vehicle.Driving;
        bool disguised = disguise != null && !string.IsNullOrEmpty(disguise.SpriteName); // Kamuflaj: yalnızca çalı görünür
        bool visible = body.enabled && !driving && !disguised;
        body.forceRenderingOff = true; // Düz resim hep gizli: yerine katmanlı gövde (ya da araç) çizilir
        if (stack.gameObject.activeSelf != visible) stack.gameObject.SetActive(visible);
        if (!visible) { lastPosition = transform.position; return; }

        stack.SortingOrder = body.sortingOrder;
        stack.SetTint(body.color);

        // Yürüdüğü yöne dön.
        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;
        bool isMoving = moved.sqrMagnitude > 0.000004f && Time.deltaTime > 0f;
        bool frozen = HitStop.IsFrozen(gameObject); // Vuruş donması: dönüş ve zıplama bir an durur
        if (isMoving && !frozen)
        {
            float target = Mathf.Atan2(moved.y, moved.x) * Mathf.Rad2Deg;
            angle = Mathf.MoveTowardsAngle(angle, target, TurnDegreesPerSecond * Time.deltaTime);
        }
        stack.Angle = angle;

        // Yürürken zıpla, dururken yere in.
        if (!frozen)
        {
            moving = Mathf.MoveTowards(moving, isMoving ? 1f : 0f, Time.deltaTime * 6f);
            bobPhase += Time.deltaTime * BobSpeed * moving;
        }
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.y);
        stack.transform.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * BobHeight * moving / parentScale, 0f);
    }

    void OnDisable()
    {
        if (body != null) body.forceRenderingOff = false;
        if (stack != null) stack.gameObject.SetActive(false);
    }
}
