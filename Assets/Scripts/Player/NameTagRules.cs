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

    /// <summary>Uzun adı kısaltır: en çok <paramref name="maxLength"/> harf, kısaltılınca sonu nokta. Boşluklar kırpılır.</summary>
    public static string Shorten(string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string trimmed = name.Trim();
        if (maxLength < 1 || trimmed.Length <= maxLength) return trimmed;
        return trimmed.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + ".";
    }

    /// <summary>Kadroda adı bilinmeyen karakterin adı: bot "Bot N" (ilk bot 1), insan "Oyuncu N".</summary>
    public static string DefaultName(int playerId, bool isBot, int firstBotId)
    {
        return isBot ? "Bot " + (playerId - firstBotId + 1) : "Oyuncu " + playerId;
    }
}
