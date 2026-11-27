/// <summary>
/// Yaratık saldırı animasyonunun aşaması: hazırlık (kurulma) → vuruş (atılma, darbe) → toparlanma. Yalnızca görüntüdür.
/// </summary>
public enum AttackStage
{
    /// <summary>Saldırı animasyonu yok.</summary>
    Idle,
    /// <summary>Hazırlık: geri çekilip ezilir, "ad_atk0" karesi.</summary>
    Windup,
    /// <summary>Vuruş: hedefe atılır ve uzar, "ad_atk" karesi; sonunda darbe izi.</summary>
    Strike,
    /// <summary>Toparlanma: yerine döner, normal kare.</summary>
    Recover,
}
