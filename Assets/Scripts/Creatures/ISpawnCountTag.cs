/// <summary>
/// Kelimeden kaç yaratık doğacağını değiştiren etiketler bunu da uygular (ör. "swarm": sürü).
/// Etiketi olmayan kelime tek yaratık doğurur.
/// </summary>
public interface ISpawnCountTag
{
    /// <summary>Bu kelimeden doğacak yaratık sayısı (en az 1).</summary>
    int GetSpawnCount(WordEntry word);
}
