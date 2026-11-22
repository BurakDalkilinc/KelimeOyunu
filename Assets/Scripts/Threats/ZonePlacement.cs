using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Çözüm alanlarının arenadaki yerlerini seçer: kenarlardan uzak, birbirinden en az belli bir mesafede.
/// Uygun yer bulunamazsa mesafe şartı gevşetilir; alan sayısı her zaman istenen kadar olur. Düz hesap.
/// </summary>
public static class ZonePlacement
{
    const int AttemptsPerZone = 60;

    public static List<Vector2> Pick(ArenaBounds arena, int count, float edgeMargin, float minSeparation, System.Random random)
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

                    if (FarEnough(point, result, needed))
                    {
                        chosen = point;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                chosen = new Vector2(Mathf.Lerp(-half.x, half.x, (float)random.NextDouble()), Mathf.Lerp(-half.y, half.y, (float)random.NextDouble()));

            result.Add(chosen);
        }
        return result;
    }

    static bool FarEnough(Vector2 point, List<Vector2> others, float distance)
    {
        foreach (Vector2 other in others)
            if ((other - point).magnitude < distance) return false;
        return true;
    }
}
