using UnityEngine;

/// <summary>
/// Yakın dövüş silahının (kılıç, sopa) hazırlıktaki savuruşu: Boşluk'a basılınca silah kısa bir süre parlar, süre dolunca vuruş
/// başlarken kilitlenen yöne yapılır (bkz. <see cref="MeleeRules.TickSwing"/>). Hedef bu sürede yaydan çıkarsa kurtulur. Düz C# sınıfı.
/// </summary>
public class MeleeWindup
{
    bool pending;

    /// <summary>Savuruş hazırlıkta mı?</summary>
    public bool IsWindingUp => pending;

    /// <summary>Savrulan silahın kelimesi (son atışta silah elden düşse de vuruş bu kelimeyle yapılır).</summary>
    public WordEntry Weapon { get; private set; }

    /// <summary>Vuruşun yönü (birim vektör), hazırlık başlarken kilitlenir.</summary>
    public Vector2 Facing { get; private set; } = Vector2.up;

    /// <summary>Vuruşa kalan süre (sn).</summary>
    public float TimeLeft { get; private set; }

    /// <summary>Hazırlığı başlatır. <paramref name="seconds"/> 0 ise ilk <see cref="Tick"/>'te hemen vurur.</summary>
    public void Begin(WordEntry weapon, Vector2 facing, float seconds)
    {
        Weapon = weapon;
        Facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        TimeLeft = Mathf.Max(0f, seconds);
        pending = true;
    }

    /// <summary>Zamanı ilerletir; hazırlık bu karede bittiyse (vuruş anı) bir kez true döner.</summary>
    public bool Tick(float deltaSeconds)
    {
        if (!pending) return false;

        TimeLeft -= Mathf.Max(0f, deltaSeconds);
        if (TimeLeft > 0f) return false;

        TimeLeft = 0f;
        pending = false;
        return true;
    }

    /// <summary>Hazırlıktaki savuruşu iptal eder (elenen, etkileri silinen oyuncu).</summary>
    public void Cancel()
    {
        pending = false;
        TimeLeft = 0f;
    }
}
