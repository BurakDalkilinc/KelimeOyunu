/// <summary>
/// "weapon": yazana bir silah verir. Silahı tutan oyuncunun Boşluk tuşu itiş yerine ateş eder; mermi baktığı yöne
/// düz uçar ve değdiği ilk rakibe kelimenin "stats.damage" kadar hasar verir (kalkan engeller, tahta duvar durdurur).
/// Atış sayısı "stats.count" (yazmıyorsa 1), silahın elde kalma süresi "stats.duration" (yazmıyorsa GameConfig'teki varsayılan).
/// Mermi hızı "stats.speed", mermi büyüklüğü "stats.radius" (bkz. <see cref="WeaponFire"/>).
/// </summary>
public class WeaponTag : ITagBehaviour, ISelfEffectTag
{
    public string Tag => "weapon";

    public void Configure(CreatureModel creature, SpawnContext context) { }

    public void ApplySelf(SelfEffectContext context)
    {
        WordStats stats = context.Word?.stats;
        int shots = stats != null && stats.count > 0 ? stats.count : 1;
        float seconds = stats != null && stats.duration > 0f ? stats.duration
                      : context.Config != null ? context.Config.weaponDefaultSeconds : 15f;

        context.Writer.Status.ArmWeapon(context.Word, shots, seconds);
    }
}
