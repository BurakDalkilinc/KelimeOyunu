using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyuncunun üzerindeki süreli etkileri ekranda yazar (ör. "Yavaşladın! (2 sn)"). Yalnızca gösterir;
/// etkilerin kendisi <see cref="StatusEffects"/>'tedir, metin <see cref="StatusText"/>'tedir.
/// </summary>
public class StatusEffectDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    void Update()
    {
        if (runner == null || label == null) return;
        label.text = runner.DescribeLocalStatus();
    }
}
