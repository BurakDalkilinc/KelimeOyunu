using UnityEngine;

/// <summary>
/// Kelime büyüsünde harflerin uçtuğu eğri yol: ikinci dereceden Bezier eğrisi. Başlangıç ile bitişin ortasından, yola dik
/// yönde bükülen bir kontrol noktası seçilir; harfler sırayla sağa/sola bükülerek yelpaze gibi açılır.
/// </summary>
public static class ArcPath
{
    /// <summary>Eğrinin <paramref name="t"/> (0..1) anındaki noktası.</summary>
    public static Vector2 Point(Vector2 from, Vector2 control, Vector2 to, float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return u * u * from + 2f * u * t * control + t * t * to;
    }

    /// <summary>Eğrinin <paramref name="t"/> anındaki yönü (türevi; normalleştirilmemiş).</summary>
    public static Vector2 Tangent(Vector2 from, Vector2 control, Vector2 to, float t)
    {
        t = Mathf.Clamp01(t);
        return 2f * (1f - t) * (control - from) + 2f * t * (to - control);
    }

    /// <summary>
    /// Kontrol noktası: yolun ortası, yola dik yönde yol uzunluğunun <paramref name="bend"/> katı kaydırılmış.
    /// <paramref name="side"/> +1 ya da -1 (bükülme yönü); 0 düz çizgi verir.
    /// </summary>
    public static Vector2 Control(Vector2 from, Vector2 to, float bend, float side)
    {
        Vector2 middle = (from + to) * 0.5f;
        Vector2 path = to - from;
        if (path.sqrMagnitude < 1e-8f) return middle;
        Vector2 normal = new Vector2(-path.y, path.x); // Sola dik (uzunluğu yol uzunluğu kadar)
        return middle + normal * (bend * Mathf.Sign(side) * Mathf.Min(1f, Mathf.Abs(side)));
    }

    /// <summary>Harfin bükülme yönü: sırayla +1, -1, +1... (tek harfte +1).</summary>
    public static float SideFor(int index)
    {
        return index % 2 == 0 ? 1f : -1f;
    }
}
