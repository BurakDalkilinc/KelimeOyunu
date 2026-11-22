using UnityEngine;

/// <summary>
/// Sıra bota gelince onun yerine kelime yazar: biraz bekler, sonra kullanılmamış bir kelime gönderir.
/// Kelimeyi seçmek <see cref="BotWordChooser"/>'ın, doğrulamak <see cref="TurnManager"/>'ın işidir.
/// </summary>
public class BotTypist : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;

    TurnManager turns;
    System.Random random;
    bool waiting;
    float waitLeft;

    void OnEnable()
    {
        if (runner == null || runner.Turns == null || config == null)
        {
            Debug.LogError("BotTypist: runner ya da config eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        random = new System.Random(runner.Random.Next());
        turns.PhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (turns != null) turns.PhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(TurnPhase phase)
    {
        waiting = phase == TurnPhase.Typing && turns.Writer != null && turns.Writer.IsBot;
        if (!waiting) return;

        float min = Mathf.Min(config.botTypingDelayMin, config.botTypingDelayMax);
        float max = Mathf.Max(config.botTypingDelayMin, config.botTypingDelayMax);
        waitLeft = min + (float)random.NextDouble() * (max - min);
    }

    void Update()
    {
        if (!waiting) return;

        waitLeft -= Time.deltaTime;
        if (waitLeft > 0f) return;

        waiting = false;
        string text = BotWordChooser.Choose(runner.Words.Words, runner.WordTracker.IsUsed, random);
        turns.SubmitWord(turns.Writer.Id, text);
    }
}
