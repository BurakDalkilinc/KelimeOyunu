using UnityEngine;

/// <summary>
/// Tek bir duvar parçası: merkezi, yönü, uzunluğu, kalınlığı ve kalan ömrü olan dikdörtgen engel.
/// Düz C# sınıfı. Yönü (<see cref="Direction"/>) duvarın uzandığı eksendir; duvar istenen açıda durabilir.
/// </summary>
public class WallSegment
{
    public Vector2 Center { get; }

    /// <summary>Duvarın uzunluğu boyunca birim yön.</summary>
    public Vector2 Direction { get; }

    public float Length { get; }
    public float Thickness { get; }
    public float TotalLife { get; }
    public float LifeLeft { get; private set; }

    public bool IsExpired => LifeLeft <= 0f;

    /// <summary>Duvar parçasını kurar. Yön sıfırsa sağa bakar; uzunluk ve kalınlık en az 0.1'dir.</summary>
    public WallSegment(Vector2 center, Vector2 direction, float length, float thickness, float life)
    {
        Center = center;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Length = Mathf.Max(0.1f, length);
        Thickness = Mathf.Max(0.1f, thickness);
        TotalLife = Mathf.Max(0f, life);
        LifeLeft = TotalLife;
    }

    /// <summary>Zamanı ilerletir; ömür bitince duvar kalkar.</summary>
    public void Tick(float deltaSeconds)
    {
        if (LifeLeft > 0f) LifeLeft = Mathf.Max(0f, LifeLeft - Mathf.Max(0f, deltaSeconds));
    }

    /// <summary>
    /// Verilen yarıçaplı daireyi duvarın dışına iter ve düzeltilmiş merkezi verir. Duvara değmiyorsa aynen döner.
    /// Duvarın yan yüzünden, uç kenarından ya da (merkez duvarın içine girmişse) en yakın yüzeyden dışarı atar.
    /// Ömrü biten duvar etki etmez.
    /// </summary>
    public Vector2 PushOut(Vector2 position, float radius)
    {
        if (IsExpired) return position;

        Vector2 normal = new Vector2(-Direction.y, Direction.x);
        Vector2 relative = position - Center;
        float along = Vector2.Dot(relative, Direction);
        float across = Vector2.Dot(relative, normal);

        float halfLength = Length * 0.5f;
        float halfThickness = Thickness * 0.5f;

        // Dikdörtgenin dairenin merkezine en yakın noktası.
        float closestAlong = Mathf.Clamp(along, -halfLength, halfLength);
        float closestAcross = Mathf.Clamp(across, -halfThickness, halfThickness);

        float deltaAlong = along - closestAlong;
        float deltaAcross = across - closestAcross;
        float distance = Mathf.Sqrt(deltaAlong * deltaAlong + deltaAcross * deltaAcross);

        if (distance >= radius) return position; // Değmiyor

        float newAlong, newAcross;
        if (distance > 0.00001f)
        {
            // Merkez dışarıda ama yarıçap içeri giriyor: en yakın noktadan radial olarak dışarı.
            float scale = radius / distance;
            newAlong = closestAlong + deltaAlong * scale;
            newAcross = closestAcross + deltaAcross * scale;
        }
        else
        {
            // Merkez duvarın içinde: en az içeri girilmiş yüzeyden dışarı çık.
            float penetrationAlong = halfLength - Mathf.Abs(along);
            float penetrationAcross = halfThickness - Mathf.Abs(across);

            if (penetrationAcross <= penetrationAlong)
            {
                newAlong = along;
                newAcross = (across >= 0f ? 1f : -1f) * (halfThickness + radius);
            }
            else
            {
                newAlong = (along >= 0f ? 1f : -1f) * (halfLength + radius);
                newAcross = across;
            }
        }

        return Center + Direction * newAlong + normal * newAcross;
    }
}
