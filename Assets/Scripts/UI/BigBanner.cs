using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın ortasındaki büyük duyuru yazısı ("HAZIR!", "SIRA SENDE!", yazacak oyuncunun adı): küçükten taşarak girer,
/// bir süre durur (istenirse sarsılır), yukarı kayıp sönerek çıkar. Her Canvas'ta bir tane; ilk istendiğinde kodla kurulur
/// (<see cref="For"/>), sahneye elle eklemek gerekmez. Yalnızca görüntüdür; süreler ve renkler <see cref="GameConfig"/>'te.
/// </summary>
public class BigBanner : MonoBehaviour
{
    Text label;
    RectTransform rect;
    GameConfig config;
    float shownAt = -1000f;
    bool shake;
    bool active;

    /// <summary>Verilen arayüz nesnesinin Canvas'ındaki duyuru yazısı (yoksa kurulur). Canvas yoksa null.</summary>
    public static BigBanner For(Component anyUnderCanvas)
    {
        if (anyUnderCanvas == null) return null;
        Canvas canvas = anyUnderCanvas.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;

        BigBanner banner = canvas.GetComponentInChildren<BigBanner>(true);
        if (banner != null) return banner;

        var go = new GameObject("BigBanner", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        return go.AddComponent<BigBanner>();
    }

    void Awake()
    {
        config = GameConfigLoader.Get();
        rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1800f, config.uiBannerFontSize * 1.6f);
        rect.anchoredPosition = new Vector2(0f, config.uiBannerYOffset);

        label = gameObject.AddComponent<Text>();
        label.font = GameFont.Get();
        label.fontSize = config.uiBannerFontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = string.Empty;

        // Okunsun diye koyu dış çizgi.
        var outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(5f, -5f);

        transform.SetAsLastSibling(); // Diğer yazıların üstünde
    }

    /// <summary>
    /// Duyuruyu baştan oynatır. <paramref name="shaking"/> true ise dururken sarsılır ("SIRA SENDE!").
    /// <paramref name="delay"/> saniye sonra başlar (ör. sahne geçişinin açılması bitsin diye).
    /// </summary>
    public void Show(string text, Color color, bool shaking, float delay = 0f)
    {
        label.text = text;
        label.color = color;
        shake = shaking;
        shownAt = Time.unscaledTime + Mathf.Max(0f, delay);
        active = true;
        transform.SetAsLastSibling();
    }

    void LateUpdate()
    {
        if (!active) return;

        float t = Time.unscaledTime - shownAt;
        if (t < 0f)
        {
            Color hidden = label.color;
            label.color = new Color(hidden.r, hidden.g, hidden.b, 0f); // Gecikme sürüyor: henüz görünmez
            return;
        }
        float inEnd = config.uiBannerInSeconds;
        float holdEnd = inEnd + config.uiBannerHoldSeconds;
        float outEnd = holdEnd + config.uiBannerOutSeconds;

        float scale = UiTween.PunchIn(t, config.uiBannerInSeconds, config.uiBannerStartScale, config.uiBannerOvershoot);
        float alpha = UiTween.Fade(t, config.uiBannerInSeconds * 0.5f, true);
        Vector2 offset = Vector2.zero;

        if (shake && t < holdEnd)
        {
            offset.x = UiTween.Shake(t, holdEnd, config.uiBannerShakePixels, config.uiBannerShakeFrequency);
            offset.y = UiTween.Shake(t + 0.037f, holdEnd, config.uiBannerShakePixels * 0.6f, config.uiBannerShakeFrequency * 1.3f);
        }
        if (t >= holdEnd)
        {
            float u = t - holdEnd;
            offset.y += UiTween.SlideOut(u, config.uiBannerOutSeconds, config.uiBannerSlideDistance);
            alpha = UiTween.Fade(u, config.uiBannerOutSeconds, false);
        }
        if (t >= outEnd)
        {
            active = false;
            label.text = string.Empty;
        }

        rect.localScale = new Vector3(scale, scale, 1f);
        rect.anchoredPosition = new Vector2(0f, config.uiBannerYOffset) + offset;
        Color c = label.color;
        label.color = new Color(c.r, c.g, c.b, alpha);
    }
}
