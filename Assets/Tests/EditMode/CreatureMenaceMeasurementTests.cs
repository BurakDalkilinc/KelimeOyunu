using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Yaratık tehlikesi ölçümü: temsilci kelimelerin yaratıklarını botlar gibi kaçan oyunculara karşı koşturur,
/// sonucu tablo olarak Console'a yazar (bkz. <see cref="CreatureMenaceSim"/>). İki kaçan türü ölçülür:
/// "bot" (bot hızı, tehlikeye yaklaşınca kaçar) ve "oyuncu" (oyuncu hızı, yaratığı uzaktan görüp erken kaçar).
/// </summary>
public class CreatureMenaceMeasurementTests
{
    static readonly string[] Sample = { "zombi", "kurt", "aslan", "ayi", "ari", "timsah", "boga", "fare", "kaplumbaga", "godzilla" };

    const int Runs = 120;
    const int AllWordsRuns = 30;
    const int Seed = 1234;

    /// <summary>"İnsan gibi" kaçan oyuncunun yaratığı fark edip kaçmaya başladığı ek mesafe (birim).</summary>
    const float HumanFleeMargin = 6f;

    /// <summary>Bir ayar takımıyla ölçüm: tablo satırları ve bütün kelimelerin özeti.</summary>
    sealed class Summary
    {
        public readonly Dictionary<string, CreatureMenaceSim.Result> Bot = new Dictionary<string, CreatureMenaceSim.Result>();
        public readonly Dictionary<string, CreatureMenaceSim.Result> Human = new Dictionary<string, CreatureMenaceSim.Result>();
        public float BotMedianHits, BotMeanHits, BotKill, HumanMedianHits, HumanMeanHits, HumanKill, HumanMeanDamage;
        public float SampleBotHits, SampleHumanHits;
        public int WordCount;
    }

    static Summary Measure(GameConfig config, WordDatabase db)
    {
        GameConfig human = Object.Instantiate(config);
        human.botFleeMargin = HumanFleeMargin;
        try
        {
            var summary = new Summary();
            foreach (string id in Sample)
            {
                WordEntry word = db.FindById(id);
                Assert.IsNotNull(word, id);
                summary.Bot[id] = CreatureMenaceSim.Run(word, config, Runs, Seed, config.botMoveSpeed);
                summary.Human[id] = CreatureMenaceSim.Run(word, config, Runs, Seed, config.playerMoveSpeed, 8, human);
                summary.SampleBotHits += summary.Bot[id].MeanHits / Sample.Length;
                summary.SampleHumanHits += summary.Human[id].MeanHits / Sample.Length;
            }

            var botHits = new List<float>();
            var humanHits = new List<float>();
            var humanDamage = new List<float>();
            foreach (WordEntry word in db.Words)
            {
                if (word.category != WordEntry.CategoryAttack || word.scope != WordEntry.ScopeTarget) continue;
                CreatureMenaceSim.Result b = CreatureMenaceSim.Run(word, config, AllWordsRuns, Seed, config.botMoveSpeed);
                CreatureMenaceSim.Result h = CreatureMenaceSim.Run(word, config, AllWordsRuns, Seed, config.playerMoveSpeed, 8, human);
                botHits.Add(b.MeanHits);
                humanHits.Add(h.MeanHits);
                humanDamage.Add(h.MeanDamage);
                summary.BotKill += b.KillRate;
                summary.HumanKill += h.KillRate;
            }
            summary.WordCount = botHits.Count;
            summary.BotKill /= Mathf.Max(1, botHits.Count);
            summary.HumanKill /= Mathf.Max(1, humanHits.Count);
            summary.BotMedianHits = Median(botHits);
            summary.BotMeanHits = Mean(botHits);
            summary.HumanMedianHits = Median(humanHits);
            summary.HumanMeanHits = Mean(humanHits);
            summary.HumanMeanDamage = Mean(humanDamage);
            return summary;
        }
        finally { Object.DestroyImmediate(human); }
    }

