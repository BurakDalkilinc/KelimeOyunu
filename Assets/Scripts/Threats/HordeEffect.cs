using UnityEngine;

/// <summary>
/// Sürü saldırısı (ör. zombi sürüsü): uyarı bitince kelimenin "stats.count" kadar yaratığı arenanın kenarlarından salar.
/// Her yaratık kelimenin etiketlerinden kurulur (zombide "undead": kalıcı kovalama, "swarm": kademeli ilk vuruş), rastgele bir
/// hayattaki oyuncuyu kovalar ve çözüm alanındaki (ev) korunanlara vuramaz. Yaratıklar yaşadığı sürece
/// ("stats.duration") etki sürer; çözüm alanları o zamana kadar ayakta kalır. Düz C# sınıfı.
/// </summary>
public class HordeEffect : IThreatEffect
{
    const int MaxMembers = 100;
    const float DefaultDuration = 15f;

    ThreatContext context;
    float lifeLeft;

    public bool IsFinished { get; private set; }

    /// <summary>Salınan yaratık sayısı.</summary>
    public int Spawned { get; private set; }

    public void Begin(ThreatContext context)
    {
        this.context = context;

        WordStats stats = context.Word?.stats;
        lifeLeft = stats != null && stats.duration > 0f ? stats.duration : DefaultDuration;

        if (context.Creatures == null)
        {
            IsFinished = true;
            return;
        }

        int count = Mathf.Clamp(stats != null ? stats.count : 0, 1, MaxMembers);
        for (int i = 0; i < count; i++)
        {
            PlayerState target = PickAlivePlayer();
            if (target == null) break;

            Vector2 position = ArenaSpawn.PickEdgePoint(context.Arena, context.Random);
            CreatureModel member = CreatureFactory.CreateAttack(context.Word, target, position, context.Config, context.Arena, i, count);
            member.Protection = context.Zones; // Evdekilere (çözüm alanı) vuramaz

            context.Creatures.Spawn(member, string.Empty);
            Spawned++;
        }

        if (Spawned == 0) IsFinished = true;
    }

    public void Tick(float deltaSeconds)
    {
        if (IsFinished) return;

        lifeLeft -= Mathf.Max(0f, deltaSeconds);
        if (lifeLeft <= 0f) IsFinished = true;
    }

    PlayerState PickAlivePlayer()
    {
        int alive = 0;
        foreach (PlayerState player in context.World.Players)
            if (player.IsAlive) alive++;
        if (alive == 0) return null;

        int pick = context.Random.Next(alive);
        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive) continue;
            if (pick-- == 0) return player;
        }
        return null;
    }
}
