using UnityEngine;

/// <summary>
/// Oyunun tek ses nesnesi (sahneler arasında kalır): birkaç AudioSource'luk efekt havuzu ve iki kaynak arasında çapraz
/// geçişli müzik. Hangi sesin çalacağına <see cref="SfxRules"/> ve <see cref="MusicDirector"/> karar verir; burası yalnızca çalar.
/// Ses dosyası yoksa sessizce geçer. Kendiliğinden kurulur (bkz. <see cref="GameServices"/>).
/// </summary>
public class AudioService : MonoBehaviour
{
    /// <summary>Çalışan ses servisi; henüz kurulmadıysa null.</summary>
    public static AudioService Instance { get; private set; }

    GameConfig config;
    SoundLibrary library;
    SfxGate gate;
    AudioSource[] sfxSources;
    readonly AudioSource[] musicSources = new AudioSource[2];
    int activeMusic;
    float fade = 1f;           // 0 → 1: yeni müziğin açılma ilerlemesi
    float fadeOutStart;        // Eski müziğin geçiş başındaki düzeyi
    System.Random pitchRandom; // Yalnızca ses perdesi için; oyun mantığının rastgeleliğine karışmaz

    /// <summary>Efekt kuralları (etiket → ses).</summary>
    public SfxRules Rules { get; private set; }

    /// <summary>Şu an çalan (ya da açılmakta olan) müzik.</summary>
    public MusicTrack CurrentTrack { get; private set; } = MusicTrack.None;

    /// <summary>İstenen müzik; maç bileşeni her karede günceller, yoksa menü müziği.</summary>
    public MusicTrack RequestedTrack { get; private set; } = MusicTrack.Menu;

    /// <summary>Ayarlardaki müzik düzeyi (0..1).</summary>
    public float MusicVolume { get; private set; } = 1f;

    /// <summary>Ayarlardaki efekt düzeyi (0..1).</summary>
    public float SfxVolume { get; private set; } = 1f;

    /// <summary>Çalınan son efektin adı (deneme ve hata ayıklama için).</summary>
    public string LastPlayed { get; private set; }

    /// <summary>Bu oturumda çalınan efekt sayısı (deneme ve hata ayıklama için).</summary>
    public int PlayedCount { get; private set; }

    /// <summary>Şu an çalan efekt sayısı.</summary>
    public int PlayingSfxCount
    {
        get
        {
            int count = 0;
            if (sfxSources == null) return 0;
            foreach (AudioSource source in sfxSources)
                if (source.isPlaying) count++;
            return count;
        }
    }

    /// <summary>Şu an duyulan müzik kaynağı (deneme için).</summary>
    public AudioSource ActiveMusicSource => musicSources[activeMusic];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        config = GameConfigLoader.Get();
        library = new SoundLibrary(config.audioFolder);
        gate = new SfxGate(config.audioMaxSimultaneousSfx);
        Rules = SfxRules.LoadFromResources(config.audioSfxTagsPath);
        pitchRandom = new System.Random();

        sfxSources = new AudioSource[Mathf.Max(1, config.audioMaxSimultaneousSfx)];
        for (int i = 0; i < sfxSources.Length; i++) sfxSources[i] = CreateSource("Efekt " + (i + 1), false);
        for (int i = 0; i < musicSources.Length; i++) musicSources[i] = CreateSource("Müzik " + (i + 1), true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    AudioSource CreateSource(string sourceName, bool loop)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D: her yerden aynı duyulur
        return source;
    }

    // ---- Ses düzeyleri ----

    /// <summary>Müzik ve efekt düzeylerini ayarlar (0..1). Ana ses AudioListener.volume ile ayrıca ayarlanır.</summary>
    public void SetVolumes(float music, float sfx)
    {
        MusicVolume = Mathf.Clamp01(music);
        SfxVolume = Mathf.Clamp01(sfx);
    }

    // ---- Efektler ----

    /// <summary>
    /// Efekti çalar. <paramref name="cooldown"/> verilmezse (negatif) ayarlardaki ortak bekleme süresi kullanılır.
    /// Aynı ses bekleme süresi dolmadan ya da havuz doluyken istenirse çalmaz; çaldıysa true.
    /// </summary>
    public bool Play(string id, float volume = 1f, float cooldown = -1f)
    {
        float variation = config.audioPitchVariation;
        return PlayPitched(id, volume, cooldown, 1f + (float)(pitchRandom.NextDouble() * 2.0 - 1.0) * variation);
    }

    /// <summary>Efekti verilen perdeyle çalar (rastgele perde oynaması yok); ör. sayacın son saniyelerindeki kalın "tik".</summary>
    /// <param name="gateKey">Bekleme süresinin sayıldığı ad (boşsa sesin adı). Vuruş katmanları aynı dosyayı farklı perdeyle çalarken
    /// birbirini engellemesin diye ayrı ad verir.</param>
    public bool PlayPitched(string id, float volume, float cooldown, float pitch, string gateKey = null)
    {
        AudioClip clip = library.Get(id);
        if (clip == null) return false;

        int playing = 0;
        AudioSource free = null;
        foreach (AudioSource source in sfxSources)
        {
            if (source.isPlaying) playing++;
            else if (free == null) free = source;
        }

        float wait = cooldown >= 0f ? cooldown : config.audioSameSoundCooldown;
        if (free == null || !gate.TryPlay(string.IsNullOrEmpty(gateKey) ? id : gateKey, Time.unscaledTime, wait, playing)) return false;

        free.clip = clip;
        free.pitch = pitch;
        free.volume = Mathf.Clamp01(volume) * SfxVolume;
        free.Play();

        LastPlayed = id;
        PlayedCount++;
        return true;
    }

    /// <summary>0..1 arası rastgele sayı (yalnızca ses perdesi için; oyun mantığının rastgeleliğine karışmaz).</summary>
    public float NextPitchRandom() => (float)pitchRandom.NextDouble();

    /// <summary>Oynanan kelimenin sesi (etiketlerden; bilinmeyen kelimede null verilir).</summary>
    public void PlayForWord(WordEntry word) => Play(Rules.ForWord(word));

    // ---- Müzik ----

    /// <summary>Çalması istenen müziği bildirir; değiştiyse çapraz geçiş başlar.</summary>
    public void RequestMusic(MusicTrack track) => RequestedTrack = track;

    void Update()
    {
        if (RequestedTrack != CurrentTrack) SwitchTo(RequestedTrack);

        float target = config.audioMusicBaseVolume * MusicVolume;
        float seconds = config.audioMusicCrossfadeSeconds;
        fade = seconds <= 0f ? 1f : Mathf.Min(1f, fade + Time.unscaledDeltaTime / seconds);

        AudioSource active = musicSources[activeMusic];
        AudioSource old = musicSources[1 - activeMusic];
        active.volume = target * fade;
        if (old.isPlaying)
        {
            old.volume = fadeOutStart * (1f - fade);
            if (fade >= 1f) old.Stop();
        }
    }

    void SwitchTo(MusicTrack track)
    {
        CurrentTrack = track;
        AudioSource previous = musicSources[activeMusic];
        fadeOutStart = previous.isPlaying ? previous.volume : 0f;

        activeMusic = 1 - activeMusic;
        AudioSource next = musicSources[activeMusic];
        next.Stop();
        next.clip = library.Get(MusicDirector.ClipName(track));
        next.volume = 0f;
        if (next.clip != null) next.Play();
        fade = 0f;
    }
}
