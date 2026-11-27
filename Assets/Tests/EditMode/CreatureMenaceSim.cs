using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ölçüm düzeneği: bir saldırı kelimesinin yaratığını, botlar gibi kaçan oyunculara karşı boş arenada defalarca koşturur ve
/// "ömrü boyunca verdiği ortalama hasar", "en az 1 vuruş yapma oranı" ve "en az bir oyuncuyu öldürme oranı"nı ölçer.
/// Oyundaki kuralların aynısını kullanır (<see cref="CreatureBuilder"/>, <see cref="CreatureDriver"/>, <see cref="BotBrain"/>);
/// tek fark harita (ev, duvar) olmamasıdır. Rastgelelik seed'li olduğu için sonuç her koşuda aynıdır.
/// </summary>
public static class CreatureMenaceSim
{
    /// <summary>
    /// Ayarları bu değişiklikten önceki hâline döndürür (hız/ömür çarpanı 1, atılma, sendeleme, hedef değiştirme kapalı,
    /// doğma mesafesi 6, botlar atılmayı hesaba katmaz). "Önce" ölçümü için.
    /// </summary>
    public static void ApplyLegacy(GameConfig config)
    {
        config.creatureSpeedMultiplier = 1f;
        config.creatureSlowMinSpeedFraction = 0f;
        config.creatureSlowMaxSpeedFraction = 1f;
        config.creatureMaxSpeedFraction = 0f;
        config.creatureDurationMultiplier = 1f;
        config.creaturePounceRange = 0f;
        config.creatureTagTunings = new CreatureTagTuning[0];
        config.creatureHitSlowSeconds = 0f;
        config.creatureRetargetSeconds = 0f;
        config.creatureSpawnMinDistance = 6f;
        config.botPounceAwareness = 0f;
    }

    /// <summary>Bir kelimenin ölçüm sonucu.</summary>
    public struct Result
    {
        public string WordId;
        public int Runs;
        public float MeanDamage;
        /// <summary>Ortalama isabet (yaratığın vuruş sayısı; hasarsız yavaşlatanlar da sayılır).</summary>
        public float MeanHits;
        public float HitRate;
        public float KillRate;
    }

    /// <summary>Oyuncuları taşıyan sahte dünya: konumlar burada, fırlatma anında uygulanır.</summary>
    sealed class World : ICreatureWorld
    {
        public readonly List<PlayerState> List = new List<PlayerState>();
        public readonly Dictionary<int, Vector2> Positions = new Dictionary<int, Vector2>();
        public ArenaBounds Arena;
        public float PlayerRadius;

        public IReadOnlyList<PlayerState> Players => List;

        public bool TryGetPosition(int playerId, out Vector2 position) => Positions.TryGetValue(playerId, out position);

        public bool TryApplyKnockback(int playerId, Vector2 distance)
        {
            if (!Positions.TryGetValue(playerId, out Vector2 at)) return false;
            Positions[playerId] = Arena.Clamp(at + distance, PlayerRadius);
            return true;
        }
    }

    const float Step = 1f / 30f;
    const float MaxSeconds = 90f;

    /// <summary>
    /// <paramref name="word"/> kelimesini <paramref name="runs"/> kez dener. Her denemede <paramref name="playerCount"/> oyuncu
    /// arenaya rastgele dağılır, ilki hedeftir; hepsi <paramref name="fleeSpeed"/> hızla botlar gibi kaçar/gezinir.
    /// <paramref name="brainConfig"/> verilirse kaçanların beyni onu kullanır (ör. daha erken kaçan "insan gibi" oyuncu).
    /// </summary>
    public static Result Run(WordEntry word, GameConfig config, int runs, int seed, float fleeSpeed, int playerCount = 8,
                             GameConfig brainConfig = null)
    {
        var arena = new ArenaBounds(config.arenaSize);
        var random = new System.Random(seed);
        var hits = new List<CreatureHit>();
        var dangers = new List<DangerZone>();

        float totalDamage = 0f;
        float totalHits = 0f;
        int runsWithHit = 0;
        int runsWithKill = 0;

        for (int run = 0; run < runs; run++)
        {
            var world = new World { Arena = arena, PlayerRadius = config.playerRadius };
            var brains = new List<BotBrain>();
            for (int i = 0; i < playerCount; i++)
            {
                var player = new PlayerState(i, "P" + i, config.startingHealth, isBot: true);
                world.List.Add(player);
                world.Positions[i] = ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, random, 1.5f);
                brains.Add(new BotBrain(brainConfig ?? config, arena, new System.Random(random.Next())));
            }

            PlayerState target = world.List[0];
            List<CreatureModel> models = CreatureBuilder.BuildAttack(word, target, world.Positions[0], config, arena, random);
            foreach (CreatureModel model in models)
            {
                model.Locator = world;
                BossRules.ScaleForPlayers(model, playerCount, config);
            }

            for (float time = 0f; time < MaxSeconds; time += Step)
            {
                bool anyAlive = false;
                dangers.Clear();
                foreach (CreatureModel model in models)
                {
                    if (model.IsFinished) continue;
                    anyAlive = true;
                    CreatureDriver.Advance(model, Step, world, config, hits, null);
                    if (!model.IsFinished && model.IsHarmful && !model.IsLeaving)
                        dangers.Add(DangerFor(model, config));
                }
                if (!anyAlive) break;

                foreach (PlayerState player in world.List)
                {
                    player.Status.Tick(Step);
                    if (!player.IsAlive) continue;
                    Vector2 at = world.Positions[player.Id];
                    Vector2 direction = brains[player.Id].Think(Step, at, dangers);
                    world.Positions[player.Id] = arena.Clamp(at + direction * fleeSpeed * player.Status.SpeedMultiplier * Step, config.playerRadius);
                }
            }

            int damage = 0;
            int attacks = 0;
            bool killed = false;
            foreach (CreatureModel model in models) attacks += model.AttackCount;
            foreach (PlayerState player in world.List)
            {
                damage += config.startingHealth - player.Health.Current;
                if (!player.IsAlive) killed = true;
            }

            totalDamage += damage;
            totalHits += attacks;
            if (attacks > 0) runsWithHit++;
            if (killed) runsWithKill++;
        }

        return new Result
        {
            WordId = word.id,
            Runs = runs,
            MeanDamage = totalDamage / Mathf.Max(1, runs),
            MeanHits = totalHits / Mathf.Max(1, runs),
            HitRate = (float)runsWithHit / Mathf.Max(1, runs),
            KillRate = (float)runsWithKill / Mathf.Max(1, runs),
        };
    }

    /// <summary>Botların kaçtığı alan (oyundakiyle aynı kural).</summary>
    static DangerZone DangerFor(CreatureModel model, GameConfig config) => CreatureAggression.DangerZoneFor(model, config);
}
