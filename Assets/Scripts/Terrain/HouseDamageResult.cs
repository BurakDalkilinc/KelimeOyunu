using System.Collections.Generic;

/// <summary>Bir yıkım hesabının sonucu: yıkılan kareler ve tamamen çöken evler. Tekrar tekrar kullanılabilir (çöp üretmesin).</summary>
public class HouseDamageResult
{
    public readonly List<DemolishedCell> Cells = new List<DemolishedCell>();
    public readonly List<House> Collapsed = new List<House>();

    public bool Any => Cells.Count > 0;

    public void Clear()
    {
        Cells.Clear();
        Collapsed.Clear();
    }
}
