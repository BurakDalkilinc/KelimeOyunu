using UnityEngine;

/// <summary>
/// Arenada bir doğma noktası seçer: verilen noktadan (ör. hedef oyuncu) en az belli bir mesafe uzakta.
/// Düz C# sınıfı; rastgelelik seed'li <see cref="System.Random"/>'dan gelir.
/// </summary>
public static class ArenaSpawn
{
    const int MaxAttempts = 30;

    /// <summary>
    /// Arena içinde, <paramref name="awayFrom"/> noktasından en az <paramref name="minDistance"/> uzakta bir nokta verir.
    /// Böyle nokta bulunamazsa (arena küçük) en uzak köşeyi verir. Her zaman arenanın içindedir.
    /// </summary>
    public static Vector2 PickPoint(ArenaBounds arena, Vector2 awayFrom, float minDistance, System.Random random, float edgeMargin = 0.5f)
    {
        if (arena == null) throw new System.ArgumentNullException(nameof(arena));
        if (random == null) throw new System.ArgumentNullException(nameof(random));

        Vector2 half = arena.HalfSize - Vector2.one * Mathf.Max(0f, edgeMargin);
        half = new Vector2(Mathf.Max(0f, half.x), Mathf.Max(0f, half.y));

        for (int i = 0; i < MaxAttempts; i++)
        {
            var point = new Vector2(
                Mathf.Lerp(-half.x, half.x, (float)random.NextDouble()),
                Mathf.Lerp(-half.y, half.y, (float)random.NextDouble()));

            if ((point - awayFrom).magnitude >= minDistance) return point;
        }

        return FarthestCorner(half, awayFrom);
    }

    /// <summary>
    /// <paramref name="center"/> noktasının <paramref name="distance"/> kadar yanında, rastgele bir yönde bir nokta verir
    /// (arenaya sıkıştırma yapılmaz; yaratık modeli yerleşirken kendisi sıkıştırır).
    /// </summary>
    public static Vector2 PointBeside(Vector2 center, float distance, System.Random random)
    {
        if (random == null) throw new System.ArgumentNullException(nameof(random));

        float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Max(0f, distance);
    }

    /// <summary>
    /// Arenanın kenarında rastgele bir nokta verir (dört kenardan biri, kenar boyunca rastgele bir yer).
    /// Sürü yaratıkları arenaya buradan girer. Nokta <paramref name="inset"/> kadar içeridedir, yani arenanın içindedir.
    /// </summary>
    public static Vector2 PickEdgePoint(ArenaBounds arena, System.Random random, float inset = 0.5f)
    {
        if (arena == null) throw new System.ArgumentNullException(nameof(arena));
        if (random == null) throw new System.ArgumentNullException(nameof(random));

        Vector2 half = arena.HalfSize - Vector2.one * Mathf.Max(0f, inset);
        half = new Vector2(Mathf.Max(0f, half.x), Mathf.Max(0f, half.y));

        float along = (float)random.NextDouble() * 2f - 1f; // -1..1
        switch (random.Next(4))
        {
            case 0: return new Vector2(-half.x, along * half.y);
            case 1: return new Vector2(half.x, along * half.y);
            case 2: return new Vector2(along * half.x, half.y);
            default: return new Vector2(along * half.x, -half.y);
        }
    }

    static Vector2 FarthestCorner(Vector2 half, Vector2 awayFrom)
    {
        Vector2 best = new Vector2(half.x, half.y);
        float bestDistance = -1f;

        foreach (float sx in new[] { -1f, 1f })
        {
            foreach (float sy in new[] { -1f, 1f })
            {
                var corner = new Vector2(sx * half.x, sy * half.y);
                float distance = (corner - awayFrom).magnitude;
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = corner;
                }
            }
        }

        return best;
    }
}
