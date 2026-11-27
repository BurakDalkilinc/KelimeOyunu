using UnityEngine;

/// <summary>
/// Hedefli saldırı yaratıklarını "korkutucu" yapan genel kurallar: hız ve ömür çarpanı, "slow" için hız alt/üst sınırı,
/// atılma (pounce), vuruşta sendeletme ve vuramadığı hedeften dönme. Değerler GameConfig'te, farklar etiketlerden gelir
/// (<see cref="CreatureTagTuning"/>); kelime adına özel kod yoktur. Global tehditlerin yaratıklarına (zombi sürüsü, meteor)
/// uygulanmaz: onları <see cref="CreatureBuilder.BuildAttack"/> kurmaz. Düz C# sınıfı, test edilebilir.
/// </summary>
public static class CreatureAggression
{
    /// <summary>Yavaş yaratık etiketi: hızı oyuncudan yavaş kalır ama fark azalır.</summary>
    public const string SlowTag = "slow";

    /// <summary>Kurallar kovalayan yaratıklara uygulanır (yumruk, bulut, tuzak, mıknatıs, mermi ve hedefsizler olduğu gibi kalır).</summary>
    public static bool IsChaser(CreatureModel model)
    {
        return model != null && model.Target != null && model.Speed > 0f
               && !model.IsStrike && !model.IsArea && !model.IsTrap && !model.IsPull && !model.IsProjectile;
    }

    /// <summary>Kovalayan yaratığa bütün kuralları uygular. Kovalayan değilse bir şey yapmaz.</summary>
    public static void Apply(CreatureModel model, WordEntry word, GameConfig config)
    {
        if (config == null || !IsChaser(model)) return;

        model.Speed = SpeedFor(model.Speed, word, config);
        model.ScaleLife(DurationMultiplierFor(word, config));

        model.PounceRange = PounceRangeFor(word, config);
        model.PounceWindupSeconds = config.creaturePounceWindupSeconds;
        model.PounceSeconds = config.creaturePounceSeconds;
        model.PounceSpeedMultiplier = PounceSpeedFor(word, config);
        model.PounceCooldownSeconds = config.creaturePounceCooldownSeconds;
        model.PounceOvershoot = config.creaturePounceOvershoot;

        model.HitSlowMultiplier = config.creatureHitSlowMultiplier;
        model.HitSlowSeconds = config.creatureHitSlowSeconds;

        model.RetargetUnreachableSeconds = config.creatureRetargetSeconds;
        model.RetargetIgnoreSeconds = config.creatureRetargetIgnoreSeconds;
        model.StuckMoveFraction = config.creatureStuckMoveFraction;
    }

    /// <summary>
    /// Yaratığın hızı: kelimenin hızı × genel çarpan × etiket çarpanları. "slow" etiketli yaratık oyuncu hızının
    /// [alt, üst] katları arasına sıkıştırılır (hep oyuncudan yavaş). Diğerleri en çok oyuncu hızının üst katına çıkar,
    /// ama kelimedeki hızın altına hiç inmez.
    /// </summary>
    public static float SpeedFor(float baseSpeed, WordEntry word, GameConfig config)
    {
        float speed = baseSpeed * config.creatureSpeedMultiplier * TagProduct(word, config, TuningField.Speed);
        float player = config.playerMoveSpeed;

        if (word != null && word.HasTag(SlowTag))
        {
            float max = player * config.creatureSlowMaxSpeedFraction;
            float min = Mathf.Min(player * config.creatureSlowMinSpeedFraction, max);
            return Mathf.Clamp(speed, min, max);
        }

        if (config.creatureMaxSpeedFraction > 0f)
            speed = Mathf.Max(Mathf.Min(baseSpeed, speed), Mathf.Min(speed, player * config.creatureMaxSpeedFraction));
        return Mathf.Max(0f, speed);
    }

    /// <summary>Ömür çarpanı: genel çarpan × etiket çarpanları.</summary>
    public static float DurationMultiplierFor(WordEntry word, GameConfig config)
    {
        return Mathf.Max(0.01f, config.creatureDurationMultiplier * TagProduct(word, config, TuningField.Duration));
    }

    /// <summary>Atılma menzili (birim): genel menzil × etiket çarpanları. 0 ise atılmaz.</summary>
    public static float PounceRangeFor(WordEntry word, GameConfig config)
    {
        return Mathf.Max(0f, config.creaturePounceRange * TagProduct(word, config, TuningField.PounceRange));
    }

    /// <summary>Atılma hız çarpanı: genel çarpan × etiket çarpanları (en az 1).</summary>
    public static float PounceSpeedFor(WordEntry word, GameConfig config)
    {
        return Mathf.Max(1f, config.creaturePounceSpeedMultiplier * TagProduct(word, config, TuningField.PounceSpeed));
    }

    /// <summary>
    /// Botların kaçtığı alan: yaratığın gövdesi + atılma menzilinin <see cref="GameConfig.botPounceAwareness"/> kadarı.
    /// Hazırlıkta ya da atılırken menzilin tamamı tehlikedir (bot "kırmızı yanan"dan kaçar).
    /// </summary>
    public static DangerZone DangerZoneFor(CreatureModel model, GameConfig config)
    {
        float extra = 0f;
        if (model.PounceRange > 0f)
            extra = model.Pounce != PounceStage.None ? model.PounceRange : model.PounceRange * config.botPounceAwareness;
        return new DangerZone(model.Position, model.Radius + extra);
    }

    enum TuningField { PounceRange, PounceSpeed, Speed, Duration }

    static float TagProduct(WordEntry word, GameConfig config, TuningField field)
    {
        float product = 1f;
        if (word == null || config.creatureTagTunings == null) return product;

        foreach (CreatureTagTuning tuning in config.creatureTagTunings)
        {
            if (tuning == null || string.IsNullOrEmpty(tuning.tag) || !word.HasTag(tuning.tag)) continue;
            switch (field)
            {
                case TuningField.PounceRange: product *= tuning.pounceRangeMultiplier; break;
                case TuningField.PounceSpeed: product *= tuning.pounceSpeedMultiplier; break;
                case TuningField.Duration: product *= tuning.durationMultiplier; break;
                default: product *= tuning.speedMultiplier; break;
            }
        }
        return product;
    }
}
