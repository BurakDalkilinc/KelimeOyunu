using System;
using System.Collections.Generic;

/// <summary>Defter sayfalarının hesabı: kaç sayfa var, bir sayfada hangi kartlar duruyor. Düz hesap, test edilebilir.</summary>
public static class CollectionPaging
{
    /// <summary>Sayfa sayısı; hiç kart yoksa da 1 (boş sayfa).</summary>
    public static int PageCount(int itemCount, int perPage)
    {
        if (perPage <= 0 || itemCount <= 0) return 1;
        return (itemCount + perPage - 1) / perPage;
    }

    /// <summary>Sayfa numarasını 0 ile son sayfa arasına sıkıştırır.</summary>
    public static int ClampPage(int page, int itemCount, int perPage)
    {
        return Math.Max(0, Math.Min(page, PageCount(itemCount, perPage) - 1));
    }

    /// <summary>Verilen sayfadaki kartlar.</summary>
    public static List<T> Slice<T>(IReadOnlyList<T> items, int page, int perPage)
    {
        var result = new List<T>();
        if (items == null || perPage <= 0) return result;
        int start = ClampPage(page, items.Count, perPage) * perPage;
        for (int i = start; i < items.Count && i < start + perPage; i++) result.Add(items[i]);
        return result;
    }
}
