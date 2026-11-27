using System.Collections.Generic;

/// <summary>
/// Bu bilgisayardaki oyuncunun sol üst göstergede çizilecek bilgileri (can, itiş, etkiler, seçilme şansı). Tur kaynağı
/// (<see cref="TurnViewSource.FillHud"/>) her karede doldurur; gösterge yalnızca okur. Tek kişilikte ve ağda aynı biçim.
/// </summary>
public class HudSnapshot
{
    /// <summary>Yerel oyuncu belli mi? Değilse gösterge çizilmez.</summary>
    public bool HasPlayer;

    /// <summary>Hayatta mı? Değilse kalpler yerine "Hayalet" çıkar.</summary>
    public bool Alive;

    public int Health;
    public int MaxHealth;

    /// <summary>İtiş göstergesi gösterilsin mi (ağda maç başlamadan gösterilmez)?</summary>
    public bool ShowShove;

    /// <summary>İtişin kalan bekleme süresi (sn); 0 = hazır.</summary>
    public float ShoveLeft;

    /// <summary>Elde silah var mı? Varsa Boşluk tuşu itiş yerine ateş eder; simgesi değişir.</summary>
    public bool Armed;

    /// <summary>Silahın kendi resmi (bilinmiyorsa null).</summary>
    public string WeaponSprite;

    /// <summary>Kalan atış/vuruş (ağda bilinmez: -1).</summary>
    public int WeaponShots = -1;

    /// <summary>Aktif etkiler (sırasız; çizerken <see cref="HudRules.SortByPriority"/> ile dizilir).</summary>
    public readonly List<HudStatus> Statuses = new List<HudStatus>();

    /// <summary>"Seçilme şansın: %12" gibi küçük alt satır; yoksa boş.</summary>
    public string ChanceText = string.Empty;

    /// <summary>Her karede yeniden doldurmadan önce temizler.</summary>
    public void Clear()
    {
        HasPlayer = false;
        Alive = false;
        Health = 0;
        MaxHealth = 0;
        ShowShove = false;
        ShoveLeft = 0f;
        Armed = false;
        WeaponSprite = null;
        WeaponShots = -1;
        Statuses.Clear();
        ChanceText = string.Empty;
    }
}
