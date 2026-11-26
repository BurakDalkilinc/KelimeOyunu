using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ses adından AudioClip bulur (Resources/Audio/ad). Bir kez yükler, sonra önbellekten verir; dosya yoksa null döner
/// ve oyun sessizce devam eder (eksik ses hata değildir).
/// </summary>
public class SoundLibrary
{
    readonly string folder;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public SoundLibrary(string folder)
    {
        this.folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.TrimEnd('/') + "/";
    }

    /// <summary>Ses dosyası; ad boşsa ya da dosya yoksa null.</summary>
    public AudioClip Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (cache.TryGetValue(id, out AudioClip clip)) return clip;

        clip = Resources.Load<AudioClip>(folder + id);
        cache[id] = clip; // Bulunamadıysa da null olarak saklanır; her seferinde diske bakılmasın.
        return clip;
    }
}
