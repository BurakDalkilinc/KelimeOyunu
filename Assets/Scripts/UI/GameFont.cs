using UnityEngine;

/// <summary>
/// Oyunun piksel yazı tipi (Resources/Fonts/PixelifySans, SIL OFL lisanslı). Bulunamazsa Unity'nin varsayılan yazısına düşer.
/// </summary>
public static class GameFont
{
    const string ResourcePath = "Fonts/PixelifySans";

    static Font cached;

    /// <summary>Piksel yazı tipi; yoksa yerleşik varsayılan yazı.</summary>
    public static Font Get()
    {
        if (cached == null)
        {
            cached = Resources.Load<Font>(ResourcePath);
            if (cached == null) cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return cached;
    }
}
