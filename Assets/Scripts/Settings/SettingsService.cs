using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Bu bilgisayardaki tek ayar kaydı: ilk istenince settings.json'dan okunur, uygulanır (ses düzeyleri, ekran, VSync, dil),
/// istenince kaydedilir. İsim etiketleri gibi görüntü bileşenleri <see cref="ShowNameTags"/>'i okur; GameConfig değişmez.
/// </summary>
public static class SettingsService
{
    static GameSettings current;
    static ISettingsStore store;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        current = null;
        store = null;
    }

    /// <summary>Geçerli ayarlar; ilk çağrıda dosyadan okunur.</summary>
    public static GameSettings Current
    {
        get
        {
            if (current == null) Load();
            return current;
        }
    }

    /// <summary>İsim etiketleri gösterilsin mi (oyuncunun tercihi)?</summary>
    public static bool ShowNameTags => Current.showNameTags;

    /// <summary>Kayıt dosyasının tam yolu.</summary>
    public static string FilePath => Path.Combine(Application.persistentDataPath, GameConfigLoader.Get().settingsFileName);

    static void Load()
    {
        store = new FileSettingsStore(FilePath);
        // Kayıt yoksa (ilk açılış) dil sistemden seçilir: Türkçe sistemde Türkçe, değilse İngilizce.
        current = SettingsStorage.Load(store, Language.FromSystem(Application.systemLanguage));
        Loc.SetLanguage(current.language);
    }

    /// <summary>Dili değiştirir: yazılar hemen yeni dile geçer (<see cref="Loc.LanguageChanged"/>) ve ayar kaydedilir.</summary>
    public static void SetLanguage(string code)
    {
        string language = Language.Normalize(code);
        if (language == null) return;
        Current.language = language;
        Loc.SetLanguage(language);
        Save();
    }

    /// <summary>Ayarları diske yazar.</summary>
    public static void Save()
    {
        if (current == null) return;
        if (store == null) store = new FileSettingsStore(FilePath);
        SettingsStorage.Save(store, current);
    }

    /// <summary>Ses, görüntü ve dil ayarlarının hepsini uygular (oyun açılırken).</summary>
    public static void ApplyAll()
    {
        Loc.SetLanguage(Current.language);
        ApplyAudio();
        ApplyDisplay();
    }

    /// <summary>Ses düzeylerini uygular: ana ses dinleyicinin (AudioListener) düzeyi, müzik ve efekt ses servisinde.</summary>
    public static void ApplyAudio()
    {
        GameSettings settings = Current;
        AudioListener.volume = GameSettings.ClampVolume(settings.masterVolume);
        AudioService service = AudioService.Instance;
        if (service != null) service.SetVolumes(settings.musicVolume, settings.sfxVolume);
    }

    /// <summary>
    /// Tam ekran, çözünürlük ve VSync'i uygular. Editörde pencere boyutu değiştirilmez (Game penceresini bozmasın diye);
    /// VSync editörde de uygulanır.
    /// </summary>
    public static void ApplyDisplay()
    {
        GameSettings settings = Current;
        QualitySettings.vSyncCount = settings.vSync ? 1 : 0;
        if (Application.isEditor) return;

        Vector2Int size = ResolvedSize(settings);
        FullScreenMode mode = settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(size.x, size.y, mode);
    }

    /// <summary>Ayardaki çözünürlük; 0 ise ekranın kendi çözünürlüğü.</summary>
    public static Vector2Int ResolvedSize(GameSettings settings)
    {
        if (settings.resolutionWidth > 0 && settings.resolutionHeight > 0)
            return new Vector2Int(settings.resolutionWidth, settings.resolutionHeight);
        Resolution native = Screen.currentResolution;
        return new Vector2Int(native.width, native.height);
    }

    /// <summary>Ekranın desteklediği çözünürlükler (ayarlar ekranındaki seçim için).</summary>
    public static ResolutionList AvailableResolutions()
    {
        var sizes = new List<Vector2Int>();
        foreach (Resolution each in Screen.resolutions) sizes.Add(new Vector2Int(each.width, each.height));
        if (sizes.Count == 0) sizes.Add(new Vector2Int(Screen.width, Screen.height));
        return new ResolutionList(sizes);
    }
}
