using UnityEngine;

/// <summary>
/// Tam ekran IMGUI ekranlarının (koleksiyon defteri, ayarlar) ortak çizim yardımcıları: 9 dilimli çerçeve, düz renk kutu,
/// en/boy oranını koruyan sprite. Resim bulunamazsa düz renge düşülür.
/// </summary>
public static class UiDraw
{
    /// <summary>Resources/Sprites içindeki resimden 9 dilimli stil; resim yoksa null (düz renge düşülür).</summary>
    public static GUIStyle NineSlice(string spriteName, int border)
    {
        Sprite sprite = SpriteLibrary.Get(spriteName);
        if (sprite == null) return null;
        var style = new GUIStyle { border = new RectOffset(border, border, border, border) };
        style.normal.background = sprite.texture;
        return style;
    }

    /// <summary>
    /// Çerçeve resmini 9 dilim olarak çizer (kenarlar gerilmez); stil yoksa düz renk kutu.
    /// Resmin bir pikseli ekranda <paramref name="pixelScale"/> kadar olsun diye çizim o ölçekte yapılır.
    /// </summary>
    public static void Frame(Rect rect, GUIStyle style, Color fallback, float pixelScale)
    {
        if (style == null) { Fill(rect, fallback); return; }
        if (Event.current.type != EventType.Repaint) return;

        float s = Mathf.Max(1f, pixelScale);
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.Scale(new Vector3(s, s, 1f));
        style.Draw(new Rect(rect.x / s, rect.y / s, rect.width / s, rect.height / s), false, false, false, false);
        GUI.matrix = old;
    }

    /// <summary>Düz renk kutu.</summary>
    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    /// <summary>Kutunun çevresine kalın renkli kenar (kutudan önce çizilir).</summary>
    public static void Outline(Rect rect, Color color, float thickness)
    {
        Fill(new Rect(rect.x - thickness, rect.y - thickness, rect.width + 2 * thickness, rect.height + 2 * thickness), color);
    }

    /// <summary>Sprite'ı kutuya en/boy oranını koruyarak sığdırır.</summary>
    public static void DrawSprite(Rect box, Sprite sprite, float alpha)
    {
        Texture2D texture = sprite.texture;
        Rect tr = sprite.textureRect;
        float aspect = tr.width / tr.height;
        float w = box.width, h = box.width / aspect;
        if (h > box.height) { h = box.height; w = h * aspect; }
        Rect fitted = new Rect(box.x + (box.width - w) * 0.5f, box.y + (box.height - h) * 0.5f, w, h);
        Rect uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);

        Color old = GUI.color;
        GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
        GUI.DrawTextureWithTexCoords(fitted, texture, uv);
        GUI.color = old;
    }

    /// <summary>Kutuyu ortasına doğru küçültür.</summary>
    public static Rect Shrink(Rect rect, float factor)
    {
        float w = rect.width * factor, h = rect.height * factor;
        return new Rect(rect.x + (rect.width - w) * 0.5f, rect.y + (rect.height - h) * 0.5f, w, h);
    }
}
