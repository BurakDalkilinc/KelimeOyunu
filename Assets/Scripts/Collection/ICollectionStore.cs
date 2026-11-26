/// <summary>
/// Defter kaydının okunup yazıldığı yer. Oyunda diskteki dosya (<see cref="FileCollectionStore"/>), testte bellekteki bir metin.
/// </summary>
public interface ICollectionStore
{
    /// <summary>Kayıt metni; kayıt yoksa ya da okunamıyorsa null.</summary>
    string Read();

    /// <summary>Kayıt metnini yazar. Başarılıysa true.</summary>
    bool Write(string text);
}
