using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kabul edilen kelimeyi sahnede yaratığa çevirir. Saldırı kelimesi, rastgele bir rakibi kovalayan yaratık olur;
/// bilinmeyen kelime zararsız bir "???" yaratığı olur. Tehdit ve koruma kelimeleri sonraki adımlarda eklenecek.
/// Canlı tehlikeli yaratıkların konumunu botlara "kaçılacak alan" olarak bildirir.
/// </summary>
public class CreatureSpawner : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] MatchRunner runner;
    [SerializeField] MatchBootstrap bootstrap;
    [SerializeField] Color attackColor = new Color(0.95f, 0.35f, 0.3f);
    [SerializeField] Color unknownColor = new Color(0.65f, 0.55f, 0.85f);

    const string UnknownLabel = "???";

    readonly List<CreatureView> creatures = new List<CreatureView>();
    TurnManager turns;
    ArenaBounds arena;
    System.Random random;

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
        turns.WordPlayed += OnWordPlayed;
    }

    void OnDisable()
    {
        if (turns != null) turns.WordPlayed -= OnWordPlayed;
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
            SpawnAttack(writer, word);

        // Tehdit (threat) ve koruma (defense) kelimeleri yol haritasında sonraki adımlarda.
    }

    void SpawnAttack(PlayerState writer, WordEntry word)
    {
        PlayerState target = TargetPicker.PickRandomOpponent(bootstrap.Match.Players, writer, random);
        if (target == null || !bootstrap.TryGetPosition(target.Id, out Vector2 targetPosition)) return;

        Vector2 spawn = ArenaSpawn.PickPoint(arena, targetPosition, config.creatureSpawnMinDistance, random);
        CreatureModel model = CreatureFactory.CreateAttack(word, target, spawn, config, arena);
        Create(model, attackColor, word.display);
    }

    void SpawnUnknown()
    {
        Vector2 spawn = ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, random);
        CreatureModel model = CreatureFactory.CreateUnknown(spawn, config, arena);
        Create(model, unknownColor, UnknownLabel);
    }

    void Create(CreatureModel model, Color color, string label)
    {
        var go = new GameObject("Yaratık: " + label);
        go.transform.SetParent(transform, false);

        var view = go.AddComponent<CreatureView>();
        view.Initialize(model, config, bootstrap, arena, new System.Random(random.Next()), color, label);
        creatures.Add(view);
    }

    void LateUpdate()
    {
        // Botlara kaçılacak alanları bildir: yalnızca canlı ve tehlikeli yaratıklar.
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

            if (view.Model.IsDangerous)
                dangers.Add(new DangerZone(view.Model.Position, view.Model.Radius));
        }
    }
}
