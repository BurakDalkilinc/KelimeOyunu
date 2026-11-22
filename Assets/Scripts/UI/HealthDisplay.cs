using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın sol üstünde oyuncunun canını yazar ("Can: 3 / 3").
/// Yalnızca gösterir; can hesabı <see cref="Health"/> sınıfındadır.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Text label;

    Health health;

    void Start()
    {
        if (player == null || label == null)
        {
            Debug.LogError("HealthDisplay: player ya da label atanmamış.", this);
            return;
        }

        health = player.Health;
        health.Changed += OnChanged;
        Refresh(health.Current, health.Max);
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    void OnChanged(int current, int max)
    {
        Refresh(current, max);
    }

    void Refresh(int current, int max)
    {
        label.text = current > 0 ? $"Can: {current} / {max}" : "Elendin";
    }
}
