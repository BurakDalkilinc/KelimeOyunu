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
        if (mine == null || mine.IsGhost) return string.Empty;

        string status = StatusText.Describe(mine.Status);
        string chance = DescribeChance(mine.PlayerId);
        if (string.IsNullOrEmpty(chance)) return status;
        return string.IsNullOrEmpty(status) ? chance : status + "\n" + chance;
    }

    /// <summary>"Seçilme şansın: %12" — zarın ağırlığı (kaçırılan turlar) istemcide sayılır.</summary>
    string DescribeChance(int playerId)
    {
        if (channel == null || channel.Client == null || channel.Config == null || !channel.MatchActive) return string.Empty;
        if (!channel.Client.Players.TryGetValue(playerId, out PlayerState me) || !me.IsAlive) return string.Empty;

        var dice = new Dice(new System.Random(0), channel.Config.diceBonusPerMissedTurn);
        float chance = dice.ChanceOf(me, channel.Client.Players.Values);
        return Loc.F("turn.chance", Mathf.RoundToInt(chance * 100f));
    }

    public override string DescribeShove()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return string.Empty;

        NetworkPlayer mine = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
        if (mine == null || mine.IsGhost || !channel.MatchActive) return string.Empty;
        if ((mine.Status & StatusFlags.Weapon) != 0) return WeaponText.ArmedOnNetwork;
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

    public override bool CanLeaveToMenu => true;

    /// <summary>
    /// Bağlantıyı keser ve menü sahnesini (bu sahne) baştan yükler. Ağ yöneticisi sahneler arasında taşındığı için önce silinir;
    /// yeniden yüklenen sahnedeki yenisi kullanılır (bkz. <see cref="NetworkMenu"/>).
    /// </summary>
    public override void LeaveToMenu()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        ScreenFader.Run(() =>
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null)
            {
                if (manager.IsListening) manager.Shutdown();
                Destroy(manager.gameObject);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        });
    }

    public override string RestartHint
    {
        get
        {
            if (channel == null) return string.Empty;
            return channel.IsServer ? Loc.T("match.restart_hint_host") : Loc.T("match.wait_host_restart");
        }
    }
}
