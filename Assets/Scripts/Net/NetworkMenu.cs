using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

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
        var manager = NetworkManager.Singleton;
        if (manager == null) { GUILayout.Label("NetworkManager yok."); return; }

        GUILayout.BeginArea(new Rect(12, 12, 280, 220), GUI.skin.box);

        if (manager.IsListening) DrawConnected(manager);
        else DrawStart(manager);

        GUILayout.EndArea();
    }

    void DrawStart(NetworkManager manager)
    {
        GUILayout.Label("Çok oyunculu deneme");
        GUILayout.Label("Host'un IP adresi (aynı bilgisayarda boş bırak):");
        addressText = GUILayout.TextField(addressText);

        if (GUILayout.Button("Host (oyunu kur)")) Start(manager, true);
        if (GUILayout.Button("Katıl")) Start(manager, false);

        if (!string.IsNullOrEmpty(error)) GUILayout.Label(error);
    }

    void DrawConnected(NetworkManager manager)
    {
        if (manager.IsHost) GUILayout.Label($"Host: {manager.ConnectedClientsIds.Count} oyuncu bağlı");
        else if (manager.IsConnectedClient) GUILayout.Label("Host'a bağlandın.");
        else GUILayout.Label("Bağlanılıyor...");

        if (GUILayout.Button("Bağlantıyı kes")) manager.Shutdown();
    }

    void Start(NetworkManager manager, bool host)
    {
        if (!ConnectionSettings.TryParse(addressText, out string address, out ushort port, out error)) return;

        var transport = manager.GetComponent<UnityTransport>();
        if (transport == null) { error = "UnityTransport bulunamadı."; return; }

        if (host)
        {
            // Host herkesten (0.0.0.0) bağlantı kabul eder.
            transport.SetConnectionData(ConnectionSettings.DefaultAddress, port, "0.0.0.0");
            if (!manager.StartHost()) error = "Host başlatılamadı (port dolu olabilir).";
        }
        else
        {
            transport.SetConnectionData(address, port);
            if (!manager.StartClient()) error = "Bağlantı başlatılamadı.";
        }
    }
}
