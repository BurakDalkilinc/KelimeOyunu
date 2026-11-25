using UnityEngine;

/// <summary>
/// Büyük tehditlerin görsel temaları (words.json'daki "fx" alanı): ekranın renk tonu, uyarı süresince çıkan parçacıklar ve
/// etki anı parlamasının rengi. Kelimeye değil temaya bağlıdır; yeni tehdit yalnızca var olan bir temayı seçer.
/// </summary>
public sealed class ThreatFxPreset
{
    public enum ParticleKind { None, Ash, Sparks, Snow, Fog, Embers }

    public string Name;
    public Color Tint = Color.white;         // Ekranın döndüğü renk
    public float MaxTint = 0.25f;            // Uyarı sonunda ulaşılan ton yoğunluğu
    public Color Flash = Color.white;        // Etki anı parlaması
    public float AfterglowSeconds = 1.5f;    // Etkiden sonra tonun sönme süresi
    public ParticleKind Particles = ParticleKind.None;
    public ParticleKind SecondParticles = ParticleKind.None;
    public Color ParticleColor = Color.white;
    public float ParticlesPerSecond = 20f;   // Uyarının sonunda saniyedeki parçacık sayısı

    static readonly ThreatFxPreset Nuke = new ThreatFxPreset
    {
        Name = "nuke", Tint = new Color(1f, 0.72f, 0.4f), MaxTint = 0.25f, Flash = new Color(1f, 0.97f, 0.8f),
        Particles = ParticleKind.Ash, ParticleColor = new Color(0.45f, 0.42f, 0.4f, 0.8f), ParticlesPerSecond = 18f,
    };

    static readonly ThreatFxPreset Solar = new ThreatFxPreset
    {
        Name = "solar", Tint = new Color(1f, 0.93f, 0.45f), MaxTint = 0.5f, Flash = Color.white, AfterglowSeconds = 2.5f,
        Particles = ParticleKind.Sparks, ParticleColor = new Color(1f, 0.85f, 0.3f, 0.95f), ParticlesPerSecond = 30f,
    };

    static readonly ThreatFxPreset Ice = new ThreatFxPreset
    {
        Name = "ice", Tint = new Color(0.55f, 0.8f, 1f), MaxTint = 0.4f, Flash = new Color(0.8f, 0.93f, 1f), AfterglowSeconds = 4f,
        Particles = ParticleKind.Snow, ParticleColor = Color.white, ParticlesPerSecond = 35f,
    };

    static readonly ThreatFxPreset Toxic = new ThreatFxPreset
    {
        Name = "toxic", Tint = new Color(0.45f, 0.95f, 0.3f), MaxTint = 0.35f, Flash = new Color(0.6f, 1f, 0.45f), AfterglowSeconds = 3f,
        Particles = ParticleKind.Fog, ParticleColor = new Color(0.5f, 0.95f, 0.3f, 0.2f), ParticlesPerSecond = 3f,
    };

    static readonly ThreatFxPreset Lava = new ThreatFxPreset
    {
        Name = "lava", Tint = new Color(1f, 0.35f, 0.1f), MaxTint = 0.35f, Flash = new Color(1f, 0.6f, 0.25f), AfterglowSeconds = 2f,
        Particles = ParticleKind.Embers, SecondParticles = ParticleKind.Ash, ParticleColor = new Color(1f, 0.55f, 0.1f, 0.95f), ParticlesPerSecond = 28f,
    };

    /// <summary>Tema adına göre ön ayar; bilinmiyorsa ya da boşsa null (eski davranış: beyaz parlama, ton yok).</summary>
    public static ThreatFxPreset For(string name)
    {
        switch (name)
        {
            case "nuke": return Nuke;
            case "solar": return Solar;
            case "ice": return Ice;
            case "toxic": return Toxic;
            case "lava": return Lava;
            default: return null;
        }
    }
}
