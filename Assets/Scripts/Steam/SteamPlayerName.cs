using Steamworks;
using UnityEngine;

/// <summary>
/// Steam açıksa oyuncunun görünen adı Steam persona adıdır: oyun açılırken <see cref="LocalPlayerName.Provider"/>'a
/// bağlanır. Steam başlatılmadıysa boş döner (host "Oyuncu N" der).
/// </summary>
public static class SteamPlayerName
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        LocalPlayerName.Provider = () => SteamClient.IsValid ? SteamClient.Name : null;
    }
}
