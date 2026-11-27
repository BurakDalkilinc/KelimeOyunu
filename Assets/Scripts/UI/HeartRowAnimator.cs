/// <summary>
/// Kalp dizisinin canlandırma zamanlarını tutar: bir kalbin doluluğu azalınca "kırılma", artınca "dolma" anı kaydedilir.
/// Gösterge bu anlardan geçen süreye göre kırılma karelerini ve büyüyerek dolmayı çizer. Zamanı dışarıdan alır, test edilebilir.
/// </summary>
public class HeartRowAnimator
{
    const float Never = -1000f;

    HeartFill[] fills = new HeartFill[0];
    float[] breakAt = new float[0];
    float[] fillAt = new float[0];
    HeartFill[] brokenFrom = new HeartFill[0];
    bool started;

    /// <summary>Şu anki doluluklar (son <see cref="Update"/>'ten).</summary>
    public HeartFill[] Fills => fills;

    /// <summary>
    /// Yeni can bilgisini verir. İlk çağrıda (ya da kalp sayısı değişince) canlandırma yapılmaz, yalnızca durum alınır.
    /// </summary>
    public void Update(int health, int maxHealth, int healthPerHeart, float now)
    {
        HeartFill[] next = HudRules.Hearts(health, maxHealth, healthPerHeart);
        if (!started || next.Length != fills.Length)
        {
            fills = next;
            breakAt = NewTimes(next.Length);
            fillAt = NewTimes(next.Length);
            brokenFrom = new HeartFill[next.Length];
            started = true;
            return;
        }

        for (int i = 0; i < next.Length; i++)
        {
            if (next[i] < fills[i])
            {
                breakAt[i] = now;
                brokenFrom[i] = fills[i];
                fillAt[i] = Never;
            }
            else if (next[i] > fills[i])
            {
                fillAt[i] = now;
                breakAt[i] = Never;
            }
        }
        fills = next;
    }

    /// <summary>Kalp en son ne zaman kırıldı; hiç kırılmadıysa çok eski (büyük sayı döner).</summary>
    public float BreakAge(int index, float now) => Age(breakAt, index, now);

    /// <summary>Kalp en son ne zaman doldu; hiç dolmadıysa büyük sayı.</summary>
    public float FillAge(int index, float now) => Age(fillAt, index, now);

    /// <summary>Kırılmadan önceki doluluk (kırılma karesinin üstüne çizilecek eski kalp).</summary>
    public HeartFill BrokenFrom(int index) => index >= 0 && index < brokenFrom.Length ? brokenFrom[index] : HeartFill.Empty;

    /// <summary>Yeni maçta ya da oyuncu değişince baştan alınsın.</summary>
    public void Reset() => started = false;

    static float Age(float[] times, int index, float now)
    {
        if (index < 0 || index >= times.Length) return float.MaxValue;
        return times[index] <= Never ? float.MaxValue : now - times[index];
    }

    static float[] NewTimes(int count)
    {
        var times = new float[count];
        for (int i = 0; i < count; i++) times[i] = Never;
        return times;
    }
}
