using System;

/// <summary>
/// Bir etiketin yaratık tehlikesine katkısı (GameConfig'te liste): atılma menzili, atılma hızı ve yürüme hızı çarpanı.
/// Kelimeye özel değil, etikete bağlıdır: "fast" taşıyan her yaratık aynı çarpanı alır.
/// </summary>
[Serializable]
public class CreatureTagTuning
{
    public string tag;

    /// <summary>Atılma menzili çarpanı (0 = bu etiketi taşıyan atılmaz).</summary>
    public float pounceRangeMultiplier = 1f;

    /// <summary>Atılma hızı çarpanı.</summary>
    public float pounceSpeedMultiplier = 1f;

    /// <summary>Yürüme hızı çarpanı (genel hız çarpanının üstüne).</summary>
    public float speedMultiplier = 1f;

    /// <summary>Ömür çarpanı (genel ömür çarpanının üstüne).</summary>
    public float durationMultiplier = 1f;
}
