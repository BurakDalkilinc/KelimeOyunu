using System.Globalization;
using UnityEngine;

/// <summary>
/// Tehdit alarmının ve çözüm alanlarının Türkçe yazıları ve renkleri. Düz hesap, test edilebilir.
/// </summary>
public static class ThreatText
{
    static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

    /// <summary>Ne yapılması gerektiğini söyleyen kısa uyarı (çözüm türüne göre).</summary>
    public static string GetHint(string counterType)
    {
        switch (counterType)
        {
            case "shelter": return "Sığınağa gir!";
            case "tower": return "Kuleye çık!";
            case "cave": return "Mağaraya sığınabilirsin!";
            case "houses": return "Eve sığınabilirsin!";
            case ThreatSpec.ShadowsType: return "Gölgelere dikkat!";
            default: return "Kaç!";
        }
    }

    /// <summary>Alarm yazısı: "ATOM BOMBASI! Sığınağa gir! (5 sn)". Kalan süre 0 ya da negatifse süre yazılmaz.</summary>
    public static string GetAlarm(WordEntry word, int secondsLeft)
    {
        string name = word.display.ToUpper(Turkish);
        string hint = GetHint(word.HasCounter ? word.counter.type : null);
        return secondsLeft > 0 ? $"{name}! {hint} ({secondsLeft} sn)" : $"{name}! {hint}";
    }

    /// <summary>Çözüm alanının yazısı için tür adı.</summary>
    public static string GetZoneName(string counterType)
    {
        switch (counterType)
        {
            case "shelter": return "Sığınak";
            case "tower": return "Kule";
            case "cave": return "Mağara";
            case "houses": return "Ev";
            default: return "Alan";
        }
    }

    /// <summary>Çözüm alanının rengi (yarı saydam).</summary>
    public static Color GetZoneColor(string counterType)
    {
        switch (counterType)
        {
            case "shelter": return new Color(0.35f, 0.85f, 0.45f, 0.30f);
            case "tower": return new Color(0.35f, 0.65f, 1.00f, 0.30f);
            case "cave": return new Color(0.75f, 0.55f, 0.35f, 0.32f);
            case "houses": return new Color(1.00f, 0.75f, 0.30f, 0.30f);
            default: return new Color(0.8f, 0.8f, 0.8f, 0.3f);
        }
    }

    /// <summary>Alan etiketi: "Sığınak 2/4".</summary>
    public static string GetZoneLabel(SafeZone zone)
    {
        return GetZoneLabel(zone.Type, zone.Occupants.Count, zone.Capacity);
    }

    /// <summary>Alan etiketi sayılardan: "Kule 2/3" (ağda istemci alanın kendisini değil fotoğrafını bilir).</summary>
    public static string GetZoneLabel(string type, int occupied, int capacity)
    {
        return $"{GetZoneName(type)} {occupied}/{capacity}";
    }
}
