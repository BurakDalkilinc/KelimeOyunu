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

        var threat = new GlobalThreat(word, writer, config, arena, bootstrap, new System.Random(random.Next()), spawner);
        threat.Zones.Added += CreateZoneView;
        threat.Zones.Removed += DestroyZoneView;
        // Ekran parlaması yalnızca çözüm alanı olan (sığınılan) tehditlerde; meteor yağmuru yavaş yavaş başlar.
        if (ThreatSpec.HasZones(word)) threat.Impacted += OnImpact;

        // Alanlar kurucuda eklendi; abone olmadan önce eklenenlerin görüntüsü burada çıkar.
        foreach (SafeZone zone in threat.Zones.Zones) CreateZoneView(zone);

        threats.Add(threat);
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

        banner.text = urgent != null ? ThreatText.GetAlarm(urgent.Word, Mathf.CeilToInt(urgent.WarningLeft)) : string.Empty;
    }

    void OnImpact()
    {
        SetFlash(0.85f);
    }

    void SetFlash(float alpha)
    {
        flashAlpha = alpha;
        flash.color = new Color(1f, 1f, 1f, alpha);
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

    /// <summary>Oyuncu herhangi bir tehdidin çözüm alanında şu an korunuyor mu?</summary>
    public bool IsSheltered(PlayerState player)
    {
        foreach (GlobalThreat threat in threats)
            if (threat.Zones.IsProtected(player.Id)) return true;
        return false;
    }

    /// <summary>
    /// Botun sığınacağı yeri verir: zaten içinde olduğu alan varsa orada kalır, yoksa en yakın boş alana gider.
    /// Etkinlik bittiyse (alan kalkınca) null döner.
    /// </summary>
    public bool TryGetRefuge(PlayerState player, Vector2 position, out Vector2 point)
    {
        point = Vector2.zero;
        float bestDistance = float.MaxValue;
        bool found = false;

        foreach (GlobalThreat threat in threats)
        {
            foreach (SafeZone zone in threat.Zones.Zones)
            {
                if (zone.IsOccupant(player.Id))
                {
                    point = zone.Position; // Zaten korunuyor: orada kal.
                    return true;
                }

                if (zone.Free <= 0) continue;

                float distance = (zone.Position - position).magnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    point = zone.Position;
                    found = true;
                }
            }
        }

        return found;
    }
}
