using System;
using UnityEngine;

/// <summary>
/// Karakterin başının üstündeki piksel isim yazısı (1 piksellik koyu kenarlı). Ne yazacağını ve karakterin durumunu sahibi her kare
/// <see cref="NameTagInfo"/> olarak verir; görünür mü, hangi renk kararı <see cref="NameTagRules"/>'dadır. Yazı karakterin çocuğu
/// değildir (düşerken küçülme, araç ölçeği yazıyı bozmasın): ayrı bir nesne olarak karakteri izler. Yalnızca görüntüdür.
/// </summary>
public class NameTagView : MonoBehaviour
{
    /// <summary>Yazı yaratık yazılarının (20) ve karakterlerin üstünde; kenar bir sıra altında.</summary>
    const int TextOrder = 30;

    const string ShaderPath = "Shaders/PixelText";

    static readonly Vector2[] OutlineOffsets = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    static Material sharedMaterial;
    static bool materialTried;

    GameConfig config;
    Func<NameTagInfo> source;
    GameObject root;
    TextMesh main;
    TextMesh[] outline;
    string shownText;

    /// <summary>Karaktere isim etiketi ekler (zaten varsa günceller). <paramref name="info"/> her kare çağrılır.</summary>
    public static NameTagView Attach(GameObject target, GameConfig config, Func<NameTagInfo> info)
    {
        var view = target.GetComponent<NameTagView>();
        if (view == null) view = target.AddComponent<NameTagView>();
        view.config = config;
        view.source = info;
        return view;
    }

    void LateUpdate()
    {
        if (config == null || source == null) return;

        NameTagInfo info = source();
        bool visible = config.nameTagsEnabled && SettingsService.ShowNameTags && NameTagRules.IsVisible(info, false);
        if (visible && TerrainView.Active != null && TerrainView.Active.IsUnderVisibleRoof(transform.position))
            visible = NameTagRules.IsVisible(info, true);

        if (!visible)
        {
            if (root != null && root.activeSelf) root.SetActive(false);
            return;
        }

        if (root == null) Build();
        if (!root.activeSelf) root.SetActive(true);

        string text = NameTagRules.Shorten(info.Name, config.nameTagMaxLength);
        if (text != shownText)
        {
            shownText = text;
            main.text = text;
            foreach (TextMesh edge in outline) edge.text = text;
        }

        Color color = NameTagRules.ColorFor(info, config.nameTagOwnColor, config.nameTagOtherColor, config.nameTagFaintAlpha);
        main.color = color;
        Color edgeColor = config.nameTagOutlineColor;
        edgeColor.a *= color.a;
        foreach (TextMesh edge in outline) edge.color = edgeColor;

        // Yazı tipi pikselinin ızgarasına oturt: karakter yumuşak kayarken harfler yarım piksele düşüp titremesin.
        float pixel = config.nameTagPixelSize;
        Vector3 at = transform.position + Vector3.up * config.nameTagHeight;
        root.transform.position = new Vector3(Mathf.Round(at.x / pixel) * pixel, Mathf.Round(at.y / pixel) * pixel, 0f);
    }

    /// <summary>Yazıyı ve dört yöne birer piksel kaydırılmış koyu kopyalarını (kenar) kurar.</summary>
    void Build()
    {
        root = new GameObject("İsim: " + name);
        outline = new TextMesh[OutlineOffsets.Length];
        for (int i = 0; i < OutlineOffsets.Length; i++)
            outline[i] = CreateText("kenar", OutlineOffsets[i] * config.nameTagPixelSize, TextOrder - 1);
        main = CreateText("yazı", Vector2.zero, TextOrder);
    }

    TextMesh CreateText(string label, Vector2 offset, int order)
    {
        Font font = GameFont.Get();
        var go = new GameObject(label);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = offset;

        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        mesh.fontSize = config.nameTagFontSize;
        // TextMesh'te bir doku pikseli = characterSize / 10 birim. Bir yazı tipi pikseli (fontSize / PixelGrid doku pikseli)
        // tam nameTagPixelSize olsun.
        mesh.characterSize = config.nameTagPixelSize * 10f * GameFont.PixelGrid / Mathf.Max(1, config.nameTagFontSize);
        mesh.anchor = TextAnchor.LowerCenter;
        mesh.alignment = TextAlignment.Center;

        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = PixelMaterial(font);
        meshRenderer.sortingOrder = order;
        return mesh;
    }

    /// <summary>
    /// Yazı tipi dokusunu nokta örnekleyen ortak malzeme. Shader bulunamazsa yazı tipinin kendi malzemesi (biraz yumuşak ama çalışır).
    /// </summary>
    static Material PixelMaterial(Font font)
    {
        if (!materialTried)
        {
            materialTried = true;
            var shader = Resources.Load<Shader>(ShaderPath);
            if (shader != null && shader.isSupported)
            {
                sharedMaterial = new Material(shader) { name = "Piksel isim yazısı" };
                Font.textureRebuilt += OnFontTextureRebuilt; // Doku büyüyüp yeniden kurulursa yenisini kullan
            }
        }
        if (sharedMaterial == null) return font.material;

        sharedMaterial.mainTexture = font.material.mainTexture;
        return sharedMaterial;
    }

    static void OnFontTextureRebuilt(Font font)
    {
        if (sharedMaterial != null && font == GameFont.Get()) sharedMaterial.mainTexture = font.material.mainTexture;
    }

    void OnDisable()
    {
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root);
    }
}
