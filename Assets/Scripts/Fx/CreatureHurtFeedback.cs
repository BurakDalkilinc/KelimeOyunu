using System;
using UnityEngine;

/// <summary>
/// Hasar alan yaratığın (boss'a itiş, silah, mermi) vuruş hissi, yalnızca görüntü ve ses: canı düşünce beyaz parlar, ezilip seker,
/// bir an donar (<see cref="HitReaction"/>), üstünde "-1" çıkar, katmanlı vuruş sesi ve sarsıntı; ölünce güçlü sarsıntı.
/// Canı sahibi verir (tek kişilikte modelden, ağda yaratık yazısından "7/10"); canı olmayan yaratıkta (-1) bir şey yapmaz.
/// </summary>
public class CreatureHurtFeedback : MonoBehaviour
{
    Func<int> health;
    GameConfig config;
    int lastHealth = int.MinValue;

    /// <summary>Yaratığın gövdesine bağlar. <paramref name="currentHealth"/> şu anki can (canı yoksa -1).</summary>
    public static CreatureHurtFeedback Attach(GameObject body, Func<int> currentHealth)
    {
        var feedback = body.GetComponent<CreatureHurtFeedback>();
        if (feedback == null) feedback = body.AddComponent<CreatureHurtFeedback>();
        feedback.health = currentHealth;
        feedback.config = GameConfigLoader.Get();
        return feedback;
    }

    void LateUpdate()
    {
        if (health == null || config == null) return;

        int current = health();
        if (current < 0) return;
        if (lastHealth == int.MinValue || current > lastHealth) { lastHealth = current; return; }
        if (current == lastHealth) return;

        int amount = lastHealth - current;
        lastHealth = current;
        Play(amount, current <= 0);
    }

    /// <summary>Tepkiyi oynatır (ölüm de dahil: gövde o karede yok olabilir, ses ve sarsıntı yine olur).</summary>
    public void Play(int amount, bool killed)
    {
        Vector2 at = transform.position;
        HitSounds.CreatureHurt(killed);
        CameraShake.AddTrauma(killed ? config.cameraTraumaBossDeath : config.cameraTraumaBossHurt, at);

        float size = Mathf.Max(1f, Mathf.Abs(transform.lossyScale.y));
        DamageNumber.Spawn(at + Vector2.up * size * 0.4f, HitFeelMath.DamageText(amount), config.damageNumberColor);
        if (!killed) HitReaction.Attach(gameObject).Play(null, 0.8f, false, config.hitStopBossHurtSeconds);
        else ImpactEffect.Spawn(at, size, true);
    }
}
