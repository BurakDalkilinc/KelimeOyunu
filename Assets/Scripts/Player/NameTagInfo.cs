/// <summary>
/// Bir karakterin isim etiketi için bilmemiz gerekenler: adı, bu bilgisayarın oyuncusu mu, hayatta mı, gövdesi çiziliyor mu,
/// görünmez/kılıkta mı. Karakteri süren sınıf (tek kişilik ya da ağ) her kare doldurur; karar <see cref="NameTagRules"/>'dadır.
/// </summary>
public struct NameTagInfo
{
    /// <summary>Gösterilecek ad (kısaltılmamış).</summary>
    public string Name;

    /// <summary>Bu bilgisayardaki oyuncunun kendi karakteri mi?</summary>
    public bool IsLocal;

    /// <summary>Hayatta mı? (Elenen hayalet olur.)</summary>
    public bool IsAlive;

    /// <summary>Karakterin gövdesi şu an çiziliyor mu? (Başkasının hayaleti, elenmiş bot çizilmez.)</summary>
    public bool BodyShown;

    /// <summary>Görünmezlik ya da kılık (karton kutu, çalı...) etkin mi?</summary>
    public bool IsStealthed;
}
