using UnityEngine;

/// <summary>
/// Çatı kuralı: içeride biri (bu bilgisayardaki oyuncu) varsa, ev yanıyorsa ya da yanıp çökmüşse çatı görünmez.
/// Düz hesap, test edilebilir; çizim <see cref="TerrainView"/>'dadır.
/// </summary>
public static class RoofRules
{
    /// <summary>Evin karolarının bu kadarı kömürleşmişse ev yanıp çökmüş sayılır ve çatısı gider.</summary>
    public const float BurnedDownFraction = 0.5f;

    /// <summary>Çatı görünmesin mi?</summary>
    public static bool ShouldHide(TerrainMap map, House house, Vector2? viewer, System.Func<int, int, bool> isBurning)
    {
        if (viewer.HasValue)
        {
            Vector2Int cell = map.CellOf(viewer.Value);
            if (house.Contains(cell.x, cell.y)) return true; // İçeridesin: içi görünsün
        }

        int charred = 0;
        for (int y = house.Y; y < house.Y + house.H; y++)
        {
            for (int x = house.X; x < house.X + house.W; x++)
            {
                if (isBurning != null && isBurning(x, y)) return true; // Alev çatının altında kalmasın
                if (map.At(x, y) == TileType.CharredFloor) charred++;
            }
        }

        return charred >= Mathf.CeilToInt(house.W * house.H * BurnedDownFraction);
    }
}
