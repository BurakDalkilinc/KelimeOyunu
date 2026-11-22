using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Sprites içindeki piksel sanat sprite'larını ada göre verir. Bulamazsa null döner;
/// çağıran taraf eski daire/kare görüntüye geri düşer. Animasyonlu sprite iki karedir: "ad" ve "ad_b".
/// </summary>
public static class SpriteLibrary
{
    const string Folder = "Sprites/";

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>Tek sprite; yoksa null.</summary>
    public static Sprite Get(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        if (!cache.TryGetValue(spriteName, out Sprite sprite) || sprite == null)
        {
            sprite = Resources.Load<Sprite>(Folder + spriteName);
            cache[spriteName] = sprite;
        }
        return sprite;
    }

    /// <summary>Animasyon kareleri: "ad" (+ varsa "ad_b"). Hiç sprite yoksa boş dizi.</summary>
    public static Sprite[] GetFrames(string spriteName)
    {
        Sprite a = Get(spriteName);
        if (a == null) return System.Array.Empty<Sprite>();
        Sprite b = Get(spriteName + "_b");
        return b == null ? new[] { a } : new[] { a, b };
    }

    /// <summary>Kelimenin sprite adı: JSON'daki "sprite" alanı, yoksa kelime kimliği; hiçbiri yoksa "unknown".</summary>
    public static string NameFor(WordEntry word)
    {
        if (word != null && !string.IsNullOrEmpty(word.sprite) && Get(word.sprite) != null) return word.sprite;
        if (word != null && Get(word.id) != null) return word.id;
        return "unknown";
    }
}
