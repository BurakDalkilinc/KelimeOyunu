using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Maç bitince ekranı kaplayan özet: kazanan ve maçta yazılan kelimeler. R'ye basınca sahne baştan yüklenir (yeni maç).
/// Yazılar <see cref="MatchSummary"/>'de hesaplanır, kazanan <see cref="TurnManager"/>'dan gelir; burası yalnızca görüntü ve girdi.
/// </summary>
public class MatchEndScreen : MonoBehaviour
{
    [SerializeField] MatchRunner runner;
    [SerializeField] GameObject panel;
    [SerializeField] Text titleText;
    [SerializeField] Text wordsText;
    [SerializeField] Text hintText;

    TurnManager turns;
    PressEdge restartEdge;
    bool shown;

    void OnEnable()
    {
        if (runner == null || runner.Turns == null || panel == null || titleText == null || wordsText == null || hintText == null)
        {
            Debug.LogError("MatchEndScreen: bir referans eksik (runner, panel, titleText, wordsText, hintText).", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        turns.MatchEnded += OnMatchEnded;
        panel.SetActive(false);
        shown = false;
    }

    void OnDisable()
    {
        if (turns != null) turns.MatchEnded -= OnMatchEnded;
    }

    void OnMatchEnded(PlayerState winner)
    {
        titleText.text = MatchSummary.GetTitle(winner);
        wordsText.text = MatchSummary.GetWordsText(runner.WordTracker.UsedWords);
        hintText.text = MatchSummary.GetHint();
        panel.SetActive(true);
        shown = true;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool rPressed = restartEdge.Update(keyboard != null && keyboard.rKey.isPressed);

        if (shown && rPressed) Restart();
    }

    /// <summary>Sahneyi baştan yükler: yeni maç, yeni oyuncular, yeni kelime kilidi.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
