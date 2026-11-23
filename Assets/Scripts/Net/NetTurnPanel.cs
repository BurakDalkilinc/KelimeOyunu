using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ denemesi için geçici tur arayüzü (IMGUI, piksel yazı tipiyle): üst ortada yazan oyuncu, süre ve kelime;
/// sağ üstte host için "Maçı başlat"; sıra sendeyse kelime kutusu.
/// Mevcut Arena arayüzü (zar animasyonu, yazı kutusu) arena ağa taşınınca bunun yerini alır.
/// </summary>
public class NetTurnPanel : MonoBehaviour
{
    [SerializeField] NetTurnChannel channel;

    /// <summary>Açıksa yalnızca maç öncesi lobi düğmesi çizilir; maç arayüzünü Canvas (Arena arayüzü) gösterir.</summary>
    [SerializeField] bool lobbyOnly;

    string wordText = "";
    GUIStyle bigStyle;
    GUIStyle smallStyle;

    void OnGUI()
    {
        var manager = NetworkManager.Singleton;
        if (channel == null || manager == null || !manager.IsListening || !channel.IsSpawned) return;

        EnsureStyles();
        NetTurnClient client = channel.Client;

        if (!channel.MatchActive) DrawBeforeMatch(manager);
        else if (!lobbyOnly) DrawBanner(client);
    }

    void EnsureStyles()
    {
        if (bigStyle != null) return;

        Font font = GameFont.Get();
        bigStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 28, alignment = TextAnchor.UpperCenter, wordWrap = true };
        bigStyle.normal.textColor = Color.white;
        smallStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, alignment = TextAnchor.UpperCenter, wordWrap = true };
        smallStyle.normal.textColor = new Color(0.85f, 0.88f, 1f);
    }

    void DrawBeforeMatch(NetworkManager manager)
    {
        GUILayout.BeginArea(new Rect(Screen.width - 312, 12, 300, 160), GUI.skin.box);
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
        GUILayout.EndArea();
    }

    void DrawBanner(NetTurnClient client)
    {
        float width = Mathf.Min(Screen.width - 24, 720);
        GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 10, width, 260));

        GUILayout.Label(HeadlineFor(client), bigStyle);
        GUILayout.Label($"Tur {client.TurnNumber} · {client.PhaseTimeLeft:0.0} sn", smallStyle);

        bool myTurnToType = client.Phase == TurnPhase.Typing && client.Writer != null && client.Writer.Id == channel.LocalPlayerId;
        if (myTurnToType) DrawTyping();

        if (!string.IsNullOrEmpty(channel.Feedback)) GUILayout.Label(channel.Feedback, smallStyle);

        if (client.Phase == TurnPhase.MatchOver) DrawMatchSummary(client);

        string targeting = TargetingText(client);
        if (targeting.Length > 0) GUILayout.Label(targeting, bigStyle);

        GUILayout.EndArea();
        DrawHealth();
    }

    /// <summary>Sol altta bu bilgisayardaki oyuncunun canı; elendiyse hayalet uyarısı.</summary>
    void DrawHealth()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return;

        NetworkPlayer mine = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
        if (mine == null) return;

        var style = new GUIStyle(bigStyle) { alignment = TextAnchor.LowerLeft };
        string text = mine.IsGhost ? "Elendin! Hayalet olarak izliyorsun." : "Can: " + mine.Health;
        GUI.Label(new Rect(12, Screen.height - 54, 520, 44), text, style);
    }

    /// <summary>Maç bitince: yazılan kelimeler (tek kişilik sahnedeki özetle aynı metin) ve host için "Yeni maç" düğmesi.</summary>
    void DrawMatchSummary(NetTurnClient client)
    {
        GUILayout.Label(MatchSummary.GetWordsText(client.PlayedWords), smallStyle);

        if (channel.CanRestart)
        {
            if (GUILayout.Button("Yeni maç için hazırla", GUILayout.Width(300))) channel.RestartMatch();
        }
        else
        {
            GUILayout.Label("Host'un yeni maçı başlatması bekleniyor...", smallStyle);
        }
    }

    /// <summary>Hedef seçilirken: seçen oyuncuya talimat, diğerlerine kimin seçtiği.</summary>
    string TargetingText(NetTurnClient client)
    {
        int writerId = channel.TargetingWriterId;
        if (writerId < 0) return "";

        if (channel.IAmChoosingTarget) return "Hedef seç! Bir oyuncuya tıkla (" + Mathf.CeilToInt(channel.TargetingTimeLeft) + " sn)";

        string name = client.Players.TryGetValue(writerId, out PlayerState writer) ? writer.Name : "Biri";
        return name + " hedef seçiyor...";
    }

    string HeadlineFor(NetTurnClient client)
    {
        string writer = client.Writer == null ? "" : client.Writer.Name
            + (client.Writer.Id == channel.LocalPlayerId ? " (SEN)" : "")
            + (client.Writer.IsBot && client.Writer.Id < NetTurnChannel.FirstBotId ? " (bot devraldı)" : ""); // Normal bot numarası FirstBotId ve üstü; altındaki bot, koparak botlaşan insandır

        switch (client.Phase)
        {
            case TurnPhase.Dice: return "Zar atılıyor... " + writer;
            case TurnPhase.Typing: return writer + " yazıyor";
            case TurnPhase.Result:
                return writer + ": " + (client.CurrentWord != null ? client.CurrentWord.display : "???");
            case TurnPhase.MatchOver:
                return client.Winner != null ? "Kazanan: " + client.Winner.Name : "Kazanan yok";
            default: return "";
        }
    }

    void DrawTyping()
    {
        GUILayout.Label("Bir kelime yaz ve Enter'a bas:", smallStyle);

        GUI.SetNextControlName("kelime");
        wordText = GUILayout.TextField(wordText, 30, GUILayout.Width(300));
        GUI.FocusControl("kelime");

        Event e = Event.current;
        bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

        if (GUILayout.Button("Gönder", GUILayout.Width(300)) || enter)
        {
            channel.SubmitWord(wordText);
            wordText = "";
            if (enter) e.Use();
        }
    }
}
