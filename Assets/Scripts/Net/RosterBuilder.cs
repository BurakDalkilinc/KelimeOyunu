using System.Collections.Generic;

/// <summary>
/// Maçın oyuncu kadrosunu kurar: bağlı insanlar + hedef oyuncu sayısına tamamlayan botlar. Düz hesap, test edilebilir.
/// </summary>
public static class RosterBuilder
{
    /// <summary>Bir maçtaki en çok oyuncu (GDD: 20).</summary>
    public const int MaxPlayers = 20;

    /// <summary>Bir maçtaki en az oyuncu.</summary>
    public const int MinPlayers = 2;

    /// <summary>Başlangıçta hedeflenen oyuncu sayısı (eksikler bot olur).</summary>
    public const int DefaultTargetPlayers = 4;

    /// <summary>Bot numaraları bağlantı numaralarıyla çakışmasın diye bu sayıdan başlar.</summary>
    public const int FirstBotId = 100;

    /// <summary>Hedef sayıyı izin verilen aralığa çeker.</summary>
    public static int ClampTarget(int target)
    {
        if (target < MinPlayers) return MinPlayers;
        return target > MaxPlayers ? MaxPlayers : target;
    }

    /// <summary>
    /// İnsanlar önce, ardından eksikleri tamamlayan botlar. Toplam <c>max(hedef, insan sayısı)</c> olur
    /// (en çok <see cref="MaxPlayers"/>; fazla insan kadroya alınmaz). İnsanın adı <paramref name="names"/>'ten
    /// (istemcinin bildirdiği ad) gelir ve burada yeniden temizlenir; adı yoksa "Oyuncu N". Kadroda aynı ad iki kez olmaz.
    /// </summary>
    public static List<RosterEntry> Build(IReadOnlyList<ulong> humanClientIds, int targetPlayers,
        IReadOnlyDictionary<ulong, string> names = null, int maxNameLength = PlayerNameSanitizer.DefaultMaxLength)
    {
        List<RosterEntry> roster = BuildHumans(humanClientIds, names, maxNameLength);
        var taken = new List<string>();
        foreach (RosterEntry entry in roster) taken.Add(entry.Name);

        int total = System.Math.Max(ClampTarget(targetPlayers), roster.Count);
        for (int bot = 0; roster.Count < total; bot++)
        {
            string name = PlayerNameSanitizer.MakeUnique("Bot " + (bot + 1), taken, maxNameLength);
            taken.Add(name);
            roster.Add(new RosterEntry(FirstBotId + bot, name, true));
        }

        return roster;
    }

    /// <summary>
    /// Yalnızca insanlar (lobi ekranındaki isim listesi ve kadronun ilk kısmı): temizlenmiş, birbirinden farklı adlar.
    /// Önce bağlanan adını korur; sonra gelen aynı adlı oyuncu " (2)" alır.
    /// </summary>
    public static List<RosterEntry> BuildHumans(IReadOnlyList<ulong> humanClientIds,
        IReadOnlyDictionary<ulong, string> names = null, int maxNameLength = PlayerNameSanitizer.DefaultMaxLength)
    {
        var roster = new List<RosterEntry>();
        var taken = new List<string>();

        foreach (ulong clientId in humanClientIds)
        {
            if (roster.Count >= MaxPlayers) break;

            string raw = names != null && names.TryGetValue(clientId, out string given) ? given : null;
            string name = PlayerNameSanitizer.MakeUnique(PlayerNameSanitizer.Sanitize(raw, (int)clientId, maxNameLength), taken, maxNameLength);
            taken.Add(name);
            roster.Add(new RosterEntry((int)clientId, name, false));
        }

        return roster;
    }
}
