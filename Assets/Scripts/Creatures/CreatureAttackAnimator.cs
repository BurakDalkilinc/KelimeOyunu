using System;
using UnityEngine;

/// <summary>
/// Yaratığın 3 aşamalı saldırı animasyonu: hazırlık ("ad_atk0", hedefin tersine hafif geri çekilip ezilir) → vuruş ("ad_atk",
/// hedefe atılıp uzar; bu anda toz, kamera sarsıntısı ve hedefte etiketlere göre vuruş izi) → toparlanma (normal kare, yerine döner).
/// Devler yürürken her adımda yeri sarsar. Kareler yoksa yalnızca hareket oynar. Süreler <see cref="GameConfig"/>'te,
/// hesap <see cref="AttackPhase"/>'te. Saldırı sayısını sahibi verir (tek kişilikte modelden, ağda anlık görüntüden).
/// Yalnızca görüntüdür: hasar kuralda, saldırı olayı anında işlenir (hazırlık bu yüzden kısadır).
/// </summary>
public class CreatureAttackAnimator : MonoBehaviour
{
    /// <summary>Hazırlık karesinin ad eki ("aslan" -> "aslan_atk0").</summary>
    public const string WindupSuffix = "_atk0";

    const float StepSeconds = 0.42f;

    Func<int> attacks;
    Func<Vector2?> targetPosition;
    bool giant;
    string markEffect;
    GameConfig config;
    AttackPhase phase;
    int lastAttacks = int.MinValue;
    float attackTime = -1f;
    bool impactDone;
    Vector2 attackDirection = Vector2.down;
    Vector3 baseLocalPosition;
    Vector3 baseLocalScale;
    bool captured;
    Vector3 lastRootPosition;
    Vector2 moveDirection = Vector2.down;
    float stepTimer;
    float stepBob;
    SpriteAnimator frames;
    Sprite windupFrame;
    Sprite attackFrame;
    bool framesResolved;

    /// <summary>Gövdeye bağlar. <paramref name="giant"/> devler için daha uzun hazırlık, güçlü atılma, sarsıntı ve adım sarsıntısı.</summary>
    /// <param name="targetPosition">Biliniyorsa hedefin konumu (atılma yönü, vuruş izinin yeri); yoksa hareket yönü kullanılır.</param>
    /// <param name="markEffect">Vuruş izi efektinin adı (ör. "fx_bite", bkz. <see cref="AttackFxRules"/>); null ise iz çıkmaz.</param>
    public static CreatureAttackAnimator Attach(GameObject body, Func<int> attackCount, bool giant,
                                                Func<Vector2?> targetPosition = null, string markEffect = null)
    {
        var animator = body.GetComponent<CreatureAttackAnimator>();
        if (animator == null) animator = body.AddComponent<CreatureAttackAnimator>();
        animator.attacks = attackCount;
        animator.giant = giant;
        animator.targetPosition = targetPosition;
        animator.markEffect = markEffect;
        animator.config = GameConfigLoader.Get();
        animator.phase = new AttackPhase(
            giant ? animator.config.creatureAttackGiantWindupSeconds : animator.config.creatureAttackWindupSeconds,
            animator.config.creatureAttackStrikeSeconds, animator.config.creatureAttackRecoverSeconds);
        return animator;
    }

    float Lunge => giant ? config.creatureAttackGiantLunge : config.creatureAttackLunge;

    /// <summary>Sprite'ın büyüklük katı (dev 3 kat): iz ve öndeki darbe noktası buna göre ölçeklenir.</summary>
    float SpriteSize => Mathf.Max(1f, Mathf.Abs(baseLocalScale.x));

    void LateUpdate()
    {
        if (config == null || phase == null) return;

        if (!captured)
        {
            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale;
            lastRootPosition = Root.position;
            captured = true;
        }

        TrackMovement();

        int count = attacks != null ? attacks() : 0;
        if (lastAttacks == int.MinValue) lastAttacks = count;
        if (count > lastAttacks) StartAttack();
        lastAttacks = count;

        Vector3 offset = Vector3.zero;
        Vector2 scale = Vector2.one;

        if (attackTime >= 0f)
        {
            attackTime += Time.deltaTime;
            if (!impactDone && attackTime >= phase.ImpactTime) Impact();

            AttackStage stage = phase.Evaluate(attackTime, out float progress);
            if (stage == AttackStage.Idle) attackTime = -1f;
            else
            {
                offset = (Vector3)(attackDirection * Lunge * AttackPhase.LungeFactor(stage, progress, config.creatureAttackPullBack));
                scale = AttackPhase.ScaleFactor(stage, progress, config.creatureAttackSquash, config.creatureAttackStretch);
            }
        }

        if (giant) offset += Vector3.up * stepBob;

        transform.localPosition = baseLocalPosition + offset;
        transform.localScale = new Vector3(baseLocalScale.x * scale.x, baseLocalScale.y * scale.y, baseLocalScale.z);
    }

