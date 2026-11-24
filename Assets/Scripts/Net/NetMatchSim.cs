using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>Ağ maçındaki karakterlerin sahnedeki halini veren/değiştiren kaynak (host'ta <see cref="NetworkPlayer"/>'lar).</summary>
public interface INetAvatars
{
    bool TryGetPosition(int playerId, out Vector2 position);
    bool TryApplyKnockback(int playerId, Vector2 distance);

    /// <summary>Karakterin baktığı (son yürüdüğü) yön; bilinmiyorsa false.</summary>
    bool TryGetFacing(int playerId, out Vector2 direction);

    /// <summary>Karakteri verilen noktaya ışınlar (uçuruma düşen güvenli kareye döner). Yapılamıyorsa false.</summary>
    bool TryTeleport(int playerId, Vector2 position) => false;
}

/// <summary>
/// Ağ maçının host tarafındaki yaratık dünyası: kabul edilen kelimeyi yaratığa çevirir, her karede ilerletir,
/// can/hasar/yavaşlama/fırlatmayı mevcut kurallarla (<see cref="CreatureModel"/>, <see cref="CreatureDriver"/>) uygular
/// ve istemcilere gidecek <see cref="CreatureSnapshot"/> listesini verir. Kuralların hiçbiri burada yeniden yazılmadı.
/// Düz C# sınıfı; zamanı dışarıdan alır, ağ kütüphanesini bilmez.
/// </summary>
public class NetMatchSim : ICreatureWorld, ICreatureSink, IRefugeProvider, IRevivalService
{
    sealed class SimCreature
    {
        public int Id;
        public CreatureModel Model;
        public BotBrain Wander;
        public string Label;
    }

    readonly GameConfig config;
    readonly MatchState match;
    readonly INetAvatars avatars;
    readonly ArenaBounds arena;
    readonly Random random;
    readonly List<SimCreature> creatures = new List<SimCreature>();
    readonly List<DangerZone> dangers = new List<DangerZone>();
    readonly List<CreatureHit> hits = new List<CreatureHit>();
    readonly Dictionary<WallSegment, int> wallIds = new Dictionary<WallSegment, int>();
    readonly List<GlobalThreat> threats = new List<GlobalThreat>();
    readonly Dictionary<SafeZone, int> zoneIds = new Dictionary<SafeZone, int>();
    int nextId = 1;
    int nextWallId = 1;
    int nextZoneId = 1;
    int impactCount;

    TargetSelection selection;
    WordEntry selectionWord;
    readonly GhostRevival revival;

    public NetMatchSim(GameConfig config, MatchState match, INetAvatars avatars, Random random, TerrainSim terrain = null)
    {
        Terrain = terrain;
        Obstacles = new CompositeObstacles(Walls, terrain?.SolidObstacles);
        BotObstacles = new CompositeObstacles(Walls, terrain?.AvoidingObstacles);
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.match = match ?? throw new ArgumentNullException(nameof(match));
        this.avatars = avatars ?? throw new ArgumentNullException(nameof(avatars));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        arena = new ArenaBounds(config.arenaSize);
        revival = new GhostRevival(match.Players, config, random);
    }

    /// <summary>Devam eden büyük tehditler (uyarıda ya da etkisi sürenler).</summary>
    public IReadOnlyList<GlobalThreat> Threats => threats;

    /// <summary>Maçın arazisi (harita, yangın, lav, uçurum); yoksa null (düz arena).</summary>
    public TerrainSim Terrain { get; }

    /// <summary>Herkesin içinden geçemediği engeller: oyuncu duvarları + haritadaki tahta duvarlar.</summary>
    public IObstacleField Obstacles { get; }

    /// <summary>Botların kendi isteğiyle yürürken kaçındığı engeller: <see cref="Obstacles"/> + lav, uçurum, yanan kareler.</summary>
    public IObstacleField BotObstacles { get; }

    /// <summary>Haritadaki duvarlar. Karakterler ve yerdeki yaratıklar bunların içinden geçemez.</summary>
    public WallField Walls { get; } = new WallField();

    /// <summary>Bir insan yazan hedef seçmeye başladı (yazanın numarası, süre). Ağ katmanı herkese duyurur.</summary>
    public event Action<int, float> TargetingStarted;

