using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hangi olayda hangi efekt çalar? Kelime sesi etiketlerden gelir (eşleme verisi Resources/Data/sfx_tags.json);
/// kelime adına göre karar YOKTUR. Düz C#, test edilebilir; sesi çalan <see cref="AudioService"/>'tir.
/// </summary>
public class SfxRules
{
    readonly List<SfxTagRule> rules = new List<SfxTagRule>();

    /// <summary>Büyük tehdit (scope "global") kelimesinin sesi.</summary>
    public string ThreatSound { get; }

    /// <summary>Hiçbir etiketi eşleşmeyen bilinen kelimenin sesi.</summary>
    public string DefaultSound { get; }

    /// <summary>Bilinmeyen kelimenin ("???" yaratığı) sesi.</summary>
    public string UnknownSound { get; }

    /// <summary>Etiket → ses kuralları, öncelik sırasıyla.</summary>
    public IReadOnlyList<SfxTagRule> Rules => rules;

    public SfxRules(SfxTagFile file)
    {
        ThreatSound = Pick(file?.threatSound, SoundId.Siren);
        DefaultSound = Pick(file?.defaultSound, SoundId.Spawn);
        UnknownSound = Pick(file?.unknownSound, DefaultSound);

        if (file?.rules == null) return;
        foreach (SfxTagRule rule in file.rules)
            if (rule != null && !string.IsNullOrEmpty(rule.tag) && !string.IsNullOrEmpty(rule.sound)) rules.Add(rule);
    }

    /// <summary>JSON metninden kurar; boş ya da bozuk metinde yalnızca varsayılan sesler kalır.</summary>
    public static SfxRules FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new SfxRules(null);
        try { return new SfxRules(JsonUtility.FromJson<SfxTagFile>(json)); }
        catch (Exception) { return new SfxRules(null); }
    }

    /// <summary>Resources'tan yükler (ör. "Data/sfx_tags"); dosya yoksa varsayılan sesler.</summary>
    public static SfxRules LoadFromResources(string path)
    {
        TextAsset asset = Resources.Load<TextAsset>(path);
        return FromJson(asset != null ? asset.text : null);
    }

    /// <summary>
    /// Oynanan kelimenin sesi: bilinmeyen kelime (null) → bilinmeyen sesi; büyük tehdit → siren;
    /// yoksa kurallardaki ilk eşleşen etiket; hiçbiri yoksa varsayılan (yaratık doğma) sesi.
    /// </summary>
    public string ForWord(WordEntry word)
    {
        if (word == null) return UnknownSound;
        if (word.scope == WordEntry.ScopeGlobal) return ThreatSound;

        foreach (SfxTagRule rule in rules)
            if (word.HasTag(rule.tag)) return rule.sound;
        return DefaultSound;
    }

    /// <summary>Kelime gönderme sonucunun sesi: kabul → onay, reddedilen → ret; sıra/aşama hatasında ses yok (null).</summary>
    public static string ForSubmit(SubmitStatus status)
    {
        switch (status)
        {
            case SubmitStatus.Accepted:
            case SubmitStatus.AcceptedUnknown:
                return SoundId.WordOk;
            case SubmitStatus.Empty:
            case SubmitStatus.TooLong:
            case SubmitStatus.Blocked:
            case SubmitStatus.AlreadyUsed:
                return SoundId.WordBad;
            default:
                return null;
        }
    }

    /// <summary>Maç sonu sesi: kazanan bu bilgisayardaki oyuncuysa zafer, değilse yenilgi. Yerel oyuncu yoksa (izleyici) zafer.</summary>
    public static string ForMatchEnd(PlayerState winner, PlayerState local)
    {
        if (local == null) return SoundId.Victory;
        return winner != null && winner.Id == local.Id ? SoundId.Victory : SoundId.Defeat;
    }

    static string Pick(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;
}
