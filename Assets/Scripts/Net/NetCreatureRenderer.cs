using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Host'tan gelen yaratık fotoğraflarını (<see cref="CreatureSnapshot"/>) çizer. Hiçbir kural işletmez:
/// yaratık nereye gidiyorsa oraya yumuşakça kayar, fotoğrafta olmayan yaratık silinir.
/// Görünüm tek kişilik sahnedeki <see cref="CreatureView"/> ile aynıdır (piksel sprite, alan yaratıkları için büyük daire + simge).
/// </summary>
public class NetCreatureRenderer : MonoBehaviour
{
    /// <summary>Yaratığın hedef konuma yaklaşma sertliği (büyük = daha sıkı takip).</summary>
    const float FollowSharpness = 20f;

    static readonly Color AttackColor = new Color(0.95f, 0.35f, 0.3f);

    [SerializeField] NetTurnChannel channel;

    sealed class View
    {
        public GameObject Root;
        public SpriteRenderer Body;
        public SpriteRenderer Fill;
        public TextMesh Label;
        public WordEntry Word;
        public CreatureSnapshot Last;
        public Vector2 Target;
    }

    readonly Dictionary<int, View> views = new Dictionary<int, View>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> toRemove = new List<int>();
    readonly List<Vector2> victimPositions = new List<Vector2>();
    int appliedVersion = -1;

    void Update()
    {
        if (channel == null) return;

        if (channel.CreaturesVersion != appliedVersion)
        {
            appliedVersion = channel.CreaturesVersion;
            Apply(channel.LatestCreatures);
        }

        float follow = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        foreach (View view in views.Values)
            view.Root.transform.position = Vector2.Lerp(view.Root.transform.position, view.Target, follow);
    }

    void OnDestroy()
    {
        foreach (View view in views.Values)
            if (view.Root != null) Destroy(view.Root);
        views.Clear();
    }

    void Apply(IReadOnlyList<CreatureSnapshot> snapshots)
    {
        seen.Clear();

        foreach (CreatureSnapshot snapshot in snapshots)
        {
            seen.Add(snapshot.Id);

            if (!views.TryGetValue(snapshot.Id, out View view))
            {
                view = Build(snapshot);
                views[snapshot.Id] = view;
                view.Root.transform.position = new Vector3(snapshot.X, snapshot.Y, 0f); // İlk karede kayarak gelmesin
            }

            view.Last = snapshot;
            view.Target = new Vector2(snapshot.X, snapshot.Y);
            Refresh(view);
        }

        toRemove.Clear();
        foreach (int id in views.Keys)
            if (!seen.Contains(id)) toRemove.Add(id);

        foreach (int id in toRemove)
        {
            Destroy(views[id].Root);
            views.Remove(id);
        }
    }

