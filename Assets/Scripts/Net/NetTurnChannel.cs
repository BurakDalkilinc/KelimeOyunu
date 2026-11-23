using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tur akışını ağa bağlar. Host'ta <see cref="NetTurnHost"/> çalışır ve mesajlarını RPC ile herkese yollar;
/// herkesin (host dahil) kendi <see cref="NetTurnClient"/> kopyası bu mesajlarla güncellenir.
/// İstemci yalnızca "şunu yazdım" der; sırayı ve sonucu host belirler, gönderen kimliği bağlantıdan alınır.
/// Kuralların kendisi düz C# sınıflarındadır; burası yalnızca taşıyıcıdır.
/// </summary>
public class NetTurnChannel : NetworkBehaviour, INetAvatars
{
    /// <summary>Maç en az bu kadar oyuncuyla başlar; eksikler botla doldurulur.</summary>
    const int MinPlayers = 4;

    /// <summary>Bot numaraları bağlantı numaralarıyla çakışmasın diye bu sayıdan başlar.</summary>
    public const int FirstBotId = 100;

    [SerializeField] GameConfig config;
    [SerializeField] NetworkObject avatarPrefab;

    /// <summary>Yaratık fotoğrafı yayın aralığı (sn). Saniyede ~12 kez; yeterince akıcı, ağı yormaz.</summary>
    const float CreatureBroadcastSeconds = 0.08f;

    WordDatabase words;
    NetTurnHost host;
    float creatureBroadcastLeft;
    readonly List<CreatureSnapshot> snapshotBuffer = new List<CreatureSnapshot>();
    readonly List<WallSnapshot> wallBuffer = new List<WallSnapshot>();

    /// <summary>
    /// Bu bilgisayardaki tur durumu kopyası. Hep vardır (arayüz ona abone olur); maç başlayana kadar aşaması
    /// <see cref="TurnPhase.NotStarted"/>'tır.
    /// </summary>
    public NetTurnClient Client { get; private set; }

    /// <summary>Maç başladı mı (istemci tur mesajı aldı)?</summary>
    public bool MatchActive => Client != null && Client.Phase != TurnPhase.NotStarted;

    Action<SubmitStatus> pendingSubmitResult;

    /// <summary>Host'taki yaratık dünyası ve can/etki durumu; maç başlayana kadar ve istemcilerde null.</summary>
    public NetMatchSim Sim { get; private set; }

    /// <summary>Host'tan gelen son yaratık fotoğrafı (her bilgisayarda çizim için).</summary>
    public IReadOnlyList<CreatureSnapshot> LatestCreatures => latestCreatures;

    /// <summary>Yeni fotoğraf geldikçe artar; çizici değişimi bundan anlar.</summary>
    public int CreaturesVersion { get; private set; }

    List<CreatureSnapshot> latestCreatures = new List<CreatureSnapshot>();
    List<WallSnapshot> latestWalls = new List<WallSnapshot>();
    List<ZoneSnapshot> latestZones = new List<ZoneSnapshot>();
    readonly List<ZoneSnapshot> zoneBuffer = new List<ZoneSnapshot>();

    /// <summary>Host'tan gelen son çözüm alanı fotoğrafı (sığınak, kule, mağara, ev).</summary>
    public IReadOnlyList<ZoneSnapshot> LatestZones => latestZones;

    /// <summary>Büyük tehdidin son durumu (alarm, parlama sayacı, dalga).</summary>
    public ThreatStatus LatestThreat { get; private set; } = new ThreatStatus { AlarmWordId = "" };

    /// <summary>Yeni tehdit durumu geldikçe artar.</summary>
    public int ThreatVersion { get; private set; }

    /// <summary>Host'tan gelen son duvar fotoğrafı.</summary>
    public IReadOnlyList<WallSnapshot> LatestWalls => latestWalls;

    /// <summary>Yeni duvar fotoğrafı geldikçe artar.</summary>
    public int WallsVersion { get; private set; }

