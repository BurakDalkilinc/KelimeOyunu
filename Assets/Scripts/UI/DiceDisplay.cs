using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın üstünde zar animasyonunu gösterir: hayattaki oyuncuların isimleri arasında dönen seçim,
/// sonunda yazacak oyuncuda durur. Kimin seçileceğine <see cref="Dice"/> karar verir, burası yalnızca gösterir.
/// Yazma ve sonuç aşamalarında seçilen oyuncunun adı ekranda kalır.
/// </summary>
public class DiceDisplay : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;
    [SerializeField] Text label;
    [SerializeField] Color spinningColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color landedColor = Color.white;
    [SerializeField] Color humanColor = new Color(0.3f, 0.95f, 0.6f);

    TurnManager turns;
    readonly List<string> candidateNames = new List<string>();
    int finalIndex;

    void OnEnable()
    {
        if (runner == null || runner.Turns == null || label == null || config == null)
        {
            Debug.LogError("DiceDisplay: runner, config ya da label eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        turns.WriterSelected += OnWriterSelected;
        label.text = string.Empty;
    }

    void OnDisable()
    {
        if (turns != null) turns.WriterSelected -= OnWriterSelected;
    }

    /// <summary>Zar atıldığı an: dönecek isimleri ve duracağı yeri belirler.</summary>
    void OnWriterSelected(PlayerState writer)
    {
        candidateNames.Clear();
        finalIndex = 0;

        foreach (PlayerState player in runner.Match.AlivePlayers)
        {
            if (player == writer) finalIndex = candidateNames.Count;
            candidateNames.Add(player.Name);
        }
    }

    void Update()
    {
        switch (turns.Phase)
        {
            case TurnPhase.Dice:
                ShowSpinner();
                break;
            case TurnPhase.Typing:
            case TurnPhase.Result:
                ShowWriter();
                break;
            case TurnPhase.MatchOver:
                label.color = landedColor;
                label.text = "Maç bitti";
                break;
            default:
                label.text = string.Empty;
                break;
        }
    }

    void ShowSpinner()
    {
        float elapsed = config.diceSeconds - turns.PhaseTimeLeft;
        int index = DiceSpinner.GetIndex(elapsed, config.diceSeconds, candidateNames.Count, finalIndex);
        if (index < 0) return;

        label.text = candidateNames[index];
        label.color = spinningColor;
    }

    void ShowWriter()
    {
        if (turns.Writer == null) return;

        label.text = turns.Writer.Name;
        label.color = turns.Writer.IsBot ? landedColor : humanColor;
    }
}
