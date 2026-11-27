/// <summary>Yıkılan tek bir ev duvarı karesi ve eskiden ne duvarı olduğu (tahta, taş, kerpiç: toz/kırıntı rengi buna göre).</summary>
public readonly struct DemolishedCell
{
    public int X { get; }
    public int Y { get; }
    public TileType Wall { get; }

    public DemolishedCell(int x, int y, TileType wall)
    {
        X = x;
        Y = y;
        Wall = wall;
    }
}