    /// <summary>Hedef seçen oyuncunun numarası; kimse seçmiyorsa -1. Her bilgisayarda aynıdır.</summary>
    public int TargetingWriterId { get; private set; } = -1;

    /// <summary>Hedef seçmek için kalan süre (sn); yaklaşık, mesajlar arasında yerelde geri sayılır.</summary>
    public float TargetingTimeLeft { get; private set; }

    /// <summary>Bu bilgisayardaki oyuncu şu an hedef seçmek zorunda mı?</summary>
    public bool IAmChoosingTarget => TargetingWriterId >= 0 && TargetingWriterId == LocalPlayerId;

    /// <summary>Kelime kimliğinden kelimeyi bulur (yaratığın sprite'ı için); yoksa null.</summary>
    public WordEntry FindWord(string id)
    {
        if (words == null || string.IsNullOrEmpty(id)) return null;
        foreach (WordEntry entry in words.Words)
            if (entry.id == id) return entry;
        return null;
    }

    /// <summary>Son kelime gönderiminin sonucu (oyuncuya gösterilecek Türkçe metin).</summary>
    public string Feedback { get; private set; } = "";

    /// <summary>Bu bilgisayardaki oyuncunun maç numarası.</summary>
    public int LocalPlayerId => NetworkManager != null ? (int)NetworkManager.LocalClientId : -1;

    void Awake()
    {
        if (config == null) { Debug.LogError("NetTurnChannel: GameConfig atanmamış.", this); enabled = false; return; }
        words = WordDatabase.LoadFromResources(config.maxWordLength);
        Client = new NetTurnClient(words);
    }

    /// <summary>Maçı başlatır (yalnızca host). Bağlı herkes oyuncu olur, eksikler bot olur.</summary>
    public bool StartMatch()
    {
        if (!IsServer || host != null) return false;

        var roster = new List<RosterEntry>();
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            roster.Add(new RosterEntry((int)clientId, "Oyuncu " + clientId, false));
        for (int bot = 0; roster.Count < MinPlayers; bot++)
            roster.Add(new RosterEntry(FirstBotId + bot, "Bot " + (bot + 1), true));

        host = new NetTurnHost(config, words, config.CreateRandom(), roster);
        host.MessageReady += Broadcast;

        // Kabul edilen kelime host'ta yaratığa dönüşür (ilk zar atılmadan önce abone ol).
        Sim = new NetMatchSim(config, host.Match, this, config.CreateRandom());
        host.Turns.WordPlayed += Sim.OnWordPlayed;
        Sim.TargetingStarted += (writerId, seconds) => TargetingStartRpc(writerId, seconds);
        Sim.TargetingEnded += () => TargetingEndRpc();
        Sim.VoteStarted += seconds => VoteStartRpc(seconds);
        Sim.VoteEnded += winnerId => VoteEndRpc(winnerId);

        SpawnBotAvatars(roster);

        RosterRpc(RosterCodec.Encode(host.Roster));
        host.Start();
        return true;
    }

    /// <summary>Bot oyuncular için arenanın içinde bir halka üzerine karakter doğurur (hareketlerini host sürer).</summary>
    void SpawnBotAvatars(List<RosterEntry> roster)
    {
        if (avatarPrefab == null)
        {
            Debug.LogError("NetTurnChannel: avatarPrefab atanmamış, botlar görünmez.", this);
            return;
        }

        var arena = new ArenaBounds(config.arenaSize);
        float ringRadius = Mathf.Min(arena.HalfSize.x, arena.HalfSize.y) * 0.6f;
        int botCount = roster.FindAll(r => r.IsBot).Count;
        int index = 0;

        foreach (RosterEntry entry in roster)
        {
            if (!entry.IsBot) continue;

            float angle = Mathf.PI * 2f * index / Mathf.Max(1, botCount);
            Vector3 spot = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
            index++;

            NetworkObject avatar = Instantiate(avatarPrefab, spot, Quaternion.identity);
            avatar.GetComponent<NetworkPlayer>().PrepareAsBot(entry.Id);
            avatar.Spawn();
        }
    }

