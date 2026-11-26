using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/Audio içindeki seslere içe aktarma ayarlarını otomatik uygular: hepsi mono; kısa efektler
/// (sfx_*) belleğe açılmış (Decompress On Load, gecikmesiz çalar), müzikler (music_*) diskten akışla (Streaming).
/// Yeni bir WAV eklediğinde elle ayar yapman gerekmez.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Audio/";
    const string MusicPrefix = "music_";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        importer.loadInBackground = false;

        bool music = System.IO.Path.GetFileName(assetPath).StartsWith(MusicPrefix);
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        if (music)
        {
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
        }
        else
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
        }
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
    }
}
