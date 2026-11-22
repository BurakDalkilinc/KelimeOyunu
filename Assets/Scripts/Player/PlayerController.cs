using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Oyuncuyu WASD (ve ok tuşları) ile hareket ettirir, arenanın içinde tutar.
/// Canı <see cref="Health"/> sınıfı tutar. Yazı kutusu açıkken hareket kapatılabilir (<see cref="CanMove"/>).
/// </summary>
public class PlayerController : MonoBehaviour, IKnockable
{
    [SerializeField] GameConfig config;
    [SerializeField] Color playerColor = new Color(0.3f, 0.85f, 0.6f);

    ArenaBounds arena;
    readonly KnockbackMotion knockback = new KnockbackMotion();

    /// <summary>Oyuncunun maçtaki durumu. <see cref="Bind"/> çağrılana kadar null.</summary>
    public PlayerState State { get; private set; }

    /// <summary>Oyuncunun canı (bağlanmadıysa null).</summary>
    public Health Health => State?.Health;

    /// <summary>Yazı kutusu açıkken ya da oyuncu elenince false yapılır.</summary>
    public bool CanMove { get; set; } = true;

    /// <summary>Oyuncuyu maç durumuna bağlar. Maçı kuran <see cref="MatchBootstrap"/> çağırır.</summary>
    public void Bind(PlayerState state)
    {
        if (State != null) State.Health.Died -= OnDied;
        State = state;
        State.Health.Died += OnDied;
    }

    void Awake()
    {
        if (config == null) { Debug.LogError("PlayerController: GameConfig atanmamış.", this); enabled = false; return; }

        arena = new ArenaBounds(config.arenaSize);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = SpriteFactory.Circle();
            sr.color = playerColor;
            sr.sortingOrder = 10;
        }
        transform.localScale = Vector3.one * (config.playerRadius * 2f);
    }

    void OnDestroy()
    {
        if (State != null) State.Health.Died -= OnDied;
    }

    void Update()
    {
        if (State == null) return;

        // Fırlatılma, yazarken ya da hareket kilitliyken de olur.
        Vector2 pushed = knockback.Step(Time.deltaTime);
        if (pushed != Vector2.zero)
            transform.position = arena.Clamp((Vector2)transform.position + pushed, config.playerRadius);

        if (!CanMove) return;

        Vector2 input = ReadMoveInput();
        if (input == Vector2.zero) return;

        Vector2 next = (Vector2)transform.position + input * (config.playerMoveSpeed * Time.deltaTime);
        transform.position = arena.Clamp(next, config.playerRadius);
    }

    /// <summary>WASD / ok tuşlarından yön okur. Çapraz hareket de aynı hızda olur.</summary>
    static Vector2 ReadMoveInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

        return new Vector2(x, y).normalized;
    }

    /// <summary>Karakteri kısa bir süreye yayarak kaydırır (bir yaratık fırlattığında).</summary>
    public void ApplyKnockback(Vector2 distance)
    {
        if (config == null) return;
        knockback.Start(distance, config.knockbackSeconds);
    }

    void OnDied()
    {
        // Eleme: karakter durur. Hayalet modu ileride (Player/Ghost) eklenecek.
        CanMove = false;
    }
}
