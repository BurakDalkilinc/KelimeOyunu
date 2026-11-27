using System.Collections.Generic;
using System.Text;

/// <summary>
/// Maç sonu ekranının yazıları: kazanan ve maçta yazılan kelimelerin listesi. Düz hesap, test edilebilir.
/// </summary>
public static class MatchSummary
{
    /// <summary>Bir satıra sığacak kelime sayısı (uzun listeler alt satıra geçer).</summary>
    public const int WordsPerLine = 6;

    /// <summary>Başlık: "Kazanan: Bot 3" ya da kimse kalmadıysa "Kazanan yok" (seçili dilde).</summary>
    public static string GetTitle(PlayerState winner)
    {
        return winner != null ? Loc.F("match.winner", winner.Name) : Loc.T("match.no_winner");
    }

    /// <summary>
    /// Yazılan kelimeler, yazılma sırasıyla: "Yazılan kelimeler (3):" ve altında virgülle ayrılmış adlar
    /// (satır başına <see cref="WordsPerLine"/> kelime). Hiç kelime yazılmadıysa bunu söyler.
    /// </summary>
    public static string GetWordsText(IReadOnlyList<WordEntry> usedWords)
    {
        if (usedWords == null || usedWords.Count == 0) return Loc.T("match.no_words");

        var builder = new StringBuilder();
        builder.Append(Loc.F("match.words_header", usedWords.Count));

        for (int i = 0; i < usedWords.Count; i++)
        {
            builder.Append(i % WordsPerLine == 0 ? '\n' : ' ');
            // Adı olmayan (bozuk) kayıtta "?" yazılır; varsa seçili dildeki ad.
            WordEntry word = usedWords[i];
            builder.Append(word != null && !string.IsNullOrEmpty(word.display) ? word.LocalizedDisplay : "?");
            if (i < usedWords.Count - 1) builder.Append(',');
        }
        return builder.ToString();
    }

    /// <summary>
    /// Maç sonu ekranında sırayla belirecek satırlar: (varsa) defter bildirimi, oynanan tur, oyuncu sayısı ve yazılan kelimeler
    /// (kelime listesi tek satır sayılır, kendi içinde alt satırlara bölünür).
    /// </summary>
    public static List<string> GetStatRows(string notice, int turnsPlayed, int playerCount, IReadOnlyList<WordEntry> usedWords)
    {
        var rows = new List<string>();
        if (!string.IsNullOrEmpty(notice)) rows.Add(notice);
        rows.Add(Loc.F("match.stat_turns", turnsPlayed));
        rows.Add(Loc.F("match.stat_players", playerCount));
        rows.Add(GetWordsText(usedWords));
        return rows;
    }

    /// <summary>Başlık, bu bilgisayardaki oyuncu kazandıysa "KAZANDIN!", değilse <see cref="GetTitle"/>.</summary>
    public static string GetTitleFor(PlayerState winner, bool localWon)
    {
        return localWon ? Loc.T("match.you_won") : GetTitle(winner);
    }

    /// <summary>Yeniden başlama ipucu.</summary>
    public static string GetHint() => Loc.T("match.restart_hint");
}
