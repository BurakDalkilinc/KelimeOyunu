using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Kamerayı oyuncuyu izleyecek şekilde sürer ve Pixel Perfect Camera'nın görüş boyutunu değiştirir:
/// normalde yakın (oyuncu büyük görünür, arenanın yalnızca bir kısmı), hedef seçerken ya da sığınak çıkınca
/// ("genel bakış") tüm arena görünür. Hesap <see cref="CameraFollowMath"/>'tedir; burası yalnızca kamera bağlantısıdır.
/// Hedefi ve "genel bakış istendi mi" bilgisini tek kişilik ve ağ sahnesindeki küçük bağlayıcılar verir.
/// </summary>
[DefaultExecutionOrder(100)] // Karakterler hareket ettikten sonra
public class CameraFollow : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] PixelPerfectCamera pixelPerfect;

    [Header("Yakın takip görüşü (piksel)")]
    [SerializeField, Min(64)] int followWidth = 384;
    [SerializeField, Min(36)] int followHeight = 216;

    [Header("Genel bakış görüşü (tüm arena)")]
    [SerializeField, Min(64)] int overviewWidth = 640;
    [SerializeField, Min(36)] int overviewHeight = 360;

    [Tooltip("Takip sertliği: büyük değer kamerayı karaktere daha sıkı bağlar.")]
    [SerializeField, Min(0.1f)] float followSharpness = 8f;

    [Tooltip("Arenanın kenarının dışında görünmesine izin verilen pay (birim): kenarlık görünsün.")]
    [SerializeField, Min(0f)] float edgeMargin = 1.2f;

    Vector2 position;
    bool placed;

    /// <summary>Takibin donduğu son an (gerçek zaman, sn); bkz. <see cref="Hold"/>.</summary>
    static float holdUntil = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => holdUntil = -1f;

    /// <summary>
    /// Kamera takibini <paramref name="seconds"/> saniye dondurur (yerel oyuncu vurulunca kısa "durak" hissi; yalnızca görüntü).
    /// </summary>
    public static void Hold(float seconds)
    {
        if (seconds <= 0f) return;
        holdUntil = Mathf.Max(holdUntil, Time.unscaledTime + seconds);
    }

    /// <summary>Kameranın izleyeceği nokta; null ise arenanın ortası.</summary>
    public Func<Vector2?> TargetProvider { get; set; }

    /// <summary>true ise tüm arena gösterilir (hedef seçme, sığınak arama).</summary>
    public Func<bool> OverviewWanted { get; set; }

    /// <summary>Şu an genel bakış modunda mı?</summary>
    public bool InOverview { get; private set; }

    void LateUpdate()
    {
        if (config == null || pixelPerfect == null) return;

        InOverview = OverviewWanted != null && OverviewWanted();
        ApplyView(InOverview);

        Vector2 viewHalf = CameraFollowMath.ViewHalf(pixelPerfect.refResolutionX, pixelPerfect.refResolutionY, pixelPerfect.assetsPPU);
        Vector2 arenaHalf = config.arenaSize * 0.5f;

        Vector2? target = InOverview ? Vector2.zero : TargetProvider?.Invoke();
        Vector2 wanted = CameraFollowMath.Clamp(target ?? Vector2.zero, arenaHalf, viewHalf, edgeMargin);

        // İlk karede ya da görüş değişirken sıçrayarak değil, yumuşakça gider (genel bakışa geçiş de yumuşak olur).
        bool held = placed && Time.unscaledTime < holdUntil && !InOverview; // Vuruş anında takip bir an durur
        if (!held) position = placed ? CameraFollowMath.Smooth(position, wanted, Time.unscaledDeltaTime, followSharpness) : wanted;
        placed = true;

        transform.position = new Vector3(position.x, position.y, -10f);
    }

    void ApplyView(bool overview)
    {
        int width = overview ? overviewWidth : followWidth;
        int height = overview ? overviewHeight : followHeight;

        if (pixelPerfect.refResolutionX != width) pixelPerfect.refResolutionX = width;
        if (pixelPerfect.refResolutionY != height) pixelPerfect.refResolutionY = height;
    }
}
