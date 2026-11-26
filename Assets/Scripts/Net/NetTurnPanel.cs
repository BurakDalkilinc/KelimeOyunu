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

    /// <summary>Lobi isim listesinde bir satırın yüksekliği (piksel).</summary>
    const float LobbyNameRowHeight = 22f;

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
        var players = channel.LobbyPlayers;
        GUILayout.BeginArea(new Rect(Screen.width - 312, 12, 300, 190 + LobbyNameRowHeight * players.Count), GUI.skin.box);
        GUILayout.Label(Loc.T("net.not_started"));
        if (players.Count > 0) GUILayout.Label(Loc.T("net.lobby_players"));
        foreach (RosterEntry player in players)
            GUILayout.Label("- " + player.Name + (player.Id == channel.LocalPlayerId ? Loc.T("net.you_suffix") : ""));
        if (manager.IsHost)
        {
            int humans = channel.ConnectedHumans;
            int target = Mathf.Max(channel.TargetPlayers, humans);
            GUILayout.Label(Loc.F("net.player_count", target, humans, Mathf.Max(0, target - humans)));
            channel.TargetPlayers = Mathf.RoundToInt(GUILayout.HorizontalSlider(channel.TargetPlayers, RosterBuilder.MinPlayers, RosterBuilder.MaxPlayers));
            if (GUILayout.Button(Loc.T("net.start_match"))) channel.StartMatch();
        }
        else
        {
            GUILayout.Label(Loc.T("net.wait_host_start"));
        }
        GUILayout.EndArea();
    }

    void DrawBanner(NetTurnClient client)
    {
        float width = Mathf.Min(Screen.width - 24, 720);
        GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 10, width, 260));

        GUILayout.Label(HeadlineFor(client), bigStyle);
        GUILayout.Label(Loc.F("net.turn_line", client.TurnNumber, client.PhaseTimeLeft), smallStyle);

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
        string text = mine.IsGhost ? Loc.T("health.ghost_net") : Loc.F("health.value_net", mine.Health);
        GUI.Label(new Rect(12, Screen.height - 54, 520, 44), text, style);
    }

    /// <summary>Maç bitince: yazılan kelimeler (tek kişilik sahnedeki özetle aynı metin) ve host için "Yeni maç" düğmesi.</summary>
    void DrawMatchSummary(NetTurnClient client)
    {
        GUILayout.Label(MatchSummary.GetWordsText(client.PlayedWords), smallStyle);

        if (channel.CanRestart)
        {
            if (GUILayout.Button(Loc.T("net.prepare_new_match"), GUILayout.Width(300))) channel.RestartMatch();
        }
        else
        {
            GUILayout.Label(Loc.T("net.wait_host_new_match"), smallStyle);
        }
    }

    /// <summary>Hedef seçilirken: seçen oyuncuya talimat, diğerlerine kimin seçtiği.</summary>
    string TargetingText(NetTurnClient client)
    {
        int writerId = channel.TargetingWriterId;
        if (writerId < 0) return "";

        if (channel.IAmChoosingTarget) return Loc.F("target.choose", Mathf.CeilToInt(channel.TargetingTimeLeft));

        string name = client.Players.TryGetValue(writerId, out PlayerState writer) ? writer.Name : Loc.T("target.someone");
        return Loc.F("target.other_choosing", name);
    }

    string HeadlineFor(NetTurnClient client)
    {
        string writer = client.Writer == null ? "" : client.Writer.Name
            + (client.Writer.Id == channel.LocalPlayerId ? Loc.T("net.you_suffix") : "")
            + (client.Writer.IsBot && client.Writer.Id < NetTurnChannel.FirstBotId ? Loc.T("net.bot_took_over") : ""); // Normal bot numarası FirstBotId ve üstü; altındaki bot, koparak botlaşan insandır

        switch (client.Phase)
        {
            case TurnPhase.Dice: return Loc.F("net.headline_dice", writer);
            case TurnPhase.Typing: return Loc.F("net.headline_typing", writer);
            case TurnPhase.Result:
                return Loc.F("net.headline_result", writer, client.CurrentWord != null ? client.CurrentWord.LocalizedDisplay : "???");
            case TurnPhase.MatchOver:
                return MatchSummary.GetTitle(client.Winner);
            default: return "";
        }
    }

    void DrawTyping()
    {
        GUILayout.Label(Loc.T("net.type_prompt"), smallStyle);

        GUI.SetNextControlName("kelime");
        wordText = GUILayout.TextField(wordText, 30, GUILayout.Width(300));
        GUI.FocusControl("kelime");

        Event e = Event.current;
        bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

        if (GUILayout.Button(Loc.T("net.send"), GUILayout.Width(300)) || enter)
        {
            channel.SubmitWord(wordText);
            wordText = "";
            if (enter) e.Use();
        }
    }
}
