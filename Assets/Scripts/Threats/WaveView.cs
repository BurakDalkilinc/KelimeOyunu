using UnityEngine;

/// <summary>
/// Dalgayı ekranda çizer: ilerleyen mavi yarı saydam bir bant (ön yüzü ve arkasındaki kalınlık).
/// Dalganın mantığı <see cref="WaveEffect"/>'tedir; burası yalnızca görüntü. Dalga bitince kendini siler.
/// </summary>
public class WaveView : MonoBehaviour
{
    WaveEffect wave;
    ArenaBounds arena;
    Transform band;

    public void Initialize(WaveEffect wave, ArenaBounds arena)
    {
        this.wave = wave;
        this.arena = arena;

        var go = new GameObject("Dalga");
        go.transform.SetParent(transform, false);
        band = go.transform;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square();
        sr.color = new Color(0.25f, 0.55f, 1f, 0.5f);
        sr.sortingOrder = 12;

        Place();
    }

    void Update()
    {
        if (wave == null || wave.IsFinished)
        {
            Destroy(gameObject);
            return;
        }

        Place();
    }

    /// <summary>Bandı ön yüzün arkasına koyar: ilerleme yönünde Thickness kalınlığında, diğer yönde arenayı boydan boya kaplar.</summary>
    void Place()
    {
        Vector2 direction = wave.Direction;
        Vector2 crossHalf = arena.HalfSize;
        bool horizontal = Mathf.Abs(direction.x) > 0.5f; // Sağa/sola ilerleyen dalga dikey bir bant olur.

        float length = (horizontal ? crossHalf.y : crossHalf.x) * 2f + 2f;
        Vector2 center = direction * (wave.Front - WaveEffect.Thickness * 0.5f);

        band.position = center;
        band.localScale = horizontal
            ? new Vector3(WaveEffect.Thickness, length, 1f)
            : new Vector3(length, WaveEffect.Thickness, 1f);
    }
}
