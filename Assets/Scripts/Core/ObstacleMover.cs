using UnityEngine;

/// <summary>
/// Bir noktadan diğerine, arenanın içinde kalarak ve duvarların içinden geçmeden hareket ettirir.
/// Hareketi küçük alt adımlara böler ve her adımda duvarlara iter; böylece çok hızlı hareket ya da ağır bir kare
/// (takılma) karakterin duvarı tek adımda atlamasına yol açmaz. Düz hesap, test edilebilir.
/// </summary>
public static class ObstacleMover
{
    const int MaxSteps = 64;

    /// <summary>
    /// <paramref name="from"/> noktasından <paramref name="to"/> noktasına gider. Yarıçap <paramref name="radius"/> olan
    /// daire duvarlara girmez; arenanın kenarından <paramref name="arenaMargin"/> içeride kalır.
    /// <paramref name="obstacles"/> null ise yalnızca arena sınırı uygulanır (uçanlar için).
    /// </summary>
    public static Vector2 Move(Vector2 from, Vector2 to, float radius, ArenaBounds arena, float arenaMargin, IObstacleField obstacles)
    {
        if (arena == null) throw new System.ArgumentNullException(nameof(arena));

        Vector2 delta = to - from;
        if (obstacles == null)
            return arena.Clamp(to, arenaMargin);

        // Adım uzunluğu yarıçapın yarısından küçük olsun: daire bir adımda duvarın merkezini aşamaz.
        float maxStep = Mathf.Max(0.05f, radius * 0.5f);
        int steps = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / maxStep), 1, MaxSteps);
        Vector2 stepVector = delta / steps;

        Vector2 position = from;
        for (int i = 0; i < steps; i++)
        {
            position = arena.Clamp(position + stepVector, arenaMargin);
            position = arena.Clamp(obstacles.Resolve(position, radius), arenaMargin);
        }
        return position;
    }
}
