using UnityEngine;

/// <summary>
/// Fareyle tıklanan (ya da üstünde durulan) dünya noktasının hangi oyuncuya denk geldiğini bulur.
/// Yazan kişi ve elenenler seçilemez. Birden fazla oyuncu yakınsa en yakın olan seçilir.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class TargetHitTester
{
    /// <summary>
    /// <paramref name="point"/> noktasına <paramref name="radius"/> kadar ya da daha yakın, seçilebilir en yakın oyuncuyu verir;
    /// yoksa null.
    /// </summary>
    public static PlayerState FindAt(Vector2 point, IPlayerLocator world, PlayerState writer, float radius)
    {
        if (world == null) throw new System.ArgumentNullException(nameof(world));

        PlayerState best = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerState player in world.Players)
        {
            if (player == null || player == writer || !player.IsAlive || player.Status.IsStealthed) continue;
            if (!world.TryGetPosition(player.Id, out Vector2 position)) continue;

            float distance = (position - point).magnitude;
            if (distance <= radius && distance < bestDistance)
            {
                best = player;
                bestDistance = distance;
            }
        }

        return best;
    }
}
