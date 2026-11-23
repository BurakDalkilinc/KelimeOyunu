using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Karakterlerin birbirinin içinden geçmemesi: üst üste binen iki karakter eşit miktarda birbirinden uzaklaştırılır.
/// Hareket eden karakter önündekini böyle iter; kalabalıkta itme zincirleme yayılır. İtilen kuleden/sığınaktan çıkabilir.
/// Hayalet ve uçan (kanat) karakterler katı değildir, iç içe geçer. Düz hesap, test edilebilir.
/// </summary>
public static class BodyCollisions
{
    /// <summary>Kalabalıkta itmenin yayılması için en çok kaç tur ayrıştırılır.</summary>
    public const int MaxPasses = 6;

    /// <summary>
    /// <paramref name="positions"/> listesindeki katı karakterleri (<paramref name="solid"/>) birbirinden ayırır ve arenanın içinde tutar.
    /// Konumlar yerinde değiştirilir. Herhangi biri kıpırdadıysa true döner.
    /// </summary>
    public static bool Resolve(IList<Vector2> positions, IReadOnlyList<bool> solid, float radius, ArenaBounds arena)
    {
        int count = Mathf.Min(positions.Count, solid.Count);
        float minDistance = radius * 2f;
        bool anyMoved = false;

        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool movedThisPass = false;

            for (int i = 0; i < count; i++)
            {
                if (!solid[i]) continue;

                for (int j = i + 1; j < count; j++)
                {
                    if (!solid[j]) continue;

                    Vector2 delta = positions[j] - positions[i];
                    float distance = delta.magnitude;
                    if (distance >= minDistance) continue;

                    // Tam üst üsteyse yön belirsiz: numaralardan türeyen sabit bir yön seç (sonuç hep aynı olsun).
                    Vector2 normal = distance > 0.0001f ? delta / distance : Fallback(i, j);
                    float push = (minDistance - distance) * 0.5f;

                    positions[i] = positions[i] - normal * push;
                    positions[j] = positions[j] + normal * push;
                    movedThisPass = true;
                }
            }

            if (!movedThisPass) break;
            anyMoved = true;
        }

        if (anyMoved && arena != null)
        {
            for (int i = 0; i < count; i++)
                if (solid[i]) positions[i] = arena.Clamp(positions[i], radius);
        }

        return anyMoved;
    }

    static Vector2 Fallback(int i, int j)
    {
        float angle = (i * 7 + j * 13) * 0.7f;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