    View Build(CreatureSnapshot s)
    {
        var view = new View { Root = new GameObject("Yaratık " + s.Id) };
        view.Root.transform.SetParent(transform, false);

        var body = new GameObject("Body");
        body.transform.SetParent(view.Root.transform, false);
        view.Body = body.AddComponent<SpriteRenderer>();
        view.Body.sortingOrder = 8;

        WordEntry word = channel.FindWord(s.WordId);
        view.Word = word;
        Sprite[] frames = SpriteLibrary.GetFrames(SpriteLibrary.NameFor(word));
        bool isZone = s.Kind == CreatureKind.Strike || s.Kind == CreatureKind.Area
                      || s.Kind == CreatureKind.FireArea || s.Kind == CreatureKind.Trap;

        if (frames.Length > 0 && !isZone)
        {
            // Koşan yaratık sprite'ın kendisidir (boss dev görünür).
            body.transform.localScale = Vector3.one * CreatureSpriteScale.ForRadius(s.Radius);
            view.Body.sortingOrder = CreatureSpriteScale.SortingOrder(s.Radius);
            body.AddComponent<SpriteAnimator>().SetFrames(frames);
            view.Body.color = Color.white;
            ExtrudedSpriteVisual.Attach(body, CreatureSpriteScale.IsGiant(s.Radius) ? 3 : 4); // 3D piksel kalınlık (boss zaten 3 kat büyük)
            bool giant = CreatureSpriteScale.IsGiant(s.Radius);
            CreatureAttackAnimator.Attach(body, () => view.Last.Attacks, giant, () => NearestVictim(view),
                                          AttackMarkEffect.EffectFor(word, giant)); // Saldırı animasyonu + etiketten vuruş izi
        }
        else
        {
            // Alan/tuzak/uyarı: büyük yarı saydam daire; ortada simge.
            view.Body.sprite = SpriteFactory.Circle();
            body.transform.localScale = Vector3.one * (s.Radius * 2f);
            view.Body.color = ZoneColor(s.Kind);

            if (frames.Length > 0)
            {
                var icon = new GameObject("Icon");
                icon.transform.SetParent(view.Root.transform, false);
                var iconRenderer = icon.AddComponent<SpriteRenderer>();
                iconRenderer.sortingOrder = 11;
                icon.AddComponent<SpriteAnimator>().SetFrames(frames);
                ExtrudedSpriteVisual.Attach(icon, 3, flipWithMovement: false); // Simge de kalın
            }
        }

        if (s.Kind == CreatureKind.Strike)
        {
            var fill = new GameObject("Fill");
            fill.transform.SetParent(view.Root.transform, false);
            fill.transform.localScale = Vector3.zero;
            view.Fill = fill.AddComponent<SpriteRenderer>();
            view.Fill.sprite = SpriteFactory.Circle();
            view.Fill.color = new Color(AttackColor.r, AttackColor.g * 0.6f, AttackColor.b * 0.6f, 0.6f);
            view.Fill.sortingOrder = 9;
        }

        if (!string.IsNullOrEmpty(s.Label)) view.Label = BuildLabel(view.Root.transform, CreatureLabel.Resolve(s.Label, word), s.Radius);
        return view;
    }

    /// <summary>
    /// İstemci yaratığın kime vurduğunu bilmez: yaratığa en yakın, yeterince yakın, hayattaki karakter hedef sayılır
    /// (atılma yönü ve vuruş izinin yeri için; yalnızca görüntü). Başkasının görünmez karakteri aranmaz, yeri belli olmasın.
    /// </summary>
    Vector2? NearestVictim(View view)
    {
        if (view.Root == null) return null;
        GameConfig config = GameConfigLoader.Get();
        float reach = view.Last.Radius + config.playerRadius + config.attackMarkTargetSearchMargin;
        int local = channel != null ? channel.LocalPlayerId : -1;

        victimPositions.Clear();
        foreach (NetworkPlayer avatar in NetworkPlayer.All)
        {
            if (avatar == null || avatar.IsGhost) continue;
            if ((avatar.Status & StatusFlags.Stealth) != 0 && avatar.PlayerId != local) continue;
            victimPositions.Add(avatar.transform.position);
        }
        return AttackTargetFinder.Nearest(view.Root.transform.position, victimPositions, reach);
    }

    static void RefreshLabel(View view)
    {
        if (view.Label == null) return;
        CreatureSnapshot s = view.Last;
        string text = CreatureLabel.Resolve(s.Label, view.Word); // Host "{w}" gönderir; ad bu oyuncunun dilinde
        view.Label.text = s.Countdown > 0 ? text + " " + s.Countdown : text;
    }

    void Refresh(View view)
    {
        CreatureSnapshot s = view.Last;

        if (view.Fill != null)
        {
            view.Fill.enabled = !s.HasStruck;
            view.Fill.transform.localScale = Vector3.one * (s.Radius * 2f * Mathf.Clamp01(s.Progress));
            if (s.HasStruck) view.Body.color = new Color(1f, 1f, 1f, 0.55f); // Darbe izi
        }

        RefreshLabel(view);
    }

    static Color ZoneColor(CreatureKind kind)
    {
        switch (kind)
        {
            case CreatureKind.Pull: return new Color(0.7f, 0.4f, 0.95f, 0.95f);
            case CreatureKind.Trap: return new Color(1f, 0.85f, 0.2f, 0.85f);
            case CreatureKind.FireArea: return new Color(1f, 0.45f, 0.1f, 0.38f);
            case CreatureKind.Area: return new Color(0.55f, 0.7f, 0.95f, 0.32f);
            case CreatureKind.Strike: return new Color(AttackColor.r, AttackColor.g, AttackColor.b, 0.28f);
            default: return AttackColor;
        }
    }

    static TextMesh BuildLabel(Transform parent, string text, float radius)
    {
        Font font = GameFont.Get();

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
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
        return mesh;
    }
}
