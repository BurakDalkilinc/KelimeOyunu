using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// İnsan oyuncunun yazı kutusu: sıra sana gelince açılır, klavyeyle yazarsın, Enter ile gönderirsin.
/// Karakter, zar atıldığı andan yazma bitene kadar durur (hareket tuşu basılıyken kutuya harf dolmasın diye).
/// Görünüş: kutu "punch" ile açılır; her tuşta son harf küçükten büyür ve kutu hafifçe titrer; reddedilen kelimede kutu
/// sallanıp kırmızı yanar, kabul edilende yeşil parlar ve kelime yukarı uçar; son saniyelerde sayaç kırmızı nabız atar ve tik sesi çalar.
/// Yazılan metin <see cref="TypingBuffer"/>'da tutulur; kelimenin geçerli olup olmadığına <see cref="TurnManager"/> karar verir.
/// </summary>
public class TypingBoxDisplay : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] TurnViewSource runner;

    /// <summary>Tek kişilik sahnede yazarken karakteri durdurmak için. Ağda boş bırakılır (karakteri ağ katmanı durdurur).</summary>
    [SerializeField] PlayerController player;
    [SerializeField] GameObject panel;
    [SerializeField] Text inputText;
    [SerializeField] Text feedbackText;

    /// <summary>
    /// Kutunun ipucu yazısı ("Bir kelime yaz ve Enter'a bas"). Boşsa paneldeki diğer (giriş ve geri bildirim olmayan) yazı
    /// kullanılır; böylece sahneye dokunmadan seçili dilde yazılır.
    /// </summary>
    [SerializeField] Text hintText;

    /// <summary>Kutunun kapanış animasyonunda küçüleceği ölçek.</summary>
    const float ClosedScale = 0.85f;

    /// <summary>Kutunun açılışta başladığı ölçek.</summary>
    const float OpenStartScale = 0.6f;

    ITurnView turns;
    TypingBuffer buffer;
    Keyboard keyboard;
    bool open;
    PressEdge enterEdge;
    float openedAt;
    float feedbackLeft;

    // Görünüş durumu
    RectTransform panelRect;
    Image panelImage;
    CanvasGroup panelGroup;
    Vector2 panelBasePosition;
    Color panelBaseColor;
    Color hintBaseColor;
    bool closing;
    float closedAt;
    float popAt = -1000f;
    float jitterAt = -1000f;
    float shakeAt = -1000f;
    float flashAt = -1000f;
    Color flashColor;
    string submittedText = string.Empty;
    readonly CountdownTicker ticker = new CountdownTicker();

    void OnEnable()
    {
        if (config == null || runner == null || runner.View == null
            || panel == null || inputText == null || feedbackText == null)
        {
            Debug.LogError("TypingBoxDisplay: bir referans eksik (config, runner, panel, inputText, feedbackText).", this);
            enabled = false;
            return;
        }

        turns = runner.View;
        buffer = new TypingBuffer(config.maxWordLength);
        if (hintText == null) hintText = FindHintText();
        if (hintText != null) hintBaseColor = hintText.color;
        SetupPanelVisuals();
        panel.SetActive(false);
        closing = false;

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
        closing = false;
        RestoreMovement();
    }

    void SetupPanelVisuals()
    {
        if (panelRect != null) return;
        panelRect = panel.GetComponent<RectTransform>();
        panelImage = panel.GetComponent<Image>();
        panelGroup = panel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = panel.AddComponent<CanvasGroup>();
        if (panelRect != null) panelBasePosition = panelRect.anchoredPosition;
        if (panelImage != null) panelBaseColor = panelImage.color;
        inputText.supportRichText = true; // Son harfin "pop" büyümesi <size> etiketiyle yapılır
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

    /// <summary>Paneldeki giriş ve geri bildirim dışındaki ilk yazı (ipucu satırı); yoksa null.</summary>
    Text FindHintText()
    {
        foreach (Text each in panel.GetComponentsInChildren<Text>(true))
            if (each != inputText && each != feedbackText) return each;
        return null;
    }

    void Open()
    {
        open = true;
        closing = false;
        openedAt = Time.unscaledTime;
        buffer.Clear();
        feedbackLeft = 0f;
        feedbackText.text = string.Empty;
        flashAt = shakeAt = jitterAt = popAt = -1000f;
        ticker.Reset();
        panel.SetActive(true);
        Animate();
    }

    /// <summary>Girdiyi hemen kapatır; kutu kısa bir küçülme animasyonuyla kaybolur.</summary>
    void Close()
    {
        bool wasVisible = panel.activeSelf && !closing;
        open = false;
        buffer.Clear();
        if (!wasVisible) return;
        closing = true;
        closedAt = Time.unscaledTime;
    }

    void OnTextInput(char c)
    {
        if (!open || UiInputBlock.IsBlocked) return; // Ayarlar açıkken harfler kutuya gitmez
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        string before = buffer.Text;
        buffer.Input(c);
        if (buffer.Text == before) return;

        GameAudio.Play(SoundId.Key, config.audioKeyVolume, config.audioKeyCooldown);
        jitterAt = Time.unscaledTime;
        if (buffer.Text.Length > before.Length) popAt = Time.unscaledTime; // Silmede harf "pop" yapmaz
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

        if (closing)
        {
            inputText.text = Loc.F("typing.input", string.Empty);
            Animate();
            if (Time.unscaledTime - closedAt >= config.uiTypingCloseSeconds)
            {
                closing = false;
                panel.SetActive(false);
            }
            return;
        }

        if (!open) return;

        if (enterJustPressed && !UiInputBlock.IsBlocked) Submit();

        UpdateFeedback();
        UpdateCountdown();
        inputText.text = Loc.F("typing.input", BuildInputBody());
        Animate();
    }

    /// <summary>Yazılan metin; son harf "pop" sürerken <c>&lt;size&gt;</c> ile küçükten büyür. Sonda yanıp sönen imleç.</summary>
    string BuildInputBody()
    {
        string text = buffer.Text;
        string caret = CaretVisible() ? "|" : " ";
        float pop = UiTween.PunchIn(Time.unscaledTime - popAt, config.uiLetterPopSeconds, config.uiLetterPopStartScale, config.uiLetterPopOvershoot);
        if (text.Length == 0 || Mathf.Approximately(pop, 1f)) return text + caret;

        // Son "harf" emoji ise iki karakterdir (bkz. TypingBuffer).
        int lastLength = text.Length >= 2 && char.IsLowSurrogate(text[text.Length - 1]) ? 2 : 1;
        string head = text.Substring(0, text.Length - lastLength);
        string last = text.Substring(text.Length - lastLength);
        int size = Mathf.Max(1, Mathf.RoundToInt(inputText.fontSize * pop));
        return head + "<size=" + size + ">" + last + "</size>" + caret;
    }

    /// <summary>Yazma süresinin son saniyeleri: ipucu satırında kalan süre, kırmızı nabız ve kalın tik sesi.</summary>
    void UpdateCountdown()
    {
        float left = turns.PhaseTimeLeft;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(left));
        bool warning = CountdownTicker.IsWarning(left, config.uiCountdownWarnSeconds);

        if (ticker.Update(left, config.uiCountdownWarnSeconds))
            GameAudio.PlayPitched(SoundId.Key, config.uiCountdownTickVolume, 0f, config.uiCountdownTickPitch);

        if (hintText == null) return;
        hintText.text = Loc.F("typing.hint_time", seconds);
        float tick = warning ? UiTween.SecondTick(left) : 0f;
        hintText.color = warning ? Color.Lerp(config.uiCountdownColor, Color.white, 0.3f * (1f - tick)) : hintBaseColor;
        float scale = 1f + config.uiCountdownPulseScale * tick;
        hintText.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Kutunun ölçeği, kayması (titreme/sallanma), saydamlığı ve rengi (kırmızı/yeşil parlama, son saniye nabzı).</summary>
    void Animate()
    {
        if (panelRect == null) return;
        float now = Time.unscaledTime;

        float scale = UiTween.PunchIn(now - openedAt, config.uiTypingOpenSeconds, OpenStartScale, Easing.DefaultOvershoot);
        float alpha = UiTween.Fade(now - openedAt, config.uiTypingOpenSeconds * 0.5f, true);
        if (closing)
        {
            float u = config.uiTypingCloseSeconds > 0f ? (now - closedAt) / config.uiTypingCloseSeconds : 1f;
            scale = Mathf.Lerp(1f, ClosedScale, Easing.InCubic(u));
            alpha = 1f - Easing.InCubic(u);
        }

        float jitter = config.uiKeyJitterPixels;
        Vector2 offset = new Vector2(
            UiTween.Shake(now - shakeAt, config.uiRejectShakeSeconds, config.uiRejectShakePixels, config.uiShakeFrequency)
            + UiTween.Shake(now - jitterAt, config.uiKeyJitterSeconds, jitter, 1f / Mathf.Max(0.001f, config.uiKeyJitterSeconds)),
            UiTween.Shake(now - jitterAt + config.uiKeyJitterSeconds * 0.25f, config.uiKeyJitterSeconds, jitter * 0.6f,
                          1.5f / Mathf.Max(0.001f, config.uiKeyJitterSeconds)));

        panelRect.localScale = new Vector3(scale, scale, 1f);
        panelRect.anchoredPosition = panelBasePosition + offset;
        panelGroup.alpha = alpha;

        if (panelImage == null) return;
        Color color = panelBaseColor;
        if (open && CountdownTicker.IsWarning(turns.PhaseTimeLeft, config.uiCountdownWarnSeconds))
            color = Color.Lerp(color, config.uiRejectColor, 0.45f * UiTween.SecondTick(turns.PhaseTimeLeft));
        float flash = config.uiFlashSeconds > 0f ? 1f - Mathf.Clamp01((now - flashAt) / config.uiFlashSeconds) : 0f;
        panelImage.color = Color.Lerp(color, flashColor, flash);
    }

    void Submit()
    {
        if (Time.unscaledTime - openedAt < config.typingInputGraceSeconds) return;

        // Kabul edilince aşama değişir ve kutu kendiliğinden kapanır. Reddedilirse sebebi gösterilir
        // (ağda cevap host'tan biraz gecikmeyle gelir). Metin saklanır: kabulde kutudan yukarı uçar.
        submittedText = buffer.Text;
        runner.SubmitWord(buffer.Text, ShowResult);
    }

    void ShowResult(SubmitStatus status)
    {
        GameAudio.Play(SfxRules.ForSubmit(status));

        bool accepted = status == SubmitStatus.Accepted || status == SubmitStatus.AcceptedUnknown;
        if (accepted)
        {
            flashColor = config.uiAcceptColor;
            flashAt = Time.unscaledTime;
            FlyingText.Launch(inputText, submittedText.Trim(), config.uiBannerReadyColor, config.uiWordFlySeconds, config.uiWordFlyDistance);
        }
        else if (status != SubmitStatus.WrongPhase)
        {
            flashColor = config.uiRejectColor;
            flashAt = Time.unscaledTime;
            shakeAt = Time.unscaledTime;
        }

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
