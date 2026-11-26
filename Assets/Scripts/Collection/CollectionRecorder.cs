using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maçta oynanan kelimeleri deftere işler: yazan bu bilgisayardaki oyuncuysa "keşfedildi", başkasıysa (oyuncu ya da bot) "görüldü".
/// Tur kaynağının (<see cref="TurnViewSource"/>) yanına kendiliğinden eklenir; sahneye elle koymak gerekmez.
/// </summary>
public class CollectionRecorder : MonoBehaviour
{
    TurnViewSource source;
    ITurnView subscribed;
    bool matchEnded;

    /// <summary>Bu maçta deftere eklenen (kilitli durumdan çıkan) kelime sayısı.</summary>
    public int NewThisMatch => CollectionService.Book.NewThisMatchCount;

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
            if (found.GetComponent<CollectionRecorder>() == null) found.gameObject.AddComponent<CollectionRecorder>();
    }

    // ---- Kayıt ----

    void Awake()
    {
        source = GetComponent<TurnViewSource>();
        CollectionService.Book.BeginMatch();
    }

    /// <summary>Ağ maçında tur görünümü sonradan oluşur (ya da yenilenir); her karede kontrol edip aboneliği tazeler.</summary>
    void Update()
    {
        ITurnView view = source != null ? source.View : null;
        if (view == subscribed) return;
        Unsubscribe();
        subscribed = view;
        if (subscribed == null) return;
        subscribed.WordPlayed += OnWordPlayed;
        subscribed.MatchEnded += OnMatchEnded;
        subscribed.PhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        Unsubscribe();
        CollectionService.Save();
    }

    void OnApplicationQuit() => CollectionService.Save();

    void Unsubscribe()
    {
        if (subscribed == null) return;
        subscribed.WordPlayed -= OnWordPlayed;
        subscribed.MatchEnded -= OnMatchEnded;
        subscribed.PhaseChanged -= OnPhaseChanged;
        subscribed = null;
    }

    /// <summary>Kabul edilen kelime. Bilinmeyen/reddedilen kelimede kayıt yoktur (WordEntry null gelir ya da olay hiç gelmez).</summary>
    void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        if (word == null) return;
        if (matchEnded) BeginNewMatch();

        // Ağda oyuncu nesneleri kadro mesajıyla yeniden kurulabilir; bu yüzden nesne değil kimlik karşılaştırılır.
        PlayerState local = source.LocalPlayer;
        bool mine = writer != null && local != null && writer.Id == local.Id;

        if (mine) CollectionService.Book.RecordWritten(word);
        else CollectionService.Book.RecordSeen(word);
    }

    void OnMatchEnded(PlayerState winner)
    {
        matchEnded = true;
        CollectionService.Save();
    }

    /// <summary>Maç bittikten sonra yeni maç hazırlandıysa "bu maçta yeni" sayacı sıfırlanır.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        if (matchEnded && phase != TurnPhase.MatchOver) BeginNewMatch();
    }

    void BeginNewMatch()
    {
        matchEnded = false;
        CollectionService.Book.BeginMatch();
    }
}
