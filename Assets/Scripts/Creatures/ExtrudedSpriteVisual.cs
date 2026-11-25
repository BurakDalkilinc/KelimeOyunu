using UnityEngine;

/// <summary>
/// Düz bir resmi 3D piksel gibi kalınlaştırır (yaratıklar, bosslar, mermiler, alan simgeleri): aynı resim birkaç katman
/// üst üste, her biri bir piksel yukarıda çizilir; alt katmanlar koyu olduğu için alttan bir yan yüz görünür. Resim önden
/// çizildiği için döndürülmez; gittiği yöne göre sağa/sola aynalanır. Animasyon (iki kare) ve renk her kare düz resimden
/// alınır; düz resim gizlenir ama yerinde kalır (boss parçalanması onu kullanır). Yalnızca görüntüdür.
/// </summary>
public class ExtrudedSpriteVisual : MonoBehaviour
{
    SpriteRenderer source;
    SpriteStack stack;
    Vector3 lastPosition;
    bool facingLeft;
    int layerCount = 4;

    /// <summary>
    /// <paramref name="target"/>'in SpriteRenderer'ını <paramref name="layers"/> katman kalınlaştırır. Hareket yönüne göre
    /// aynalama <paramref name="flipWithMovement"/> ile açılır (simgelerde kapalı).
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

        if (stack == null)
        {
            Sprite sprite = source.sprite;
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            stack = SpriteStack.Create(transform, "Kalınlık", StackLibrary.Extrude(sprite, layerCount), source.sortingOrder, width, 1f, 0.68f);
            lastPosition = transform.position;
        }

        source.forceRenderingOff = true;
        bool visible = source.enabled;
        if (stack.gameObject.activeSelf != visible) stack.gameObject.SetActive(visible);
        if (!visible) return;

        stack.SortingOrder = source.sortingOrder;
        stack.SetSprite(source.sprite);  // Animasyon karesi
        stack.SetTint(source.color);

        if (FlipWithMovement)
        {
            Vector3 moved = transform.position - lastPosition;
            if (Mathf.Abs(moved.x) > 0.002f) facingLeft = moved.x < 0f;
            stack.SetFlipX(facingLeft != source.flipX);
        }
        else stack.SetFlipX(source.flipX);
        lastPosition = transform.position;
    }

    void OnDisable()
    {
        if (source != null) source.forceRenderingOff = false;
    }
}
