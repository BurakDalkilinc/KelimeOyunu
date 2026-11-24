/// <summary>
/// "stealth": yazanı görünmez yapar. Bu süre boyunca (kelimenin "stats.duration" değeri) saldırı kelimelerinin
/// hedefi olarak seçilemez. Zaten peşindeki yaratıklar peşini bırakmaz.
/// </summary>
public class StealthTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "stealth";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        float seconds = context.Word?.stats != null && context.Word.stats.duration > 0f
            ? context.Word.stats.duration
            : (context.Config != null ? context.Config.stealthDefaultSeconds : 6f);

        // Kelimenin kendi resmi varsa (kamuflaj → çalı) karakter ona bürünür; yoksa yalnızca soluklaşır.
        string disguise = context.Word != null && !string.IsNullOrEmpty(context.Word.sprite) ? context.Word.sprite : null;
        context.Writer.Status.ApplyStealth(seconds, disguise);
    }
}
