using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Araç (motor, araba) kuralları: araç süresi bitene kadar hiç durmaz, oyuncu yalnızca yön verir; arenanın kenarından seker.
/// Değdiği kişiyi ezer (kelimenin hasarı, kalkan korur; her kişi bir sürüşte bir kez ezilir; "stats.count" kadar ezince araç durur). "explosive" etiketli araç bir
/// binaya/duvara çarparsa patlar: sürücü ölür, çevredekiler hasar alıp savrulur, tahtalar tutuşur. Patlamayan araç çarpınca durur.
/// Tek kişilik sahne ve ağ maçı aynı kuralı kullanır. Düz C# sınıfı, test edilebilir.
/// </summary>
public static class VehicleRules
{
    const float SubStep = 0.15f;

    /// <summary>Aracın hızı (birim/sn): kelimenin "stats.speed"i, yoksa ayardaki varsayılan.</summary>
    public static float Speed(WordEntry vehicle, GameConfig config)
    {
        float speed = vehicle?.stats != null ? vehicle.stats.speed : 0f;
        return speed > 0f ? speed : config.vehicleDefaultSpeed;
    }

    /// <summary>Aracın gövde yarıçapı (ezme alanı): kelimenin "stats.radius"u, yoksa varsayılan.</summary>
    public static float BodyRadius(WordEntry vehicle, GameConfig config)
    {
        float radius = vehicle?.stats != null ? vehicle.stats.radius : 0f;
        return radius > 0f ? radius : config.vehicleDefaultRadius;
    }

    /// <summary>Ezme hasarı (en az 1).</summary>
    public static int CrushDamage(WordEntry vehicle)
    {
        float damage = vehicle?.stats != null ? vehicle.stats.damage : 0f;
        return damage > 0f ? Mathf.Max(1, Mathf.RoundToInt(damage)) : 1;
    }

    /// <summary>Kaç kişiyi ezince araç durur ("stats.count"); 0 ise sınırsız.</summary>
    public static int MaxCrushes(WordEntry vehicle) => vehicle?.stats != null ? Mathf.Max(0, vehicle.stats.count) : 0;

    /// <summary>Çarpınca patlar mı ("explosive" etiketi)?</summary>
    public static bool Explodes(WordEntry vehicle)
    {
        if (vehicle?.tags == null) return false;
        foreach (string tag in vehicle.tags)
            if (tag == "explosive") return true;
        return false;
    }

    /// <summary>
    /// Aracı bir kare sürer ve yeni konumu döner. <paramref name="input"/> varsa yön ona döner, yoksa eski yönde devam eder.
    /// Arenanın kenarına değince seker. Önünde katı bir engel (ev duvarı, duvar) varsa durur ve <paramref name="crashed"/> true olur.
    /// </summary>
    public static Vector2 Drive(Vector2 position, ref Vector2 heading, Vector2 input, float speed, float deltaSeconds,
                                float radius, ArenaBounds arena, IObstacleField solid, out bool crashed)
    {
        crashed = false;
        if (input.sqrMagnitude > 0.0001f) heading = input.normalized;
        if (heading.sqrMagnitude < 0.0001f) heading = Vector2.up;

        float distance = Mathf.Max(0f, speed * deltaSeconds);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / SubStep));
        float stepLength = distance / steps;

        Vector2 pos = position;
        for (int i = 0; i < steps; i++)
        {
            Vector2 next = pos + heading * stepLength;

            if (solid != null && (solid.Resolve(next, radius) - next).sqrMagnitude > 0.000001f)
            {
                crashed = true;
                return pos;
            }

            if (arena != null)
            {
                Vector2 clamped = arena.Clamp(next, radius);
                if (clamped != next)
                {
                    // Kenardan sek: değen eksende yön tersine döner.
                    if (Mathf.Abs(clamped.x - next.x) > 0.00001f) heading.x = -heading.x;
                    if (Mathf.Abs(clamped.y - next.y) > 0.00001f) heading.y = -heading.y;
                    next = clamped;
                }
            }
            pos = next;
        }
        return pos;
    }

    /// <summary>
    /// Aracın gövdesine değen (sürücü dışında, hayattaki, uçmayan) herkesi ezer. Bir sürüşte aynı kişi bir kez ezilir.
    /// Ezilenlerin sayısını döner.
    /// </summary>
    public static int Crush(PlayerState driver, Vector2 position, IPlayerLocator world, GameConfig config, List<PlayerState> victims = null,
                            Func<PlayerState, bool> isSheltered = null)
    {
        victims?.Clear();
        if (driver == null || world == null || !driver.Status.IsDriving) return 0;

        WordEntry vehicle = driver.Status.Vehicle;
        float reach = BodyRadius(vehicle, config) + config.playerRadius;
        int damage = CrushDamage(vehicle);

        int count = 0;
        foreach (PlayerState other in world.Players)
        {
            if (other == null || other == driver || !other.IsAlive || other.Status.IsFlying) continue;
            if (isSheltered != null && isSheltered(other)) continue; // Sığınaktaki (tehdidin çözümü) ezilmez
            if (!world.TryGetPosition(other.Id, out Vector2 at) || (at - position).magnitude > reach) continue;
            if (!driver.Status.MarkVehicleHit(other.Id)) continue;

            int before = other.Health.Current;
            other.TakeDamage(damage);
            victims?.Add(other);
            count++;

            // Ezme sınırı (araba: 1 kişi): dolunca araç durur, sürücü sağ salim iner.
            int limit = MaxCrushes(vehicle);
            if (other.Health.Current < before && limit > 0 && driver.Status.CountVehicleCrush() >= limit)
            {
                driver.Status.Dismount();
                break;
            }
        }
        return count;
    }

    /// <summary>
    /// Çarpma: patlayan araçta sürücü ölür, patlama yarıçapındaki diğerleri hasar alıp dışarı savrulur ve tahtalar tutuşur.
    /// Patlamayan araç yalnızca durur (sürücü iner). Patladıysa true döner.
    /// </summary>
    public static bool Crash(PlayerState driver, Vector2 at, IPlayerLocator world, Action<int, Vector2> knockback,
                             FireSim fire, GameConfig config)
    {
        if (driver == null || !driver.Status.IsDriving) return false;

        WordEntry vehicle = driver.Status.Vehicle;
        driver.Status.Dismount();
        if (!Explodes(vehicle)) return false;

        driver.Eliminate(); // Kalkan da kurtarmaz

        float radius = config.vehicleCrashRadius;
        if (world != null)
        {
            foreach (PlayerState other in world.Players)
            {
                if (other == null || other == driver || !other.IsAlive) continue;
                if (!world.TryGetPosition(other.Id, out Vector2 position)) continue;

                Vector2 delta = position - at;
                float distance = delta.magnitude;
                if (distance > radius + config.playerRadius) continue;

                other.TakeFireDamage(config.vehicleCrashDamage, config); // Patlama: yanar ve kömürleşir
                Vector2 away = distance > 0.0001f ? delta / distance : Vector2.up;
                knockback?.Invoke(other.Id, away * config.shoveDistance);
            }
        }

        fire?.IgniteArea(at, radius);
        return true;
    }
}
