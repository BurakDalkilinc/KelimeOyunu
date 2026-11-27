using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ana menü (IMGUI): hareketli arka plan, WordRoyale piksel logosu ve ortada dikey düğme listesi
/// (Tek kişilik oyna / Çok oyunculu / Defter / Ayarlar / Çıkış). "Çok oyunculu" sayfasında yerel ağ host/katıl düğmeleri,
/// katlanır "Gelişmiş" bölümünde host IP alanı ve yanda Steam lobisi kutusu (<c>SteamLobbyMenu</c>) durur.
/// Ağ başladıktan sonra yalnızca köşede küçük bağlantı kutusu kalır. Adres çözümü <see cref="ConnectionSettings"/>'dedir.
/// </summary>
public class NetworkMenu : MonoBehaviour
{
    enum Page { Main, Multiplayer }

    const string LogoSprite = "ui_logo";
    const float ReferenceHeight = 1080f;

    static NetworkMenu active;
    static Rect onlinePanelRect;

    string addressText = ConnectionSettings.DefaultAddress;
    string error;
    Page page = Page.Main;
    bool advancedOpen;

    GameConfig config;
    ButtonPainter buttons;
    MenuBackdropPainter backdrop;
    GUIStyle smallStyle, fieldStyle;

    /// <summary>Steam lobisi kutusu şu an çizilmeli mi (çok oyunculu sayfası açık, ağ başlamamış, üstte başka ekran yok)?</summary>
    public static bool OnlinePanelVisible =>
        active != null && active.isActiveAndEnabled && active.page == Page.Multiplayer
        && !CollectionBookScreen.IsOpen && !SettingsScreen.IsOpen
        && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening);

    /// <summary>Steam lobisi kutusunun yeri (ekran pikseli; yüksekliği kutunun kendisi belirler).</summary>
    public static Rect OnlinePanelRect => onlinePanelRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => active = null;

    void OnEnable() => active = this;

    void OnDisable()
    {
        if (active == this) active = null;
    }

    void OnGUI()
    {
        GUI.depth = 10; // Steam kutusu, ayarlar ve defter bunun üstünde çizilir
        if (config == null)
        {
            config = GameConfigLoader.Get();
            buttons = new ButtonPainter(config);
            backdrop = new MenuBackdropPainter(config);
        }

        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
        {
            DrawConnectedBox(manager);
            return;
        }

        backdrop.Draw(Time.unscaledTime);
        if (CollectionBookScreen.IsOpen || SettingsScreen.IsOpen) return; // Defter/ayarlar açıkken ekranı onlar kaplar

        EnsureStyles();
        float logoBottom = DrawLogo();

        float scale = Screen.height / ReferenceHeight;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float top = logoBottom / scale + config.uiMenuButtonSpacing * 3f;

        if (page == Page.Main) DrawMainPage(width, top);
        else DrawMultiplayerPage(manager, width, top, scale);

        GUI.matrix = old;
    }

    // ---- Logo ----

    /// <summary>Logoyu tam sayı katında büyütüp üst ortaya çizer (piksel net kalsın); alt kenarını (ekran pikseli) döndürür.</summary>
    float DrawLogo()
    {
        Sprite logo = SpriteLibrary.Get(LogoSprite);
        float top = Screen.height * config.uiLogoTopRatio;
        if (logo == null) return top;

        Texture2D texture = logo.texture;
        int factor = Mathf.Max(1, Mathf.RoundToInt(Screen.height * config.uiLogoHeightRatio / texture.height));
        float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * config.uiLogoBobSpeed * 2f * Mathf.PI) * config.uiLogoBobPixels) * factor;
        float w = texture.width * factor, h = texture.height * factor;
        Rect rect = new Rect(Mathf.Round((Screen.width - w) * 0.5f), Mathf.Round(top + bob), w, h);
        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(rect, texture);
        return top + h;
    }

    // ---- Sayfalar ----

    void DrawMainPage(float width, float top)
    {
        float x = (width - config.uiMenuButtonWidth) * 0.5f;
        float y = top;

        if (Button(x, ref y, Loc.T("menu.solo"), "solo")) PlaySolo(NetworkManager.Singleton);
        if (Button(x, ref y, Loc.T("menu.multiplayer"), "multi")) { page = Page.Multiplayer; error = null; }
        if (Button(x, ref y, Loc.T("menu.book"), "book")) CollectionBookScreen.Open();
        if (Button(x, ref y, SettingsText.MenuButton, "settings")) SettingsScreen.Open();
        if (Button(x, ref y, Loc.T("menu.quit"), "quit")) Quit();
    }

    void DrawMultiplayerPage(NetworkManager manager, float width, float top, float scale)
    {
        float gap = config.uiMenuButtonSpacing * 2f;
        float total = config.uiMenuButtonWidth + gap + config.uiOnlinePanelWidth;
        float x = Mathf.Max(config.uiMenuButtonSpacing, (width - total) * 0.5f);
        float y = top;

        // Steam kutusu düğme sütununun sağında (ekran pikseline çevrilir).
        onlinePanelRect = new Rect((x + config.uiMenuButtonWidth + gap) * scale, top * scale, config.uiOnlinePanelWidth * scale, 0f);

        if (manager == null)
        {
            GUI.Label(new Rect(x, y, config.uiMenuButtonWidth, config.uiMenuButtonHeight), Loc.T("netmenu.no_manager"), smallStyle);
            y += config.uiMenuButtonHeight;
        }
        else
        {
            if (Button(x, ref y, Loc.T("menu.lan_host"), "host")) StartNetwork(manager, true);
            if (Button(x, ref y, Loc.T("menu.lan_join"), "join")) StartNetwork(manager, false);
        }

        if (Button(x, ref y, Loc.T(advancedOpen ? "menu.advanced_open" : "menu.advanced_closed"), "advanced", config.uiMenuSmallFontSize))
            advancedOpen = !advancedOpen;

        if (advancedOpen)
        {
            float line = config.uiMenuSmallFontSize * 1.6f;
            GUI.Label(new Rect(x, y, config.uiMenuButtonWidth, line * 1.6f), Loc.T("netmenu.address"), smallStyle);
            y += line * 1.6f;
            addressText = GUI.TextField(new Rect(x, y, config.uiMenuButtonWidth, line), addressText, fieldStyle);
            y += line + config.uiMenuButtonSpacing;
        }

        if (Button(x, ref y, Loc.T("menu.back"), "back")) { page = Page.Main; error = null; }

        if (!string.IsNullOrEmpty(error))
            GUI.Label(new Rect(x, y, config.uiMenuButtonWidth, config.uiMenuButtonHeight * 1.5f), error, smallStyle);
    }

    /// <summary>Sütunda bir düğme çizer ve <paramref name="y"/>'yi bir sonraki satıra ilerletir.</summary>
    bool Button(float x, ref float y, string text, string id, int fontSize = 0)
    {
        Rect rect = new Rect(x, y, config.uiMenuButtonWidth, config.uiMenuButtonHeight);
        y += config.uiMenuButtonHeight + config.uiMenuButtonSpacing;
        return buttons.Draw(rect, text, id, true, fontSize);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Tek kişilik maça (Arena) geçer. Ağ yöneticisi sahneler arası taşındığı için önce kapatılıp silinir; menüye dönülünce
    /// sahnedeki yenisi kullanılır.
    /// </summary>
    public const string SoloScene = "Arena";

    static void PlaySolo(NetworkManager manager)
    {
        ScreenFader.Run(() =>
        {
            if (manager != null)
            {
                if (manager.IsListening) manager.Shutdown();
                Destroy(manager.gameObject);
            }
            SceneManager.LoadScene(SoloScene);
        });
    }

    // ---- Ağ başladıktan sonra ----

    /// <summary>Köşede küçük bağlantı kutusu: kaç kişi bağlı, bağlantıyı kes, ayarlar.</summary>
    void DrawConnectedBox(NetworkManager manager)
    {
        if (CollectionBookScreen.IsOpen || SettingsScreen.IsOpen) return;

        // Sol alta: sol üst köşe Arena arayüzünün "Can" yazısına ait.
        const float height = 108f;
        GUILayout.BeginArea(new Rect(12, Screen.height - height - 12, 280, height), GUI.skin.box);

        if (manager.IsHost) GUILayout.Label(Loc.F("netmenu.host_count", manager.ConnectedClientsIds.Count));
        else if (manager.IsConnectedClient) GUILayout.Label(Loc.T("netmenu.connected"));
        else GUILayout.Label(Loc.T("netmenu.connecting"));

        if (LayoutButton(Loc.T("netmenu.disconnect"))) manager.Shutdown();
        if (LayoutButton(SettingsText.MenuButton)) SettingsScreen.Open();

        GUILayout.EndArea();
    }

    /// <summary>Tık sesli düz düğme (maç içi küçük kutu için).</summary>
    static bool LayoutButton(string text)
    {
        if (!GUILayout.Button(text)) return false;
        GameAudio.Click();
        return true;
    }

    void StartNetwork(NetworkManager manager, bool host)
    {
        if (!ConnectionSettings.TryParse(addressText, out string address, out ushort port, out error)) return;

        var transport = manager.GetComponent<UnityTransport>();
        if (transport == null) { error = Loc.T("netmenu.no_transport"); return; }

        // Steam lobisi aktarım katmanını değiştirmiş olabilir; yerel ağ için yeniden UnityTransport'a dön.
        manager.NetworkConfig.NetworkTransport = transport;

        if (host)
        {
            // Host herkesten (0.0.0.0) bağlantı kabul eder.
            transport.SetConnectionData(ConnectionSettings.DefaultAddress, port, "0.0.0.0");
            if (!manager.StartHost()) error = Loc.T("netmenu.host_failed");
        }
        else
        {
            transport.SetConnectionData(address, port);
            if (!manager.StartClient()) error = Loc.T("netmenu.client_failed");
        }
    }

    void EnsureStyles()
    {
        if (smallStyle != null) return;
        Font font = GameFont.Get();
        smallStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = config.uiMenuSmallFontSize, wordWrap = true, alignment = TextAnchor.UpperLeft };
        smallStyle.normal.textColor = Color.white;
        fieldStyle = new GUIStyle(GUI.skin.textField) { font = font, fontSize = config.uiMenuSmallFontSize, alignment = TextAnchor.MiddleLeft };
    }
}
