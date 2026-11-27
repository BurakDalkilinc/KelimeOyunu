using UnityEngine;

/// <summary>
/// Sürü saldırısı (ör. zombi sürüsü; tek üyeli olarak ejderha): kelimenin "stats.count" kadar yaratığı arenanın kenarlarından salar.
/// Sürüler hemen gelir; uyarılı kurulursa (ateşli tek yaratık: ejderha) yaratık tehdidin uyarı süresi bitince kenardan girer,
/// oyuncular o sürede mağaraya koşar.
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

    readonly bool needsWarning;

    /// <param name="needsWarning">true ise yaratıklar tehdidin uyarı süresi bitince salınır ("fire" etiketi); false ise hemen.</param>
    public HordeEffect(bool needsWarning = false)
    {
        this.needsWarning = needsWarning;
    }

    public bool NeedsWarning => needsWarning;

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
            Vector2 position = ArenaSpawn.PickEdgePoint(context.Arena, context.Random);

            // Her yaratık, doğduğu yere en yakın düşmana (oyuncuya) saldırır.
            PlayerState target = NearestTarget(position);
            if (target == null) break;

            CreatureModel member = CreatureFactory.CreateAttack(context.Word, target, position, context.Config, context.Arena, i, count);
            member.Protection = context.Zones;       // İsteğe bağlı sığınakta (ev, mağara) olanlara vuramaz
            member.Locator = context.World;          // Hedefi ölünce ya da başkası yaklaşınca yeniden seçebilsin
            member.TargetsNearest = true;
            member.RetargetsWhenTargetDies = true;

            // Tek yaratıkta (ejderha) adı yazılır; ad, ekrana basan tarafta seçili dilde doldurulur.
            context.Creatures.Spawn(member, count == 1 ? CreatureLabel.NameToken : string.Empty);
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

    /// <summary>Verilen noktaya en yakın, hayattaki ve görünmez olmayan oyuncu; yoksa null.</summary>
    PlayerState NearestTarget(Vector2 from)
    {
        PlayerState best = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive || player.Status.IsStealthed) continue;
            if (!context.World.TryGetPosition(player.Id, out Vector2 position)) continue;

            float distance = (position - from).magnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }
        return best;
    }
}
