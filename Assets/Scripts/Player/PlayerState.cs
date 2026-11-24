/// <summary>
/// Maçtaki bir oyuncunun durumu (insan ya da bot). Unity'den bağımsız düz C# sınıfı.
/// Can <see cref="Health"/> içindedir; can 0 olunca oyuncu elenmiş sayılır.
/// </summary>
public class PlayerState
{
    public int Id { get; }
    public string Name { get; }
    /// <summary>Bot mu? İnsan oyuncu maç sırasında koparsa bota dönüşür (<see cref="BecomeBot"/>).</summary>
    public bool IsBot { get; private set; }
    public Health Health { get; }

    /// <summary>Üzerindeki süreli etkiler (yavaşlama...). Süreyi karakterin kendisi ilerletir.</summary>
    public StatusEffects Status { get; } = new StatusEffects();

    /// <summary>Zar için: üst üste kaç tur seçilmedi. Ne kadar büyükse seçilme şansı o kadar artar.</summary>
    public int MissedTurns { get; set; }

    /// <summary>
    /// Hasar alır. Kalkan varsa hasarı engeller (hakkı bir azalır, can düşmez). Gerçekten düşen canı döndürür.
    /// Oyundaki bütün hasar bu yoldan geçer ki kalkan her yerde işlesin.
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive) return 0;
        if (Status.TryAbsorbHit()) return 0;
        return Health.TakeDamage(amount);
    }

    /// <summary>Lava ya da uçuruma düşüyor mu? Düşerken hareket edemez; animasyon bitince <see cref="Eliminate"/> çağrılır.</summary>
    public FallKind Falling { get; set; }

    /// <summary>Bütün canı anında alır (lava/uçuruma düşme). Kalkan da engelleyemez.</summary>
    public void Eliminate()
    {
        Falling = FallKind.None;
        if (IsAlive) Health.TakeDamage(Health.Current);
    }

    /// <summary>Elendi mi? Elenenler hayalet olur: izlemeye devam eder, hiçbir şeyi etkilemez, hiçbir şey ona işlemez.</summary>
    public bool IsGhost => Health.IsDead;

    /// <summary>Hayatta mı? (Elenenler hayalet olur, zarda seçilmez.)</summary>
    public bool IsAlive => !Health.IsDead;

    /// <summary>Oyuncuyu bota çevirir (bağlantısı koptu): bundan sonra tur sırası gelince kelimeyi bot yazar, hedefi bot seçer.</summary>
    public void BecomeBot()
    {
        IsBot = true;
    }

    public PlayerState(int id, string name, int startingHealth, bool isBot = false)
    {
        Id = id;
        Name = name;
        IsBot = isBot;
        Health = new Health(startingHealth);
    }
}
