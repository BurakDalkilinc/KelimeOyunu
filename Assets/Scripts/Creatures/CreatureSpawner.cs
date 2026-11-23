using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kabul edilen kelimeyi sahnede yaratığa çevirir. Saldırı kelimesi, rastgele bir rakibi kovalayan yaratık olur;
/// bilinmeyen kelime zararsız bir "???" yaratığı olur. Tehdit ve koruma kelimeleri sonraki adımlarda eklenecek.
/// Canlı tehlikeli yaratıkların konumunu botlara "kaçılacak alan" olarak bildirir.
/// </summary>
public class CreatureSpawner : MonoBehaviour, ICreatureSink
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;
    [SerializeField] MatchBootstrap bootstrap;
    [SerializeField] TargetSelector humanTargeting;
    [SerializeField] Color attackColor = new Color(0.95f, 0.35f, 0.3f);
    [SerializeField] Color unknownColor = new Color(0.65f, 0.55f, 0.85f);

    const string UnknownLabel = "???";

    readonly List<CreatureView> creatures = new List<CreatureView>();
    TurnManager turns;
    ArenaBounds arena;
    System.Random random;
    IRevivalService revival;

    void OnEnable()
    {
        if (config == null || runner == null || runner.Turns == null || bootstrap == null)
        {
            Debug.LogError("CreatureSpawner: config, runner ya da bootstrap eksik.", this);
            enabled = false;
            return;
        }

        turns = runner.Turns;
        arena = new ArenaBounds(config.arenaSize);
        random = new System.Random(runner.Random.Next());
        revival = new GhostRevival(bootstrap.Match.Players, config, new System.Random(runner.Random.Next()));
        turns.WordPlayed += OnWordPlayed;
        bootstrap.CreatureSource = Models;
    }

    /// <summary>Sahnedeki canlı yaratıkların modelleri (arazi bunlara bakıp tutuşturur).</summary>
    IEnumerable<CreatureModel> Models()
    {
        foreach (CreatureView view in creatures)
            if (view != null) yield return view.Model;
    }

    void OnDisable()
    {
        if (turns != null) turns.WordPlayed -= OnWordPlayed;
        if (bootstrap != null && bootstrap.CreatureSource == (System.Func<IEnumerable<CreatureModel>>)Models) bootstrap.CreatureSource = null;
    }

    /// <summary>Kelime kabul edilince: türüne göre yaratık doğur. Bilinmeyen kelimede <paramref name="word"/> null'dır.</summary>
    void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        if (word == null)
        {
            SpawnUnknown();
            return;
        }

        if (word.category == WordEntry.CategoryAttack && word.scope == WordEntry.ScopeTarget)
            BeginAttack(writer, word);

        // Koruma kelimeleri yazana uygulanır: can, kalkan, görünmezlik, uçma...
        if (word.category == WordEntry.CategoryDefense && word.scope == WordEntry.ScopeSelf)
        {
            // Duvar gibi yer isteyen etkiler için yazanın konumu ve baktığı yön de verilir.
            bootstrap.TryGetPosition(writer.Id, out Vector2 writerPosition);
            Vector2 facing = bootstrap.TryGetFacing(writer.Id, out Vector2 f) ? f : Vector2.up;
            TagRegistry.ApplySelfEffects(word, new SelfEffectContext(writer, word, config, writerPosition, facing, bootstrap.Walls, revival));
        }

        // Tehdit (threat) kelimeleri yol haritasında sonraki adımlarda.
    }

    /// <summary>
    /// Saldırı kelimesinin hedefini belirler: insan oyuncu fareyle seçer (süre dolarsa rastgele),
    /// bot hemen rastgele bir rakip seçer. Yazan kendini hedefleyemez.
    /// </summary>
    void BeginAttack(PlayerState writer, WordEntry word)
    {
        if (!writer.IsBot && humanTargeting != null)
        {
            humanTargeting.Begin(writer, chosen => SpawnAttack(writer, word, chosen));
            return;
        }

        PlayerState target = TargetPicker.PickRandomOpponent(bootstrap.Match.Players, writer, random);
        if (target != null) SpawnAttack(writer, word, target);
    }

    void SpawnAttack(PlayerState writer, WordEntry word, PlayerState target)
    {
        // Hedef seçilirken elenmiş olabilir; öyleyse başka bir rakip seç.
        if (!target.IsAlive)
        {
            target = TargetPicker.PickRandomOpponent(bootstrap.Match.Players, writer, random);
            if (target == null) return;
        }

        if (!bootstrap.TryGetPosition(target.Id, out Vector2 targetPosition)) return;

        List<CreatureModel> models = CreatureBuilder.BuildAttack(word, target, targetPosition, config, arena, random);
        for (int i = 0; i < models.Count; i++)
            Create(models[i], attackColor, models.Count == 1 || i == 0 ? word.display : string.Empty);
    }

    void SpawnUnknown()
    {
        Create(CreatureBuilder.BuildUnknown(config, arena, random), unknownColor, UnknownLabel);
    }

    /// <summary>Tehdit etkilerinin (ör. meteor yağmuru) yaratık koyması için: saldırı rengiyle sahneye koyar.</summary>
    public void Spawn(CreatureModel model, string label)
    {
        Create(model, attackColor, label);
    }

    void Create(CreatureModel model, Color color, string label)
    {
        model.Obstacles = bootstrap.Obstacles; // Uçmayan yaratıklar duvardan (oyuncu duvarları ve ev duvarları) geçemez

        var go = new GameObject("Yaratık: " + label);
        go.transform.SetParent(transform, false);

        var view = go.AddComponent<CreatureView>();
        view.Initialize(model, config, bootstrap, arena, new System.Random(random.Next()), color, label);
        creatures.Add(view);
    }

    void LateUpdate()
    {
        // Botlara kaçılacak alanları bildir: yalnızca canlı ve zararlı (hasar, fırlatma ya da yavaşlatma) yaratıklar.
        IList<DangerZone> dangers = bootstrap.Dangers;
        dangers.Clear();

        for (int i = creatures.Count - 1; i >= 0; i--)
        {
            CreatureView view = creatures[i];
            if (view == null || view.Model.IsFinished)
            {
                creatures.RemoveAt(i);
                continue;
            }

            // Sıkılıp giden avcı artık tehlike değil; botlar ondan kaçmak zorunda kalmaz.
            if (view.Model.IsHarmful && !view.Model.IsLeaving)
                dangers.Add(new DangerZone(view.Model.Position, view.Model.Radius));
        }

        // Yanan haritalar da tehlikedir: botlar alevden kaçar.
        bootstrap.AppendFireDangers(dangers);
    }
}
