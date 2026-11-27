using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vurulan karakterin ya da yaratığın görsel tepkisi (yalnızca görüntü): 1-2 kare tamamen beyaz parlar, sonra kısa bir kırmızı
/// (kalkan engellediyse camgöbeği) tona döner; gövde vuruş yönünde ezilip yayla geri seker (aşıp geri gelir); donma süresince
/// vuruş yönünde titrer ve görüntüsü durur (<see cref="HitStop"/>). Katmanlı (sprite stack) gövdede bütün dilimlere uygulanır.
/// Hesaplar <see cref="HitFeelMath"/> ve <see cref="SpringValue"/>'da, sayılar <see cref="GameConfig"/>'te.
/// Gövde bileşenleri rengi ve konumu yazdıktan sonra çalışır (onların üstüne ekler, birikmez).
/// </summary>
[DefaultExecutionOrder(55)]
public class HitReaction : MonoBehaviour
{
    const string FlashShaderPath = "Shaders/SpriteFlash";

    static Material flashMaterial;
    static bool materialTried;

    readonly List<SpriteRenderer> swapped = new List<SpriteRenderer>();
    readonly List<Material> originals = new List<Material>();

    GameConfig config;
    SpringValue squash;
    float flashAge = -1f;
    float strength = 1f;
    bool blocked;
    Vector2? direction;
    bool squashing;
    Transform shaped;          // Ölçeği ve titremesi değiştirilen parça (katmanlı gövde)
    Vector3 lastWrittenPosition;
    Vector3 jitterOffset;
    int jitterSign = 1;

    /// <summary>Nesneye tepki bileşeni ekler (zaten varsa onu verir).</summary>
    public static HitReaction Attach(GameObject target)
    {
        var reaction = target.GetComponent<HitReaction>();
        if (reaction == null) reaction = target.AddComponent<HitReaction>();
        return reaction;
    }

    /// <summary>
    /// Tepkiyi oynatır. <paramref name="hitDirection"/> vuruşun yönü (bilinmiyorsa null: dikey ezilme), <paramref name="power"/> şiddet
    /// (1 normal, dev vuruşta büyük), <paramref name="wasBlocked"/> kalkan engelledi mi, <paramref name="freezeSeconds"/> görüntü donması.
    /// </summary>
    public void Play(Vector2? hitDirection, float power, bool wasBlocked, float freezeSeconds)
    {
        if (config == null) config = GameConfigLoader.Get();
        if (squash == null) squash = new SpringValue(config.hitSquashFrequency, config.hitSquashDamping);

        direction = hitDirection.HasValue && hitDirection.Value.sqrMagnitude > 0.0001f ? hitDirection.Value.normalized : (Vector2?)null;
        strength = Mathf.Max(0f, power);
        blocked = wasBlocked;
        flashAge = 0f;

        squash.FrequencyHz = config.hitSquashFrequency;
        squash.DampingRatio = config.hitSquashDamping;
        squash.Set(Mathf.Min(1.5f, strength)); // Tam ezik başlar, yay 0'a çeker (aşıp uzar, geri gelir)
        squashing = true;

        if (freezeSeconds > 0f) HitStop.Freeze(gameObject, freezeSeconds);
    }

    /// <summary>Şu an bir tepki oynuyor mu (deneme için)?</summary>
    public bool IsPlaying => flashAge >= 0f || squashing;

    void LateUpdate()
    {
        if (config == null || (!squashing && flashAge < 0f)) return;

        float dt = Time.unscaledDeltaTime;
        SpriteStack stack = FindStack();
        UpdateFlash(stack, dt);
        UpdateShape(stack, dt);
    }

    /// <summary>Gövdenin katmanlı çizimi (karakter ya da yaratık); yoksa null.</summary>
    SpriteStack FindStack()
    {
        if (TryGetComponent(out CharacterStackVisual character) && character.Stack != null) return character.Stack;
        if (TryGetComponent(out ExtrudedSpriteVisual extruded) && extruded.Stack != null) return extruded.Stack;
        return null;
    }

