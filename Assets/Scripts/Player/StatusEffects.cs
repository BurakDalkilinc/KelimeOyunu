using UnityEngine;

/// <summary>
/// Bir oyuncunun üzerindeki süreli etkiler: yavaşlama, çekilme, kalkan, görünmezlik, uçma.
/// Düz C# sınıfı. Süreyi karakter her karede <see cref="Tick"/> ile ilerletir, hızını
/// <see cref="SpeedMultiplier"/> ile çarpar, çekilmeyi <see cref="GetPullStep"/> ile uygular.
/// </summary>
public class StatusEffects
{
    const float MinMultiplier = 0.05f;

    float slowMultiplier = 1f;
    float slowTimeLeft;

    Vector2 pullPoint;
    float pullForce;
    float pullTimeLeft;

    int shieldCharges;
    float shieldTimeLeft;

    float stealthTimeLeft;

    float flightSpeedMultiplier = 1f;
    float flightTimeLeft;

    // ---- Durum sorguları ----

    /// <summary>Şu an yavaşlatılmış mı?</summary>
    public bool IsSlowed => slowTimeLeft > 0f;

    /// <summary>Yavaşlamanın bitmesine kalan süre (sn). Yavaşlamıyorsa 0.</summary>
    public float SlowTimeLeft => slowTimeLeft;

    /// <summary>Şu an bir noktaya çekiliyor mu?</summary>
    public bool IsPulled => pullTimeLeft > 0f;

    /// <summary>Çekmenin bitmesine kalan süre (sn). Çekilmiyorsa 0.</summary>
    public float PullTimeLeft => pullTimeLeft;

    /// <summary>Kalkanı var mı? (Bir sonraki hasarı engeller.)</summary>
    public bool HasShield => shieldCharges > 0 && shieldTimeLeft > 0f;

    /// <summary>Kalkanın bitmesine kalan süre (sn).</summary>
    public float ShieldTimeLeft => HasShield ? shieldTimeLeft : 0f;

    /// <summary>Görünmez mi? (Saldırı kelimesinin hedefi seçilemez.)</summary>
    public bool IsStealthed => stealthTimeLeft > 0f;

    /// <summary>Görünmezliğin bitmesine kalan süre (sn).</summary>
    public float StealthTimeLeft => stealthTimeLeft;

    /// <summary>Uçuyor mu? (Yerdeki yaratıkların temasından ve tuzaklardan etkilenmez.)</summary>
    public bool IsFlying => flightTimeLeft > 0f;

    /// <summary>Uçmanın bitmesine kalan süre (sn).</summary>
    public float FlightTimeLeft => flightTimeLeft;

    /// <summary>
    /// Hız çarpanı: yavaşlama ve uçma çarpanlarının çarpımı. Etki yoksa 1.
    /// </summary>
    public float SpeedMultiplier => (IsSlowed ? slowMultiplier : 1f) * (IsFlying ? flightSpeedMultiplier : 1f);

    // ---- Yavaşlama ----

    /// <summary>
    /// Yavaşlatır. İki yavaşlatma üst üste binmez: en güçlü (en küçük) çarpan geçerli olur, süre en uzun olana tazelenir.
    /// Süresi 0 ya da çarpanı 1 (yavaşlatmayan) ise bir şey yapmaz.
    /// </summary>
    public void ApplySlow(float multiplier, float seconds)
    {
        if (seconds <= 0f) return;

        multiplier = Mathf.Clamp(multiplier, MinMultiplier, 1f);
        if (multiplier >= 1f) return;

        slowMultiplier = IsSlowed ? Mathf.Min(slowMultiplier, multiplier) : multiplier;
        slowTimeLeft = Mathf.Max(slowTimeLeft, seconds);
    }

    // ---- Çekilme ----

