using UnityEngine;

/// <summary>
/// Kamera sarsıntısı (yalnızca görüntü): darbeler "travma" ekler, sarsıntı travmanın karesiyle Perlin gürültüsüyle titrer ve
/// söner (<see cref="Trauma"/>); ayrıca yönlü itme (kick) kamerayı vuruş yönünde iter, sönümlü yayla geri döner
/// (<see cref="SpringValue"/>). Takip eden kamera her karede konumunu kendisi yazdığı için sarsıntı ondan sonra eklenir (birikmez).
/// Ayarlardaki "Ekran sarsıntısı" kapalıysa hiç sarsmaz, şiddet yüzdesiyle çarpılır. Time.timeScale'den bağımsızdır.
/// Pixel Perfect Camera görüş boyutunu tam sayı katlarla kilitlediği için yakınlaştırma (zoom) yapılmaz; büyük darbede itme güçlenir.
/// </summary>
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    readonly Trauma trauma = new Trauma();
    SpringValue kickX;
    SpringValue kickY;
    GameConfig config;
    float seed;
    Vector3 lastWritten;
    Vector3 lastOffset;

    /// <summary>Şu anki travma (0..1; deneme ve test için).</summary>
    public float TraumaValue => trauma.Value;

    /// <summary>
    /// Ana kamerayı en çok <paramref name="amplitude"/> birim kadar, yaklaşık <paramref name="seconds"/> boyunca sarsar. Kaynağı kameradan
    /// uzaksa (<paramref name="source"/> verildiyse) sarsıntı uzaklıkla azalır.
    /// </summary>
    public static void Shake(float amplitude, float seconds, Vector2? source = null)
    {
        if (amplitude <= 0f || seconds <= 0f) return;
        CameraShake shake = Find(source, ref amplitude);
        if (shake == null) return;
        shake.trauma.Add(Trauma.AmountFor(amplitude, shake.config.cameraShakeMaxOffset, shake.config.cameraShakeExponent), seconds);
    }

    /// <summary>Travma ekler (0..1, büyük darbeler: eleme, boss, patlama). Kaynak uzaksa azalır.</summary>
    public static void AddTrauma(float amount, Vector2? source = null)
    {
        if (amount <= 0f) return;
        CameraShake shake = Find(source, ref amount);
        if (shake == null) return;
        shake.trauma.Add(amount, shake.config.cameraTraumaSeconds);
    }

    /// <summary>
    /// Kamerayı <paramref name="direction"/> yönünde en çok <paramref name="distance"/> birim iter; sönümlü yayla geri döner (aşıp seker).
    /// Kaynak uzaksa azalır.
    /// </summary>
    public static void Kick(Vector2 direction, float distance, Vector2? source = null)
    {
        if (distance <= 0f || direction.sqrMagnitude < 0.0001f) return;
        CameraShake shake = Find(source, ref distance);
        if (shake == null) return;

        // Durgun yaydan v0 hızıyla çıkış: tepe kayma ≈ v0 / ω. Bu yüzden hız = mesafe × ω.
        float omega = 2f * Mathf.PI * shake.config.cameraKickFrequency;
        Vector2 velocity = direction.normalized * Mathf.Min(distance, shake.config.cameraKickMax) * omega;
        shake.kickX.Kick(velocity.x);
        shake.kickY.Kick(velocity.y);
    }

    /// <summary>Ana kameradaki bileşeni bulur (yoksa ekler) ve kaynak uzaklığına göre <paramref name="amount"/>'ı azaltır.</summary>
    static CameraShake Find(Vector2? source, ref float amount)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        var shake = cam.GetComponent<CameraShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
        shake.Setup();

        if (source.HasValue)
        {
            float distance = Vector2.Distance(source.Value, cam.transform.position);
            amount *= HitFeelMath.DistanceFalloff(distance, shake.config.cameraShakeFullDistance, shake.config.cameraShakeZeroDistance);
            if (amount <= 0.001f) return null;
        }
        return shake;
    }

    void Setup()
    {
        if (config != null) return;
        config = GameConfigLoader.Get();
        trauma.Exponent = config.cameraShakeExponent;
        kickX = new SpringValue(config.cameraKickFrequency, config.cameraKickDamping);
        kickY = new SpringValue(config.cameraKickFrequency, config.cameraKickDamping);
        seed = Random.value * 100f;
    }

    void LateUpdate()
    {
        if (config == null) return;

        float dt = Time.unscaledDeltaTime;
        trauma.Step(dt);
        kickX.Step(dt);
        kickY.Step(dt);

        // Kamerayı bu karede kimse yeniden yazmadıysa (takip eden kamera yoksa) geçen karenin kaymasını geri al: birikmesin.
        Vector3 basePosition = transform.position == lastWritten ? transform.position - lastOffset : transform.position;

        Vector2 offset = trauma.Offset(Time.unscaledTime, seed, config.cameraShakeFrequency, config.cameraShakeMaxOffset);
        offset += Vector2.ClampMagnitude(new Vector2(kickX.Value, kickY.Value), config.cameraKickMax);
        offset *= SettingsService.ScreenShakeScale;

        lastOffset = new Vector3(offset.x, offset.y, 0f);
        lastWritten = basePosition + lastOffset;
        transform.position = lastWritten;
    }
}
