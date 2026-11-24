using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir yaratığı bir kare ilerletir: türüne göre doğru <see cref="CreatureModel"/> adımını seçer
/// (yumruk uyarır, mıknatıs çeker, tuzak bekler, bulut alanı takip eder, diğerleri kovalar) ve vurulanları fırlatır.
/// Tek kişilik sahnede <see cref="CreatureView"/>, ağ maçında host'taki simülasyon bunu çağırır; kural tek yerde kalır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class CreatureDriver
{
    static readonly IReadOnlyList<DangerZone> NoDangers = new List<DangerZone>();

    /// <summary>
    /// <paramref name="hits"/> yeniden kullanılan geçici listedir (her çağrıda temizlenir).
    /// <paramref name="wanderBrain"/> yalnızca hedefsiz ("???") yaratık için gerekir.
    /// </summary>
    public static void Advance(CreatureModel model, float dt, ICreatureWorld world, GameConfig config,
                               List<CreatureHit> hits, BotBrain wanderBrain)
    {
        // Mermi düz uçar, değdiği ilk rakibe vurur.
        if (model.IsProjectile)
        {
            model.TickProjectile(dt, world, config.playerRadius);
            return;
        }

        if (model.Target == null)
        {
            if (wanderBrain == null) return;
            Vector2 direction = wanderBrain.Think(dt, model.Position, NoDangers);
            model.Wander(direction, dt);
            return;
        }

        // Anında vuran yaratık (yumruk) koşmaz: uyarı verir, süre dolunca alanındaki herkese vurur.
        if (model.IsStrike)
        {
            hits.Clear();
            model.TickStrike(dt, world, config.playerRadius, hits);
            Knock(world, hits);
            return;
        }

        // Mıknatıs arenanın merkezinde durur ve hedefi çeker.
        if (model.IsPull)
        {
            model.TickPull(dt);
            return;
        }

        // Tuzak (muz kabuğu) yerinde bekler; yarıçapına giren herkesi etkiler ve kaybolur.
        if (model.IsTrap)
        {
            hits.Clear();
            model.TickTrap(dt, world, config.playerRadius, hits);
            Knock(world, hits);
            return;
        }

        // Sürekli alan yaratığı (bulut): hedefi yavaşça takip eder, içindeki herkesi etkiler.
        if (model.IsArea)
        {
            model.TickArea(dt, world, config.playerRadius);
            return;
        }

        bool hasTarget = world.TryGetPosition(model.Target.Id, out Vector2 targetPosition);
        bool hitSomeone = model.Tick(dt, hasTarget ? targetPosition : model.Position, config.playerRadius, config.creatureHitCooldownSeconds);

        if (hitSomeone && model.LastHit.Knockback != Vector2.zero)
            world.TryApplyKnockback(model.LastHit.Player.Id, model.LastHit.Knockback);
    }

    static void Knock(ICreatureWorld world, List<CreatureHit> hits)
    {
        foreach (CreatureHit hit in hits)
            world.TryApplyKnockback(hit.Player.Id, hit.Knockback);
    }
}
