using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// İnsan oyuncunun yazı kutusu: sıra sana gelince açılır, klavyeyle yazarsın, Enter ile gönderirsin.
/// Karakter, zar atıldığı andan yazma bitene kadar durur (hareket tuşu basılıyken kutuya harf dolmasın diye).
/// Yazılan metin <see cref="TypingBuffer"/>'da tutulur; kelimenin geçerli olup olmadığına <see cref="TurnManager"/> karar verir.
/// </summary>
public class TypingBoxDisplay : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;
    [SerializeField] PlayerController player;
    [SerializeField] GameObject panel;
    [SerializeField] Text inputText;
    [SerializeField] Text feedbackText;

    TurnManager turns;
    TypingBuffer buffer;
    Keyboard keyboard;
    bool open;
    PressEdge enterEdge;
    float openedAt;
    float feedbackLeft;

    void OnEnable()
    {
        if (config == null || runner == null || runner.Turns == null || player == null
            || panel == null || inputText == null || feedbackText == null)
        {
            Debug.LogError("TypingBoxDisplay: bir referans eksik (config, runner, player, panel, inputText, feedbackText).", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        buffer = new TypingBuffer(config.maxWordLength);
        panel.SetActive(false);

        turns.PhaseChanged += OnPhaseChanged;

        keyboard = Keyboard.current;
        if (keyboard != null) keyboard.onTextInput += OnTextInput;
        else Debug.LogWarning("TypingBoxDisplay: klavye bulunamadı, yazı yazılamaz.", this);
    }

    void OnDisable()
    {
        if (turns != null) turns.PhaseChanged -= OnPhaseChanged;
        if (keyboard != null) keyboard.onTextInput -= OnTextInput;
        if (panel != null) panel.SetActive(false);
        RestoreMovement();
    }

    /// <summary>Aşama değişince: insan oyuncunun sırasıysa kutuyu aç ve karakteri durdur, değilse kapat.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        bool humanWriter = turns.Writer != null && !turns.Writer.IsBot;
        bool humanTurn = humanWriter && (phase == TurnPhase.Dice || phase == TurnPhase.Typing);

        if (humanTurn) player.CanMove = false;
        else RestoreMovement();

        if (humanWriter && phase == TurnPhase.Typing) Open();
        else Close();
    }

    void RestoreMovement()
    {
        if (player != null && player.State != null) player.CanMove = player.State.IsAlive;
    }

    void Open()
    {
        open = true;
        openedAt = Time.unscaledTime;
        buffer.Clear();
        feedbackLeft = 0f;
        feedbackText.text = string.Empty;
        panel.SetActive(true);
    }

    void Close()
    {
        open = false;
        buffer.Clear();
        panel.SetActive(false);
    }

    void OnTextInput(char c)
    {
        if (!open) return;
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        buffer.Input(c);
    }

    /// <summary>Klavye çıkarılıp takılırsa (yeni cihaz örneği) metin girişine yeniden abone olur.</summary>
    void EnsureKeyboard()
    {
        Keyboard current = Keyboard.current;
        if (current == keyboard) return;

        if (keyboard != null) keyboard.onTextInput -= OnTextInput;
        keyboard = current;
        if (keyboard != null) keyboard.onTextInput += OnTextInput;
    }

    void Update()
    {
        EnsureKeyboard();

        // Enter'ın "yeni basıldı" anı, tuşun basılı durumundaki değişimden bulunur (bkz. PressEdge).
        // Kutu kapalıyken de güncellenir: Enter basılı tutularak açılırsa hemen gönderme olmasın.
        bool enterDown = keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
        bool enterJustPressed = enterEdge.Update(enterDown);

        if (!open) return;

        if (enterJustPressed) Submit();

        UpdateFeedback();
        inputText.text = "Kelime: " + buffer.Text + (CaretVisible() ? "|" : " ");
    }

    void Submit()
    {
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        SubmitStatus status = turns.SubmitWord(turns.Writer.Id, buffer.Text);

        // Kabul edilince aşama değişir ve kutu kendiliğinden kapanır. Reddedilirse sebebi gösterilir.
        string message = SubmitFeedback.GetMessage(status, config.maxWordLength);
        if (message.Length > 0)
        {
            feedbackText.text = message;
            feedbackLeft = config.typingFeedbackSeconds;
        }
    }

    void UpdateFeedback()
    {
        if (feedbackLeft <= 0f) return;

        feedbackLeft -= Time.unscaledDeltaTime;
        if (feedbackLeft <= 0f) feedbackText.text = string.Empty;
    }

    static bool CaretVisible()
    {
        return Time.unscaledTime % 1f < 0.5f;
    }
}
