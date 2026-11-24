using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Botun "beyni": nereye gideceğine karar verir. Normalde arenada rastgele noktalara gidip bekler;
/// yakında tehlikeli alan varsa ondan uzaklaşır. Düz C# sınıfı; hareketi <see cref="BotController"/> yapar.
/// Rastgelelik tek bir seed'li <see cref="System.Random"/>'dan gelir (maç tekrar oynatılabilir).
/// </summary>
public class BotBrain
{
    readonly GameConfig config;
    readonly ArenaBounds arena;
    readonly System.Random random;

    Vector2 target;
    bool hasTarget;
    float pauseLeft;
    float bestDistance;
    float stuckFor;

    /// <summary>Takılan bot (hedefe yaklaşamıyor) bu kadar saniye sonra yeni hedef seçer.</summary>
    const float StuckSeconds = 1.2f;

    /// <summary>Hedef seçilirken kaç kez yeniden denenir (kötü yere düşerse).</summary>
    const int TargetTries = 12;

    /// <summary>
    /// Gezinme hedefi olamayacak yerler (lav, uçurum, duvar ve kenarları). Boşsa her yer olur.
    /// Bot böylece uçuruma doğru yürüyüp kenarında takılı kalmaz.
    /// </summary>
    public System.Func<Vector2, bool> IsBadTarget { get; set; }

    public BotBrain(GameConfig config, ArenaBounds arena, System.Random random)
    {
        this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        this.arena = arena ?? throw new System.ArgumentNullException(nameof(arena));
        this.random = random ?? throw new System.ArgumentNullException(nameof(random));
    }

    /// <summary>Şu anki gezinme hedefi (yoksa konumun kendisi). Test ve hata ayıklama için.</summary>
    public Vector2 Target => target;

    /// <summary>
    /// Bu karede gidilecek yönü verir (uzunluğu 1 ya da bekliyorsa 0).
    /// Yakında tehlike varsa kaçar, yoksa gezinir.
    /// </summary>
    public Vector2 Think(float deltaSeconds, Vector2 position, IReadOnlyList<DangerZone> dangers)
    {
        return Think(deltaSeconds, position, dangers, null);
    }

    /// <summary>
    /// Yönü verir. Öncelik: yakındaki tehlikeden kaçmak, sonra (varsa) <paramref name="refuge"/> sığınağına gitmek,
    /// yoksa gezinmek. Sığınağa varınca (merkeze yaklaşınca) durur.
    /// </summary>
    public Vector2 Think(float deltaSeconds, Vector2 position, IReadOnlyList<DangerZone> dangers, Vector2? refuge)
    {
        return Think(deltaSeconds, position, dangers, refuge, false);
    }

    /// <summary>
    /// Yukarıdakinin aynısı; <paramref name="sheltered"/> true ise bot zaten bir çözüm alanında korunuyordur: tehlikeden
    /// kaçmaz, sığınağında kalır (korunduğu yerden çıkıp ölmesin).
    /// </summary>
    public Vector2 Think(float deltaSeconds, Vector2 position, IReadOnlyList<DangerZone> dangers, Vector2? refuge, bool sheltered)
    {
        if (sheltered && refuge.HasValue)
        {
            hasTarget = false;
            pauseLeft = 0f;
            Vector2 toShelter = refuge.Value - position;
            return toShelter.magnitude <= config.botArrivalDistance ? Vector2.zero : toShelter.normalized;
        }

        if (TryFlee(position, dangers, out Vector2 fleeDirection))
        {
            hasTarget = false; // Kaçtıktan sonra yeni bir hedef seçilsin.
            pauseLeft = 0f;
            return fleeDirection;
        }

        if (refuge.HasValue)
        {
            hasTarget = false;
            pauseLeft = 0f;

            Vector2 toRefuge = refuge.Value - position;
            if (toRefuge.magnitude <= config.botArrivalDistance) return Vector2.zero; // Vardı, orada kalır.
            return toRefuge.normalized;
        }

        return Wander(deltaSeconds, position);
    }

    bool TryFlee(Vector2 position, IReadOnlyList<DangerZone> dangers, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (dangers == null) return false;

        Vector2 away = Vector2.zero;
        bool threatened = false;

        for (int i = 0; i < dangers.Count; i++)
        {
            DangerZone danger = dangers[i];
            Vector2 delta = position - danger.Position;
            float distance = delta.magnitude;
            float reach = danger.Radius + config.botFleeMargin;
            if (distance >= reach) continue;

            threatened = true;
            Vector2 push = distance > 0.0001f ? delta / distance : RandomDirection();
            // Tehlikeye ne kadar yakınsa o kadar güçlü kaç.
            float strength = 1f + (reach - distance) / Mathf.Max(reach, 0.0001f);
            away += push * strength;
        }

        if (!threatened) return false;

        // Köşeye sıkışmamak için duvara yaklaştıkça güçlenen bir "duvardan uzak dur" kuvveti ekle.
        away += WallAvoidance(position) * config.botWallAvoidWeight;

        Vector2 flee = away.sqrMagnitude > 0.0001f ? away.normalized : RandomDirection();
        direction = SlideAlongWalls(position, flee);
        return true;
    }

