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
    const float TurnDegreesPerSecond = 300f;

    /// <summary>
    /// Voxel yaratık kameraya bakan yönden en fazla bu kadar döner ("dörtte üç" görünüş). Tam yana dönen uzun boylu bir model
    /// üstten bakan kamerada omuz genişliği boy gibi uzayıp sütuna benzediği için tam dönüş yapılmaz.
    /// </summary>
    const float MaxTurnFromCamera = 40f;

    SpriteRenderer source;
    SpriteStack stack;
    Vector3 lastPosition;
    bool facingLeft;
    bool voxel;
    float angle = -90f; // Başta kameraya bakar
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

    void LateUpdate()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null) return;

        if (stack == null) Build();

        source.forceRenderingOff = true;
        bool visible = source.enabled;
        if (stack.gameObject.activeSelf != visible) stack.gameObject.SetActive(visible);
        if (!visible) { lastPosition = transform.position; return; }

        stack.SortingOrder = source.sortingOrder;
        stack.SetTint(source.color);

        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;

        if (voxel)
        {
            // Animasyon karesi değiştiyse o karenin modelini kullan.
            if (shownSprite != source.sprite.name)
            {
                shownSprite = source.sprite.name;
                stack.SetSlices(StackLibrary.Get(shownSprite));
            }
            if (FlipWithMovement && moved.sqrMagnitude > 0.000004f && Time.deltaTime > 0f)
            {
                // Sağa giderken biraz sağa, sola giderken biraz sola döner; yüzü hep kameraya dönük kalır.
                float side = Mathf.Clamp(moved.x / moved.magnitude, -1f, 1f);
                float target = -90f + side * MaxTurnFromCamera;
                angle = Mathf.MoveTowardsAngle(angle, target, TurnDegreesPerSecond * Time.deltaTime);
            }
            stack.Angle = angle;
            return;
        }

        stack.SetSprite(source.sprite); // Animasyon karesi
        if (FlipWithMovement)
        {
            if (Mathf.Abs(moved.x) > 0.002f) facingLeft = moved.x < 0f;
            stack.SetFlipX(facingLeft != source.flipX);
        }
        else stack.SetFlipX(source.flipX);
    }

    void Build()
    {
        Sprite sprite = source.sprite;
        float width = sprite.rect.width / sprite.pixelsPerUnit;
        lastPosition = transform.position;

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
