using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Çözüm alanlarının arenadaki yerlerini seçer: kenarlardan uzak, birbirinden en az belli bir mesafede.
/// Uygun yer bulunamazsa mesafe şartı gevşetilir; alan sayısı her zaman istenen kadar olur. Düz hesap.
/// </summary>
public static class ZonePlacement
{
    const int AttemptsPerZone = 60;

    public static List<Vector2> Pick(ArenaBounds arena, int count, float edgeMargin, float minSeparation, System.Random random,
                                     System.Func<Vector2, bool> isBlockedPoint = null)
    {
        if (arena == null) throw new System.ArgumentNullException(nameof(arena));
        if (random == null) throw new System.ArgumentNullException(nameof(random));

        var result = new List<Vector2>();
        Vector2 half = arena.HalfSize - Vector2.one * Mathf.Max(0f, edgeMargin);
        half = new Vector2(Mathf.Max(0f, half.x), Mathf.Max(0f, half.y));

        float separation = Mathf.Max(0f, minSeparation);
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            Vector2 chosen = default;
            bool found = false;

            // Mesafe şartını tutturamazsak her turda biraz gevşet.
            for (int round = 0; round < 4 && !found; round++)
            {
                float needed = separation * (1f - 0.25f * round);
                for (int attempt = 0; attempt < AttemptsPerZone; attempt++)
                {
                    var point = new Vector2(
                        Mathf.Lerp(-half.x, half.x, (float)random.NextDouble()),
                        Mathf.Lerp(-half.y, half.y, (float)random.NextDouble()));

                    if (!IsClear(point, isBlockedPoint)) continue;

                    if (FarEnough(point, result, needed))
                    {
                        chosen = point;
                        found = true;
                        break;
                    }
                }
            }

            // Hiç uygun yer bulunamazsa (çok dolu harita) engel bakmadan rastgele bir yere koy.
            if (!found)
                chosen = new Vector2(Mathf.Lerp(-half.x, half.x, (float)random.NextDouble()), Mathf.Lerp(-half.y, half.y, (float)random.NextDouble()));

            result.Add(chosen);
        }
        return result;
    }

    /// <summary>Alanın merkezi ve çevresindeki dört nokta (1,2 birim) duvar/lav/uçurum üstünde değilse temiz sayılır.</summary>
    static bool IsClear(Vector2 point, System.Func<Vector2, bool> isBlockedPoint)
    {
        if (isBlockedPoint == null) return true;
        const float reach = 1.2f;
        return !isBlockedPoint(point)
            && !isBlockedPoint(point + Vector2.right * reach) && !isBlockedPoint(point + Vector2.left * reach)
            && !isBlockedPoint(point + Vector2.up * reach) && !isBlockedPoint(point + Vector2.down * reach);
    }

    static bool FarEnough(Vector2 point, List<Vector2> others, float distance)
    {
        foreach (Vector2 other in others)
            if ((other - point).magnitude < distance) return false;
        return true;
    }
}
