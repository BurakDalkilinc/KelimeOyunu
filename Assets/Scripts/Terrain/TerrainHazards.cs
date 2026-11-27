using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arazinin tehlikeleri: lav/uçuruma düşme, yanan karoda, dondurucu suda ve bataklık kumunda duran oyuncuya aralıklarla hasar,
/// kaktüse dokunana küçük hasar.
/// Büyük tehdidin çözüm alanında (mağara, sığınak...) korunan oyuncuya yerdeki ateş işlemez (bkz. <see cref="IsSheltered"/>): çözüm alanı tehdidin bütün hasar türlerinden korur.
/// Kalkan hasarı engeller (hasar <see cref="PlayerState.TakeDamage"/> yolundan gider). Uçan ve elenmiş oyuncuya hiçbir şey işlemez.
/// Düz C# sınıfı: konumu ve ışınlamayı dışarıdan (tek kişilik sahne ya da ağ) alır, test edilebilir.
/// </summary>
public class TerrainHazards
{
    sealed class PlayerTimers
    {
        public float LavaLeft;
        public float FireLeft;
        public float FallCooldown;
        public float DoomLeft;

        /// <summary>Hasar veren zeminde (su, kum) bir sonraki hasara kalan süre; zeminin dışındayken negatif.</summary>
        public float GroundLeft = -1f;
        public float CactusCooldown;

        /// <summary>Son karede kaktüse değiyor muydu? Hasar yalnızca temasın başladığı anda verilir (sürtünmek öldürmez).</summary>
        public bool TouchingCactus;
    }

    readonly TerrainMap map;
    readonly FireSim fire;
    readonly GameConfig config;
    readonly Dictionary<int, PlayerTimers> timers = new Dictionary<int, PlayerTimers>();

    /// <summary>
    /// Oyuncu şu an bir tehdidin çözüm alanında korunuyor mu? Tehdit yöneticisi bağlar (<see cref="RefugeFinder.IsSheltered"/>);
    /// boşsa kimse korunmaz.
    /// </summary>
    public Func<PlayerState, bool> IsSheltered { get; set; }

    /// <summary>Bir oyuncu uçuruma düştü: oyuncunun numarası, düştüğü karenin merkezi ve ışınlandığı güvenli nokta.</summary>
    public event Action<int, Vector2> Fell;

    public TerrainHazards(TerrainMap map, FireSim fire, GameConfig config)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.fire = fire;
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <param name="positionOf">Oyuncunun konumu; bilinmiyorsa null.</param>
    /// <param name="teleport">Düşen oyuncuyu güvenli noktaya götürür.</param>
    public void Tick(float deltaSeconds, IReadOnlyList<PlayerState> players, Func<int, Vector2?> positionOf, Action<int, Vector2> teleport)
    {
        float dt = Mathf.Max(0f, deltaSeconds);

        foreach (PlayerState player in players)
        {
            PlayerTimers t = TimersOf(player.Id);
            t.FallCooldown = Mathf.Max(0f, t.FallCooldown - dt);
            t.CactusCooldown = Mathf.Max(0f, t.CactusCooldown - dt);

            if (player.Falling != FallKind.None)
            {
                // Düşüyor: normal lav/uçurum kuralları işlemez, süre bitince can tamamen gider.
                if (!player.IsAlive) { player.Falling = FallKind.None; continue; }

                t.DoomLeft -= dt;
                if (t.DoomLeft <= 0f) player.Eliminate();
                continue;
            }

            Vector2? position = positionOf(player.Id);
            bool sheltered = player.IsAlive && IsSheltered != null && IsSheltered(player); // Mağaradakine yerdeki ateş işlemez

            if (!player.IsAlive || player.Status.IsFlying || position == null)
            {
                t.LavaLeft = 0f;
                t.FireLeft = 0f;
                t.GroundLeft = -1f;
                t.TouchingCactus = false;
                continue;
            }

            Vector2Int cell = map.CellOf(position.Value);
            TileType type = map.At(cell.x, cell.y);

            // Lava ya da uçuruma düşen (itilen ya da kendi yürüyen) düşme animasyonuyla bütün canını kaybeder.
            if (type == TileType.Chasm) { StartFall(player, FallKind.Chasm); continue; }
            if (type == TileType.Lava) { StartFall(player, FallKind.Lava); continue; }

            // Ateş: girişte hemen, sonra her aralıkta bir hasar. Çıkınca sayaç sıfırlanır.
            bool onFire = !sheltered && fire != null && fire.IsBurning(cell.x, cell.y);
            t.FireLeft = TickFire(onFire, t.FireLeft, dt, player);

            // Dondurucu su / bataklık kumu: içinde durdukça aralıklarla hasar. Kaktüs: dokununca hasar, sonra kısa bekleme.
            t.GroundLeft = TickGround(type, t.GroundLeft, dt, player);
            TickCactus(player, position.Value, t, teleport);
        }
    }

