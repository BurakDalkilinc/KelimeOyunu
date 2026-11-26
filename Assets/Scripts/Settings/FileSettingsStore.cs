using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Ayar kaydını diskteki bir dosyada tutar (oyunda Application.persistentDataPath/settings.json).
/// Önce geçici dosyaya yazar, sonra yerine koyar; yazarken oyun kapanırsa eski kayıt bozulmaz.
/// </summary>
public class FileSettingsStore : ISettingsStore
{
    readonly string path;

    public FileSettingsStore(string path)
    {
        this.path = path;
    }

    /// <summary>Dosyanın tam yolu.</summary>
    public string FilePath => path;

    public string Read()
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch (Exception exception)
        {
            Debug.LogWarning("Ayar dosyası okunamadı: " + exception.Message);
            return null;
        }
    }

    public bool Write(string text)
    {
        try
        {
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            string temp = path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Ayar dosyası yazılamadı: " + exception.Message);
            return false;
        }
    }
}
