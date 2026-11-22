using UnityEngine;

/// <summary>
/// Arenanın sınırlarını tutar ve bir noktayı sınırların içinde tutar.
/// Arena merkezi (0,0) noktasıdır. Düz C# sınıfı, test edilebilir.
/// </summary>
public class ArenaBounds
{
    public Vector2 Size { get; }
    public Vector2 HalfSize => Size * 0.5f;

    public ArenaBounds(Vector2 size)
    {
        Size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
    }

    /// <summary>Noktayı, kenarlardan <paramref name="margin"/> kadar içeride kalacak şekilde sıkıştırır.</summary>
    public Vector2 Clamp(Vector2 position, float margin)
    {
        float maxX = Mathf.Max(0f, HalfSize.x - margin);
        float maxY = Mathf.Max(0f, HalfSize.y - margin);
        return new Vector2(
            Mathf.Clamp(position.x, -maxX, maxX),
            Mathf.Clamp(position.y, -maxY, maxY));
    }

    /// <summary>Nokta arenanın içinde mi?</summary>
    public bool Contains(Vector2 position)
    {
        return Mathf.Abs(position.x) <= HalfSize.x && Mathf.Abs(position.y) <= HalfSize.y;
    }
}
