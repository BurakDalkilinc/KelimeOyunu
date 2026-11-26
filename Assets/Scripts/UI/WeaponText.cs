using UnityEngine;

/// <summary>Silah tutan oyuncunun ekrandaki "Boşluk" satırı ("Silah (Boşluk): 1 atış, 12 sn"). Düz hesap, test edilebilir.</summary>
public static class WeaponText
{
    /// <summary>Ağda istemci atış sayısını bilmez; yalnızca silah tuttuğunu bilir.</summary>
    public static string ArmedOnNetwork => Loc.T("weapon.armed_net");

    public static string Describe(StatusEffects status)
    {
        if (status == null || !status.HasWeapon) return string.Empty;

        string name = status.Weapon != null ? status.Weapon.LocalizedDisplay : null;
        if (string.IsNullOrEmpty(name)) name = Loc.T("weapon.default_name");
        int seconds = Mathf.CeilToInt(status.WeaponTimeLeft);
        if (MeleeRules.IsMelee(status.Weapon))
            return Loc.F("weapon.melee", name, status.WeaponShots, seconds);
        return Loc.F("weapon.ranged", name, status.WeaponShots, seconds);
    }
}
