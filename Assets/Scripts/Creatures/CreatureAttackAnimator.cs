using System;
using UnityEngine;

/// <summary>
/// Yaratığın saldırı animasyonu: her vuruşta hedefe doğru atılır, büyür (darbe anı), sonra geri çekilir; vurduğu yerde toz
/// halkası ve saçılan toprak çıkar, kamera sarsılır (devlerde sert). Devler (boss, King Kong...) yürürken de her adımda yeri
/// sarsar ve ayaklarından toz kalkar. Saldırı sayısını sahibi verir (tek kişilikte modelden, ağda anlık görüntüden);
/// yalnızca görüntüdür, kurala dokunmaz.
/// </summary>
public class CreatureAttackAnimator : MonoBehaviour
{
    const float LungeSeconds = 0.08f;
    const float RecoverSeconds = 0.28f;
    const float StepSeconds = 0.42f;

    Func<int> attacks;
    Func<Vector2?> targetPosition;
    bool giant;
    int lastAttacks = int.MinValue;
    float attackTime = -1f;
    Vector2 attackDirection = Vector2.down;
    Vector3 baseLocalPosition;
    Vector3 baseLocalScale;
    bool captured;
    Vector3 lastRootPosition;
    Vector2 moveDirection = Vector2.down;
    float stepTimer;
    float stepBob;

    /// <summary>Gövdeye bağlar. <paramref name="giant"/> devler için daha güçlü atılma, sarsıntı ve adım sarsıntısı.</summary>
    /// <param name="targetPosition">Biliniyorsa hedefin konumu (atılma yönü); yoksa hareket yönü kullanılır (ağda).</param>
    public static CreatureAttackAnimator Attach(GameObject body, Func<int> attackCount, bool giant, Func<Vector2?> targetPosition = null)
    {
        var animator = body.GetComponent<CreatureAttackAnimator>();
        if (animator == null) animator = body.AddComponent<CreatureAttackAnimator>();
        animator.attacks = attackCount;
        animator.giant = giant;
        animator.targetPosition = targetPosition;
        return animator;
    }

    float Size => giant ? 3f : 1f;

    void LateUpdate()
    {
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
        float scale = 1f;

        if (attackTime >= 0f)
        {
            attackTime += Time.deltaTime;
            float lunge = (giant ? 0.55f : 0.28f);
            if (attackTime < LungeSeconds)
            {
                float t = attackTime / LungeSeconds;
                offset = (Vector3)(attackDirection * lunge * t);
                scale = 1f + 0.2f * t;
            }
            else if (attackTime < LungeSeconds + RecoverSeconds)
            {
                float t = (attackTime - LungeSeconds) / RecoverSeconds;
                float ease = 1f - (1f - t) * (1f - t);
                offset = (Vector3)(attackDirection * lunge * (1f - ease));
                scale = 1f + 0.2f * (1f - ease);
            }
            else attackTime = -1f;
        }

        if (giant) offset += Vector3.up * stepBob;

        transform.localPosition = baseLocalPosition + offset;
        transform.localScale = baseLocalScale * scale;
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

    void StartAttack()
    {
        attackTime = 0f;
        Vector2? target = targetPosition?.Invoke();
        Vector2 toTarget = target.HasValue ? target.Value - (Vector2)Root.position : Vector2.zero;
        attackDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized
                        : moveDirection.sqrMagnitude > 0.01f ? moveDirection : Vector2.down;

        Vector2 at = (Vector2)Root.position + attackDirection * (giant ? 1.6f : 0.6f);
        ImpactEffect.Spawn(at, giant ? 2.2f : 0.8f);
        CameraShake.Shake(giant ? 0.3f : 0.07f, giant ? 0.35f : 0.15f, Root.position);
    }
}
