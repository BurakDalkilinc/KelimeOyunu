using UnityEngine;

/// <summary>
/// Yaratıkların doğuş/kayboluş efektleri (yalnızca görüntü): kaybolurken "puf" (fx_poof kareleri, yoksa sönen gri daire),
/// büyünün varışında yerde açılan çağırma çemberi (fx_summon, yoksa genişleyen halka) ve kısa beyaz flaş.
/// Tek kişilik ve ağ istemcisi aynı yolu kullanır.
/// </summary>
public static class CreatureFx
{
    /// <summary>Yaratık kayboldu: kuralı sağlıyorsa (bkz. <see cref="WordSpellRules.ShouldPoof"/>) puf efekti çıkar.</summary>
    public static void Vanished(Vector2 at, float radius, bool projectile, bool strike)
    {
        GameConfig config = GameConfigLoader.Get();
        if (config == null || !WordSpellRules.ShouldPoof(projectile, strike, radius, config.spellPoofMinRadius)) return;

        float size = Mathf.Max(0.3f, radius * 2f * config.spellPoofSize);
        if (AttackMarkEffect.Spawn("fx_poof", at, size, false, config.spellPoofFrameSeconds) != null) return;
        PulseSprite.Spawn(at, config.spellPoofColor, size * 0.4f, size, 0.35f, Gore.AirOrder + 2);
    }

    /// <summary>Kelime büyüsünün ilk harfi vardı: yerde çağırma çemberi.</summary>
    public static void Summon(Vector2 at)
    {
        GameConfig config = GameConfigLoader.Get();
        if (config == null) return;
        if (AttackMarkEffect.Spawn("fx_summon", at, config.spellSummonSize, false, config.spellSummonFrameSeconds) != null) return;
        PulseSprite.Spawn(at, config.spellSummonColor, config.spellSummonSize * 0.2f, config.spellSummonSize, 0.4f, 1);
    }

    /// <summary>Kelime büyüsünün son harfi vardı: yerde kısa beyaz flaş.</summary>
    public static void Flash(Vector2 at)
    {
        GameConfig config = GameConfigLoader.Get();
        if (config == null || config.spellWorldFlashSize <= 0f) return;
        PulseSprite.Spawn(at, new Color(1f, 1f, 1f, 0.85f), config.spellWorldFlashSize * 0.5f, config.spellWorldFlashSize,
                          Mathf.Max(0.05f, config.spellFlashSeconds), Gore.AirOrder + 3);
    }
}