    /// <summary>Hedef seçimi bitti (seçildi, süre doldu ya da iptal oldu).</summary>
    public event Action TargetingEnded;

    /// <summary>Şu an bir insan oyuncu hedef seçiyor mu?</summary>
    public bool IsSelectingTarget => selection != null && selection.IsPending;

    /// <summary>Hedef seçen oyuncunun numarası; seçim yoksa -1.</summary>
    public int SelectingWriterId => IsSelectingTarget ? selection.Writer.Id : -1;

    /// <summary>Canlı yaratık sayısı.</summary>
    public int CreatureCount => creatures.Count;

    /// <summary>Botların kaçacağı alanlar (canlı ve zararlı yaratıklar). Her <see cref="Tick"/>'te yenilenir.</summary>
    public IReadOnlyList<DangerZone> Dangers => dangers;

    /// <summary>Numarasına göre oyuncunun maç durumu (can, etkiler); yoksa null.</summary>
    public PlayerState FindPlayer(int playerId)
    {
        foreach (PlayerState player in match.Players)
            if (player.Id == playerId) return player;
        return null;
    }

    // ---- ICreatureWorld ----

    IReadOnlyList<PlayerState> IPlayerLocator.Players => match.Players;

    bool IPlayerLocator.TryGetPosition(int playerId, out Vector2 position) => avatars.TryGetPosition(playerId, out position);

    bool ICreatureWorld.TryApplyKnockback(int playerId, Vector2 distance) => avatars.TryApplyKnockback(playerId, distance);

    // ---- Kelime → yaratık ----

    /// <summary>
    /// Kabul edilen kelimeyi uygular. Saldırı kelimesinde hedefi insan yazan seçer (<see cref="TryChooseTarget"/>;
    /// süre dolarsa rastgele), bot yazan hemen rastgele bir rakip seçer. Koruma kelimesi yazana uygulanır.
    /// Bilinmeyen kelime zararsız "???" olur. (Büyük tehditler sonraki adımda.)
    /// </summary>
    public void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        if (word == null)
        {
            Add(CreatureBuilder.BuildUnknown(config, arena, random), "???", true);
            return;
        }

