using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Korku ipuçlarını ekrana çizer: yerel oyuncuya yakın tehlikeli yaratık varsa ekran kenarı kırmızı nabız atar; ekran dışından
/// seni kovalayan yaratık için ekran kenarında kırmızı ok çıkar. Yalnızca görüntü; hesap <see cref="DangerSense"/>'te.
/// Sahneye elle eklenmez: tek kişilikte <see cref="CreatureSpawner"/>, ağda <see cref="NetCreatureRenderer"/> kendine ekler.
/// </summary>
public class DangerVignette : MonoBehaviour
{
    /// <summary>Okun ve kenar ölçülerinin ayarlandığı ekran yüksekliği (piksel).</summary>
    const float ReferenceHeight = 1080f;

    /// <summary>Vinyetin yeni değere yaklaşma hızı (büyük = sert).</summary>
    const float Smoothing = 10f;

    static Texture2D horizontalFade;
    static Texture2D verticalFade;
    static Texture2D arrowTexture;

    readonly List<SensedCreature> creatures = new List<SensedCreature>();

    Func<Vector2?> localPosition;
    Func<int> localPlayerId;
    Action<List<SensedCreature>> collect;
    GameConfig config;
    float intensity;
    Vector2? local;

    /// <summary>
    /// <paramref name="host"/>'a ekler (varsa günceller). <paramref name="position"/> yerel oyuncunun yeri (ölü/hayaletse null),
    /// <paramref name="playerId"/> onun kimliği, <paramref name="fill"/> yaratıkları verilen listeye doldurur.
    /// </summary>
    public static DangerVignette Attach(GameObject host, Func<Vector2?> position, Func<int> playerId, Action<List<SensedCreature>> fill)
    {
        var vignette = host.GetComponent<DangerVignette>();
        if (vignette == null) vignette = host.AddComponent<DangerVignette>();
        vignette.localPosition = position;
        vignette.localPlayerId = playerId;
        vignette.collect = fill;
        vignette.config = GameConfigLoader.Get();
        return vignette;
    }

    void Update()
    {
        if (config == null || collect == null) return;

        creatures.Clear();
        collect(creatures);
        local = localPosition?.Invoke();

        float wanted = local.HasValue ? DangerSense.Intensity(local.Value, creatures, config.dangerVignetteRange) : 0f;
        intensity = Mathf.Lerp(intensity, wanted, 1f - Mathf.Exp(-Smoothing * Time.deltaTime));
    }

    void OnGUI()
    {
        if (config == null || Event.current.type != EventType.Repaint) return;
        EnsureTextures();

        // Piksel kamera görüntüyü siyah kenarlarla (letterbox) ortalayabilir: çizim kameranın gördüğü alana yapılır.
        Camera cam = Camera.main;
        Rect view = cam != null
            ? new Rect(cam.pixelRect.x, Screen.height - cam.pixelRect.yMax, cam.pixelRect.width, cam.pixelRect.height)
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float scale = view.height / ReferenceHeight;
        if (intensity > 0.01f) DrawVignette(view, intensity * config.dangerVignetteMaxAlpha * DangerSense.Pulse(Time.time, config.dangerVignettePulseHz));
        if (local.HasValue && cam != null) DrawArrows(cam, view, scale);
    }

    /// <summary>Görüntü alanının dört kenarında içe doğru solan kırmızı şerit.</summary>
    void DrawVignette(Rect view, float alpha)
    {
        float t = Mathf.Min(view.width, view.height) * config.dangerVignetteThickness;

        Color old = GUI.color;
        GUI.color = new Color(0.85f, 0.05f, 0.05f, Mathf.Clamp01(alpha));
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.y, t, view.height), horizontalFade, new Rect(0f, 0f, 1f, 1f));            // Sol
        GUI.DrawTextureWithTexCoords(new Rect(view.xMax - t, view.y, t, view.height), horizontalFade, new Rect(1f, 0f, -1f, 1f));    // Sağ
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.y, view.width, t), verticalFade, new Rect(0f, 1f, 1f, -1f));              // Üst
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.yMax - t, view.width, t), verticalFade, new Rect(0f, 0f, 1f, 1f));        // Alt
        GUI.color = old;
    }

    /// <summary>Ekran dışından yerel oyuncuyu kovalayan zararlı yaratıklar için kenarda nabız atan kırmızı ok.</summary>
    void DrawArrows(Camera cam, Rect view, float scale)
    {
        if (localPlayerId == null) return;

        int me = localPlayerId();
        float size = config.dangerArrowSize * scale;
        float inset = config.dangerArrowEdgeMargin * scale + size * 0.5f;
        Vector2 margin = new Vector2(inset / Mathf.Max(1f, view.width), inset / Mathf.Max(1f, view.height));

        Color old = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;
        foreach (SensedCreature creature in creatures)
        {
            if (!creature.Harmful || creature.TargetId != me) continue;

            Vector3 viewport = cam.WorldToViewportPoint(creature.Position);
            if (!DangerSense.TryEdgeArrow(viewport, margin, out Vector2 edge, out float angle)) continue;

            var at = new Vector2(view.x + edge.x * view.width, view.y + (1f - edge.y) * view.height); // GUI'de y aşağı doğru
            float alpha = creature.PounceWindup ? 1f : 0.6f + 0.4f * DangerSense.Pulse(Time.time, config.dangerVignettePulseHz);
            GUI.color = new Color(1f, 0.15f, 0.1f, alpha);
            GUI.matrix = oldMatrix;
            GUIUtility.RotateAroundPivot(-angle, at); // GUI'de saat yönü pozitif
            GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), arrowTexture);
        }
        GUI.matrix = oldMatrix;
        GUI.color = old;
    }

    /// <summary>Solma şeritleri ve ok resmi kodla bir kez üretilir (hazır asset yok; prototip).</summary>
    static void EnsureTextures()
    {
        if (horizontalFade != null && verticalFade != null && arrowTexture != null) return;

        const int fadeSize = 64;
        horizontalFade = new Texture2D(fadeSize, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        verticalFade = new Texture2D(1, fadeSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        for (int i = 0; i < fadeSize; i++)
        {
            float a = Mathf.Pow(1f - i / (fadeSize - 1f), 2f); // Kenarda koyu, içe doğru solar
            horizontalFade.SetPixel(i, 0, new Color(1f, 1f, 1f, a));
            verticalFade.SetPixel(0, i, new Color(1f, 1f, 1f, a));
        }
        horizontalFade.Apply();
        verticalFade.Apply();

        // Sağa bakan dolu üçgen (piksel görünüm için nokta filtre).
        const int arrowSize = 16;
        arrowTexture = new Texture2D(arrowSize, arrowSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave,
        };
        for (int y = 0; y < arrowSize; y++)
        {
            for (int x = 0; x < arrowSize; x++)
            {
                float halfHeight = (arrowSize - 1 - x) * 0.5f;
                bool filled = Mathf.Abs(y - (arrowSize - 1) * 0.5f) <= halfHeight;
                arrowTexture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
        }
        arrowTexture.Apply();
    }
}
