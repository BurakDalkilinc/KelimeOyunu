using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ölçüm düzeneği: bir global tehdit kelimesini (ejderha gibi) gerçek <see cref="GlobalThreat"/> ile boş arenada defalarca başlatır.
/// Rastgele dağılmış oyuncular tehdit yazıldığı anda en yakın boş çözüm alanına (mağara) düz koşar. Ölçülenler: mağaraya giren oran,
/// hiç hasar almadan giren oran, "uyarı süresinde koşarak yetişir" oranı (yalnız mesafe hesabı), ortalama hasar ve eleme oranı.
/// Rastgelelik seed'li olduğu için sonuç her koşuda aynıdır.
/// </summary>
public static class DragonRefugeSim
{
    /// <summary>Bir ölçümün sonucu.</summary>
    public struct Result
    {
        public int Runs;
        public int Players;
        public int Zones;
        public int Capacity;
        public float WarningSeconds;
        /// <summary>Herhangi bir anda mağarada korunmuş oyuncu oranı.</summary>
        public float ReachRate;
        /// <summary>Hiç hasar almadan mağaraya girmiş oyuncu oranı.</summary>
        public float SafeRate;
        /// <summary>En yakın mağaranın kenarına (tehdidin gerçekten verdiği) uyarı süresi içinde koşarak yetişebilecek oyuncu oranı (yalnız mesafe).</summary>
        public float InTimeRate;
        public float MeanDamage;
        public float KillRate;
    }

    sealed class World : IPlayerLocator
    {
        public readonly List<PlayerState> List = new List<PlayerState>();
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => List;

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);
    }

    sealed class Sink : ICreatureSink
    {
        public readonly List<CreatureModel> Models = new List<CreatureModel>();
        public void Spawn(CreatureModel model, string label) => Models.Add(model);
    }

    const float Step = 1f / 30f;
    const float MaxSeconds = 40f;

    /// <summary>
    /// <paramref name="word"/> tehdidini <paramref name="runs"/> kez dener; her denemede <paramref name="playerCount"/> oyuncu
    /// <paramref name="runSpeed"/> hızıyla en yakın boş çözüm alanına koşar.
    /// </summary>
    public static Result Run(WordEntry word, GameConfig config, int runs, int seed, float runSpeed, int playerCount = 8)
    {
        var arena = new ArenaBounds(config.arenaSize);
        var random = new System.Random(seed);
        float warning = 0f;

        int total = 0, reached = 0, safe = 0, inTime = 0, killed = 0, damage = 0, zoneCount = 0, capacity = 0;
        var threats = new List<GlobalThreat>();

        for (int run = 0; run < runs; run++)
        {
            var world = new World();
            for (int i = 0; i < playerCount; i++)
            {
                world.List.Add(new PlayerState(i, "P" + i, config.startingHealth, isBot: true));
                world.Positions[i] = ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, random, 1.5f);
            }

            var sink = new Sink();
            var threat = new GlobalThreat(word, world.List[0], config, arena, world, new System.Random(random.Next()), sink);
            threats.Clear();
            threats.Add(threat);
            warning = threat.WarningLeft; // Gerçek uyarı (yaratık hemen geliyorsa 0)
            zoneCount = threat.Zones.Zones.Count;
            capacity = zoneCount > 0 ? threat.Zones.Zones[0].Capacity : 0;

            // Mesafe hesabı: en yakın mağaranın kenarı / koşu hızı <= uyarı süresi.
            foreach (PlayerState player in world.List)
            {
                float nearest = float.MaxValue;
                foreach (SafeZone zone in threat.Zones.Zones)
                    nearest = Mathf.Min(nearest, (zone.Position - world.Positions[player.Id]).magnitude - zone.Radius);
                if (Mathf.Max(0f, nearest) / runSpeed <= warning) inTime++;
            }

            var everProtected = new HashSet<int>();
            var hurtBeforeShelter = new HashSet<int>();

            for (float time = 0f; time < MaxSeconds && threat.Phase != ThreatPhase.Finished; time += Step)
            {
                threat.Tick(Step);
                foreach (CreatureModel model in sink.Models)
                {
                    if (model.IsFinished) continue;
                    if (model.IsArea) model.TickArea(Step, world, config.playerRadius);
                    else model.Tick(Step, TargetPosition(world, model), config.playerRadius, config.creatureHitCooldownSeconds);
                }

                foreach (PlayerState player in world.List)
                {
                    if (!player.IsAlive) continue;
                    if (threat.Zones.IsProtected(player.Id)) everProtected.Add(player.Id);
                    else if (player.Health.Current < config.startingHealth) hurtBeforeShelter.Add(player.Id);

                    if (!RefugeFinder.TryGetRefuge(threats, player, world.Positions[player.Id], out Vector2 point)) continue;
                    Vector2 at = world.Positions[player.Id];
                    world.Positions[player.Id] = arena.Clamp(Vector2.MoveTowards(at, point, runSpeed * Step), config.playerRadius);
                }
            }

            foreach (PlayerState player in world.List)
            {
                total++;
                if (everProtected.Contains(player.Id)) reached++;
                if (everProtected.Contains(player.Id) && !hurtBeforeShelter.Contains(player.Id)) safe++;
                if (!player.IsAlive) killed++;
                damage += config.startingHealth - player.Health.Current;
            }
        }

        float n = Mathf.Max(1, total);
        return new Result
        {
            Runs = runs,
            Players = playerCount,
            Zones = zoneCount,
            Capacity = capacity,
            WarningSeconds = warning,
            ReachRate = reached / n,
            SafeRate = safe / n,
            InTimeRate = inTime / n,
            MeanDamage = damage / n,
            KillRate = killed / n,
        };
    }

    static Vector2 TargetPosition(World world, CreatureModel model)
    {
        return model.Target != null && world.TryGetPosition(model.Target.Id, out Vector2 at) ? at : model.Position;
    }

    public static string Format(string label, Result r)
    {
        return string.Format("{0}: {1} oyuncu, {2} mağara x {3} kişi, uyarı {4:0.#} sn | mağaraya giren %{5:0} | hasarsız giren %{6:0} | " +
                             "uyarıda yetişir %{7:0} | ort. hasar {8:0.00} | elenen %{9:0}",
            label, r.Players, r.Zones, r.Capacity, r.WarningSeconds, r.ReachRate * 100f, r.SafeRate * 100f, r.InTimeRate * 100f,
            r.MeanDamage, r.KillRate * 100f);
    }
}
