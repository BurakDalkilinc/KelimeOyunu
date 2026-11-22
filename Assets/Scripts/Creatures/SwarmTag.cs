using UnityEngine;

/// <summary>
/// "swarm": sürü. Kelimeden "stats.count" kadar küçük yaratık doğar. Hepsi aynı anda vurup hedefi anında
/// öldürmesin diye, her üyenin ilk vuruşu sırasına göre ertelenir (GameConfig'teki swarmHitStaggerSeconds).
/// </summary>
public class SwarmTag : ITagBehaviour, ISpawnCountTag
{
    const int MaxMembers = 100;

    public string Tag => "swarm";

    public int GetSpawnCount(WordEntry word)
    {
        int count = word?.stats?.count ?? 1;
        return Mathf.Clamp(count, 1, MaxMembers);
    }

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        if (context.Config == null) return;
        creature.DelayFirstHit(context.Index * context.Config.swarmHitStaggerSeconds);
    }
}
