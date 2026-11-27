using UnityEngine;

/// <summary>Beyaz parlamanın aşaması: yok, tam beyaz, renk tonu (kırmızı ya da kalkan rengi) sönüyor.</summary>
public enum HitFlashPhase
{
    None,
    White,
    Tint,
}

/// <summary>
/// Vuruş hissinin küçük hesapları (donma süresi, parlama aşaması, ezilme ölçeği, hasar sayısının uçuşu, kalp atışı,
/// ağır çekim, erişilebilirlik çarpanları). Hepsi düz hesap, test edilebilir; görüntüyü Fx bileşenleri uygular.
/// Sayılar <see cref="GameConfig"/>'ten gelir, burada sabit sayı tutulmaz.
/// </summary>
public static class HitFeelMath
{
    /// <summary>Vuruş donmasının süresi: normal, dev vuruşu ya da eleme (en uzunu seçilir).</summary>
    public static float HitStopSeconds(bool giant, bool eliminated, float normalSeconds, float giantSeconds, float deathSeconds)
    {
        float seconds = giant ? Mathf.Max(normalSeconds, giantSeconds) : normalSeconds;
        if (eliminated) seconds = Mathf.Max(seconds, deathSeconds);
        return Mathf.Max(0f, seconds);
    }

    /// <summary>
    /// Vuruştan <paramref name="age"/> saniye sonra parlamanın aşaması: önce <paramref name="whiteSeconds"/> tam beyaz, sonra
    /// <paramref name="tintSeconds"/> boyunca sönen renk tonu (<paramref name="tintAmount"/> 1'den 0'a). Sonra hiçbiri.
    /// </summary>
    public static HitFlashPhase Flash(float age, float whiteSeconds, float tintSeconds, out float tintAmount)
    {
        tintAmount = 0f;
        if (age < 0f) return HitFlashPhase.None;
        if (age < whiteSeconds) return HitFlashPhase.White;

        float t = age - Mathf.Max(0f, whiteSeconds);
        if (tintSeconds <= 0f || t >= tintSeconds) return HitFlashPhase.None;

        float left = 1f - t / tintSeconds;
        tintAmount = left * left; // Hızlı söner
        return HitFlashPhase.Tint;
    }

    /// <summary>
    /// Ezilme ölçeği (x, y). <paramref name="amount"/> 1 = tam ezik, 0 = normal, eksi = ters yöne uzama (yay geri sekerken).
    /// Vuruş yönü yataysa gövde yatayda basılıp dikeyde şişer; dikeyse (ya da yön bilinmiyorsa) dikeyde basılıp yatayda şişer
    /// (ör. <paramref name="squash"/> 0,2 ve <paramref name="bulge"/> 0,25 ile 1,25x / 0,8y).
    /// </summary>
    public static Vector2 SquashScale(Vector2? direction, float amount, float squash, float bulge)
    {
        float along = Mathf.Max(0.05f, 1f - squash * amount);
        float across = Mathf.Max(0.05f, 1f + bulge * amount);
        bool horizontal = direction.HasValue && Mathf.Abs(direction.Value.x) > Mathf.Abs(direction.Value.y);
        return horizontal ? new Vector2(along, across) : new Vector2(across, along);
    }

    /// <summary>Ayardaki ekran sarsıntısı çarpanı: kapalıysa 0, açıksa şiddet yüzdesi (0..1).</summary>
    public static float ShakeScale(bool enabled, float strength)
    {
        if (!enabled || float.IsNaN(strength)) return 0f;
        return Mathf.Clamp01(strength);
    }

    /// <summary>Ekran parlamalarının çarpanı: sarsıntı kapalıysa parlamalar <paramref name="reducedFactor"/> kadar yumuşar.</summary>
    public static float ScreenFlashScale(bool shakeEnabled, float reducedFactor)
    {
        return shakeEnabled ? 1f : Mathf.Clamp01(reducedFactor);
    }

    /// <summary>Kameraya uzaklığa göre sarsıntı payı: <paramref name="full"/> birime kadar tam, <paramref name="zero"/> birimde sıfır.</summary>
    public static float DistanceFalloff(float distance, float full, float zero)
    {
        if (distance <= full) return 1f;
        if (zero <= full) return 0f;
        return Mathf.Clamp01(1f - (distance - full) / (zero - full));
    }

    /// <summary>Hasar sayısının yazısı: "-1", "-2".</summary>
    public static string DamageText(int amount) => "-" + Mathf.Max(1, amount);

    /// <summary>Hasar sayısının <paramref name="t"/> saniyedeki kayması: yukarı fırlar, yerçekimiyle yavaşlayıp düşer.</summary>
    public static Vector2 DamageNumberOffset(float t, Vector2 velocity, float gravity)
    {
        t = Mathf.Max(0f, t);
        return new Vector2(velocity.x * t, velocity.y * t - 0.5f * gravity * t * t);
    }

