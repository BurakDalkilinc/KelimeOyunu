using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menülerin "oyun gibi" düğmesi (IMGUI): kart çerçevesi (ui_card_frame, 9 dilim), üzerine gelince yayla büyür ve hafif tık,
/// basılıyken küçülür, tıklanınca tık sesi. Her ekran kendi örneğini tutar; düğmeler kimlikle (id) ayırt edilir.
/// Hareket hesabı <see cref="MenuButtonMotion"/>'dadır; ölçüler ve renkler <see cref="GameConfig"/>'te.
/// </summary>
public class ButtonPainter
{
    const string FrameSprite = "ui_card_frame";

    /// <summary>Düğme yüksekliğinin kaç pikseli çerçevenin bir resim pikseline denk gelsin (çerçeve kalınlığı için).</summary>
    const float FrameRowsPerPixel = 24f;

    readonly Dictionary<string, MenuButtonMotion> motions = new Dictionary<string, MenuButtonMotion>();
    readonly GameConfig config;
    GUIStyle frameStyle, labelStyle;
    bool stylesReady;
    int lastUpdateFrame = -1;
    readonly HashSet<string> updatedThisFrame = new HashSet<string>();

    public ButtonPainter(GameConfig config)
    {
        this.config = config;
    }

    /// <summary>
    /// Düğmeyi çizer; tıklandıysa true (tık sesi çalınmıştır). <paramref name="rect"/> çizimin o anki GUI matrisindedir.
    /// <paramref name="fontSize"/> 0 ise ayarlardaki menü yazı boyu kullanılır.
    /// </summary>
    public bool Draw(Rect rect, string text, string id, bool enabled = true, int fontSize = 0)
    {
        EnsureStyles();
        if (!motions.TryGetValue(id, out MenuButtonMotion motion))
        {
            motion = new MenuButtonMotion();
            motions[id] = motion;
        }

        Event e = Event.current;
        bool hovered = enabled && rect.Contains(e.mousePosition) && !UiInputBlockedByOverlay();
        Mouse mouse = Mouse.current;
        bool pressed = hovered && mouse != null && mouse.leftButton.isPressed;

        // Hareket her karede bir kez ilerler (OnGUI bir karede birkaç kez çağrılır).
        if (e.type == EventType.Repaint) Step(id, motion, hovered, pressed);

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none) && enabled;
        if (e.type == EventType.Repaint) Paint(rect, text, motion.Scale, hovered, enabled, fontSize);

        if (clicked) GameAudio.Click();
        return clicked;
    }

    void Step(string id, MenuButtonMotion motion, bool hovered, bool pressed)
    {
        if (Time.frameCount != lastUpdateFrame)
        {
            lastUpdateFrame = Time.frameCount;
            updatedThisFrame.Clear();
        }
        if (!updatedThisFrame.Add(id)) return;

        bool entered = motion.Update(hovered, pressed, Time.unscaledDeltaTime, config.uiMenuHoverScale, config.uiMenuPressScale,
                                     config.uiSpringStiffness, config.uiSpringDamping);
        if (entered && config.uiMenuHoverVolume > 0f) GameAudio.Play(SoundId.UiClick, config.uiMenuHoverVolume, 0.05f);
    }

    void Paint(Rect rect, string text, float scale, bool hovered, bool enabled, int fontSize)
    {
        Matrix4x4 old = GUI.matrix;
        Color oldColor = GUI.color;
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), rect.center);
        if (!enabled) GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * 0.45f);

        // Çerçeve pikseli düğme boyuna göre tam sayı katında (küçük düğmede kenar ince kalsın).
        float pixelScale = Mathf.Clamp(Mathf.Floor(rect.height / FrameRowsPerPixel), 1f, config.collectionFramePixelScale);
        UiDraw.Frame(rect, frameStyle, config.uiMenuButtonFallbackColor, pixelScale);
        labelStyle.fontSize = fontSize > 0 ? fontSize : config.uiMenuFontSize;
        labelStyle.normal.textColor = hovered ? config.uiMenuHoverTextColor : config.uiMenuTextColor;
        GUI.Label(rect, text, labelStyle);

        GUI.color = oldColor;
        GUI.matrix = old;
    }

    /// <summary>Ayarlar ya da defter açıkken alttaki düğmeler tepki vermesin.</summary>
    static bool UiInputBlockedByOverlay() => SettingsScreen.IsOpen || CollectionBookScreen.IsOpen;

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;
        frameStyle = UiDraw.NineSlice(FrameSprite, config.collectionFrameBorder);
        labelStyle = new GUIStyle(GUI.skin.label) { font = GameFont.Get(), alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
    }
}
