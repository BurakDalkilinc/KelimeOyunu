using System;
using System.Text;

/// <summary>
/// Yazı kutusunun içindeki metin: klavyeden gelen karakterleri ekler, geri silmeyi işler, uzunluğu sınırlar.
/// Düz C# sınıfı; ekran ve klavye bağlantısı <see cref="TypingBoxDisplay"/>'dedir.
/// Emoji gibi iki karakterlik (vekil çift) simgeler tek parça sayılır, yarım kalmaz.
/// </summary>
public class TypingBuffer
{
    readonly int maxLength;
    readonly StringBuilder text = new StringBuilder();

    public TypingBuffer(int maxLength)
    {
        if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength), "En az 1 karakter gerekir.");
        this.maxLength = maxLength;
    }

    public string Text => text.ToString();
    public int Length => text.Length;
    public bool IsEmpty => text.Length == 0;

    /// <summary>
    /// Klavyeden gelen tek bir karakteri işler: '\b' geri siler, Enter ('\r', '\n') yok sayılır
    /// (göndermeyi ekran bileşeni yapar), diğerleri eklenmeye çalışılır.
    /// </summary>
    public void Input(char c)
    {
        if (c == '\b') Backspace();
        else if (c == '\r' || c == '\n') return;
        else TryAppend(c);
    }

    /// <summary>Karakteri ekler. Kontrol karakteri, dolu kutu ya da yarım emoji ise eklemez ve false döner.</summary>
    public bool TryAppend(char c)
    {
        if (char.IsControl(c)) return false;

        // Sonda yarım kalmış bir emoji parçası varsa ve yeni karakter onu tamamlamıyorsa, yarım parçayı at.
        if (text.Length > 0 && char.IsHighSurrogate(text[text.Length - 1]) && !char.IsLowSurrogate(c))
            text.Length--;

        if (char.IsLowSurrogate(c))
        {
            // Emojinin ikinci yarısı ancak birinci yarısının hemen arkasına gelebilir.
            if (text.Length > 0 && char.IsHighSurrogate(text[text.Length - 1]))
            {
                text.Append(c);
                return true;
            }
            return false;
        }

        int needed = char.IsHighSurrogate(c) ? 2 : 1; // Emoji sığmayacaksa hiç başlama.
        if (text.Length + needed > maxLength) return false;

        text.Append(c);
        return true;
    }

    /// <summary>Son karakteri siler (emoji ise bütün olarak). Boşsa bir şey yapmaz.</summary>
    public void Backspace()
    {
        if (text.Length == 0) return;

        bool endsWithPair = text.Length >= 2
                            && char.IsLowSurrogate(text[text.Length - 1])
                            && char.IsHighSurrogate(text[text.Length - 2]);
        text.Length -= endsWithPair ? 2 : 1;
    }

    public void Clear() => text.Length = 0;
}
