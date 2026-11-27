using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Global tehditleri sahnede yönetir: tehdit kelimesi kabul edilince <see cref="GlobalThreat"/> başlatır, her karede
/// ilerletir, çözüm alanlarını çizer, ekranın üstüne alarm yazar, etki anında ekranı parlatır ve botlara en yakın
/// boş sığınağı gösterir. Tehdit mantığı GlobalThreat'tedir; burası yalnızca bağlantı ve görüntü.
/// </summary>
public class ThreatDirector : MonoBehaviour, IRefugeProvider
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;
    [SerializeField] MatchBootstrap bootstrap;
    [SerializeField] CreatureSpawner spawner;
    [SerializeField] Text banner;
    [SerializeField] Image flash;

    readonly List<GlobalThreat> threats = new List<GlobalThreat>();
    readonly Dictionary<SafeZone, GameObject> zoneViews = new Dictionary<SafeZone, GameObject>();
    readonly HashSet<WaveEffect> waveViews = new HashSet<WaveEffect>();
    TurnManager turns;
    ArenaBounds arena;
    System.Random random;
    float flashAlpha;

    void OnEnable()
    {
        if (config == null || runner == null || runner.Turns == null || bootstrap == null || banner == null || flash == null)
        {
            Debug.LogError("ThreatDirector: bir referans eksik (config, runner, bootstrap, banner, flash).", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        arena = new ArenaBounds(config.arenaSize);
        random = new System.Random(runner.Random.Next());
        turns.WordPlayed += OnWordPlayed;

        foreach (BotController bot in bootstrap.Bots)
            bot.Refuge = this;
        BindHouseProtection();

        banner.text = string.Empty;
        SetFlash(0f);
    }

    void OnDisable()
    {
        if (turns != null) turns.WordPlayed -= OnWordPlayed;
        foreach (GameObject view in zoneViews.Values)
            if (view != null) Destroy(view);
        zoneViews.Clear();
    }

    void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        if (word == null || word.category != WordEntry.CategoryThreat || word.scope != WordEntry.ScopeGlobal) return;

        BindHouseProtection();
        var threat = new GlobalThreat(word, writer, config, arena, bootstrap, new System.Random(random.Next()), spawner,
                                       bootstrap.Terrain != null ? (System.Func<Vector2, bool>)bootstrap.Terrain.IsBlocked : null);
        threat.Zones.Added += CreateZoneView;
        threat.Zones.Removed += DestroyZoneView;
        // Ekran parlaması yalnızca çözüm alanı olan (sığınılan) tehditlerde; meteor yağmuru yavaş yavaş başlar.
        if (ThreatSpec.HasZones(word)) threat.Impacted += () => OnImpact(word);

        // Alanlar kurucuda eklendi; abone olmadan önce eklenenlerin görüntüsü burada çıkar.
        foreach (SafeZone zone in threat.Zones.Zones) CreateZoneView(zone);

        threats.Add(threat);
    }

    /// <summary>Sığınak evleri tehdit sürerken boss tarafından yıkılmasın (arazi bu soruyu buraya sorar).</summary>
    void BindHouseProtection()
    {
        TerrainSim terrain = bootstrap.Terrain;
        if (terrain != null) terrain.IsHouseProtected = house => RefugeFinder.IsHouseRefuge(threats, terrain.Map, house);
    }

    void Update()
    {
        for (int i = threats.Count - 1; i >= 0; i--)
        {
            threats[i].Tick(Time.deltaTime);
            if (threats[i].Phase == ThreatPhase.Finished) threats.RemoveAt(i);
        }

        CreateEffectViews();
        UpdateBanner();

        if (flashAlpha > 0f) SetFlash(Mathf.Max(0f, flashAlpha - Time.deltaTime * 1.5f));
    }

    /// <summary>Etkinleşen dalgalar için (bir kez) görüntü oluşturur.</summary>
    void CreateEffectViews()
    {
        foreach (GlobalThreat threat in threats)
        {
            foreach (IThreatEffect effect in threat.Effects)
            {
                if (effect is WaveEffect wave && wave.IsActive && waveViews.Add(wave))
                {
                    var go = new GameObject("Dalga görüntüsü");
                    go.transform.SetParent(transform, false);
                    go.AddComponent<WaveView>().Initialize(wave, arena);
                }
            }
        }
    }

    /// <summary>En acil uyarıyı (etkinleşmeye en az süresi kalan) ekranın üstüne yazar.</summary>
    void UpdateBanner()
    {
        GlobalThreat urgent = null;
        foreach (GlobalThreat threat in threats)
        {
            // Uyarıdaki tehditler; ayrıca yaratık/gölge tehditleri (meteor, sürü, ejderha) etkisi sürerken hatırlatılır.
            bool activeAndRunning = threat.Phase == ThreatPhase.Active && !threat.EffectsFinished;
            if (threat.Phase != ThreatPhase.Warning && !activeAndRunning) continue;
            if (urgent == null || threat.WarningLeft < urgent.WarningLeft) urgent = threat;
        }

        // Yeni tehdit uyarısı "punch" ile girer, bitince yukarı kayarak çıkar (bkz. PunchText).
        string alarm = urgent != null ? ThreatText.GetAlarm(urgent.Word, Mathf.CeilToInt(urgent.WarningLeft)) : string.Empty;
        PunchText.Of(banner).Set(alarm, urgent != null ? urgent.Word.id : null);

        ReportAtmosphere();
    }

    /// <summary>
    /// Tehdidin havası: teması olan tehditler arasından en ilerlemiş olanı (uyarı süresi en çok dolmuş ya da etkisi süren)
    /// ekranı temanın rengine boyar, parçacık çıkarır. Temasız bir tehdit (sürü) aynı anda olsa da hava kaybolmaz.
    /// </summary>
    void ReportAtmosphere()
    {
        GlobalThreat best = null;
        float bestProgress = -1f;
        foreach (GlobalThreat threat in threats)
        {
            if (string.IsNullOrEmpty(threat.Word.fx)) continue;
            bool activeAndRunning = threat.Phase == ThreatPhase.Active && !threat.EffectsFinished;
            if (threat.Phase != ThreatPhase.Warning && !activeAndRunning) continue;

            float total = threat.Word.stats != null ? threat.Word.stats.warningSeconds : 0f;
            float progress = threat.Phase == ThreatPhase.Warning && total > 0f ? 1f - threat.WarningLeft / total : 1f;
            if (progress > bestProgress) { best = threat; bestProgress = progress; }
        }
        if (best != null) ThreatAtmosphere.Instance?.Report(best.Word.fx, bestProgress);
    }

    void OnImpact(WordEntry word)
    {
        ThreatAtmosphere atmosphere = ThreatAtmosphere.Instance;
        if (atmosphere != null && !string.IsNullOrEmpty(word.fx))
        {
            atmosphere.Impact(word.fx);
            flashColor = atmosphere.FlashColor;
        }
        else flashColor = Color.white;
        SetFlash(0.85f);
    }

    Color flashColor = Color.white;

    void SetFlash(float alpha)
    {
        flashAlpha = alpha;
        flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
    }

    void CreateZoneView(SafeZone zone)
    {
        var go = new GameObject("Çözüm: " + ThreatText.GetZoneName(zone.Type));
        go.transform.SetParent(transform, false);
        go.AddComponent<SafeZoneView>().Initialize(zone);
        zoneViews[zone] = go;
    }

    void DestroyZoneView(SafeZone zone)
    {
        if (zoneViews.TryGetValue(zone, out GameObject view))
        {
            if (view != null) Destroy(view);
            zoneViews.Remove(zone);
        }
    }

    /// <summary>Şu an haritada çözüm alanı (sığınak, kule, mağara, ev) var mı? Kamera bu sırada tüm arenayı gösterir.</summary>
    public bool HasZones
    {
        get
        {
            foreach (GlobalThreat threat in threats)
                if (threat.Zones.Zones.Count > 0) return true;
            return false;
        }
    }

    /// <summary>Oyuncu herhangi bir tehdidin çözüm alanında şu an korunuyor mu?</summary>
    public bool IsSheltered(PlayerState player)
    {
        return RefugeFinder.IsSheltered(threats, player);
    }

    /// <summary>
    /// Botun sığınacağı yeri verir: zaten içinde olduğu alan varsa orada kalır, yoksa en yakın boş alana gider.
    /// Etkinlik bittiyse (alan kalkınca) false döner.
    /// </summary>
    public bool TryGetRefuge(PlayerState player, Vector2 position, out Vector2 point)
    {
        return RefugeFinder.TryGetRefuge(threats, player, position, out point);
    }
}
