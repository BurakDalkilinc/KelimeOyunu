using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zar yazısının altında tur durumunu söyler: kim yazıyor, kaç saniye kaldı, hangi kelime çıktı, kim kazandı.
/// Her yeni durum (aşama/yazan değişince) yazıya "punch" ile girer, boşalınca kayarak çıkar (<see cref="PunchText"/>);
/// yazma süresinin son saniyelerinde yazı kırmızı nabız atar. Yalnızca gösterir; tur mantığı <see cref="TurnManager"/>'dadır.
/// </summary>
public class TurnStatusDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    ITurnView turns;
    PunchText punch;
    GameConfig config;
    Color baseColor;

    void OnEnable()
    {
        if (runner == null || runner.View == null || label == null)
        {
            Debug.LogError("TurnStatusDisplay: runner ya da label eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.View;
        config = GameConfigLoader.Get();
        punch = PunchText.Of(label);
        baseColor = label.color;
    }

    void Update()
    {
        // Konu: aşama + tur + yazan. Aynı konuda yalnızca geri sayım değişir, yazı yeniden zıplamaz.
        string topic = turns.Phase + ":" + turns.TurnNumber + ":" + (turns.Writer != null ? turns.Writer.Id : -1);
        punch.Set(BuildText(), topic);
        ApplyCountdownColor();
    }

    /// <summary>Yazma süresinin son saniyelerinde her saniye başında kırmızıya parlar.</summary>
    void ApplyCountdownColor()
    {
        float alpha = label.color.a; // Saydamlığı PunchText yönetir
        Color color = baseColor;
        if (turns.Phase == TurnPhase.Typing && CountdownTicker.IsWarning(turns.PhaseTimeLeft, config.uiCountdownWarnSeconds))
            color = Color.Lerp(config.uiCountdownColor, Color.white, 0.35f * (1f - UiTween.SecondTick(turns.PhaseTimeLeft)));
        label.color = new Color(color.r, color.g, color.b, alpha);
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
