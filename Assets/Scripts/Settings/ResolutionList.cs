using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seçilebilir çözünürlükler: ekranın desteklediği boyutlar, tekrarsız ve küçükten büyüğe. Ayarlar ekranındaki
/// "önceki/sonraki" düğmeleri bu listede gezer. Düz hesap, test edilebilir (liste dışarıdan verilir).
/// </summary>
public class ResolutionList
{
    readonly List<Vector2Int> sizes = new List<Vector2Int>();

    /// <summary>Boyutlar, küçükten büyüğe.</summary>
    public IReadOnlyList<Vector2Int> Sizes => sizes;

    public ResolutionList(IEnumerable<Vector2Int> available)
    {
        if (available != null)
            foreach (Vector2Int size in available)
                if (size.x > 0 && size.y > 0 && !sizes.Contains(size)) sizes.Add(size);

        sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
    }

    /// <summary>
    /// Verilen boyutun listedeki sırası. Tam eşleşme yoksa alanı en yakın olanınki; liste boşsa -1.
    /// Boyut 0 ise (ekranın kendisi) <paramref name="fallback"/> aranır.
    /// </summary>
    public int IndexOf(int width, int height, Vector2Int fallback)
    {
        if (sizes.Count == 0) return -1;
        if (width <= 0 || height <= 0) { width = fallback.x; height = fallback.y; }

        int best = 0;
        long bestDiff = long.MaxValue;
        for (int i = 0; i < sizes.Count; i++)
        {
            if (sizes[i].x == width && sizes[i].y == height) return i;
            long diff = System.Math.Abs((long)sizes[i].x * sizes[i].y - (long)width * height);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }

    /// <summary>Sırayı <paramref name="delta"/> kadar kaydırır; listenin ucunda durur.</summary>
    public int Step(int index, int delta)
    {
        if (sizes.Count == 0) return -1;
        return Mathf.Clamp(index + delta, 0, sizes.Count - 1);
    }
}
