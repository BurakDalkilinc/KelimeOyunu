using UnityEngine;

/// <summary>
/// Kılıç/sopa savuruşu: silahın resmi vuranın önünde kısa bir yay çizerek döner, arkasında beyaz bir iz bırakır.
/// Yalnızca görüntüdür.
/// </summary>
public class MeleeSwingEffect : MonoBehaviour
{
    const float Seconds = 0.2f;
    const float ArcDegrees = 140f;

    float age;
    float reach;
    float baseAngle;
    Vector2 origin;
    SpriteRenderer weapon;
    SpriteRenderer trail;

    public static void Spawn(Vector2 from, Vector2 facing, float reach, Sprite sprite)
    {
        var go = new GameObject("Savuruş");
        var effect = go.AddComponent<MeleeSwingEffect>();
        effect.origin = from;
        effect.reach = Mathf.Max(0.6f, reach);
        Vector2 dir = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        effect.baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        effect.weapon = go.AddComponent<SpriteRenderer>();
        effect.weapon.sprite = sprite != null ? sprite : SpriteFactory.Circle();
        effect.weapon.sortingOrder = 13;

        var trailGo = new GameObject("Savuruş izi");
        trailGo.transform.SetParent(go.transform, false);
        effect.trail = trailGo.AddComponent<SpriteRenderer>();
        effect.trail.sprite = SpriteFactory.Circle();
        effect.trail.sortingOrder = 12;
        effect.Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Seconds) { Destroy(gameObject); return; }
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(age / Seconds);
        float angle = baseAngle + Mathf.Lerp(ArcDegrees * 0.5f, -ArcDegrees * 0.5f, t);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        transform.position = origin + dir * (reach * 0.6f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f); // Resimdeki silah çapraz (sağ üst) çizili
        transform.localScale = Vector3.one * 1.1f;

        trail.transform.localScale = new Vector3(reach * 0.9f, 0.25f, 1f);
        trail.color = new Color(1f, 1f, 1f, 0.5f * (1f - t));
        weapon.color = new Color(1f, 1f, 1f, 1f - t * t);
    }
}
