using System;
using System.Collections.Generic;

/// <summary>
/// Botun hangi kelimeyi yazacağını seçer: bu maçta henüz kullanılmamış kelimelerden rastgele biri.
/// Hepsi kullanıldıysa bilinmeyen bir kelime yazar (zararsız "???" yaratığı çıkar, tur yanmaz).
/// Düz C# sınıfı; rastgelelik seed'li <see cref="Random"/>'dan gelir.
/// </summary>
public static class BotWordChooser
{
    /// <summary>Kullanılmamış kelime kalmayınca yazılan, veritabanında olmayan kelime.</summary>
    public const string FallbackUnknownWord = "bilinmeyen";

    /// <summary>Yazılacak metni döndürür (kelimenin görünen adı).</summary>
    public static string Choose(IReadOnlyList<WordEntry> words, Func<string, bool> isUsed, Random random)
    {
        if (words == null) throw new ArgumentNullException(nameof(words));
        if (isUsed == null) throw new ArgumentNullException(nameof(isUsed));
        if (random == null) throw new ArgumentNullException(nameof(random));

        var available = new List<WordEntry>();
        foreach (WordEntry word in words)
            if (word != null && !isUsed(word.id)) available.Add(word);

        if (available.Count == 0) return FallbackUnknownWord;

        WordEntry chosen = available[random.Next(available.Count)];
        return string.IsNullOrWhiteSpace(chosen.display) ? chosen.id : chosen.display;
    }
}
