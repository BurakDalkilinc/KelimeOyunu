using UnityEngine;

/// <summary>
/// Korku ipuçları için bir yaratığın ekrana yansıyan bilgisi: yeri, boyu, zararlı mı, atılmaya hazırlanıyor mu, kimi kovalıyor.
/// Tek kişilikte modelden, ağda anlık görüntüden doldurulur. Düz veri.
/// </summary>
public struct SensedCreature
{
    public Vector2 Position;
    public float Radius;
    public bool Harmful;
    public bool PounceWindup;

    /// <summary>Kovaladığı oyuncunun kimliği; yoksa -1.</summary>
    public int TargetId;
}
