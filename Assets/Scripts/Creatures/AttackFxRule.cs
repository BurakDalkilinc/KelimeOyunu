using System;

/// <summary>attack_fx.json'daki tek eşleme: bu etiketi taşıyan yaratık vurunca bu iz efekti (ör. "fx_bite") oynar.</summary>
[Serializable]
public class AttackFxRule
{
    public string tag;
    public string effect;
}