        if (word.category == WordEntry.CategoryAttack && word.scope == WordEntry.ScopeTarget)
            BeginAttack(writer, word);
        else if (word.category == WordEntry.CategoryDefense && word.scope == WordEntry.ScopeSelf)
            ApplyDefense(writer, word);
        else if (word.category == WordEntry.CategoryThreat && word.scope == WordEntry.ScopeGlobal)
            BeginThreat(writer, word);
    }

    /// <summary>Büyük tehdidi başlatır: çözüm alanları çıkar, uyarı sayılır, süre dolunca etki başlar (mevcut <see cref="GlobalThreat"/>).</summary>
    void BeginThreat(PlayerState writer, WordEntry word)
    {
        var threat = new GlobalThreat(word, writer, config, arena, this, new Random(random.Next()), this,
                                      Terrain != null ? (Func<Vector2, bool>)Terrain.IsBlocked : null);
        // Ekran parlaması yalnızca çözüm alanı olan (sığınılan) tehditlerde; meteor yağmuru yavaş yavaş başlar.
        if (ThreatSpec.HasZones(word)) threat.Impacted += () => impactCount++;
        threats.Add(threat);
    }

    // ---- ICreatureSink (tehdit etkileri yaratık koyar: meteor, zombi sürüsü, ejderha) ----

    void ICreatureSink.Spawn(CreatureModel model, string label)
    {
        Add(model, label, false);
    }

    // ---- IRefugeProvider (botlar en yakın boş sığınağa koşar) ----

    bool IRefugeProvider.IsSheltered(PlayerState player) => RefugeFinder.IsSheltered(threats, player);

    bool IRefugeProvider.TryGetRefuge(PlayerState player, Vector2 position, out Vector2 point)
        => RefugeFinder.TryGetRefuge(threats, player, position, out point);

    void BeginAttack(PlayerState writer, WordEntry word)
    {
        // Önceki seçim bitmeden yenisi gelirse, öncekini rastgele bir hedefle tamamla.
        if (IsSelectingTarget) CompleteSelection(forceRandom: true);

        if (writer.IsBot)
        {
            PlayerState target = TargetPicker.PickRandomOpponent(match.Players, writer, random);
            if (target != null) SpawnAttack(writer, word, target);
            return;
        }

        selection = new TargetSelection(writer, config.targetSelectionSeconds);
        selectionWord = word;
        TargetingStarted?.Invoke(writer.Id, config.targetSelectionSeconds);
    }

    /// <summary>
    /// Hedef seçen oyuncunun seçimini uygular. <paramref name="senderPlayerId"/> ağ katmanının bağlantıdan bulduğu numaradır;
    /// seçen yazan değilse, süre bitmişse ya da hedef geçersizse (kendisi, elenmiş, görünmez) reddedilir.
    /// </summary>
    public bool TryChooseTarget(int senderPlayerId, int targetPlayerId)
    {
        if (!IsSelectingTarget || selection.Writer.Id != senderPlayerId) return false;
        if (!selection.TrySelect(FindPlayer(targetPlayerId))) return false;

        CompleteSelection(forceRandom: false);
        return true;
    }

    /// <summary>
    /// Bir oyuncu bota dönüştü (bağlantısı koptu). Hedef seçiyorsa seçim rastgele bir rakiple tamamlanır,
    /// çünkü artık kimse tıklayamaz. Oyuncunun kendisinin bota çevrilmesi <see cref="NetTurnHost.ReplaceWithBot"/>'tadır.
    /// </summary>
    public void OnPlayerBecameBot(int playerId)
    {
        if (IsSelectingTarget && selection.Writer.Id == playerId) CompleteSelection(forceRandom: true);
    }

    void CompleteSelection(bool forceRandom)
    {
        PlayerState writer = selection.Writer;
        WordEntry word = selectionWord;
        PlayerState target = selection.State == TargetSelectionState.Selected && !forceRandom
            ? selection.Chosen
            : TargetPicker.PickRandomOpponent(match.Players, writer, random);

        selection = null;
        selectionWord = null;
        TargetingEnded?.Invoke();

        if (target != null) SpawnAttack(writer, word, target);
    }

    void SpawnAttack(PlayerState writer, WordEntry word, PlayerState target)
    {
        // Hedef seçilirken elenmiş olabilir; öyleyse başka bir rakip seç.
        if (!target.IsAlive)
        {
            target = TargetPicker.PickRandomOpponent(match.Players, writer, random);
            if (target == null) return;
        }

        if (!avatars.TryGetPosition(target.Id, out Vector2 targetPosition)) return;

        List<CreatureModel> models = CreatureBuilder.BuildAttack(word, target, targetPosition, config, arena, random);
        for (int i = 0; i < models.Count; i++)
            Add(models[i], models.Count == 1 || i == 0 ? word.display : "", false);
    }

    /// <summary>Koruma kelimeleri yazana uygulanır: can, kalkan, görünmezlik, uçma, duvar...</summary>
    void ApplyDefense(PlayerState writer, WordEntry word)
    {
        avatars.TryGetPosition(writer.Id, out Vector2 writerPosition);
        Vector2 facing = avatars.TryGetFacing(writer.Id, out Vector2 f) ? f : Vector2.up;
        TagRegistry.ApplySelfEffects(word, new SelfEffectContext(writer, word, config, writerPosition, facing, Walls, this));
    }

    /// <summary>
    /// Silahla ateş (host'ta): insan baktığı yöne, bot menzildeki en yakın rakibe nişan alır. Atış yapıldıysa true.
    /// </summary>
    public bool TryFireWeapon(PlayerState shooter, Vector2 facing, bool onlyIfVictims)
    {
        if (shooter == null || !shooter.IsAlive || !shooter.Status.HasWeapon) return false;
        if (!avatars.TryGetPosition(shooter.Id, out Vector2 from)) return false;

        Vector2 direction = facing;
        if (onlyIfVictims && !WeaponFire.TryAimAtNearest(shooter, from, this, config.weaponRange * 0.8f, out direction)) return false;

        if (!shooter.Status.TryUseWeaponShot(config.weaponShotIntervalSeconds, out WordEntry weapon)) return false;

        Add(WeaponFire.Build(shooter, weapon, from, direction, config, arena), string.Empty, false);
        return true;
    }

    // ---- IRevivalService: "yeniden canlanma" ----

    /// <summary>Elenenlerden rastgele biri 1 canla geri döner. Hayalet yoksa kelime boşa gider.</summary>
    void IRevivalService.RequestRevival(PlayerState writer)
    {
        revival.RequestRevival(writer);
    }

    void Add(CreatureModel model, string label, bool wanders)
    {
        model.Locator = this; // Hedefi ölünce ya da başkası yaklaşınca yeniden seçebilsin
        model.Obstacles = Obstacles; // Uçmayan yaratıklar duvardan (oyuncu duvarları ve ev duvarları) geçemez

        creatures.Add(new SimCreature
        {
            Id = nextId++,
            Model = model,
            Label = label,
            Wander = wanders || model.Target == null ? new BotBrain(config, arena, new Random(random.Next())) : null,
        });
    }

    // ---- Zaman ----

    /// <summary>Tüm yaratıkları ve oyuncu etkilerini (kalkan, yavaşlama...) <paramref name="deltaSeconds"/> kadar ilerletir.</summary>
    public void Tick(float deltaSeconds)
    {
        foreach (PlayerState player in match.Players)
        {
            if (player.IsGhost) player.Status.Clear(); // Elenenin üstünde etki kalmaz
            else player.Status.Tick(deltaSeconds);
        }

        Walls.Tick(deltaSeconds);
        TickSelection(deltaSeconds);

        for (int i = threats.Count - 1; i >= 0; i--)
        {
            threats[i].Tick(deltaSeconds);
            if (threats[i].Phase == ThreatPhase.Finished) threats.RemoveAt(i);
        }

        // Arazi: yangın, yaratıkların tutuşturması, lav/ateş/uçurum hasarı.
        Terrain?.Tick(deltaSeconds, Models(), match.Players,
            id => avatars.TryGetPosition(id, out Vector2 at) ? at : (Vector2?)null,
            (id, to) => avatars.TryTeleport(id, to));

        dangers.Clear();
        for (int i = creatures.Count - 1; i >= 0; i--)
        {
            CreatureModel model = creatures[i].Model;
            CreatureDriver.Advance(model, deltaSeconds, this, config, hits, creatures[i].Wander);

            if (model.IsFinished)
            {
                creatures.RemoveAt(i);
                continue;
            }

            // Sıkılıp giden avcı artık tehlike değil; botlar ondan kaçmak zorunda kalmaz.
            if (model.IsHarmful && !model.IsLeaving) dangers.Add(new DangerZone(model.Position, model.Radius));
        }

        Terrain?.AppendFireDangers(dangers); // Botlar alevden kaçar
    }

    IEnumerable<CreatureModel> Models()
    {
        foreach (SimCreature creature in creatures) yield return creature.Model;
    }

    void TickSelection(float deltaSeconds)
    {
        if (selection == null) return;

        // Yazan elendiyse ya da maç bittiyse seçim anlamını yitirir.
        if (!selection.Writer.IsAlive || match.IsOver)
        {
            selection = null;
            selectionWord = null;
            TargetingEnded?.Invoke();
            return;
        }

        selection.Tick(deltaSeconds);
        if (selection.State == TargetSelectionState.TimedOut) CompleteSelection(forceRandom: true);
    }

    // ---- İstemciye giden fotoğraf ----

    /// <summary>Şu anki tüm yaratıkların fotoğrafı (<paramref name="into"/> temizlenip doldurulur).</summary>
    public void Snapshot(List<CreatureSnapshot> into)
    {
        into.Clear();
        foreach (SimCreature creature in creatures)
        {
            CreatureModel m = creature.Model;
            into.Add(new CreatureSnapshot
            {
                Id = creature.Id,
                WordId = m.Word != null ? m.Word.id : "",
                X = m.Position.x,
                Y = m.Position.y,
                Radius = m.Radius,
                Kind = KindOf(m),
                Progress = m.IsStrike ? m.StrikeProgress : 0f,
                HasStruck = m.HasStruck,
                Countdown = m.ShowsCountdown && !m.IsSky && m.IsStrike && !m.HasStruck ? Mathf.CeilToInt(m.StrikeWarningLeft) : 0,
                Label = creature.Label,
            });
        }
    }

    /// <summary>Şu anki tüm çözüm alanlarının fotoğrafı (<paramref name="into"/> temizlenip doldurulur).</summary>
    public void SnapshotZones(List<ZoneSnapshot> into)
    {
        into.Clear();

        var alive = new HashSet<SafeZone>();
        foreach (GlobalThreat threat in threats)
        {
            foreach (SafeZone zone in threat.Zones.Zones)
            {
                alive.Add(zone);
                if (!zoneIds.TryGetValue(zone, out int id))
                {
                    id = nextZoneId++;
                    zoneIds[zone] = id;
                }

                into.Add(new ZoneSnapshot
                {
                    Id = id, Type = zone.Type, X = zone.Position.x, Y = zone.Position.y,
                    Radius = zone.Radius, Capacity = zone.Capacity, Occupied = zone.Occupants.Count,
                });
            }
        }

        // Kalkan alanların numarası unutulur.
        if (zoneIds.Count > alive.Count)
        {
            var stale = new List<SafeZone>();
            foreach (SafeZone known in zoneIds.Keys)
                if (!alive.Contains(known)) stale.Add(known);
            foreach (SafeZone gone in stale) zoneIds.Remove(gone);
        }
    }

    /// <summary>Tehdidin genel durumu: en acil alarm, etki anı sayacı ve ilerleyen dalga.</summary>
    public ThreatStatus GetThreatStatus()
    {
        var status = new ThreatStatus { AlarmWordId = "", ImpactCount = impactCount };

        // En acil uyarı: etkinleşmeye en az süresi kalan (ThreatDirector ile aynı seçim).
        GlobalThreat urgent = null;
        foreach (GlobalThreat threat in threats)
        {
            bool activeAndRunning = threat.Phase == ThreatPhase.Active && !threat.EffectsFinished;
            if (threat.Phase != ThreatPhase.Warning && !activeAndRunning) continue;
            if (urgent == null || threat.WarningLeft < urgent.WarningLeft) urgent = threat;
        }

        if (urgent != null)
        {
            status.AlarmWordId = urgent.Word.id;
            status.AlarmSeconds = Mathf.CeilToInt(urgent.WarningLeft);
        }

        foreach (GlobalThreat threat in threats)
        {
            foreach (IThreatEffect effect in threat.Effects)
            {
                if (effect is WaveEffect wave && wave.IsActive)
                {
                    status.WaveActive = true;
                    status.WaveDirX = wave.Direction.x;
                    status.WaveDirY = wave.Direction.y;
                    status.WaveFront = wave.Front;
                }
            }
        }

        return status;
    }

    /// <summary>Şu anki tüm duvarların fotoğrafı (<paramref name="into"/> temizlenip doldurulur).</summary>
    public void SnapshotWalls(List<WallSnapshot> into)
    {
        into.Clear();

        // Ömrü biten duvarların numarası unutulur.
        if (wallIds.Count > Walls.Walls.Count)
        {
            var alive = new HashSet<WallSegment>(Walls.Walls);
            var stale = new List<WallSegment>();
            foreach (WallSegment known in wallIds.Keys)
                if (!alive.Contains(known)) stale.Add(known);
            foreach (WallSegment gone in stale) wallIds.Remove(gone);
        }

        foreach (WallSegment wall in Walls.Walls)
        {
            if (!wallIds.TryGetValue(wall, out int id))
            {
                id = nextWallId++;
                wallIds[wall] = id;
            }

            into.Add(new WallSnapshot
            {
                Id = id,
                CenterX = wall.Center.x, CenterY = wall.Center.y,
                DirX = wall.Direction.x, DirY = wall.Direction.y,
                Length = wall.Length, Thickness = wall.Thickness,
                LifeRatio = wall.TotalLife > 0f ? Mathf.Clamp01(wall.LifeLeft / wall.TotalLife) : 0f,
            });
        }
    }

    static CreatureKind KindOf(CreatureModel m)
    {
        if (m.IsStrike) return CreatureKind.Strike;
        if (m.IsPull) return CreatureKind.Pull;
        if (m.IsTrap) return CreatureKind.Trap;
        if (m.IsArea) return m.BurnsArea ? CreatureKind.FireArea : CreatureKind.Area;
        return CreatureKind.Chaser;
    }
}
