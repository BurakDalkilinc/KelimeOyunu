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

    /// <summary>
    /// Botun kendi isteğiyle yürürken uyduğu engeller: <see cref="Obstacles"/> + lav, uçurum, yanan kareler. Böylece bot oralara girmez,
    /// ama itilir ya da fırlatılırsa (<see cref="Obstacles"/> geçerli) girebilir.
    /// </summary>
    public IObstacleField VoluntaryObstacles { get; set; }

    /// <summary>İtiş için maç bağlantısı (<see cref="MatchBootstrap"/> atar).</summary>
    public MatchBootstrap Bootstrap { get; set; }

    float shoveThinkTimer;
    CreatureModel attackingBoss;
    bool huntingWithMelee;
    System.Random shoveRandom;

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
        spriteRenderer.sprite = SpriteLibrary.Get("char") ?? SpriteFactory.Circle();
        Sprite[] frames = SpriteLibrary.GetFrames("char");
        if (frames.Length >= 2)
        {
            var animator = gameObject.GetComponent<SpriteAnimator>();
            if (animator == null) animator = gameObject.AddComponent<SpriteAnimator>();
            animator.SetFrames(frames);
        }
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 5;
        transform.localScale = Vector3.one * (config.playerRadius * 2f);

        State.Health.Died += OnDied;
        GoreEmitter.Attach(gameObject, () => State.Health.Current, () => State.Falling); // Kan ve parçalanma
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
        ThinkShove(Time.deltaTime);
    }

    /// <summary>
    /// Bot dörtte bir saniyede bir düşünür: önünde biri varsa (ve bekleme süresi dolmuşsa) belli bir olasılıkla itiş atar.
    /// Önünde kimse yokken hak harcamaz.
    /// </summary>
    void ThinkShove(float dt)
    {
        shoveThinkTimer += dt;
        if (shoveThinkTimer < 0.25f) return;
        shoveThinkTimer = 0f;

        if (Bootstrap == null) return;
        if (shoveRandom == null) shoveRandom = new System.Random(State.Id * 31 + 7);

        // Boss'a saldırıyorsa her düşünüşte vurmayı dener (önünde boss yoksa hak harcanmaz).
        if (attackingBoss != null && !attackingBoss.IsFinished) { Bootstrap.TryShove(State.Id, true); return; }
        if (huntingWithMelee) { Bootstrap.TryShove(State.Id, true); return; } // Kılıç/sopa: kurban önündeyse vurur

        if (shoveRandom.NextDouble() < config.botShoveChancePerSecond * 0.25f)
            Bootstrap.TryShove(State.Id, true);
    }

    /// <summary>
    /// Botu verilen miktar kadar oynatır: arenanın içinde kalır ve (uçmuyorsa) duvarların içinden geçmez.
    /// Hareket alt adımlara bölünür, böylece ağır bir karede bile duvar atlanmaz.
    /// </summary>
    void MoveBy(Vector2 delta, bool voluntary = false)
    {
        Vector2 from = transform.position;
        IObstacleField obstacles = MovementRules.EffectiveObstacles(State, voluntary && VoluntaryObstacles != null ? VoluntaryObstacles : Obstacles);
        transform.position = ObstacleMover.Move(from, from + delta, config.playerRadius, arena, config.playerRadius, obstacles);
    }

    /// <summary>İtilen bot yolda lav/uçuruma değdiyse orada durur (üstünden atlayıp geçmez).</summary>
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

        // Lava/uçuruma düşüyor: animasyon oynar, hareket edemez.
        if (fall == null) fall = FallAnimation.Attach(gameObject, config.fallSeconds);
        fall.Kind = State.Falling;
        FlightVisual.Attach(gameObject).Flying = State.Status.IsFlying; // Uçarken gölge ve alev
        DisguiseVisual.Attach(gameObject).SpriteName = State.Status.StealthDisguise; // Kamuflaj: çalı
        if (State.Falling != FallKind.None) return;

        Vector2 pushed = knockback.Step(Time.deltaTime);
        if (pushed != Vector2.zero)
        {
            Vector2 before = transform.position;
            MoveBy(pushed);
            LandOnHazard(before);
            if (State.Falling != FallKind.None) { fall.Kind = State.Falling; return; } // Düştü: kımıldamaz
        }

        // Savrulurken bot kendi yürümez. Yürüseydi "lav/uçuruma girme" engeli onu her karede kenara geri iterdi
        // ve itilen bot kenarda çakılı kalırdı.
        if (knockback.IsActive) return;

        Vector2 pulled = State.Status.GetPullStep(transform.position, Time.deltaTime);
        if (pulled != Vector2.zero) MoveBy(pulled);

        // Araç süren bot en yakın rakibin üstüne sürer (ezmek için); kimse yoksa düz gider.
        VehicleVisual vehicleVisual = VehicleVisual.Attach(gameObject);
        vehicleVisual.SpriteName = State.Status.IsDriving ? SpriteLibrary.NameFor(State.Status.Vehicle) : null;
        if (State.Status.IsDriving && Bootstrap != null)
        {
            vehicleVisual.WorldSize = VehicleRules.BodyRadius(State.Status.Vehicle, config) * 2f;
            Vector2 here = transform.position;
            Vector2 steer = MeleeRules.TryFindPrey(State, here, Bootstrap, 60f, out Vector2 victimAt) ? (victimAt - here).normalized : Vector2.zero;
            Vector2 heading = FacingDirection;
            Vector2 moved = Bootstrap.DriveVehicle(State, here, ref heading, steer, Time.deltaTime, MovementRules.EffectiveObstacles(State, Obstacles));
            FacingDirection = heading;
            transform.position = new Vector3(moved.x, moved.y, transform.position.z);
            LandOnHazard(here);
            return;
        }

        Vector2? refuge = null;
        if (Refuge != null && Refuge.TryGetRefuge(State, transform.position, out Vector2 refugePoint))
            refuge = refugePoint;

        bool sheltered = Refuge != null && Refuge.IsSheltered(State);

        // Gezinme hedefi lav/uçurum/duvar (ve kenarları) olmasın: bot oraya doğru yürüyüp takılmasın.
        TerrainSim terrain = Bootstrap != null ? Bootstrap.Terrain : null;
        if (terrain != null && brain.IsBadTarget == null) brain.IsBadTarget = terrain.IsBadWanderTarget;

        // Büyük tehdit yoksa ve canı yetiyorsa yakındaki boss'a saldırır (yüzü boss'a döner, Boşluk'la vurur).
        attackingBoss = refuge.HasValue || sheltered || Bootstrap == null || Bootstrap.CreatureSource == null ? null
            : BossRules.PickBossToAttack(Bootstrap.CreatureSource(), State, transform.position, config);

        Vector2 direction;

        // Elinde kılıç/sopa varsa en yakın rakibi kovalar ve yanına varınca vurur.
        huntingWithMelee = false;
        if (attackingBoss == null && State.Status.HasWeapon && MeleeRules.IsMelee(State.Status.Weapon) && refuge == null && !sheltered
            && MeleeRules.TryFindPrey(State, transform.position, Bootstrap, config.botBossAggroRange, out Vector2 prey))
        {
            huntingWithMelee = true;
            direction = brain.ThinkBossAttack(transform.position, dangers, prey, 0f, out Vector2 faceToPrey);
            FacingDirection = faceToPrey;
            if (direction != Vector2.zero)
                MoveBy(direction * (config.botMoveSpeed * State.Status.SpeedMultiplier * Time.deltaTime), voluntary: true);
            return;
        }

        if (attackingBoss != null)
        {
            direction = brain.ThinkBossAttack(transform.position, dangers, attackingBoss.Position, attackingBoss.Radius, out Vector2 face);
            FacingDirection = face;
            if (direction != Vector2.zero)
                MoveBy(direction * (config.botMoveSpeed * State.Status.SpeedMultiplier * Time.deltaTime), voluntary: true);
            return;
        }

        direction = brain.Think(Time.deltaTime, transform.position, dangers, refuge, sheltered);
        if (direction == Vector2.zero) return;

        FacingDirection = direction;
        MoveBy(direction * (config.botMoveSpeed * State.Status.SpeedMultiplier * Time.deltaTime), voluntary: true);
    }

    /// <summary>Botu kısa bir süreye yayarak kaydırır (bir yaratık fırlattığında).</summary>
    public void ApplyKnockback(Vector2 distance)
    {
        if (config == null) return;
        knockback.Start(distance, config.knockbackSeconds);
    }

    void OnDied()
    {
        // Elenen bot araçtan iner, silahı düşer, etkileri kalkar (cesedinin altında araba kalmasın).
        State.Status.Clear();
        var vehicleVisual = GetComponent<VehicleVisual>();
        if (vehicleVisual != null) vehicleVisual.SpriteName = null;
        var flight = GetComponent<FlightVisual>();
        if (flight != null) flight.Flying = false;
        var disguise = GetComponent<DisguiseVisual>();
        if (disguise != null) disguise.SpriteName = null;

        // Eleme: bot durur ve soluklaşır. Hayalet modu ileride (Player/Ghost) eklenecek.
        if (spriteRenderer != null)
        {
            Color faded = baseColor;
            faded.a = 0.25f;
            spriteRenderer.color = faded;
        }
    }
}