    /// <summary>
    /// Duvara <see cref="GameConfig.botWallAvoidRange"/> kadar yaklaşan bot için duvardan uzağa işaret eden kuvvet.
    /// Duvarda en güçlü (1), mesafe sınırında 0'dır. Her eksen ayrı hesaplanır.
    /// </summary>
    Vector2 WallAvoidance(Vector2 position)
    {
        float range = Mathf.Max(0.0001f, config.botWallAvoidRange);
        Vector2 half = arena.HalfSize;

        float x = Mathf.Clamp01((Mathf.Abs(position.x) - (half.x - range)) / range);
        float y = Mathf.Clamp01((Mathf.Abs(position.y) - (half.y - range)) / range);

        return new Vector2(-Mathf.Sign(position.x) * x, -Mathf.Sign(position.y) * y);
    }

    /// <summary>
    /// Bot duvara dayandıysa ve kaçış yönü duvarın içine doğruysa, o yönü iptal edip duvar boyunca kaydırır.
    /// Böylece bot duvara yapışıp kalmaz. Köşedeyse merkeze doğru döner.
    /// </summary>
    Vector2 SlideAlongWalls(Vector2 position, Vector2 direction)
    {
        const float wallTolerance = 0.01f;
        Vector2 limit = arena.HalfSize - Vector2.one * config.playerRadius;
        limit = new Vector2(Mathf.Max(0f, limit.x), Mathf.Max(0f, limit.y));

        bool blockedX = (position.x >= limit.x - wallTolerance && direction.x > 0f)
                     || (position.x <= -limit.x + wallTolerance && direction.x < 0f);
        bool blockedY = (position.y >= limit.y - wallTolerance && direction.y > 0f)
                     || (position.y <= -limit.y + wallTolerance && direction.y < 0f);

        if (!blockedX && !blockedY) return direction;

        if (blockedX && blockedY)
            return new Vector2(-Mathf.Sign(position.x), -Mathf.Sign(position.y)).normalized; // Köşe: merkeze dön.

        if (blockedX)
        {
            float y = Mathf.Abs(direction.y) >= 0.2f ? Mathf.Sign(direction.y) : -SignOrRandom(position.y);
            return new Vector2(0f, y);
        }

        float x = Mathf.Abs(direction.x) >= 0.2f ? Mathf.Sign(direction.x) : -SignOrRandom(position.x);
        return new Vector2(x, 0f);
    }

    float SignOrRandom(float value)
    {
        if (Mathf.Abs(value) > 0.0001f) return Mathf.Sign(value);
        return random.NextDouble() < 0.5 ? -1f : 1f;
    }

    Vector2 Wander(float deltaSeconds, Vector2 position)
    {
        if (pauseLeft > 0f)
        {
            pauseLeft -= Mathf.Max(0f, deltaSeconds);
            return Vector2.zero;
        }

        if (!hasTarget)
            PickTarget();

        Vector2 toTarget = target - position;
        float distance = toTarget.magnitude;
        if (distance <= config.botArrivalDistance)
        {
            // Vardı: biraz bekle, sonra yeni hedef seç.
            hasTarget = false;
            pauseLeft = RandomRange(config.botPauseMin, config.botPauseMax);
            return Vector2.zero;
        }

        // Takıldı mı? (Duvar ya da lav kenarına dayanıp yaklaşamıyor.) Öyleyse başka yere git.
        if (distance < bestDistance - 0.25f)
        {
            bestDistance = distance;
            stuckFor = 0f;
        }
        else
        {
            stuckFor += Mathf.Max(0f, deltaSeconds);
            if (stuckFor >= StuckSeconds)
            {
                PickTarget();
                toTarget = target - position;
                if (toTarget.sqrMagnitude < 0.0001f) return Vector2.zero;
            }
        }

        return toTarget.normalized;
    }

    void PickTarget()
    {
        Vector2 half = arena.HalfSize;
        float maxX = Mathf.Max(0f, half.x - config.botWanderMargin);
        float maxY = Mathf.Max(0f, half.y - config.botWanderMargin);
        for (int i = 0; i < TargetTries; i++)
        {
            target = new Vector2(RandomRange(-maxX, maxX), RandomRange(-maxY, maxY));
            if (IsBadTarget == null || !IsBadTarget(target)) break;
        }
        hasTarget = true;
        bestDistance = float.MaxValue;
        stuckFor = 0f;
    }

    float RandomRange(float min, float max)
    {
        if (max < min) { float t = min; min = max; max = t; }
        return min + (float)random.NextDouble() * (max - min);
    }

    Vector2 RandomDirection()
    {
        float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
