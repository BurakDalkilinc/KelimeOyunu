using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dev dalga (ör. tsunami): arenanın rastgele bir kenarından başlayıp karşı kenara doğru kelimenin "stats.speed"
/// hızıyla ilerler. Dalganın önü bir oyuncunun üstünden geçince, o anda çözüm alanında (kule) korunmuyorsa
/// kelimenin "stats.damage" kadar hasar alır. Her oyuncu dalgadan bir kez etkilenir. Kalkan bir kez engeller.
/// Düz C# sınıfı; çizimi <see cref="WaveView"/>'dadır.
/// </summary>
public class WaveEffect : IThreatEffect
{
    /// <summary>Dalganın ön yüzünün önündeki kalınlık: görüntü için, çarpışma yalnızca ön yüze göre.</summary>
    public const float Thickness = 1.6f;

    const float DefaultSpeed = 4f;

    readonly HashSet<int> alreadyHit = new HashSet<int>();
    ThreatContext context;
    float halfExtent;
    int damage;

    public bool IsFinished { get; private set; }

    public bool NeedsWarning => true;

    /// <summary>Etkin ve henüz arenadan çıkmadı mı?</summary>
    public bool IsActive { get; private set; }

    /// <summary>Dalganın ilerlediği yön (birim vektör: sağ, sol, yukarı ya da aşağı).</summary>
    public Vector2 Direction { get; private set; }

    /// <summary>Dalganın ön yüzünün, arena merkezinden <see cref="Direction"/> boyunca konumu.</summary>
    public float Front { get; private set; }

    /// <summary>Dalganın ilerleme hızı (birim/sn).</summary>
    public float Speed { get; private set; }

    /// <summary>Dalganın vurduğu (korunmayan) oyuncu sayısı.</summary>
    public int Affected { get; private set; }

    /// <summary>Dalganın arenayı baştan sona kat edeceği toplam süre (sn).</summary>
    public float CrossingSeconds => Speed > 0f ? (2f * halfExtent + Thickness) / Speed : 0f;

    public void Begin(ThreatContext context)
    {
        this.context = context;

        WordStats stats = context.Word?.stats;
        Speed = stats != null && stats.speed > 0f ? stats.speed : DefaultSpeed;
        damage = stats != null ? Mathf.Max(1, Mathf.RoundToInt(stats.damage)) : 1;

        Direction = PickDirection(context.Random);
        halfExtent = Mathf.Abs(Direction.x) * context.Arena.HalfSize.x + Mathf.Abs(Direction.y) * context.Arena.HalfSize.y;
        Front = -halfExtent;
        IsActive = true;

        Sweep(); // Kenarda duran biri varsa ilk karede etkilenir.
    }

    public void Tick(float deltaSeconds)
    {
        if (IsFinished || !IsActive) return;

        Front += Speed * Mathf.Max(0f, deltaSeconds);
        Sweep();

        // Dalga ve kalınlığı arenanın karşı kenarını geçince biter.
        if (Front - Thickness > halfExtent)
        {
            IsActive = false;
            IsFinished = true;
        }
    }

    /// <summary>Ön yüzün geçtiği (arkada kalan) ve henüz vurulmamış oyuncuları etkiler.</summary>
    void Sweep()
    {
        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive || alreadyHit.Contains(player.Id)) continue;
            if (!context.World.TryGetPosition(player.Id, out Vector2 position)) continue;

            if (Vector2.Dot(position, Direction) > Front) continue; // Dalga henüz ulaşmadı

            alreadyHit.Add(player.Id);
            if (context.Zones.IsProtected(player.Id)) continue;   // Kuledeki kurtulur

            player.TakeDamage(damage);
            Affected++;
        }
    }

    static Vector2 PickDirection(System.Random random)
    {
        switch (random.Next(4))
        {
            case 0: return Vector2.right;
            case 1: return Vector2.left;
            case 2: return Vector2.up;
            default: return Vector2.down;
        }
    }
}
