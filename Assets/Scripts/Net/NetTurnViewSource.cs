using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ maçında Arena arayüzünün (zar yazısı, durum yazısı, yazı kutusu, can, etkiler, maç sonu) okuduğu kaynak.
/// Tur durumu <see cref="NetTurnClient"/>'tan, can ve etkiler karakterlerden, kelime gönderimi ve yeni maç
/// <see cref="NetTurnChannel"/>'dan gelir. Tek kişilikteki karşılığı <see cref="MatchRunner"/>'dır.
/// </summary>
public class NetTurnViewSource : TurnViewSource
{
    [SerializeField] NetTurnChannel channel;

    static readonly List<PlayerState> NoPlayers = new List<PlayerState>();

    public override ITurnView View => channel != null ? channel.Client : null;

    public override IReadOnlyList<PlayerState> Players
    {
        get
        {
            if (channel == null || channel.Client == null) return NoPlayers;
            return new List<PlayerState>(channel.Client.Players.Values);
        }
    }

    public override IReadOnlyList<WordEntry> UsedWords => channel != null ? channel.Client.PlayedWords : new List<WordEntry>();

    public override PlayerState LocalPlayer
    {
        get
        {
            if (channel == null || channel.Client == null || !channel.IsSpawned) return null;
            return channel.Client.Players.TryGetValue(channel.LocalPlayerId, out PlayerState player) ? player : null;
        }
    }

    /// <summary>İstemci kalan süreleri bilmez; karakterin etki bayraklarından süresiz özet çıkarılır.</summary>
    public override string DescribeLocalStatus()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return string.Empty;

        NetworkPlayer mine = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
        return mine == null || mine.IsGhost ? string.Empty : StatusText.Describe(mine.Status);
    }

    public override string DescribeShove()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return string.Empty;

        NetworkPlayer mine = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
        if (mine == null || mine.IsGhost || !channel.MatchActive) return string.Empty;
        return ShoveCooldown.Describe(mine.ShoveLeft);
    }

    public override void SubmitWord(string text, Action<SubmitStatus> onResult)
    {
        channel.SubmitWord(text, onResult);
    }

    public override bool CanRestart => channel != null && channel.CanRestart;

    public override void Restart()
    {
        if (channel != null) channel.RestartMatch();
    }

    public override string RestartHint
    {
        get
        {
            if (channel == null) return string.Empty;
            return channel.IsServer ? "Yeni maç için R'ye bas" : "Host'un yeni maçı hazırlaması bekleniyor...";
        }
    }
}
