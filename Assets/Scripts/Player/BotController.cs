using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir botu sahnede hareket ettirir: <see cref="BotBrain"/>'in verdiği yöne gider, arenanın içinde kalır.
/// Elenince durur ve soluklaşır (hayalet modu ileride eklenecek).
/// </summary>
public class BotController : MonoBehaviour
{
    GameConfig config;
    ArenaBounds arena;
    BotBrain brain;
    SpriteRenderer spriteRenderer;
    IReadOnlyList<DangerZone> dangers;
    Color baseColor;

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

        Vector2 direction = brain.Think(Time.deltaTime, transform.position, dangers);
        if (direction == Vector2.zero) return;

        Vector2 next = (Vector2)transform.position + direction * (config.botMoveSpeed * Time.deltaTime);
        transform.position = arena.Clamp(next, config.playerRadius);
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