    /// <summary>
    /// Bir noktaya doğru çekmeyi başlatır ya da tazeler. Güç saniyedeki birimdir; oyuncunun kendi hızından düşükse
    /// yürüyerek direnebilir. Güç ya da süre 0'sa bir şey yapmaz.
    /// </summary>
    public void ApplyPull(Vector2 point, float forcePerSecond, float seconds)
    {
        if (seconds <= 0f || forcePerSecond <= 0f) return;

        pullPoint = point;
        pullForce = forcePerSecond;
        pullTimeLeft = Mathf.Max(pullTimeLeft, seconds);
    }

    /// <summary>
    /// Bu karede çekmenin getireceği kayma. Çekilmiyorsa sıfır; noktayı aşmaz (noktaya varınca tam orada durur).
    /// </summary>
    public Vector2 GetPullStep(Vector2 position, float deltaSeconds)
    {
        if (!IsPulled) return Vector2.zero;

        Vector2 toPoint = pullPoint - position;
        float distance = toPoint.magnitude;
        if (distance < 0.0001f) return Vector2.zero;

        float step = Mathf.Min(pullForce * Mathf.Max(0f, deltaSeconds), distance);
        return toPoint / distance * step;
    }

    // ---- Kalkan ----

    /// <summary>
    /// Kalkan verir: sonraki <paramref name="blocks"/> hasarı engeller, en geç <paramref name="seconds"/> saniye durur.
    /// Zaten kalkanı varsa daha güçlü olan (çok hak, uzun süre) korunur; kalkanlar üst üste binmez.
    /// </summary>
    public void ApplyShield(int blocks, float seconds)
    {
        if (blocks <= 0 || seconds <= 0f) return;

        shieldCharges = Mathf.Max(HasShield ? shieldCharges : 0, blocks);
        shieldTimeLeft = Mathf.Max(HasShield ? shieldTimeLeft : 0f, seconds);
    }

    /// <summary>Kalkan varsa bir hasarı engeller (hakkı bir azalır) ve true döner; yoksa false.</summary>
    public bool TryAbsorbHit()
    {
        if (!HasShield) return false;

        shieldCharges--;
        if (shieldCharges <= 0)
        {
            shieldCharges = 0;
            shieldTimeLeft = 0f;
        }
        return true;
    }

    // ---- Görünmezlik ----

    /// <summary>Görünmez yapar (süre tazelenir, kısalmaz). Süre 0 ya da negatifse bir şey yapmaz.</summary>
    public void ApplyStealth(float seconds)
    {
        ApplyStealth(seconds, null);
    }

    /// <summary>
    /// Görünmez yapar; <paramref name="disguise"/> verilirse karakter o resme (ör. çalı) bürünür. Süre kısalmaz.
    /// </summary>
    public void ApplyStealth(float seconds, string disguise)
    {
        if (seconds <= 0f) return;
        stealthTimeLeft = Mathf.Max(stealthTimeLeft, seconds);
        stealthDisguise = string.IsNullOrEmpty(disguise) ? null : disguise;
    }

    /// <summary>Görünmezken büründüğü resmin adı (kamuflajda "cali"); kılık yoksa ya da görünmez değilse null.</summary>
    public string StealthDisguise => IsStealthed ? stealthDisguise : null;

    string stealthDisguise;

    // ---- Uçma ----

    /// <summary>
    /// Uçurur: bu süre boyunca yerdeki yaratıkların temasından etkilenmez ve hızı <paramref name="speedMultiplier"/> katı olur
    /// (1'den küçükse 1 sayılır, uçmak yavaşlatmaz).
    /// </summary>
    public void ApplyFlight(float seconds, float speedMultiplier)
    {
        if (seconds <= 0f) return;

        flightSpeedMultiplier = IsFlying
            ? Mathf.Max(flightSpeedMultiplier, Mathf.Max(1f, speedMultiplier))
            : Mathf.Max(1f, speedMultiplier);
        flightTimeLeft = Mathf.Max(flightTimeLeft, seconds);
    }

    // ---- Silah ----

