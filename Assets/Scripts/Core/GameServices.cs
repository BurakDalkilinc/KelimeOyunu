using UnityEngine;

/// <summary>
/// Sahnelerden bağımsız servisleri oyun açılırken bir kez kurar: ses (<see cref="AudioService"/>) ve Esc kısayolu
/// (<see cref="SettingsHotkey"/>); ardından kayıtlı ayarları uygular. Nesne sahneler arasında kalır; sahneye elle koymak gerekmez.
/// </summary>
public static class GameServices
{
    const string ObjectName = "Oyun Servisleri";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (AudioService.Instance != null) return;

        var host = new GameObject(ObjectName);
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AudioService>();
        host.AddComponent<SettingsHotkey>();

        SettingsService.ApplyAll();
    }
}
