using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>Host'un istemcilere gönderdiği bir çözüm alanının (sığınak, kule, mağara, ev) fotoğrafı.</summary>
public struct ZoneSnapshot
{
    public int Id;

    /// <summary>Çözüm türü: "shelter", "tower", "cave", "houses".</summary>
    public string Type;

    public float X;
    public float Y;
    public float Radius;
    public int Capacity;
    public int Occupied;
}

/// <summary>
/// Büyük tehdidin genel durumu: ekranın üstündeki alarm, etki anındaki parlama ve ilerleyen dalga.
/// Düz veri; ağ kütüphanesinden bağımsız.
/// </summary>
public struct ThreatStatus
{
    /// <summary>Alarmı verilen tehdidin kelime kimliği; alarm yoksa boş metin.</summary>
    public string AlarmWordId;

    /// <summary>Etkinin başlamasına kalan tam saniye (0 ise süre yazılmaz).</summary>
    public int AlarmSeconds;

    /// <summary>Her etki anında bir artar; istemci artışı görünce ekranı parlatır.</summary>
    public int ImpactCount;

    public bool WaveActive;
    public float WaveDirX;
    public float WaveDirY;
    public float WaveFront;
}

/// <summary>Çözüm alanı fotoğraflarını tek metne çevirir ve geri çözer. Düz hesap, test edilebilir.</summary>
public static class ZoneSnapshotCodec
{
    const char EntrySeparator = ';';
    const char FieldSeparator = '|';
    const int FieldCount = 7;

    public static string Encode(IReadOnlyList<ZoneSnapshot> zones)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < zones.Count; i++)
        {
            ZoneSnapshot z = zones[i];
            if (i > 0) builder.Append(EntrySeparator);
            builder.Append(z.Id).Append(FieldSeparator)
                   .Append((z.Type ?? "").Replace(EntrySeparator, ' ').Replace(FieldSeparator, ' ')).Append(FieldSeparator)
                   .Append(Num(z.X)).Append(FieldSeparator).Append(Num(z.Y)).Append(FieldSeparator)
                   .Append(Num(z.Radius)).Append(FieldSeparator)
                   .Append(z.Capacity).Append(FieldSeparator).Append(z.Occupied);
        }
        return builder.ToString();
    }

    /// <summary>Metni çözer; bozuksa false (liste boş döner).</summary>
    public static bool TryDecode(string text, out List<ZoneSnapshot> zones)
    {
        zones = new List<ZoneSnapshot>();
        if (string.IsNullOrEmpty(text)) return true;

        foreach (string part in text.Split(EntrySeparator))
        {
            string[] f = part.Split(FieldSeparator);
            if (f.Length != FieldCount || !int.TryParse(f[0], out int id)
                || !TryNum(f[2], out float x) || !TryNum(f[3], out float y) || !TryNum(f[4], out float radius)
                || !int.TryParse(f[5], out int capacity) || !int.TryParse(f[6], out int occupied))
            {
                zones.Clear();
                return false;
            }

            zones.Add(new ZoneSnapshot { Id = id, Type = f[1], X = x, Y = y, Radius = radius, Capacity = capacity, Occupied = occupied });
        }
        return true;
    }

    static string Num(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static bool TryNum(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>Tehdit durumunu tek metne çevirir ve geri çözer. Düz hesap, test edilebilir.</summary>
public static class ThreatStatusCodec
{
    const char FieldSeparator = '|';
    const int FieldCount = 8;

    public static string Encode(ThreatStatus s)
    {
        return string.Join(FieldSeparator.ToString(), new[]
        {
            (s.AlarmWordId ?? "").Replace(FieldSeparator, ' '),
            s.AlarmSeconds.ToString(CultureInfo.InvariantCulture),
            s.ImpactCount.ToString(CultureInfo.InvariantCulture),
            s.WaveActive ? "1" : "0",
            Num(s.WaveDirX), Num(s.WaveDirY), Num(s.WaveFront),
            "v1",
        });
    }

    /// <summary>Metni çözer; bozuksa false (boş durum döner).</summary>
    public static bool TryDecode(string text, out ThreatStatus status)
    {
        status = new ThreatStatus { AlarmWordId = "" };
        if (string.IsNullOrEmpty(text)) return false;

        string[] f = text.Split(FieldSeparator);
        if (f.Length != FieldCount || f[7] != "v1"
            || !int.TryParse(f[1], out int seconds) || !int.TryParse(f[2], out int impact)
            || (f[3] != "0" && f[3] != "1")
            || !TryNum(f[4], out float dx) || !TryNum(f[5], out float dy) || !TryNum(f[6], out float front))
            return false;

        status = new ThreatStatus
        {
            AlarmWordId = f[0], AlarmSeconds = seconds, ImpactCount = impact,
            WaveActive = f[3] == "1", WaveDirX = dx, WaveDirY = dy, WaveFront = front,
        };
        return true;
    }

    static string Num(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static bool TryNum(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
