using System;

/// <summary>
/// Bu bilgisayardaki oyuncunun görünen adı (bağlanınca host'a bildirilir). Steam modülü açılışta
/// <see cref="Provider"/>'a Steam adını veren bir işlev koyar; Steam yoksa (editör, yerel ağ) <see cref="Override"/>
/// ya da boş metin döner ve host adı "Oyuncu N" yapar. Burada temizlik yapılmaz: host her gelen adı kendisi temizler.
/// </summary>
public static class LocalPlayerName
{
    /// <summary>Adı veren işlev (ör. Steam persona adı). Null ya da boş dönerse <see cref="Override"/> kullanılır.</summary>
    public static Func<string> Provider { get; set; }

    /// <summary>Elle verilen ad (ileride ayarlar ekranı için). Boşsa kullanılmaz.</summary>
    public static string Override { get; set; }

    /// <summary>Bildirilecek ad; bilinmiyorsa boş metin.</summary>
    public static string Get()
    {
        string name = null;
        try { name = Provider?.Invoke(); }
        catch (Exception) { name = null; } // Steam kapalıysa ad sorgusu hata verebilir; ad yine de boş kalır
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return string.IsNullOrWhiteSpace(Override) ? "" : Override;
    }
}
