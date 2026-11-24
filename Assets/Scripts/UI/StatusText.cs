using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun üzerindeki etkilerin Türkçe özeti ("Yavaşladın! (2 sn)  Kalkan var"). Tek kişilikte kalan süreler yazılır;
/// ağda istemci süreleri bilmez, bu yüzden bayraklardan süresiz özet çıkar. Düz hesap, test edilebilir.
/// </summary>
public static class StatusText
{
    public static string Describe(StatusEffects status)
    {
        if (status == null) return string.Empty;

        var parts = new List<string>();
        if (status.IsSlowed) parts.Add($"Yavaşladın! ({Mathf.CeilToInt(status.SlowTimeLeft)} sn)");
        if (status.IsPulled) parts.Add("Çekiliyorsun!");
        if (status.HasShield) parts.Add("Kalkan var");
        if (status.IsDriving) parts.Add($"{status.Vehicle.display} sürüyorsun ({Mathf.CeilToInt(status.VehicleTimeLeft)} sn) — eve çarparsan patlarsın!");
        if (status.IsStealthed) parts.Add(status.StealthDisguise != null
            ? $"Kamufle oldun ({Mathf.CeilToInt(status.StealthTimeLeft)} sn)"
            : $"Görünmezsin ({Mathf.CeilToInt(status.StealthTimeLeft)} sn)");
        if (status.IsFlying) parts.Add($"Uçuyorsun ({Mathf.CeilToInt(status.FlightTimeLeft)} sn)");
        return string.Join("  ", parts);
    }

    public static string Describe(StatusFlags flags)
    {
        var parts = new List<string>();
        if ((flags & StatusFlags.Slow) != 0) parts.Add("Yavaşladın!");
        if ((flags & StatusFlags.Pull) != 0) parts.Add("Çekiliyorsun!");
        if ((flags & StatusFlags.Shield) != 0) parts.Add("Kalkan var");
        if ((flags & StatusFlags.Stealth) != 0) parts.Add("Görünmezsin");
        if ((flags & StatusFlags.Flight) != 0) parts.Add("Uçuyorsun");
        if ((flags & StatusFlags.Vehicle) != 0) parts.Add("Araç sürüyorsun — eve çarparsan patlarsın!");
        return string.Join("  ", parts);
    }
}
