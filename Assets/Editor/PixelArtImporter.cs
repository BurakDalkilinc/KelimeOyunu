using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/Sprites içindeki PNG'lere piksel sanat ayarlarını otomatik uygular:
/// Sprite, 16 piksel = 1 birim, Point filtre, sıkıştırma yok, mipmap yok.
/// Yeni bir PNG eklediğinde elle ayar yapman gerekmez.
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Sprites/";

    /// <summary>Katmanlı (sprite stacking) nesnelerin dilim şeritleri; aynı piksel ayarlarıyla alınır, dilimler kodda kesilir.</summary>
    const string StackFolder = "Assets/Resources/Stacks/";
    const float PixelsPerUnit = 16f;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder) && !assetPath.StartsWith(StackFolder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Repeat; // Zemin/duvar karoları döşenebilsin

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
    }
}
