/// <summary>
/// Kelime büyüsünün düz kuralları: harflerin hedef türü (kelimenin kapsamından; kelime adına bakılmaz), doğan yaratığın
/// bu büyüye ait olup olmadığı ve yaratık kaybolurken "puf" efekti çıkıp çıkmayacağı.
/// </summary>
public static class WordSpellRules
{
    /// <summary>
    /// Kapsamdan hedef türü: "global" → ekranın tepesi, "self" → yazanın kendisi, diğerleri (hedefli saldırı, bilinmeyen kelime)
    /// → doğan yaratık.
    /// </summary>
    public static SpellTargetKind KindFor(WordEntry word)
    {
        if (word == null) return SpellTargetKind.Creature; // Bilinmeyen kelime "???" yaratığı olur
        if (word.scope == WordEntry.ScopeGlobal) return SpellTargetKind.Global;
        if (word.scope == WordEntry.ScopeSelf) return SpellTargetKind.Self;
        return SpellTargetKind.Creature;
    }

    /// <summary>Doğan yaratığın kelimesi büyünün kelimesiyle aynı mı? Boş ve null kimlik aynı sayılır (bilinmeyen kelime).</summary>
    public static bool Matches(string spellWordId, string creatureWordId)
    {
        return (spellWordId ?? string.Empty) == (creatureWordId ?? string.Empty);
    }

    /// <summary>
    /// Kaybolan yaratıkta "puf" çıksın mı? Mermiler ve vurmuş uyarı alanları (patlama efekti zaten var) ile çok küçük şeyler
    /// puf yapmaz.
    /// </summary>
    public static bool ShouldPoof(bool projectile, bool strike, float radius, float minRadius)
    {
        return !projectile && !strike && radius >= minRadius;
    }
}
