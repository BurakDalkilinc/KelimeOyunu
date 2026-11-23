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
    /// (en çok <see cref="MaxPlayers"/>; fazla insan kadroya alınmaz).
    /// </summary>
    public static List<RosterEntry> Build(IReadOnlyList<ulong> humanClientIds, int targetPlayers)
    {
        var roster = new List<RosterEntry>();

        foreach (ulong clientId in humanClientIds)
        {
            if (roster.Count >= MaxPlayers) break;
            roster.Add(new RosterEntry((int)clientId, "Oyuncu " + clientId, false));
        }

        int total = System.Math.Max(ClampTarget(targetPlayers), roster.Count);
        for (int bot = 0; roster.Count < total; bot++)
            roster.Add(new RosterEntry(FirstBotId + bot, "Bot " + (bot + 1), true));

        return roster;
    }
}
