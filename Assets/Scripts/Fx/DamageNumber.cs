using UnityEngine;

/// <summary>
/// Hasar sayısı (yalnızca görüntü): hasar alınan yerde "-1" / "-2" (kalkan engellediyse "BLOK") piksel yazısı "pat" diye büyük çıkar,
/// yukarı fırlar, yerçekimiyle düşerek söner. Yazı 1 piksellik koyu kenarlıdır ve piksel ızgarasına oturur (isim etiketleri gibi,
/// bkz. <see cref="NameTagView"/>). Hareket hesabı <see cref="HitFeelMath"/>'te, sayılar <see cref="GameConfig"/>'te.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    /// <summary>İsim etiketlerinin (30) üstünde.</summary>
    const int TextOrder = 32;

    const string ShaderPath = "Shaders/PixelText";

    static readonly Vector2[] OutlineOffsets = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    static Material sharedMaterial;
    static bool materialTried;

    GameConfig config;
    Vector2 origin;
    Vector2 velocity;
    float age;
    Color color;
    TextMesh main;
    TextMesh[] outline;

    /// <summary><paramref name="at"/> noktasında (karakterin merkezi) <paramref name="text"/> yazısını <paramref name="textColor"/> renkte uçurur.</summary>
    public static DamageNumber Spawn(Vector2 at, string text, Color textColor)
    {
        if (string.IsNullOrEmpty(text)) return null;
        GameConfig config = GameConfigLoader.Get();

        var go = new GameObject("Hasar sayısı " + text);
        var number = go.AddComponent<DamageNumber>();
        number.config = config;
        number.origin = at + Vector2.up * config.damageNumberHeight;
        number.velocity = new Vector2(Random.Range(-config.damageNumberSideSpeed, config.damageNumberSideSpeed), config.damageNumberRiseSpeed);
        number.color = textColor;
        number.Build(text);
        number.Apply();
        return number;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= config.damageNumberSeconds)
        {
            Destroy(gameObject);
            return;
        }
        Apply();
    }

    void Apply()
    {
        float alpha = HitFeelMath.DamageNumberAlpha(age, config.damageNumberSeconds, config.damageNumberFadeFraction);
        float scale = HitFeelMath.DamageNumberScale(age, config.damageNumberPopSeconds, config.damageNumberPopScale);
        Vector2 at = origin + HitFeelMath.DamageNumberOffset(age, velocity, config.damageNumberGravity);

        // Piksel ızgarasına oturt (büyürken de harfler yarım piksele düşmesin diye büyüklük tam piksel katına yuvarlanır).
        float pixel = config.damageNumberPixelSize;
        transform.position = new Vector3(Mathf.Round(at.x / pixel) * pixel, Mathf.Round(at.y / pixel) * pixel, 0f);
        transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Round(scale * 2f) * 0.5f);

        Color c = color;
        c.a *= alpha;
        main.color = c;
        Color edge = config.nameTagOutlineColor;
        edge.a *= alpha;
        foreach (TextMesh each in outline) each.color = edge;
    }

    void Build(string text)
    {
        outline = new TextMesh[OutlineOffsets.Length];
        for (int i = 0; i < OutlineOffsets.Length; i++)
            outline[i] = CreateText(text, OutlineOffsets[i] * config.damageNumberPixelSize, TextOrder - 1);
        main = CreateText(text, Vector2.zero, TextOrder);
    }

    TextMesh CreateText(string text, Vector2 offset, int order)
    {
        Font font = GameFont.Get();
        var go = new GameObject("yazı");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;

        var mesh = go.AddComponent<TextMesh>();
        mesh.font = font;
        mesh.text = text;
        mesh.fontSize = config.damageNumberFontSize;
        // Bir yazı tipi pikseli (fontSize / PixelGrid doku pikseli) tam damageNumberPixelSize olsun (bkz. NameTagView).
        mesh.characterSize = config.damageNumberPixelSize * 10f * GameFont.PixelGrid / Mathf.Max(1, config.damageNumberFontSize);
        mesh.anchor = TextAnchor.LowerCenter;
        mesh.alignment = TextAlignment.Center;

        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = PixelMaterial(font);
        meshRenderer.sortingOrder = order;
        return mesh;
    }

    /// <summary>Yazı tipi dokusunu nokta örnekleyen ortak malzeme; shader yoksa yazı tipinin kendi malzemesi.</summary>
    static Material PixelMaterial(Font font)
    {
        if (!materialTried)
        {
            materialTried = true;
            var shader = Resources.Load<Shader>(ShaderPath);
            if (shader != null && shader.isSupported)
            {
                sharedMaterial = new Material(shader) { name = "Piksel hasar yazısı", hideFlags = HideFlags.DontSave };
                Font.textureRebuilt += OnFontTextureRebuilt;
            }
        }
        if (sharedMaterial == null) return font.material;

        sharedMaterial.mainTexture = font.material.mainTexture; // Yazı tipi dokusu büyüyüp yeniden kurulmuş olabilir
        return sharedMaterial;
    }

    static void OnFontTextureRebuilt(Font font)
    {
        if (sharedMaterial != null && font == GameFont.Get()) sharedMaterial.mainTexture = font.material.mainTexture;
    }
}
