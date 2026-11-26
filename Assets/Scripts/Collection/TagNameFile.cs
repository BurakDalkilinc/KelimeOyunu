using System;

/// <summary>tag_names.json dosyasının yapısı (Resources/Data/tag_names.json).</summary>
[Serializable]
public class TagNameFile
{
    public int version;
    public TagNameRecord[] tags;
}
