using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Host'tan gelen duvar fotoğraflarını (<see cref="WallSnapshot"/>) tuğla karolarla çizer; ömrü bitmeye yakın duvar solar.
/// Hiçbir kural işletmez: duvarın engel olma işi host'taki <see cref="WallField"/>'dadır.
/// </summary>
public class NetWallRenderer : MonoBehaviour
{
    /// <summary>Kalan ömür oranı bunun altına inince duvar solmaya başlar.</summary>
    const float FadeBelowRatio = 0.25f;

    [SerializeField] NetTurnChannel channel;
    [SerializeField] Color wallColor = new Color(0.78f, 0.45f, 0.3f);

    readonly Dictionary<int, SpriteRenderer> views = new Dictionary<int, SpriteRenderer>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> toRemove = new List<int>();
    int appliedVersion = -1;

    void Update()
    {
        if (channel == null || channel.WallsVersion == appliedVersion) return;

        appliedVersion = channel.WallsVersion;
        seen.Clear();

        foreach (WallSnapshot wall in channel.LatestWalls)
        {
            seen.Add(wall.Id);

            if (!views.TryGetValue(wall.Id, out SpriteRenderer view))
            {
                view = Build(wall);
                views[wall.Id] = view;
            }

            Color color = wallColor;
            color.a = Mathf.Clamp01(wall.LifeRatio / FadeBelowRatio); // Son çeyrekte solar
            WallPresenter.ApplyLayerColors(view, color); // Katmanlı (3D piksel) duvarın tüm katları
        }

        toRemove.Clear();
        foreach (int id in views.Keys)
            if (!seen.Contains(id)) toRemove.Add(id);
        foreach (int id in toRemove)
        {
            Destroy(views[id].gameObject);
            views.Remove(id);
        }
    }

    void OnDestroy()
    {
        foreach (SpriteRenderer view in views.Values)
            if (view != null) Destroy(view.gameObject);
        views.Clear();
    }

    SpriteRenderer Build(WallSnapshot wall)
    {
        var go = new GameObject("Duvar " + wall.Id);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(wall.CenterX, wall.CenterY, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(wall.DirY, wall.DirX) * Mathf.Rad2Deg);

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

        WallPresenter.Extrude(go, sr); // Yükselen blok (3D piksel), tek kişilik sahnedeki gibi
        return sr;
    }
}
