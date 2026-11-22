using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir yaratığı sahnede gösterir ve her karede <see cref="CreatureModel"/>'i ilerletir:
/// renkli bir daire ve üstünde kelimenin adı. Oyun mantığı modeldedir; burası yalnızca görüntü ve bağlantıdır.
/// Hedefsiz ("???") yaratık, bot beyniyle rastgele gezinir.
/// </summary>
public class CreatureView : MonoBehaviour
{
    static readonly IReadOnlyList<DangerZone> NoDangers = new List<DangerZone>();

    GameConfig config;
    MatchBootstrap bootstrap;
    BotBrain wanderBrain;

    /// <summary>Bu yaratığın oyun mantığı.</summary>
    public CreatureModel Model { get; private set; }

    /// <summary>Yaratığı kurar. <paramref name="wanderRandom"/> yalnızca hedefsiz yaratık için gerekir.</summary>
    public void Initialize(CreatureModel model, GameConfig config, MatchBootstrap bootstrap,
                           ArenaBounds arena, System.Random wanderRandom, Color color, string label)
    {
        Model = model;
        this.config = config;
        this.bootstrap = bootstrap;

        if (model.Target == null)
            wanderBrain = new BotBrain(config, arena, wanderRandom);

        BuildBody(color, model.Radius);
        BuildLabel(label, model.Radius);
        transform.position = model.Position;
    }

    void Update()
    {
        if (Model == null) return;

        Advance(Time.deltaTime);
        transform.position = Model.Position;

        if (Model.IsFinished) Destroy(gameObject);
    }

    void Advance(float dt)
    {
        if (Model.Target == null)
        {
            Vector2 direction = wanderBrain.Think(dt, Model.Position, NoDangers);
            Model.Wander(direction, dt);
            return;
        }

        if (bootstrap.TryGetPosition(Model.Target.Id, out Vector2 targetPosition))
            Model.Tick(dt, targetPosition, config.playerRadius, config.creatureHitCooldownSeconds);
        else
            Model.Tick(dt, Model.Position, config.playerRadius, config.creatureHitCooldownSeconds);
    }

    void BuildBody(Color color, float radius)
    {
        var body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * (radius * 2f);

        var renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteFactory.Circle();
        renderer.color = color;
        renderer.sortingOrder = 8;
    }

    void BuildLabel(string text, float radius)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, radius + 0.45f, 0f);

        var mesh = labelObject.AddComponent<TextMesh>();
        mesh.font = font;
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = 0.1f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = Color.white;

        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = 20;
    }
}
