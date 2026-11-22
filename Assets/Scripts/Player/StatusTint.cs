using UnityEngine;

/// <summary>
/// Oyuncunun üzerindeki etkilere göre karakterin rengini hesaplar: yavaşlayan mavimsi, kalkanlı açık camgöbeği,
/// uçan açık, görünmez soluk (yarı saydam). Düz hesap; renk atamasını karakterler yapar.
/// </summary>
public static class StatusTint
{
    static readonly Color SlowTint = new Color(0.45f, 0.65f, 1f);
    static readonly Color ShieldTint = new Color(0.55f, 0.95f, 1f);
    static readonly Color FlightTint = new Color(1f, 1f, 0.8f);

    /// <summary>Görünmez karakterin saydamlığı (0 = tamamen görünmez).</summary>
    public const float StealthAlpha = 0.3f;

    /// <summary>Hayaletin (elenmiş oyuncunun) saydamlığı.</summary>
    public const float GhostAlpha = 0.35f;

    /// <summary>Hayalet görünümü: taban renk beyaza doğru solar ve yarı saydam olur.</summary>
    public static Color Ghost(Color baseColor)
    {
        Color color = Color.Lerp(baseColor, Color.white, 0.5f);
        color.a = GhostAlpha;
        return color;
    }

    /// <summary>Taban rengi etkilere göre değiştirir. Etki yoksa taban renk aynen döner.</summary>
    public static Color Apply(Color baseColor, StatusEffects status)
    {
        if (status == null) return baseColor;

        Color color = baseColor;
        if (status.IsSlowed) color = Color.Lerp(color, SlowTint, 0.6f);
        if (status.HasShield) color = Color.Lerp(color, ShieldTint, 0.5f);
        if (status.IsFlying) color = Color.Lerp(color, FlightTint, 0.5f);
        if (status.IsStealthed) color.a = Mathf.Min(color.a, StealthAlpha);
        return color;
    }
}
