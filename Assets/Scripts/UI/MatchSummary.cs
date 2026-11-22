using System.Collections.Generic;
using System.Text;

/// <summary>
/// Maç sonu ekranının yazıları: kazanan ve maçta yazılan kelimelerin listesi. Düz hesap, test edilebilir.
/// </summary>
public static class MatchSummary
{
    /// <summary>Bir satıra sığacak kelime sayısı (uzun listeler alt satıra geçer).</summary>
    public const int WordsPerLine = 6;

    /// <summary>Başlık: "Kazanan: Bot 3" ya da kimse kalmadıysa "Kazanan yok".</summary>
    public static string GetTitle(PlayerState winner)
    {
        return winner != null ? $"Kazanan: {winner.Name}" : "Kazanan yok";
    }

    /// <summary>
    /// Yazılan kelimeler, yazılma sırasıyla: "Yazılan kelimeler (3):" ve altında virgülle ayrılmış adlar
    /// (satır başına <see cref="WordsPerLine"/> kelime). Hiç kelime yazılmadıysa bunu söyler.
    /// </summary>
    public static string GetWordsText(IReadOnlyList<WordEntry> usedWords)
    {
        if (usedWords == null || usedWords.Count == 0) return "Maçta hiç kelime yazılmadı.";

        var builder = new StringBuilder();
        builder.Append("Yazılan kelimeler (").Append(usedWords.Count).Append("):");

        for (int i = 0; i < usedWords.Count; i++)
        {
            builder.Append(i % WordsPerLine == 0 ? '\n' : ' ');
            builder.Append(usedWords[i] != null && !string.IsNullOrEmpty(usedWords[i].display) ? usedWords[i].display : "?");
            if (i < usedWords.Count - 1) builder.Append(',');
        }
        return builder.ToString();
    }

    /// <summary>Yeniden başlama ipucu.</summary>
    public static string GetHint() => "Yeniden başlamak için R'ye bas";
}
