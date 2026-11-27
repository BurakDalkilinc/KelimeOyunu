using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Akış yazısının (durum satırı "X yazdı: Kurt", tehdit uyarısı) girip çıkışı: yeni bir konu gelince yazı küçükten taşarak
/// büyür (punch-in), yazı boşalınca son metin yukarı kayıp sönerek çıkar. Aynı konuda metin değişirse (geri sayım) animasyon
/// yeniden başlamaz. Yazıya kendiliğinden eklenir: <c>PunchText.Of(label).Set(metin, konu)</c>.
/// Hesaplar <see cref="UiTween"/>'dedir; süreler <see cref="GameConfig"/>'te.
/// </summary>
[RequireComponent(typeof(Text))]
public class PunchText : MonoBehaviour
{
    Text label;
    RectTransform rect;
    GameConfig config;
    Vector2 basePosition;
    string identity;
    bool showing;
    bool leaving;
    float changedAt;

    /// <summary>Yazının animasyon bileşeni (yoksa eklenir).</summary>
    public static PunchText Of(Text text)
    {
        if (text == null) return null;
        PunchText punch = text.GetComponent<PunchText>();
        return punch != null ? punch : text.gameObject.AddComponent<PunchText>();
    }

    void Awake()
    {
        label = GetComponent<Text>();
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;
        config = GameConfigLoader.Get();
    }

    /// <summary>
    /// Yazıyı ayarlar. <paramref name="topic"/> öncekinden farklıysa (ya da yazı gizliyse) içeri "punch" ile girer;
    /// metin boşsa çıkış animasyonu başlar (son metin kayarak kaybolur).
    /// </summary>
    public void Set(string text, string topic)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (showing && !leaving)
            {
                leaving = true;
                changedAt = Time.unscaledTime;
            }
            else if (!showing) label.text = string.Empty;
            identity = null;
            return;
        }

        if (!showing || leaving || topic != identity)
        {
            showing = true;
            leaving = false;
            changedAt = Time.unscaledTime;
            identity = topic;
        }
        label.text = text;
    }

    void LateUpdate()
    {
        float elapsed = Time.unscaledTime - changedAt;
        float scale = 1f, alpha = 1f, rise = 0f;

        if (leaving)
        {
            rise = UiTween.SlideOut(elapsed, config.uiTextSlideOutSeconds, config.uiTextSlideOutDistance);
            alpha = UiTween.Fade(elapsed, config.uiTextSlideOutSeconds, false);
            if (elapsed >= config.uiTextSlideOutSeconds)
            {
                leaving = false;
                showing = false;
                label.text = string.Empty;
                rise = 0f;
                alpha = 1f;
            }
        }
        else if (showing)
        {
            scale = UiTween.PunchIn(elapsed, config.uiTextPunchSeconds, config.uiTextPunchStartScale, config.uiTextPunchOvershoot);
            alpha = UiTween.Fade(elapsed, config.uiTextPunchSeconds * 0.5f, true);
        }

        rect.localScale = new Vector3(scale, scale, 1f);
        rect.anchoredPosition = basePosition + new Vector2(0f, rise);
        Color c = label.color;
        label.color = new Color(c.r, c.g, c.b, alpha);
    }
}
