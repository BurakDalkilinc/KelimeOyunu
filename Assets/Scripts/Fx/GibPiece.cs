using UnityEngine;

/// <summary>
/// Parçalanan karakterin bir parçası: dönerek havaya savrulur, yerçekimiyle düşer, yere çarpınca seker ve kayarak durur.
/// Uçarken arkasında kan damlatır, durduğunda altında kan gölü bırakıp yerde kalır.
/// </summary>
public class GibPiece : MonoBehaviour
{
    const float Gravity = 30f;
    const float Bounce = 0.35f;
    const float GroundFriction = 6f;
    const float TrailInterval = 0.04f;

    Vector2 ground;
    Vector2 velocity;
    float height;
    float verticalSpeed;
    float spin;
    float trailTimer;
    int bounces;

    public static void Spawn(Sprite sprite, Vector2 at, Vector2 velocity, float upSpeed, float scale, Color tint)
    {
        var go = new GameObject("Parça");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(tint.r * 0.85f, tint.g * 0.7f, tint.b * 0.7f, 1f); // Kanlı, biraz koyu
        sr.sortingOrder = Gore.AirOrder;
        go.transform.localScale = Vector3.one * scale;

        var gib = go.AddComponent<GibPiece>();
        gib.ground = at;
        gib.velocity = velocity;
        gib.height = 0.4f;
        gib.verticalSpeed = upSpeed;
        gib.spin = Random.Range(-900f, 900f);
        gib.Place();
        Gore.RegisterGib(go);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        ground += velocity * dt;
        verticalSpeed -= Gravity * dt;
        height += verticalSpeed * dt;
        transform.Rotate(0f, 0f, spin * dt);

        if (height <= 0f)
        {
            height = 0f;
            if (bounces < 2 && verticalSpeed < -3f)
            {
                // Seker: hızın bir kısmı geri döner, yere kan bulaşır.
                bounces++;
                verticalSpeed = -verticalSpeed * Bounce;
                velocity *= 0.6f;
                spin *= 0.5f;
                Gore.Splash(ground, 4, 0.4f);
            }
            else
            {
                verticalSpeed = 0f;
                velocity *= Mathf.Exp(-GroundFriction * dt);
                spin *= Mathf.Exp(-GroundFriction * dt);
                if (velocity.sqrMagnitude < 0.01f) { Settle(); return; }
            }
        }

        trailTimer -= dt;
        if (trailTimer <= 0f)
        {
            trailTimer = TrailInterval;
            BloodDrop.Spawn(ground, velocity * 0.2f + Random.insideUnitCircle * 0.5f, height, 0f);
        }
        Place();
    }

    void Place()
    {
        transform.position = new Vector3(ground.x, ground.y + height * 0.5f, 0f);
    }

    void Settle()
    {
        Place();
        Gore.Splash(ground, 8, 0.3f); // Altında kan gölü
        GetComponent<SpriteRenderer>().sortingOrder = Gore.StainOrder + 1;
        enabled = false;
    }
}