    /// <summary>Maç bitti mi? (Yeni maç başlatılabilir.)</summary>
    public bool CanRestart => IsServer && host != null && host.Turns.Phase == TurnPhase.MatchOver;

    /// <summary>
    /// Bitmiş maçı kapatıp yeni maça hazırlar (yalnızca host): bot karakterleri (kopanlar dahil) silinir, insanlar tam canla
    /// başlangıç yerine döner, herkesin ekranı temizlenir. Sonra "Maçı başlat" yeniden kullanılır.
    /// </summary>
    public bool RestartMatch()
    {
        if (!CanRestart) return false;

        host = null;
        Sim = null;

        // Bot karakterleri sil (liste değişeceği için kopyadan git).
        foreach (NetworkPlayer avatar in new List<NetworkPlayer>(NetworkPlayer.All))
        {
            if (avatar == null) continue;
            if (avatar.IsBot) { if (avatar.NetworkObject.IsSpawned) avatar.NetworkObject.Despawn(); }
            else avatar.ResetOnServer();
        }

        ResetMatchRpc();
        return true;
    }

    /// <summary>Hayaletler kimin dirileceğini oyluyor mu? Her bilgisayarda aynıdır.</summary>
    public bool VoteActive { get; private set; }

    /// <summary>Oylamanın kalan süresi (sn); yaklaşık, mesajlar arasında yerelde geri sayılır.</summary>
    public float VoteTimeLeft { get; private set; }

    /// <summary>Bu bilgisayardaki oyuncu elenmiş ve oy verebilir mi?</summary>
    public bool IAmVoting
    {
        get
        {
            if (!VoteActive || NetworkManager == null || NetworkManager.LocalClient == null || NetworkManager.LocalClient.PlayerObject == null) return false;
            NetworkPlayer mine = NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
            return mine != null && mine.IsGhost && !mine.IsBot;
        }
    }

    /// <summary>Elenmiş oyuncu, dirilmesini istediği hayalete tıkladı: host'a bildirir (geçerli mi, host karar verir).</summary>
    public void VoteForGhost(int candidatePlayerId)
    {
        if (!IsSpawned || !IAmVoting) return;
        VoteRpc(candidatePlayerId);
    }

    /// <summary>Hedef seçen oyuncu bir rakibe tıkladı: host'a bildirir (geçerli mi, host karar verir).</summary>
    public void ChooseTarget(int targetPlayerId)
    {
        if (!IsSpawned || !IAmChoosingTarget) return;
        ChooseTargetRpc(targetPlayerId);
    }

    /// <summary>Bu bilgisayardaki oyuncu bir kelime yazdı: host'a gönderir.</summary>
    public void SubmitWord(string text)
    {
        SubmitWord(text, null);
    }

    /// <summary>Bir kelime gönderir; host cevap verince <paramref name="onResult"/> çağrılır.</summary>
    public void SubmitWord(string text, Action<SubmitStatus> onResult)
    {
        if (!IsSpawned || !MatchActive) return;
        pendingSubmitResult = onResult;
        SubmitWordRpc(text ?? "");
    }

    void Update()
    {
        if (host != null)
        {
            host.Tick(Time.deltaTime);
            Sim.Tick(Time.deltaTime);
            BroadcastCreatures(Time.deltaTime);
        }

        if (Client != null)
        {
            Client.Tick(Time.deltaTime);
            SyncClientHealth();
        }
        if (TargetingWriterId >= 0) TargetingTimeLeft = Mathf.Max(0f, TargetingTimeLeft - Time.deltaTime);
        if (VoteActive) VoteTimeLeft = Mathf.Max(0f, VoteTimeLeft - Time.deltaTime);
    }

