using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arazinin tehlikeleri: lavda ve yanan karoda duran oyuncuya aralıklarla hasar, uçuruma düşene hasar ve en yakın güvenli kareye dönüş.
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
    }

    readonly TerrainMap map;
    readonly FireSim fire;
    readonly GameConfig config;
    readonly Dictionary<int, PlayerTimers> timers = new Dictionary<int, PlayerTimers>();

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

            Vector2? position = positionOf(player.Id);
            if (!player.IsAlive || player.Status.IsFlying || position == null)
            {
                t.LavaLeft = 0f;
                t.FireLeft = 0f;
                continue;
            }

            Vector2Int cell = map.CellOf(position.Value);
            TileType type = map.At(cell.x, cell.y);

            if (type == TileType.Chasm && t.FallCooldown <= 0f) Fall(player, position.Value, t, teleport);

            // Lav ve ateş: girişte hemen, sonra her aralıkta bir hasar. Çıkınca sayaç sıfırlanır.
            t.LavaLeft = TickDamage(type == TileType.Lava, t.LavaLeft, dt, config.lavaDamageIntervalSeconds, config.lavaDamage, player);
            bool onFire = fire != null && fire.IsBurning(cell.x, cell.y);
            t.FireLeft = TickDamage(onFire, t.FireLeft, dt, config.terrainFireDamageIntervalSeconds, config.terrainFireDamage, player);
        }
    }

    static float TickDamage(bool inside, float left, float dt, float interval, int damage, PlayerState player)
    {
        if (!inside) return 0f;

        left -= dt;
        if (left > 0f) return left;

        player.TakeDamage(damage);
        return Mathf.Max(0.1f, interval);
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
