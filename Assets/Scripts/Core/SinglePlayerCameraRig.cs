using UnityEngine;

/// <summary>
/// Tek kişilik sahnede kameranın bağlantıları: oyuncuyu izler; hedef seçerken ya da çözüm alanları (sığınak, kule...) çıkınca
/// tüm arenayı gösterir.
/// </summary>
public class SinglePlayerCameraRig : MonoBehaviour
{
    [SerializeField] CameraFollow follow;
    [SerializeField] PlayerController player;
    [SerializeField] TargetSelector targetSelector;
    [SerializeField] ThreatDirector threatDirector;

    void OnEnable()
    {
        if (follow == null || player == null)
        {
            Debug.LogError("SinglePlayerCameraRig: follow ya da player eksik.", this);
            enabled = false;
            return;
        }

        follow.TargetProvider = () => (Vector2?)(Vector2)player.transform.position;
        follow.OverviewWanted = () => (targetSelector != null && targetSelector.IsSelecting)
                                      || (threatDirector != null && threatDirector.HasZones);
    }

    void OnDisable()
    {
        if (follow == null) return;
        follow.TargetProvider = null;
        follow.OverviewWanted = null;
    }
}