    void UpdateFlash(SpriteStack stack, float dt)
    {
        if (flashAge < 0f) return;

        HitFlashPhase phase = HitFeelMath.Flash(flashAge, config.hitFlashWhiteSeconds, config.hitFlashTintSeconds, out float tint);
        flashAge += dt;

        if (phase == HitFlashPhase.White)
        {
            ApplyWhite(stack);
            return;
        }

        RestoreMaterials();
        if (phase == HitFlashPhase.None)
        {
            flashAge = -1f;
            return;
        }

        // Renk tonu: gövde bileşeni bu karenin rengini yazdı; üstüne kırmızı (kalkanda camgöbeği) karıştır.
        if (stack == null) return;
        Color baseColor = stack.Tint;
        Color target = blocked ? config.hitFlashBlockColor : config.hitFlashTintColor;
        Color mixed = Color.Lerp(baseColor, target, tint * config.hitFlashTintStrength);
        mixed.a = baseColor.a;
        stack.SetTint(mixed);
    }

    /// <summary>Çizicileri tek renkli parlama malzemesine geçirir (yoksa bir şey yapmaz).</summary>
    void ApplyWhite(SpriteStack stack)
    {
        Material flash = FlashMaterial();
        if (flash == null) return;

        if (stack != null)
        {
            foreach (SpriteRenderer layer in stack.Layers) Swap(layer, flash);
        }
        else if (TryGetComponent(out SpriteRenderer own) && !own.forceRenderingOff) Swap(own, flash);
    }

    void Swap(SpriteRenderer renderer, Material flash)
    {
        if (renderer == null || renderer.sharedMaterial == flash) return;
        swapped.Add(renderer);
        originals.Add(renderer.sharedMaterial);
        renderer.sharedMaterial = flash;
    }

    void RestoreMaterials()
    {
        for (int i = 0; i < swapped.Count; i++)
            if (swapped[i] != null) swapped[i].sharedMaterial = originals[i];
        swapped.Clear();
        originals.Clear();
    }

    /// <summary>Ezilme yayı ve donma titremesi: katmanlı gövdenin ölçeği ve yerel konumu.</summary>
    void UpdateShape(SpriteStack stack, float dt)
    {
        Transform part = stack != null ? stack.transform : null;
        if (part != shaped)
        {
            ResetShape();
            shaped = part;
            lastWrittenPosition = part != null ? part.localPosition : Vector3.zero;
            jitterOffset = Vector3.zero;
        }
        if (shaped == null) { squashing = false; return; }

        // Ezilme: donma boyunca ezik poz tutulur, sonra yay tam ezikten 0'a çeker; aşınca ters yöne uzar.
        if (squashing && !HitStop.IsFrozen(gameObject))
        {
            squash.Step(dt);
            if (squash.IsSettled(0.002f))
            {
                squash.Set(0f);
                squashing = false;
            }
        }
        Vector2 scale = HitFeelMath.SquashScale(direction, squash.Value, config.hitSquash, config.hitBulge);
        shaped.localScale = new Vector3(scale.x, scale.y, 1f);

        // Donma titremesi: gövde bileşeni konumu bu kare yeniden yazdıysa onun üstüne, yazmadıysa önceki titremeyi geri alarak.
        Vector3 basePosition = shaped.localPosition == lastWrittenPosition ? shaped.localPosition - jitterOffset : shaped.localPosition;
        jitterOffset = Vector3.zero;
        if (HitStop.IsFrozen(gameObject) && config.hitStopJitter > 0f)
        {
            jitterSign = -jitterSign;
            Vector2 axis = direction ?? Vector2.right;
            float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
            jitterOffset = (Vector3)(axis * (config.hitStopJitter * Mathf.Min(1.5f, strength) * jitterSign / parentScale));
        }
        shaped.localPosition = basePosition + jitterOffset;
        lastWrittenPosition = shaped.localPosition;
    }

    void ResetShape()
    {
        if (shaped == null) return;
        shaped.localScale = Vector3.one;
        if (shaped.localPosition == lastWrittenPosition) shaped.localPosition -= jitterOffset;
        jitterOffset = Vector3.zero;
    }

    void OnDisable()
    {
        RestoreMaterials();
        ResetShape();
        flashAge = -1f;
        squashing = false;
    }

    /// <summary>Ortak parlama malzemesi; shader yoksa ya da desteklenmiyorsa null (yalnızca kırmızı ton ve ezilme oynar).</summary>
    static Material FlashMaterial()
    {
        if (materialTried) return flashMaterial;
        materialTried = true;
        var shader = Resources.Load<Shader>(FlashShaderPath);
        if (shader != null && shader.isSupported)
            flashMaterial = new Material(shader) { name = "Vuruş parlaması", hideFlags = HideFlags.DontSave };
        return flashMaterial;
    }
}
