using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Büyük tehdidi herkese gösterir (kural işletmez, host'tan gelen fotoğrafı çizer): çözüm alanları (renkli daire, simge, "Kule 2/3"),
/// ilerleyen dalga bandı, ekranın üstünde kırmızı alarm yazısı ve etki anında beyaz parlama.
/// Görünüm tek kişilik sahnedeki <see cref="ThreatDirector"/>, <see cref="SafeZoneView"/> ve <see cref="WaveView"/> ile aynıdır.
/// </summary>
public class NetThreatRenderer : MonoBehaviour
{
    [SerializeField] NetTurnChannel channel;
    [SerializeField] GameConfig config;

    /// <summary>İsteğe bağlı: alarm yazısı (Arena arayüzündeki ThreatBanner). Boşsa IMGUI ile çizilir.</summary>
    [SerializeField] Text banner;

    /// <summary>İsteğe bağlı: etki anı parlaması (Arena arayüzündeki ThreatFlash). Boşsa IMGUI ile çizilir.</summary>
    [SerializeField] Image flash;

    sealed class ZoneView
    {
        public GameObject Root;
        public TextMesh Label;
    }

    readonly Dictionary<int, ZoneView> zones = new Dictionary<int, ZoneView>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> toRemove = new List<int>();
    Transform waveBand;
    ArenaBounds arena;
    int appliedVersion = -1;
    int lastImpact;
    float flashAlpha;
    GUIStyle alarmStyle;
    Texture2D white;

    void Awake()
    {
        if (config != null) arena = new ArenaBounds(config.arenaSize);

        var go = new GameObject("Dalga");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square();
        sr.color = new Color(0.25f, 0.55f, 1f, 0.5f);
        sr.sortingOrder = 12;
        go.SetActive(false);
        waveBand = go.transform;

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
    }

    void OnDestroy()
    {
        foreach (ZoneView view in zones.Values)
            if (view.Root != null) Destroy(view.Root);
        zones.Clear();
        if (white != null) Destroy(white);
    }

    void Update()
    {
        if (channel == null) return;

        if (channel.ThreatVersion != appliedVersion)
        {
            appliedVersion = channel.ThreatVersion;
            ApplyZones(channel.LatestZones);
            ThreatStatus status = channel.LatestThreat;
            ApplyWave(status);

            if (status.ImpactCount > lastImpact) flashAlpha = 0.85f; // Etki anı: ekran parlar
            lastImpact = status.ImpactCount;
        }

        if (flashAlpha > 0f) flashAlpha = Mathf.Max(0f, flashAlpha - Time.deltaTime * 1.5f);

        if (flash != null) flash.color = new Color(1f, 1f, 1f, flashAlpha);
        if (banner != null) banner.text = AlarmText();
    }

    string AlarmText()
    {
        ThreatStatus status = channel.LatestThreat;
        if (string.IsNullOrEmpty(status.AlarmWordId)) return string.Empty;

        WordEntry word = channel.FindWord(status.AlarmWordId);
        return word == null ? string.Empty : ThreatText.GetAlarm(word, status.AlarmSeconds);
    }

    void ApplyZones(IReadOnlyList<ZoneSnapshot> snapshots)
    {
        seen.Clear();

        foreach (ZoneSnapshot z in snapshots)
        {
            seen.Add(z.Id);
            if (!zones.TryGetValue(z.Id, out ZoneView view))
            {
                view = BuildZone(z);
                zones[z.Id] = view;
            }

            view.Label.text = ThreatText.GetZoneLabel(z.Type, z.Occupied, z.Capacity);
        }

        toRemove.Clear();
        foreach (int id in zones.Keys)
            if (!seen.Contains(id)) toRemove.Add(id);
        foreach (int id in toRemove)
        {
            Destroy(zones[id].Root);
            zones.Remove(id);
        }
    }

    ZoneView BuildZone(ZoneSnapshot z)
    {
        var root = new GameObject("Çözüm: " + ThreatText.GetZoneName(z.Type));
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(z.X, z.Y, 0f);

        var body = new GameObject("Alan");
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = Vector3.one * (z.Radius * 2f);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle();
        sr.color = ThreatText.GetZoneColor(z.Type);
        sr.sortingOrder = 2;

        Sprite icon = SpriteLibrary.Get(z.Type);
        if (icon != null)
        {
            var iconObject = new GameObject("Simge");
            iconObject.transform.SetParent(root.transform, false);
            iconObject.transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Round(z.Radius));
            var iconRenderer = iconObject.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = icon;
            iconRenderer.sortingOrder = 3;
        }

        Font font = GameFont.Get();
        var labelObject = new GameObject("Etiket");
        labelObject.transform.SetParent(root.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, z.Radius + 0.4f, 0f);
        var label = labelObject.AddComponent<TextMesh>();
        label.font = font;
        label.fontSize = 64;
        label.characterSize = 0.1f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = 20;

        return new ZoneView { Root = root, Label = label };
    }

    /// <summary>Dalga bandını ön yüzün arkasına koyar: ilerleme yönünde Thickness kalınlığında, diğer yönde arenayı boydan boya kaplar.</summary>
    void ApplyWave(ThreatStatus status)
    {
        if (!status.WaveActive || arena == null)
        {
            waveBand.gameObject.SetActive(false);
            return;
        }

        waveBand.gameObject.SetActive(true);
        Vector2 direction = new Vector2(status.WaveDirX, status.WaveDirY);
        bool horizontal = Mathf.Abs(direction.x) > 0.5f; // Sağa/sola ilerleyen dalga dikey bir bant olur.

        float length = (horizontal ? arena.HalfSize.y : arena.HalfSize.x) * 2f + 2f;
        waveBand.position = direction * (status.WaveFront - WaveEffect.Thickness * 0.5f);
        waveBand.localScale = horizontal
            ? new Vector3(WaveEffect.Thickness, length, 1f)
            : new Vector3(length, WaveEffect.Thickness, 1f);
    }

    void OnGUI()
    {
        // uGUI afiş ve parlama atanmışsa onlar gösterir.
        if (channel == null || (banner != null && flash != null)) return;

        if (flashAlpha > 0f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = previous;
        }

        ThreatStatus status = channel.LatestThreat;
        if (string.IsNullOrEmpty(status.AlarmWordId)) return;

        WordEntry word = channel.FindWord(status.AlarmWordId);
        if (word == null) return;

        if (alarmStyle == null)
        {
            alarmStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Get(), fontSize = 30, alignment = TextAnchor.UpperCenter, wordWrap = true };
            alarmStyle.normal.textColor = new Color(1f, 0.3f, 0.25f);
        }

        GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 80), ThreatText.GetAlarm(word, status.AlarmSeconds), alarmStyle);
    }
}
