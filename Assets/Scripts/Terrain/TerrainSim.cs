using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir maçın arazisi: harita, yangın, tehlikeler ve engeller bir arada. Yaratıkların (Ateş, Ejderha) tutuşturmasını da buradan yürütür.
/// Tek kişilik sahne (<see cref="MatchBootstrap"/>) ve ağ maçı (<see cref="NetMatchSim"/>) aynı sınıfı kullanır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public class TerrainSim
{
    readonly GameConfig config;
    readonly HashSet<CreatureModel> struckAlready = new HashSet<CreatureModel>();
    readonly List<CreatureModel> staleModels = new List<CreatureModel>();

    public TerrainMap Map { get; }
    public FireSim Fire { get; }
    public TerrainHazards Hazards { get; }

    /// <summary>Yalnızca içinden geçilemeyen karolar (tahta duvar): itilen/fırlatılan karakterler için.</summary>
    public IObstacleField SolidObstacles { get; }

    /// <summary>Duvar + lav + uçurum + yanan karolar: botların kendi isteğiyle yürürken kaçındığı yerler.</summary>
    public IObstacleField AvoidingObstacles { get; }

    /// <summary>Haritada bir şey değişti (karo ya da yanma). Ağ katmanı bunu görünce yeni durumu yayar.</summary>
    public bool Dirty { get; set; }

    /// <summary>Tohumdan yeni harita üretir.</summary>
    public TerrainSim(GameConfig config, int seed, System.Random random)
        : this(config, MapGenerator.Generate(Mathf.RoundToInt(config.arenaSize.x), Mathf.RoundToInt(config.arenaSize.y), seed, MapRecipe.Default), random)
    {
    }

    /// <summary>Hazır haritayla (testler ve istemci için).</summary>
    public TerrainSim(GameConfig config, TerrainMap map, System.Random random)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Fire = new FireSim(map, config, random ?? throw new ArgumentNullException(nameof(random)));
        Hazards = new TerrainHazards(map, Fire, config);
        SolidObstacles = new TerrainObstacles(map, Fire, blockHazards: false);
        AvoidingObstacles = new TerrainObstacles(map, Fire, blockHazards: true);

        map.TileChanged += (_, __) => Dirty = true;
        Fire.BurnChanged += (_, __) => Dirty = true;
    }

    /// <summary>Nokta lav, uçurum ya da yanan bir karoda mı? (Botlar buradan çıkmaya çalışır.)</summary>
    public bool IsHazardousAt(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        return TerrainMap.IsHazard(Map.At(cell.x, cell.y)) || Fire.IsBurning(cell.x, cell.y);
    }

    /// <summary>
    /// Kayan karakterin yolunu (from → to) küçük adımlarla tarar; ilk lav/uçurum/alevli karede durur.
    /// Böylece hızlı itilen biri ince bir uçurumun üstünden atlayıp geçemez. Tehlike yoksa <paramref name="to"/> aynen verilir.
    /// </summary>
    public Vector2 FirstHazardOnPath(Vector2 from, Vector2 to, out bool hit)
    {
        const float step = 0.2f;
        float length = Vector2.Distance(from, to);
        int steps = Mathf.CeilToInt(length / step);
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
            if (IsHazardousAt(point))
            {
                hit = true;
                return point;
            }
        }
        hit = false;
        return to;
    }

    /// <summary>
    /// İtilen oyuncu lava ya da uçuruma varıp içinde kaldıysa düşme başlar (animasyon, sonra bütün can gider). Başladıysa true.
    /// Yanan kare (alev) bunu başlatmaz; yalnızca lav ve uçurum.
    /// </summary>
    public bool TryStartFall(PlayerState victim, Vector2 landedAt)
    {
        if (victim == null || !victim.IsAlive || victim.Status.IsFlying || victim.Falling != FallKind.None) return false;

        TileType type = Map.At(landedAt);
        FallKind kind = type == TileType.Chasm ? FallKind.Chasm : type == TileType.Lava ? FallKind.Lava : FallKind.None;
        if (kind == FallKind.None) return false;

        Hazards.StartFall(victim, kind);
        return true;
    }

    /// <summary>Noktadan en yakın güvenli (yürünür, yanmayan) kareye doğru birim yön; güvenli yer yoksa sıfır.</summary>
    public Vector2 EscapeDirection(Vector2 point)
    {
        if (!Map.TryFindNearestWalkable(point, out Vector2 safe, Fire.IsBurning)) return Vector2.zero;
        Vector2 toward = safe - point;
        return toward.sqrMagnitude < 0.0001f ? Vector2.zero : toward.normalized;
    }

    /// <summary>Yanan karoları botların kaçacağı tehlike alanları olarak listeye ekler (en çok 60).</summary>
    public void AppendFireDangers(IList<DangerZone> into)
    {
        int added = 0;
        foreach (int index in Fire.BurningCells)
        {
            if (added++ >= 60) break;
            into.Add(new DangerZone(Map.CellCenter(index % Map.Width, index / Map.Width), 0.9f));
        }
    }

    /// <summary>
    /// Bot gezinme hedefi için kötü yer mi: duvar, lav, uçurum ya da yanan kare, veya bunlardan birine bitişik (kenarda takılmasın).
    /// </summary>
    public bool IsBadWanderTarget(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = cell.x + dx, y = cell.y + dy;
                if (!Map.InBounds(x, y)) continue;
                TileType type = Map.At(x, y);
                if (TerrainMap.IsSolid(type) || TerrainMap.IsHazard(type) || Fire.IsBurning(x, y)) return true;
            }
        }
        return false;
    }

    /// <summary>Dünya noktası duvar, lav ya da uçurum karosunda mı? (Kule/sığınak yerleşimi buradan kaçınır.)</summary>
    public bool IsBlocked(Vector2 point) => Map.IsBlockedPoint(point);

    /// <summary>
    /// Noktayı en yakın güvenli (yürünür, yanmayan) karoya taşır; nokta zaten güvenliyse aynen verir.
    /// Başlangıç yerleri (karakterler, botlar) bununla ayarlanır.
    /// </summary>
    public Vector2 NearestSafe(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        if (TerrainMap.IsWalkable(Map.At(cell.x, cell.y)) && !Fire.IsBurning(cell.x, cell.y)) return point;

        return Map.TryFindNearestWalkable(point, out Vector2 safe, Fire.IsBurning) ? safe : point;
    }

    /// <summary>
    /// Zamanı ilerletir: yangın, yaratıkların tutuşturması ve tehlikelerin hasarı.
    /// </summary>
    public void Tick(float deltaSeconds, IEnumerable<CreatureModel> creatures, IReadOnlyList<PlayerState> players,
                     Func<int, Vector2?> positionOf, Action<int, Vector2> teleport)
    {
        IgniteFromCreatures(deltaSeconds, creatures);
        Fire.Tick(deltaSeconds);
        Hazards.Tick(deltaSeconds, players, positionOf, teleport);
    }

    /// <summary>
    /// Tutuşturan yaratıklar: "Ateş" uyarısı dolup vurunca bir kez alanını tutuşturur; ejderha gibi sürekli ateş
    /// püskürtenler yarıçaplarındaki karoları saniyede <see cref="GameConfig.creatureIgniteChancePerSecond"/> olasılıkla tutuşturur.
    /// </summary>
    void IgniteFromCreatures(float dt, IEnumerable<CreatureModel> creatures)
    {
        if (creatures == null) return;

        foreach (CreatureModel creature in creatures)
        {
            if (creature == null || !creature.IgnitesTerrain || creature.IsFinished) continue;

            if (creature.IsStrike)
            {
                if (creature.HasStruck && struckAlready.Add(creature)) Fire.IgniteArea(creature.Position, creature.Radius);
            }
            else if (creature.BurnsArea)
            {
                float chance = 1f - Mathf.Exp(-config.creatureIgniteChancePerSecond * Mathf.Max(0f, dt));
                Fire.IgniteArea(creature.Position, creature.Radius * 0.8f, chance);
            }
        }

        // Bitmiş yaratıkların kaydını unut (bellek şişmesin).
        if (struckAlready.Count > 64)
        {
            staleModels.Clear();
            foreach (CreatureModel model in struckAlready)
                if (model.IsFinished) staleModels.Add(model);
            foreach (CreatureModel model in staleModels) struckAlready.Remove(model);
        }
    }
}
