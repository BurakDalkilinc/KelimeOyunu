using System;

/// <summary>words.json dosyasının en üst düzey yapısı.</summary>
[Serializable]
public class WordFile
{
    public int version;
    public string language;
    /// <summary>Yasaklı kelimeler: yazılırsa reddedilir, oyuna girmez.</summary>
    public string[] blocked;
    public WordEntry[] words;
}
