using System.Collections.Generic;
using System.Text;

/// <summary>Maçtaki bir oyuncunun ağdan gönderilen kısa kaydı.</summary>
public struct RosterEntry
{
    public int Id;
    public string Name;
    public bool IsBot;

    public RosterEntry(int id, string name, bool isBot)
    {
        Id = id;
        Name = name;
        IsBot = isBot;
    }
}

/// <summary>
/// Oyuncu listesini tek metne çevirir ve geri çözer ("1|Oyuncu 1|0;100|Bot 1|1"). Ağ üstünden tek RPC ile gider.
/// Ayraç karakterleri isimlerden temizlenir. Düz hesap, test edilebilir.
/// </summary>
public static class RosterCodec
{
    const char EntrySeparator = ';';
    const char FieldSeparator = '|';

    public static string Encode(IReadOnlyList<RosterEntry> entries)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) builder.Append(EntrySeparator);
            string name = (entries[i].Name ?? "").Replace(EntrySeparator, ' ').Replace(FieldSeparator, ' ');
            builder.Append(entries[i].Id).Append(FieldSeparator).Append(name).Append(FieldSeparator).Append(entries[i].IsBot ? '1' : '0');
        }
        return builder.ToString();
    }

    /// <summary>Metni çözer; bozuksa ya da aynı numara iki kez varsa false (liste boş döner).</summary>
    public static bool TryDecode(string text, out List<RosterEntry> entries)
    {
        entries = new List<RosterEntry>();
        if (string.IsNullOrEmpty(text)) return true;

        var ids = new HashSet<int>();
        foreach (string part in text.Split(EntrySeparator))
        {
            string[] fields = part.Split(FieldSeparator);
            if (fields.Length != 3 || !int.TryParse(fields[0], out int id) || (fields[2] != "0" && fields[2] != "1") || !ids.Add(id))
            {
                entries.Clear();
                return false;
            }
            entries.Add(new RosterEntry(id, fields[1], fields[2] == "1"));
        }
        return true;
    }
}
