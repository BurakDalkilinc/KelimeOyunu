using UnityEngine;

/// <summary>
/// Yaratıkların, bossların, mermilerin ve alan simgelerinin 3D piksel görüntüsü.
/// - Resmin voxel modeli varsa (<c>Resources/Stacks/&lt;resim adı&gt;</c>, üretici önden çizimden otomatik yapar): katmanlı model
///   çizilir ve gittiği yöne döner; kameraya dönünce yüzü görünür. Animasyonun her karesinin kendi modeli vardır.
/// - Yoksa düz resim kalınlaştırılır: aynı resim birkaç katman üst üste, alt katlar koyu; gittiği yöne göre aynalanır.
/// Renk her kare düz resimden alınır; düz resim gizlenir ama yerinde kalır (boss parçalanması onu kullanır). Yalnızca görüntüdür.
/// </summary>
public class ExtrudedSpriteVisual : MonoBehaviour
{
    /// <summary>
    /// Voxel yaratık hep kameraya bakar (-90°): çapraz ya da yandan bakan uzun boylu model üstten bakan kamerada çarpık görünür.
    /// Sağa/sola gidişte ayna gibi döner, yürürken bu kadar derece sağa-sola sallanır.
    /// </summary>
    const float WalkSwayDegrees = 7f;
    const float WalkSwaySpeed = 12f;
    const float FacingAngle = -90f;

    SpriteRenderer source;
    SpriteStack stack;
    Vector3 lastPosition;
    bool facingLeft;
    bool voxel;
    float angle = FacingAngle; // Hep kameraya bakar
    float swayPhase;
    float moving;
    string shownSprite;
    int layerCount = 4;

    /// <summary>
    /// <paramref name="target"/>'in SpriteRenderer'ını 3D piksel çizer. Voxel modeli yoksa <paramref name="layers"/> katman kalınlaştırır.
    /// <paramref name="flipWithMovement"/> kapalıysa (alan simgeleri) dönmez, hep kameraya bakar.
    /// </summary>
    public static ExtrudedSpriteVisual Attach(GameObject target, int layers, bool flipWithMovement = true)
    {
        var visual = target.GetComponent<ExtrudedSpriteVisual>();
        if (visual == null) visual = target.AddComponent<ExtrudedSpriteVisual>();
        visual.layerCount = Mathf.Max(2, layers);
        visual.FlipWithMovement = flipWithMovement;
        return visual;
    }

    public bool FlipWithMovement { get; set; } = true;

    /// <summary>Katmanlı model (vuruş tepkisi rengini ve ölçeğini buna uygular); henüz kurulmadıysa null.</summary>
    public SpriteStack Stack => stack;

    void LateUpdate()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null) return;

        if (stack == null) Build();

        source.forceRenderingOff = true;
        bool visible = source.enabled;
        if (stack.gameObject.activeSelf != visible) stack.gameObject.SetActive(visible);
        if (!visible) { lastPosition = transform.parent != null ? transform.parent.position : transform.position; return; }

        stack.SortingOrder = source.sortingOrder;
        stack.SetTint(source.color);

        // Yön, yaratığın kendi hareketinden (kök): saldırıda ileri-geri atılma aynalamayı titretmesin.
        Vector3 here = transform.parent != null ? transform.parent.position : transform.position;
        Vector3 moved = here - lastPosition;
        lastPosition = here;

        if (voxel)
        {
            // Animasyon karesi değiştiyse o karenin modelini kullan.
            if (shownSprite != source.sprite.name)
            {
                shownSprite = source.sprite.name;
                stack.SetSlices(StackLibrary.Get(shownSprite));
            }
            bool isMoving = FlipWithMovement && moved.sqrMagnitude > 0.000004f && Time.deltaTime > 0f
                            && !HitStop.IsFrozen(gameObject); // Vuruş donmasında sallanma ve dönüş durur
            if (isMoving && Mathf.Abs(moved.x) > 0.002f) facingLeft = moved.x < 0f;
            stack.SetFlipY(facingLeft);

            // Yürürken hafifçe sallanır, dururken düzelir.
            moving = Mathf.MoveTowards(moving, isMoving ? 1f : 0f, Time.deltaTime * 5f);
            swayPhase += Time.deltaTime * WalkSwaySpeed * moving;
            angle = FacingAngle + Mathf.Sin(swayPhase) * WalkSwayDegrees * moving;
            stack.Angle = angle;
            return;
        }

        stack.SetSprite(source.sprite); // Animasyon karesi
        if (FlipWithMovement)
        {
            if (Mathf.Abs(moved.x) > 0.002f && !HitStop.IsFrozen(gameObject)) facingLeft = moved.x < 0f; // Donmada yön değişmez
            stack.SetFlipX(facingLeft != source.flipX);
        }
        else stack.SetFlipX(source.flipX);
    }

    void Build()
    {
        Sprite sprite = source.sprite;
        float width = sprite.rect.width / sprite.pixelsPerUnit;
        lastPosition = transform.parent != null ? transform.parent.position : transform.position;

        Sprite[] model = StackLibrary.Get(sprite.name);
        voxel = model != null;
        if (voxel)
        {
            shownSprite = sprite.name;
            stack = SpriteStack.Create(transform, "Voxel model", model, source.sortingOrder, width, 1f, 0.85f);
            stack.Angle = angle;
        }
        else stack = SpriteStack.Create(transform, "Kalınlık", StackLibrary.Extrude(sprite, layerCount), source.sortingOrder, width, 1f, 0.68f);
    }

    void OnDisable()
    {
        if (source != null) source.forceRenderingOff = false;
    }
}
