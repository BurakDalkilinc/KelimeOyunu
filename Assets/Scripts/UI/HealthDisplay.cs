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

    void Awake()
    {
        // Uzun "Elendin..." yazısı alt satıra kayıp altındaki satırla çakışmasın: tek satırda sağa taşsın.
        if (label != null) label.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

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
        label.text = health.Current > 0 ? Loc.F("health.value", health.Current, health.Max) : Loc.T("health.ghost");
    }
}
