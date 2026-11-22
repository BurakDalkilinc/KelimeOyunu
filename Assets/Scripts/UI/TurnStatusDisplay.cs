using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zar yazısının altında tur durumunu söyler: kim yazıyor, kaç saniye kaldı, hangi kelime çıktı, kim kazandı.
/// Yalnızca gösterir; tur mantığı <see cref="TurnManager"/>'dadır.
/// </summary>
public class TurnStatusDisplay : MonoBehaviour
{
    [SerializeField] MatchRunner runner;
    [SerializeField] Text label;

    TurnManager turns;

    void OnEnable()
    {
        if (runner == null || runner.Turns == null || label == null)
        {
            Debug.LogError("TurnStatusDisplay: runner ya da label eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
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
                return writer.IsBot
                    ? $"{writer.Name} bir kelime yazıyor... ({secondsLeft} sn)"
                    : $"Sıra sende! Bir kelime yaz ({secondsLeft} sn)";

            case TurnPhase.Result:
                string word = turns.CurrentWord != null ? turns.CurrentWord.display : "???";
                return $"{writer.Name} yazdı: {word}  ({secondsLeft} sn)";

            case TurnPhase.MatchOver:
                return turns.Winner != null ? $"Kazanan: {turns.Winner.Name}" : "Kazanan yok";

            default:
                return string.Empty;
        }
    }
}