    WordEntry weaponWord;
    int weaponShots;
    float weaponTimeLeft;
    float weaponShotWait;

    /// <summary>Elinde ateş edebileceği bir silah var mı?</summary>
    public bool HasWeapon => weaponWord != null && weaponShots > 0 && weaponTimeLeft > 0f;

    /// <summary>Elindeki silahın kelimesi (yoksa null).</summary>
    public WordEntry Weapon => HasWeapon ? weaponWord : null;

    /// <summary>Kalan atış sayısı.</summary>
    public int WeaponShots => HasWeapon ? weaponShots : 0;

    /// <summary>Silahın elde kalacağı süre (sn).</summary>
    public float WeaponTimeLeft => HasWeapon ? weaponTimeLeft : 0f;

    /// <summary>Silah verir (eskisinin yerine geçer).</summary>
    public void ArmWeapon(WordEntry word, int shots, float seconds)
    {
        if (word == null || shots <= 0 || seconds <= 0f) return;
        weaponWord = word;
        weaponShots = shots;
        weaponTimeLeft = seconds;
        weaponShotWait = 0f;
    }

    /// <summary>
    /// Bir atış harcar. Silah yoksa ya da iki atış arası süre dolmadıysa false. Son atışta silah biter.
    /// </summary>
    public bool TryUseWeaponShot(float shotIntervalSeconds, out WordEntry weapon)
    {
        weapon = null;
        if (!HasWeapon || weaponShotWait > 0f) return false;

        weapon = weaponWord;
        weaponShots--;
        weaponShotWait = Mathf.Max(0f, shotIntervalSeconds);
        if (weaponShots <= 0) DropWeapon();
        return true;
    }

    void DropWeapon()
    {
        weaponWord = null;
        weaponShots = 0;
        weaponTimeLeft = 0f;
        weaponShotWait = 0f;
    }

    // ---- Zaman ----

    /// <summary>Zamanı ilerletir; süre dolan etkiler biter.</summary>
    public void Tick(float deltaSeconds)
    {
        float dt = Mathf.Max(0f, deltaSeconds);

        if (slowTimeLeft > 0f)
        {
            slowTimeLeft -= dt;
            if (slowTimeLeft <= 0f)
            {
                slowTimeLeft = 0f;
                slowMultiplier = 1f;
            }
        }

        if (pullTimeLeft > 0f)
        {
            pullTimeLeft -= dt;
            if (pullTimeLeft <= 0f) pullTimeLeft = 0f;
        }

        if (shieldTimeLeft > 0f)
        {
            shieldTimeLeft -= dt;
            if (shieldTimeLeft <= 0f)
            {
                shieldTimeLeft = 0f;
                shieldCharges = 0;
            }
        }

        if (stealthTimeLeft > 0f)
        {
            stealthTimeLeft -= dt;
            if (stealthTimeLeft <= 0f) stealthTimeLeft = 0f;
        }

        if (weaponShotWait > 0f) weaponShotWait = Mathf.Max(0f, weaponShotWait - dt);
        if (weaponTimeLeft > 0f)
        {
            weaponTimeLeft -= dt;
            if (weaponTimeLeft <= 0f) DropWeapon();
        }

        if (flightTimeLeft > 0f)
        {
            flightTimeLeft -= dt;
            if (flightTimeLeft <= 0f)
            {
                flightTimeLeft = 0f;
                flightSpeedMultiplier = 1f;
            }
        }
    }

    /// <summary>Tüm etkileri hemen kaldırır.</summary>
    public void Clear()
    {
        slowTimeLeft = 0f;
        slowMultiplier = 1f;
        pullTimeLeft = 0f;
        pullForce = 0f;
        shieldTimeLeft = 0f;
        shieldCharges = 0;
        stealthTimeLeft = 0f;
        stealthDisguise = null;
        flightTimeLeft = 0f;
        flightSpeedMultiplier = 1f;
        DropWeapon();
    }
}
