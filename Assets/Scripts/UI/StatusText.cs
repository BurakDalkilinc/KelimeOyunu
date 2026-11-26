using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun üzerindeki etkilerin seçili dildeki özeti ("Yavaşladın! (2 sn)  Kalkan var"). Tek kişilikte kalan süreler yazılır;
/// ağda istemci süreleri bilmez, bu yüzden bayraklardan süresiz özet çıkar. Düz hesap, test edilebilir.
/// </summary>
public static class StatusText
{
    public static string Describe(StatusEffects status)
    {
        if (status == null) return string.Empty;

        var parts = new List<string>();
        if (status.IsSlowed) parts.Add(Loc.F("status.slowed_time", Mathf.CeilToInt(status.SlowTimeLeft)));
        if (status.IsPulled) parts.Add(Loc.T("status.pulled"));
        if (status.HasShield) parts.Add(Loc.T("status.shield"));
        if (status.IsBurning) parts.Add(Loc.T("status.burning"));
        if (status.IsDriving) parts.Add(Loc.F("status.driving_time", status.Vehicle.LocalizedDisplay, Mathf.CeilToInt(status.VehicleTimeLeft))
                                        + (VehicleRules.Explodes(status.Vehicle) ? Loc.T("status.driving_explodes") : ""));
        if (status.IsStealthed) parts.Add(status.StealthDisguise != null
            ? Loc.F("status.disguised_time", Mathf.CeilToInt(status.StealthTimeLeft))
            : Loc.F("status.stealth_time", Mathf.CeilToInt(status.StealthTimeLeft)));
        if (status.IsFlying) parts.Add(Loc.F("status.flying_time", Mathf.CeilToInt(status.FlightTimeLeft)));
        return string.Join("  ", parts);
    }

    public static string Describe(StatusFlags flags)
    {
        var parts = new List<string>();
        if ((flags & StatusFlags.Slow) != 0) parts.Add(Loc.T("status.slowed"));
        if ((flags & StatusFlags.Pull) != 0) parts.Add(Loc.T("status.pulled"));
        if ((flags & StatusFlags.Shield) != 0) parts.Add(Loc.T("status.shield"));
        if ((flags & StatusFlags.Stealth) != 0) parts.Add(Loc.T("status.stealth"));
        if ((flags & StatusFlags.Flight) != 0) parts.Add(Loc.T("status.flying"));
        if ((flags & StatusFlags.Burning) != 0) parts.Add(Loc.T("status.burning"));
        if ((flags & StatusFlags.Vehicle) != 0) parts.Add(Loc.T("status.vehicle_net"));
        return string.Join("  ", parts);
    }
}
