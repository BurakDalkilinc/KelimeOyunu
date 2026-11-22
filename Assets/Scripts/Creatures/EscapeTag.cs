/// <summary>
/// "escape": geçici uçma / kaçış. Bu süre boyunca (kelimenin "stats.duration" değeri) yerdeki, yani uçmayan yaratıkların
/// temasından ve tuzaklardan etkilenmez; hız kelimenin "stats.speed" değerine göre artar (oyuncu hızına oranla).
/// Alan vuruşları ve patlamalar yine etkiler.
/// </summary>
public class EscapeTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "escape";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        WordStats stats = context.Word?.stats;
        GameConfig config = context.Config;

        float seconds = stats != null && stats.duration > 0f ? stats.duration : (config != null ? config.flightDefaultSeconds : 5f);

        float speedMultiplier = 1f;
        if (config != null && stats != null && stats.speed > 0f && config.playerMoveSpeed > 0f)
            speedMultiplier = stats.speed / config.playerMoveSpeed;

        context.Writer.Status.ApplyFlight(seconds, speedMultiplier);
    }
}
