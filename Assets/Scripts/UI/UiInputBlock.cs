/// <summary>
/// Tam ekran bir pencere (ayarlar, defter) açıkken oyun girdisi kapanır: karakter yürümez, itiş atılmaz, yazı kutusuna
/// harf gitmez, hedef seçilmez. Oyun durmaz (çok oyunculu); yalnızca tuşlar ve tıklamalar oyuna ulaşmaz.
/// </summary>
public static class UiInputBlock
{
    /// <summary>Şu an oyun girdisi kapalı mı?</summary>
    public static bool IsBlocked => SettingsScreen.IsOpen || CollectionBookScreen.IsOpen;
}
