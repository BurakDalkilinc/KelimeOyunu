using System;

/// <summary>
/// Resources/Data/attack_fx.json dosyasının biçimi. Kurallar sırayla denenir: yaratığın kelimesinin taşıdığı ilk eşleşen
/// etiketin izi oynar; hiçbiri yoksa dev yaratıkta <see cref="giantEffect"/>, değilse <see cref="defaultEffect"/>.
/// Yeni eşleme = JSON'a satır, kod değişikliği değil.
/// </summary>
[Serializable]
public class AttackFxFile
{
    public int version;
    public string defaultEffect;
    public string giantEffect;
    public AttackFxRule[] rules;
}
