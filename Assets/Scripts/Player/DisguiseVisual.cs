using UnityEngine;

/// <summary>
/// Kılık görüntüsü (kamuflaj): karakterin üstünde kelimenin resmi (çalı gibi) belirir ve karakteri örter; karakter
/// yürürken çalı hafifçe sallanır. Hangi resmin görüneceğini sahibi <see cref="SpriteName"/> ile söyler (boşsa görünmez).
/// </summary>
public class DisguiseVisual : MonoBehaviour
{
    SpriteRenderer cover;
    SpriteRenderer body;
    string shownName;
    Vector3 lastPosition;
    float wobble;

    /// <summary>Gösterilecek resmin adı (<see cref="SpriteLibrary"/>); null ya da boşsa kılık yok.</summary>
    public string SpriteName { get; set; }

    public static DisguiseVisual Attach(GameObject target)
    {
        var visual = target.GetComponent<DisguiseVisual>();
        if (visual == null) visual = target.AddComponent<DisguiseVisual>();
        return visual;
    }

    void LateUpdate()
    {
        if (cover == null)
        {
            body = GetComponent<SpriteRenderer>();
            var go = new GameObject("Kılık");
            go.transform.SetParent(transform, false);
            cover = go.AddComponent<SpriteRenderer>();
            cover.sortingOrder = (body != null ? body.sortingOrder : 5) + 1;
            cover.enabled = false;
            lastPosition = transform.position;
        }

        if (shownName != SpriteName)
        {
            shownName = SpriteName;
            cover.sprite = string.IsNullOrEmpty(shownName) ? null : SpriteLibrary.Get(shownName);
        }

        cover.enabled = cover.sprite != null;
        if (!cover.enabled) { lastPosition = transform.position; return; }

        // Yürürken sallanır, dururken sakinleşir.
        float moved = (transform.position - lastPosition).magnitude;
        lastPosition = transform.position;
        wobble = Mathf.MoveTowards(wobble, moved > 0.001f ? 1f : 0f, Time.deltaTime * 4f);
        float angle = Mathf.Sin(Time.time * 14f) * 8f * wobble;
        cover.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        cover.transform.localScale = new Vector3(1.15f, 1.15f - 0.05f * wobble, 1f);
    }
}
