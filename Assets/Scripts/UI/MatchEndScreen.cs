using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Maç bitince ekranı kaplayan özet: büyük başlık "punch" ile girer (kazandıysan "KAZANDIN!" ve konfeti), istatistik satırları
/// sırayla belirir, altta "Tekrar oyna" / "Ana menü" düğmeleri (R tuşu da yeniden başlatır). Tek kişilikte sahne baştan yüklenir,
/// ağda yalnızca host yeni maç hazırlar. Yeni maç başlayınca panel kapanır.
/// Yazılar <see cref="MatchSummary"/>'de, zamanlama <see cref="UiTween"/>'de hesaplanır; burası yalnızca görüntü ve girdi.
/// </summary>
public class MatchEndScreen : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] GameObject panel;
    [SerializeField] Text titleText;
    [SerializeField] Text wordsText;
    [SerializeField] Text hintText;

    /// <summary>Panelin açılırken başladığı ölçek.</summary>
    const float PanelStartScale = 0.9f;

    ITurnView turns;
    PressEdge restartEdge;
    bool shown;
    PlayerState winner;
    bool localWon;
    float shownAt;

    GameConfig config;
    ButtonPainter buttons;
    readonly ConfettiField confetti = new ConfettiField();
    List<string> rows = new List<string>();
    readonly StringBuilder builder = new StringBuilder();

    void OnEnable()
    {
        if (runner == null || runner.View == null || panel == null || titleText == null || wordsText == null || hintText == null)
        {
            Debug.LogError("MatchEndScreen: bir referans eksik (runner, panel, titleText, wordsText, hintText).", this);
            enabled = false;
            return;
        }

        config = GameConfigLoader.Get();
        buttons = new ButtonPainter(config);
        turns = runner.View;
        turns.MatchEnded += OnMatchEnded;
        turns.PhaseChanged += OnPhaseChanged;
        Loc.LanguageChanged += OnLanguageChanged;
        panel.SetActive(false);
        shown = false;
        wordsText.supportRichText = true; // Belirmekte olan satırın saydamlığı <color> ile verilir
    }

    void OnDisable()
    {
        Loc.LanguageChanged -= OnLanguageChanged;
        confetti.Clear();
        if (turns == null) return;
        turns.MatchEnded -= OnMatchEnded;
        turns.PhaseChanged -= OnPhaseChanged;
    }

    void OnMatchEnded(PlayerState winner)
    {
        this.winner = winner;
        localWon = winner != null && runner.IsLocal(winner);
        shownAt = Time.unscaledTime;
        FillTexts();
        panel.SetActive(true);
        shown = true;

        confetti.Clear();
        if (localWon)
        {
            // Konfetinin rastgeleliği yalnızca görüntü içindir; oyun mantığının rastgeleliğine karışmaz.
            var random = new System.Random(turns.TurnNumber * 7919 + winner.Id);
            confetti.Burst(random, config.uiConfettiCount, Screen.width, config.uiConfettiMinSpeed * Scale,
                           config.uiConfettiMaxSpeed * Scale, config.uiConfettiColors != null ? config.uiConfettiColors.Length : 0);
        }
        Animate();
    }

    /// <summary>Ayarlardan dil değişirse açık özetin yazıları yeni dille yeniden yazılır.</summary>
    void OnLanguageChanged(string language)
    {
        if (shown) FillTexts();
    }

    void FillTexts()
    {
        titleText.text = MatchSummary.GetTitleFor(winner, localWon);
        titleText.color = localWon || winner == null ? config.uiWinTitleColor : config.uiLoseTitleColor;

        // Koleksiyon defteri: bu maçta ilk kez görülen ya da keşfedilen kelime varsa en üstte bildir (yoksa satır yok).
        CollectionRecorder recorder = runner.GetComponent<CollectionRecorder>();
        string notice = recorder != null ? CollectionText.NewWordsNotice(recorder.NewThisMatch) : string.Empty;
        rows = MatchSummary.GetStatRows(notice, turns.TurnNumber, runner.Players.Count, runner.UsedWords);
        hintText.text = runner.RestartHint;
    }

    /// <summary>Maç bittikten sonra başka bir aşamaya geçilirse (yeni maç hazırlandı) özet kapanır.</summary>
    void OnPhaseChanged(TurnPhase phase)
    {
        if (!shown || phase == TurnPhase.MatchOver) return;
        panel.SetActive(false);
        shown = false;
        confetti.Clear();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool rPressed = restartEdge.Update(keyboard != null && keyboard.rKey.isPressed);

        if (!shown) return;

        hintText.text = runner.RestartHint; // Ağda "host bekleniyor" yazısı değişebilir
        Animate();
        confetti.Step(Time.unscaledDeltaTime, config.uiConfettiGravity * Scale, config.uiConfettiDrag, Screen.height + 40f);

        if (rPressed && runner.CanRestart && !UiInputBlock.IsBlocked) Restart();
    }

    /// <summary>1080 piksel yükseklikli ekrana göre ölçek.</summary>
    static float Scale => Screen.height / 1080f;

    /// <summary>Satırlar ve düğmeler başlıktan biraz sonra başlar.</summary>
    float RowsElapsed => Time.unscaledTime - shownAt - config.uiEndTitleSeconds * 0.6f;

    /// <summary>Başlık "punch", panel hafif büyüme, satırlar sırayla, ipucu en son.</summary>
    void Animate()
    {
        float elapsed = Time.unscaledTime - shownAt;
        float titleScale = UiTween.PunchIn(elapsed, config.uiEndTitleSeconds, config.uiEndTitleStartScale, config.uiBannerOvershoot);
        titleText.rectTransform.localScale = new Vector3(titleScale, titleScale, 1f);

        float panelScale = UiTween.PunchIn(elapsed, config.uiEndTitleSeconds * 0.5f, PanelStartScale, Easing.DefaultOvershoot);
        panel.transform.localScale = new Vector3(panelScale, panelScale, 1f);

        wordsText.text = BuildRows(RowsElapsed);

        float hintAlpha = UiTween.StaggerProgress(RowsElapsed, rows.Count, config.uiEndRowDelay, config.uiEndRowFadeSeconds);
        Color c = hintText.color;
        hintText.color = new Color(c.r, c.g, c.b, hintAlpha);
    }

    /// <summary>Belirmiş satırlar; belirmekte olan satır yarı saydam (rich text renk etiketiyle).</summary>
    string BuildRows(float elapsed)
    {
        builder.Length = 0;
        int started = UiTween.StaggerStarted(elapsed, rows.Count, config.uiEndRowDelay);
        string rgb = ColorUtility.ToHtmlStringRGB(wordsText.color);
        for (int i = 0; i < started; i++)
        {
            if (i > 0) builder.Append('\n');
            float alpha = UiTween.StaggerProgress(elapsed, i, config.uiEndRowDelay, config.uiEndRowFadeSeconds);
            if (alpha >= 1f) { builder.Append(rows[i]); continue; }
            int a = Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
            builder.Append("<color=#").Append(rgb).Append(a.ToString("X2")).Append('>').Append(rows[i]).Append("</color>");
        }
        return builder.ToString();
    }

    void OnGUI()
    {
        if (!shown) return;
        DrawConfetti();
        DrawButtons();
    }

    void DrawConfetti()
    {
        if (Event.current.type != EventType.Repaint || confetti.Pieces.Count == 0) return;
        Color[] colors = config.uiConfettiColors;
        if (colors == null || colors.Length == 0) return;

        float size = Mathf.Max(2f, Mathf.Round(config.uiConfettiPieceSize * Scale));
        foreach (ConfettiPiece piece in confetti.Pieces)
        {
            // Dönen kâğıt: genişlik titrer (piksel adımlarıyla).
            float w = Mathf.Max(2f, Mathf.Round(size * Mathf.Abs(Mathf.Cos(piece.Spin))));
            Rect rect = new Rect(Mathf.Round(piece.Position.x - w * 0.5f), Mathf.Round(piece.Position.y), w, size);
            UiDraw.Fill(rect, colors[piece.ColorIndex % colors.Length]);
        }
    }

    /// <summary>Panelin hemen altında "Tekrar oyna" ve "Ana menü" düğmeleri (satırlar bitince belirir).</summary>
    void DrawButtons()
    {
        if (SettingsScreen.IsOpen || CollectionBookScreen.IsOpen) return;

        float appear = UiTween.StaggerProgress(RowsElapsed, rows.Count + 1, config.uiEndRowDelay, config.uiEndRowFadeSeconds);
        if (appear <= 0f) return;

        // Panelin alt kenarı (Overlay Canvas'ta dünya koordinatı = ekran pikseli, y yukarı).
        var corners = new Vector3[4];
        ((RectTransform)panel.transform).GetWorldCorners(corners);
        float panelBottom = Screen.height - corners[0].y;

        bool leave = runner.CanLeaveToMenu;
        int count = leave ? 2 : 1;
        float scale = Scale;
        float w = config.uiEndButtonWidth * scale, h = config.uiEndButtonHeight * scale, gap = config.uiEndButtonGap * scale;
        float x = (Screen.width - (count * w + (count - 1) * gap)) * 0.5f;
        float y = Mathf.Min(panelBottom + gap, Screen.height - h - gap);
        int fontSize = Mathf.Max(1, Mathf.RoundToInt(config.uiMenuFontSize * scale * 0.9f));

        Color old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * appear);
        if (buttons.Draw(new Rect(x, y, w, h), Loc.T("match.play_again"), "again", runner.CanRestart, fontSize)) Restart();
        if (leave && buttons.Draw(new Rect(x + w + gap, y, w, h), Loc.T("match.main_menu"), "menu", true, fontSize)) runner.LeaveToMenu();
        GUI.color = old;
    }

    /// <summary>Yeni maç için hazırlar (tek kişilikte sahneyi baştan yükler).</summary>
    public void Restart()
    {
        runner.Restart();
    }
}
