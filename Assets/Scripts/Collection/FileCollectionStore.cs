using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Defter kaydını diskteki bir dosyada tutar (oyunda Application.persistentDataPath/collection.json).
/// Önce geçici dosyaya yazar, sonra yerine koyar; yazarken oyun kapanırsa eski kayıt bozulmaz.
/// </summary>
public class FileCollectionStore : ICollectionStore
{
    readonly string path;

    public FileCollectionStore(string path)
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
            Debug.LogWarning("Koleksiyon dosyası okunamadı: " + exception.Message);
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
            Debug.LogWarning("Koleksiyon dosyası yazılamadı: " + exception.Message);
            return false;
        }
    }
}
