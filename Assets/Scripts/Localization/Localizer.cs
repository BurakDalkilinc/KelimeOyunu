using System;
using System.Collections.Generic;

/// <summary>
/// Seçili dile göre anahtardan metin verir. Eksik anahtarda önce Türkçe'ye düşer, o da yoksa anahtarın kendisini gösterir
/// ve her eksik anahtar için bir kez uyarır. Dil değişince <see cref="LanguageChanged"/> yayılır.
/// Düz C# sınıfı: tabloları dışarıdan alır, testte doğrudan kurulur. Oyun bunu <see cref="Loc"/> üzerinden kullanır.
/// </summary>
public class Localizer
{
    readonly Func<string, LocTable> loadTable;
    readonly Dictionary<string, LocTable> tables = new Dictionary<string, LocTable>();
    readonly HashSet<string> warnedKeys = new HashSet<string>();

    /// <summary>Eksik anahtar uyarısı (oyunda editör konsolu). Null ise uyarı verilmez.</summary>
    public Action<string> Warn { get; set; }

    /// <summary>Seçili dil kodu ("tr" / "en").</summary>
    public string Current { get; private set; } = Language.Fallback;

    /// <summary>Dil değişti (yeni dil kodu).</summary>
    public event Action<string> LanguageChanged;

    /// <param name="loadTable">Dil kodundan tabloyu getirir (her dil için ilk istendiğinde bir kez çağrılır).</param>
    public Localizer(Func<string, LocTable> loadTable)
    {
        this.loadTable = loadTable ?? throw new ArgumentNullException(nameof(loadTable));
    }

    /// <summary>Dili değiştirir. Desteklenmeyen kod yok sayılır. Gerçekten değiştiyse true döner ve olay yayılır.</summary>
    public bool SetLanguage(string code)
    {
        string language = Language.Normalize(code);
        if (language == null || language == Current) return false;
        Current = language;
        LanguageChanged?.Invoke(Current);
        return true;
    }

    /// <summary>Dilin tablosu (ilk istendiğinde yüklenir).</summary>
    public LocTable TableFor(string language)
    {
        string code = Language.Normalize(language) ?? Language.Fallback;
        if (!tables.TryGetValue(code, out LocTable table))
        {
            table = loadTable(code) ?? new LocTable();
            tables[code] = table;
        }
        return table;
    }

    /// <summary>Anahtarın seçili dildeki metni (düşme sırası için bkz. sınıf açıklaması).</summary>
    public string T(string key) => TextFor(Current, key);

    /// <summary>Anahtarın verilen dildeki metni; yoksa Türkçe, o da yoksa anahtarın kendisi.</summary>
    public string TextFor(string language, string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (TableFor(language).TryGet(key, out string text)) return text;

        WarnOnce(key, Language.Normalize(language) ?? Language.Fallback);
        return TableFor(Language.Fallback).TryGet(key, out text) ? text : key;
    }

    /// <summary>Biçimli metin: "Kazanan: {0}" + ad. Biçim bozuksa (eksik argüman) ham metin döner, çökmez.</summary>
    public string F(string key, params object[] args)
    {
        string format = T(key);
        if (args == null || args.Length == 0) return format;
        try { return string.Format(Language.Culture(Current), format, args); }
        catch (FormatException)
        {
            WarnOnce(key + " (biçim)", Current);
            return format;
        }
    }

    void WarnOnce(string key, string language)
    {
        if (Warn == null || !warnedKeys.Add(language + ":" + key)) return;
        Warn("Çeviri eksik: '" + key + "' (" + language + " tablosunda yok)");
    }
}
