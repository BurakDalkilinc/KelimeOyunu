using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yaratık vurunca hangi iz efekti oynar (pençe, ısırık, kesik, yumruk, ezme, düz darbe)? Karar etiketlerden gelir
/// (eşleme verisi Resources/Data/attack_fx.json); kelime adına göre karar YOKTUR. Düz C#, test edilebilir.
/// </summary>
public class AttackFxRules
{
    /// <summary>Dosya yoksa ya da bozuksa kullanılan varsayılan iz.</summary>
    public const string FallbackEffect = "fx_hit";

    readonly List<AttackFxRule> rules = new List<AttackFxRule>();

    /// <summary>Hiçbir etiketi eşleşmeyen yaratığın izi.</summary>
    public string DefaultEffect { get; }

    /// <summary>Hiçbir etiketi eşleşmeyen dev yaratığın izi (boşsa varsayılan).</summary>
    public string GiantEffect { get; }

    /// <summary>Etiket → iz kuralları, öncelik sırasıyla.</summary>
    public IReadOnlyList<AttackFxRule> Rules => rules;

    public AttackFxRules(AttackFxFile file)
    {
        DefaultEffect = string.IsNullOrEmpty(file?.defaultEffect) ? FallbackEffect : file.defaultEffect;
        GiantEffect = string.IsNullOrEmpty(file?.giantEffect) ? DefaultEffect : file.giantEffect;

        if (file?.rules == null) return;
        foreach (AttackFxRule rule in file.rules)
            if (rule != null && !string.IsNullOrEmpty(rule.tag) && !string.IsNullOrEmpty(rule.effect)) rules.Add(rule);
    }

    /// <summary>JSON metninden kurar; boş ya da bozuk metinde yalnızca varsayılan iz kalır.</summary>
    public static AttackFxRules FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AttackFxRules(null);
        try { return new AttackFxRules(JsonUtility.FromJson<AttackFxFile>(json)); }
        catch (Exception) { return new AttackFxRules(null); }
    }

    /// <summary>Resources'tan yükler (ör. "Data/attack_fx"); dosya yoksa varsayılan iz.</summary>
    public static AttackFxRules LoadFromResources(string path)
    {
        TextAsset asset = string.IsNullOrEmpty(path) ? null : Resources.Load<TextAsset>(path);
        return FromJson(asset != null ? asset.text : null);
    }

    /// <summary>
    /// Yaratığın vuruş izi: kurallardaki ilk eşleşen etiket; yoksa dev ise dev izi, değilse varsayılan.
    /// Bilinmeyen kelime (null) de varsayılanı (ya da dev izini) alır.
    /// </summary>
    public string ForCreature(WordEntry word, bool giant)
    {
        if (word != null)
            foreach (AttackFxRule rule in rules)
                if (word.HasTag(rule.tag)) return rule.effect;
        return giant ? GiantEffect : DefaultEffect;
    }
}
