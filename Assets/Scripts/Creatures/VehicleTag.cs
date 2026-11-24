/// <summary>
/// "vehicle": yazan araca (motor, araba) biner. Araç süresi ("stats.duration") bitene kadar hiç durmaz, oyuncu yön verir;
/// değdiğini ezer. "explosive" etiketi de varsa binaya çarpınca patlar (bkz. <see cref="VehicleRules"/>).
/// </summary>
public class VehicleTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "vehicle";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        WordStats stats = context.Word?.stats;
        float seconds = stats != null && stats.duration > 0f ? stats.duration
                      : context.Config != null ? context.Config.vehicleDefaultSeconds : 8f;

        context.Writer.Status.Mount(context.Word, seconds);
    }
}
