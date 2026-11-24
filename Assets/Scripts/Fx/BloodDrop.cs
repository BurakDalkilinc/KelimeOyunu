using UnityEngine;

/// <summary>
/// Tek kan damlası: yerden bir yükseklikte (sahte "z") fırlar, yerçekimiyle düşer ve yere değince yayvan bir lekeye
/// dönüşüp maç boyunca orada kalır. Kuşbakışı oyunda yükseklik, damlanın ekranda biraz yukarıda çizilmesiyle gösterilir.
/// </summary>
public class BloodDrop : MonoBehaviour
{
    const float Gravity = 28f;
    const float Drag = 2.5f;

    Vector2 ground;
    Vector2 velocity;
    float height;
    float verticalSpeed;
    SpriteRenderer sr;

    public static BloodDrop Spawn(Vector2 at, Vector2 velocity, float height, float upSpeed, float size = 0f)
    {
        var go = new GameObject("Kan");
        var drop = go.AddComponent<BloodDrop>();
        drop.ground = at;
        drop.velocity = velocity;
        drop.height = height;
        drop.verticalSpeed = upSpeed;
        drop.sr = go.AddComponent<SpriteRenderer>();
        drop.sr.sprite = Gore.Pixel;
        drop.sr.color = Gore.RandomBlood();
        drop.sr.sortingOrder = Gore.AirOrder;
        go.transform.localScale = Vector3.one * (size > 0f ? size : Random.Range(0.07f, 0.14f));
        drop.Place();
        return drop;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        ground += velocity * dt;
        velocity *= Mathf.Exp(-Drag * dt);
        verticalSpeed -= Gravity * dt;
        height += verticalSpeed * dt;

        if (height <= 0f)
        {
            Land();
            return;
        }
        Place();
    }

    void Place()
    {
        transform.position = new Vector3(ground.x, ground.y + height * 0.5f, 0f);
    }

    void Land()
    {
        height = 0f;
        Place();

        // Leke: yere yayılır, biraz koyulaşır ve kalıcı olur.
        float size = transform.localScale.x * Random.Range(1.4f, 2.6f);
        transform.localScale = new Vector3(size * Random.Range(1f, 1.8f), size, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
        sr.color = Color.Lerp(sr.color, new Color(0.35f, 0.02f, 0.04f), 0.3f);
        sr.sortingOrder = Gore.StainOrder;
        Gore.RegisterStain(gameObject);
        enabled = false;
    }
}
