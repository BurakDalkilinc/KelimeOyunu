using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Haritadaki bütün duvarlar. Duvar ekler, zamanını ilerletir, ömrü bitenleri kaldırır ve hareket eden şeyleri
/// duvarların dışına iter (<see cref="IObstacleField"/>). Düz C# sınıfı.
/// </summary>
public class WallField : IObstacleField
{
    readonly List<WallSegment> walls = new List<WallSegment>();

    /// <summary>Şu an ayakta olan duvarlar.</summary>
    public IReadOnlyList<WallSegment> Walls => walls;

    /// <summary>Yeni bir duvar eklendiğinde çağrılır (ekranda çizmek için).</summary>
    public event Action<WallSegment> Added;

    /// <summary>Duvar ekler ve aynı duvarı döndürür. Ömrü zaten bitmiş duvar eklenmez.</summary>
    public WallSegment Add(WallSegment wall)
    {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        if (wall.IsExpired) return wall;

        walls.Add(wall);
        Added?.Invoke(wall);
        return wall;
    }

    /// <summary>Zamanı ilerletir ve ömrü biten duvarları kaldırır.</summary>
    public void Tick(float deltaSeconds)
    {
        for (int i = walls.Count - 1; i >= 0; i--)
        {
            walls[i].Tick(deltaSeconds);
            if (walls[i].IsExpired) walls.RemoveAt(i);
        }
    }

    /// <summary>
    /// Noktayı bütün duvarların dışına iter. Birden fazla duvar bir köşede buluşabildiği için iki tur yapılır.
    /// </summary>
    public Vector2 Resolve(Vector2 position, float radius)
    {
        if (walls.Count == 0) return position;

        for (int pass = 0; pass < 2; pass++)
        {
            bool moved = false;
            foreach (WallSegment wall in walls)
            {
                Vector2 pushed = wall.PushOut(position, radius);
                if (pushed != position)
                {
                    position = pushed;
                    moved = true;
                }
            }
            if (!moved) break;
        }
        return position;
    }
}
