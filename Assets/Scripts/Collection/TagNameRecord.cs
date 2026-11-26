using System;

/// <summary>tag_names.json içindeki tek satır: etiket kimliği, Türkçe adı ve İngilizce adı.</summary>
[Serializable]
public class TagNameRecord
{
    public string id;
    public string name;

    /// <summary>İngilizce ad; boşsa İngilizce'de de Türkçe ad gösterilir.</summary>
    public string nameEn;
}
