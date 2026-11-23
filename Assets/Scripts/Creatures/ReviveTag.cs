/// <summary>
/// "revive": yazan oyuncu "yeniden canlanma" yazarsa elenenlerden biri yeniden doğar. Kimin dirileceğini hayaletler oylar
/// (kimse oy vermezse rastgele); yeniden doğan oyuncu <c>GameConfig.reviveHealth</c> canla döner.
/// Etki, yazanın üzerine değil elenmiş bir oyuncunun üzerine gider; bu yüzden iş <see cref="IRevivalService"/>'e devredilir.
/// </summary>
public class ReviveTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "revive";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        context.Revival?.RequestRevival(context.Writer);
    }
}