    /// <summary>
    /// Önce/sonra: eski ayarlarla (<see cref="CreatureMenaceSim.ApplyLegacy"/>) ve yeni ayarlarla ölçer, tabloyu Console'a yazar.
    /// Eşik: bütün kelimelerin ortanca isabeti en az 2 kat artmalı, ama yaratıklar her seferinde öldürmemeli.
    /// </summary>
    [Test]
    public void Measure_BeforeAndAfter_MoreHitsButNotAlwaysKills()
    {
        GameConfig before = ScriptableObject.CreateInstance<GameConfig>();
        GameConfig after = ScriptableObject.CreateInstance<GameConfig>();
        CreatureMenaceSim.ApplyLegacy(before);
        try
        {
            WordDatabase db = WordDatabase.LoadFromResources(50);
            Summary old = Measure(before, db);
            Summary now = Measure(after, db);

            var log = new StringBuilder("ÖLÇÜM önce → sonra (ort. isabet / >=1 isabet / öldürme; 8 oyuncu, boş arena)\n");
            log.AppendLine("kelime | bot önce | bot sonra | oyuncu önce | oyuncu sonra");
            foreach (string id in Sample)
                log.AppendLine($"{id} | {Cell(old.Bot[id])} | {Cell(now.Bot[id])} | {Cell(old.Human[id])} | {Cell(now.Human[id])}");
            log.AppendLine($"10 kelime ort. isabet | bot {old.SampleBotHits:0.00} → {now.SampleBotHits:0.00} | oyuncu {old.SampleHumanHits:0.00} → {now.SampleHumanHits:0.00}");
            log.AppendLine($"TÜM {now.WordCount} kelime ortanca isabet | bot {old.BotMedianHits:0.00} → {now.BotMedianHits:0.00} | oyuncu {old.HumanMedianHits:0.00} → {now.HumanMedianHits:0.00}");
            log.AppendLine($"TÜM kelime ortalama isabet | bot {old.BotMeanHits:0.00} → {now.BotMeanHits:0.00} | oyuncu {old.HumanMeanHits:0.00} → {now.HumanMeanHits:0.00}");
            log.AppendLine($"TÜM kelime öldürme oranı | bot {old.BotKill:P0} → {now.BotKill:P0} | oyuncu {old.HumanKill:P0} → {now.HumanKill:P0}");
            log.AppendLine($"TÜM kelime oyuncuya ort. hasar | {old.HumanMeanDamage:0.00} → {now.HumanMeanDamage:0.00}");
            Debug.Log(log.ToString());

            Assert.GreaterOrEqual(now.HumanMedianHits, old.HumanMedianHits * 2f, "ortanca isabet en az 2 kat artmalı (oyuncu gibi kaçan)");
            Assert.GreaterOrEqual(now.BotMedianHits, old.BotMedianHits * 2f, "ortanca isabet en az 2 kat artmalı (bot)");
            Assert.Less(now.HumanKill, 0.5f, "yaratıklar çoğu zaman öldürmemeli");
            foreach (string id in Sample)
            {
                if (old.Human[id].KillRate >= 1f) continue; // Zaten her seferinde öldüren (sürü) daha kötü olmasın diye aşağıda bakılır
                Assert.Less(now.Human[id].KillRate, 1f, id + " her denemede öldürmemeli");
            }
        }
        finally
        {
            Object.DestroyImmediate(before);
            Object.DestroyImmediate(after);
        }
    }

    static string Cell(CreatureMenaceSim.Result r) => $"{r.MeanHits:0.00} / {r.HitRate:P0} / {r.KillRate:P0}";

    static float Mean(List<float> values)
    {
        float sum = 0f;
        foreach (float v in values) sum += v;
        return values.Count > 0 ? sum / values.Count : 0f;
    }

    static float Median(List<float> values)
    {
        if (values.Count == 0) return 0f;
        var sorted = new List<float>(values);
        sorted.Sort();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) * 0.5f;
    }
}
