using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın sol üstünde bu bilgisayardaki oyuncunun canını yazar ("Can: 3 / 3").
/// Yalnızca gösterir; can hesabı <see cref="Health"/> sınıfındadır. Oyuncu ağda maç başlayınca belli olur,
/// bu yüzden her karede okunur. Vuruş hissi: yerel oyuncu vurulunca yazı kısa süre sallanıp kırmızı yanar, can düşükken
/// kalp atışıyla kırmızıya nabız atar (zamanlama <see cref="HitFeelDirector"/>'de).
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] TurnViewSource runner;
    [SerializeField] Text label;

    GameConfig config;
    RectTransform labelRect;
    Vector2 basePosition;
    Color baseColor = Color.white;

    void Awake()
    {
        // Uzun "Elendin..." yazısı alt satıra kayıp altındaki satırla çakışmasın: tek satırda sağa taşsın.
        if (label != null)
        {
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelRect = label.rectTransform;
            basePosition = labelRect.anchoredPosition;
            baseColor = label.color;
        }
        config = GameConfigLoader.Get();
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
        ApplyHurtFeel();
    }

    /// <summary>Vurulunca sallanma ve kırmızı parlama; düşük canda kırmızı nabız.</summary>
    void ApplyHurtFeel()
    {
        if (config == null || labelRect == null) return;

        float age = HitFeelDirector.LocalHitAge;
        float seconds = config.healthShakeSeconds;
        float hurt = seconds > 0f && age < seconds && !HitFeelDirector.LastLocalHitBlocked ? 1f - age / seconds : 0f;

        // Sönen, hızlı sağ-sol sallanma (ayarlarda sarsıntı kapalıysa sallanmaz, yalnızca renk).
        float shake = hurt * hurt * config.healthShakePixels * SettingsService.ScreenShakeScale;
        labelRect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(age * 70f) * shake, Mathf.Cos(age * 53f) * shake * 0.4f);

        float red = Mathf.Max(hurt, HitFeelDirector.LowHealthPulse * 0.8f);
        label.color = Color.Lerp(baseColor, config.healthHurtColor, red);
    }
}
