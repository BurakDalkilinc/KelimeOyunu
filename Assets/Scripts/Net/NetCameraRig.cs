using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ sahnesinde kameranın bağlantıları: bu bilgisayardaki oyuncunun karakterini izler; hedef seçerken ya da çözüm alanları
/// çıkınca tüm arenayı gösterir (<see cref="CameraFollow"/>).
/// </summary>
public class NetCameraRig : MonoBehaviour
{
    [SerializeField] CameraFollow follow;
    [SerializeField] NetTurnChannel channel;

    void OnEnable()
    {
        if (follow == null || channel == null)
        {
            Debug.LogError("NetCameraRig: follow ya da channel eksik.", this);
            enabled = false;
            return;
        }

        follow.TargetProvider = LocalAvatarPosition;
        follow.OverviewWanted = () => channel.IAmChoosingTarget || (channel.LatestZones != null && channel.LatestZones.Count > 0);
    }

    void OnDisable()
    {
        if (follow == null) return;
        follow.TargetProvider = null;
        follow.OverviewWanted = null;
    }

    /// <summary>Bu bilgisayardaki oyuncunun karakteri; henüz yoksa null (kamera ortada durur).</summary>
    static Vector2? LocalAvatarPosition()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return null;
        return (Vector2)manager.LocalClient.PlayerObject.transform.position;
    }
}
