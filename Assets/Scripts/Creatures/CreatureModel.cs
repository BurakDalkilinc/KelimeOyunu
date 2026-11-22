using UnityEngine;

/// <summary>
/// Bir yaratığın oyun mantığı: hedefini kovalar, değince hasar verir, süresi bitince kaybolur.
/// Düz C# sınıfı; ekrandaki görüntüsü <see cref="CreatureView"/>'dadır. Davranış farkları etiketlerden gelir
/// (bkz. <see cref="TagRegistry"/>), kelime adına özel kod yoktur.
/// Hedefi olmayan yaratık (bilinmeyen kelimenin "???" yaratığı) zararsızdır ve yalnızca gezinir.
/// </summary>
public class CreatureModel
{
    readonly ArenaBounds arena;
    float hitCooldownLeft;

    /// <summary>Yaratığı doğuran kelime; bilinmeyen kelimenin "???" yaratığında null.</summary>
    public WordEntry Word { get; }

    /// <summary>Kovaladığı oyuncu; "???" yaratığında null.</summary>
    public PlayerState Target { get; }

    public Vector2 Position { get; private set; }

    /// <summary>Saniyedeki hız (birim). Değeri kelimenin "stats.speed" değeri verir.</summary>
    public float Speed { get; set; }

    /// <summary>Vuruş başına giden can. 0 ise zararsızdır.</summary>
    public int Damage { get; set; }

    public float Radius { get; }

    /// <summary>Kalan ömür (sn). 0'a inince yaratık kaybolur.</summary>
    public float LifeLeft { get; private set; }

    /// <summary>
    /// true ise süresi bitene kadar tekrar tekrar vurur (zombi gibi); false ise bir vuruştan sonra kaybolur.
    /// "undead" etiketi bunu açar.
    /// </summary>
    public bool Persistent { get; set; }

    public bool IsFinished { get; private set; }

    /// <summary>Zarar verebilir mi?</summary>
    public bool IsDangerous => !IsFinished && Damage > 0 && Target != null;

    public CreatureModel(WordEntry word, PlayerState target, Vector2 position, float speed, int damage,
                         float radius, float life, ArenaBounds arena)
    {
        this.arena = arena ?? throw new System.ArgumentNullException(nameof(arena));
        Word = word;
        Target = target;
        Position = arena.Clamp(position, 0f);
        Speed = Mathf.Max(0f, speed);
        Damage = Mathf.Max(0, damage);
        Radius = Mathf.Max(0.01f, radius);
        LifeLeft = Mathf.Max(0f, life);
        IsFinished = LifeLeft <= 0f;
    }

    /// <summary>
    /// Zamanı ilerletir: ömrü azaltır, hedefe yaklaşır, değiyorsa vurur. Bu karede vurduysa true döner.
    /// Hedef elenmişse yaratık kaybolur.
    /// </summary>
    public bool Tick(float deltaSeconds, Vector2 targetPosition, float targetRadius, float hitCooldownSeconds)
    {
        if (IsFinished) return false;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return false;
        }

        if (hitCooldownLeft > 0f) hitCooldownLeft -= dt;
        if (Target == null) return false;

        if (!Target.IsAlive)
        {
            IsFinished = true;
            return false;
        }

        float reach = Radius + targetRadius;
        MoveToward(targetPosition, reach * 0.8f, dt);

        bool touching = (targetPosition - Position).magnitude <= reach;
        if (!touching || Damage <= 0 || hitCooldownLeft > 0f) return false;

        Target.Health.TakeDamage(Damage);
        hitCooldownLeft = hitCooldownSeconds;
        if (!Persistent) IsFinished = true;
        return true;
    }

    /// <summary>Hedefsiz yaratığın gezinmesi: verilen yöne (uzunluğu en çok 1) hızıyla gider; ömrü azalır.</summary>
    public void Wander(Vector2 direction, float deltaSeconds)
    {
        if (IsFinished) return;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return;
        }

        Position = arena.Clamp(Position + direction * (Speed * dt), 0f);
    }

    /// <summary>Hedefe, ona <paramref name="stopDistance"/> kadar yaklaşana dek ilerler; hedefi aşmaz.</summary>
    void MoveToward(Vector2 targetPosition, float stopDistance, float dt)
    {
        float distance = (targetPosition - Position).magnitude;
        if (distance <= stopDistance) return;

        float step = Mathf.Min(Speed * dt, distance - stopDistance);
        Position = arena.Clamp(Vector2.MoveTowards(Position, targetPosition, step), 0f);
    }
}
