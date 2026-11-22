/// <summary>
/// "shield": yazana kalkan verir. Kelimenin "stats.blocks" kadar hasarı engeller (yazmıyorsa 1),
/// en geç "stats.duration" saniye (yazmıyorsa GameConfig'teki varsayılan) durur.
/// </summary>
public class ShieldTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "shield";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        WordStats stats = context.Word?.stats;
        int blocks = stats != null && stats.blocks > 0 ? stats.blocks : 1;
        float seconds = stats != null && stats.duration > 0f ? stats.duration : DefaultSeconds(context.Config);

        context.Writer.Status.ApplyShield(blocks, seconds);
    }

    static float DefaultSeconds(GameConfig config) => config != null ? config.shieldDefaultSeconds : 30f;
}
