using System;
using System.Collections.Generic;

/// <summary>
/// collection.json dosyasının yapısı: sürüm ve kelime sayaçları. Dosya biçimi değişirse <see cref="version"/> artırılır.
/// </summary>
[Serializable]
public class CollectionSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public List<CollectionEntry> entries = new List<CollectionEntry>();
}