    /// <summary>Hasar sayısının saydamlığı: ömrünün son <paramref name="fadeFraction"/> kısmında söner.</summary>
    public static float DamageNumberAlpha(float t, float life, float fadeFraction)
    {
        if (life <= 0f || t >= life) return 0f;
        float fadeStart = life * (1f - Mathf.Clamp01(fadeFraction));
        if (t <= fadeStart) return 1f;
        return Mathf.Clamp01(1f - (t - fadeStart) / Mathf.Max(0.0001f, life - fadeStart));
    }

    /// <summary>Hasar sayısının büyüklüğü: ilk <paramref name="popSeconds"/> içinde <paramref name="popScale"/>'den 1'e iner ("pat" diye çıkar).</summary>
    public static float DamageNumberScale(float t, float popSeconds, float popScale)
    {
        if (popSeconds <= 0f || t >= popSeconds) return 1f;
        float p = Mathf.Clamp01(t / popSeconds);
        return Mathf.Lerp(popScale, 1f, 1f - (1f - p) * (1f - p));
    }

    /// <summary>Ses perdesi: 0..1 arası rastgele sayıdan 1 ± <paramref name="variation"/>.</summary>
    public static float RandomPitch(float random01, float variation)
    {
        return 1f + (Mathf.Clamp01(random01) * 2f - 1f) * Mathf.Max(0f, variation);
    }

    /// <summary>Düşük can mı? Yaşıyor, can eşiğe inmiş ve en yüksek can eşikten büyük (tek canlık maçta hep çalmasın).</summary>
    public static bool IsLowHealth(int current, int max, int threshold)
    {
        return current > 0 && current <= threshold && max > threshold;
    }

    /// <summary>
    /// Kalp atışı nabzı (0..1): dakikada <paramref name="bpm"/> atış, her atışta iki vuruş ("güm-güm"), arada sessiz.
    /// <paramref name="beat"/> o anda yeni bir vuruşun başladığı yarım-atışın sırası (0 = birinci, 1 = ikinci).
    /// </summary>
    public static float HeartbeatPulse(float time, float bpm, out int beat)
    {
        beat = 0;
        if (bpm <= 0f) return 0f;
        float period = 60f / bpm;
        float phase = Mathf.Repeat(time, period) / period; // 0..1

        // İlk vuruş 0..0,15, ikinci 0,22..0,37; geri kalanı sessiz.
        float first = Bump(phase, 0f, 0.15f);
        float second = Bump(phase, 0.22f, 0.15f) * 0.7f;
        beat = phase < 0.22f ? 0 : 1;
        return Mathf.Max(first, second);
    }

    /// <summary>Kalp atışı vuruşunun başladığı nabız evreleri (0..1): ses bu anlarda çalar.</summary>
    public static readonly float[] HeartbeatOnsets = { 0f, 0.22f };

    /// <summary>
    /// Döngüsel evre (0..1) <paramref name="previous"/>'dan <paramref name="current"/>'e ilerlerken <paramref name="onset"/>'i geçti mi?
    /// Başa sarmayı (0,95 → 0,05) da tanır.
    /// </summary>
    public static bool CrossedPhase(float previous, float current, float onset)
    {
        if (current >= previous) return previous < onset && onset <= current;
        return onset > previous || onset <= current; // Başa sardı
    }

    static float Bump(float x, float start, float width)
    {
        if (x < start || x > start + width) return 0f;
        float p = (x - start) / width;
        return Mathf.Sin(p * Mathf.PI);
    }

    /// <summary>
    /// Ağır çekimde zaman çarpanı: önce <paramref name="scale"/> (ör. 0,3), sürenin son <paramref name="easeFraction"/> kısmında
    /// yumuşakça 1'e döner. <paramref name="elapsed"/> gerçek zaman (sn); süre bitince 1.
    /// </summary>
    public static float SlowMotionScale(float elapsed, float seconds, float scale, float easeFraction)
    {
        if (seconds <= 0f || elapsed >= seconds) return 1f;
        float clampedScale = Mathf.Clamp(scale, 0.01f, 1f);
        if (elapsed <= 0f) return clampedScale;

        float easeStart = seconds * (1f - Mathf.Clamp01(easeFraction));
        if (elapsed <= easeStart) return clampedScale;
        float p = Mathf.Clamp01((elapsed - easeStart) / Mathf.Max(0.0001f, seconds - easeStart));
        return Mathf.Lerp(clampedScale, 1f, p * p * (3f - 2f * p));
    }

    /// <summary>
    /// Boss yazısından ("Godzilla 7/10", "{w} 7/10") kalan canı okur (ağda istemci boss canını yazıdan bilir).
    /// Yazıda "sayı/sayı" yoksa -1.
    /// </summary>
    public static int BossHealthFromLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return -1;
        int slash = label.LastIndexOf('/');
        if (slash <= 0 || slash == label.Length - 1) return -1;

        int start = slash;
        while (start > 0 && char.IsDigit(label[start - 1])) start--;
        if (start == slash) return -1;
        for (int i = slash + 1; i < label.Length; i++) if (!char.IsDigit(label[i])) return -1;

        return int.TryParse(label.Substring(start, slash - start), out int health) ? health : -1;
    }
}
