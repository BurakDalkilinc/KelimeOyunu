using UnityEngine;

/// <summary>
/// İtişin görsel izi (oyun mantığı değil, yalnızca görüntü): karakterin çevresinde hızla büyüyüp sönen beyaz bir halka; ayrıca
/// vuruş hissi: iten bir an donar, önündeki (menzildeki) karakterler beyaz parlayıp ezilir ve donar, ayaklarından toz kalkar,
/// itilen varsa kısa bir "güm" duyulur; itişte yerel oyuncu varsa kamera itiş yönünde itilir. İtilenler görüntüden tahmin edilir
/// (ağda istemci kimin itildiğini bilmez); kural <see cref="ShoveRules"/>'tadır.
/// </summary>
public class ShoveEffect : MonoBehaviour
{
    const float Seconds = 0.22f;

    /// <summary>Bu yakınlıktaki karakter itenin kendisi sayılır (birim).</summary>
    const float SelfDistance = 0.05f;

    float age;
    float maxScale;
    SpriteRenderer sr;

    /// <summary>
    /// Verilen noktada, <paramref name="radius"/> yarıçapına kadar büyüyen bir halka çıkarır. <paramref name="facing"/> biliniyorsa
    /// yalnızca o yöndeki (itiş açısı içindeki) karakterler tepki verir; bilinmiyorsa menzildeki herkes.
    /// </summary>
    public static void Spawn(Vector2 position, float radius, Vector2? facing = null)
    {
        var go = new GameObject("İtiş izi");
        go.transform.position = new Vector3(position.x, position.y, 0f);

        var effect = go.AddComponent<ShoveEffect>();
        effect.maxScale = Mathf.Max(0.5f, radius * 2f);
        effect.sr = go.AddComponent<SpriteRenderer>();
        effect.sr.sprite = SpriteFactory.Circle();
        effect.sr.sortingOrder = 12;
        effect.Apply();

        Feel(position, radius, facing);
    }

    /// <summary>İtişin vuruş hissi: donma, parlama, toz, ses, kamera itmesi.</summary>
    static void Feel(Vector2 position, float radius, Vector2? facing)
    {
        GameConfig config = GameConfigLoader.Get();
        float reach = radius + config.playerRadius;
        float halfAngle = config.shoveHalfAngleDegrees;

        HitFeedback shover = null;
        bool localInvolved = false;
        int victims = 0;
        Vector2 push = Vector2.zero;

        foreach (HitFeedback each in HitFeedback.All)
        {
            if (each == null || !each.IsAlive) continue;
            Vector2 offset = (Vector2)each.transform.position - position;
            float distance = offset.magnitude;
            if (distance < SelfDistance) { shover = each; continue; }
            if (distance > reach) continue;
            if (facing.HasValue && facing.Value.sqrMagnitude > 0.0001f && Vector2.Angle(facing.Value, offset) > halfAngle) continue;
            if (!each.TryGetComponent(out CharacterStackVisual body) || body.Stack == null || !body.Stack.gameObject.activeInHierarchy)
                continue; // Görünmeyen (hayalet gizli, araçta) karakter tepki vermez

            Vector2 direction = offset / Mathf.Max(0.0001f, distance);
            HitReaction.Attach(each.gameObject).Play(direction, 0.7f, false, config.hitStopShoveSeconds);
            ImpactEffect.Spawn((Vector2)each.transform.position + Vector2.down * config.playerRadius, config.shoveDustSize, false);
            push += direction;
            victims++;
            localInvolved |= each.IsLocal;
        }

        ImpactEffect.Spawn(position + Vector2.down * config.playerRadius, config.shoveDustSize * 0.6f, false); // İtenin ayağında toz
        if (shover != null)
        {
            HitStop.Freeze(shover.gameObject, config.hitStopShoveSeconds);
            localInvolved |= shover.IsLocal;
        }

        if (victims > 0) HitSounds.Shove(localInvolved ? config.shoveThumpVolume : config.shoveThumpVolume * config.audioOtherHitVolume);
        if (!localInvolved) return;

        Vector2 kickDirection = victims > 0 ? push : facing ?? Vector2.zero;
        CameraShake.Kick(kickDirection, config.cameraKickShove * (victims > 0 ? 2f : 1f));
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Seconds)
        {
            Destroy(gameObject);
            return;
        }
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01(age / Seconds);
        transform.localScale = Vector3.one * Mathf.Lerp(0.6f, maxScale, t);
        sr.color = new Color(1f, 1f, 1f, 0.55f * (1f - t));
    }
}
