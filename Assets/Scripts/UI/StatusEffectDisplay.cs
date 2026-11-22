using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyuncunun üzerindeki süreli etkileri ekranda yazar (ör. "Yavaşladın! (2 sn)"). Yalnızca gösterir;
/// etkilerin kendisi <see cref="StatusEffects"/>'tedir.
/// </summary>
public class StatusEffectDisplay : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Text label;

    void Update()
    {
        if (player == null || label == null) return;

        StatusEffects status = player.State?.Status;
        label.text = status != null && status.IsSlowed
            ? $"Yavaşladın! ({Mathf.CeilToInt(status.SlowTimeLeft)} sn)"
            : string.Empty;
    }
}
