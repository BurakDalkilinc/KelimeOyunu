using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sol üst göstergenin (<see cref="PlayerHud"/>) düz hesapları: kalp dizilimi (tam/yarım/boş), tam sayı piksel ölçeği,
/// bekleme oranı, etki simgelerinin sırası, bitmek üzere olan simgenin yanıp sönmesi, kırılan kalbin parça yolu.
/// Unity sahnesine bağlı değildir, EditMode testleriyle denenir.
/// </summary>
public static class HudRules
{
    /// <summary>Simgelerin soldan sağa sırası: önce koruyanlar ve elindekiler, sonra kötü etkiler.</summary>
    public static readonly HudStatusKind[] Priority =
    {
        HudStatusKind.Shield, HudStatusKind.Weapon, HudStatusKind.Vehicle, HudStatusKind.Flight,
        HudStatusKind.Disguise, HudStatusKind.Stealth, HudStatusKind.Burn, HudStatusKind.Slow, HudStatusKind.Pull,
    };

    // ---- Kalpler ----

    /// <summary>Gereken kalp sayısı: en yüksek can kalp başına cana bölünüp yukarı yuvarlanır (en az 1).</summary>
    public static int HeartCount(int maxHealth, int healthPerHeart)
    {
        int per = Mathf.Max(1, healthPerHeart);
        return Mathf.Max(1, (Mathf.Max(1, maxHealth) + per - 1) / per);
    }

    /// <summary>
    /// <paramref name="index"/>. kalbin doluluğu. Kalp başına 1 canda kalpler yalnızca tam/boş olur; 2 ve üstünde
    /// kalbin canının en az yarısı varsa yarım, tamamı varsa tam sayılır.
    /// </summary>
    public static HeartFill HeartAt(int index, int health, int healthPerHeart)
    {
        int per = Mathf.Max(1, healthPerHeart);
        int inThisHeart = Mathf.Clamp(health - index * per, 0, per);
        if (inThisHeart >= per) return HeartFill.Full;
        if (inThisHeart <= 0) return HeartFill.Empty;
        return inThisHeart * 2 >= per ? HeartFill.Half : HeartFill.Empty;
    }

    /// <summary>Tüm kalplerin doluluğu, soldan sağa.</summary>
    public static HeartFill[] Hearts(int health, int maxHealth, int healthPerHeart)
    {
        var hearts = new HeartFill[HeartCount(maxHealth, healthPerHeart)];
        for (int i = 0; i < hearts.Length; i++) hearts[i] = HeartAt(i, health, healthPerHeart);
        return hearts;
    }

    /// <summary>Düşük canda atan kalp: dolu (tam ya da yarım) olan en sağdaki kalp; hiç dolu yoksa -1.</summary>
    public static int LastFilledHeart(HeartFill[] hearts)
    {
        if (hearts == null) return -1;
        for (int i = hearts.Length - 1; i >= 0; i--)
            if (hearts[i] != HeartFill.Empty) return i;
        return -1;
    }

    // ---- Piksel ölçeği ----

    /// <summary>
    /// Resmin bir pikselinin ekrandaki tam sayı boyu: ekran yüksekliği referans yüksekliğin kaç katıysa (aşağı yuvarlanır),
    /// en az <paramref name="min"/>, en fazla <paramref name="max"/>. Böylece pikseller hiç bulanıklaşmaz.
    /// </summary>
    public static int PixelScale(int screenHeight, int referenceHeight, int min, int max)
    {
        int lo = Mathf.Max(1, min);
        int hi = Mathf.Max(lo, max);
        if (referenceHeight <= 0) return lo;
        return Mathf.Clamp(screenHeight / referenceHeight, lo, hi);
    }

    /// <summary>Ekran pikseli değerini ölçeğin tam katına yuvarlar (sarsıntı kaydırması da pikseli bozmasın).</summary>
    public static float SnapToPixel(float value, int scale)
    {
        int s = Mathf.Max(1, scale);
        return Mathf.Round(value / s) * s;
    }

    // ---- Bekleme ve süre ----

    /// <summary>Kalan oran (0..1): toplam bilinmiyor ya da süre bitmişse 0.</summary>
    public static float CooldownFraction(float remaining, float total)
    {
        if (total <= 0f || remaining <= 0f) return 0f;
        return Mathf.Clamp01(remaining / total);
    }

