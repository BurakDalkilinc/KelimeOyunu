using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zar simgesinin yüzleri: 9×9 piksellik, köşeleri yuvarlatılmış kare ve üstünde noktalar. Resim dosyası gerekmez, kodla çizilir.
/// Nokta yerleri <see cref="PipCells"/>'te (düz hesap, test edilebilir); resimler ilk istendiğinde üretilip saklanır.
/// </summary>
public static class DieFaces
{
    /// <summary>Yüz resminin kenar uzunluğu (piksel).</summary>
    public const int Size = 9;

    static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
    static Color cachedBody, cachedPip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => cache.Clear();

    /// <summary>1-6 yüzündeki noktaların piksel yerleri (sol alttan). Aralık dışı sayı 1-6'ya katlanır.</summary>
    public static Vector2Int[] PipCells(int face)
    {
        face = ((face - 1) % 6 + 6) % 6 + 1;
        Vector2Int c = new Vector2Int(4, 4);
        Vector2Int bl = new Vector2Int(2, 2), br = new Vector2Int(6, 2), tl = new Vector2Int(2, 6), tr = new Vector2Int(6, 6);
        Vector2Int ml = new Vector2Int(2, 4), mr = new Vector2Int(6, 4);
        switch (face)
        {
            case 1: return new[] { c };
            case 2: return new[] { bl, tr };
            case 3: return new[] { bl, c, tr };
            case 4: return new[] { bl, br, tl, tr };
            case 5: return new[] { bl, br, c, tl, tr };
            default: return new[] { bl, br, ml, mr, tl, tr };
        }
    }

    /// <summary>
    /// Zar durunca görünen yüz (1-6). Zar oyunda yalnızca kimin yazacağını seçer; bu sayı süstür. Tur numarasından ve zarın
    /// sırasından (0 = sol/üst zar, 1 = sağ zar) hesaplanır: maçın rastgele sayı üretecine dokunmaz, her ekranda aynı çıkar.
    /// </summary>
    public static int LandedFace(int turnNumber, int side)
    {
        unchecked
        {
            int hash = turnNumber * 7919 + side * 104729 + 13;
            hash ^= hash >> 7;
            hash *= 31;
            return ((hash % 6) + 6) % 6 + 1;
        }
    }

    /// <summary>Zar yüzü: varsa Resources/Sprites/ui_die_N resmi, yoksa kodla çizilen yüz.</summary>
    public static Sprite SpriteFor(int face, Color body, Color pip)
    {
        face = ((face - 1) % 6 + 6) % 6 + 1;
        Sprite art = UiSprites.Get("ui_die_" + face);
        return art != null ? art : Get(face, body, pip);
    }

    /// <summary>Yüzün resmi (renk değişirse yeniden üretilir).</summary>
    public static Sprite Get(int face, Color body, Color pip)
    {
        face = ((face - 1) % 6 + 6) % 6 + 1;
        if (body != cachedBody || pip != cachedPip)
        {
            cache.Clear();
            cachedBody = body;
            cachedPip = pip;
        }
        if (cache.TryGetValue(face, out Sprite sprite) && sprite != null) return sprite;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool corner = (x == 0 || x == Size - 1) && (y == 0 || y == Size - 1);
                pixels[y * Size + x] = corner ? Color.clear : body;
            }
        foreach (Vector2Int p in PipCells(face)) pixels[p.y * Size + p.x] = pip;
        texture.SetPixels(pixels);
        texture.Apply();

        sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        cache[face] = sprite;
        return sprite;
    }
}
