using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Yerel ağ deneme menüsü: "Host" (oyunu kur) ya da "Katıl" (host'un IP'sini yaz). Geçici geliştirici arayüzüdür;
/// Steam lobisi gelince yerini lobi ekranı alır. Adres çözümü <see cref="ConnectionSettings"/>'dedir.
/// </summary>
public class NetworkMenu : MonoBehaviour
{
    string addressText = ConnectionSettings.DefaultAddress;
    string error;

    void OnGUI()
    {
        if (CollectionBookScreen.IsOpen || SettingsScreen.IsOpen) return; // Defter/ayarlar açıkken ekranı onlar kaplar

        var manager = NetworkManager.Singleton;
        if (manager == null) { GUILayout.Label(Loc.T("netmenu.no_manager")); return; }

        // Sol alta: sol üst köşe Arena arayüzünün "Can" yazısına ait.
        float height = manager.IsListening ? 108 : 313;
        GUILayout.BeginArea(new Rect(12, Screen.height - height - 12, 280, height), GUI.skin.box);

        if (manager.IsListening) DrawConnected(manager);
        else DrawStart(manager);

        GUILayout.EndArea();
    }

    void DrawStart(NetworkManager manager)
    {
        GUILayout.Label(Loc.T("netmenu.title"));
        GUILayout.Label(Loc.T("netmenu.address"));
        addressText = GUILayout.TextField(addressText);

        if (Button(Loc.T("netmenu.host"))) StartNetwork(manager, true);
        if (Button(Loc.T("netmenu.join"))) StartNetwork(manager, false);
        if (Button(Loc.T("netmenu.solo"))) PlaySolo(manager);
        if (GUILayout.Button(Loc.T("collection.menu_button"))) CollectionBookScreen.Open(); // Defter kendi sayfa sesini çalar
        if (Button(SettingsText.MenuButton)) SettingsScreen.Open();

        if (!string.IsNullOrEmpty(error)) GUILayout.Label(error);
    }

    /// <summary>
    /// Tek kişilik maça (Arena) geçer. Ağ yöneticisi sahneler arası taşındığı için önce kapatılıp silinir; menüye dönülünce
    /// sahnedeki yenisi kullanılır.
    /// </summary>
    public const string SoloScene = "Arena";

    static void PlaySolo(NetworkManager manager)
    {
        if (manager != null)
        {
            if (manager.IsListening) manager.Shutdown();
            Destroy(manager.gameObject);
        }
        SceneManager.LoadScene(SoloScene);
    }

    void DrawConnected(NetworkManager manager)
    {
        if (manager.IsHost) GUILayout.Label(Loc.F("netmenu.host_count", manager.ConnectedClientsIds.Count));
        else if (manager.IsConnectedClient) GUILayout.Label(Loc.T("netmenu.connected"));
        else GUILayout.Label(Loc.T("netmenu.connecting"));

        if (Button(Loc.T("netmenu.disconnect"))) manager.Shutdown();
        if (Button(SettingsText.MenuButton)) SettingsScreen.Open();
    }

    /// <summary>Tık sesli düğme.</summary>
    static bool Button(string text)
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
}
