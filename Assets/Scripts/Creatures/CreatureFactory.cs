using UnityEngine;

/// <summary>
/// Kelimeden yaratık kurar: sayılar kelimenin "stats" değerlerinden, davranış farkları etiketlerinden gelir.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class CreatureFactory
{
    /// <summary>Saldırı kelimesinden, hedefini kovalayan bir yaratık kurar.</summary>
    public static CreatureModel CreateAttack(WordEntry word, PlayerState target, Vector2 position,
                                             GameConfig config, ArenaBounds arena)
    {
        if (word == null) throw new System.ArgumentNullException(nameof(word));
        if (target == null) throw new System.ArgumentNullException(nameof(target));
        if (config == null) throw new System.ArgumentNullException(nameof(config));

        WordStats stats = word.stats ?? new WordStats();
        float life = stats.duration > 0f ? stats.duration : config.resultSecondsMin;

        var creature = new CreatureModel(word, target, position, stats.speed, Mathf.RoundToInt(stats.damage),
                                         config.creatureRadius, life, arena);
        TagRegistry.Apply(creature, word.tags);
        return creature;
    }

    /// <summary>
    /// Bilinmeyen kelimenin zararsız "???" yaratığı: hedefi ve hasarı yoktur, sonuç süresi boyunca gezinir.
    /// </summary>
    public static CreatureModel CreateUnknown(Vector2 position, GameConfig config, ArenaBounds arena)
    {
        if (config == null) throw new System.ArgumentNullException(nameof(config));

        return new CreatureModel(null, null, position, config.unknownCreatureSpeed, 0,
                                 config.creatureRadius, config.resultSecondsMin, arena);
    }
}
