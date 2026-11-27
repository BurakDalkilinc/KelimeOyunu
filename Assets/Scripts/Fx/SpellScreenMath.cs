using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kelime büyüsünün ekran hesapları: kamera dışındaki hedefi ekran kenarına sıkıştırma ve büyük tehdit uyarı üçgenlerinin
/// ekran kenarındaki yerleri ile içeri bakan açıları. Ekran koordinatları IMGUI'deki gibidir (sol üst 0,0).
/// </summary>
public static class SpellScreenMath
{
    /// <summary>Nokta ekranın içinde mi (kenardan <paramref name="inset"/> içeride)?</summary>
    public static bool IsInside(Vector2 point, Vector2 screen, float inset)
    {
        return point.x >= inset && point.x <= screen.x - inset && point.y >= inset && point.y <= screen.y - inset;
    }

    /// <summary>
    /// Ekran dışındaki noktayı, ekran ortasından o noktaya giden çizginin kenarı kestiği yere çeker (kenardan
    /// <paramref name="inset"/> içeride). İçerideki nokta aynen döner.
    /// </summary>
    public static Vector2 ClampToEdge(Vector2 point, Vector2 screen, float inset)
    {
        if (IsInside(point, screen, inset)) return point;
        Vector2 center = screen * 0.5f;
        Vector2 half = new Vector2(Mathf.Max(1f, center.x - inset), Mathf.Max(1f, center.y - inset));
        Vector2 dir = point - center;
        if (dir.sqrMagnitude < 1e-6f) return center;
        float scale = Mathf.Min(Mathf.Abs(dir.x) > 1e-6f ? half.x / Mathf.Abs(dir.x) : float.MaxValue,
                                Mathf.Abs(dir.y) > 1e-6f ? half.y / Mathf.Abs(dir.y) : float.MaxValue);
        return center + dir * scale;
    }

    /// <summary>
    /// Ekran kenarı boyunca eşit aralıklı <paramref name="count"/> uyarı yeri (çevre üzerinde, kenardan <paramref name="inset"/> içeride)
    /// ve her birinin ekran ortasına bakan açısı (derece; 0 = sağa, saat yönü +, IMGUI'de y aşağı).
    /// </summary>
    public static void EdgeMarkers(int count, Vector2 screen, float inset, List<Vector2> positions, List<float> angles)
    {
        positions.Clear();
        angles.Clear();
        if (count <= 0) return;

        float w = Mathf.Max(1f, screen.x - 2f * inset), h = Mathf.Max(1f, screen.y - 2f * inset);
        float perimeter = 2f * (w + h);
        Vector2 center = screen * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float d = (i + 0.5f) / count * perimeter;
            Vector2 p;
            if (d < w) p = new Vector2(inset + d, inset);
            else if (d < w + h) p = new Vector2(inset + w, inset + (d - w));
            else if (d < 2f * w + h) p = new Vector2(inset + w - (d - w - h), inset + h);
            else p = new Vector2(inset, inset + h - (d - 2f * w - h));
            positions.Add(p);
            Vector2 toCenter = center - p;
            angles.Add(Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg);
        }
    }
}
