using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ağda istemci yaratığın kime vurduğunu bilmez: vuruş anında yaratığa en yakın (ve yeterince yakın) karakter hedef sayılır.
/// Yalnızca görüntü içindir (vuruş izinin yeri, atılma yönü). Düz C#, test edilebilir.
/// </summary>
public static class AttackTargetFinder
{
    /// <summary><paramref name="from"/>'a en yakın, en çok <paramref name="maxDistance"/> uzaktaki nokta; yoksa null.</summary>
    public static Vector2? Nearest(Vector2 from, IReadOnlyList<Vector2> points, float maxDistance)
    {
        if (points == null) return null;

        Vector2? best = null;
        float bestDistance = maxDistance;
        foreach (Vector2 point in points)
        {
            float distance = (point - from).magnitude;
            if (distance <= bestDistance)
            {
                best = point;
                bestDistance = distance;
            }
        }
        return best;
    }
}
