using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ denemesi için geçici tur paneli (IMGUI): host'ta "Maçı başlat", herkeste tur durumu, sıra sendeyse kelime kutusu.
/// Gerçek arayüz (zar animasyonu, yazı kutusu) arena ağa taşınınca mevcut UI'ın yerini alır.
/// </summary>
public class NetTurnPanel : MonoBehaviour
{
    [SerializeField] NetTurnChannel channel;

    string wordText = "";

    void OnGUI()
    {
        var manager = NetworkManager.Singleton;
        if (channel == null || manager == null || !manager.IsListening || !channel.IsSpawned) return;

        GUILayout.BeginArea(new Rect(Screen.width - 312, 12, 300, 300), GUI.skin.box);

        NetTurnClient client = channel.Client;
        if (client == null) DrawBeforeMatch(manager);
        else DrawMatch(client);

        GUILayout.EndArea();
    }

    void DrawBeforeMatch(NetworkManager manager)
    {
        GUILayout.Label("Maç henüz başlamadı.");
        if (manager.IsHost)
        {
            GUILayout.Label("Herkes bağlanınca başlat. Eksik yerler bot olur.");
            if (GUILayout.Button("Maçı başlat")) channel.StartMatch();
        }
        else
        {
            GUILayout.Label("Host'un maçı başlatması bekleniyor...");
        }
    }

    void DrawMatch(NetTurnClient client)
    {
        GUILayout.Label($"Tur {client.TurnNumber} · {PhaseName(client.Phase)} · {client.PhaseTimeLeft:0.0} sn");

        if (client.Writer != null)
            GUILayout.Label("Yazan: " + client.Writer.Name + (client.Writer.Id == channel.LocalPlayerId ? " (SEN)" : ""));

        if (client.Phase == TurnPhase.Result)
            GUILayout.Label("Kelime: " + (client.CurrentWord != null ? client.CurrentWord.display : "???"));

        if (client.Phase == TurnPhase.MatchOver)
            GUILayout.Label(client.Winner != null ? "Kazanan: " + client.Winner.Name : "Kazanan yok");

        bool myTurnToType = client.Phase == TurnPhase.Typing && client.Writer != null && client.Writer.Id == channel.LocalPlayerId;
        if (myTurnToType) DrawTyping();

        if (!string.IsNullOrEmpty(channel.Feedback)) GUILayout.Label(channel.Feedback);
    }

    void DrawTyping()
    {
        GUILayout.Label("Bir kelime yaz:");

        GUI.SetNextControlName("kelime");
        wordText = GUILayout.TextField(wordText, 30);

        Event e = Event.current;
        bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && GUI.GetNameOfFocusedControl() == "kelime";

        if (GUILayout.Button("Gönder") || enter)
        {
            channel.SubmitWord(wordText);
            wordText = "";
            if (enter) e.Use();
        }
    }

    static string PhaseName(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.Dice: return "Zar";
            case TurnPhase.Typing: return "Yazma";
            case TurnPhase.Result: return "Sonuç";
            case TurnPhase.MatchOver: return "Maç bitti";
            default: return "Başlamadı";
        }
    }
}
