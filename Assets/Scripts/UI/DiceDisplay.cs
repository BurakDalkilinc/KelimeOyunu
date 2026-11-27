using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın üstünde zar animasyonunu gösterir: hayattaki oyuncuların isimleri arasında dönen seçim ve yanında hızla dönüp
/// yavaşlayan (ease-out) bir zar; isim her değiştiğinde yazı hafifçe sıçrar, durunca büyük sıçrar ve ekranın ortasına
/// seçilenin adı (kendi sıranda sarı, sarsılan "SIRA SENDE!") gelir. Maçın ilk zarında "HAZIR!" duyurulur.
/// Kimin seçileceğine <see cref="Dice"/> karar verir, burası yalnızca gösterir. Duruş sesi MatchAudio'dadır.
/// </summary>
public class DiceDisplay : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;
    [SerializeField] Color spinningColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color landedColor = Color.white;
    [SerializeField] Color humanColor = new Color(0.3f, 0.95f, 0.6f);

    ITurnView turns;
    readonly List<string> candidateNames = new List<string>();
    int finalIndex;

    TurnPhase lastPhase = TurnPhase.NotStarted;
    int lastIndex = -1;
    float tickAt = -1000f;
    float landedAt = -1000f;
    Image die;
    BigBanner banner;

    void OnEnable()
    {
        if (runner == null || runner.View == null || label == null || config == null)
        {
            Debug.LogError("DiceDisplay: runner, config ya da label eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.View;
        turns.WriterSelected += OnWriterSelected;
        label.text = string.Empty;
        lastPhase = turns.Phase;
        EnsureDie();
        banner = BigBanner.For(label);
    }

    void OnDisable()
    {
        if (turns != null) turns.WriterSelected -= OnWriterSelected;
        if (die != null) die.enabled = false;
    }

    /// <summary>Zar atıldığı an: dönecek isimleri ve duracağı yeri belirler.</summary>
    void OnWriterSelected(PlayerState writer)
    {
        candidateNames.Clear();
        finalIndex = 0;
        lastIndex = -1;

        foreach (PlayerState player in runner.AlivePlayers)
        {
            if (player == writer) finalIndex = candidateNames.Count;
            candidateNames.Add(player.Name);
        }
    }

    void Update()
    {
        TurnPhase phase = turns.Phase;
        if (phase != lastPhase) OnPhaseChanged(lastPhase, phase);
        lastPhase = phase;

        switch (phase)
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
                label.text = Loc.T("dice.match_over");
                break;
            default:
                label.text = string.Empty;
                break;
        }

        AnimateLabel();
        AnimateDie(phase);
    }

    /// <summary>Maçın ilk zarında "HAZIR!", zar durunca seçilenin adı ortaya gelir.</summary>
    void OnPhaseChanged(TurnPhase from, TurnPhase to)
    {
        if (to == TurnPhase.Dice && turns.TurnNumber <= 1 && banner != null)
            banner.Show(Loc.T("banner.ready"), config.uiBannerReadyColor, false, config.uiFadeSeconds); // Sahne geçişi açılınca

        if (from != TurnPhase.Dice || to != TurnPhase.Typing) return;

        landedAt = Time.unscaledTime;
        PlayerState writer = turns.Writer;
        if (writer == null || banner == null) return;

        if (runner.IsLocal(writer)) banner.Show(Loc.T("banner.your_turn"), config.uiBannerYourTurnColor, true);
        else banner.Show(writer.Name, config.uiBannerWriterColor, false);
    }

    void ShowSpinner()
    {
        float elapsed = config.diceSeconds - turns.PhaseTimeLeft;
        int index = DiceSpinner.GetIndex(elapsed, config.diceSeconds, candidateNames.Count, finalIndex);
        if (index < 0) return;

        if (index != lastIndex)
        {
            lastIndex = index;
            tickAt = Time.unscaledTime;
        }
        label.text = candidateNames[index];
        label.color = spinningColor;
    }

    void ShowWriter()
    {
        if (turns.Writer == null) return;

        label.text = turns.Writer.Name;
        label.color = runner.IsLocal(turns.Writer) ? humanColor : landedColor;
    }

    /// <summary>İsim değişince küçük, zar durunca büyük sıçrama (ikisinden büyüğü).</summary>
    void AnimateLabel()
    {
        float now = Time.unscaledTime;
        float tick = UiTween.Bump(now - tickAt, config.uiDiceTickSeconds, config.uiDiceTickBump);
        float land = UiTween.Bump(now - landedAt, config.uiDiceLandSeconds, config.uiDiceLandBump);
        float scale = Mathf.Max(tick, land);
        label.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Zar simgesi: zar atılırken hızla döner ve yavaşlar, durunca kısa süre ekranda kalır.</summary>
    void AnimateDie(TurnPhase phase)
    {
        if (die == null) return;

        bool lingering = Time.unscaledTime - landedAt < config.uiDiceLingerSeconds;
        bool visible = (phase == TurnPhase.Dice || lingering) && label.text.Length > 0;
        die.enabled = visible;
        if (!visible) return;

        float progress = phase == TurnPhase.Dice && config.diceSeconds > 0f
            ? Mathf.Clamp01(1f - turns.PhaseTimeLeft / config.diceSeconds)
            : 1f;
        float angle = Easing.OutCubic(progress) * config.uiDiceSpinTurns * 360f;
        die.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
        // Dönerken ui_die_roll kareleri (yoksa isimle değişen yüz), durunca yazı kutusundaki sol zarla aynı yüz.
        Sprite rollFrame = phase == TurnPhase.Dice
            ? UiSprites.FrameAt("ui_die_roll", Time.unscaledTime, 1f / Mathf.Max(1f, config.typeBoxDieRollFps), true)
            : null;
        int face = phase == TurnPhase.Dice ? Mathf.Max(0, lastIndex) % 6 + 1 : DieFaces.LandedFace(turns.TurnNumber, 0);
        die.sprite = rollFrame != null ? rollFrame : DieFaces.SpriteFor(face, config.uiDiceColor, config.uiDicePipColor);

        // Yazının solunda dursun (yazı genişliği isimle değişir).
        float half = Mathf.Min(label.preferredWidth, label.rectTransform.rect.width) * 0.5f;
        die.rectTransform.anchoredPosition = new Vector2(-(half + config.uiDiceSize * 0.75f), 0f);
    }

    void EnsureDie()
    {
        if (die != null) return;
        var go = new GameObject("Zar simgesi", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(label.rectTransform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(config.uiDiceSize, config.uiDiceSize);
        die = go.AddComponent<Image>();
        die.raycastTarget = false;
        die.preserveAspect = true;
        die.enabled = false;
    }
}
