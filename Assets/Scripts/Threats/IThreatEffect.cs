/// <summary>
/// Bir global tehdidin uyarı süresi bittikten sonra yaptığı iş (ör. her yeri patlatmak, dalga göndermek).
/// Tehdidin etiketlerinden kurulur (bkz. <see cref="IThreatEffectTag"/>); kelime adına özel kod yoktur.
/// </summary>
public interface IThreatEffect
{
    /// <summary>Uyarı bitip tehdit etkinleşince bir kez çağrılır.</summary>
    void Begin(ThreatContext context);

    /// <summary>Etkin tehdit sırasında her karede çağrılır.</summary>
    void Tick(float deltaSeconds);

    /// <summary>Etki işini bitirdi mi? Tüm etkiler bitince tehdit sona erer.</summary>
    bool IsFinished { get; }
}

/// <summary>Global tehdit etkisi üreten etiketler bunu uygular.</summary>
public interface IThreatEffectTag
{
    /// <summary>Kelimenin etiketlerine bakarak etkiyi kurar; bu etiket bu kelimede etki üretmiyorsa null.</summary>
    IThreatEffect CreateEffect(WordEntry word);
}
