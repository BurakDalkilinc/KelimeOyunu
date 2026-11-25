using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Duvarları ekranda çizer: <see cref="WallField"/>'a her yeni duvar eklendiğinde bir kahverengi dikdörtgen çıkarır,
/// ömrünün son saniyelerinde soldurur ve duvar kalkınca siler. Duvarın kendisi (çarpışma, süre) WallField'dadır.
/// </summary>
public class WallPresenter : MonoBehaviour
{
    [SerializeField] MatchBootstrap bootstrap;
    [SerializeField] Color wallColor = new Color(0.55f, 0.38f, 0.25f);
    [SerializeField, Min(0.1f)] float fadeSeconds = 1.5f;

    readonly List<KeyValuePair<WallSegment, SpriteRenderer>> views = new List<KeyValuePair<WallSegment, SpriteRenderer>>();

    void OnEnable()
    {
        if (bootstrap == null)
        {
            Debug.LogError("WallPresenter: bootstrap atanmamış.", this);
            enabled = false;
            return;
        }

        bootstrap.Walls.Added += OnWallAdded;
    }

    void OnDisable()
    {
        if (bootstrap != null) bootstrap.Walls.Added -= OnWallAdded;
    }

    void OnWallAdded(WallSegment wall)
    {
        var go = new GameObject("Duvar");
        go.transform.SetParent(transform, false);
        go.transform.position = wall.Center;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(wall.Direction.y, wall.Direction.x) * Mathf.Rad2Deg);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 6;

        Sprite brick = SpriteLibrary.Get("wall");
        if (brick != null)
        {
            sr.sprite = brick;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(wall.Length, wall.Thickness);
        }
        else
        {
            go.transform.localScale = new Vector3(wall.Length, wall.Thickness, 1f);
            sr.sprite = SpriteFactory.Square();
        }
        sr.color = wallColor;
        Extrude(go, sr);

        views.Add(new KeyValuePair<WallSegment, SpriteRenderer>(wall, sr));
    }

    /// <summary>Katmanlı (3D piksel) duvar yüksekliği (dilim = 1 piksel).</summary>
    const int WallLayers = 8;
    const float Pixel = 1f / 16f;

    /// <summary>Duvarı yükselen bir blok yapar: aynı resim her katta 1 piksel yukarıda tekrar çizilir, alt katlar koyu.</summary>
    public static void Extrude(GameObject wall, SpriteRenderer baseRenderer)
    {
        for (int i = 1; i < WallLayers; i++)
        {
            var layer = new GameObject("Kat " + i);
            layer.transform.SetParent(wall.transform, false);
            // Duvar döndürülmüş olsa da katlar ekranda hep yukarı yükselir.
            layer.transform.position = wall.transform.position + Vector3.up * (Pixel * i);
            layer.transform.rotation = wall.transform.rotation;
            layer.transform.localScale = Vector3.one;
            var sr = layer.AddComponent<SpriteRenderer>();
            sr.sprite = baseRenderer.sprite;
            sr.drawMode = baseRenderer.drawMode;
            if (sr.drawMode == SpriteDrawMode.Tiled) sr.size = baseRenderer.size;
            sr.sortingOrder = baseRenderer.sortingOrder + i;
        }
        ApplyLayerColors(baseRenderer, baseRenderer.color);
    }

    /// <summary>Katmanlı duvarın renklerini verir (alt katlar koyu); solma için saydamlık da buradan.</summary>
    public static void ApplyLayerColors(SpriteRenderer baseRenderer, Color color)
    {
        Transform root = baseRenderer.transform;
        int count = root.childCount;
        Color bottom = color * 0.55f; bottom.a = color.a;
        baseRenderer.color = bottom;
        for (int i = 0; i < count; i++)
        {
            var sr = root.GetChild(i).GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Color c = color * Mathf.Lerp(0.55f, 1f, (float)(i + 1) / Mathf.Max(1, count));
            c.a = color.a;
            sr.color = c;
        }
    }

    void Update()
    {
        for (int i = views.Count - 1; i >= 0; i--)
        {
            WallSegment wall = views[i].Key;
            SpriteRenderer sr = views[i].Value;

            if (sr == null || wall.IsExpired)
            {
                if (sr != null) Destroy(sr.gameObject);
                views.RemoveAt(i);
                continue;
            }

            Color color = wallColor;
            color.a = Mathf.Clamp01(wall.LifeLeft / fadeSeconds); // Son saniyelerde solar
            sr.color = color;
            ApplyLayerColors(sr, color);
        }
    }
}
