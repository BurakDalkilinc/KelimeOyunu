using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yakın dövüş silahı (kılıç, sopa) kuralı: vuran oyuncunun önündeki yay içinde (menzil = kelimenin "stats.radius"u,
/// yoksa itiş menzili; açı = itiş açısı) duran, hayattaki ve uçmayan herkes kelimenin hasarını alır (kalkan korur).
/// Savuruşun önce kısa bir hazırlığı vardır (<see cref="GameConfig.swordWindupSeconds"/>: silah parlar, hedef kaçabilir), iki
/// savuruş arasında da bekleme (<see cref="GameConfig.swordSwingCooldownSeconds"/>). Tek kişilik sahne ve ağ maçı aynı kuralı kullanır. Düz C# sınıfı, test edilebilir.
/// </summary>
public static class MeleeRules
{
    /// <summary>Bu silah yakından mı vurur ("melee" etiketi)?</summary>
    public static bool IsMelee(WordEntry weapon)
    {
        if (weapon?.tags == null) return false;
        foreach (string tag in weapon.tags)
            if (tag == MeleeTag.Name) return true;
        return false;
    }

    /// <summary>Silahın erişimi (birim).</summary>
    public static float Reach(WordEntry weapon, GameConfig config)
    {
        float radius = weapon?.stats != null ? weapon.stats.radius : 0f;
        return radius > 0f ? radius : config.shoveRange;
    }

    /// <summary>Bir vuruşun hasarı (en az 1).</summary>
    public static int Damage(WordEntry weapon)
    {
        float damage = weapon?.stats != null ? weapon.stats.damage : 0f;
        return damage > 0f ? Mathf.Max(1, Mathf.RoundToInt(damage)) : 1;
    }

    /// <summary>
    /// Önündeki kurbanları <paramref name="victims"/>'e yazar ve sayısını döner. <paramref name="apply"/> true ise hasarı da verir.
    /// </summary>
    public static int Strike(PlayerState attacker, Vector2 from, Vector2 facing, WordEntry weapon, IPlayerLocator world,
                             GameConfig config, bool apply, List<PlayerState> victims)
    {
        victims?.Clear();
        if (attacker == null || world == null || config == null) return 0;

        Vector2 forward = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        float cosLimit = Mathf.Cos(Mathf.Clamp(config.shoveHalfAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);
        float reach = Reach(weapon, config) + config.playerRadius;
        int damage = Damage(weapon);

        int count = 0;
        foreach (PlayerState other in world.Players)
        {
            if (other == null || other == attacker || !other.IsAlive || other.Status.IsFlying) continue;
            if (!world.TryGetPosition(other.Id, out Vector2 at)) continue;

            Vector2 delta = at - from;
            float distance = delta.magnitude;
            if (distance > reach) continue;
            if (distance > 0.0001f && Vector2.Dot(forward, delta / distance) < cosLimit) continue;

            count++;
            victims?.Add(other);
            if (apply) other.TakeDamage(damage);
        }
        return count;
    }

    /// <summary>
    /// Savuruşu başlatır: bir atış harcanır, silah <see cref="GameConfig.swordWindupSeconds"/> kadar parlar, vuruş o süre dolunca
    /// <paramref name="facing"/> yönüne yapılır (<see cref="TickSwing"/>). Hazırlık sürüyorsa, bekleme dolmadıysa ya da yakın
    /// silah yoksa false.
    /// </summary>
    public static bool TryBeginSwing(PlayerState attacker, Vector2 facing, GameConfig config)
    {
        if (attacker == null || config == null || !attacker.IsAlive || attacker.Status.Swing.IsWindingUp) return false;
        if (!IsMelee(attacker.Status.Weapon)) return false;

        float windup = Mathf.Max(0f, config.swordWindupSeconds);
        float wait = windup + Mathf.Max(0f, config.swordSwingCooldownSeconds); // Bekleme vuruştan sonra sayılır
        if (!attacker.Status.TryUseWeaponShot(wait, out WordEntry used)) return false;

        attacker.Status.Swing.Begin(used, facing, windup);
        return true;
    }

    /// <summary>
    /// Hazırlıktaki savuruşun zamanını ilerletir; süre dolunca vuranın o anki konumundan, hazırlıkta kilitlenen yöne vurur.
    /// Vuruş bu karede yapıldıysa true (kurbanlar <paramref name="victims"/>'te). Vuran elendiyse vuruş olmaz.
    /// </summary>
    public static bool TickSwing(PlayerState attacker, float deltaSeconds, IPlayerLocator world, GameConfig config, List<PlayerState> victims)
    {
        victims?.Clear();
        if (attacker == null) return false;

        MeleeWindup swing = attacker.Status.Swing;
        if (!swing.Tick(deltaSeconds)) return false;
        if (!attacker.IsAlive || world == null || !world.TryGetPosition(attacker.Id, out Vector2 from)) return false;

        Strike(attacker, from, swing.Facing, swing.Weapon, world, config, true, victims);
        return true;
    }

    /// <summary>
    /// Yakın silahlı botun kovalayacağı kurban: menzildeki en yakın, hayattaki, uçmayan ve görünmez olmayan rakip.
    /// </summary>
    public static bool TryFindPrey(PlayerState hunter, Vector2 from, IPlayerLocator world, float range, out Vector2 preyPosition)
    {
        preyPosition = Vector2.zero;
        if (hunter == null || world == null) return false;

        float best = range * range;
        bool found = false;
        foreach (PlayerState other in world.Players)
        {
            if (other == null || other == hunter || !other.IsAlive || other.Status.IsFlying || other.Status.IsStealthed) continue;
            if (!world.TryGetPosition(other.Id, out Vector2 at)) continue;

            float distance = (at - from).sqrMagnitude;
            if (distance > best) continue;
            best = distance;
            preyPosition = at;
            found = true;
        }
        return found;
    }
}
