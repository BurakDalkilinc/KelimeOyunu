/// <summary>
/// Kovalayan yaratığın atılma (pounce) aşaması: kovalıyor → hazırlık (durur, titrer, kırmızı yanar) → atılma (hızlı, düz).
/// </summary>
public enum PounceStage
{
    None,
    Windup,
    Lunge,
}
