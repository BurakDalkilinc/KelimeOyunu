using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Steam lobisi (geçici IMGUI arayüz): "Steam'e bağlan", "Lobi kur" (host olur, arkadaşlar davet edilebilir),
/// "Lobileri bul" ve listeden "Katıl". Lobi bulma/katılma Steam'in lobi sistemiyle, oyun verisi Steam Relay üzerinden
/// <see cref="FacepunchTransport"/> ile gider; IP ya da port açmak gerekmez.
/// Test için Spacewar uygulama kimliği (480) kullanılır; herkesin paylaştığı bu kimlikte yalnızca bu oyunun lobileri
/// "game" anahtarıyla süzülür. Maçın kendisi (tur, hareket, yaratıklar) ağ katmanında aynen çalışır.
/// </summary>
public class SteamLobbyMenu : MonoBehaviour
{
    const string GameKey = "game";
    const string GameValue = "KelimeBattleRoyale";
    const int MaxMembers = NetTurnChannel.MaxPlayers;

    [SerializeField] FacepunchTransport steamTransport;
    [SerializeField] uint appId = 480;

    readonly List<Lobby> found = new List<Lobby>();
    Lobby? current;
    // Durum yazısı anahtar + değerler olarak tutulur; ekrana her çizimde seçili dilde basılır.
    string statusKey = "steam.not_connected";
    object[] statusArgs = System.Array.Empty<object>();
    bool busy;
    bool friendsOnly = true;
    bool subscribed;

    void OnDisable()
    {
        if (subscribed) SteamFriends.OnGameLobbyJoinRequested -= OnJoinRequested;
        subscribed = false;
    }

    void Update()
    {
        // Ağ başlamadan da lobi geri çağrıları işlensin (ağ başlayınca aktarım katmanı da çağırır; iki kez zararsız).
        if (SteamClient.IsValid) SteamClient.RunCallbacks();
    }

    void OnGUI()
    {
        if (CollectionBookScreen.IsOpen || SettingsScreen.IsOpen) return; // Defter/ayarlar açıkken ekranı onlar kaplar

        var manager = NetworkManager.Singleton;
        if (manager == null) return;

        // Yerel ağda (Steam lobisi olmadan) host/istemciyken Steam kutusu gereksiz.
        if (manager.IsListening && !current.HasValue) return;

        // Ağ başlamadan önce kutu yalnızca ana menünün "Çok oyunculu" sayfasında, düğmelerin yanında durur.
        if (!manager.IsListening && !NetworkMenu.OnlinePanelVisible) return;

        float height = manager.IsListening ? 130f : (SteamClient.IsValid ? 270f + 26f * found.Count : 120f);
        Rect area = new Rect(304, Screen.height - height - 12, 320, height);
        if (!manager.IsListening)
        {
            Rect place = NetworkMenu.OnlinePanelRect;
            area = new Rect(place.x, place.y, place.width, height);
        }
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label(Loc.T("steam.title"));

        if (!SteamClient.IsValid) DrawConnect();
        else if (manager.IsListening) DrawInLobby(manager);
        else DrawLobbyChoices(manager);

        GUILayout.Label(Loc.F(statusKey, statusArgs));
        GUILayout.EndArea();
    }

    // ---- Arayüz ----

    void DrawConnect()
    {
        GUI.enabled = !busy;
        if (Button(Loc.T("steam.connect"))) Connect();
        GUI.enabled = true;
        GUILayout.Label(Loc.T("steam.client_hint"));
    }

    void DrawLobbyChoices(NetworkManager manager)
    {
        GUI.enabled = !busy;
        friendsOnly = GUILayout.Toggle(friendsOnly, Loc.T("steam.friends_only"));
        if (Button(Loc.T("steam.create_lobby"))) CreateLobbyAndHost(manager);
        if (Button(Loc.T("steam.find_lobbies"))) RefreshList();

        foreach (Lobby lobby in found)
        {
            string owner = new Friend(lobby.Owner.Id).Name;
            if (Button(Loc.F("steam.join_lobby", owner, lobby.MemberCount, lobby.MaxMembers))) JoinLobby(manager, lobby);
        }
        GUI.enabled = true;
    }