    /// <summary>
    /// Karakterlerden gelen canı istemcideki oyuncu kayıtlarına yansıtır; arayüz (can yazısı, hayatta kalanlar) bunları okur.
    /// </summary>
    void SyncClientHealth()
    {
        foreach (NetworkPlayer avatar in NetworkPlayer.All)
        {
            if (avatar == null || !Client.Players.TryGetValue(avatar.PlayerId, out PlayerState state)) continue;

            int difference = avatar.Health - state.Health.Current;
            if (difference < 0) state.Health.TakeDamage(-difference);
            else if (difference > 0 && state.Health.IsDead) state.Health.Revive(avatar.Health); // Yeniden doğdu
            else if (difference > 0) state.Health.Heal(difference);
        }
    }

    void BroadcastCreatures(float dt)
    {
        creatureBroadcastLeft -= dt;
        if (creatureBroadcastLeft > 0f) return;
        creatureBroadcastLeft = CreatureBroadcastSeconds;

        Sim.Snapshot(snapshotBuffer);
        CreaturesRpc(CreatureSnapshotCodec.Encode(snapshotBuffer));

        Sim.SnapshotWalls(wallBuffer);
        WallsRpc(WallSnapshotCodec.Encode(wallBuffer));

        Sim.SnapshotZones(zoneBuffer);
        ThreatsRpc(ZoneSnapshotCodec.Encode(zoneBuffer), ThreatStatusCodec.Encode(Sim.GetThreatStatus()));
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Bir oyuncunun bağlantısı koptu. Maç başlamadıysa karakteri silinir; başladıysa bot devralır:
    /// tur sırası, hedef seçimi ve karakterin hareketi bota geçer, herkese haber verilir.
    /// </summary>
    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return; // Host giderse oyun zaten biter.

        int playerId = (int)clientId;
        bool avatarFound = NetworkPlayer.TryFind(playerId, out NetworkPlayer avatar);

        if (host == null)
        {
            if (avatarFound && avatar.NetworkObject.IsSpawned) avatar.NetworkObject.Despawn();
            return;
        }

        if (!host.ReplaceWithBot(playerId)) return;

        Sim.OnPlayerBecameBot(playerId);
        if (avatarFound) avatar.BecomeBotOnServer();
        PlayerBecameBotRpc(playerId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        host = null;
        Sim = null;
        Client?.Reset();
        latestCreatures = new List<CreatureSnapshot>();
        CreaturesVersion++;
        latestWalls = new List<WallSnapshot>();
        WallsVersion++;
        latestZones = new List<ZoneSnapshot>();
        LatestThreat = new ThreatStatus { AlarmWordId = "" };
        ThreatVersion++;
        TargetingWriterId = -1;
        VoteActive = false;
    }

    // ---- INetAvatars (host'taki karakterler) ----

    bool INetAvatars.TryGetPosition(int playerId, out Vector2 position)
    {
        if (NetworkPlayer.TryFind(playerId, out NetworkPlayer avatar))
        {
            position = avatar.transform.position;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    bool INetAvatars.TryGetFacing(int playerId, out Vector2 direction)
    {
        if (NetworkPlayer.TryFind(playerId, out NetworkPlayer avatar))
        {
            direction = avatar.FacingDirection;
            return true;
        }

        direction = Vector2.up;
        return false;
    }

    bool INetAvatars.TryApplyKnockback(int playerId, Vector2 distance)
    {
        if (!NetworkPlayer.TryFind(playerId, out NetworkPlayer avatar)) return false;
        avatar.ApplyKnockback(distance);
        return true;
    }

    void Broadcast(TurnMessage m)
    {
        StateRpc((int)m.Kind, (int)m.Phase, m.TurnNumber, m.WriterId, m.PhaseTimeLeft, m.WordId ?? "", m.WinnerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void RosterRpc(string packedRoster)
    {
        if (!RosterCodec.TryDecode(packedRoster, out List<RosterEntry> roster))
        {
            Debug.LogError("[Ağ] Bozuk oyuncu listesi geldi.");
            return;
        }

        Client.Reset();
        Client.SetRoster(roster, config.startingHealth);
        Feedback = "";
    }

    [Rpc(SendTo.ClientsAndHost)]
    void StateRpc(int kind, int phase, int turn, int writerId, float timeLeft, string wordId, int winnerId)
    {
        Client.Apply(new TurnMessage
        {
            Kind = (TurnMessageKind)kind,
            Phase = (TurnPhase)phase,
            TurnNumber = turn,
            WriterId = writerId,
            PhaseTimeLeft = timeLeft,
            WordId = wordId,
            WinnerId = winnerId,
        });
    }

    [Rpc(SendTo.ClientsAndHost)]
    void CreaturesRpc(string packedCreatures)
    {
        if (!CreatureSnapshotCodec.TryDecode(packedCreatures, out List<CreatureSnapshot> list)) return;
        latestCreatures = list;
        CreaturesVersion++;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void WallsRpc(string packedWalls)
    {
        if (!WallSnapshotCodec.TryDecode(packedWalls, out List<WallSnapshot> list)) return;
        latestWalls = list;
        WallsVersion++;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ThreatsRpc(string packedZones, string packedStatus)
    {
        if (ZoneSnapshotCodec.TryDecode(packedZones, out List<ZoneSnapshot> zones)) latestZones = zones;
        if (ThreatStatusCodec.TryDecode(packedStatus, out ThreatStatus status)) LatestThreat = status;
        ThreatVersion++;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ResetMatchRpc()
    {
        Client.Reset();
        Feedback = "";
        latestCreatures = new List<CreatureSnapshot>();
        CreaturesVersion++;
        latestWalls = new List<WallSnapshot>();
        WallsVersion++;
        latestZones = new List<ZoneSnapshot>();
        LatestThreat = new ThreatStatus { AlarmWordId = "" };
        ThreatVersion++;
        TargetingWriterId = -1;
        TargetingTimeLeft = 0f;
        VoteActive = false;
        VoteTimeLeft = 0f;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void VoteStartRpc(float seconds)
    {
        VoteActive = true;
        VoteTimeLeft = seconds;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void VoteEndRpc(int winnerId)
    {
        VoteActive = false;
        VoteTimeLeft = 0f;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void VoteRpc(int candidatePlayerId, RpcParams rpcParams = default)
    {
        if (Sim == null) return;

        // Oy veren bağlantıdan bulunur; yalnızca elenmiş insan oyuncular oy verebilir.
        Sim.TryVote((int)rpcParams.Receive.SenderClientId, candidatePlayerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayerBecameBotRpc(int playerId)
    {
        Client.MarkBot(playerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TargetingStartRpc(int writerId, float seconds)
    {
        TargetingWriterId = writerId;
        TargetingTimeLeft = seconds;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TargetingEndRpc()
    {
        TargetingWriterId = -1;
        TargetingTimeLeft = 0f;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ChooseTargetRpc(int targetPlayerId, RpcParams rpcParams = default)
    {
        if (Sim == null) return;

        // Seçen kişi bağlantıdan bulunur; yalnızca sırası olan yazan seçebilir.
        Sim.TryChooseTarget((int)rpcParams.Receive.SenderClientId, targetPlayerId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SubmitWordRpc(string text, RpcParams rpcParams = default)
    {
        if (host == null) return;

        // Kimin yazdığı bağlantıdan bulunur; istemcinin kendi söylediğine güvenilmez.
        ulong sender = rpcParams.Receive.SenderClientId;
        SubmitStatus status = host.Submit((int)sender, text);
        FeedbackRpc((int)status, RpcTarget.Single(sender, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void FeedbackRpc(int status, RpcParams rpcParams)
    {
        Feedback = SubmitFeedback.GetMessage((SubmitStatus)status, config.maxWordLength);

        Action<SubmitStatus> callback = pendingSubmitResult;
        pendingSubmitResult = null;
        callback?.Invoke((SubmitStatus)status);
    }
}
