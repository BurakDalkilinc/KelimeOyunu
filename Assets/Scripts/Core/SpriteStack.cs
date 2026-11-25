using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sprite stacking çizimi (sahte 3D): dilimleri aynı noktaya, her biri bir öncekinden birkaç piksel yukarı kaydırarak üst üste
/// çizer ve hepsini <see cref="Angle"/> kadar döndürür. Alt dilimler koyulaştırılır (yan yüz gölgesi). Bütün yığın bir
/// <see cref="SortingGroup"/> içindedir: sahnede tek parça gibi sıralanır. Yalnızca görüntüdür.
/// </summary>
public class SpriteStack : MonoBehaviour
{
    const float Pixel = 1f / 16f;

    SpriteRenderer[] layers;
    SortingGroup group;
    Color tint = Color.white;
    float pixelsPerLayer = 1f;
    float bottomShade = 0.55f;
    float alpha = 1f;
    float worldPixel = Pixel; // Resmin bir pikselinin dünyadaki boyu (büyütülmüş araçta daha büyük)

    /// <summary>Dünyadaki yönü (derece, 0 = sağa). Dilimler bu açıyla döner; yığın ekranda hep yukarı doğru yükselir.</summary>
    public float Angle { get; set; }

    /// <summary>Yığının yüksekliği (birim): dilim sayısı x dilim başına kayma.</summary>
    public float Height => layers == null ? 0f : (layers.Length - 1) * pixelsPerLayer * worldPixel;

    /// <summary>
    /// <paramref name="parent"/> altına bir yığın kurar. <paramref name="worldSize"/> bir dilimin dünyadaki genişliği (birim),
    /// <paramref name="pixelsPerLayer"/> her dilimin kaç piksel yukarı kayacağı, <paramref name="sortingOrder"/> yığının sırası.
    /// </summary>
    public static SpriteStack Create(Transform parent, string name, Sprite[] slices, int sortingOrder, float worldSize,
                                     float pixelsPerLayer = 1f, float bottomShade = 0.55f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var stack = go.AddComponent<SpriteStack>();
        stack.pixelsPerLayer = pixelsPerLayer;
        stack.bottomShade = bottomShade;
        stack.group = go.AddComponent<SortingGroup>();
        stack.group.sortingOrder = sortingOrder;
        stack.Build(slices, worldSize);
        return stack;
    }

    /// <summary>Renk (karakter rengi, yanma, saydamlık); alt dilimlerin gölgesiyle çarpılır.</summary>
    public void SetTint(Color color)
    {
        tint = color;
        ApplyColors();
    }

    /// <summary>Saydamlık (çatı içeri girilince kaybolur).</summary>
    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        ApplyColors();
    }

    /// <summary>Bütün dilimlere aynı resmi verir (kalınlaştırılmış animasyonlu yaratık: her karede güncel kare).</summary>
    public void SetSprite(Sprite sprite)
    {
        if (layers == null) return;
        foreach (SpriteRenderer layer in layers) if (layer.sprite != sprite) layer.sprite = sprite;
    }

    /// <summary>Dilimleri değiştirir (aynı sayıda; animasyonlu voxel model: her karenin kendi dilimleri).</summary>
    public void SetSlices(Sprite[] slices)
    {
        if (layers == null || slices == null) return;
        int count = Mathf.Min(layers.Length, slices.Length);
        for (int i = 0; i < count; i++) if (layers[i].sprite != slices[i]) layers[i].sprite = slices[i];
    }

    /// <summary>Bütün dilimleri yatay aynalar (önden çizilmiş yaratık sola giderken).</summary>
    public void SetFlipX(bool flip)
    {
        if (layers == null) return;
        foreach (SpriteRenderer layer in layers) layer.flipX = flip;
    }

    /// <summary>
    /// Bütün dilimleri resmin kendi y ekseninde aynalar. Kameraya bakan (-90°) voxel modelde bu, ekranda sağ-sol aynalamadır.
    /// </summary>
    public void SetFlipY(bool flip)
    {
        if (layers == null) return;
        foreach (SpriteRenderer layer in layers) layer.flipY = flip;
    }

    public int SortingOrder
    {
        get => group != null ? group.sortingOrder : 0;
        set { if (group != null) group.sortingOrder = value; }
    }

    void Build(Sprite[] slices, float worldSize)
    {
        if (slices == null || slices.Length == 0) return;

        layers = new SpriteRenderer[slices.Length];
        float sourcePixels = slices[0] != null ? slices[0].rect.width : 16f;
        worldPixel = worldSize / Mathf.Max(1f, sourcePixels);
        for (int i = 0; i < slices.Length; i++)
        {
            var go = new GameObject("Dilim " + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = slices[i];
            sr.sortingOrder = i; // Grup içinde: üstteki dilim üstte
            float spriteWidth = slices[i] != null ? slices[i].rect.width / slices[i].pixelsPerUnit : 1f;
            go.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.001f, spriteWidth));
            layers[i] = sr;
        }
        ApplyColors();
        Place();
    }

    void ApplyColors()
    {
        if (layers == null) return;
        int last = Mathf.Max(1, layers.Length - 1);
        for (int i = 0; i < layers.Length; i++)
        {
            float shade = Mathf.Lerp(bottomShade, 1f, (float)i / last);
            Color c = tint * shade;
            c.a = tint.a * alpha;
            layers[i].color = c;
        }
    }

    void LateUpdate() => Place();

    float placedAngle = float.NaN;

    void Place()
    {
        if (layers == null) return;
        if (placedAngle == Angle) return; // Değişen bir şey yok (ev duvarı, çatı): her karede yeniden yazma
        placedAngle = Angle;

        // Dilimler yığının çocuğudur: kayma yerel birimde, resmin bir pikseli kadar (büyütülmüş boss'ta kendiliğinden büyür).
        float step = pixelsPerLayer * worldPixel;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Angle);

        for (int i = 0; i < layers.Length; i++)
        {
            Transform t = layers[i].transform;
            t.localPosition = new Vector3(0f, step * i, 0f);
            t.rotation = rotation;
        }
    }
}
