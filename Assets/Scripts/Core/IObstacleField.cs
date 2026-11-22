using UnityEngine;

/// <summary>
/// Hareket eden şeyleri (oyuncu, bot, yaratık) içinden geçilemeyen engellerden (duvarlar) uzak tutan alan.
/// Gerçek uygulaması <see cref="WallField"/>; testlerde sahtesi kullanılabilir.
/// </summary>
public interface IObstacleField
{
    /// <summary>
    /// Verilen noktayı (yarıçaplı bir daire olarak) engellerin dışına iter ve düzeltilmiş noktayı verir.
    /// Engele değmiyorsa nokta aynen döner.
    /// </summary>
    Vector2 Resolve(Vector2 position, float radius);
}
