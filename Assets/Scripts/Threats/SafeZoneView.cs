using UnityEngine;

/// <summary>
/// Bir çözüm alanını ekranda gösterir: renkli yarı saydam daire ve üstünde "Sığınak 2/4" gibi doluluk yazısı.
/// Kimin korunduğu <see cref="SafeZone"/>'dadır; burası yalnızca görüntü.
/// </summary>
public class SafeZoneView : MonoBehaviour
{
    SafeZone zone;
    TextMesh label;

    /// <summary>Alanı kurar ve çizer.</summary>
    public void Initialize(SafeZone zone)
    {
        this.zone = zone;
        transform.position = zone.Position;

        var body = new GameObject("Alan");
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * (zone.Radius * 2f);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = ThreatText.GetZoneColor(zone.Type);
        sr.sortingOrder = 2;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var labelObject = new GameObject("Etiket");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, zone.Radius + 0.4f, 0f);

        label = labelObject.AddComponent<TextMesh>();
        label.font = font;
        label.fontSize = 64;
        label.characterSize = 0.1f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = 20;

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (zone == null || label == null) return;
        label.text = ThreatText.GetZoneLabel(zone);
    }
}
