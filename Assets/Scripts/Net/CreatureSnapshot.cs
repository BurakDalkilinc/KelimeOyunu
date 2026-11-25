using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>İstemcinin yaratığı nasıl çizeceğini belirleyen tür.</summary>
public enum CreatureKind
{
    /// <summary>Hedefini kovalayan ya da gezinen yaratık (zombi, kurt, arı, "???").</summary>
    Chaser,

    /// <summary>Anında vuran uyarı alanı (yumruk, bomba).</summary>
    Strike,

    /// <summary>Sürekli alan (bulut).</summary>
    Area,

    /// <summary>Yakıcı alan (ateş).</summary>
    FireArea,

    /// <summary>Yerde bekleyen tuzak (muz kabuğu).</summary>
    Trap,

    /// <summary>Çeken yaratık (mıknatıs).</summary>
    Pull,
}

/// <summary>
/// Host'un istemcilere gönderdiği bir yaratığın "fotoğrafı": çizmek için gereken her şey, hiçbir kural yok.
/// Düz veri; ağ kütüphanesinden bağımsız.
/// </summary>
public struct CreatureSnapshot
{
    public int Id;

    /// <summary>Kelimenin kimliği (sprite seçimi için); bilinmeyen kelimede boş.</summary>
    public string WordId;

    public float X;
    public float Y;
    public float Radius;
    public CreatureKind Kind;

    /// <summary>Uyarı ilerlemesi 0-1 (yalnızca <see cref="CreatureKind.Strike"/>).</summary>
    public float Progress;

    /// <summary>Anında vuran yaratık vurdu mu (darbe izi gösterilir).</summary>
    public bool HasStruck;

    /// <summary>Patlayıcının kalan saniyesi (3, 2, 1); yoksa 0.</summary>
    public int Countdown;

    /// <summary>Yaratığın üstündeki yazı; boşsa yazı yok.</summary>
    public string Label;

    /// <summary>Yaratığın şimdiye kadarki vuruş sayısı; istemci artışı görünce saldırı animasyonunu oynatır.</summary>
    public int Attacks;
}

/// <summary>
/// Yaratık fotoğraflarını tek metne çevirir ve geri çözer. Ayraç karakterleri yazılardan temizlenir.
/// Düz hesap, test edilebilir.
/// </summary>
public static class CreatureSnapshotCodec
{
    const char EntrySeparator = ';';
    const char FieldSeparator = '|';
    const int FieldCount = 11;

    public static string Encode(IReadOnlyList<CreatureSnapshot> snapshots)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < snapshots.Count; i++)
        {
            CreatureSnapshot s = snapshots[i];
            if (i > 0) builder.Append(EntrySeparator);

            builder.Append(s.Id).Append(FieldSeparator)
                   .Append(Clean(s.WordId)).Append(FieldSeparator)
                   .Append(Num(s.X)).Append(FieldSeparator)
                   .Append(Num(s.Y)).Append(FieldSeparator)
                   .Append(Num(s.Radius)).Append(FieldSeparator)
                   .Append((int)s.Kind).Append(FieldSeparator)
                   .Append(Num(s.Progress)).Append(FieldSeparator)
                   .Append(s.HasStruck ? '1' : '0').Append(FieldSeparator)
                   .Append(s.Countdown).Append(FieldSeparator)
                   .Append(Clean(s.Label)).Append(FieldSeparator)
                   .Append(s.Attacks);
        }
        return builder.ToString();
    }

    /// <summary>Metni çözer; bozuksa false (liste boş döner).</summary>
    public static bool TryDecode(string text, out List<CreatureSnapshot> snapshots)
    {
        snapshots = new List<CreatureSnapshot>();
        if (string.IsNullOrEmpty(text)) return true;

        foreach (string part in text.Split(EntrySeparator))
        {
            string[] f = part.Split(FieldSeparator);
            if (f.Length != FieldCount
                || !int.TryParse(f[0], out int id)
                || !TryNum(f[2], out float x) || !TryNum(f[3], out float y) || !TryNum(f[4], out float radius)
                || !int.TryParse(f[5], out int kind) || !System.Enum.IsDefined(typeof(CreatureKind), kind)
                || !TryNum(f[6], out float progress)
                || (f[7] != "0" && f[7] != "1")
                || !int.TryParse(f[8], out int countdown)
                || !int.TryParse(f[10], out int attacks))
            {
                snapshots.Clear();
                return false;
            }

            snapshots.Add(new CreatureSnapshot
            {
                Id = id, WordId = f[1], X = x, Y = y, Radius = radius, Kind = (CreatureKind)kind,
                Progress = progress, HasStruck = f[7] == "1", Countdown = countdown, Label = f[9], Attacks = attacks,
            });
        }
        return true;
    }

    static string Clean(string value)
    {
        return (value ?? "").Replace(EntrySeparator, ' ').Replace(FieldSeparator, ' ');
    }

    static string Num(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    static bool TryNum(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
