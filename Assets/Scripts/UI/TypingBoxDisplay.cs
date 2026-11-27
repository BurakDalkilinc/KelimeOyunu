using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// İnsan oyuncunun yazı kutusu: sıra sana gelince açılır, klavyeyle yazarsın, Enter ile gönderirsin.
/// Karakter, zar atıldığı andan yazma bitene kadar durur (hareket tuşu basılıyken kutuya harf dolmasın diye).
/// Burası girdi ve kuraldır; görüntü kodla eklenen <see cref="TypingBoxView"/>'da (Steam görselindeki kutu, sayaç, zarlar,
/// tuş kıvılcımı, silinen harfin düşüşü, red/kabul efektleri), kabul edilen kelimenin yaratığa uçuşu <see cref="WordSpellFx"/>'te.
/// Sahnedeki eski uGUI paneli gizli tutulur (sahne dosyası değişmez).
/// Yazılan metin <see cref="TypingBuffer"/>'da tutulur; kelimenin geçerli olup olmadığına <see cref="TurnManager"/> karar verir.
/// </summary>
public class TypingBoxDisplay : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] TurnViewSource runner;

    /// <summary>Tek kişilik sahnede yazarken karakteri durdurmak için. Ağda boş bırakılır (karakteri ağ katmanı durdurur).</summary>
    [SerializeField] PlayerController player;

    /// <summary>Eski uGUI kutusu: artık gizli tutulur (görüntü <see cref="TypingBoxView"/>'da).</summary>
    [SerializeField] GameObject panel;
    [SerializeField] Text inputText;
    [SerializeField] Text feedbackText;
    [SerializeField] Text hintText;

    ITurnView turns;
    TypingBuffer buffer;
    Keyboard keyboard;
    bool open;
    PressEdge enterEdge;
    float openedAt;
    TypingBoxView view;
    WordSpellFx spellFx;
    readonly CountdownTicker ticker = new CountdownTicker();

    /// <summary>En son gönderilen metin (kabul edilirse kelime büyüsünün harfleri bundan çıkar).</summary>
    public string LastSubmitted { get; private set; } = string.Empty;

    /// <summary>En son gönderme anı (unscaled sn).</summary>
    public float LastSubmittedAt { get; private set; } = -1000f;

    void OnEnable()
    {
        if (config == null || runner == null || runner.View == null)
        {
            Debug.LogError("TypingBoxDisplay: bir referans eksik (config, runner).", this);
            enabled = false;
            return;
        }

        turns = runner.View;
        buffer = new TypingBuffer(config.maxWordLength);
        HideLegacyPanel();

        view = GetComponent<TypingBoxView>();
        if (view == null) view = gameObject.AddComponent<TypingBoxView>();
        view.Bind(config, runner);
        spellFx = GetComponent<WordSpellFx>();
        if (spellFx == null) spellFx = gameObject.AddComponent<WordSpellFx>();
        spellFx.Bind(config, runner, view, this);

        turns.PhaseChanged += OnPhaseChanged;

        keyboard = Keyboard.current;
        if (keyboard != null) keyboard.onTextInput += OnTextInput;
        else Debug.LogWarning("TypingBoxDisplay: klavye bulunamadı, yazı yazılamaz.", this);
    }

    void OnDisable()
    {
        if (turns != null) turns.PhaseChanged -= OnPhaseChanged;
        if (keyboard != null) keyboard.onTextInput -= OnTextInput;
        HideLegacyPanel();
        RestoreMovement();
    }

    /// <summary>Sahnedeki eski uGUI kutusunu gizler ve yazılarını boşaltır.</summary>
    void HideLegacyPanel()
    {
        if (panel != null) panel.SetActive(false);
        if (inputText != null) inputText.text = string.Empty;
        if (feedbackText != null) feedbackText.text = string.Empty;
        if (hintText != null) hintText.text = string.Empty;
    }

    /// <summary>Aşama değişince: insan oyuncunun sırasıysa kutuyu aç ve karakteri durdur, değilse kapat.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        bool humanWriter = runner.IsLocal(turns.Writer);
        bool humanTurn = humanWriter && (phase == TurnPhase.Dice || phase == TurnPhase.Typing);

        if (humanTurn && player != null) player.CanMove = false;
        else RestoreMovement();

        if (humanWriter && phase == TurnPhase.Typing) Open();
        else Close();
    }

    void RestoreMovement()
    {
        if (player != null && player.State != null) player.CanMove = true; // Hayalet de gezebilir
    }

    void Open()
    {
        open = true;
        openedAt = Time.unscaledTime;
        buffer.Clear();
        ticker.Reset();
        view.Text = string.Empty;
    }

    void Close()
    {
        open = false;
        buffer.Clear();
        if (view != null) view.Text = string.Empty;
    }

    void OnTextInput(char c)
    {
        if (!open || UiInputBlock.IsBlocked) return; // Ayarlar açıkken harfler kutuya gitmez
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        string before = buffer.Text;
        buffer.Input(c);
        string after = buffer.Text;
        if (after == before) return;

        if (after.Length > before.Length)
        {
            // Harf başına hafif farklı perde: yazı bir melodi gibi duyulur.
            float pitch = TypingFxMath.KeyPitch(c, config.typeBoxKeyPitchSpread, config.typeBoxKeyPitchSteps);
            GameAudio.PlayPitched(SoundId.Key, config.audioKeyVolume, config.audioKeyCooldown, pitch);
            view.NotifyKey(TypingFxMath.SplitLetters(after).Count - 1);
        }
        else if (after.Length < before.Length && before.StartsWith(after))
        {
            // Silinen harf aşağı düşüp söner; ses biraz daha pes.
            GameAudio.PlayPitched(SoundId.Key, config.audioKeyVolume * 0.8f, config.audioKeyCooldown, 1f - config.typeBoxKeyPitchSpread * 2f);
            view.NotifyDeleted(before.Substring(after.Length), TypingFxMath.SplitLetters(after).Count);
        }
        view.Text = after;
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
        if (enterJustPressed && !UiInputBlock.IsBlocked) Submit();

        // Son saniyelerde kalın tik sesi (sayaç çubuğu kutuda titrer ve kızarır).
        if (ticker.Update(turns.PhaseTimeLeft, config.uiCountdownWarnSeconds))
            GameAudio.PlayPitched(SoundId.Key, config.uiCountdownTickVolume, 0f, config.uiCountdownTickPitch);
    }

    void Submit()
    {
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        // Kabul edilince aşama değişir ve kutu kendiliğinden kapanır. Reddedilirse sebebi gösterilir
        // (ağda cevap host'tan biraz gecikmeyle gelir). Metin saklanır: kabulde harfleri yaratığa uçar.
        LastSubmitted = buffer.Text;
        LastSubmittedAt = Time.unscaledTime;
        runner.SubmitWord(buffer.Text, ShowResult);
    }

    void ShowResult(SubmitStatus status)
    {
        GameAudio.Play(SfxRules.ForSubmit(status));

        bool accepted = status == SubmitStatus.Accepted || status == SubmitStatus.AcceptedUnknown;
        if (accepted) view.NotifyAccepted();
        else if (status != SubmitStatus.WrongPhase) view.NotifyRejected();

        string message = SubmitFeedback.GetMessage(status, config.maxWordLength);
        if (message.Length > 0) view.ShowFeedback(message, config.typingFeedbackSeconds);
    }
}
