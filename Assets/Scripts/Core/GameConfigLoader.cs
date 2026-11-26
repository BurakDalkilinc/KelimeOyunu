using UnityEngine;

/// <summary>
/// Sahneye bağlı olmayan servislerin (ses, ayarlar) ayar dosyasını bulduğu yer: Resources/Data/GameConfig.
/// Bulunamazsa koddaki varsayılanlarla geçici bir ayar nesnesi kurulur.
/// </summary>
public static class GameConfigLoader
{
    const string ResourcePath = "Data/GameConfig";

    static GameConfig config;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => config = null;

    /// <summary>Ayarlar; ilk çağrıda yüklenir.</summary>
    public static GameConfig Get()
    {
        if (config == null)
        {
            config = Resources.Load<GameConfig>(ResourcePath);
            if (config == null) config = ScriptableObject.CreateInstance<GameConfig>();
        }
        return config;
    }
}
