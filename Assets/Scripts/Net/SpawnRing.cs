using UnityEngine;

/// <summary>
/// Arenanın ortasında bir halka üzerinde eşit aralıklı başlangıç noktaları verir (20 oyuncuya kadar üst üste binmesin diye).
/// Düz hesap, test edilebilir.
/// </summary>
public static class SpawnRing
{
    /// <summary>
    /// <paramref name="index"/>. noktayı verir (0..count-1). Halkanın yarıçapı, arenanın kısa yarı kenarının
    /// <paramref name="radiusFraction"/> katıdır; bu yüzden nokta her zaman arenanın içindedir.
    /// </summary>
    public static Vector2 Point(ArenaBounds arena, int index, int count, float radiusFraction)
    {
        count = Mathf.Max(1, count);
        float radius = Mathf.Min(arena.HalfSize.x, arena.HalfSize.y) * Mathf.Clamp01(radiusFraction);
        float angle = Mathf.PI * 2f * (Mathf.Abs(index) % count) / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
