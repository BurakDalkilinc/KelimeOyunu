using UnityEngine;

/// <summary>İki (ya da daha fazla) kareyi sırayla gösterir: piksel sanat "nefes alma" hareketi.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField, Min(0.05f)] float secondsPerFrame = 0.35f;

    SpriteRenderer target;
    Sprite[] frames = System.Array.Empty<Sprite>();
    float timer;
    int index;

    /// <summary>Kareleri verir; her animatör kendi fazından başlar, hepsi aynı anda sallanmasın.</summary>
    public void SetFrames(Sprite[] newFrames)
    {
        target = GetComponent<SpriteRenderer>();
        frames = newFrames ?? System.Array.Empty<Sprite>();
        index = 0;
        timer = Random.value * secondsPerFrame;
        if (frames.Length > 0) target.sprite = frames[0];
    }

    void Update()
    {
        if (frames.Length < 2 || target == null) return;

        timer += Time.deltaTime;
        if (timer < secondsPerFrame) return;

        timer -= secondsPerFrame;
        index = (index + 1) % frames.Length;
        target.sprite = frames[index];
    }
}
