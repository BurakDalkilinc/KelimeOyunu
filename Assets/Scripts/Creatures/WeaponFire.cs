using UnityEngine;

/// <summary>
/// Silahla ateş etme: botun nişan alacağı rakibi bulur ve mermiyi (düz uçan bir <see cref="CreatureModel"/>) kurar.
/// Tek kişilik sahne (<see cref="MatchBootstrap"/>) ve ağ maçı (<see cref="NetMatchSim"/>) aynı hesabı kullanır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class WeaponFire
{
    /// <summary>Merminin namludan ne kadar önde doğacağı (birim): atanın kendisine değmesin.</summary>
    const float MuzzleOffset = 0.55f;

    /// <summary>
    /// Botlar için nişan: <paramref name="range"/> içindeki en yakın hayattaki, uçmayan rakibe doğru birim yön.
    /// Kimse yoksa false.
    /// </summary>
    public static bool TryAimAtNearest(PlayerState shooter, Vector2 from, IPlayerLocator world, float range, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (shooter == null || world == null) return false;

        float best = range * range;
        bool found = false;
        foreach (PlayerState other in world.Players)
        {
            if (other == null || other == shooter || !other.IsAlive || other.Status.IsFlying) continue;
            if (!world.TryGetPosition(other.Id, out Vector2 at)) continue;

            Vector2 delta = at - from;
            float distance = delta.sqrMagnitude;
            if (distance < 0.0001f || distance > best) continue;

            best = distance;
            direction = delta.normalized;
            found = true;
        }
        return found;
    }

    /// <summary>
    /// Mermiyi kurar: <paramref name="from"/> noktasından <paramref name="direction"/> yönünde uçar,
    /// <see cref="GameConfig.weaponRange"/> kadar gidince kaybolur.
    /// </summary>
    public static CreatureModel Build(PlayerState shooter, WordEntry weapon, Vector2 from, Vector2 direction,
                                      GameConfig config, ArenaBounds arena)
    {
        WordStats stats = weapon?.stats;
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        float speed = stats != null && stats.speed > 0f ? stats.speed : config.projectileDefaultSpeed;
        int damage = stats != null && stats.damage > 0f ? Mathf.Max(1, Mathf.RoundToInt(stats.damage)) : 1;
        float radius = stats != null && stats.radius > 0f ? stats.radius : 0.25f;
        float life = config.weaponRange / Mathf.Max(0.1f, speed);

        var model = new CreatureModel(weapon, null, from + dir * MuzzleOffset, speed, damage, radius, life, arena);
        model.MakeProjectile(shooter, dir * speed);
        return model;
    }
}
