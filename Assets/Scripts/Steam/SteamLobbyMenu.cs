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
    const int MaxMembers = 8;

    [SerializeField] FacepunchTransport steamTransport;
    [SerializeField] uint appId = 480;

    readonly List<Lobby> found = new List<Lobby>();
    Lobby? current;
    string status = "Steam'e bağlanılmadı.";
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
        var manager = NetworkManager.Singleton;
        if (manager == null) return;

        // Yerel ağda (Steam lobisi olmadan) host/istemciyken Steam kutusu gereksiz.
        if (manager.IsListening && !current.HasValue) return;

        float height = manager.IsListening ? 130f : (SteamClient.IsValid ? 270f : 120f);
        GUILayout.BeginArea(new Rect(304, Screen.height - height - 12, 320, height), GUI.skin.box);
        GUILayout.Label("Steam ile oyna");

        if (!SteamClient.IsValid) DrawConnect();
        else if (manager.IsListening) DrawInLobby(manager);
        else DrawLobbyChoices(manager);

        GUILayout.Label(status);
        GUILayout.EndArea();
    }

    // ---- Arayüz ----

    void DrawConnect()
    {
        GUI.enabled = !busy;
        if (GUILayout.Button("Steam'e bağlan")) Connect();
        GUI.enabled = true;
        GUILayout.Label("Steam istemcisi açık ve giriş yapılmış olmalı.");
    }

    void DrawLobbyChoices(NetworkManager manager)
    {
        GUI.enabled = !busy;
        friendsOnly = GUILayout.Toggle(friendsOnly, "Yalnızca arkadaşlar (davet ile katılır, aramada görünmez)");
        if (GUILayout.Button("Lobi kur (host ol)")) CreateLobbyAndHost(manager);
        if (GUILayout.Button("Lobileri bul")) RefreshList();

        foreach (Lobby lobby in found)
        {
            string owner = new Friend(lobby.Owner.Id).Name;
            if (GUILayout.Button($"Katıl: {owner} ({lobby.MemberCount}/{lobby.MaxMembers})")) JoinLobby(manager, lobby);
        }
        GUI.enabled = true;
    }

    void DrawInLobby(NetworkManager manager)
    {
        if (current.HasValue)
        {
            GUILayout.Label($"Lobi: {current.Value.MemberCount}/{current.Value.MaxMembers} kişi");
            if (manager.IsHost && GUILayout.Button("Arkadaş davet et (Steam)")) SteamFriends.OpenGameInviteOverlay(current.Value.Id);
        }
        if (GUILayout.Button("Lobiden ayrıl")) Leave(manager);
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

            status = "Steam'e bağlandı: " + SteamClient.Name;
        }
        catch (System.Exception e)
        {
            status = "Steam'e bağlanılamadı. Steam açık mı? (" + e.Message + ")";
        }
    }

    async void CreateLobbyAndHost(NetworkManager manager)
    {
        busy = true;
        status = "Lobi kuruluyor...";
        try
        {
            Lobby? created = await SteamMatchmaking.CreateLobbyAsync(MaxMembers);
            if (!created.HasValue) { status = "Lobi kurulamadı."; return; }

            Lobby lobby = created.Value;
            lobby.SetData(GameKey, GameValue);
            lobby.SetJoinable(true);
            if (friendsOnly) lobby.SetFriendsOnly(); else lobby.SetPublic();
            current = lobby;

            manager.NetworkConfig.NetworkTransport = steamTransport;
            if (manager.StartHost()) status = "Lobi hazır. Arkadaşını davet et, sonra Maçı başlat.";
            else status = "Host başlatılamadı.";
        }
        catch (System.Exception e)
        {
            status = "Lobi hatası: " + e.Message;
        }
        finally
        {
            busy = false;
        }
    }

    async void RefreshList()
    {
        busy = true;
        status = "Lobiler aranıyor...";
        try
        {
            Lobby[] result = await SteamMatchmaking.LobbyList
                .FilterDistanceWorldwide()
                .WithKeyValue(GameKey, GameValue)
                .WithMaxResults(20)
                .RequestAsync();

            found.Clear();
            if (result != null) found.AddRange(result);
            status = found.Count == 0 ? "Lobi bulunamadı." : found.Count + " lobi bulundu.";
        }
        catch (System.Exception e)
        {
            status = "Arama hatası: " + e.Message;
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
        status = "Lobiye katılınıyor...";
        try
        {
            RoomEnter result = await lobby.Join();
            if (result != RoomEnter.Success) { status = "Lobiye girilemedi: " + result; return; }

            current = lobby;
            steamTransport.targetSteamId = lobby.Owner.Id;
            manager.NetworkConfig.NetworkTransport = steamTransport;

            status = manager.StartClient() ? "Host'a bağlanılıyor..." : "Bağlantı başlatılamadı.";
        }
        catch (System.Exception e)
        {
            status = "Katılma hatası: " + e.Message;
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
        status = "Lobiden ayrıldın.";
    }
}
