using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tur sistemini sahnede çalıştırır: kelime veritabanını yükler, zarı ve <see cref="TurnManager"/>'ı kurar,
/// her karede zamanı ilerletir. Oyun mantığının kendisi TurnManager'dadır; burası yalnızca bağlayıcıdır.
/// </summary>
[DefaultExecutionOrder(-50)] // MatchBootstrap'tan sonra, ekran bileşenlerinden önce.
public class MatchRunner : TurnViewSource
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchBootstrap bootstrap;

    /// <summary>Tur döngüsü. Awake'ten sonra hazırdır.</summary>
    public TurnManager Turns { get; private set; }

    /// <summary>Yüklenen kelimeler.</summary>
    public WordDatabase Words { get; private set; }

    /// <summary>Bu maçta kullanılan kelimelerin kilidi.</summary>
    public MatchWordTracker WordTracker { get; private set; }

    public MatchState Match => bootstrap.Match;

    /// <summary>Maçın ortak rastgelelik kaynağı.</summary>
    public System.Random Random => bootstrap.Random;

    // ---- TurnViewSource (ekran bileşenleri buradan okur) ----

    public override ITurnView View => Turns;

    public override IReadOnlyList<PlayerState> Players => bootstrap.Match.Players;

    public override IReadOnlyList<WordEntry> UsedWords => WordTracker.UsedWords;

    /// <summary>Tek kişilikte yerel oyuncu, bot olmayan tek oyuncudur.</summary>
    public override PlayerState LocalPlayer
    {
        get
        {
            foreach (PlayerState player in bootstrap.Match.Players)
                if (!player.IsBot) return player;
            return null;
        }
    }

    public override string DescribeLocalStatus()
    {
        string status = StatusText.Describe(LocalPlayer?.Status);
        string chance = DescribeChance();
        if (string.IsNullOrEmpty(chance)) return status;
        return string.IsNullOrEmpty(status) ? chance : status + "\n" + chance;
    }

    /// <summary>"Seçilme şansın: %12" (bir sonraki zarda); oyuncu elendiyse boş.</summary>
    string DescribeChance()
    {
        PlayerState local = LocalPlayer;
        if (dice == null || local == null || !local.IsAlive) return string.Empty;
        float chance = dice.ChanceOf(local, bootstrap.Match.Players);
        return Loc.F("turn.chance", Mathf.RoundToInt(chance * 100f));
    }

    Dice dice;

    public override string DescribeShove()
    {
        PlayerState local = LocalPlayer;
        if (local == null || !local.IsAlive) return string.Empty;
        if (local.Status.HasWeapon) return WeaponText.Describe(local.Status);
        return ShoveCooldown.Describe(bootstrap.GetShoveCooldownLeft(local.Id));
    }

    public override void SubmitWord(string text, Action<SubmitStatus> onResult)
    {
        SubmitStatus status = Turns.SubmitWord(Turns.Writer.Id, text);
        onResult?.Invoke(status);
    }

    public override bool CanRestart => true;

    /// <summary>Sahneyi baştan yükler: yeni maç, yeni oyuncular, yeni kelime kilidi.</summary>
    public override void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    void Awake()
    {
        if (config == null || bootstrap == null)
        {
            Debug.LogError("MatchRunner: config ya da bootstrap atanmamış.", this);
            enabled = false;
            return;
        }

        Words = WordDatabase.LoadFromResources(config.maxWordLength);
        WordTracker = new MatchWordTracker(Words);

        dice = new Dice(bootstrap.Random, config.diceBonusPerMissedTurn);
        Turns = new TurnManager(config, bootstrap.Match, dice, WordTracker);
    }

    void Start()
    {
        // Ekran bileşenleri OnEnable'da olaylara abone oldu; şimdi ilk zar atılabilir.
        Turns.Start();

        // Esc ayarları açar (bkz. SettingsHotkey); ana menüye dönüş düğmesi ayarlar ekranında durur.
        if (SceneUtility.GetBuildIndexByScenePath(MenuScenePath) >= 0) SettingsScreen.SetLeaveAction(() => SettingsText.BackToMenu, GoToMenu);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DebugPanel.EnsureExists(); // F1 deneme paneli (oyuncuya giden sürümde yok)
#endif
    }

    /// <summary>Ana menü (ağ/Steam lobisi) sahnesi; build'de varsa ayarlar ekranındaki "Ana menüye dön" ile oraya dönülür.</summary>
    const string MenuScenePath = "Assets/Scenes/NetTest.unity";

    void OnDestroy() => SettingsScreen.ClearLeaveAction(GoToMenu);

    /// <summary>Tek kişilik oyundan ana menüye (lobiye) döner.</summary>
    void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath(MenuScenePath));
    }

    void Update()
    {
        Turns.Tick(Time.deltaTime);
    }
}
