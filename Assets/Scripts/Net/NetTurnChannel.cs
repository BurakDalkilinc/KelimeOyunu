using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tur akışını ağa bağlar. Host'ta <see cref="NetTurnHost"/> çalışır ve mesajlarını RPC ile herkese yollar;
/// herkesin (host dahil) kendi <see cref="NetTurnClient"/> kopyası bu mesajlarla güncellenir.
/// İstemci yalnızca "şunu yazdım" der; sırayı ve sonucu host belirler, gönderen kimliği bağlantıdan alınır.
/// Kuralların kendisi düz C# sınıflarındadır; burası yalnızca taşıyıcıdır.
/// </summary>
public class NetTurnChannel : NetworkBehaviour
{
    /// <summary>Maç en az bu kadar oyuncuyla başlar; eksikler botla doldurulur.</summary>
    const int MinPlayers = 4;

    /// <summary>Bot numaraları bağlantı numaralarıyla çakışmasın diye bu sayıdan başlar.</summary>
    const int FirstBotId = 100;

    [SerializeField] GameConfig config;

    WordDatabase words;
    NetTurnHost host;

    /// <summary>Bu bilgisayardaki tur durumu kopyası; maç başlayana kadar null.</summary>
    public NetTurnClient Client { get; private set; }

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

        RosterRpc(RosterCodec.Encode(host.Roster));
        host.Start();
        return true;
    }

    /// <summary>Bu bilgisayardaki oyuncu bir kelime yazdı: host'a gönderir.</summary>
    public void SubmitWord(string text)
    {
        if (!IsSpawned || Client == null) return;
        SubmitWordRpc(text ?? "");
    }

    void Update()
    {
        if (host != null) host.Tick(Time.deltaTime);
        if (Client != null) Client.Tick(Time.deltaTime);
    }

    public override void OnNetworkDespawn()
    {
        host = null;
        Client = null;
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
