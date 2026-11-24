using UnityEngine;

/// <summary>
/// Uçuş görüntüsü (kanat, jetpack...): karakterin altında yerde bir gölge ve ayaklarının altında titreyen alev belirir,
/// gölge hafifçe inip kalkar, böylece karakterin havada olduğu belli olur. Yalnızca görüntüdür; uçup uçmadığını
/// sahibi <see cref="Flying"/> ile söyler.
/// </summary>
public class FlightVisual : MonoBehaviour
{
    SpriteRenderer shadow;
    SpriteRenderer flame;
    SpriteRenderer body;
    float time;

    /// <summary>Karakter şu an uçuyor mu?</summary>
    public bool Flying { get; set; }

    public static FlightVisual Attach(GameObject target)
    {
        var visual = target.GetComponent<FlightVisual>();
        if (visual == null) visual = target.AddComponent<FlightVisual>();
        return visual;
    }

    void Build()
    {
        body = GetComponent<SpriteRenderer>();
        int order = body != null ? body.sortingOrder : 5;

        shadow = MakeChild("Uçuş gölgesi", new Color(0f, 0f, 0f, 0.35f), order - 1);
        flame = MakeChild("Uçuş alevi", new Color(1f, 0.55f, 0.1f, 0.9f), order - 1);
    }

    SpriteRenderer MakeChild(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteFactory.Circle();
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    void LateUpdate()
    {
        if (shadow == null) Build();

        bool show = Flying && (body == null || body.enabled);
        shadow.enabled = show;
        flame.enabled = show;
        if (!show) { time = 0f; return; }

        time += Time.deltaTime;
        float bob = Mathf.Sin(time * 6f) * 0.08f;

        // Gölge karakterden aşağıda: karakter "yükseldikçe" gölge küçülür.
        shadow.transform.localPosition = new Vector3(0f, -0.85f - bob, 0f);
        shadow.transform.localScale = new Vector3(1.0f - bob, 0.35f, 1f);

        // Alev titrer (boyu ve rengi değişir).
        float flicker = 0.75f + Mathf.PerlinNoise(time * 12f, 0.3f) * 0.5f;
        flame.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        flame.transform.localScale = new Vector3(0.45f, 0.7f * flicker, 1f);
        flame.color = Color.Lerp(new Color(1f, 0.85f, 0.2f, 0.9f), new Color(1f, 0.35f, 0.05f, 0.9f), flicker - 0.75f);

        // Hayalet/görünmez karakterin alevi de soluk olsun.
        if (body != null)
        {
            Color c = flame.color; c.a *= body.color.a; flame.color = c;
            Color s = shadow.color; s.a = 0.35f * body.color.a; shadow.color = s;
        }
    }
}
