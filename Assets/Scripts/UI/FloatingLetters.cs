using UnityEngine;

/// <summary>
/// Ana menünün arka planında yavaşça yukarı süzülen harfler. Her harfin yeri sırasından ve zamandan hesaplanır
/// (rastgele sayı yok, kare hızından bağımsız). Düz hesap; çizimi <see cref="MenuBackdropPainter"/> yapar.
/// </summary>
public static class FloatingLetters
{
    const string Alphabet = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

    /// <summary><paramref name="index"/>. harfin <paramref name="time"/> anındaki durumu; üstten çıkınca alttan yeniden girer.</summary>
    public static FloatingLetter Get(int index, float time, float width, float height, float speed, float minSize, float maxSize, float alpha)
    {
        float a = PixelDissolve.Threshold(index, 11);
        float b = PixelDissolve.Threshold(index, 23);
        float c = PixelDissolve.Threshold(index, 37);

        float size = Mathf.Lerp(minSize, maxSize, b);
        float span = height + size * 2f; // Harf tamamen çıkınca alttan girsin
        float rise = time * speed * (0.5f + c);
        float y = Mod(a * span - rise, span) - size;
        float x = c * width + Mathf.Sin(time * (0.3f + a * 0.4f) + index) * size * 0.4f;

        return new FloatingLetter
        {
            Letter = Alphabet[Mathf.FloorToInt(b * Alphabet.Length) % Alphabet.Length],
            X = x,
            Y = y,
            Size = size,
            Angle = Mathf.Sin(time * (0.2f + b * 0.3f) + index * 1.7f) * 18f,
            Alpha = alpha * (0.5f + 0.5f * a),
        };
    }

    static float Mod(float value, float m)
    {
        float r = value % m;
        return r < 0f ? r + m : r;
    }
}
