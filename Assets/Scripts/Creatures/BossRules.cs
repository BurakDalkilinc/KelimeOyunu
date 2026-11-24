using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss'a vurma kuralı: Boşluk'a basan oyuncunun önünde (itiş menzili + boss'un yarıçapı, itiş açısı içinde) duran
/// bosslar 1 hasar alır. Boss'un içine girmiş oyuncu hangi yöne bakarsa baksın vurur. Boss'u bitiren ödül alır:
/// canı eksikse +1 can, canı tamsa bir vuruşluk kalkan. Tek kişilik sahne ve ağ maçı aynı kuralı kullanır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class BossRules
{
    /// <summary>
    /// Vurulabilecek boss sayısı. <paramref name="apply"/> true ise vurur (canlarını düşürür, ölene ödül verir).
    /// </summary>
    public static int HitBosses(IEnumerable<CreatureModel> creatures, PlayerState hitter, Vector2 from, Vector2 facing,
                                GameConfig config, bool apply)
    {
        if (creatures == null || hitter == null || config == null) return 0;

        Vector2 forward = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        float cosLimit = Mathf.Cos(Mathf.Clamp(config.shoveHalfAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);

        int count = 0;
        foreach (CreatureModel boss in creatures)
        {
            if (boss == null || !boss.IsBoss || boss.IsFinished) continue;

            Vector2 delta = boss.Position - from;
            float distance = delta.magnitude;
            if (distance > config.shoveRange + boss.Radius) continue;

            bool inside = distance <= boss.Radius;
            if (!inside && Vector2.Dot(forward, delta / distance) < cosLimit) continue;

            count++;
            if (apply && boss.TakeBossHit(hitter, config.bossHitDamage)) Reward(hitter, config);
        }
        return count;
    }

    /// <summary>
    /// Botun saldıracağı boss: menzildeki en yakın canlı boss. Botun canı azsa, uçuyorsa, düşüyorsa ya da silahı varsa
    /// (silah boss'a işlemez, Boşluk ateş eder) null döner ve bot her zamanki gibi kaçar.
    /// </summary>
    public static CreatureModel PickBossToAttack(IEnumerable<CreatureModel> creatures, PlayerState bot, Vector2 position, GameConfig config)
    {
        if (creatures == null || bot == null || config == null || !bot.IsAlive) return null;
        if (bot.Health.Current < config.botBossAttackMinHealth) return null;
        if (bot.Status.IsFlying || bot.Status.HasWeapon || bot.Falling != FallKind.None) return null;

        CreatureModel best = null;
        float bestDistance = config.botBossAggroRange;
        foreach (CreatureModel boss in creatures)
        {
            if (boss == null || !boss.IsBoss || boss.IsFinished) continue;
            float distance = (boss.Position - position).magnitude - boss.Radius;
            if (distance > bestDistance) continue;
            best = boss;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>Boss'u bitirenin ödülü: canı eksikse can, tamsa kalkan.</summary>
    public static void Reward(PlayerState killer, GameConfig config)
    {
        if (killer == null || !killer.IsAlive) return;

        if (killer.Health.Current < killer.Health.Max) killer.Health.Heal(config.bossKillReward);
        else killer.Status.ApplyShield(1, config.shieldDefaultSeconds);
    }

    /// <summary>Boss'un üstündeki yazı: "Godzilla 7/10".</summary>
    public static string Label(CreatureModel boss)
    {
        if (boss == null || !boss.IsBoss) return string.Empty;
        string name = boss.Word != null ? boss.Word.display : "Boss";
        return $"{name} {boss.BossHealth}/{boss.BossMaxHealth}";
    }
}
