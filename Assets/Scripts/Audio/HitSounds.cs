/// <summary>
/// Vuruş sesleri katmanlı çalar (yalnızca ses; yeni dosya yok, mevcut sesler perde/düzey oynamasıyla): ana darbe ("sfx_hit") +
/// altında kısa, düşük perdeli "güm" + ağır vuruşta (dev, eleme) üçüncü sert katman. Her vuruşta perde ±%8 oynar. Her katmanın
/// bekleme süresi ayrı sayılır (<see cref="AudioService.PlayPitched"/>): art arda vuruşlar duyulur, çalan ses kesilmez.
/// Başkasının vuruşu daha kısık duyulur. Ses servisi yoksa (EditMode testi) hiçbir şey yapmaz.
/// </summary>
public static class HitSounds
{
    const string ThumpGate = "#thump";
    const string HeavyGate = "#heavy";

    /// <summary>Karakter hasar aldı. <paramref name="mine"/> yerel oyuncu mu, <paramref name="heavy"/> dev vuruşu ya da eleme mi.</summary>
    public static void Hit(bool mine, bool heavy)
    {
        AudioService service = AudioService.Instance;
        if (service == null) return;
        GameConfig config = GameConfigLoader.Get();

        float volume = mine ? 1f : config.audioOtherHitVolume;
        service.PlayPitched(SoundId.Hit, volume, config.hitSoundCooldown, Pitch(service, config, 1f));
        Thump(service, config, volume);
        if (heavy) Heavy(service, config, volume);
    }

    /// <summary>Karakter elendi: eleme sesi + güm + ağır katman.</summary>
    public static void Eliminated(bool mine)
    {
        AudioService service = AudioService.Instance;
        if (service == null) return;
        GameConfig config = GameConfigLoader.Get();

        service.PlayPitched(SoundId.Eliminated, 1f, config.audioHitCooldown, Pitch(service, config, 1f));
        float volume = mine ? 1f : config.audioOtherHitVolume;
        Thump(service, config, volume);
        Heavy(service, config, volume);
    }

    /// <summary>Kalkan vuruşu engelledi: kalkan sesi + hafif güm.</summary>
    public static void Blocked(bool mine)
    {
        AudioService service = AudioService.Instance;
        if (service == null) return;
        GameConfig config = GameConfigLoader.Get();

        float volume = mine ? 1f : config.audioOtherHitVolume;
        service.PlayPitched(SoundId.Shield, volume, config.hitSoundCooldown, Pitch(service, config, 1.1f));
        Thump(service, config, volume * 0.6f);
    }

    /// <summary>Boss'a vuruldu: darbe + güm (dev olduğu için ağır katman da).</summary>
    public static void CreatureHurt(bool killed)
    {
        AudioService service = AudioService.Instance;
        if (service == null) return;
        GameConfig config = GameConfigLoader.Get();

        service.PlayPitched(SoundId.Hit, config.audioOtherHitVolume, config.hitSoundCooldown, Pitch(service, config, 0.8f));
        Thump(service, config, 1f);
        if (killed) Heavy(service, config, 1f);
    }

    /// <summary>İtiş birine değdi: yalnızca kısa güm.</summary>
    public static void Shove(float volume)
    {
        AudioService service = AudioService.Instance;
        if (service == null) return;
        GameConfig config = GameConfigLoader.Get();
        Thump(service, config, volume);
    }

    static void Thump(AudioService service, GameConfig config, float volume)
    {
        if (string.IsNullOrEmpty(config.hitThumpSound) || config.hitThumpVolume <= 0f) return;
        service.PlayPitched(config.hitThumpSound, volume * config.hitThumpVolume, config.hitSoundCooldown,
                            Pitch(service, config, config.hitThumpPitch), config.hitThumpSound + ThumpGate);
    }

    static void Heavy(AudioService service, GameConfig config, float volume)
    {
        if (string.IsNullOrEmpty(config.hitHeavySound) || config.hitHeavyVolume <= 0f) return;
        service.PlayPitched(config.hitHeavySound, volume * config.hitHeavyVolume, config.hitSoundCooldown,
                            Pitch(service, config, config.hitHeavyPitch), config.hitHeavySound + HeavyGate);
    }

    /// <summary>Taban perde × (1 ± oynama).</summary>
    static float Pitch(AudioService service, GameConfig config, float basePitch)
    {
        return basePitch * HitFeelMath.RandomPitch(service.NextPitchRandom(), config.hitSoundPitchVariation);
    }
}
