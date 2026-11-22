using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global bir tehdidin haritada çıkardığı çözüm alanı (sığınak, kule, mağara, ev). İçindeki oyuncular tehditten korunur,
/// ama kapasite sınırlıdır: alanın içindeki en fazla <see cref="Capacity"/> oyuncu korunur, fazlası korunmaz.
/// Kimlerin korunacağı her karede güncellenir: içeridekiler arasında merkeze en yakın olanlar önce girer.
/// Düz C# sınıfı.
/// </summary>
public class SafeZone
{
    readonly List<int> occupants = new List<int>();

    /// <summary>Çözümün türü: "shelter", "tower", "cave", "houses".</summary>
    public string Type { get; }

    public Vector2 Position { get; }
    public float Radius { get; }
    public int Capacity { get; }
    public float LifeLeft { get; private set; }

    /// <summary>Şu an korunan oyuncuların numaraları.</summary>
    public IReadOnlyList<int> Occupants => occupants;

    /// <summary>Boş yer sayısı.</summary>
    public int Free => Mathf.Max(0, Capacity - occupants.Count);

    public bool IsExpired => LifeLeft <= 0f;

    public SafeZone(string type, Vector2 position, float radius, int capacity, float life)
    {
        Type = type;
        Position = position;
        Radius = Mathf.Max(0.1f, radius);
        Capacity = Mathf.Max(1, capacity);
        LifeLeft = Mathf.Max(0f, life);
    }

    public bool IsOccupant(int playerId) => occupants.Contains(playerId);

    /// <summary>
    /// Kimlerin içeride (ve kapasiteye sığıp) korunduğunu günceller: alandan çıkanlar ve elenenler listeden düşer,
    /// içeridekiler arasında merkeze en yakın olanlar boş yerlere girer. Oyuncu konumu bilinmiyorsa dışarıda sayılır.
    /// </summary>
    public void UpdateOccupants(IPlayerLocator world)
    {
        if (world == null) return;

        // Çıkanlar ve elenenler
        for (int i = occupants.Count - 1; i >= 0; i--)
        {
            PlayerState player = FindPlayer(world, occupants[i]);
            if (player == null || !player.IsAlive || !IsInside(world, player.Id))
                occupants.RemoveAt(i);
        }

        if (Free == 0) return;

        // Yeni girenler: içeridekiler arasında merkeze yakın olanlar önce.
        var waiting = new List<KeyValuePair<float, int>>();
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || IsOccupant(player.Id)) continue;
            if (!world.TryGetPosition(player.Id, out Vector2 position)) continue;

            float distance = (position - Position).magnitude;
            if (distance <= Radius) waiting.Add(new KeyValuePair<float, int>(distance, player.Id));
        }

        waiting.Sort((a, b) => a.Key.CompareTo(b.Key));
        foreach (KeyValuePair<float, int> entry in waiting)
        {
            if (Free == 0) break;
            occupants.Add(entry.Value);
        }
    }

    /// <summary>Zamanı ilerletir.</summary>
    public void Tick(float deltaSeconds)
    {
        if (LifeLeft > 0f) LifeLeft = Mathf.Max(0f, LifeLeft - Mathf.Max(0f, deltaSeconds));
    }

    bool IsInside(IPlayerLocator world, int playerId)
    {
        return world.TryGetPosition(playerId, out Vector2 position) && (position - Position).magnitude <= Radius;
    }

    static PlayerState FindPlayer(IPlayerLocator world, int playerId)
    {
        foreach (PlayerState player in world.Players)
            if (player.Id == playerId) return player;
        return null;
    }
}
