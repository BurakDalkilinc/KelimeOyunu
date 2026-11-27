using UnityEngine;

/// <summary>
/// Saldırı animasyonunun zaman hesabı: geçen süreden aşamayı (hazırlık/vuruş/toparlanma) ve o aşamadaki ilerlemeyi (0-1),
/// ayrıca atılma mesafesini ve ezilme/uzama ölçeğini verir. Düz C#, test edilebilir; görüntüyü <see cref="CreatureAttackAnimator"/> uygular.
/// </summary>
public class AttackPhase
{
    public float WindupSeconds { get; }
    public float StrikeSeconds { get; }
    public float RecoverSeconds { get; }

    /// <summary>Animasyonun toplam süresi (sn).</summary>
    public float TotalSeconds => WindupSeconds + StrikeSeconds + RecoverSeconds;

    /// <summary>Vuruş anı (darbe izi, sarsıntı): hazırlığın bittiği an (sn).</summary>
    public float ImpactTime => WindupSeconds;

    public AttackPhase(float windupSeconds, float strikeSeconds, float recoverSeconds)
    {
        WindupSeconds = Mathf.Max(0f, windupSeconds);
        StrikeSeconds = Mathf.Max(0.001f, strikeSeconds);
        RecoverSeconds = Mathf.Max(0.001f, recoverSeconds);
    }

    /// <summary>Saldırı başladıktan <paramref name="elapsed"/> saniye sonra hangi aşamadayız? Başlamadıysa ya da bittiyse Idle.</summary>
    public AttackStage Evaluate(float elapsed, out float progress)
    {
        progress = 0f;
        if (elapsed < 0f || elapsed >= TotalSeconds) return AttackStage.Idle;

        if (elapsed < WindupSeconds)
        {
            progress = elapsed / WindupSeconds;
            return AttackStage.Windup;
        }

        elapsed -= WindupSeconds;
        if (elapsed < StrikeSeconds)
        {
            progress = elapsed / StrikeSeconds;
            return AttackStage.Strike;
        }

        progress = Mathf.Clamp01((elapsed - StrikeSeconds) / RecoverSeconds);
        return AttackStage.Recover;
    }

    /// <summary>
    /// Saldırı yönündeki konum, atılma mesafesinin katı olarak: hazırlıkta geriye (-<paramref name="pullBack"/>'e kadar),
    /// vuruşta ileri (1'e kadar), toparlanmada yumuşakça 0'a döner.
    /// </summary>
    public static float LungeFactor(AttackStage stage, float progress, float pullBack)
    {
        float p = Mathf.Clamp01(progress);
        switch (stage)
        {
            case AttackStage.Windup: return -pullBack * EaseOut(p);
            case AttackStage.Strike: return Mathf.Lerp(-pullBack, 1f, p);
            case AttackStage.Recover: return 1f - EaseOut(p);
            default: return 0f;
        }
    }

    /// <summary>
    /// Gövdenin ölçek katı (x, y): hazırlıkta ezilir (geniş + basık), vuruşta uzayıp büyür, toparlanmada normale (1, 1) döner.
    /// </summary>
    public static Vector2 ScaleFactor(AttackStage stage, float progress, float squash, float stretch)
    {
        float p = Mathf.Clamp01(progress);
        var squashed = new Vector2(1f + squash, 1f - squash);
        var stretched = new Vector2(1f + stretch * 0.6f, 1f + stretch);
        switch (stage)
        {
            case AttackStage.Windup: return Vector2.Lerp(Vector2.one, squashed, EaseOut(p));
            case AttackStage.Strike: return Vector2.Lerp(squashed, stretched, p);
            case AttackStage.Recover: return Vector2.Lerp(stretched, Vector2.one, EaseOut(p));
            default: return Vector2.one;
        }
    }

    /// <summary>Saldırı karesi ("ad_atk") gösterilmeli mi? Vuruşta ve toparlanmanın ilk <paramref name="recoverFraction"/> kısmında.</summary>
    public static bool ShowsStrikeFrame(AttackStage stage, float progress, float recoverFraction)
    {
        return stage == AttackStage.Strike || (stage == AttackStage.Recover && progress < recoverFraction);
    }

    /// <summary>Hızlı başlayıp yavaşlayan geçiş (0 → 1).</summary>
    static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
