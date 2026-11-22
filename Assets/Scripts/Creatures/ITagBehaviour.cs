/// <summary>
/// Bir etiketin (ör. "undead") yaratığa kattığı davranış. Her etiket kendi küçük sınıfıdır;
/// kelime adına özel "if (word == ...)" kodu yazılmaz.
/// </summary>
public interface ITagBehaviour
{
    /// <summary>Etiketin adı (words.json ve GDD'deki yazımıyla).</summary>
    string Tag { get; }

    /// <summary>Yaratık doğarken çağrılır; yaratığın ayarlarını bu etikete göre değiştirir.</summary>
    void Configure(CreatureModel creature, SpawnContext context);
}
