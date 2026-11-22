/// <summary>
/// Tehdit etkilerinin sahneye yaratık (ör. meteor) koyabildiği yer. Gerçek uygulaması <see cref="CreatureSpawner"/>;
/// testlerde yaratıkları toplayan sahtesi kullanılır.
/// </summary>
public interface ICreatureSink
{
    /// <summary>Yaratığı sahneye koyar. <paramref name="label"/> boşsa yaratığın üstünde yazı olmaz.</summary>
    void Spawn(CreatureModel model, string label);
}
