using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yangın: yanan karo komşu tahta karolara yayılır, süresi dolunca tahta kömürleşir (duvar çöker, yürünür kömür zemin olur).
/// Lav, bitişiğindeki tahtayı ara sıra tutuşturur. Toprak/çimen yanmaz; üstüne düşen alev kısa süre kalır ve yayılmaz.
/// Düz C# sınıfı: zamanı dışarıdan alır, rastgelelik seed'li <see cref="System.Random"/>'dan gelir, test edilebilir.
/// </summary>
public class FireSim
{
    readonly TerrainMap map;
    readonly GameConfig config;
    readonly System.Random random;
    readonly float[] burnLeft;
    readonly List<int> burning = new List<int>();
    readonly List<int> lavaEdge = new List<int>();
    readonly List<int> scratch = new List<int>();

    /// <summary>Bir karonun yanma durumu değişince (tutuştu, söndü, kömürleşti) sütun ve satırla çağrılır.</summary>
    public event Action<int, int> BurnChanged;

    /// <summary>Şu an yanan karo sayısı.</summary>
    public int BurningCount => burning.Count;

    public IReadOnlyList<int> BurningCells => burning;

    public TerrainMap Map => map;

    public FireSim(TerrainMap map, GameConfig config, System.Random random)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        burnLeft = new float[map.Width * map.Height];

        // Lavın bitişiğindeki tahta karolar (lav bunları tutuşturabilir).
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (TerrainMap.IsFlammable(map.At(x, y)) && TouchesLava(x, y)) lavaEdge.Add(map.Index(x, y));
    }

    bool TouchesLava(int x, int y)
    {
        return map.At(x + 1, y) == TileType.Lava || map.At(x - 1, y) == TileType.Lava
            || map.At(x, y + 1) == TileType.Lava || map.At(x, y - 1) == TileType.Lava;
    }

    public bool IsBurning(int x, int y) => map.InBounds(x, y) && burnLeft[map.Index(x, y)] > 0f;

    // ---- Tutuşturma ----

    /// <summary>
    /// Karoyu tutuşturur. Lav, uçurum, su, kum ve yanmayan engeller (taş/kerpiç duvar, kaktüs) alev almaz; zaten yanan karo değişmez.
    /// Tahta tam süre yanar ve yayılır; diğer zeminlerde (çimen, kar, kum, taş, buz, kömür) alev kısa süre kalır ve yayılmaz.
    /// Yeni tutuştuysa true.
    /// </summary>
    public bool Ignite(int x, int y)
    {
        if (!map.InBounds(x, y)) return false;

        TileType type = map.At(x, y);
        if (!TerrainMap.CanHoldFire(type)) return false;

        int index = map.Index(x, y);
        if (burnLeft[index] > 0f) return false;

        burnLeft[index] = TerrainMap.IsFlammable(type) ? config.fireBurnSeconds : config.groundFireSeconds;
        burning.Add(index);
        BurnChanged?.Invoke(x, y);
        return true;
    }

    /// <summary>
    /// Dünya konumundaki daireyi (merkezi <paramref name="radius"/> içinde kalan karolar) tutuşturur. Yeni tutuşan karo sayısını verir.
    /// <paramref name="chance"/> (0-1) her karonun tutuşma olasılığıdır.
    /// </summary>
    public int IgniteArea(Vector2 center, float radius, float chance = 1f)
    {
        int ignited = 0;
        Vector2Int cell = map.CellOf(center);
        int reach = Mathf.CeilToInt(radius) + 1;

        for (int y = cell.y - reach; y <= cell.y + reach; y++)
        {
            for (int x = cell.x - reach; x <= cell.x + reach; x++)
            {
                if (!map.InBounds(x, y)) continue;
                if ((map.CellCenter(x, y) - center).magnitude > radius + 0.4f) continue;
                if (chance < 1f && random.NextDouble() > chance) continue;
                if (Ignite(x, y)) ignited++;
            }
        }
        return ignited;
    }

    /// <summary>Daire içindeki alevleri söndürür (ör. yağmur bulutu). Söndürülen karo sayısını verir.</summary>
    public int ExtinguishArea(Vector2 center, float radius)
    {
        int count = 0;
        scratch.Clear();
        scratch.AddRange(burning);
        foreach (int index in scratch)
        {
            int x = index % map.Width, y = index / map.Width;
            if ((map.CellCenter(x, y) - center).magnitude > radius + 0.4f) continue;
            Remove(index);
            count++;
        }
        return count;
    }

    // ---- Zaman ----

    public void Tick(float deltaSeconds)
    {
        float dt = Mathf.Max(0f, deltaSeconds);
        if (dt <= 0f) return;

        TickLava(dt);

        scratch.Clear();
        scratch.AddRange(burning);

        float spreadChance = 1f - Mathf.Exp(-config.fireSpreadChancePerSecond * dt);
        foreach (int index in scratch)
        {
            int x = index % map.Width, y = index / map.Width;

            // Yanan tahta komşu tahtalara yayılır. Çimen/patika üstündeki kısa alev yayılmaz.
            if (TerrainMap.IsFlammable(map.At(x, y)))
            {
                TrySpread(x + 1, y, spreadChance);
                TrySpread(x - 1, y, spreadChance);
                TrySpread(x, y + 1, spreadChance);
                TrySpread(x, y - 1, spreadChance);
            }

            burnLeft[index] -= dt;
            if (burnLeft[index] <= 0f) BurnOut(index, x, y);
        }
    }

    void TickLava(float dt)
    {
        float chance = 1f - Mathf.Exp(-config.lavaIgniteChancePerSecond * dt);
        foreach (int index in lavaEdge)
        {
            if (burnLeft[index] > 0f || !TerrainMap.IsFlammable(map.At(index % map.Width, index / map.Width))) continue;
            if (random.NextDouble() < chance) Ignite(index % map.Width, index / map.Width);
        }
    }

    void TrySpread(int x, int y, float chance)
    {
        if (!map.InBounds(x, y) || !TerrainMap.IsFlammable(map.At(x, y))) return;
        if (random.NextDouble() < chance) Ignite(x, y);
    }

    void BurnOut(int index, int x, int y)
    {
        TileType type = map.At(x, y);
        Remove(index);

        // Yanan tahta kömürleşir; duvar çöküp yürünür kömür zemin olur. Çimende alev sadece söner.
        if (TerrainMap.IsFlammable(type)) map.Set(x, y, TileType.CharredFloor);
    }

    void Remove(int index)
    {
        burnLeft[index] = 0f;
        burning.Remove(index);
        BurnChanged?.Invoke(index % map.Width, index / map.Width);
    }

    // ---- Ağ: durumu dışarıdan uygulama (istemci) ----

    /// <summary>Host'tan gelen yanma durumunu uygular (istemcide yangın hesaplanmaz, yalnızca gösterilir).</summary>
    public void ApplyBurning(bool[] state)
    {
        if (state == null || state.Length != burnLeft.Length) return;

        for (int i = 0; i < state.Length; i++)
        {
            bool now = burnLeft[i] > 0f;
            if (now == state[i]) continue;

            burnLeft[i] = state[i] ? 1f : 0f;
            if (state[i]) burning.Add(i); else burning.Remove(i);
            BurnChanged?.Invoke(i % map.Width, i / map.Width);
        }
    }
}
