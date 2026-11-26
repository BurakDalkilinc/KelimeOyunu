using UnityEngine;

/// <summary>
/// Host'ta bir karakterin bir karede nasıl hareket ettiğini hesaplar: istemcinin gönderdiği yön sınırlanır
/// (hız hilesi olmasın), karakter arenanın içinde kalır ve (verilirse) duvarlardan geçmez.
/// Düz hesap, test edilebilir. Hareketin kendisi <see cref="ObstacleMover"/>'dadır.
/// </summary>
public static class AvatarMover
{
    /// <summary>
    /// İstemciden gelen yönü güvenli hale getirir: sayı değilse sıfır, uzunluğu 1'den büyükse 1'e indirilir.
    /// (Çapraz giderken hızlı olmak ya da dev değer göndermek işe yaramaz.)
    /// </summary>
    public static Vector2 SanitizeInput(Vector2 input)
    {
        if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.x) || float.IsInfinity(input.y))
            return Vector2.zero;

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    /// <summary>
    /// <paramref name="position"/> konumundaki karakteri bir kare (<paramref name="deltaSeconds"/>) ilerletir;
    /// yeni konumu verir. <paramref name="obstacles"/> null ise yalnızca arena sınırı uygulanır.
    /// </summary>
    public static Vector2 Step(Vector2 position, Vector2 input, float speed, float deltaSeconds,
                               float radius, ArenaBounds arena, IObstacleField obstacles)
    {
        Vector2 direction = SanitizeInput(input);
        if (direction == Vector2.zero || speed <= 0f || deltaSeconds <= 0f) return arena.Clamp(position, radius);

        Vector2 target = position + direction * (speed * deltaSeconds);
        return ObstacleMover.Move(position, target, radius, arena, radius, obstacles);
    }

    /// <summary>
    /// Karakteri hazır bir yer değiştirme kadar oynatır (zemine göre hesaplanmış yürüyüş, <see cref="GroundMotion"/>):
    /// arenanın içinde kalır ve (verilirse) duvarlardan geçmez. Yer değiştirme sıfırsa yalnızca arena sınırı uygulanır.
    /// </summary>
    public static Vector2 Displace(Vector2 position, Vector2 delta, float radius, ArenaBounds arena, IObstacleField obstacles)
    {
        if (delta == Vector2.zero) return arena.Clamp(position, radius);
        return ObstacleMover.Move(position, position + delta, radius, arena, radius, obstacles);
    }
}
