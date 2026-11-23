using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Maç bitince ekranı kaplayan özet: kazanan ve maçta yazılan kelimeler. R'ye basınca yeni maç hazırlanır
/// (tek kişilikte sahne baştan yüklenir, ağda yalnızca host yeni maç hazırlar). Yeni maç başlayınca panel kapanır.
/// Yazılar <see cref="MatchSummary"/>'de hesaplanır, kazanan tur kaynağından gelir; burası yalnızca görüntü ve girdi.
/// </summary>
public class MatchEndScreen : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] GameObject panel;
    [SerializeField] Text titleText;
    [SerializeField] Text wordsText;
    [SerializeField] Text hintText;

    ITurnView turns;
    PressEdge restartEdge;
    bool shown;

    void OnEnable()
    {
        if (runner == null || runner.View == null || panel == null || titleText == null || wordsText == null || hintText == null)
        {
            Debug.LogError("MatchEndScreen: bir referans eksik (runner, panel, titleText, wordsText, hintText).", this);
            enabled = false;
            return;
        }

        turns = runner.View;
        turns.MatchEnded += OnMatchEnded;
        turns.PhaseChanged += OnPhaseChanged;
        panel.SetActive(false);
        shown = false;
    }

    void OnDisable()
    {
        if (turns == null) return;
        turns.MatchEnded -= OnMatchEnded;
        turns.PhaseChanged -= OnPhaseChanged;
    }

    void OnMatchEnded(PlayerState winner)
    {
        titleText.text = MatchSummary.GetTitle(winner);
        wordsText.text = MatchSummary.GetWordsText(runner.UsedWords);
        hintText.text = runner.RestartHint;
        panel.SetActive(true);
        shown = true;
    }

    /// <summary>Maç bittikten sonra başka bir aşamaya geçilirse (yeni maç hazırlandı) özet kapanır.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        if (!shown || phase == TurnPhase.MatchOver) return;
        panel.SetActive(false);
        shown = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool rPressed = restartEdge.Update(keyboard != null && keyboard.rKey.isPressed);

        if (shown) hintText.text = runner.RestartHint; // Ağda "host bekleniyor" yazısı değişebilir

        if (shown && rPressed && runner.CanRestart) Restart();
    }

    /// <summary>Yeni maç için hazırlar (tek kişilikte sahneyi baştan yükler).</summary>
    public void Restart()
    {
        runner.Restart();
    }
}
