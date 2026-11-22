/// <summary>
/// "slowdown": vurduğu oyuncuyu yavaşlatır (hız çarpanı GameConfig'ten, süre kelimenin "stats.slowSeconds" değerinden).
/// Hasarı olmasa da yavaşlatır; botlar bu yaratıklardan da kaçar.
/// </summary>
public class SlowdownTag : ITagBehaviour
{
    public string Tag => "slowdown";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        GameConfig config = context.Config;
        if (config == null) return;

        float seconds = creature.Word?.stats != null ? creature.Word.stats.slowSeconds : 0f;
        creature.SlowSeconds = seconds > 0f ? seconds : config.slowdownDefaultSeconds;
        creature.SlowMultiplier = config.slowdownSpeedMultiplier;
    }
}
