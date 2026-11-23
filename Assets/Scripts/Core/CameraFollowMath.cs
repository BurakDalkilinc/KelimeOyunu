using UnityEngine;

/// <summary>
/// Takip kamerasının hesabı: hedefi izler, ama görüş alanı arenanın dışına taşmaz (arena görüşten küçükse o eksende ortalanır).
/// Düz hesap, test edilebilir.
/// </summary>
public static class CameraFollowMath
{
    /// <summary>
    /// Kameranın olması gereken merkez. <paramref name="viewHalf"/> görüşün yarı boyutu, <paramref name="arenaHalf"/> arenanın yarı boyutu,
    /// <paramref name="margin"/> arenanın kenarının dışında görünmesine izin verilen pay (kenarlık görünsün diye).
    /// </summary>
    public static Vector2 Clamp(Vector2 target, Vector2 arenaHalf, Vector2 viewHalf, float margin)
    {
        return new Vector2(ClampAxis(target.x, arenaHalf.x, viewHalf.x, margin), ClampAxis(target.y, arenaHalf.y, viewHalf.y, margin));
    }

    static float ClampAxis(float value, float arenaHalf, float viewHalf, float margin)
    {
        float room = arenaHalf + margin - viewHalf; // Kameranın merkezinin gidebileceği en uzak nokta
        if (room <= 0f) return 0f;                  // Görüş arenadan büyük: ortala
        return Mathf.Clamp(value, -room, room);
    }

    /// <summary>
    /// Mevcut konumdan hedefe yumuşak yaklaşır; kare hızından bağımsızdır (<paramref name="sharpness"/> büyükse daha sıkı takip).
    /// </summary>
    public static Vector2 Smooth(Vector2 current, Vector2 target, float deltaSeconds, float sharpness)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaSeconds));
        return Vector2.Lerp(current, target, t);
    }

    /// <summary>Piksel referans çözünürlüğünden (ve piksel/birim oranından) görüşün yarı boyutu.</summary>
    public static Vector2 ViewHalf(int refWidth, int refHeight, int pixelsPerUnit)
    {
        float ppu = Mathf.Max(1, pixelsPerUnit);
        return new Vector2(refWidth, refHeight) / (2f * ppu);
    }
}
