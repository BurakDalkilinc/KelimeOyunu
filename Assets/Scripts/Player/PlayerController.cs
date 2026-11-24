using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Oyuncuyu WASD (ve ok tuşları) ile hareket ettirir, arenanın içinde tutar.
/// Canı <see cref="Health"/> sınıfı tutar. Yazı kutusu açıkken hareket kapatılabilir (<see cref="CanMove"/>).
/// </summary>
public class PlayerController : MonoBehaviour, IKnockable, IFacing
{
    [SerializeField] GameConfig config;
    [SerializeField] Color playerColor = new Color(0.3f, 0.85f, 0.6f);

    ArenaBounds arena;
    SpriteRenderer spriteRenderer;
    readonly KnockbackMotion knockback = new KnockbackMotion();

    /// <summary>İtiş için maç bağlantısı (<see cref="MatchBootstrap"/> atar).</summary>
    public MatchBootstrap Bootstrap { get; set; }

    PressEdge shoveEdge;

    /// <summary>Oyuncunun maçtaki durumu. <see cref="Bind"/> çağrılana kadar null.</summary>
    public PlayerState State { get; private set; }

    /// <summary>Oyuncunun canı (bağlanmadıysa null).</summary>
    public Health Health => State?.Health;

    /// <summary>İçinden geçilemeyen engeller (duvarlar). Uçarken (kanat) engeller yok sayılır.</summary>
    public IObstacleField Obstacles { get; set; }

    /// <summary>Son yürüdüğü yön (duvar önünde belirsin diye). Henüz yürümediyse yukarı.</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.up;

    /// <summary>Yazı kutusu açıkken ya da oyuncu elenince false yapılır.</summary>
    public bool CanMove { get; set; } = true;

    /// <summary>Oyuncuyu maç durumuna bağlar. Maçı kuran <see cref="MatchBootstrap"/> çağırır.</summary>
    public void Bind(PlayerState state)
    {
        if (State != null) State.Health.Died -= OnDied;
        State = state;
        State.Health.Died += OnDied;
        GoreEmitter.Attach(gameObject, () => State != null ? State.Health.Current : 0,
                           () => State != null && State.Falling != FallKind.None); // Vurulunca kan, ölünce parçalanma
    }

    void Awake()
    {
        if (config == null) { Debug.LogError("PlayerController: GameConfig atanmamış.", this); enabled = false; return; }

        arena = new ArenaBounds(config.arenaSize);

        spriteRenderer = GetComponent<SpriteRenderer>();
        var sr = spriteRenderer;
        if (sr != null)
        {
            sr.sprite = SpriteLibrary.Get("char") ?? SpriteFactory.Circle();
            sr.color = playerColor;
            Sprite[] frames = SpriteLibrary.GetFrames("char");
            if (frames.Length >= 2)
            {
                var animator = sr.gameObject.GetComponent<SpriteAnimator>();
                if (animator == null) animator = sr.gameObject.AddComponent<SpriteAnimator>();
                animator.SetFrames(frames);
            }
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

        Step();
        ResolveObstacles();
        UpdateShove();
    }

    /// <summary>Boşluk: itiş. Yazı kutusu açıkken (CanMove kapalı) ya da elenince çalışmaz.</summary>
    void UpdateShove()
    {
        var keyboard = Keyboard.current;
        bool pressed = shoveEdge.Update(keyboard != null && keyboard.spaceKey.isPressed);

        if (pressed && CanMove && State.IsAlive && Bootstrap != null) Bootstrap.TryShove(State.Id, false);
    }

    /// <summary>
    /// Karakteri verilen miktar kadar oynatır: arenanın içinde kalır ve (uçmuyorsa) duvarların içinden geçmez.
    /// Hareket alt adımlara bölünür, böylece ağır bir karede bile duvar atlanmaz.
    /// </summary>
    void MoveBy(Vector2 delta)
    {
        Vector2 from = transform.position;
        IObstacleField obstacles = MovementRules.EffectiveObstacles(State, Obstacles);
        transform.position = ObstacleMover.Move(from, from + delta, config.playerRadius, arena, config.playerRadius, obstacles);
    }

    /// <summary>İtilen karakter yolda lav/uçuruma değdiyse orada durur (üstünden atlayıp geçmez).</summary>
    void LandOnHazard(Vector2 before)
    {
        TerrainSim terrain = Bootstrap != null ? Bootstrap.Terrain : null;
        if (terrain == null || State.Status.IsFlying) return;

        Vector2 landed = terrain.FirstHazardOnPath(before, transform.position, out bool hit);
        if (!hit) return;
        transform.position = new Vector3(landed.x, landed.y, transform.position.z);
        knockback.Cancel();
        terrain.TryStartFall(State, landed); // Lava/uçurumda kaldıysa düşme animasyonu, sonra ölüm
    }

    FallAnimation fall;

    /// <summary>Karakter bir duvarın içine girmişse (duvar tam üstüne çıktı ya da fırlatıldı) dışarı iter.</summary>
    void ResolveObstacles()
    {
        IObstacleField obstacles = MovementRules.EffectiveObstacles(State, Obstacles);
        if (obstacles == null) return;

        Vector2 position = transform.position;
        Vector2 resolved = obstacles.Resolve(position, config.playerRadius);
        if (resolved != position)
            transform.position = arena.Clamp(resolved, config.playerRadius);
    }

    void Step()
    {
        State.Status.Tick(Time.deltaTime);
        UpdateTint();

        // Fırlatılma, yazarken ya da hareket kilitliyken de olur.
        Vector2 pushed = knockback.Step(Time.deltaTime);
        if (pushed != Vector2.zero)
        {
            Vector2 before = transform.position;
            MoveBy(pushed);
            LandOnHazard(before);
        }

        // Mıknatıs çekiyorsa karakter noktaya doğru kayar; kendi hızıyla ters yönde yürüyerek direnebilir.
        Vector2 pulled = State.Status.GetPullStep(transform.position, Time.deltaTime);
        if (pulled != Vector2.zero) MoveBy(pulled);

        if (fall == null) fall = FallAnimation.Attach(gameObject, config.fallSeconds);
        fall.Kind = State.Falling;
        FlightVisual.Attach(gameObject).Flying = State.IsAlive && State.Status.IsFlying; // Uçarken gölge ve alev
        DisguiseVisual.Attach(gameObject).SpriteName = State.IsAlive ? State.Status.StealthDisguise : null; // Kamuflaj: çalı
        if (!CanMove || State.Falling != FallKind.None) return;

        Vector2 input = ReadMoveInput();
        if (input == Vector2.zero) return;

        FacingDirection = input;
        float speed = config.playerMoveSpeed * State.Status.SpeedMultiplier; // Yavaşlatılmışsa daha yavaş
        MoveBy(input * (speed * Time.deltaTime));
    }

    /// <summary>Üzerindeki etkilere göre renk: yavaş mavimsi, kalkanlı camgöbeği, uçan açık, görünmez soluk.</summary>
    void UpdateTint()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = State.IsGhost ? StatusTint.Ghost(playerColor) : StatusTint.Apply(playerColor, State.Status);
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
        // Eleme: oyuncu hayalet olur. Yarı saydam görünür, WASD ile arenada gezip maçı izlemeye devam eder;
        // duvarlar, yaratıklar ve tehditler ona işlemez. Üzerindeki etkiler (yavaşlama, kalkan...) kalkar.
        State.Status.Clear();
        CanMove = true;
    }
}
