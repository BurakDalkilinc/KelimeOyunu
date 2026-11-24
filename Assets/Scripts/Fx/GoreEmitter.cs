using System;
using UnityEngine;

/// <summary>
/// Bir karakterin canını her kare izler: can azalınca kan sıçratır, sıfıra inince karakteri parçalara ayırır.
/// Lavda ölen kömürleşip kıvılcım saçar; uçuruma düşen parçalanmaz (onun kendi animasyonu var). Tek kişilik sahnede ve ağda
/// (her istemcide, eşitlenen candan) aynı şekilde çalışır.
/// </summary>
public class GoreEmitter : MonoBehaviour
{
    Func<int> health;
    Func<FallKind> falling;

    /// <summary>Karakter yanıyor ya da çok kömürleşmiş mi? Öyle ölürse kara parçalara ayrılır.</summary>
    public Func<bool> Charred { get; set; }
    SpriteRenderer body;
    int lastHealth = int.MinValue;
    FallKind wasFalling;
    bool wasCharred;
    Color aliveColor = Color.white;
    Sprite aliveSprite;

    /// <summary>Karaktere bağlar. <paramref name="health"/> şu anki can, <paramref name="falling"/> düşüyor mu.</summary>
    public static GoreEmitter Attach(GameObject target, Func<int> health, Func<FallKind> falling)
    {
        var emitter = target.GetComponent<GoreEmitter>();
        if (emitter == null) emitter = target.AddComponent<GoreEmitter>();
        emitter.health = health;
        emitter.falling = falling;
        emitter.body = target.GetComponent<SpriteRenderer>();
        return emitter;
    }

    void LateUpdate()
    {
        if (health == null) return;

        int current = health();
        if (lastHealth == int.MinValue) lastHealth = current;

        if (current < lastHealth && lastHealth > 0)
        {
            Vector2 at = transform.position;
            if (wasFalling == FallKind.Lava)
            {
                if (current <= 0) Gore.Burn(at); // Lavda yanan: kömürleşmiş parçalar, kıvılcım ve duman
            }
            else if (wasFalling == FallKind.None)
            {
                if (current <= 0 && (wasCharred || (Charred != null && Charred()))) Gore.ExplodeCharred(aliveSprite, at, transform.lossyScale.x, aliveColor);
                else if (current <= 0) Gore.Explode(aliveSprite, at, transform.lossyScale.x, aliveColor);
                else Gore.Splash(at, 12 * Mathf.Min(3, lastHealth - current), 1f);
            }
        }

        lastHealth = current;
        wasFalling = falling != null ? falling() : FallKind.None;
        if (current > 0) wasCharred = Charred != null && Charred(); // Ölünce etkiler silinse de son hâli hatırlanır

        // Parçalanırken canlı hâlinin resmini ve rengini kullan (öldükten sonra hayalet rengine döner).
        if (current > 0 && body != null)
        {
            aliveSprite = body.sprite;
            aliveColor = body.color;
            aliveColor.a = 1f;
        }
    }
}
