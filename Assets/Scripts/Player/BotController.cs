using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir botu sahnede hareket ettirir: <see cref="BotBrain"/>'in verdiği yöne gider, arenanın içinde kalır.
/// Elenince durur ve soluklaşır (hayalet modu ileride eklenecek).
/// </summary>
public class BotController : MonoBehaviour, IKnockable, IFacing
{
    GameConfig config;
    ArenaBounds arena;
    BotBrain brain;
    SpriteRenderer spriteRenderer;
    IReadOnlyList<DangerZone> dangers;
    Color baseColor;
    readonly KnockbackMotion knockback = new KnockbackMotion();

    /// <summary>Tehdit varken botun sığınacağı yeri söyleyen kaynak (yoksa bot gezinmeye devam eder).</summary>
    public IRefugeProvider Refuge { get; set; }

    /// <summary>İçinden geçilemeyen engeller (duvarlar). Uçarken (kanat) engeller yok sayılır.</summary>
    public IObstacleField Obstacles { get; set; }

    /// <summary>Son yürüdüğü yön. Henüz yürümediyse yukarı.</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.up;

    /// <summary>Bu botun maçtaki durumu (can, sıra bekleme sayacı...).</summary>
    public PlayerState State { get; private set; }

    /// <summary>Botu kurar. <paramref name="dangers"/> botların kaçacağı alanların ortak listesidir.</summary>
    public void Initialize(PlayerState state, GameConfig config, ArenaBounds arena,
                           System.Random random, IReadOnlyList<DangerZone> dangers, Color color)
    {
        State = state;
        this.config = config;
        this.arena = arena;
        this.dangers = dangers;
        baseColor = color;
        brain = new BotBrain(config, arena, random);

        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.Circle();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 5;
        transform.localScale = Vector3.one * (config.playerRadius * 2f);

        State.Health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (State != null) State.Health.Died -= OnDied;
    }

    void Update()
    {
        if (brain == null || !State.IsAlive) return;

        Step();
        ResolveObstacles();
    }

    /// <summary>
    /// Botu verilen miktar kadar oynatır: arenanın içinde kalır ve (uçmuyorsa) duvarların içinden geçmez.
    /// Hareket alt adımlara bölünür, böylece ağır bir karede bile duvar atlanmaz.
    /// </summary>
    void MoveBy(Vector2 delta)
    {
        Vector2 from = transform.position;
        IObstacleField obstacles = State.Status.IsFlying ? null : Obstacles;
        transform.position = ObstacleMover.Move(from, from + delta, config.playerRadius, arena, config.playerRadius, obstacles);
    }

    /// <summary>Bot bir duvarın içine girmişse dışarı iter.</summary>
    void ResolveObstacles()
    {
        if (Obstacles == null || State.Status.IsFlying) return;

        Vector2 position = transform.position;
        Vector2 resolved = Obstacles.Resolve(position, config.playerRadius);
        if (resolved != position)
            transform.position = arena.Clamp(resolved, config.playerRadius);
    }

    void Step()
    {
        State.Status.Tick(Time.deltaTime);
        spriteRenderer.color = StatusTint.Apply(baseColor, State.Status); // Etkilere göre renk

        Vector2 pushed = knockback.Step(Time.deltaTime);
        if (pushed != Vector2.zero) MoveBy(pushed);

        Vector2 pulled = State.Status.GetPullStep(transform.position, Time.deltaTime);
        if (pulled != Vector2.zero) MoveBy(pulled);

        Vector2? refuge = null;
        if (Refuge != null && Refuge.TryGetRefuge(State, transform.position, out Vector2 refugePoint))
            refuge = refugePoint;

        bool sheltered = Refuge != null && Refuge.IsSheltered(State);
        Vector2 direction = brain.Think(Time.deltaTime, transform.position, dangers, refuge, sheltered);
        if (direction == Vector2.zero) return;

        FacingDirection = direction;
        MoveBy(direction * (config.botMoveSpeed * State.Status.SpeedMultiplier * Time.deltaTime));
    }

    /// <summary>Botu kısa bir süreye yayarak kaydırır (bir yaratık fırlattığında).</summary>
    public void ApplyKnockback(Vector2 distance)
    {
        if (config == null) return;
        knockback.Start(distance, config.knockbackSeconds);
    }

    void OnDied()
    {
        // Eleme: bot durur ve soluklaşır. Hayalet modu ileride (Player/Ghost) eklenecek.
        if (spriteRenderer != null)
        {
            Color faded = baseColor;
            faded.a = 0.25f;
            spriteRenderer.color = faded;
        }
    }
}
