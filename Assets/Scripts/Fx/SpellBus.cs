using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kelime büyüsünün sahneyle bağlantısı (yalnızca görüntü): yaratık çizenler (tek kişilikte <see cref="CreatureSpawner"/>, ağda
/// <see cref="NetCreatureRenderer"/>) doğan her yaratığı buraya bildirir; <see cref="WordSpellFx"/> harfleri ona uçurur.
/// Kelime olayı ile yaratığın doğuşu hangi sırada gelirse gelsin eşleşsin diye son doğanlar kısa süre saklanır.
/// Oyuncu konumları da buradan sorulur (yazanın kendisine uçan koruma büyüsü için).
/// </summary>
public static class SpellBus
{
    /// <summary>Saklanan son doğuşların en fazla sayısı.</summary>
    const int MaxRecent = 32;

    /// <summary>Sahnede beliren bir yaratık.</summary>
    public struct Appearance
    {
        public string WordId;
        public Transform Visual;
        public float Time;
    }

    static readonly List<Appearance> recent = new List<Appearance>();

    /// <summary>Yeni yaratık belirdiğinde çağrılır.</summary>
    public static event Action<Appearance> Appeared;

    /// <summary>Oyuncunun dünyadaki yeri (numarasına göre); bilinmiyorsa null. Sahne kurulurken atanır.</summary>
    public static Func<int, Vector2?> PlayerPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        recent.Clear();
        Appeared = null;
        PlayerPosition = null;
    }

    /// <summary>Yaratık sahneye kondu. Kelime kimliği bilinmeyen kelimede boştur.</summary>
    public static void CreatureAppeared(string wordId, Transform visual)
    {
        if (visual == null) return;
        var appearance = new Appearance { WordId = wordId ?? string.Empty, Visual = visual, Time = UnityEngine.Time.unscaledTime };
        recent.Add(appearance);
        if (recent.Count > MaxRecent) recent.RemoveAt(0);
        Appeared?.Invoke(appearance);
    }

    /// <summary>
    /// <paramref name="since"/> anından beri doğmuş, <paramref name="wordId"/> kelimesine ait ve hâlâ sahnede olan yaratıkları ekler.
    /// </summary>
    public static void Recent(string wordId, float since, List<Transform> into)
    {
        foreach (Appearance a in recent)
            if (a.Visual != null && a.Time >= since && WordSpellRules.Matches(wordId, a.WordId)) into.Add(a.Visual);
    }

    /// <summary>Oyuncunun yeri (bilinmiyorsa null).</summary>
    public static Vector2? PositionOf(int playerId)
    {
        return PlayerPosition != null ? PlayerPosition(playerId) : null;
    }
}
