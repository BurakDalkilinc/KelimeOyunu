using System;

/// <summary>
/// Resources/Data/sfx_tags.json dosyasının biçimi. Kurallar sırayla denenir: kelimenin taşıdığı ilk eşleşen etiketin sesi çalar.
/// Yeni bir eşleme = JSON'a satır, kod değişikliği değil.
/// </summary>
[Serializable]
public class SfxTagFile
{
    public int version;
    public string threatSound;
    public string defaultSound;
    public string unknownSound;
    public SfxTagRule[] rules;
}
