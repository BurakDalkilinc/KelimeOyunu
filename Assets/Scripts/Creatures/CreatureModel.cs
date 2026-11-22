using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir yaratığın oyun mantığı: hedefini kovalar, değince hasar verir, süresi bitince kaybolur.
/// Düz C# sınıfı; ekrandaki görüntüsü <see cref="CreatureView"/>'dadır. Davranış farkları etiketlerden gelir
/// (bkz. <see cref="TagRegistry"/>), kelime adına özel kod yoktur.
/// İki özel hâli vardır: hedefi olmayan yaratık (bilinmeyen kelimenin "???" yaratığı) zararsızdır ve gezinir;
/// "anında vuran" yaratık (instant) koşmaz, yerinde uyarı verip alanındaki herkese vurur.
/// </summary>
public class CreatureModel
{
    readonly ArenaBounds arena;
    float hitCooldownLeft;
    float retreatLeft;
    float strikeWarningSeconds;
    float strikeWarningLeft;

    /// <summary>Yaratığı doğuran kelime; bilinmeyen kelimenin "???" yaratığında null.</summary>
    public WordEntry Word { get; }

    /// <summary>Kovaladığı oyuncu; "???" yaratığında null.</summary>
    public PlayerState Target { get; }

    public Vector2 Position { get; private set; }

    /// <summary>Saniyedeki hız (birim). Değeri kelimenin "stats.speed" değeri verir.</summary>
    public float Speed { get; set; }

    /// <summary>Vuruş başına giden can. 0 ise zararsızdır.</summary>
    public int Damage { get; set; }

    /// <summary>Temas yarıçapı (birim). Anında vuran yaratıkta vuruş alanının yarıçapıdır.</summary>
    public float Radius { get; set; }

    /// <summary>Kalan ömür (sn). 0'a inince yaratık kaybolur.</summary>
    public float LifeLeft { get; private set; }

    /// <summary>
    /// true ise süresi bitene kadar tekrar tekrar vurur (zombi gibi); false ise bir vuruştan sonra kaybolur.
    /// "undead" ve "predator" etiketleri bunu açar.
    /// </summary>
    public bool Persistent { get; set; }

    /// <summary>
    /// 0'dan büyükse yaratık her vuruştan sonra bu kadar saniye hedeften uzaklaşır, sonra tekrar saldırır
    /// ("vur-kaç"). "predator" etiketi ayarlar.
    /// </summary>
    public float RetreatAfterHitSeconds { get; set; }

    /// <summary>
    /// 0'dan büyükse yaratık ömrünün son bu kadar saniyesinde sıkılıp hedeften uzaklaşır ve vurmaz.
    /// "predator" etiketi ayarlar.
    /// </summary>
    public float LeaveBeforeEndSeconds { get; set; }

    /// <summary>
    /// 0'dan büyükse vurduğu oyuncuyu kendinden uzağa bu kadar birim fırlatır. Hasarı olmasa da fırlatır.
    /// "knockback" etiketi ayarlar.
    /// </summary>
    public float KnockbackDistance { get; set; }

    /// <summary>
    /// 0'dan büyükse yaratık "anında vuran"dır: koşmaz, bu kadar saniye uyarı verir, sonra yarıçapındaki herkese vurur.
    /// "instant" etiketi ayarlar. Değer atanınca uyarı sayacı baştan başlar.
    /// </summary>
    public float StrikeWarningSeconds
    {
        get => strikeWarningSeconds;
        set
        {
            strikeWarningSeconds = Mathf.Max(0f, value);
            strikeWarningLeft = strikeWarningSeconds;
        }
    }

    /// <summary>Anında vuran yaratık mı?</summary>
    public bool IsStrike => strikeWarningSeconds > 0f;

    /// <summary>Anında vuran yaratık vuruşunu yaptı mı?</summary>
    public bool HasStruck { get; private set; }

    /// <summary>Uyarının ne kadarı doldu (0 → 1). Vuruş yapılınca 1'dir. Anında vuran olmayanda 0.</summary>
    public float StrikeProgress
    {
        get
        {
            if (!IsStrike) return 0f;
            if (HasStruck) return 1f;
            return Mathf.Clamp01(1f - strikeWarningLeft / strikeWarningSeconds);
        }
    }

    /// <summary>En son temas vuruşunun fırlatma bilgisi (<see cref="Tick"/> true döndürdüğünde geçerlidir).</summary>
    public CreatureHit LastHit { get; private set; }

    public bool IsFinished { get; private set; }

    /// <summary>Ömrünün sonunda sıkılıp gidiyor mu? (Bu sırada vurmaz.)</summary>
    public bool IsLeaving => !IsFinished && LeaveBeforeEndSeconds > 0f && LifeLeft <= LeaveBeforeEndSeconds;

    /// <summary>Vurduktan sonra geri çekiliyor mu? (Bu sırada vurmaz.)</summary>
    public bool IsRetreating => retreatLeft > 0f;

    /// <summary>
    /// Zarar verebilir mi? Anında vuran yaratık vuruşunu yaptıktan sonra artık tehlike değildir.
    /// </summary>
    public bool IsDangerous => !IsFinished && Damage > 0 && Target != null && !(IsStrike && HasStruck);

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

