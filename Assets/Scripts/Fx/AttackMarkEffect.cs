using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vuruş izi: yaratık vurduğu anda hedefin üstünde kısa bir kare animasyonu (pençe, ısırık, kesik...) oynar ve kendini yok eder.
/// Kareler Resources/Sprites altında "fx_ad_0", "fx_ad_1"... adındadır; hiç kare yoksa hiçbir şey çıkmaz. Yalnızca görüntüdür.
/// </summary>
public class AttackMarkEffect : MonoBehaviour
{
    /// <summary>Bir efektin en fazla kaç karesi aranır.</summary>
    const int MaxFrames = 8;

    static readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();
    static AttackFxRules rules;

    Sprite[] frames;
    float frameSeconds;
    float age;
    SpriteRenderer spriteRenderer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        frameCache.Clear();
        rules = null;
    }

    /// <summary>Yaratığın vuruş izinin adı (etiketlerden, bkz. <see cref="AttackFxRules"/>).</summary>
    public static string EffectFor(WordEntry word, bool giant)
    {
        if (rules == null) rules = AttackFxRules.LoadFromResources(GameConfigLoader.Get().attackFxPath);
        return rules.ForCreature(word, giant);
    }

    /// <summary>Efektin kareleri ("ad_0", "ad_1"...); hiç yoksa boş dizi.</summary>
    public static Sprite[] FramesFor(string effect)
    {
        if (string.IsNullOrEmpty(effect)) return System.Array.Empty<Sprite>();
        // Boş sonuç önbelleğe alınmaz: resimler oyun açıkken sonradan eklenirse de bulunsun.
        if (frameCache.TryGetValue(effect, out Sprite[] cached) && cached.Length > 0 && cached[0] != null) return cached;

        var found = new List<Sprite>();
        for (int i = 0; i < MaxFrames; i++)
        {
            Sprite frame = SpriteLibrary.Get(effect + "_" + i);
            if (frame == null) break;
            found.Add(frame);
        }

        Sprite[] result = found.ToArray();
        if (result.Length > 0) frameCache[effect] = result;
        return result;
    }

    /// <summary>
    /// <paramref name="at"/> noktasında <paramref name="effect"/> izini <paramref name="size"/> katı boyda oynatır.
    /// <paramref name="flip"/> sola vuruşta aynalar. Kareler yoksa null döner (eski görüntü yeterli).
    /// </summary>
    public static AttackMarkEffect Spawn(string effect, Vector2 at, float size, bool flip, float frameSeconds)
    {
        Sprite[] frames = FramesFor(effect);
        if (frames.Length == 0) return null;

        var go = new GameObject("Vuruş izi " + effect);
        go.transform.position = at;
        go.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);

        var mark = go.AddComponent<AttackMarkEffect>();
        mark.frames = frames;
        mark.frameSeconds = Mathf.Max(0.01f, frameSeconds);
        mark.spriteRenderer = go.AddComponent<SpriteRenderer>();
        mark.spriteRenderer.sprite = frames[0];
        mark.spriteRenderer.flipX = flip;
        mark.spriteRenderer.sortingOrder = Gore.AirOrder + 2; // Karakterlerin ve yaratıkların üstünde
        return mark;
    }

    void Update()
    {
        age += Time.deltaTime;
        int index = Mathf.FloorToInt(age / frameSeconds);
        if (index >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        if (spriteRenderer.sprite != frames[index]) spriteRenderer.sprite = frames[index];
    }
}
