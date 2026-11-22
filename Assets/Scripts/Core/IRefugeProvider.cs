using UnityEngine;

/// <summary>
/// Tehdit varken botlara "şuraya sığın" diyen kaynak. Tehdit yöneticisi uygular: en yakın boş çözüm alanını,
/// ya da botun zaten içinde olduğu alanı verir.
/// </summary>
public interface IRefugeProvider
{
    /// <summary>Oyuncunun gitmesi gereken sığınma noktasını verir; sığınılacak yer yoksa false.</summary>
    bool TryGetRefuge(PlayerState player, Vector2 position, out Vector2 point);

    /// <summary>Oyuncu şu an bir çözüm alanında korunuyor mu? Korunan bot tehlikeden kaçmayı bırakıp orada kalır.</summary>
    bool IsSheltered(PlayerState player);
}
