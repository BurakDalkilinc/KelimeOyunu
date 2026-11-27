using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maçın seslerini tur olaylarından çalar (zar, kelime, tur yanması, maç sonu), duruma göre müziği seçtirir. Vuruş ve eleme
/// sesleri karakterlerin vuruş tepkisinde, darbe anında çalar (bkz. <see cref="HitFeedback"/>, <see cref="HitSounds"/>). Tek kişilikte ve ağ maçında (istemcide de) aynı tur görünümünü dinler.
/// Tur kaynağının (<see cref="TurnViewSource"/>) yanına kendiliğinden eklenir; sahneye elle koymak gerekmez.
/// </summary>
public class MatchAudio : MonoBehaviour
{
    TurnViewSource source;
    ITurnView subscribed;
    TurnPhase lastPhase = TurnPhase.NotStarted;

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
            if (found.GetComponent<MatchAudio>() == null) found.gameObject.AddComponent<MatchAudio>();
    }

    // ---- Olaylar ----

    void Awake()
    {
        source = GetComponent<TurnViewSource>();
    }

    void OnDisable()
    {
        Unsubscribe();
        AudioService service = AudioService.Instance;
        if (service != null) service.RequestMusic(MusicTrack.Menu);
    }

    void Update()
    {
        ITurnView view = source != null ? source.View : null;
        if (view != subscribed) Resubscribe(view);

        AudioService service = AudioService.Instance;
        if (service != null)
            service.RequestMusic(view != null ? MusicDirector.Choose(true, view.Phase, view.CurrentWord) : MusicTrack.Menu);

    }

    void Resubscribe(ITurnView view)
    {
        Unsubscribe();
        subscribed = view;
        if (subscribed == null) return;

        lastPhase = subscribed.Phase;
        subscribed.PhaseChanged += OnPhaseChanged;
        subscribed.WriterSelected += OnWriterSelected;
        subscribed.WordPlayed += OnWordPlayed;
        subscribed.TurnSkipped += OnTurnSkipped;
        subscribed.MatchEnded += OnMatchEnded;
    }

    void Unsubscribe()
    {
        if (subscribed == null) return;
        subscribed.PhaseChanged -= OnPhaseChanged;
        subscribed.WriterSelected -= OnWriterSelected;
        subscribed.WordPlayed -= OnWordPlayed;
        subscribed.TurnSkipped -= OnTurnSkipped;
        subscribed.MatchEnded -= OnMatchEnded;
        subscribed = null;
    }

    void OnWriterSelected(PlayerState writer) => GameAudio.Play(SoundId.DiceRoll);

    /// <summary>Zar durunca (zar aşamasından yazmaya geçiş) "tak" sesi.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        if (lastPhase == TurnPhase.Dice && phase == TurnPhase.Typing) GameAudio.Play(SoundId.DiceLand);
        lastPhase = phase;
    }

    /// <summary>Kelimenin sesi etiketlerinden gelir (bkz. <see cref="SfxRules"/>); bilinmeyen kelimede word null'dır.</summary>
    void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        AudioService service = AudioService.Instance;
        if (service != null) service.PlayForWord(word);
    }

    void OnTurnSkipped(PlayerState writer) => GameAudio.Play(SoundId.WordBad);

    void OnMatchEnded(PlayerState winner) => GameAudio.Play(SfxRules.ForMatchEnd(winner, source.LocalPlayer));
}
