using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Bir vuruş tepkisinin bilgisi (yerel oyuncunun ekran efektleri ve deneme için).</summary>
public struct HitFeedbackInfo
{
    public int Amount;
    public bool Blocked;
    public bool Eliminated;
    public bool Giant;
    public Vector2? Direction;
    public Vector2 Position;
}

/// <summary>
/// Karakterin vuruş hissi (yalnızca görüntü ve ses): canını ve kalkan engellemelerini her kare izler; can düşünce tepkiyi
/// yaklaşan yaratık darbesinin görüneceği ana denk getirir (<see cref="StrikeRegistry"/>; hasar kuralda saldırı olayı anında işlenir,
/// darbe hazırlıktan sonra görünür). Tepki: görüntü donması, beyaz parlama, ezilme (<see cref="HitReaction"/>), hasar sayısı
/// (<see cref="DamageNumber"/>), katmanlı ses (<see cref="HitSounds"/>); yerel oyuncuda kamera itmesi ve kısa takip donması; elemede
/// güçlü sarsıntı ve vuran yaratığın uzun donması. Tek kişilikte ve ağda (her istemcide, eşitlenen candan) aynı çalışır.
/// Kural ve hasar zamanı değişmez.
/// </summary>
[DefaultExecutionOrder(50)] // Yaratık saldırı animasyonları darbeyi bildirdikten sonra, kan/parçalanmadan önce
public class HitFeedback : MonoBehaviour
{
    static readonly List<HitFeedback> all = new List<HitFeedback>();

    /// <summary>Sahnedeki bütün karakterlerin tepkileri (itiş görüntüsü itilenleri buradan bulur).</summary>
    public static IReadOnlyList<HitFeedback> All => all;

