using UnityEngine;

/// <summary>
/// Haritada kaçılması gereken bir alan (yaratık, patlama alanı, tehdit...). Botlar bundan uzaklaşır.
/// Şimdilik boş bir liste olarak kullanılır; yaratıklar eklenince onlar bu listeyi doldurur.
/// </summary>
public readonly struct DangerZone
{
    public Vector2 Position { get; }

    /// <summary>Tehlikeli alanın yarıçapı (birim).</summary>
    public float Radius { get; }

    public DangerZone(Vector2 position, float radius)
    {
        Position = position;
        Radius = Mathf.Max(0f, radius);
    }
}
