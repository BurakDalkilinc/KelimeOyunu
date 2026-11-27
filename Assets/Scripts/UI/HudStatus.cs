/// <summary>
/// Göstergede çizilecek tek bir durum etkisi: türü, kalan süresi (bilinmiyorsa -1), küçük sayı rozeti (yoksa -1) ve
/// isteğe bağlı özel simge adı (silahta silahın kendi resmi gibi). Düz veri.
/// </summary>
public struct HudStatus
{
    public HudStatusKind Kind;

    /// <summary>Kalan süre (sn); ağda istemci bilmez, o zaman -1.</summary>
    public float TimeLeft;

    /// <summary>Rozet sayısı (kalkanın kalan bloğu, silahın kalan atışı); yoksa -1.</summary>
    public int Count;

    /// <summary>Tür simgesi bulunamazsa denenecek resim (ör. silah kelimesinin kendi resmi); yoksa null.</summary>
    public string SpriteOverride;

    public HudStatus(HudStatusKind kind, float timeLeft, int count = -1, string spriteOverride = null)
    {
        Kind = kind;
        TimeLeft = timeLeft;
        Count = count;
        SpriteOverride = spriteOverride;
    }

    /// <summary>Kalan süre biliniyor mu?</summary>
    public bool HasTime => TimeLeft >= 0f;
}
