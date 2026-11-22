/// <summary>
/// Yazanın kendine uyguladığı etkileri olan etiketler (can, kalkan, görünmezlik, uçma...) bunu uygular.
/// Yaratık doğurmazlar; etki doğrudan yazan oyuncunun üzerine gider.
/// </summary>
public interface ISelfEffectTag
{
    /// <summary>Etkiyi yazan oyuncuya uygular.</summary>
    void ApplySelf(SelfEffectContext context);
}