    Transform Root => transform.parent != null ? transform.parent : transform;

    /// <summary>Hareket yönünü izler (atılma yönü); devler her adımda yeri sarsar.</summary>
    void TrackMovement()
    {
        Vector3 rootPosition = Root.position;
        Vector2 moved = rootPosition - lastRootPosition;
        lastRootPosition = rootPosition;

        bool moving = moved.sqrMagnitude > 0.0000025f && Time.deltaTime > 0f;
        if (moving) moveDirection = Vector2.Lerp(moveDirection, moved.normalized, 0.3f).normalized;

        stepBob = Mathf.MoveTowards(stepBob, 0f, Time.deltaTime * 0.8f);
        if (!giant || !moving) return;

        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;
        stepTimer = StepSeconds;

        // Ağır adım: yer sarsılır, ayaklardan toz kalkar, gövde hafifçe sıçrar.
        Vector2 feet = (Vector2)rootPosition + Vector2.down * 0.9f;
        ImpactEffect.Spawn(feet + Vector2.right * UnityEngine.Random.Range(-0.6f, 0.6f), 0.6f, false);
        CameraShake.Shake(0.06f, 0.12f, rootPosition);
        stepBob = 0.08f;
    }

    /// <summary>Saldırı olayı geldi: hazırlık başlar (yön hedefe doğru, yoksa hareket yönü).</summary>
    void StartAttack()
    {
        attackTime = 0f;
        impactDone = false;
        ResolveFrames();

        Vector2? target = targetPosition?.Invoke();
        Vector2 toTarget = target.HasValue ? target.Value - (Vector2)Root.position : Vector2.zero;
        attackDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized
                        : moveDirection.sqrMagnitude > 0.01f ? moveDirection : Vector2.down;

        if (frames != null && windupFrame != null && phase.WindupSeconds > 0f) frames.Hold(windupFrame, phase.WindupSeconds);
    }

    /// <summary>Darbe anı (hazırlık bitti): saldırı karesi, toz, sarsıntı ve hedefte vuruş izi.</summary>
    void Impact()
    {
        impactDone = true;

        float hold = phase.StrikeSeconds + phase.RecoverSeconds * config.creatureAttackFrameRecoverFraction;
        if (frames != null && attackFrame != null) frames.Hold(attackFrame, hold);

        Vector2 root = Root.position;
        Vector2 ahead = root + attackDirection * config.attackMarkForwardOffset * SpriteSize;
        ImpactEffect.Spawn(ahead, giant ? 2.2f : 0.8f);
        CameraShake.Shake(giant ? 0.3f : 0.07f, giant ? 0.35f : 0.15f, root);

        if (string.IsNullOrEmpty(markEffect)) return;
        Vector2 at = targetPosition?.Invoke() ?? ahead; // Hedef bu arada kaçmış olabilir: izi şimdiki yerinde göster
        AttackMarkEffect.Spawn(markEffect, at, config.attackMarkScale * SpriteSize, attackDirection.x < 0f,
                               config.attackMarkFrameSeconds);
    }

    /// <summary>Hazırlık ("ad_atk0") ve saldırı ("ad_atk") karelerini bir kez bulur; olmayan kare null kalır (o aşamada normal kare).</summary>
    void ResolveFrames()
    {
        if (framesResolved) return;
        framesResolved = true;
        frames = GetComponent<SpriteAnimator>();
        Sprite baseFrame = frames != null ? frames.BaseFrame : null;
        if (baseFrame == null) return;
        attackFrame = SpriteLibrary.GetAttackFrame(baseFrame.name);
        windupFrame = SpriteLibrary.Get(baseFrame.name + WindupSuffix);
    }
}
