using UnityEngine;

/// <summary>
/// İki (ya da daha fazla) kareyi sırayla gösterir: piksel sanat "nefes alma" hareketi.
/// <see cref="Hold"/> ile döngü kısa süre durup tek bir kare gösterir (yaratığın saldırı karesi).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField, Min(0.05f)] float secondsPerFrame = 0.35f;

    SpriteRenderer target;
    Sprite[] frames = System.Array.Empty<Sprite>();
    float timer;
    int index;
    Sprite held;
    float holdLeft;

    /// <summary>Döngünün ilk karesi (sprite adı buradan bulunur); kare yoksa null.</summary>
    public Sprite BaseFrame => frames.Length > 0 ? frames[0] : null;

    /// <summary>Kareleri verir; her animatör kendi fazından başlar, hepsi aynı anda sallanmasın.</summary>
    public void SetFrames(Sprite[] newFrames)
    {
        target = GetComponent<SpriteRenderer>();
        frames = newFrames ?? System.Array.Empty<Sprite>();
        index = 0;
        timer = Random.value * secondsPerFrame;
        if (frames.Length > 0) target.sprite = frames[0];
    }

    /// <summary>Döngüyü <paramref name="seconds"/> saniye durdurur ve <paramref name="sprite"/>'ı gösterir; süre bitince kaldığı kareye döner.</summary>
    public void Hold(Sprite sprite, float seconds)
    {
        if (sprite == null || seconds <= 0f) return;
        if (target == null) target = GetComponent<SpriteRenderer>();
        held = sprite;
        holdLeft = seconds;
        target.sprite = sprite;
    }

    void Update()
    {
        if (target == null) return;

        if (held != null)
        {
            holdLeft -= Time.deltaTime;
            if (holdLeft > 0f)
            {
                if (target.sprite != held) target.sprite = held;
                return;
            }
            held = null;
            if (frames.Length > 0) target.sprite = frames[index];
        }

        if (frames.Length < 2) return;

        timer += Time.deltaTime;
        if (timer < secondsPerFrame) return;

        timer -= secondsPerFrame;
        index = (index + 1) % frames.Length;
        target.sprite = frames[index];
    }
}
