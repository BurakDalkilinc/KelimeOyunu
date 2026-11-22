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
    [SerializeField] TargetSelector humanTargeting;
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
            BeginAttack(writer, word);

        // Tehdit (threat) ve koruma (defense) kelimeleri yol haritasında sonraki adımlarda.
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

        // Sürü (swarm) kelimesi birden fazla yaratık doğurur; hepsi aynı noktanın etrafında bir halkada başlar.
        int count = TagRegistry.GetSpawnCount(word);
        Vector2 center = ArenaSpawn.PickPoint(arena, targetPosition, config.creatureSpawnMinDistance, random);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawn = count == 1 ? center : center + RingOffset(i, count, config.swarmSpawnRingRadius);
            CreatureModel model = CreatureFactory.CreateAttack(word, target, spawn, config, arena, i, count);

            // Anında vuran yaratık (yumruk) uzaktan koşmaz: uyarı alanı hedefin o anki yerine konur.
            if (model.PlacesNearTarget)
            {
                // Yumruk hedefin tam üstüne düşer; patlayıcı (bomba) ve tuzak (muz kabuğu) yanına düşer.
                Vector2 at = model.PlacementOffset > 0f
                    ? ArenaSpawn.PointBeside(targetPosition, model.PlacementOffset, random)
                    : targetPosition;
                model.PlaceAt(at);
            }

            Create(model, attackColor, count == 1 || i == 0 ? word.display : string.Empty);
        }
    }

    /// <summary>Sürü üyesinin halka üzerindeki yeri (üyeler eşit aralıkla dizilir).</summary>
    static Vector2 RingOffset(int index, int count, float radius)
    {
        float angle = Mathf.PI * 2f * index / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
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
    }
}