    void DrawInLobby(NetworkManager manager)
    {
        if (current.HasValue)
        {
            GUILayout.Label(Loc.F("steam.lobby_members", current.Value.MemberCount, current.Value.MaxMembers));
            if (manager.IsHost && Button(Loc.T("steam.invite"))) SteamFriends.OpenGameInviteOverlay(current.Value.Id);
        }
        if (Button(Loc.T("steam.leave"))) Leave(manager);
    }

    /// <summary>Tık sesli düğme. ("Ayarlar" düğmesi yandaki NetworkMenu kutusunda; burada ikincisi olmasın.)</summary>
    static bool Button(string text)
    {
        if (!GUILayout.Button(text)) return false;
        GameAudio.Click();
        return true;
    }

    /// <summary>Durum satırını değiştirir (anahtar ve yer tutucu değerleri).</summary>
    void SetStatus(string key, params object[] args)
    {
        statusKey = key;
        statusArgs = args ?? System.Array.Empty<object>();
    }

    // ---- İşlemler ----

    void Connect()
    {
        try
        {
            if (!SteamClient.IsValid) SteamClient.Init(appId, false);

            if (!subscribed)
            {
                SteamFriends.OnGameLobbyJoinRequested += OnJoinRequested; // Arkadaşın daveti kabul edilince
                subscribed = true;
            }

            SetStatus("steam.connected", SteamClient.Name);
        }
        catch (System.Exception e)
        {
            SetStatus("steam.connect_failed", e.Message);
        }
    }

    async void CreateLobbyAndHost(NetworkManager manager)
    {
        busy = true;
        SetStatus("steam.creating");
        try
        {
            Lobby? created = await SteamMatchmaking.CreateLobbyAsync(MaxMembers);
            if (!created.HasValue) { SetStatus("steam.create_failed"); return; }

            Lobby lobby = created.Value;
            lobby.SetData(GameKey, GameValue);
            lobby.SetJoinable(true);
            if (friendsOnly) lobby.SetFriendsOnly(); else lobby.SetPublic();
            current = lobby;

            manager.NetworkConfig.NetworkTransport = steamTransport;
            if (manager.StartHost()) SetStatus("steam.lobby_ready");
            else SetStatus("steam.host_failed");
        }
        catch (System.Exception e)
        {
            SetStatus("steam.lobby_error", e.Message);
        }
        finally
        {
            busy = false;
        }
    }

    async void RefreshList()
    {
        busy = true;
        SetStatus("steam.searching");
        try
        {
            Lobby[] result = await SteamMatchmaking.LobbyList
                .FilterDistanceWorldwide()
                .WithKeyValue(GameKey, GameValue)
                .WithMaxResults(20)
                .RequestAsync();

            found.Clear();
            if (result != null) found.AddRange(result);
            if (found.Count == 0) SetStatus("steam.none_found");
            else SetStatus("steam.found", found.Count);
        }
        catch (System.Exception e)
        {
            SetStatus("steam.search_error", e.Message);
        }
        finally
        {
            busy = false;
        }
    }

    void OnJoinRequested(Lobby lobby, SteamId friend)
    {
        var manager = NetworkManager.Singleton;
        if (manager != null && !manager.IsListening) JoinLobby(manager, lobby);
    }

    async void JoinLobby(NetworkManager manager, Lobby lobby)
    {
        busy = true;
        SetStatus("steam.joining");
        try
        {
            RoomEnter result = await lobby.Join();
            if (result != RoomEnter.Success) { SetStatus("steam.join_failed", result); return; }

            current = lobby;
            steamTransport.targetSteamId = lobby.Owner.Id;
            manager.NetworkConfig.NetworkTransport = steamTransport;

            SetStatus(manager.StartClient() ? "steam.connecting_host" : "steam.client_failed");
        }
        catch (System.Exception e)
        {
            SetStatus("steam.join_error", e.Message);
        }
        finally
        {
            busy = false;
        }
    }

    void Leave(NetworkManager manager)
    {
        if (current.HasValue) current.Value.Leave();
        current = null;
        found.Clear();
        manager.Shutdown(); // Aktarım katmanı Steam'i de kapatır; yeniden bağlanmak için "Steam'e bağlan".
        SetStatus("steam.left");
    }
}
