using NUnit.Framework;

/// <summary>
/// Bütün EditMode testlerinden önce dili Türkçe'ye sabitler. Metin karşılaştıran eski testler Türkçe yazılmıştır;
/// editörde Play'de İngilizce seçilip çıkılmışsa (alan yeniden yüklenmezse) dil İngilizce kalabilirdi.
/// Ad alanı olmadığı için tüm test derlemesine uygulanır.
/// </summary>
[SetUpFixture]
public class TestLanguageSetup
{
    [OneTimeSetUp]
    public void UseTurkish() => Loc.SetLanguage(Language.Turkish);
}
