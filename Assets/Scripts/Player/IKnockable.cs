using UnityEngine;

/// <summary>Dışarıdan itilebilen sahne karakteri (oyuncu ve botlar).</summary>
public interface IKnockable
{
    /// <summary>Karakteri, kısa bir süreye yayılarak <paramref name="distance"/> kadar kaydırır (duvarda durur).</summary>
    void ApplyKnockback(Vector2 distance);
}
