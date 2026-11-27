using UnityEngine;

/// <summary>
/// Yerel oyuncu vurulunca ekran kenarında kısa, sert bir kırmızı parlama (kalkan engellediyse camgöbeği); can 1'e düşünce kenar
/// kalp atışıyla yumuşak kırmızı nabız atar. Korku ipuçlarının kenar vinyetiyle (<see cref="DangerVignette"/>) aynı biçimde,
/// kameranın gördüğü alana çizilir. Ayarlarda ekran sarsıntısı kapalıysa parlamalar yumuşar. Yalnızca görüntü; zamanlama
/// <see cref="HitFeelDirector"/>'dedir, oraya kendiliğinden eklenir.
/// </summary>
public class HurtScreenFlash : MonoBehaviour
{
    static Texture2D horizontalFade;
    static Texture2D verticalFade;

    GameConfig config;

    void Awake() => config = GameConfigLoader.Get();

    void OnGUI()
    {
        if (config == null || Event.current.type != EventType.Repaint) return;

        float flashScale = SettingsService.ScreenFlashScale;
        float age = HitFeelDirector.LocalHitAge;
        float hit = config.hurtFlashSeconds > 0f && age < config.hurtFlashSeconds ? 1f - age / config.hurtFlashSeconds : 0f;
        hit *= hit; // Hızlı söner
        float pulse = HitFeelDirector.LowHealthPulse;

        float hitAlpha = hit * config.hurtFlashMaxAlpha * flashScale;
        float pulseAlpha = pulse * config.lowHealthPulseAlpha * flashScale;
        if (hitAlpha < 0.005f && pulseAlpha < 0.005f) return;

        EnsureTextures();
        Camera cam = Camera.main;
        Rect view = cam != null
            ? new Rect(cam.pixelRect.x, Screen.height - cam.pixelRect.yMax, cam.pixelRect.width, cam.pixelRect.height)
            : new Rect(0f, 0f, Screen.width, Screen.height);

        if (pulseAlpha >= 0.005f) DrawEdges(view, new Color(0.8f, 0.04f, 0.04f, pulseAlpha), config.dangerVignetteThickness * 1.4f);
        if (hitAlpha >= 0.005f)
        {
            Color tint = HitFeelDirector.LastLocalHitBlocked ? config.hitFlashBlockColor : config.hitFlashTintColor;
            tint.a = hitAlpha;
            DrawEdges(view, tint, config.hurtFlashThickness);
        }
    }

    /// <summary>Görüntü alanının dört kenarında içe doğru solan şerit.</summary>
    static void DrawEdges(Rect view, Color color, float thickness)
    {
        float t = Mathf.Min(view.width, view.height) * Mathf.Clamp01(thickness);
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.y, t, view.height), horizontalFade, new Rect(0f, 0f, 1f, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(view.xMax - t, view.y, t, view.height), horizontalFade, new Rect(1f, 0f, -1f, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.y, view.width, t), verticalFade, new Rect(0f, 1f, 1f, -1f));
        GUI.DrawTextureWithTexCoords(new Rect(view.x, view.yMax - t, view.width, t), verticalFade, new Rect(0f, 0f, 1f, 1f));
        GUI.color = old;
    }

    /// <summary>Solma şeritleri kodla bir kez üretilir (hazır asset yok).</summary>
    static void EnsureTextures()
    {
        if (horizontalFade != null && verticalFade != null) return;

        const int size = 64;
        horizontalFade = new Texture2D(size, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        verticalFade = new Texture2D(1, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        for (int i = 0; i < size; i++)
        {
            float a = Mathf.Pow(1f - i / (size - 1f), 1.6f);
            horizontalFade.SetPixel(i, 0, new Color(1f, 1f, 1f, a));
            verticalFade.SetPixel(0, i, new Color(1f, 1f, 1f, a));
        }
        horizontalFade.Apply();
        verticalFade.Apply();
    }
}
