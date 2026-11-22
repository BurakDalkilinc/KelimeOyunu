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
                        IPlayerLocator world, System.Random random, ICreatureSink creatures = null)
    {
        if (word == null) throw new ArgumentNullException(nameof(word));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (arena == null) throw new ArgumentNullException(nameof(arena));
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (random == null) throw new ArgumentNullException(nameof(random));

        var zones = new SafeZoneField();
        context = new ThreatContext(word, writer, config, arena, world, random, zones, creatures);
        effects = TagRegistry.CreateThreatEffects(word);

        PlaceZones();

        WarningLeft = ThreatSpec.GetWarningSeconds(word, config);
        Phase = ThreatPhase.Warning;
        if (WarningLeft <= 0f) Activate();
    }

    void PlaceZones()
    {
        int count = ThreatSpec.GetZoneCount(Word);
        if (count == 0) return;

        GameConfig config = context.Config;
        int capacity = ThreatSpec.GetZoneCapacity(Word, context.World.Players.Count(p => p.IsAlive), config);
        List<Vector2> positions = ZonePlacement.Pick(context.Arena, count, config.safeZoneEdgeMargin, config.safeZoneSeparation, context.Random);

        foreach (Vector2 position in positions)
            context.Zones.Add(new SafeZone(Word.counter.type, position, config.safeZoneRadius, capacity, float.MaxValue));
    }

    /// <summary>Zamanı ilerletir: korunanları günceller, uyarıyı sayar, etkileri çalıştırır.</summary>
    public void Tick(float deltaSeconds)
    {
        if (Phase == ThreatPhase.Finished) return;

        float dt = Mathf.Max(0f, deltaSeconds);
        context.Zones.Update(context.World, dt);

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
