using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Katmanlı (sprite stacking) nesnelerin dilimlerini yükler: <c>Resources/Stacks/&lt;ad&gt;.png</c> yatay bir şerittir
/// (dilim boyu = resmin yüksekliği), soldan sağa en alttan en üste dilimler. Şerit yoksa null döner ve çağıran düz resme döner.
/// </summary>
public static class StackLibrary
{
    const string Folder = "Stacks/";
    const float PixelsPerUnit = 16f;

    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    /// <summary>Adı verilen nesnenin dilimleri (0 = en alt); yoksa null.</summary>
    public static Sprite[] Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        // Önbellekteki dilimler bir önceki Play oturumunda silinmiş olabilir (Play'e girişte kod yeniden yüklenmiyor): yeniden kes.
        if (cache.TryGetValue(name, out Sprite[] slices) && (slices == null || (slices.Length > 0 && slices[0] != null)))
            return slices;

        Texture2D sheet = Resources.Load<Texture2D>(Folder + name);
        if (sheet != null && sheet.height > 0 && sheet.width >= sheet.height)
        {
            int size = sheet.height;
            int count = sheet.width / size;
            slices = new Sprite[count];
            for (int i = 0; i < count; i++)
                slices[i] = Sprite.Create(sheet, new Rect(i * size, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit,
                                          0, SpriteMeshType.FullRect);
        }

        cache[name] = slices;
        return slices;
    }

    /// <summary>
    /// Elde katmanlı çizim yokken düz bir resmi "kalınlaştırır": aynı resim <paramref name="layers"/> kez üst üste konur
    /// (alt katlar <see cref="SpriteStack"/> tarafından koyulaştırılır). Bloklar (duvar, çatı) için yeterlidir.
    /// </summary>
    public static Sprite[] Extrude(Sprite sprite, int layers)
    {
        if (sprite == null || layers <= 0) return null;
        var slices = new Sprite[layers];
        for (int i = 0; i < layers; i++) slices[i] = sprite;
        return slices;
    }
}
