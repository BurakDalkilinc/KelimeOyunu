/// <summary>
/// "Yeniden canlanma" kelimesinin elenmiş bir oyuncuyu geri getirmesini sağlayan hizmet. Elenenlerden rastgele biri
/// dirilir. Kelime etiketi (<see cref="ReviveTag"/>) sadece bu hizmeti çağırır.
/// </summary>
public interface IRevivalService
{
    /// <summary>
    /// Yazan oyuncunun kelimesiyle bir diriliş başlatır. Hayalet yoksa hiçbir şey olmaz.
    /// </summary>
    void RequestRevival(PlayerState writer);
}
