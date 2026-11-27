using System.Collections.Generic;

/// <summary>
/// Oyuncunun etkilerini gösterge simgelerine çevirir: tek kişilikte <see cref="StatusEffects"/>'ten (kalan süreler ve
/// kalkan/atış sayısıyla), ağda <see cref="StatusFlags"/>'ten (istemci süreleri bilmez, yalnızca simge). Düz hesap, test edilebilir.
/// </summary>
public static class HudStatusCollector
{
    /// <summary>Etkileri listeye ekler (silah dahil; silahın resmi kelimenin resmidir).</summary>
    public static void Collect(StatusEffects status, List<HudStatus> into)
    {
        if (status == null || into == null) return;

        if (status.HasShield) into.Add(new HudStatus(HudStatusKind.Shield, status.ShieldTimeLeft, status.ShieldCharges));
        if (status.HasWeapon)
            into.Add(new HudStatus(HudStatusKind.Weapon, status.WeaponTimeLeft, status.WeaponShots, WeaponSprite(status.Weapon)));
        if (status.IsDriving)
            into.Add(new HudStatus(HudStatusKind.Vehicle, status.VehicleTimeLeft, -1, WeaponSprite(status.Vehicle)));
        if (status.IsFlying) into.Add(new HudStatus(HudStatusKind.Flight, status.FlightTimeLeft));
        if (status.IsStealthed)
            into.Add(new HudStatus(status.StealthDisguise != null ? HudStatusKind.Disguise : HudStatusKind.Stealth, status.StealthTimeLeft));
        if (status.IsBurning) into.Add(new HudStatus(HudStatusKind.Burn, status.BurnTimeLeft));
        if (status.IsSlowed) into.Add(new HudStatus(HudStatusKind.Slow, status.SlowTimeLeft));
        if (status.IsPulled) into.Add(new HudStatus(HudStatusKind.Pull, status.PullTimeLeft));
        HudRules.SortByPriority(into);
    }

    /// <summary>Ağdaki etki bayraklarından süresiz simgeler.</summary>
    public static void Collect(StatusFlags flags, List<HudStatus> into)
    {
        if (into == null) return;

        if ((flags & StatusFlags.Shield) != 0) into.Add(new HudStatus(HudStatusKind.Shield, -1f));
        if ((flags & StatusFlags.Weapon) != 0) into.Add(new HudStatus(HudStatusKind.Weapon, -1f));
        if ((flags & StatusFlags.Vehicle) != 0) into.Add(new HudStatus(HudStatusKind.Vehicle, -1f));
        if ((flags & StatusFlags.Flight) != 0) into.Add(new HudStatus(HudStatusKind.Flight, -1f));
        if ((flags & StatusFlags.Stealth) != 0) into.Add(new HudStatus(HudStatusKind.Stealth, -1f));
        if ((flags & StatusFlags.Burning) != 0) into.Add(new HudStatus(HudStatusKind.Burn, -1f));
        if ((flags & StatusFlags.Slow) != 0) into.Add(new HudStatus(HudStatusKind.Slow, -1f));
        if ((flags & StatusFlags.Pull) != 0) into.Add(new HudStatus(HudStatusKind.Pull, -1f));
        HudRules.SortByPriority(into);
    }

    /// <summary>
    /// Kelimenin kendi resmi (ör. tabanca) — yalnızca JSON'da adı verilmiş ya da kimliğiyle resmi olan kelimelerde; yoksa null
    /// (gösterge tür simgesine düşer). Resim yüklemez, yalnızca adı seçer; varlığını gösterge denetler.
    /// </summary>
    public static string WeaponSprite(WordEntry word)
    {
        if (word == null) return null;
        if (!string.IsNullOrEmpty(word.sprite)) return word.sprite;
        return string.IsNullOrEmpty(word.id) ? null : word.id;
    }
}
