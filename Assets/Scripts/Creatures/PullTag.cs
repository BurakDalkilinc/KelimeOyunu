/// <summary>
/// "pull": çekme. Yaratık (mıknatıs) koşmaz; arenanın merkezine düşer ve süresi boyunca hedefi o noktaya çeker.
/// Çekme gücü kelimenin "stats.pullForce" değerinden gelir (yazmıyorsa GameConfig'teki varsayılan).
/// Güç oyuncu hızından düşük olduğu için hedef ters yönde yürüyerek direnebilir. Etkisi yalnızca hedefedir.
/// </summary>
public class PullTag : ITagBehaviour
{
    public string Tag => "pull";

    public void Configure(CreatureModel creature, SpawnContext context)
    {
        creature.IsPull = true;

        GameConfig config = context.Config;
        float force = creature.Word?.stats != null ? creature.Word.stats.pullForce : 0f;
        creature.PullForce = force > 0f ? force : (config != null ? config.pullDefaultForce : 4f);
        creature.PullLingerSeconds = config != null ? config.pullLingerSeconds : 0.3f;
    }
}