    /// <summary>Simge altındaki saniye: yukarı yuvarlanır (0,2 sn kaldıysa "1").</summary>
    public static int SecondsLabel(float timeLeft) => timeLeft <= 0f ? 0 : Mathf.CeilToInt(timeLeft);

    /// <summary>
    /// Bitmek üzere olan simge görünür mü? Süresi bilinmiyorsa ya da uyarı süresinden uzunsa hep görünür;
    /// son <paramref name="warnSeconds"/> saniyede saniyede <paramref name="blinkHz"/> kez söner.
    /// </summary>
    public static bool IsBlinkVisible(float timeLeft, float warnSeconds, float time, float blinkHz)
    {
        if (timeLeft < 0f || timeLeft > warnSeconds || blinkHz <= 0f) return true;
        return Mathf.FloorToInt(time * blinkHz * 2f) % 2 == 0;
    }

    // ---- Sıra ----

    /// <summary>Türün sıradaki yeri (küçük = solda); listede yoksa en sona.</summary>
    public static int PriorityOf(HudStatusKind kind)
    {
        int index = System.Array.IndexOf(Priority, kind);
        return index < 0 ? Priority.Length : index;
    }

    /// <summary>Etkileri öncelik sırasına dizer (aynı türde gelen sırayı korur).</summary>
    public static void SortByPriority(List<HudStatus> statuses)
    {
        if (statuses == null || statuses.Count < 2) return;
        // List.Sort kararlı değil: eşitlikte eski sırayı korumak için yerleştirerek sıralanır (liste en fazla birkaç simge).
        for (int i = 1; i < statuses.Count; i++)
        {
            HudStatus item = statuses[i];
            int p = PriorityOf(item.Kind);
            int j = i - 1;
            while (j >= 0 && PriorityOf(statuses[j].Kind) > p)
            {
                statuses[j + 1] = statuses[j];
                j--;
            }
            statuses[j + 1] = item;
        }
    }

    /// <summary>Türün simge resminin adı (Resources/Sprites/ui_status_ad).</summary>
    public static string IconName(HudStatusKind kind)
    {
        switch (kind)
        {
            case HudStatusKind.Shield: return "ui_status_shield";
            case HudStatusKind.Weapon: return "ui_status_weapon";
            case HudStatusKind.Vehicle: return "ui_status_vehicle";
            case HudStatusKind.Flight: return "ui_status_fly";
            case HudStatusKind.Disguise: return "ui_status_disguise";
            case HudStatusKind.Stealth: return "ui_status_stealth";
            case HudStatusKind.Burn: return "ui_status_burn";
            case HudStatusKind.Slow: return "ui_status_slow";
            case HudStatusKind.Pull: return "ui_status_pull";
            default: return null;
        }
    }

    // ---- Kırılan kalp ----

    /// <summary>Kırılma karesi (0..frameCount-1); kırılma bittiyse -1.</summary>
    public static int BreakFrame(float age, float seconds, int frameCount)
    {
        if (frameCount <= 0 || seconds <= 0f || age < 0f || age >= seconds) return -1;
        return Mathf.Clamp(Mathf.FloorToInt(age / seconds * frameCount), 0, frameCount - 1);
    }

    /// <summary>
    /// Kırılan kalpten fırlayan <paramref name="shard"/>. parçanın kalp ortasına göre konumu (resim pikseli, y aşağı):
    /// yelpaze gibi yukarı-yana fırlar, yerçekimiyle düşer.
    /// </summary>
    public static Vector2 ShardOffset(int shard, int shardCount, float age, float speed, float gravity)
    {
        int n = Mathf.Max(1, shardCount);
        float t = Mathf.Max(0f, age);
        // -60°..+60° arası yelpaze (0° = dümdüz yukarı).
        float spread = n == 1 ? 0f : Mathf.Lerp(-60f, 60f, shard / (float)(n - 1));
        float rad = spread * Mathf.Deg2Rad;
        float vx = Mathf.Sin(rad) * speed;
        float vy = -Mathf.Cos(rad) * speed;
        return new Vector2(vx * t, vy * t + 0.5f * gravity * t * t);
    }
}
