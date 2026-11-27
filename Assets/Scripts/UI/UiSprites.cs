using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arayüz ve efekt resimlerini (Resources/Sprites/ui_*, fx_*) bulur ve IMGUI ile çizer: tek resim, kare dizisi, 9 dilimli çerçeve.
/// Bulunamayan resim her karede yeniden aranmaz; birkaç saniyede bir tekrar bakılır (resimler oyun açıkken eklenirse de görünsün).
/// Resim yoksa null/boş döner; çağıran düz renkli kutuya düşer.
/// </summary>
public static class UiSprites
{
    /// <summary>Bulunamayan resme tekrar bakma aralığı (sn).</summary>
    const float RetrySeconds = 3f;

    /// <summary>Bir kare dizisinde en fazla kaç kare aranır.</summary>
    const int MaxFrames = 12;

    static readonly Dictionary<string, Sprite> found = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, float> missingUntil = new Dictionary<string, float>();
    static readonly Dictionary<string, Sprite[]> frames = new Dictionary<string, Sprite[]>();
    static readonly Dictionary<Sprite, GUIStyle> styles = new Dictionary<Sprite, GUIStyle>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        found.Clear();
        missingUntil.Clear();
        frames.Clear();
        styles.Clear();
    }

    /// <summary>Tek resim; yoksa null.</summary>
    public static Sprite Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (found.TryGetValue(name, out Sprite sprite) && sprite != null) return sprite;
        float now = Time.realtimeSinceStartup;
        if (missingUntil.TryGetValue(name, out float until) && now < until) return null;

        sprite = SpriteLibrary.Get(name);
        if (sprite != null)
        {
            found[name] = sprite;
            missingUntil.Remove(name);
        }
        else missingUntil[name] = now + RetrySeconds;
        return sprite;
    }

    /// <summary>"ad_0", "ad_1"... kareleri; hiç yoksa boş dizi.</summary>
    public static Sprite[] Frames(string prefix)
    {
        if (frames.TryGetValue(prefix, out Sprite[] cached) && cached.Length > 0 && cached[0] != null) return cached;
        if (Get(prefix + "_0") == null) return System.Array.Empty<Sprite>();

        var list = new List<Sprite>();
        for (int i = 0; i < MaxFrames; i++)
        {
            Sprite frame = Get(prefix + "_" + i);
            if (frame == null) break;
            list.Add(frame);
        }
        Sprite[] result = list.ToArray();
        frames[prefix] = result;
        return result;
    }

    /// <summary>
    /// Kare dizisinin <paramref name="elapsed"/> anındaki karesi. <paramref name="loop"/> değilse son kareden sonra null
    /// (efekt bitti). Kare yoksa null.
    /// </summary>
    public static Sprite FrameAt(string prefix, float elapsed, float frameSeconds, bool loop)
    {
        Sprite[] all = Frames(prefix);
        if (all.Length == 0 || elapsed < 0f) return null;
        int index = Mathf.FloorToInt(elapsed / Mathf.Max(0.005f, frameSeconds));
        if (loop) index %= all.Length;
        else if (index >= all.Length) return null;
        return all[index];
    }

    /// <summary>Resmi kutuya gererek çizer (renk ve saydamlıkla); isteğe bağlı olarak kutunun ortası etrafında döndürür.</summary>
    public static void Draw(Rect rect, Sprite sprite, Color tint, float angleDegrees = 0f)
    {
        if (sprite == null || Event.current.type != EventType.Repaint) return;
        Texture2D texture = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);

        Matrix4x4 oldMatrix = GUI.matrix;
        if (Mathf.Abs(angleDegrees) > 0.01f) GUIUtility.RotateAroundPivot(angleDegrees, rect.center);
        Color old = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, texture, uv);
        GUI.color = old;
        GUI.matrix = oldMatrix;
    }

    /// <summary>Düz renk kutu (isteğe bağlı döndürülmüş).</summary>
    public static void Fill(Rect rect, Color color, float angleDegrees = 0f)
    {
        if (Event.current.type != EventType.Repaint) return;
        Matrix4x4 oldMatrix = GUI.matrix;
        if (Mathf.Abs(angleDegrees) > 0.01f) GUIUtility.RotateAroundPivot(angleDegrees, rect.center);
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
        GUI.matrix = oldMatrix;
    }

    /// <summary>
    /// Çerçeve resmini 9 dilimli çizer: köşeler gerilmez, bir resim pikseli ekranda <paramref name="pixelScale"/> olur.
    /// Kenar payları resmin kendi "border" ayarından; yoksa <paramref name="fallbackBorder"/>. Resim yoksa false döner.
    /// </summary>
    public static bool NineSlice(Rect rect, Sprite sprite, Color tint, float pixelScale, int fallbackBorder)
    {
        if (sprite == null) return false;
        if (Event.current.type != EventType.Repaint) return true;

        if (!styles.TryGetValue(sprite, out GUIStyle style) || style == null)
        {
            Vector4 b = sprite.border; // x = sol, y = alt, z = sağ, w = üst
            bool hasBorder = b.sqrMagnitude > 0.01f;
            style = new GUIStyle
            {
                border = hasBorder
                    ? new RectOffset((int)b.x, (int)b.z, (int)b.w, (int)b.y)
                    : new RectOffset(fallbackBorder, fallbackBorder, fallbackBorder, fallbackBorder),
            };
            style.normal.background = sprite.texture;
            styles[sprite] = style;
        }

        float s = Mathf.Max(1f, pixelScale);
        Matrix4x4 oldMatrix = GUI.matrix;
        Color old = GUI.color;
        GUI.color = tint;
        GUI.matrix = oldMatrix * Matrix4x4.TRS(new Vector3(rect.x, rect.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
        style.Draw(new Rect(0f, 0f, rect.width / s, rect.height / s), false, false, false, false);
        GUI.matrix = oldMatrix;
        GUI.color = old;
        return true;
    }

    /// <summary>Yazıyı koyu kenarlı çizer (dünyanın üstünde de okunur): önce 4 yöne kaydırılmış kenar, sonra yazı.</summary>
    public static void OutlinedLabel(Rect rect, string text, GUIStyle style, Color color, Color outline, float thickness)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color old = style.normal.textColor;
        if (thickness > 0f && outline.a > 0f)
        {
            style.normal.textColor = outline;
            GUI.Label(new Rect(rect.x - thickness, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + thickness, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - thickness, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + thickness, rect.width, rect.height), text, style);
        }
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = old;
    }
}
