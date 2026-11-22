using UnityEngine;

/// <summary>
/// "wall": duvar. Yazanın baktığı yönün önünde, o yöne dik duran bir duvar örer. Uzunluğu kelimenin "stats.length",
/// ömrü "stats.duration" değerinden gelir (yazmıyorsa GameConfig'teki varsayılanlar). Oyuncular, botlar ve uçmayan
/// yaratıklar içinden geçemez; uçan yaratıklar ve uçan (kanat) oyuncular geçer.
/// </summary>
public class WallTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "wall";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        GameConfig config = context.Config;
        if (context.Walls == null || config == null) return;

        WordStats stats = context.Word?.stats;
        float length = stats != null && stats.length > 0f ? stats.length : config.wallDefaultLength;
        float life = stats != null && stats.duration > 0f ? stats.duration : config.wallDefaultSeconds;

        Vector2 facing = context.Facing.sqrMagnitude > 0.0001f ? context.Facing.normalized : Vector2.up;
        Vector2 along = new Vector2(-facing.y, facing.x); // Bakılan yöne dik

        // Duvarın merkezi arenanın içinde kalsın.
        Vector2 center = new ArenaBounds(config.arenaSize).Clamp(context.WriterPosition + facing * config.wallDistanceInFront, 0.5f);

        context.Walls.Add(new WallSegment(center, along, length, config.wallThickness, life));
    }
}
