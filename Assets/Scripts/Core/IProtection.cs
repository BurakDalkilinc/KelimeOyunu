/// <summary>
/// Bir tehdidin etkisinden korunan oyuncuları söyler (sığınaktakiler, kuledekiler...).
/// Gerçek uygulaması <see cref="SafeZoneField"/>; testlerde sahtesi kullanılabilir.
/// </summary>
public interface IProtection
{
    /// <summary>Oyuncu şu an bir çözüm alanının içinde ve korunuyor mu?</summary>
    bool IsProtected(int playerId);
}
