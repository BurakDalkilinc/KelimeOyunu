/// <summary>
/// Sahne geçişindeki piksel "erime" deseni: ekran karelere bölünür, her kare kendi eşiğinde kararır.
/// Eşikler karenin konumundan hesaplanır (her seferinde aynı desen, rastgele sayı kullanmaz). Düz hesap.
/// </summary>
public static class PixelDissolve
{
    /// <summary>Karenin kararma eşiği (0..1). Aynı kare için hep aynı değer.</summary>
    public static float Threshold(int x, int y)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    /// <summary>İlerleme <paramref name="progress"/> (0 açık, 1 tam siyah) iken kare kapalı mı?</summary>
    public static bool IsCovered(int x, int y, float progress)
    {
        if (progress >= 1f) return true;
        if (progress <= 0f) return false;
        return Threshold(x, y) < progress;
    }
}
