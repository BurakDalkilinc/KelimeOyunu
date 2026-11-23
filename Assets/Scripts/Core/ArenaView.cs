using UnityEngine;

/// <summary>
/// Arenanın zeminini ve kenarlığını çizer, kamerayı arenayı tam gösterecek şekilde ayarlar.
/// Boyut GameConfig'ten gelir.
/// </summary>
public class ArenaView : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] Camera targetCamera;
    [SerializeField] Color floorColor = new Color(0.16f, 0.18f, 0.22f);
    [SerializeField] Color borderColor = new Color(0.85f, 0.85f, 0.9f);
    [SerializeField, Min(0.05f)] float borderThickness = 0.25f;

    float lastAspect;

    void Awake()
    {
        if (config == null) { Debug.LogError("ArenaView: GameConfig atanmamış.", this); return; }
        BuildVisuals();
    }

    void Start()
    {
        FitCamera();
    }

    void LateUpdate()
    {
        // Pencere boyutu değişirse kamerayı yeniden ayarla.
        if (targetCamera != null && !Mathf.Approximately(lastAspect, targetCamera.aspect))
            FitCamera();
    }

    void BuildVisuals()
    {
        Vector2 size = config.arenaSize;

        CreatePiece("Floor", Vector2.zero, size, floorColor, -10);

        float t = borderThickness;
        CreatePiece("BorderTop", new Vector2(0f, size.y * 0.5f + t * 0.5f), new Vector2(size.x + t * 2f, t), borderColor, -9);
        CreatePiece("BorderBottom", new Vector2(0f, -size.y * 0.5f - t * 0.5f), new Vector2(size.x + t * 2f, t), borderColor, -9);
        CreatePiece("BorderLeft", new Vector2(-size.x * 0.5f - t * 0.5f, 0f), new Vector2(t, size.y), borderColor, -9);
        CreatePiece("BorderRight", new Vector2(size.x * 0.5f + t * 0.5f, 0f), new Vector2(t, size.y), borderColor, -9);
    }

    void CreatePiece(string pieceName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        var go = new GameObject(pieceName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;

        // Piksel sanat varsa karo döşenir (ölçek yerine boyut verilir); yoksa renkli kare.
        Sprite tile = SpriteLibrary.Get(pieceName == "Floor" ? "floor" : "border");
        if (tile != null)
        {
            go.transform.localScale = Vector3.one;
            sr.sprite = tile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.color = Color.white;
            return;
        }

        sr.sprite = SpriteFactory.Square();
        sr.color = color;
    }

    /// <summary>Kamerayı arenanın tamamı (kenarlıkla birlikte) görünecek şekilde yakınlaştırır.</summary>
    void FitCamera()
    {
        if (targetCamera == null || config == null) return;

        lastAspect = targetCamera.aspect;
        float halfWidth = config.arenaSize.x * 0.5f + borderThickness + config.cameraPadding;
        float halfHeight = config.arenaSize.y * 0.5f + borderThickness + config.cameraPadding;

        // Takip kamerası (CameraFollow) konumu kendisi sürer; yoksa arenanın ortasına koy.
        if (targetCamera.GetComponent<CameraFollow>() == null) targetCamera.transform.position = new Vector3(0f, 0f, -10f);

        // Pixel Perfect Camera varsa boyutu kendisi yönetir; ona karışma.
        if (targetCamera.GetComponent("PixelPerfectCamera") != null) return;

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / targetCamera.aspect);
        targetCamera.transform.position = new Vector3(0f, 0f, -10f);
    }
}
