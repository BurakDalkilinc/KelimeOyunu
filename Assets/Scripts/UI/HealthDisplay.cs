using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın sol üstünde bu bilgisayardaki oyuncunun canını yazar ("Can: 3 / 3").
/// Yalnızca gösterir; can hesabı <see cref="Health"/> sınıfındadır. Oyuncu ağda maç başlayınca belli olur,
/// bu yüzden her karede okunur.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    void Update()
    {
        if (runner == null || label == null) return;

        PlayerState player = runner.LocalPlayer;
        if (player == null)
        {
            label.text = string.Empty;
            return;
        }

        Health health = player.Health;
        label.text = health.Current > 0 ? $"Can: {health.Current} / {health.Max}" : "Elendin: hayalet olarak izliyorsun";
    }
}
