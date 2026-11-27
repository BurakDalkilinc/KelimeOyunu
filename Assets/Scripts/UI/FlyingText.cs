using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kabul edilen kelimenin kutudan yukarı uçuşu: kaynak yazının kopyası yukarı kayar, biraz büyür ve söner, sonra silinir.
/// Kopya Canvas'ın doğrudan çocuğudur; böylece yazı kutusu kapansa da uçuş sürer. Yalnızca görüntü.
/// </summary>
public class FlyingText : MonoBehaviour
{
    RectTransform rect;
    Text label;
    Vector2 start;
    float startedAt;
    float duration;
    float distance;
    Color color;

    /// <summary><paramref name="source"/> yazısının bulunduğu yerden <paramref name="text"/>'i yukarı uçurur.</summary>
    public static void Launch(Text source, string text, Color color, float duration, float distance)
    {
        if (source == null || string.IsNullOrEmpty(text)) return;
        Canvas canvas = source.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;

        var go = new GameObject("Uçan kelime", typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(canvas.transform, false);
        r.position = source.rectTransform.TransformPoint(source.rectTransform.rect.center);
        r.sizeDelta = source.rectTransform.rect.size;
        r.SetAsLastSibling();

        var label = go.AddComponent<Text>();
        label.font = source.font;
        label.fontSize = source.fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = text;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        var fly = go.AddComponent<FlyingText>();
        fly.rect = r;
        fly.label = label;
        fly.start = r.anchoredPosition;
        fly.startedAt = Time.unscaledTime;
        fly.duration = Mathf.Max(0.01f, duration);
        fly.distance = distance;
        fly.color = color;
        fly.LateUpdate();
    }

    void LateUpdate()
    {
        float t = (Time.unscaledTime - startedAt) / duration;
        if (t >= 1f) { Destroy(gameObject); return; }

        rect.anchoredPosition = start + new Vector2(0f, Easing.OutCubic(t) * distance);
        float scale = 1f + 0.3f * Easing.OutCubic(t);
        rect.localScale = new Vector3(scale, scale, 1f);
        label.color = new Color(color.r, color.g, color.b, color.a * (1f - Easing.InCubic(t)));
    }
}
