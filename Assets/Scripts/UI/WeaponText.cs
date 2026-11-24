using UnityEngine;

/// <summary>Silah tutan oyuncunun ekrandaki "Boşluk" satırı ("Silah (Boşluk): 1 atış, 12 sn"). Düz hesap, test edilebilir.</summary>
public static class WeaponText
{
    /// <summary>Ağda istemci atış sayısını bilmez; yalnızca silah tuttuğunu bilir.</summary>
    public const string ArmedOnNetwork = "Silah elinde: Boşluk ile ateş et!";

    public static string Describe(StatusEffects status)
    {
        if (status == null || !status.HasWeapon) return string.Empty;

        string name = status.Weapon != null && !string.IsNullOrEmpty(status.Weapon.display) ? status.Weapon.display : "Silah";
        if (MeleeRules.IsMelee(status.Weapon))
            return $"{name} (Boşluk ile vur): {status.WeaponShots} vuruş, {Mathf.CeilToInt(status.WeaponTimeLeft)} sn";
        return $"{name} (Boşluk ile ateş): {status.WeaponShots} atış, {Mathf.CeilToInt(status.WeaponTimeLeft)} sn";
    }
}
