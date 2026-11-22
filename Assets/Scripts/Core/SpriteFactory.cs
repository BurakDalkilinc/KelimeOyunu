using UnityEngine;

/// <summary>
/// Prototip için kodla sprite üretir (hazır grafik gelene kadar renkli kare ve daire).
/// Her sprite bir kez üretilir ve tekrar kullanılır.
/// </summary>
public static class SpriteFactory
{
    static Sprite square;
    static Sprite circle;

    /// <summary>1x1 birim beyaz kare. Rengi SpriteRenderer.color ile verilir.</summary>
    public static Sprite Square()
    {
        if (square == null)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            square.name = "ProtoSquare";
        }
        return square;
    }

    /// <summary>1 birim çapında beyaz daire. Rengi SpriteRenderer.color ile verilir.</summary>
    public static Sprite Circle()
    {
        if (circle == null)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            circle.name = "ProtoCircle";
        }
        return circle;
    }
}
