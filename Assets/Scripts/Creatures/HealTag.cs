/// <summary>
/// "heal": yazana can verir (kelimenin "stats.heal" değeri kadar, yazmıyorsa 1). Başlangıç canını aşmaz.
/// Elenmiş oyuncu iyileşmez.
/// </summary>
public class HealTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "heal";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        int amount = context.Word?.stats != null ? UnityEngine.Mathf.RoundToInt(context.Word.stats.heal) : 0;
        context.Writer.Health.Heal(amount > 0 ? amount : 1);
    }
}
