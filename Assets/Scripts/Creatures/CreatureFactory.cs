using UnityEngine;

/// <summary>
/// Kelimeden yaratık kurar: sayılar kelimenin "stats" değerlerinden, davranış farkları etiketlerinden gelir.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class CreatureFactory
{
    /// <summary>
    /// Saldırı kelimesinden, hedefini kovalayan bir yaratık kurar. Kelime sürü doğuruyorsa (swarm) her üye için
    /// ayrı çağrılır; <paramref name="memberIndex"/> üyenin sırası, <paramref name="memberCount"/> toplam sayıdır.
    /// </summary>
    public static CreatureModel CreateAttack(WordEntry word, PlayerState target, Vector2 position,
                                             GameConfig config, ArenaBounds arena,
                                             int memberIndex = 0, int memberCount = 1)
    {
        if (word == null) throw new System.ArgumentNullException(nameof(word));
        if (target == null) throw new System.ArgumentNullException(nameof(target));
        if (config == null) throw new System.ArgumentNullException(nameof(config));

        WordStats stats = word.stats ?? new WordStats();
        float life = stats.duration > 0f ? stats.duration : config.resultSecondsMin;

        var creature = new CreatureModel(word, target, position, stats.speed, Mathf.RoundToInt(stats.damage),
                                         config.creatureRadius, life, arena);
        TagRegistry.Apply(creature, word.tags, new SpawnContext(memberIndex, memberCount, config));
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
