using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maç boyunca yerel oyuncunun vuruş hissini yönetir (yalnızca görüntü ve ses): yerel oyuncu vurulunca ekran kırmızı parlar ve can
/// yazısı sallanır (bkz. <see cref="HurtScreenFlash"/>, <see cref="HealthDisplay"/>), can 1'e düşünce yumuşak kırmızı nabız ve
/// kalp atışı (mevcut bir sesin düşük perdesiyle) başlar. Tek kişilik maçta kazanan belli olunca (son eleme) kısa bir ağır çekim
/// yapar; bu tek yerde Time.timeScale değişir, çünkü maç bitmiştir ve ağ yoktur (ağ maçında ağır çekim yok).
/// Tur kaynağının (<see cref="TurnViewSource"/>) yanına kendiliğinden eklenir.
/// </summary>
public class HitFeelDirector : MonoBehaviour
{
    /// <summary>Yerel oyuncunun son vuruş tepkisinin anı (gerçek zaman, sn); hiç vurulmadıysa çok eski.</summary>
    static float lastLocalHit = -1000f;

    /// <summary>Son yerel vuruş kalkanla mı engellendi?</summary>
    public static bool LastLocalHitBlocked { get; private set; }

    /// <summary>Yerel oyuncunun son vuruşundan bu yana geçen gerçek zaman (sn).</summary>
    public static float LocalHitAge => Time.unscaledTime - lastLocalHit;

    /// <summary>Düşük can nabzı (0..1); can düşük değilse 0.</summary>
    public static float LowHealthPulse { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lastLocalHit = -1000f;
        LastLocalHitBlocked = false;
        LowHealthPulse = 0f;
    }

    // ---- Sahnedeki tur kaynaklarına kendiliğinden eklenme ----

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToSources();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToSources();

    static void AttachToSources()
    {
        foreach (TurnViewSource found in FindObjectsByType<TurnViewSource>(FindObjectsInactive.Include))
        {
            if (found.GetComponent<HitFeelDirector>() == null) found.gameObject.AddComponent<HitFeelDirector>();
            if (found.GetComponent<HurtScreenFlash>() == null) found.gameObject.AddComponent<HurtScreenFlash>();
        }
    }

    TurnViewSource source;
    ITurnView subscribed;
    GameConfig config;
    float heartbeatPhase = -1f;

    // Ağır çekim
    bool slowMotion;
    float slowMotionStart;
    float appliedScale = 1f;

    void Awake()
    {
        source = GetComponent<TurnViewSource>();
        config = GameConfigLoader.Get();
    }

    void OnEnable() => HitFeedback.LocalHit += OnLocalHit;

    void OnDisable()
    {
        HitFeedback.LocalHit -= OnLocalHit;
        Unsubscribe();
        EndSlowMotion();
        LowHealthPulse = 0f;
    }

    void OnLocalHit(HitFeedbackInfo info)
    {
        lastLocalHit = Time.unscaledTime;
        LastLocalHitBlocked = info.Blocked;
    }

    void Update()
    {
        ITurnView view = source != null ? source.View : null;
        if (view != subscribed)
        {
            Unsubscribe();
            subscribed = view;
            if (subscribed != null) subscribed.MatchEnded += OnMatchEnded;
        }

        UpdateLowHealth();
        UpdateSlowMotion();
    }

    void Unsubscribe()
    {
        if (subscribed != null) subscribed.MatchEnded -= OnMatchEnded;
        subscribed = null;
    }

    /// <summary>Düşük can: kırmızı nabız ve her atışta boğuk "güm" (ayrı kalp sesi yok; mevcut sesin düşük perdesi).</summary>
    void UpdateLowHealth()
    {
        PlayerState local = source != null ? source.LocalPlayer : null;
        bool low = local != null && HitFeelMath.IsLowHealth(local.Health.Current, local.Health.Max, config.lowHealthThreshold);
        if (!low)
        {
            LowHealthPulse = 0f;
            heartbeatPhase = -1f;
            return;
        }

        float time = Time.unscaledTime;
        LowHealthPulse = HitFeelMath.HeartbeatPulse(time, config.lowHealthHeartbeatBpm, out int _);

        float period = 60f / Mathf.Max(1f, config.lowHealthHeartbeatBpm);
        float phase = Mathf.Repeat(time, period) / period;
        if (heartbeatPhase >= 0f && !string.IsNullOrEmpty(config.lowHealthHeartbeatSound))
        {
            for (int i = 0; i < HitFeelMath.HeartbeatOnsets.Length; i++)
            {
                if (!HitFeelMath.CrossedPhase(heartbeatPhase, phase, HitFeelMath.HeartbeatOnsets[i])) continue;
                GameAudio.PlayPitched(config.lowHealthHeartbeatSound, config.lowHealthHeartbeatVolume * (i == 0 ? 1f : 0.7f), 0f,
                                      config.lowHealthHeartbeatPitch * (i == 0 ? 1f : 0.9f));
            }
        }
        heartbeatPhase = phase;
    }

    /// <summary>Tek kişilik maç bitti (kazanan belli oldu, son eleme): kısa ağır çekim.</summary>
    void OnMatchEnded(PlayerState winner)
    {
        if (!(source is MatchRunner)) return;          // Ağ maçında ağır çekim yok (yalnızca görüntü)
        if (config.finalSlowMotionSeconds <= 0f) return;
        if (!Mathf.Approximately(Time.timeScale, 1f)) return; // Deneme paneli zamanı değiştirmişse karışma

        slowMotion = true;
        slowMotionStart = Time.unscaledTime;
        ApplyScale(HitFeelMath.SlowMotionScale(0f, config.finalSlowMotionSeconds, config.finalSlowMotionScale, config.finalSlowMotionEaseFraction));
    }

    void UpdateSlowMotion()
    {
        if (!slowMotion) return;

        // Başkası (yeniden başlatma, deneme paneli) zamanı değiştirdiyse ağır çekim biter, ona dokunulmaz.
        if (!Mathf.Approximately(Time.timeScale, appliedScale)) { slowMotion = false; return; }

        float elapsed = Time.unscaledTime - slowMotionStart;
        float scale = HitFeelMath.SlowMotionScale(elapsed, config.finalSlowMotionSeconds, config.finalSlowMotionScale, config.finalSlowMotionEaseFraction);
        ApplyScale(scale);
        if (elapsed >= config.finalSlowMotionSeconds) EndSlowMotion();
    }

    void ApplyScale(float scale)
    {
        appliedScale = scale;
        Time.timeScale = scale;
    }

    void EndSlowMotion()
    {
        if (!slowMotion) return;
        slowMotion = false;
        if (Mathf.Approximately(Time.timeScale, appliedScale)) Time.timeScale = 1f;
        appliedScale = 1f;
    }
}
