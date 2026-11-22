/// <summary>
/// Hareket kuralları: bir oyuncunun hangi engellere (duvarlar) takıldığı. Hayaletler (elenenler) ve uçanlar (kanat)
/// engellerden geçer. Düz hesap, test edilebilir.
/// </summary>
public static class MovementRules
{
    /// <summary>
    /// Oyuncunun hareketini sınırlayacak engeller. Hayalet ya da uçan oyuncu için null (engel yok); yaşayan ve uçmayan
    /// oyuncu için verilen engel alanı.
    /// </summary>
    public static IObstacleField EffectiveObstacles(PlayerState player, IObstacleField obstacles)
    {
        if (player == null || player.IsGhost || player.Status.IsFlying) return null;
        return obstacles;
    }
}