    /// <summary>
    /// Kaktüs: temas yeni başladıysa (ve bekleme bittiyse) bir kez batar ve karakteri kaktüsten biraz geri iter.
    /// Kaktüse sürtünerek yürümek tekrar tekrar hasar vermez; ayrılıp yeniden değmek gerekir.
    /// </summary>
    void TickCactus(PlayerState player, Vector2 position, PlayerTimers t, Action<int, Vector2> teleport)
    {
        Vector2 away = Vector2.zero;
        bool touching = config.cactusDamage > 0 && TryTouchCactus(position, out away);
        if (touching && !t.TouchingCactus && t.CactusCooldown <= 0f)
        {
            player.TakeDamage(config.cactusDamage);
            t.CactusCooldown = Mathf.Max(0.1f, config.cactusDamageCooldownSeconds);

            // Geri itme: yalnızca güvenli bir kareye (duvara, kuma, lava değil).
            Vector2 pushedTo = position + away * config.cactusPushDistance;
            if (config.cactusPushDistance > 0f && teleport != null && TerrainMap.IsSafeGround(map.At(pushedTo)))
            {
                teleport(player.Id, pushedTo);
                touching = false;
            }
        }
        t.TouchingCactus = touching;
    }

    /// <summary>Hasar veren zeminde: girişte ilk beklemeyi kurar, süre dolunca vurur. Zeminin dışında -1 döner.</summary>
    float TickGround(TileType type, float left, float dt, PlayerState player)
    {
        if (!GroundRules.TryGetGroundDamage(type, config, out int damage, out float interval, out float firstDelay)) return -1f;

        if (left < 0f) left = firstDelay; // Yeni girdi
        left -= dt;
        if (left > 0f) return left;

        player.TakeDamage(damage);
        return interval;
    }

    /// <summary>Karakterin (daire) kenarı bir kaktüs karosuna değiyor mu? Değiyorsa kaktüsten uzaklaşma yönünü verir.</summary>
    bool TryTouchCactus(Vector2 position, out Vector2 away)
    {
        away = Vector2.zero;
        float reach = config.playerRadius + config.cactusTouchDistance;
        Vector2Int cell = map.CellOf(position);
        int cells = Mathf.CeilToInt(reach) + 1;

        for (int y = cell.y - cells; y <= cell.y + cells; y++)
        {
            for (int x = cell.x - cells; x <= cell.x + cells; x++)
            {
                if (!map.InBounds(x, y) || !TerrainMap.IsPrickly(map.At(x, y))) continue;

                Vector2 min = new Vector2(map.Origin.x + x, map.Origin.y + y);
                Vector2 closest = new Vector2(Mathf.Clamp(position.x, min.x, min.x + 1f), Mathf.Clamp(position.y, min.y, min.y + 1f));
                if ((closest - position).sqrMagnitude > reach * reach) continue;

                Vector2 delta = position - closest;
                if (delta.sqrMagnitude < 0.000001f) delta = position - map.CellCenter(x, y); // Merkez karonun içinde
                away = delta.sqrMagnitude < 0.000001f ? Vector2.up : delta.normalized;
                return true;
            }
        }
        return false;
    }

    /// <summary>Yanan karodaki oyuncu: girişte hemen, sonra her aralıkta ateş hasarı alır ve tutuşur. Çıkınca sayaç sıfırlanır.</summary>
    float TickFire(bool inside, float left, float dt, PlayerState player)
    {
        if (!inside) return 0f;

        left -= dt;
        if (left > 0f) return left;

        player.TakeFireDamage(config.terrainFireDamage, config);
        return Mathf.Max(0.1f, config.terrainFireDamageIntervalSeconds);
    }

    void Fall(PlayerState player, Vector2 position, PlayerTimers t, Action<int, Vector2> teleport)
    {
        t.FallCooldown = config.chasmFallCooldownSeconds;
        player.TakeDamage(config.chasmFallDamage);

        // Yanan karelere dönme: güvenli, yürünür ve yanmayan en yakın kare.
        Func<int, int, bool> burning = fire != null ? (Func<int, int, bool>)fire.IsBurning : null;
        if (!map.TryFindNearestWalkable(position, out Vector2 safe, burning)) return;

        teleport?.Invoke(player.Id, safe);
        Fell?.Invoke(player.Id, safe);
    }

    /// <summary>
    /// Oyuncu lava ya da uçuruma itilip içinde kaldı: <see cref="GameConfig.fallSeconds"/> sonra bütün canı gider (arada animasyon oynar).
    /// </summary>
    public void StartFall(PlayerState player, FallKind kind)
    {
        if (player == null || !player.IsAlive || player.IsInvulnerable || player.Falling != FallKind.None || kind == FallKind.None) return;

        player.Falling = kind;
        TimersOf(player.Id).DoomLeft = config.fallSeconds;
    }

    PlayerTimers TimersOf(int id)
    {
        if (!timers.TryGetValue(id, out PlayerTimers t))
        {
            t = new PlayerTimers();
            timers[id] = t;
        }
        return t;
    }
}
