using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zar yazısının altında tur durumunu söyler: kim yazıyor, kaç saniye kaldı, hangi kelime çıktı, kim kazandı.
/// Yalnızca gösterir; tur mantığı <see cref="TurnManager"/>'dadır.
/// </summary>
public class TurnStatusDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    ITurnView turns;

    void OnEnable()
    {
        if (runner == null || runner.View == null || label == null)
        {
            Debug.LogError("TurnStatusDisplay: runner ya da label eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.View;
    }

    void Update()
    {
        label.text = BuildText();
    }

    string BuildText()
    {
        int secondsLeft = Mathf.CeilToInt(turns.PhaseTimeLeft);
        PlayerState writer = turns.Writer;

        switch (turns.Phase)
        {
            case TurnPhase.Dice:
                return Loc.T("turn.rolling");

            case TurnPhase.Typing:
                if (writer == null) return string.Empty;
                return runner.IsLocal(writer)
                    ? Loc.F("turn.your_turn", secondsLeft)
                    : Loc.F("turn.other_typing", writer.Name, secondsLeft);

            case TurnPhase.Result:
                string word = turns.CurrentWord != null ? turns.CurrentWord.LocalizedDisplay : "???";
                return writer == null ? string.Empty : Loc.F("turn.wrote", writer.Name, word, secondsLeft);

            case TurnPhase.MatchOver:
                return MatchSummary.GetTitle(turns.Winner);

            default:
                return string.Empty;
        }
    }
}
