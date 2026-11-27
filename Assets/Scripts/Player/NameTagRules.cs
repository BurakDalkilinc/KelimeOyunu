using UnityEngine;

/// <summary>
/// İsim etiketinin kuralları: isim karakterin görünürlüğünü takip eder. Görünmezlik/kılıkta, görünen bir çatının altında,
/// gövdesi çizilmeyen ya da elenmiş karakterde isim gizlenir (kendi hayaletin ve kendi görünmezliğin soluk görünür).
/// Düz hesap, test edilebilir; çizim <see cref="NameTagView"/>'dadır.
/// </summary>
public static class NameTagRules
{
    /// <summary>İsim görünsün mü? <paramref name="underRoof"/>: karakter görünen bir çatının altında (bkz. <see cref="RoofRules"/>).</summary>
    public static bool IsVisible(NameTagInfo info, bool underRoof)
    {
        if (string.IsNullOrEmpty(info.Name)) return false;
        if (!info.BodyShown) return false;                    // Karakter çizilmiyorsa adı da yok
        if (!info.IsAlive) return info.IsLocal;               // Ölünce isim kalkar; yalnızca kendi hayaletin (soluk) adını görür
        if (info.IsStealthed && !info.IsLocal) return false;  // Kılık boşa gitmesin: başkaları adını görmez
        if (underRoof) return false;                          // Çatının altındaki karakter gizli, adı da gizli
        return true;
    }

    /// <summary>
    /// İsmin rengi: kendi karakterin <paramref name="ownColor"/> (sarı), diğerleri <paramref name="otherColor"/> (beyaz).
    /// Kendi hayaletin ve görünmezken kendi adın soluk (<paramref name="faintAlpha"/>).
    /// </summary>
    public static Color ColorFor(NameTagInfo info, Color ownColor, Color otherColor, float faintAlpha)
    {
        Color color = info.IsLocal ? ownColor : otherColor;
        if (!info.IsAlive || info.IsStealthed) color.a *= Mathf.Clamp01(faintAlpha);
        return color;
    }

    /// <summary>
    /// İsmin rengi, bot rozetiyle: rozetli (ağ maçındaki) bot <paramref name="botColor"/> (gri) olur, kendi karakterin her zaman
    /// <paramref name="ownColor"/>. Soluklaşma kuralı <see cref="ColorFor(NameTagInfo, Color, Color, float)"/> ile aynı.
    /// </summary>
    public static Color ColorFor(NameTagInfo info, Color ownColor, Color otherColor, Color botColor, float faintAlpha)
    {
        Color other = info.ShowBotBadge && !info.IsLocal ? botColor : otherColor;
        return ColorFor(info, ownColor, other, faintAlpha);
    }

    /// <summary>
    /// Bot rozeti gösterilsin mi? Yalnızca ağ maçında (<paramref name="online"/>) ve ayarda açıksa. Tek kişilikte botlar
    /// takma adla oynar; orada rozet yoktur, oyuncu botla oynadığını görmez.
    /// </summary>
    public static bool ShowsBotBadge(bool isBot, bool online, bool badgeEnabled) => isBot && online && badgeEnabled;

    /// <summary>
    /// Etikette yazacak metin: kısaltılmış ad, rozetliyse arkasında boşluk ve <paramref name="badge"/> ("Bot 3 [BOT]").
    /// Rozet kısaltmaya girmez; uzun adda ad kısalır, rozet hep görünür.
    /// </summary>
    public static string Label(NameTagInfo info, string badge, int maxLength)
    {
        string name = Shorten(info.Name, maxLength);
        if (!info.ShowBotBadge || string.IsNullOrEmpty(badge) || name.Length == 0) return name;
        return name + " " + badge;
    }

    /// <summary>Uzun adı kısaltır: en çok <paramref name="maxLength"/> harf, kısaltılınca sonu nokta. Boşluklar kırpılır.</summary>
    public static string Shorten(string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string trimmed = name.Trim();
        if (maxLength < 1 || trimmed.Length <= maxLength) return trimmed;
        return trimmed.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + ".";
    }

    /// <summary>Kadroda adı bilinmeyen karakterin adı: bot "Bot N" (ilk bot 1), insan "Oyuncu N" / "Player N" (seçili dilde).</summary>
    public static string DefaultName(int playerId, bool isBot, int firstBotId)
    {
        return isBot ? "Bot " + (playerId - firstBotId + 1) : Loc.F("player.default_name", playerId);
    }
}
