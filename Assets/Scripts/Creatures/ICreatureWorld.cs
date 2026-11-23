using UnityEngine;

/// <summary>
/// Yaratıkların içinde yaşadığı dünya: oyuncuların konumlarını bilir ve oyuncuları fırlatabilir.
/// Tek kişilik sahnede <see cref="MatchBootstrap"/>, ağ maçında host'taki simülasyon bunu uygular.
/// </summary>
public interface ICreatureWorld : IPlayerLocator
{
    /// <summary>Oyuncuyu verilen mesafe kadar fırlatır (kısa bir süreye yayılarak kayar). İtilemiyorsa false.</summary>
    bool TryApplyKnockback(int playerId, Vector2 distance);
}
