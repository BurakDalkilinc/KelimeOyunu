using System;
using UnityEngine;

/// <summary>
/// Yaratığın 3 aşamalı saldırı animasyonu: hazırlık ("ad_atk0", hedefin tersine hafif geri çekilip ezilir) → vuruş ("ad_atk",
/// hedefe atılıp uzar; bu anda toz, kamera sarsıntısı ve hedefte etiketlere göre vuruş izi) → toparlanma (normal kare, yerine döner).
/// Devler yürürken her adımda yeri sarsar. Kareler yoksa yalnızca hareket oynar. Süreler <see cref="GameConfig"/>'te,
/// hesap <see cref="AttackPhase"/>'te. Saldırı sayısını sahibi verir (tek kişilikte modelden, ağda anlık görüntüden).
/// Yalnızca görüntüdür: hasar kuralda, saldırı olayı anında işlenir (hazırlık bu yüzden kısadır).
/// Atılmaya hazırlanan yaratık (kuraldaki <see cref="PounceStage.Windup"/>) hazırlık karesinde durur, titrer ve kırmızı yanar;
/// atılırken gidiş yönünde uzar. Vuruş hissi: saldırı başlarken darbenin görüneceği an <see cref="StrikeRegistry"/>'ye bildirilir
/// (vurulan karakter tepkisini o ana denk getirir), darbe anında yaratığın görüntüsü kısa süre donar (<see cref="HitStop"/>);
/// donukken animasyon zamanı ilerlemez.
/// </summary>
public class CreatureAttackAnimator : MonoBehaviour
{
    /// <summary>Hazırlık karesinin ad eki ("aslan" -> "aslan_atk0").</summary>
    public const string WindupSuffix = "_atk0";

    const float StepSeconds = 0.42f;

    Func<int> attacks;
    Func<Vector2?> targetPosition;
    Func<bool> pounceWindup;
    Func<bool> lunging;
    SpringValue lungeStretch;
    SpriteRenderer bodyRenderer;
    Color baseColor = Color.white;
    bool tinted;
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
    /// <param name="pounceWindup">Yaratık şu an atılmaya hazırlanıyor mu (tek kişilikte modelden, ağda anlık görüntüden)?</param>
    public static CreatureAttackAnimator Attach(GameObject body, Func<int> attackCount, bool giant,
                                                Func<Vector2?> targetPosition = null, string markEffect = null,
                                                Func<bool> pounceWindup = null, Func<bool> lunging = null)
    {
        var animator = body.GetComponent<CreatureAttackAnimator>();
        if (animator == null) animator = body.AddComponent<CreatureAttackAnimator>();
        animator.attacks = attackCount;
        animator.giant = giant;
        animator.targetPosition = targetPosition;
        animator.markEffect = markEffect;
        animator.pounceWindup = pounceWindup;
        animator.lunging = lunging;
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
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
            captured = true;
        }

        TrackMovement();

        int count = attacks != null ? attacks() : 0;
        if (lastAttacks == int.MinValue) lastAttacks = count;
        if (count > lastAttacks) StartAttack();
        lastAttacks = count;

        Vector3 offset = Vector3.zero;
        Vector2 scale = Vector2.one;

        bool frozen = HitStop.IsFrozen(gameObject); // Vuruş donması: animasyon zamanı durur, poz korunur
        if (attackTime >= 0f)
        {
            if (!frozen) attackTime += Time.deltaTime;
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
        offset += PounceWindupVisual();
        scale = Vector2.Scale(scale, LungeStretch(frozen));

        transform.localPosition = baseLocalPosition + offset;
        transform.localScale = new Vector3(baseLocalScale.x * scale.x, baseLocalScale.y * scale.y, baseLocalScale.z);
    }

    Transform Root => transform.parent != null ? transform.parent : transform;

    /// <summary>
    /// Atılma hazırlığı: hazırlık karesi tutulur, gövde kırmızı nabızla yanar ve titrer (titreme ofsetini döndürür).
    /// Hazırlık bitince renk eski hâline döner.
    /// </summary>
    Vector3 PounceWindupVisual()
    {
        bool windup = pounceWindup != null && pounceWindup();
        if (!windup)
        {
            if (tinted && bodyRenderer != null) bodyRenderer.color = baseColor;
            tinted = false;
            return Vector3.zero;
        }

        ResolveFrames();
        if (frames != null && windupFrame != null) frames.Hold(windupFrame, Mathf.Max(0.05f, Time.deltaTime * 2f));

        if (bodyRenderer != null)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 40f);
            bodyRenderer.color = Color.Lerp(baseColor, config.creaturePounceTint, pulse);
            tinted = true;
        }

        float amplitude = config.creaturePounceTremble * (giant ? 2f : 1f);
        return new Vector3(UnityEngine.Random.Range(-amplitude, amplitude), UnityEngine.Random.Range(-amplitude, amplitude) * 0.5f, 0f);
    }

    /// <summary>
    /// Atılırken (kuraldaki <see cref="PounceStage.Lunge"/>) gidiş yönünde uzar, bitince yayla eski hâline seker
    /// (ezilmenin tersi, bkz. <see cref="HitFeelMath.SquashScale"/>).
    /// </summary>
    Vector2 LungeStretch(bool frozen)
    {
        if (lunging == null) return Vector2.one;
        if (lungeStretch == null) lungeStretch = new SpringValue(config.hitSquashFrequency, config.hitSquashDamping);

        lungeStretch.Target = lunging() ? -1f : 0f; // Eksi = gidiş yönünde uzama
        if (!frozen) lungeStretch.Step(Time.deltaTime);
        if (Mathf.Abs(lungeStretch.Value) < 0.001f && lungeStretch.Target == 0f) return Vector2.one;
        return HitFeelMath.SquashScale(moveDirection, lungeStretch.Value, config.creatureLungeStretch, config.creatureLungeStretch * 0.6f);
    }

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

        // Darbenin görüneceği anı bildir: vurulan karakter tepkisini (donma, parlama, sayı, ses) o ana denk getirir.
        Vector2 aimed = target ?? (Vector2)Root.position + attackDirection * config.attackMarkForwardOffset * SpriteSize;
        StrikeRegistry.Shared.Add(new IncomingStrike
        {
            Position = aimed, Direction = attackDirection, ImpactTime = Time.time + phase.ImpactTime, Giant = giant, Attacker = this,
        });
    }

    /// <summary>Darbe anı (hazırlık bitti): saldırı karesi, toz, sarsıntı ve hedefte vuruş izi.</summary>
    void Impact()
    {
        impactDone = true;
        HitStop.Freeze(gameObject, giant ? config.hitStopGiantSeconds : config.hitStopSeconds); // Vuran da bir an donar

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
