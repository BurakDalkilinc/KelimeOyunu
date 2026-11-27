using UnityEngine;

/// <summary>
/// Kılıç/sopa savuruşu: önce kısa hazırlık (silah kalkık durur ve parlar, vuracağı yay kırmızı noktalarla görünür; hedef kaçabilir),
/// sonra silahın resmi vuranın önünde kısa bir yay çizerek döner, arkasında beyaz bir iz bırakır. Yalnızca görüntüdür;
/// vuruş kuralı <see cref="MeleeRules"/>'tadır.
/// </summary>
public class MeleeSwingEffect : MonoBehaviour
{
    const float Seconds = 0.2f;
    const float ArcDegrees = 140f;
    const int PreviewDots = 9;

    static readonly Color PreviewColor = new Color(1f, 0.25f, 0.2f, 1f);
    static readonly Color GlowColor = new Color(1f, 0.92f, 0.45f, 1f);

    float age;
    float windup;
    float reach;
    float baseAngle;
    Vector2 origin;
    Transform follow;
    SpriteRenderer weapon;
    SpriteRenderer trail;
    SpriteRenderer[] dots;

    /// <param name="windupSeconds">Savuruştan önceki hazırlık (sn); 0 ise hemen savrulur.</param>
    /// <param name="follower">Verilirse efekt vuranın üstünde kalır (hazırlıkta yürüyebilir); yoksa <paramref name="from"/>'da durur.</param>
    public static void Spawn(Vector2 from, Vector2 facing, float reach, Sprite sprite, float windupSeconds = 0f, Transform follower = null)
    {
        var go = new GameObject("Savuruş");
        var effect = go.AddComponent<MeleeSwingEffect>();
        effect.origin = from;
        effect.follow = follower;
        effect.windup = Mathf.Max(0f, windupSeconds);
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

        // Vuruş yayının ön izlemesi: yayın üstünde küçük kırmızı noktalar (dünyada sabit, efektle birlikte silinir).
        effect.dots = new SpriteRenderer[PreviewDots];
        for (int i = 0; i < PreviewDots; i++)
        {
            var dotGo = new GameObject("Savuruş yayı");
            dotGo.transform.SetParent(go.transform, false);
            effect.dots[i] = dotGo.AddComponent<SpriteRenderer>();
            effect.dots[i].sprite = SpriteFactory.Circle();
            effect.dots[i].sortingOrder = 11;
        }
        effect.Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= windup + Seconds) { Destroy(gameObject); return; }
        Apply();
    }

    Vector2 Origin => follow != null ? (Vector2)follow.position : origin;

    void Apply()
    {
        Vector2 center = Origin;
        bool windingUp = age < windup;
        float t = windingUp ? 0f : Mathf.Clamp01((age - windup) / Seconds);
        float build = windup > 0f ? Mathf.Clamp01(age / windup) : 1f; // Hazırlık ne kadar doldu

        // Silah: hazırlıkta yayın başında kalkık durur ve parlar; sonra yay boyunca döner.
        float angle = baseAngle + Mathf.Lerp(ArcDegrees * 0.5f, -ArcDegrees * 0.5f, t);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        transform.position = center + dir * (reach * 0.6f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f); // Resimdeki silah çapraz (sağ üst) çizili

        if (windingUp)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(age * 50f);
            transform.localScale = Vector3.one * (1.1f + 0.15f * build);
            weapon.color = Color.Lerp(Color.white, GlowColor, pulse);
            trail.color = Color.clear;
        }
        else
        {
            transform.localScale = Vector3.one * 1.1f;
            trail.transform.localScale = new Vector3(reach * 0.9f, 0.25f, 1f);
            trail.color = new Color(1f, 1f, 1f, 0.5f * (1f - t));
            weapon.color = new Color(1f, 1f, 1f, 1f - t * t);
        }

        // Yayın ön izlemesi: hazırlıkta kırmızılaşarak belirir, savuruşta söner. Noktalar dünyada yerleşir (silahla dönmez).
        float dotAlpha = windingUp ? 0.25f + 0.55f * build : 0.8f * (1f - t);
        for (int i = 0; i < dots.Length; i++)
        {
            float along = dots.Length > 1 ? i / (float)(dots.Length - 1) : 0.5f;
            float a = (baseAngle + Mathf.Lerp(ArcDegrees * 0.5f, -ArcDegrees * 0.5f, along)) * Mathf.Deg2Rad;
            Transform dot = dots[i].transform;
            dot.position = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach;
            dot.rotation = Quaternion.identity;
            SetWorldScale(dot, 0.16f);
            dots[i].color = new Color(PreviewColor.r, PreviewColor.g, PreviewColor.b, dotAlpha);
        }
    }

    /// <summary>Çocuğun dünyadaki boyutunu sabitler (ebeveyn ölçeklense de).</summary>
    void SetWorldScale(Transform child, float size)
    {
        Vector3 parent = transform.lossyScale;
        child.localScale = new Vector3(size / Mathf.Max(0.0001f, parent.x), size / Mathf.Max(0.0001f, parent.y), 1f);
    }
}
