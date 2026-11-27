using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir maçın arazisi: harita, yangın, tehlikeler ve engeller bir arada. Yaratıkların (Ateş, Ejderha) tutuşturmasını ve boss'ların ev
/// yıkmasını (<see cref="HouseDamage"/>) da buradan yürütür.
/// Tek kişilik sahne (<see cref="MatchBootstrap"/>) ve ağ maçı (<see cref="NetMatchSim"/>) aynı sınıfı kullanır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public class TerrainSim
{
    readonly GameConfig config;
    readonly HashSet<CreatureModel> struckAlready = new HashSet<CreatureModel>();
    readonly List<CreatureModel> staleModels = new List<CreatureModel>();
    readonly List<Vector2Int> bodyCells = new List<Vector2Int>();
    readonly HouseDamageResult damage = new HouseDamageResult();

    public TerrainMap Map { get; }
    public FireSim Fire { get; }
    public TerrainHazards Hazards { get; }

    /// <summary>Yalnızca içinden geçilemeyen karolar (ev duvarları, kaktüs): itilen/fırlatılan karakterler için.</summary>
    public IObstacleField SolidObstacles { get; }

    /// <summary>Duvar + kaktüs + lav + uçurum + su + kum + yanan karolar: botların kendi isteğiyle yürürken kaçındığı yerler.</summary>
    public IObstacleField AvoidingObstacles { get; }

    /// <summary>Haritada bir şey değişti (karo, yanma ya da ev listesi). Ağ katmanı bunu görünce yeni durumu yayar.</summary>
    public bool Dirty { get; set; }

    /// <summary>
    /// Bu ev şu an yıkılamaz mı? Aktif küresel tehdidin sığınağı olan evler (bkz. <see cref="RefugeFinder.IsHouseRefuge"/>) tehdit
    /// sürerken yıkılmaz: tehdidin çözümü haritada kalmalı. Tehdit yöneticisi bağlar; boşsa hiçbir ev korunmaz.
    /// </summary>
    public Func<House, bool> IsHouseProtected { get; set; }

    /// <summary>Boss bir evin duvarını yıktı ya da evi çökertti (sonuç bir sonraki yıkıma kadar geçerlidir).</summary>
    public event Action<HouseDamageResult> HousesDamaged;

    /// <summary>Tohumdan yeni köy haritası üretir (ilk harita).</summary>
    public TerrainSim(GameConfig config, int seed, System.Random random)
        : this(config, seed, MapTheme.Village, random)
    {
    }

    /// <summary>Tohumdan verilen temada yeni harita üretir.</summary>
    public TerrainSim(GameConfig config, int seed, MapTheme theme, System.Random random)
        : this(config, MapGenerator.Generate(Mathf.RoundToInt(config.arenaSize.x), Mathf.RoundToInt(config.arenaSize.y), seed,
                                             MapRecipe.For(theme ?? MapTheme.Village)), random)
    {
    }

    /// <summary>Maçın teması: ayarlarda zorlanan tema, yoksa tohumdan rastgele (<see cref="MapTheme.Choose"/>).</summary>
    public static MapTheme ThemeFor(GameConfig config, int seed) => MapTheme.Choose(config != null ? config.forcedMapTheme : null, seed);

    /// <summary>Hazır haritayla (testler ve istemci için).</summary>
    public TerrainSim(GameConfig config, TerrainMap map, System.Random random)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Fire = new FireSim(map, config, random ?? throw new ArgumentNullException(nameof(random)));
        Hazards = new TerrainHazards(map, Fire, config);
        SolidObstacles = new TerrainObstacles(map, Fire, blockHazards: false);
        AvoidingObstacles = new TerrainObstacles(map, Fire, blockHazards: true, pricklyClearance: config.botCactusClearance);

        map.TileChanged += (_, __) => Dirty = true;
        map.HousesChanged += () => Dirty = true;
        Fire.BurnChanged += (_, __) => Dirty = true;
    }

    /// <summary>Nokta lav, uçurum ya da yanan bir karoda mı? (Savrulan karakter burada durur.)</summary>
    public bool IsHazardousAt(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        return TerrainMap.IsHazard(Map.At(cell.x, cell.y)) || Fire.IsBurning(cell.x, cell.y);
    }

    /// <summary>Nokta botun durmak istemeyeceği bir yerde mi: duvar, kaktüs, lav, uçurum, su, kum ya da alev.</summary>
    public bool IsDangerousAt(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        return TerrainMap.IsDangerous(Map.At(cell.x, cell.y)) || Fire.IsBurning(cell.x, cell.y);
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
        if (victim == null || !victim.IsAlive || victim.IsInvulnerable || victim.Status.IsFlying || victim.Falling != FallKind.None) return false;

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
    /// Bot gezinme hedefi için kötü yer mi: duvar, kaktüs, lav, uçurum, su, kum ya da yanan kare, veya bunlardan birine bitişik
    /// (kenarda takılmasın).
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
                if (TerrainMap.IsDangerous(type) || Fire.IsBurning(x, y)) return true;
            }
        }
        return false;
    }

    /// <summary>Dünya noktası güvenli zeminde değil mi (duvar, kaktüs, lav, uçurum, su, kum)? (Kule/sığınak yerleşimi buradan kaçınır.)</summary>
    public bool IsBlocked(Vector2 point) => Map.IsBlockedPoint(point);

    /// <summary>
    /// Noktayı en yakın güvenli (yürünür, yanmayan) karoya taşır; nokta zaten güvenliyse aynen verir.
    /// Başlangıç yerleri (karakterler, botlar) bununla ayarlanır.
    /// </summary>
    public Vector2 NearestSafe(Vector2 point)
    {
        Vector2Int cell = Map.CellOf(point);
        if (TerrainMap.IsSafeGround(Map.At(cell.x, cell.y)) && !IsBadSpawnCell(cell.x, cell.y)) return point;

        // Önce kaktüse komşu olmayan kare; hiç yoksa (çok küçük harita) yalnızca yanmayan kare.
        if (Map.TryFindNearestWalkable(point, out Vector2 safe, IsBadSpawnCell)) return safe;
        return Map.TryFindNearestWalkable(point, out safe, Fire.IsBurning) ? safe : point;
    }

    /// <summary>Doğma için kötü kare: yanıyor ya da bir kaktüse komşu (8 yön).</summary>
    bool IsBadSpawnCell(int x, int y) => Fire.IsBurning(x, y) || IsNextToPrickly(x, y);

    /// <summary>Karonun 8 komşusundan biri kaktüs mü?</summary>
    public bool IsNextToPrickly(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && Map.InBounds(x + dx, y + dy) && TerrainMap.IsPrickly(Map.At(x + dx, y + dy))) return true;
        return false;
    }

    /// <summary>
    /// Zamanı ilerletir: boss'ların ev yıkması, yangın, yaratıkların tutuşturması ve tehlikelerin hasarı.
    /// </summary>
    public void Tick(float deltaSeconds, IEnumerable<CreatureModel> creatures, IReadOnlyList<PlayerState> players,
                     Func<int, Vector2?> positionOf, Action<int, Vector2> teleport)
    {
        DemolishWithCreatures(creatures);
        IgniteFromCreatures(deltaSeconds, creatures);
        Fire.Tick(deltaSeconds);
        Hazards.Tick(deltaSeconds, players, positionOf, teleport);
    }

    /// <summary>
    /// Ev yıkan yaratıklar (boss): gövdesinin (yarıçap + <see cref="GameConfig.bossWallBreakReach"/>) değdiği ev duvarları yıkılır,
    /// oranı aşan ev çöker. Yıkılan duvar yürünür zemin olur; boss bir sonraki adımda oradan geçer. Sığınak evleri korunur.
    /// </summary>
    public void DemolishWithCreatures(IEnumerable<CreatureModel> creatures)
    {
        if (creatures == null || Map.Houses.Count == 0) return;

        foreach (CreatureModel creature in creatures)
        {
            if (!HouseDamage.CanBreakHouses(creature)) continue;

            HouseDamage.CellsUnderBody(Map, creature.Position, creature.Radius + config.bossWallBreakReach, bodyCells);
            if (HouseDamage.Apply(Map, bodyCells, config.bossHouseCollapseRatio, IsHouseProtected, damage))
                HousesDamaged?.Invoke(damage);
            if (Map.Houses.Count == 0) return;
        }
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