    /// <summary>Yerel oyuncu bir tepki oynattığında (ekran kırmızı parlaması, can yazısının sallanması).</summary>
    public static event Action<HitFeedbackInfo> LocalHit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
        LocalHit = null;
    }

    Func<int> health;
    Func<FallKind> falling;
    Func<bool> isLocal;
    Func<int> blocks;
    GameConfig config;
    int lastHealth = int.MinValue;
    int lastBlocks = int.MinValue;

    bool pending;
    float fireAt;
    int pendingAmount;
    bool pendingBlocked;
    bool pendingDeath;
    bool pendingFalling;
    bool hasStrike;
    IncomingStrike strike;

    /// <summary>Karakter hayatta mı (hayalet değil)?</summary>
    public bool IsAlive => health != null && health() > 0;

    /// <summary>Yerel oyuncunun karakteri mi?</summary>
    public bool IsLocal => isLocal != null && isLocal();

    /// <summary>Can düştü ama tepki darbe anını bekliyor mu? Kan/parçalanma da bekler (<see cref="GoreEmitter"/>).</summary>
    public bool IsWaitingForImpact => pending;

    /// <summary>
    /// Karaktere bağlar. <paramref name="health"/> şu anki can, <paramref name="falling"/> düşüyor mu, <paramref name="isLocal"/> bu
    /// bilgisayarın oyuncusu mu, <paramref name="blocks"/> kalkanın engellediği vuruş sayısı (bilinmiyorsa null).
    /// </summary>
    public static HitFeedback Attach(GameObject target, Func<int> health, Func<FallKind> falling, Func<bool> isLocal, Func<int> blocks)
    {
        var feedback = target.GetComponent<HitFeedback>();
        if (feedback == null) feedback = target.AddComponent<HitFeedback>();
        feedback.health = health;
        feedback.falling = falling;
        feedback.isLocal = isLocal;
        feedback.blocks = blocks;
        feedback.config = GameConfigLoader.Get();
        return feedback;
    }

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }

    void OnDisable()
    {
        all.Remove(this);
        pending = false;
    }

    void LateUpdate()
    {
        if (health == null || config == null) return;

        int current = health();
        int blocked = blocks != null ? blocks() : 0;
        if (lastHealth == int.MinValue) { lastHealth = current; lastBlocks = blocked; return; }

        if (current < lastHealth && lastHealth > 0) Queue(lastHealth - current, false, current <= 0);
        else if (blocked > lastBlocks && current > 0) Queue(0, true, false);

        lastHealth = current;
        lastBlocks = blocked;

        if (pending && Time.time >= fireAt) Fire();
    }

    /// <summary>Tepkiyi sıraya koyar: yakında görünecek bir darbe varsa onun anına, yoksa hemen.</summary>
    void Queue(int amount, bool blocked, bool death)
    {
        FallKind fall = falling != null ? falling() : FallKind.None;

        if (pending)
        {
            // Darbe beklenirken ikinci vuruş: tek tepkide birleşir.
            pendingAmount += amount;
            pendingBlocked = pendingBlocked && blocked;
            pendingDeath |= death;
            return;
        }

        pending = true;
        pendingAmount = amount;
        pendingBlocked = blocked;
        pendingDeath = death;
        pendingFalling = fall != FallKind.None;
        fireAt = Time.time;

        hasStrike = !pendingFalling && StrikeRegistry.Shared.TryTake(transform.position, Time.time, config.hitImpactMatchRadius,
                                                                     config.hitImpactGiantRadiusMultiplier, config.hitImpactLookBackSeconds,
                                                                     config.hitImpactMaxWaitSeconds, out strike);
        if (hasStrike) fireAt = Mathf.Max(Time.time, strike.ImpactTime);
    }

    void Fire()
    {
        pending = false;
        bool local = IsLocal;
        bool giant = hasStrike && strike.Giant;
        Vector2? direction = hasStrike ? strike.Direction : (Vector2?)null;
        Vector2 at = transform.position;

        var info = new HitFeedbackInfo
        {
            Amount = pendingAmount, Blocked = pendingBlocked, Eliminated = pendingDeath, Giant = giant, Direction = direction, Position = at,
        };

        // Ses: eleme, kalkan ya da katmanlı vuruş.
        if (pendingDeath) HitSounds.Eliminated(local);
        else if (pendingBlocked) HitSounds.Blocked(local);
        else HitSounds.Hit(local, giant);

        if (pendingFalling)
        {
            // Lav/uçurum: düşme animasyonu kendi görüntüsünü oynar; yalnızca ses ve elemede sarsıntı.
            if (pendingDeath) CameraShake.AddTrauma(config.cameraTraumaDeath, at);
            if (local) LocalHit?.Invoke(info);
            return;
        }

        float freeze = HitFeelMath.HitStopSeconds(giant, pendingDeath, config.hitStopSeconds, config.hitStopGiantSeconds, config.hitStopDeathSeconds);
        if (!pendingDeath) HitReaction.Attach(gameObject).Play(direction, giant ? 1.3f : 1f, pendingBlocked, freeze);

        string text = pendingBlocked ? Loc.T("hit.block") : HitFeelMath.DamageText(pendingAmount);
        Color color = pendingBlocked ? config.damageNumberBlockColor : local ? config.damageNumberLocalColor : config.damageNumberColor;
        DamageNumber.Spawn(at, text, color);

        // Vuran yaratık da donar (elemede daha uzun).
        if (hasStrike && strike.Attacker is Component attacker && attacker != null)
            HitStop.Freeze(attacker.gameObject, pendingDeath ? config.hitStopDeathSeconds : 0f);

        if (pendingDeath)
        {
            CameraShake.AddTrauma(config.cameraTraumaDeath, at);
            CameraShake.Kick(direction ?? Vector2.down, config.cameraKickDeath, at);
        }
        else if (giant) CameraShake.Kick(direction ?? Vector2.down, config.cameraKickGiantHit, at);

        if (local)
        {
            CameraFollow.Hold(config.hitLocalFollowFreezeSeconds);
            if (!pendingBlocked) CameraShake.Kick(direction ?? Vector2.down, config.cameraKickLocalHit);
            LocalHit?.Invoke(info);
        }
    }
}
