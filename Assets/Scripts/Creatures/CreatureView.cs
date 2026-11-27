using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir yaratığı sahnede gösterir ve her karede <see cref="CreatureModel"/>'i ilerletir:
/// renkli bir daire ve üstünde kelimenin adı. Oyun mantığı modeldedir; burası yalnızca görüntü ve bağlantıdır.
/// Hedefsiz ("???") yaratık, bot beyniyle rastgele gezinir.
/// </summary>
public class CreatureView : MonoBehaviour
{
    readonly List<CreatureHit> hits = new List<CreatureHit>();

    GameConfig config;
    MatchBootstrap bootstrap;
    BotBrain wanderBrain;
    SpriteRenderer bodyRenderer;
    SpriteRenderer fillRenderer;
    TextMesh labelMesh;

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
        UpdateStrikeVisual();
        if (Model.IsBoss && labelMesh != null) labelMesh.text = BossRules.Label(Model); // "Godzilla 7/10"

        if (Model.IsFinished)
        {
            // Öldürülen boss parçalara ayrılır (süresi bitip giden değil).
            if (Model.IsBoss && Model.BossHealth <= 0 && bodyRenderer != null)
                Gore.Explode(bodyRenderer.sprite, Model.Position, bodyRenderer.bounds.size.x, bodyRenderer.color);
            Destroy(gameObject);
        }
    }

    /// <summary>Saldırdığı oyuncunun konumu (saldırı animasyonunun yönü için).</summary>
    Vector2? TargetPosition()
    {
        if (Model?.Target == null || bootstrap == null) return null;
        return bootstrap.TryGetPosition(Model.Target.Id, out Vector2 at) ? at : (Vector2?)null;
    }

    void Advance(float dt)
    {
        CreatureDriver.Advance(Model, dt, bootstrap, config, hits, wanderBrain);
    }

    void BuildBody(Color color, float radius)
    {
        var body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * (radius * 2f);

        bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = SpriteFactory.Circle();
        bodyRenderer.sortingOrder = 8;

        // Piksel sanat: koşan yaratık sprite'ın kendisidir; alan/tuzak/uyarı yaratığı büyük daire + ortada simge.
        bool isZone = Model.IsStrike || Model.IsArea || Model.IsTrap;
        Sprite[] frames = SpriteLibrary.GetFrames(SpriteLibrary.NameFor(Model.Word));
        if (frames.Length > 0)
        {
            if (!isZone)
            {
                body.transform.localScale = Vector3.one * CreatureSpriteScale.ForRadius(radius); // Boss dev görünür
                bodyRenderer.sortingOrder = CreatureSpriteScale.SortingOrder(radius);
                body.AddComponent<SpriteAnimator>().SetFrames(frames);
                bodyRenderer.color = Color.white;
                ExtrudedSpriteVisual.Attach(body, CreatureSpriteScale.IsGiant(radius) ? 3 : 4); // 3D piksel kalınlık (boss zaten 3 kat büyük)
                bool giant = CreatureSpriteScale.IsGiant(radius);
                CreatureAttackAnimator.Attach(body, () => Model.AttackCount, giant, TargetPosition,
                                              AttackMarkEffect.EffectFor(Model.Word, giant), // Saldırı animasyonu + etiketten vuruş izi
                                              () => Model.IsPounceWindup,                     // Atılma hazırlığında kırmızı yanar
                                              () => Model.IsLunging);                         // Atılırken uzar
                CreatureHurtFeedback.Attach(body, () => Model.IsBoss ? Model.BossHealth : -1); // Boss'a vurulunca parlar, "-1"
                return;
            }

            var icon = new GameObject("Icon");
            icon.transform.SetParent(transform, false);
            var iconRenderer = icon.AddComponent<SpriteRenderer>();
            iconRenderer.sortingOrder = 11;
            icon.AddComponent<SpriteAnimator>().SetFrames(frames);
            ExtrudedSpriteVisual.Attach(icon, 3, flipWithMovement: false); // Simge de kalın
        }

        if (Model.IsPull)
        {
            color = new Color(0.7f, 0.4f, 0.95f, 0.95f); // Mor: mıknatıs
        }

        if (Model.IsTrap)
        {
            color = new Color(1f, 0.85f, 0.2f, 0.85f); // Sarı: muz kabuğu
        }

        if (Model.IsArea && Model.BurnsArea)
        {
            color = new Color(1f, 0.45f, 0.1f, 0.38f); // Turuncu: ateş alanı
        }
        else if (Model.IsArea)
        {
            // Sürekli alan: büyük, yarı saydam, soğuk renkli bir daire.
            color = new Color(0.55f, 0.7f, 0.95f, 0.32f);
        }

        if (Model.IsStrike)
        {
            // Uyarı alanı: soluk bir daire; içindeki dolgu uyarı ilerledikçe büyür.
            color.a = 0.28f;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(transform, false);
            fill.transform.localScale = Vector3.zero;
            fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = SpriteFactory.Circle();
            fillRenderer.color = new Color(color.r, color.g * 0.6f, color.b * 0.6f, 0.6f);
            fillRenderer.sortingOrder = 9;
        }

        bodyRenderer.color = color;
    }

    /// <summary>Anında vuran yaratığın uyarı dolgusunu büyütür; vuruştan sonra darbe izini gösterir.</summary>
    void UpdateStrikeVisual()
    {
        if (!Model.IsStrike || fillRenderer == null) return;

        if (Model.HasStruck)
        {
            fillRenderer.enabled = false;
            bodyRenderer.color = new Color(1f, 1f, 1f, 0.55f); // Darbe izi
            return;
        }

        fillRenderer.transform.localScale = Vector3.one * (Model.Radius * 2f * Model.StrikeProgress);

        // Patlayıcıda fitilin kalan saniyesi kelimenin yanında yazılır (3, 2, 1).
        if (Model.ShowsCountdown && !Model.IsSky && labelMesh != null && Model.Word != null)
            labelMesh.text = Model.Word.LocalizedDisplay + " " + Mathf.CeilToInt(Model.StrikeWarningLeft);
    }

    void BuildLabel(string text, float radius)
    {
        if (string.IsNullOrEmpty(text)) return; // Sürünün diğer üyelerinde yazı olmaz.

        var font = GameFont.Get();

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, radius + 0.45f, 0f);

        var mesh = labelObject.AddComponent<TextMesh>();
        labelMesh = mesh;
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