    /// <summary>İlk vuruşu bu kadar saniye erteler (sürü üyelerini birbirinden ayırmak için).</summary>
    public void DelayFirstHit(float seconds)
    {
        hitCooldownLeft = Mathf.Max(hitCooldownLeft, seconds);
    }

    /// <summary>Ömrü en az bu kadar saniyeye çıkarır (kısaltmaz). Doğarken, etiketler ayarlanırken kullanılır.</summary>
    public void EnsureLifeAtLeast(float seconds)
    {
        if (IsFinished) return;
        LifeLeft = Mathf.Max(LifeLeft, seconds);
    }

    /// <summary>Yaratığı doğrudan bir noktaya koyar (arenanın içine sıkıştırılır). Anında vuran yaratığı hedefin üstüne yerleştirmek için.</summary>
    public void PlaceAt(Vector2 position)
    {
        Position = arena.Clamp(position, 0f);
    }

    /// <summary>
    /// Zamanı ilerletir: ömrü azaltır, hedefe yaklaşır (ya da uzaklaşır), değiyorsa vurur.
    /// Bu karede vurduysa true döner (fırlatma bilgisi <see cref="LastHit"/>'te). Hedef elenmişse yaratık kaybolur.
    /// Anında vuran yaratık için bunun yerine <see cref="TickStrike"/> kullanılır.
    /// </summary>
    public bool Tick(float deltaSeconds, Vector2 targetPosition, float targetRadius, float hitCooldownSeconds)
    {
        if (IsFinished || IsStrike) return false;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return false;
        }

        if (hitCooldownLeft > 0f) hitCooldownLeft -= dt;
        if (retreatLeft > 0f) retreatLeft -= dt;
        if (Target == null) return false;

        if (!Target.IsAlive)
        {
            IsFinished = true;
            return false;
        }

        // Sıkılıp giden ya da vurup geri çekilen yaratık hedeften uzaklaşır ve vurmaz.
        if (IsLeaving || IsRetreating)
        {
            MoveAway(targetPosition, dt);
            return false;
        }

        float reach = Radius + targetRadius;
        MoveToward(targetPosition, reach * 0.8f, dt);

        bool touching = (targetPosition - Position).magnitude <= reach;
        bool hasEffect = Damage > 0 || KnockbackDistance > 0f; // Hasarsız ama fırlatan yaratık da etki eder.
        if (!touching || !hasEffect || hitCooldownLeft > 0f) return false;

        if (Damage > 0) Target.Health.TakeDamage(Damage);

        Vector2 push = KnockbackDistance > 0f ? DirectionAway(Position, targetPosition) * KnockbackDistance : Vector2.zero;
        LastHit = new CreatureHit(Target, push);

        hitCooldownLeft = hitCooldownSeconds;
        if (RetreatAfterHitSeconds > 0f) retreatLeft = RetreatAfterHitSeconds;
        if (!Persistent) IsFinished = true;
        return true;
    }

    /// <summary>
    /// Anında vuran yaratığın zamanını ilerletir: uyarı sayar, dolunca alanın (yarıçap + oyuncu yarıçapı) içindeki
    /// hayattaki herkese hasar verir. Fırlatılacaklar <paramref name="hits"/> listesine eklenir.
    /// Vuran oyuncu sayısını döndürür (vuruşun yapılmadığı karelerde 0).
    /// </summary>
    public int TickStrike(float deltaSeconds, IPlayerLocator world, float playerRadius, List<CreatureHit> hits)
    {
        if (!IsStrike || IsFinished) return 0;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;

        int affected = 0;
        if (!HasStruck)
        {
            strikeWarningLeft -= dt;
            if (strikeWarningLeft <= 0f)
            {
                affected = Strike(world, playerRadius, hits);
                HasStruck = true;
            }
        }

        if (LifeLeft <= 0f) IsFinished = true;
        return affected;
    }

    int Strike(IPlayerLocator world, float playerRadius, List<CreatureHit> hits)
    {
        if (world == null) return 0;

        int affected = 0;
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || !world.TryGetPosition(player.Id, out Vector2 playerPosition)) continue;
            if ((playerPosition - Position).magnitude > Radius + playerRadius) continue;

            if (Damage > 0) player.Health.TakeDamage(Damage);
            affected++;

            if (KnockbackDistance > 0f && hits != null)
                hits.Add(new CreatureHit(player, DirectionAway(Position, playerPosition) * KnockbackDistance));
        }
        return affected;
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

    /// <summary>Hedeften hızıyla uzaklaşır. Tam üst üstelerse sağa doğru gider.</summary>
    void MoveAway(Vector2 targetPosition, float dt)
    {
        Position = arena.Clamp(Position + DirectionAway(targetPosition, Position) * (Speed * dt), 0f);
    }

    /// <summary>"from" noktasından "to" noktasına doğru birim yön. Aynı noktadalarsa sağa (belirli bir yön).</summary>
    static Vector2 DirectionAway(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }
}
