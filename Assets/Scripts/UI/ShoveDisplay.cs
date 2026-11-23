using UnityEngine;
using UnityEngine.UI;

/// <summary>Sol üstte oyuncunun itiş durumunu yazar ("İtiş (Boşluk): hazır" / "İtiş: 2 sn"). Yalnızca gösterir.</summary>
public class ShoveDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    void Update()
    {
        if (runner == null || label == null) return;
        label.text = runner.DescribeShove();
    }
}
