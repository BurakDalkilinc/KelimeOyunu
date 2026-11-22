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

        views.Add(new KeyValuePair<WallSegment, SpriteRenderer>(wall, sr));
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
        }
    }
}
