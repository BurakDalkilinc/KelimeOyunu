/// <summary>Tur akışının aşamaları: zar → yazma → sonuç → (eleme kontrolü) → tekrar zar.</summary>
public enum TurnPhase
{
    /// <summary>Maç henüz başlamadı.</summary>
    NotStarted,

    /// <summary>Zar atılıyor, yazacak oyuncu seçildi (animasyon süresi).</summary>
    Dice,

    /// <summary>Seçilen oyuncu kelime yazıyor.</summary>
    Typing,

    /// <summary>Kelime yaratığa/olaya dönüştü, herkes kaçıyor.</summary>
    Result,

    /// <summary>Maç bitti.</summary>
    MatchOver
}
