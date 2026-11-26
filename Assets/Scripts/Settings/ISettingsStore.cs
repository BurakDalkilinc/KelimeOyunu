/// <summary>
/// Ayar kaydının okunup yazıldığı yer. Oyunda diskteki dosya (<see cref="FileSettingsStore"/>), testte bellekteki bir metin.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Kayıt metni; kayıt yoksa ya da okunamıyorsa null.</summary>
    string Read();

    /// <summary>Kayıt metnini yazar. Başarılıysa true.</summary>
    bool Write(string text);
}
