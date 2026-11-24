using System;
using UnityEngine;

/// <summary>Karakterin üzerindeki etkilerin küçük bayrak kümesi: ağda tek sayı olarak gider, herkes rengini buradan hesaplar.</summary>
[Flags]
public enum StatusFlags
{
    None = 0,
    Slow = 1,
    Shield = 2,
    Stealth = 4,
    Flight = 8,
    Pull = 16,
    FallChasm = 32,
    FallLava = 64,
    Weapon = 128,
}

/// <summary>Etkiler ile bayraklar arasında çeviri ve bayraklara göre renk. Düz hesap, test edilebilir.</summary>
public static class StatusFlagsUtil
{
    static readonly Color SlowTint = new Color(0.45f, 0.65f, 1f);
    static readonly Color ShieldTint = new Color(0.55f, 0.95f, 1f);
    static readonly Color FlightTint = new Color(1f, 1f, 0.8f);

    /// <summary>Oyuncunun şu anki etkilerini bayraklara çevirir.</summary>
    public static StatusFlags Of(StatusEffects status)
    {
        if (status == null) return StatusFlags.None;

        StatusFlags flags = StatusFlags.None;
        if (status.IsSlowed) flags |= StatusFlags.Slow;
        if (status.HasShield) flags |= StatusFlags.Shield;
        if (status.IsStealthed) flags |= StatusFlags.Stealth;
        if (status.IsFlying) flags |= StatusFlags.Flight;
        if (status.IsPulled) flags |= StatusFlags.Pull;
        return flags;
    }

    /// <summary>
    /// Taban rengi bayraklara göre değiştirir; <see cref="StatusTint.Apply"/> ile aynı sonucu verir
    /// (görünmezlik herkese soluk görünür, çünkü yalnızca hedef alınamazlık sağlar).
    /// </summary>
    public static Color Tint(Color baseColor, StatusFlags flags)
    {
        Color color = baseColor;
        if ((flags & StatusFlags.Slow) != 0) color = Color.Lerp(color, SlowTint, 0.6f);
        if ((flags & StatusFlags.Shield) != 0) color = Color.Lerp(color, ShieldTint, 0.5f);
        if ((flags & StatusFlags.Flight) != 0) color = Color.Lerp(color, FlightTint, 0.5f);
        if ((flags & StatusFlags.Stealth) != 0) color.a = Mathf.Min(color.a, StatusTint.StealthAlpha);
        return color;
    }
}
