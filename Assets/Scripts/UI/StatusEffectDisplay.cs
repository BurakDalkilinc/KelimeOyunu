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
        if (status == null)
        {
            label.text = string.Empty;
            return;
        }

        string text = string.Empty;
        if (status.IsSlowed) text += $"Yavaşladın! ({Mathf.CeilToInt(status.SlowTimeLeft)} sn)";
        if (status.IsPulled) text += (text.Length > 0 ? "  " : string.Empty) + "Çekiliyorsun!";
        label.text = text;
    }
}
