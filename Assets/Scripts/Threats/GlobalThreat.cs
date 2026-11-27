using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Global tehdidin aşamaları.</summary>
public enum ThreatPhase
{
    /// <summary>Uyarı süresi: çözüm alanları görünür, oyuncular sığınmaya çalışır.</summary>
    Warning,

    /// <summary>Etki başladı (patlama, dalga...).</summary>
    Active,

    /// <summary>Bitti; çözüm alanları kaldırıldı.</summary>
    Finished
}

/// <summary>
/// Bir global tehdit (atom bombası, tsunami...). Kelime kabul edilince uyarı süresi başlar ve çözüm alanları çıkar;
/// süre dolunca etkiler (etiketlerden) başlar; bitince kısa bir toparlanma süresi sonra alanlar kalkar.
/// Düz C# sınıfı; ekran ve girdi bağlantısı <see cref="ThreatDirector"/>'dadır.
/// </summary>
public class GlobalThreat
{
    readonly ThreatContext context;
    readonly System.Func<Vector2, bool> isBlockedPoint;
    readonly List<IThreatEffect> effects;
    float lingerLeft;
    bool effectsFinished;

    public WordEntry Word => context.Word;
    public PlayerState Writer => context.Writer;
    public SafeZoneField Zones => context.Zones;
    public ThreatPhase Phase { get; private set; }

    /// <summary>Etkilerin hepsi işini bitirdi mi? (Toparlanma süresi ayrıdır.)</summary>
    public bool EffectsFinished => effectsFinished;

    /// <summary>Tehdidin etkileri (ekranda çizilecek olanlar için).</summary>
    public IReadOnlyList<IThreatEffect> Effects => effects;

    /// <summary>Etkinin başlamasına kalan süre (sn). Etkin ya da bittiyse 0.</summary>
    public float WarningLeft { get; private set; }

    /// <summary>Etkiler başladığı anda bir kez çağrılır (ekran parlaması için).</summary>
    public event Action Impacted;

    public GlobalThreat(WordEntry word, PlayerState writer, GameConfig config, ArenaBounds arena,
                        IPlayerLocator world, System.Random random, ICreatureSink creatures = null,
                        System.Func<Vector2, bool> isBlockedPoint = null)
    {
        this.isBlockedPoint = isBlockedPoint;
        if (word == null) throw new ArgumentNullException(nameof(word));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (arena == null) throw new ArgumentNullException(nameof(arena));
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (random == null) throw new ArgumentNullException(nameof(random));

        var zones = new SafeZoneField();
        context = new ThreatContext(word, writer, config, arena, world, random, zones, creatures);
        effects = TagRegistry.CreateThreatEffects(word);

        PlaceZones();

        // Uyarı süresi yalnızca ani felaketlerde (patlama, dalga) gerekir; yaratıklar hemen görünür.
        WarningLeft = effects.Exists(e => e.NeedsWarning) ? ThreatSpec.GetWarningSeconds(word, config) : 0f;
        Phase = ThreatPhase.Warning;
        if (WarningLeft <= 0f) Activate();
    }

    void PlaceZones()
    {
        GameConfig config = context.Config;
        int alive = context.World.Players.Count(p => p.IsAlive);

        int count = ThreatSpec.GetZoneCount(Word, alive, config);
        if (count == 0) return;

        int capacity = ThreatSpec.GetZoneCapacity(Word, alive, config);
        List<Vector2> positions = ZonePlacement.Pick(context.Arena, count, config.safeZoneEdgeMargin, config.safeZoneSeparation, context.Random, isBlockedPoint);

        foreach (Vector2 position in positions)
            context.Zones.Add(new SafeZone(Word.counter.type, position, config.safeZoneRadius, capacity, float.MaxValue));
    }

    /// <summary>Zamanı ilerletir: korunanları günceller, uyarıyı sayar, etkileri çalıştırır.</summary>
    public void Tick(float deltaSeconds)
    {
        if (Phase == ThreatPhase.Finished) return;

        float dt = Mathf.Max(0f, deltaSeconds);
        context.Zones.Update(context.World, dt);
        ExtinguishSheltered();

        if (Phase == ThreatPhase.Warning)
        {
            WarningLeft -= dt;
            if (WarningLeft <= 0f)
            {
                WarningLeft = 0f;
                Activate();
            }
            return;
        }

        // Etkin: etkiler çalışır; hepsi bitince toparlanma süresi, sonra son.
        if (!effectsFinished)
        {
            bool allDone = true;
            foreach (IThreatEffect effect in effects)
            {
                if (effect.IsFinished) continue;
                effect.Tick(dt);
                if (!effect.IsFinished) allDone = false;
            }

            if (allDone)
            {
                effectsFinished = true;
                lingerLeft = context.Config.safeZoneLingerSeconds;
            }
            return;
        }

        lingerLeft -= dt;
        if (lingerLeft <= 0f)
        {
            Phase = ThreatPhase.Finished;
            context.Zones.Clear();
        }
    }

    /// <summary>
    /// Çözüm alanı tehdidin bütün hasar türlerinden korur: içeride korunan oyuncunun üstündeki alev söner
    /// (ateş alanında tutuşup mağaraya kaçan yanmaya devam etmesin). Yerdeki ateşi <see cref="TerrainHazards.IsSheltered"/> keser.
    /// </summary>
    void ExtinguishSheltered()
    {
        foreach (PlayerState player in context.World.Players)
            if (player.IsAlive && player.Status.IsBurning && context.Zones.IsProtected(player.Id)) player.Status.Extinguish();
    }

    void Activate()
    {
        // Korunanlar en güncel hâlinde olsun diye etkiden hemen önce bir kez daha güncelle.
        context.Zones.Update(context.World, 0f);

        Phase = ThreatPhase.Active;
        foreach (IThreatEffect effect in effects)
            effect.Begin(context);

        effectsFinished = effects.TrueForAll(e => e.IsFinished);
        if (effectsFinished) lingerLeft = context.Config.safeZoneLingerSeconds;

        Impacted?.Invoke();
    }
}
