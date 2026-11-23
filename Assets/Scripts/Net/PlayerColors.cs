using UnityEngine;

/// <summary>
/// Oyuncu numarasından ayırt edilebilir bir renk üretir (altın oran adımıyla ton, ayrıca doygunluk ve parlaklık değişir), böylece 20 oyuncu bile
/// birbirinden ayrılır ve herkes aynı oyuncuyu her bilgisayarda aynı renkte görür. Düz hesap, test edilebilir.
/// </summary>
public static class PlayerColors
{
    const float GoldenRatioStep = 0.61803398875f;

    public static Color For(int playerId)
    {
        int id = Mathf.Max(0, playerId);
        float hue = Mathf.Repeat(id * GoldenRatioStep, 1f);

        // Komşu tonlar karışmasın diye doygunluk ve parlaklık da numaraya göre değişir (koyu zeminde hep okunur kalır).
        float saturation = 0.5f + 0.2f * (id % 2);
        float value = 0.95f - 0.13f * (id % 3);
        return Color.HSVToRGB(hue, saturation, value);
    }
}
