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
    public PlayerState Target { get; private set; }

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
    /// 0'dan büyükse vurduğu oyuncuyu bu kadar saniye yavaşlatır (hasarı olmasa da). "slowdown" etiketi ayarlar.
    /// </summary>
    public float SlowSeconds { get; set; }

    /// <summary>Yavaşlatılan oyuncunun hız çarpanı (0.5 = yarı hız). "slowdown" etiketi ayarlar.</summary>
    public float SlowMultiplier { get; set; } = 1f;

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

    /// <summary>"area" etiketi bunu açar: etki hedefe temasla değil, yaratığın yarıçapındaki herkese uygulanmak ister.</summary>
    public bool AreaRequested { get; set; }

    /// <summary>Alan yaratığında, alandan çıkan oyuncuda etkinin ne kadar süre daha kalacağı (sn).</summary>
    public float AreaLingerSeconds { get; set; }

    /// <summary>
    /// Sürekli alan yaratığı mı (ör. yağmur bulutu)? Hedefi yavaşça takip eder ve yarıçapındaki herkesi etkiler.
    /// Anında vuran yaratıklar zaten alan vuruşu yaptığı için bunlarda "area" bir şey değiştirmez.
    /// Alanın uygulayacağı bir etki de olmalı (şimdilik yavaşlatma); etkisiz alan yaratığı eski temas davranışında kalır.
    /// </summary>
    public bool IsArea => AreaRequested && !IsStrike && (SlowSeconds > 0f || BurnsArea);

    /// <summary>
    /// 0'dan büyükse anında vuran yaratık hedefin üstüne değil, onun bu kadar yanına yerleştirilir (birim).
    /// "explosive" etiketi ayarlar.
    /// </summary>
    public float PlacementOffset { get; set; }

    /// <summary>
    /// Çözüm alanındaki (ev, mağara...) korunanlara vurulmasını engelleyen kaynak. null ise herkese vurabilir.
    /// Sürü saldırısı gibi tehdit yaratıkları için tehdit tarafından atanır.
    /// </summary>
    public IProtection Protection { get; set; }

    /// <summary>Oyuncu bu yaratığa karşı korunuyor mu?</summary>
    bool IsProtectedFromThis(PlayerState player) => Protection != null && Protection.IsProtected(player.Id);

    /// <summary>true ise sürekli alanı ateş püskürtür: yarıçapındaki herkese aralıklarla hasar verir. "fire" etiketi açar.</summary>
    public bool BurnsArea { get; set; }

    /// <summary>
    /// Haritadaki tahtaları tutuşturur mu? Uyarı alanı vuranlar (Ateş) vurunca bir kez, sürekli ateş püskürtenler (Ejderha)
    /// yarıçaplarında sürekli tutuşturur. Yangını <see cref="TerrainSim"/> yürütür.
    /// </summary>
    public bool IgnitesTerrain { get; set; }

    /// <summary>Ateşin aynı oyuncuya iki hasarı arasındaki süre (sn). "fire" etiketi GameConfig'ten ayarlar.</summary>
    public float AreaDamageIntervalSeconds { get; set; } = 1f;

    /// <summary>true ise sürekli alan yaratığı hedefi elenince başka hayattaki bir oyuncuya geçer (ejderha); değilse kaybolur.</summary>
    public bool RetargetsWhenTargetDies { get; set; }

    /// <summary>
    /// true ise yaratık her zaman en yakınındaki oyuncuyu hedefler (hedefi ölünce de en yakına geçer). Tehdit yaratıkları
    /// (zombi sürüsü, ejderha) bunu kullanır. <see cref="Locator"/> gerekir.
    /// </summary>
    public bool TargetsNearest { get; set; }

    /// <summary>Oyuncuların konumunu veren kaynak; en yakın hedefi seçmek için. null ise hedef değişmez.</summary>
    public IPlayerLocator Locator { get; set; }

    /// <summary>Başka bir oyuncu hedeften bu kadar daha yakın olunca yaratık ona geçer (sürekli kararsız kalmasın).</summary>
    public const float NearestSwitchMargin = 1.0f;

    float areaClock;
    readonly System.Collections.Generic.Dictionary<int, float> nextBurnAt = new System.Collections.Generic.Dictionary<int, float>();

    /// <summary>true ise yaratık gökten düşer (ör. meteor): gölge olarak görünür, geri sayım yazısı gösterilmez. "sky" etiketi açar.</summary>
    public bool IsSky { get; set; }

    /// <summary>true ise ekranda fitilin kalan saniyesi yazılır (geri sayım). "explosive" etiketi açar.</summary>
    public bool ShowsCountdown { get; set; }

    /// <summary>Anında vuran yaratığın vuruşuna kalan süre (sn). Vuruş yapıldıysa ya da vuran değilse 0.</summary>
    public float StrikeWarningLeft => IsStrike && !HasStruck ? Mathf.Max(0f, strikeWarningLeft) : 0f;

    /// <summary>
    /// true ise yaratık bir tuzaktır: yerinde bekler, yarıçapına giren herkesi (hedef olsun olmasın) etkiler ve bir kez
    /// tetiklenince kaybolur. "trap" etiketi açar. Botlar tuzakları görmez, onlardan kaçmaz.
    /// </summary>
    public bool IsTrap { get; set; }

    /// <summary>
    /// true ise yaratık uçar (ör. Arı). Uçan yaratık, uçan oyunculara da vurabilir; uçmayanlar uçan oyunculara vuramaz.
    /// "flying" etiketi açar.
    /// </summary>
    public bool IsFlying { get; set; }

    /// <summary>
    /// true ise yaratık bir mıknatıstır: koşmaz, bulunduğu noktaya hedefi çeker (süresi bitene kadar).
    /// "pull" etiketi açar. Botlar için tehlike alanı değildir (etkisi yalnızca hedefedir).
    /// </summary>
    public bool IsPull { get; set; }

    /// <summary>Çekme gücü (birim/sn). "pull" etiketi kelimenin "stats.pullForce" değerinden ayarlar.</summary>
    public float PullForce { get; set; }

    /// <summary>Çekme etkisinin, mıknatıs kaybolunca ne kadar süre daha kalacağı (sn).</summary>
    public float PullLingerSeconds { get; set; }

    /// <summary>Hedefin yakınına yerleştirilen türden mi (anında vuran ya da tuzak)? Mesafeyi <see cref="PlacementOffset"/> belirler.</summary>
    public bool PlacesNearTarget => IsStrike || IsTrap;

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

    /// <summary>
    /// Yaratığın hareketini sınırlayan engeller (duvarlar). Uçan yaratıklar ve sürekli alanlar (bulut) engellerden geçer.
    /// null ise engel yoktur.
    /// </summary>
    public IObstacleField Obstacles { get; set; }

    /// <summary>Ömrünün sonunda sıkılıp gidiyor mu? (Bu sırada vurmaz.)</summary>
    public bool IsLeaving => !IsFinished && LeaveBeforeEndSeconds > 0f && LifeLeft <= LeaveBeforeEndSeconds;

    /// <summary>Vurduktan sonra geri çekiliyor mu? (Bu sırada vurmaz.)</summary>
    public bool IsRetreating => retreatLeft > 0f;

    /// <summary>
    /// Zarar verebilir mi? Anında vuran yaratık vuruşunu yaptıktan sonra artık tehlike değildir.
    /// </summary>
    public bool IsDangerous => !IsFinished && Damage > 0 && Target != null && !(IsStrike && HasStruck);

    /// <summary>
    /// Oyuncuya herhangi bir zarar ya da engel verebilir mi (hasar, fırlatma ya da yavaşlatma)? Botlar bunlardan kaçar.
    /// Anında vuran yaratık vuruşunu yaptıktan sonra artık zararlı değildir.
    /// </summary>
    public bool IsHarmful => !IsFinished && Target != null && !IsTrap && !IsPull // Botlar tuzakları ve mıknatısı görmez
                             && (Damage > 0 || KnockbackDistance > 0f || SlowSeconds > 0f)
                             && !(IsStrike && HasStruck);

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

    /// <summary>Ömrü en çok bu kadar saniyeye indirir (uzatmaz). Tek bir meteor gibi, kelimenin süresinden kısa yaşayanlar için.</summary>
    public void LimitLife(float seconds)
    {
        if (IsFinished) return;
        LifeLeft = Mathf.Min(LifeLeft, Mathf.Max(0.01f, seconds));
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
        if (IsFinished || IsStrike || IsArea || IsTrap || IsPull) return false;

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

        UpdateTarget(Locator);
        if (IsFinished) return false;

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
        bool hasEffect = Damage > 0 || KnockbackDistance > 0f || SlowSeconds > 0f; // Hasarsız ama fırlatan/yavaşlatan da etki eder.
        bool targetOutOfReach = (Target.Status.IsFlying && !IsFlying) // Uçan oyuncuya yerdeki yaratık vuramaz
                                || IsProtectedFromThis(Target);       // Evdekine (çözüm alanı) vurulamaz
        if (!touching || !hasEffect || hitCooldownLeft > 0f || targetOutOfReach) return false;

        if (Damage > 0) Target.TakeDamage(Damage);
        if (SlowSeconds > 0f) Target.Status.ApplySlow(SlowMultiplier, SlowSeconds);

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

    /// <summary>
    /// Sürekli alan yaratığının zamanını ilerletir: hedefin üstüne doğru süzülür (hızı kelimenin değeri, genelde
    /// oyuncudan yavaş, yani alandan kaçılabilir) ve yarıçapındaki hayattaki herkesi etkiler (şimdilik yavaşlatma).
    /// Etki her karede tazelenir ve alandan çıkınca <see cref="AreaLingerSeconds"/> kadar sürer.
    /// Alandaki oyuncu sayısını döndürür. Hedef elenirse yaratık kaybolur.
    /// </summary>
    public int TickArea(float deltaSeconds, IPlayerLocator world, float playerRadius)
    {
        if (!IsArea || IsFinished) return 0;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return 0;
        }

        UpdateTarget(world);
        if (IsFinished) return 0;

        if (world == null) return 0;

        areaClock += dt;

        if (Target != null && world.TryGetPosition(Target.Id, out Vector2 targetPosition))
            MoveToward(targetPosition, 0.1f, dt);

        int inside = 0;
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || !world.TryGetPosition(player.Id, out Vector2 playerPosition)) continue;
            if ((playerPosition - Position).magnitude > Radius + playerRadius) continue;
            if (IsProtectedFromThis(player)) continue; // Çözüm alanındakine (mağara) etki etmez

            inside++;
            if (SlowSeconds > 0f) player.Status.ApplySlow(SlowMultiplier, AreaLingerSeconds);
            if (BurnsArea && Damage > 0) Burn(player);
        }
        return inside;
    }

    /// <summary>
    /// Mıknatısın zamanını ilerletir: hedefi, bulunduğu noktaya doğru çekecek etkiyi her karede tazeler.
    /// Hedef elenirse ya da süre dolarsa mıknatıs kaybolur. Çekiyorsa true döner.
    /// </summary>
    public bool TickPull(float deltaSeconds)
    {
        if (!IsPull || IsFinished) return false;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return false;
        }

        if (Target == null || !Target.IsAlive)
        {
            IsFinished = true;
            return false;
        }

        Target.Status.ApplyPull(Position, PullForce, PullLingerSeconds);
        return true;
    }

    /// <summary>
    /// Tuzağın zamanını ilerletir: yarıçapına (+ oyuncu yarıçapı) giren hayattaki herkesi etkiler (hasar, yavaşlatma,
    /// fırlatma: tuzağın merkezinden dışarı) ve tek kullanımlık olduğu için kaybolur. Fırlatılacaklar
    /// <paramref name="hits"/> listesine eklenir. Tetiklenen oyuncu sayısını döndürür. Hedef elense de tuzak yerinde kalır.
    /// </summary>
    public int TickTrap(float deltaSeconds, IPlayerLocator world, float playerRadius, List<CreatureHit> hits)
    {
        if (!IsTrap || IsFinished) return 0;

        float dt = Mathf.Max(0f, deltaSeconds);
        LifeLeft -= dt;
        if (LifeLeft <= 0f)
        {
            IsFinished = true;
            return 0;
        }

        if (world == null) return 0;

        int triggered = 0;
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || !world.TryGetPosition(player.Id, out Vector2 playerPosition)) continue;
            if ((playerPosition - Position).magnitude > Radius + playerRadius) continue;

            if (player.Status.IsFlying && !IsFlying) continue; // Uçanlar tuzağa basmaz
            if (IsProtectedFromThis(player)) continue;

            triggered++;
            if (Damage > 0) player.TakeDamage(Damage);
            if (SlowSeconds > 0f) player.Status.ApplySlow(SlowMultiplier, SlowSeconds);
            if (KnockbackDistance > 0f && hits != null)
                hits.Add(new CreatureHit(player, DirectionAway(Position, playerPosition) * KnockbackDistance));
        }

        if (triggered > 0) IsFinished = true; // Tek kullanımlık
        return triggered;
    }

    /// <summary>Ateş alanındaki oyuncuya, son yanmasından bu yana yeterli süre geçtiyse hasar verir.</summary>
    void Burn(PlayerState player)
    {
        if (nextBurnAt.TryGetValue(player.Id, out float readyAt) && areaClock < readyAt) return;

        player.TakeDamage(Damage);
        nextBurnAt[player.Id] = areaClock + Mathf.Max(0.05f, AreaDamageIntervalSeconds);
    }

    /// <summary>
    /// Hedefi günceller: hedef öldüyse ve yaratık yeniden hedef seçebiliyorsa en yakına geçer (kimse yoksa kaybolur,
    /// seçemiyorsa da kaybolur). <see cref="TargetsNearest"/> ise hedeften belirgin şekilde daha yakın biri varsa ona geçer.
    /// </summary>
    void UpdateTarget(IPlayerLocator world)
    {
        if (Target == null) return;

        if (!Target.IsAlive)
        {
            PlayerState next = (RetargetsWhenTargetDies || TargetsNearest) ? NearestAlivePlayer(world) : null;
            if (next == null)
            {
                IsFinished = true;
                return;
            }
            Target = next;
            return;
        }

        if (!TargetsNearest || world == null) return;

        PlayerState nearest = NearestAlivePlayer(world);
        if (nearest == null || nearest == Target) return;
        if (!world.TryGetPosition(Target.Id, out Vector2 currentPosition) || !world.TryGetPosition(nearest.Id, out Vector2 nearestPosition)) return;

        float toCurrent = (currentPosition - Position).magnitude;
        float toNearest = (nearestPosition - Position).magnitude;
        if (toNearest + NearestSwitchMargin < toCurrent) Target = nearest;
    }

    /// <summary>Konumu bilinen, hayattaki ve görünmez olmayan en yakın oyuncu; yoksa null.</summary>
    PlayerState NearestAlivePlayer(IPlayerLocator world)
    {
        if (world == null) return null;

        PlayerState best = null;
        float bestDistance = float.MaxValue;
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || player.Status.IsStealthed || !world.TryGetPosition(player.Id, out Vector2 position)) continue;

            float distance = (position - Position).magnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }
        return best;
    }

    int Strike(IPlayerLocator world, float playerRadius, List<CreatureHit> hits)
    {
        if (world == null) return 0;

        int affected = 0;
        foreach (PlayerState player in world.Players)
        {
            if (!player.IsAlive || !world.TryGetPosition(player.Id, out Vector2 playerPosition)) continue;
            if ((playerPosition - Position).magnitude > Radius + playerRadius) continue;
            if (IsProtectedFromThis(player)) continue;

            if (Damage > 0) player.TakeDamage(Damage);
            if (SlowSeconds > 0f) player.Status.ApplySlow(SlowMultiplier, SlowSeconds);
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

        Position = Constrain(Position, Position + direction * (Speed * dt));
    }

    /// <summary>Hedefe, ona <paramref name="stopDistance"/> kadar yaklaşana dek ilerler; hedefi aşmaz.</summary>
    void MoveToward(Vector2 targetPosition, float stopDistance, float dt)
    {
        float distance = (targetPosition - Position).magnitude;
        if (distance <= stopDistance) return;

        float step = Mathf.Min(Speed * dt, distance - stopDistance);
        Position = Constrain(Position, Vector2.MoveTowards(Position, targetPosition, step));
    }

    /// <summary>Hedeften hızıyla uzaklaşır. Tam üst üstelerse sağa doğru gider.</summary>
    void MoveAway(Vector2 targetPosition, float dt)
    {
        Position = Constrain(Position, Position + DirectionAway(targetPosition, Position) * (Speed * dt));
    }

    /// <summary>Yeni konumu arenanın içinde tutar ve (uçan değilse) duvarların dışına iter.</summary>
    Vector2 Constrain(Vector2 from, Vector2 to)
    {
        IObstacleField obstacles = Obstacles != null && !IsFlying && !IsArea ? Obstacles : null;
        return ObstacleMover.Move(from, to, Radius, arena, 0f, obstacles);
    }

    /// <summary>"from" noktasından "to" noktasına doğru birim yön. Aynı noktadalarsa sağa (belirli bir yön).</summary>
    static Vector2 DirectionAway(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }
}
