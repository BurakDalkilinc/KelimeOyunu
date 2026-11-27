using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Korku ipuçlarının hesabı (yalnızca görüntü, kuralı değiştirmez): yerel oyuncuya yakın tehlikeli yaratık varsa ekran kenarının
/// ne kadar kırmızı yanacağı ve ekran dışındaki kovalayan yaratığı gösteren okun ekran kenarındaki yeri. Düz C#, test edilebilir.
/// </summary>
public static class DangerSense
{
    /// <summary>Menzilin sınırındaki yaratık bile vinyeti bu kadar yakar (0-1); yaklaştıkça 1'e çıkar.</summary>
    const float EdgeIntensity = 0.35f;

    /// <summary>
    /// Vinyetin gücü (0-1): <paramref name="range"/> içindeki en yakın zararlı yaratığa göre (gövdesinin kenarından ölçülür).
    /// Atılmaya hazırlanan yakın yaratık her zaman 1 verir. Kimse yoksa 0.
    /// </summary>
    public static float Intensity(Vector2 local, IReadOnlyList<SensedCreature> creatures, float range)
    {
        if (creatures == null || range <= 0f) return 0f;

        float best = 0f;
        foreach (SensedCreature creature in creatures)
        {
            if (!creature.Harmful) continue;
            float gap = Mathf.Max(0f, (creature.Position - local).magnitude - creature.Radius);
            if (gap > range) continue;

            float value = creature.PounceWindup ? 1f : Mathf.Lerp(1f, EdgeIntensity, gap / range);
            if (value > best) best = value;
        }
        return best;
    }

    /// <summary>
    /// Ekran dışındaki bir noktayı gösteren okun yeri: <paramref name="viewport"/> kameranın görüş noktası (0-1; içindeyse ok yok,
    /// false). Ok, ekranın ortasından o noktaya giden çizginin ekran kenarını (<paramref name="margin"/> kadar içeride, 0-0.5) kestiği yerde durur.
    /// <paramref name="angleDegrees"/> okun yönü (sağ = 0, saat yönünün tersi pozitif, görüş noktası ekseninde).
    /// </summary>
    public static bool TryEdgeArrow(Vector2 viewport, Vector2 margin, out Vector2 edgePoint, out float angleDegrees)
    {
        edgePoint = viewport;
        angleDegrees = 0f;
        bool inside = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (inside) return false;

        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 direction = viewport - center;
        if (direction.sqrMagnitude < 0.000001f) return false;

        float halfX = Mathf.Max(0.01f, 0.5f - margin.x);
        float halfY = Mathf.Max(0.01f, 0.5f - margin.y);
        float scaleX = Mathf.Abs(direction.x) > 0.000001f ? halfX / Mathf.Abs(direction.x) : float.MaxValue;
        float scaleY = Mathf.Abs(direction.y) > 0.000001f ? halfY / Mathf.Abs(direction.y) : float.MaxValue;

        edgePoint = center + direction * Mathf.Min(scaleX, scaleY);
        angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return true;
    }

    /// <summary>Nabız: zamanla 0.55 ile 1 arasında gidip gelen çarpan (<paramref name="hz"/> saniyedeki atış).</summary>
    public static float Pulse(float time, float hz)
    {
        return 0.775f + 0.225f * Mathf.Sin(time * hz * Mathf.PI * 2f);
    }
}
