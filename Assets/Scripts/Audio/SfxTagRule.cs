using System;

/// <summary>sfx_tags.json'daki tek eşleme: bu etiketi taşıyan kelime oynanınca bu ses çalar.</summary>
[Serializable]
public class SfxTagRule
{
    public string tag;
    public string sound;
}
