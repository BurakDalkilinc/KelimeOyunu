using System;

/// <summary>lang_xx.json dosyasının yapısı (Resources/Data/lang_tr.json, lang_en.json). JsonUtility ile okunur.</summary>
[Serializable]
public class LocFile
{
    public int version;
    public string language;
    public LocEntry[] entries;
}
