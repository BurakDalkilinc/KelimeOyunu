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
                return "Zar atılıyor...";

            case TurnPhase.Typing:
                if (writer == null) return string.Empty;
                return runner.IsLocal(writer)
                    ? $"Sıra sende! Bir kelime yaz ({secondsLeft} sn)"
                    : $"{writer.Name} bir kelime yazıyor... ({secondsLeft} sn)";

            case TurnPhase.Result:
                string word = turns.CurrentWord != null ? turns.CurrentWord.display : "???";
                return writer == null ? string.Empty : $"{writer.Name} yazdı: {word}  ({secondsLeft} sn)";

            case TurnPhase.MatchOver:
                return turns.Winner != null ? $"Kazanan: {turns.Winner.Name}" : "Kazanan yok";

            default:
                return string.Empty;
        }
    }
}
