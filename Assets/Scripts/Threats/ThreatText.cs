using UnityEngine;

/// <summary>
/// Tehdit alarmının ve çözüm alanlarının yazıları (seçili dilde) ve renkleri. Düz hesap, test edilebilir.
/// Ağda da istemci yazıyı kendi dilinde buradan üretir; host yalnızca kelime kimliğini ve sayıları gönderir.
/// </summary>
public static class ThreatText
{
    /// <summary>Ne yapılması gerektiğini söyleyen kısa uyarı (çözüm türüne göre).</summary>
    public static string GetHint(string counterType)
    {
        switch (counterType)
        {
            case "shelter": return Loc.T("threat.hint_shelter");
            case "tower": return Loc.T("threat.hint_tower");
            case "cave": return Loc.T("threat.hint_cave");
            case "houses": return Loc.T("threat.hint_houses");
            case ThreatSpec.ShadowsType: return Loc.T("threat.hint_shadows");
            default: return Loc.T("threat.hint_default");
        }
    }

    /// <summary>Alarm yazısı: "ATOM BOMBASI! Sığınağa gir! (5 sn)". Kalan süre 0 ya da negatifse süre yazılmaz.</summary>
    public static string GetAlarm(WordEntry word, int secondsLeft)
    {
        string name = word.LocalizedDisplay.ToUpper(Loc.Culture);
        string hint = GetHint(word.HasCounter ? word.counter.type : null);
        return secondsLeft > 0 ? Loc.F("threat.alarm", name, hint, secondsLeft) : Loc.F("threat.alarm_no_time", name, hint);
    }

    /// <summary>Çözüm alanının yazısı için tür adı.</summary>
    public static string GetZoneName(string counterType)
    {
        switch (counterType)
        {
            case "shelter": return Loc.T("zone.shelter");
            case "tower": return Loc.T("zone.tower");
            case "cave": return Loc.T("zone.cave");
            case "houses": return Loc.T("zone.houses");
            default: return Loc.T("zone.default");
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
        return Loc.F("zone.label", GetZoneName(type), occupied, capacity);
    }
}
