using UnityEngine;

/// <summary>İtişin bekleme süresi. Düz hesap, zamanı dışarıdan alır, test edilebilir.</summary>
public class ShoveCooldown
{
    readonly float seconds;

    public float Remaining { get; private set; }

    public bool IsReady => Remaining <= 0f;

    public ShoveCooldown(float seconds)
    {
        this.seconds = Mathf.Max(0f, seconds);
    }

    /// <summary>Hazırsa itişi kullanır (bekleme süresini başlatır) ve true döner; hazır değilse false.</summary>
    public bool TryUse()
    {
        if (!IsReady) return false;
        Remaining = seconds;
        return true;
    }

    /// <summary>Hazırsa kullanır ve bekleme süresini bu kez <paramref name="customSeconds"/> yapar (boss'a vuruş gibi).</summary>
    public bool TryUse(float customSeconds)
    {
        if (!IsReady) return false;
        Remaining = Mathf.Max(0f, customSeconds);
        return true;
    }

    public void Tick(float deltaSeconds)
    {
        if (Remaining > 0f) Remaining = Mathf.Max(0f, Remaining - Mathf.Max(0f, deltaSeconds));
    }

    /// <summary>Bekleme süresini sıfırlar (yeni maç).</summary>
    public void Reset()
    {
        Remaining = 0f;
    }

    /// <summary>Arayüz yazısı: "İtiş (Boşluk): hazır" ya da "İtiş: 2 sn".</summary>
    public static string Describe(float remainingSeconds)
    {
        return remainingSeconds <= 0f ? "İtiş (Boşluk): hazır" : $"İtiş: {Mathf.CeilToInt(remainingSeconds)} sn";
    }
}
