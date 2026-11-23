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
    const int FirstBotId = 100;

    [SerializeField] GameConfig config;
    [SerializeField] NetworkObject avatarPrefab;

    /// <summary>Yaratık fotoğrafı yayın aralığı (sn). Saniyede ~12 kez; yeterince akıcı, ağı yormaz.</summary>
    const float CreatureBroadcastSeconds = 0.08f;

    WordDatabase words;
    NetTurnHost host;
    float creatureBroadcastLeft;
    readonly List<CreatureSnapshot> snapshotBuffer = new List<CreatureSnapshot>();

    /// <summary>Bu bilgisayardaki tur durumu kopyası; maç başlayana kadar null.</summary>
    public NetTurnClient Client { get; private set; }

    /// <summary>Host'taki yaratık dünyası ve can/etki durumu; maç başlayana kadar ve istemcilerde null.</summary>
    public NetMatchSim Sim { get; private set; }

    /// <summary>Host'tan gelen son yaratık fotoğrafı (her bilgisayarda çizim için).</summary>
    public IReadOnlyList<CreatureSnapshot> LatestCreatures => latestCreatures;

    /// <summary>Yeni fotoğraf geldikçe artar; çizici değişimi bundan anlar.</summary>
    public int CreaturesVersion { get; private set; }

    List<CreatureSnapshot> latestCreatures = new List<CreatureSnapshot>();

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

    /// <summary>Bu bilgisayardaki oyuncu bir kelime yazdı: host'a gönderir.</summary>
    public void SubmitWord(string text)
    {
        if (!IsSpawned || Client == null) return;
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

        if (Client != null) Client.Tick(Time.deltaTime);
    }

    void BroadcastCreatures(float dt)
    {
        creatureBroadcastLeft -= dt;
        if (creatureBroadcastLeft > 0f) return;
        creatureBroadcastLeft = CreatureBroadcastSeconds;

        Sim.Snapshot(snapshotBuffer);
        CreaturesRpc(CreatureSnapshotCodec.Encode(snapshotBuffer));
    }

    public override void OnNetworkDespawn()
    {
        host = null;
        Sim = null;
        Client = null;
        latestCreatures = new List<CreatureSnapshot>();
        CreaturesVersion++;
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

        Client = new NetTurnClient(words);
        Client.SetRoster(roster, config.startingHealth);
        Feedback = "";
    }

    [Rpc(SendTo.ClientsAndHost)]
    void StateRpc(int kind, int phase, int turn, int writerId, float timeLeft, string wordId, int winnerId)
    {
        if (Client == null) return;

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
    }
}
