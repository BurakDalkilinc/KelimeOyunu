using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yazı kutusu efektlerinin düz hesapları: kutunun hali, tuş sesinin perdesi, "..." noktaları, silinen harfin düşüşü,
/// reddedilen harflerin dağılıp geri gelmesi, metnin harflere bölünmesi. Test edilebilir; Unity sahnesi gerekmez.
/// </summary>
public static class TypingFxMath
{
    /// <summary>Aşamaya ve yazanın bu bilgisayardaki oyuncu olup olmadığına göre kutunun hali.</summary>
    public static TypingBoxMode ModeFor(TurnPhase phase, bool localWriter)
    {
        switch (phase)
        {
            case TurnPhase.Dice: return TypingBoxMode.Rolling;
            case TurnPhase.Typing: return localWriter ? TypingBoxMode.Typing : TypingBoxMode.Watching;
            default: return TypingBoxMode.Hidden;
        }
    }

    /// <summary>
    /// Tuş sesinin perdesi: harfe göre <paramref name="steps"/> basamaktan biri, 1 ± <paramref name="spread"/> aralığında.
    /// Aynı harf hep aynı perdeyi verir (yazı bir melodi gibi duyulur).
    /// </summary>
    public static float KeyPitch(char letter, float spread, int steps)
    {
        if (steps < 2 || spread <= 0f) return 1f;
        int step = letter % steps;
        float t = step / (float)(steps - 1);
        return 1f + Mathf.Lerp(-spread, spread, t);
    }

    /// <summary>"Yazıyor..." animasyonundaki nokta sayısı (0..<paramref name="maxDots"/>, dönerek artar).</summary>
    public static int DotCount(float time, float periodSeconds, int maxDots)
    {
        if (maxDots <= 0) return 0;
        if (periodSeconds <= 0f) return maxDots;
        int step = Mathf.FloorToInt(Mathf.Max(0f, time) / periodSeconds);
        return step % (maxDots + 1);
    }

    /// <summary>
    /// Silinen harfin düşüşü: başladıktan <paramref name="elapsed"/> sn sonra aşağı kayma (piksel, ekranda aşağı +) ve saydamlık.
    /// Süre bitince saydamlık 0.
    /// </summary>
    public static Vector2 Fall(float elapsed, float duration, float gravityPixels)
    {
        if (duration <= 0f || elapsed >= duration) return new Vector2(0f, 0f);
        float t = Mathf.Max(0f, elapsed);
        float drop = 0.5f * gravityPixels * t * t;
        float alpha = 1f - Easing.InCubic(t / duration);
        return new Vector2(drop, alpha);
    }

    /// <summary>
    /// Reddedilen kelimede harfin kayması: harfler ortadan dışa doğru (ve sırayla yukarı/aşağı) dağılır, sonra yerine döner.
    /// Süre dışında sıfır.
    /// </summary>
    public static Vector2 Scatter(int index, int count, float elapsed, float duration, float distance)
    {
        if (duration <= 0f || elapsed < 0f || elapsed >= duration || count <= 0) return Vector2.zero;
        float u = elapsed / duration;
        float amount = Mathf.Sin(u * Mathf.PI) * (1f - u * 0.5f); // Hızla açılır, yavaşça toplanır
        float centered = count > 1 ? index / (float)(count - 1) * 2f - 1f : 0f;
        float vertical = index % 2 == 0 ? -0.6f : 0.6f;
        return new Vector2(centered, vertical) * (distance * amount);
    }

    /// <summary>Metni ekrandaki harflere böler; emoji gibi iki karakterlik simgeler tek harf sayılır.</summary>
    public static List<string> SplitLetters(string text)
    {
        var letters = new List<string>();
        if (string.IsNullOrEmpty(text)) return letters;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                letters.Add(text.Substring(i, 2));
                i++;
            }
            else letters.Add(text[i].ToString());
        }
        return letters;
    }
}
