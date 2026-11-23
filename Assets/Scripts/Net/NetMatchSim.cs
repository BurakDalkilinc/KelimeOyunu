using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>Ağ maçındaki karakterlerin sahnedeki halini veren/değiştiren kaynak (host'ta <see cref="NetworkPlayer"/>'lar).</summary>
public interface INetAvatars
{
    bool TryGetPosition(int playerId, out Vector2 position);
    bool TryApplyKnockback(int playerId, Vector2 distance);
}

/// <summary>
/// Ağ maçının host tarafındaki yaratık dünyası: kabul edilen kelimeyi yaratığa çevirir, her karede ilerletir,
/// can/hasar/yavaşlama/fırlatmayı mevcut kurallarla (<see cref="CreatureModel"/>, <see cref="CreatureDriver"/>) uygular
/// ve istemcilere gidecek <see cref="CreatureSnapshot"/> listesini verir. Kuralların hiçbiri burada yeniden yazılmadı.
/// Düz C# sınıfı; zamanı dışarıdan alır, ağ kütüphanesini bilmez.
/// </summary>
public class NetMatchSim : ICreatureWorld
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
    int nextId = 1;

    public NetMatchSim(GameConfig config, MatchState match, INetAvatars avatars, Random random)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.match = match ?? throw new ArgumentNullException(nameof(match));
        this.avatars = avatars ?? throw new ArgumentNullException(nameof(avatars));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        arena = new ArenaBounds(config.arenaSize);
    }

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
    /// Kabul edilen kelimeyi yaratığa çevirir. Saldırı kelimesinin hedefi rastgele bir rakiptir; bilinmeyen kelime "???" olur.
    /// (Koruma ve büyük tehdit kelimeleri sonraki adımlarda eklenecek.)
    /// </summary>
    public void OnWordPlayed(PlayerState writer, WordEntry word)
    {
        if (word == null)
        {
            Add(CreatureBuilder.BuildUnknown(config, arena, random), "???", true);
            return;
        }

        if (word.category != WordEntry.CategoryAttack || word.scope != WordEntry.ScopeTarget) return;

        PlayerState target = TargetPicker.PickRandomOpponent(match.Players, writer, random);
        if (target == null || !avatars.TryGetPosition(target.Id, out Vector2 targetPosition)) return;

        List<CreatureModel> models = CreatureBuilder.BuildAttack(word, target, targetPosition, config, arena, random);
        for (int i = 0; i < models.Count; i++)
            Add(models[i], models.Count == 1 || i == 0 ? word.display : "", false);
    }

    void Add(CreatureModel model, string label, bool wanders)
    {
        model.Locator = this; // Hedefi ölünce ya da başkası yaklaşınca yeniden seçebilsin

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

    static CreatureKind KindOf(CreatureModel m)
    {
        if (m.IsStrike) return CreatureKind.Strike;
        if (m.IsPull) return CreatureKind.Pull;
        if (m.IsTrap) return CreatureKind.Trap;
        if (m.IsArea) return m.BurnsArea ? CreatureKind.FireArea : CreatureKind.Area;
        return CreatureKind.Chaser;
    }
}
