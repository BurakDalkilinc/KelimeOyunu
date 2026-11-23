using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>Host'un istemcilere gönderdiği bir duvarın fotoğrafı (çizmek için gereken her şey).</summary>
public struct WallSnapshot
{
    public int Id;
    public float CenterX;
    public float CenterY;
    public float DirX;
    public float DirY;
    public float Length;
    public float Thickness;

    /// <summary>Kalan ömrün toplam ömre oranı (0-1); son saniyelerde duvar solar.</summary>
    public float LifeRatio;
}

/// <summary>Duvar fotoğraflarını tek metne çevirir ve geri çözer. Düz hesap, test edilebilir.</summary>
public static class WallSnapshotCodec
{
    const char EntrySeparator = ';';
    const char FieldSeparator = '|';
    const int FieldCount = 8;

    public static string Encode(IReadOnlyList<WallSnapshot> walls)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < walls.Count; i++)
        {
            WallSnapshot w = walls[i];
            if (i > 0) builder.Append(EntrySeparator);
            builder.Append(w.Id).Append(FieldSeparator)
                   .Append(Num(w.CenterX)).Append(FieldSeparator).Append(Num(w.CenterY)).Append(FieldSeparator)
                   .Append(Num(w.DirX)).Append(FieldSeparator).Append(Num(w.DirY)).Append(FieldSeparator)
                   .Append(Num(w.Length)).Append(FieldSeparator).Append(Num(w.Thickness)).Append(FieldSeparator)
                   .Append(Num(w.LifeRatio));
        }
        return builder.ToString();
    }

    /// <summary>Metni çözer; bozuksa false (liste boş döner).</summary>
    public static bool TryDecode(string text, out List<WallSnapshot> walls)
    {
        walls = new List<WallSnapshot>();
        if (string.IsNullOrEmpty(text)) return true;

        foreach (string part in text.Split(EntrySeparator))
        {
            string[] f = part.Split(FieldSeparator);
            if (f.Length != FieldCount || !int.TryParse(f[0], out int id)
                || !TryNum(f[1], out float cx) || !TryNum(f[2], out float cy)
                || !TryNum(f[3], out float dx) || !TryNum(f[4], out float dy)
                || !TryNum(f[5], out float length) || !TryNum(f[6], out float thickness) || !TryNum(f[7], out float ratio))
            {
                walls.Clear();
                return false;
            }

            walls.Add(new WallSnapshot { Id = id, CenterX = cx, CenterY = cy, DirX = dx, DirY = dy, Length = length, Thickness = thickness, LifeRatio = ratio });
        }
        return true;
    }

    static string Num(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static bool TryNum(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
