using System.Collections.Generic;
using UnityEngine;

/// <summary>Bir itişin vurduğu karakter ve savrulacağı yön/mesafe.</summary>
public readonly struct ShoveHit
{
    public int Index { get; }
    public Vector2 Push { get; }

    public ShoveHit(int index, Vector2 push)
    {
        Index = index;
        Push = push;
    }
}

/// <summary>
/// "İtiş" (Boşluk tuşu): bakılan yönde yakındaki karakterleri güçlü bir şekilde savurur. Bu sınıf kimin etkileneceğini ve
/// ne kadar savrulacağını hesaplar; savurma hareketi mevcut fırlatma sisteminde (<see cref="KnockbackMotion"/>) olur.
/// Düz hesap, test edilebilir.
/// </summary>
public static class ShoveRules
{
    /// <summary>
    /// <paramref name="shover"/> numaralı karakterin itişinin vurduğu karakterler (<paramref name="results"/> temizlenip doldurulur).
    /// Katı olmayanlar (hayalet, uçan) etkilenmez. Vuruş alanı: <paramref name="range"/> yarıçaplı, bakılan yönün iki yanında
    /// <paramref name="halfAngleDegrees"/> derecelik bir yelpaze. Yakındakiler daha uzağa savrulur. Tam üst üste olanlar bakılan yöne gider.
    /// </summary>
    public static void Find(int shover, Vector2 facing, IReadOnlyList<Vector2> positions, IReadOnlyList<bool> solid,
                            float range, float halfAngleDegrees, float distance, List<ShoveHit> results)
    {
        results.Clear();
        if (shover < 0 || shover >= positions.Count) return;

        Vector2 forward = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        float cosLimit = Mathf.Cos(Mathf.Clamp(halfAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);
        Vector2 origin = positions[shover];

        for (int i = 0; i < positions.Count && i < solid.Count; i++)
        {
            if (i == shover || !solid[i]) continue;

            Vector2 delta = positions[i] - origin;
            float d = delta.magnitude;
            if (d > range) continue;

            Vector2 toward = d > 0.0001f ? delta / d : forward;
            if (Vector2.Dot(forward, toward) < cosLimit) continue;

            // İleri yönle hedefe doğru yönün karışımı: düz bir "ileri itiş" hissi, ama yanındakini de yana savurur.
            Vector2 direction = (forward + toward).normalized;
            float falloff = 1f - 0.35f * (range > 0f ? d / range : 0f);
            results.Add(new ShoveHit(i, direction * (distance * falloff)));
        }
    }
}
