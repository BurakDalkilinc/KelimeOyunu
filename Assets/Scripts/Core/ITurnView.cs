using System;

/// <summary>
/// Tur durumunun salt okunur görünümü: aşama, yazan oyuncu, kelime, süre ve olaylar. Ekran bileşenleri bunu okur.
/// Tek kişilik sahnede <see cref="TurnManager"/>, ağ maçında istemcideki <see cref="NetTurnClient"/> uygular.
/// </summary>
public interface ITurnView
{
    /// <summary>Aşama her değiştiğinde çağrılır.</summary>
    event Action<TurnPhase> PhaseChanged;

    /// <summary>Zar atıldı ve yazacak oyuncu belli oldu.</summary>
    event Action<PlayerState> WriterSelected;

    /// <summary>Kelime kabul edildi. Bilinmeyen kelimede <c>WordEntry</c> null'dır.</summary>
    event Action<PlayerState, WordEntry> WordPlayed;

    /// <summary>Yazan oyuncu süreyi kaçırdı; tur yandı.</summary>
    event Action<PlayerState> TurnSkipped;

    /// <summary>Maç bitti. Kazanan yoksa null.</summary>
    event Action<PlayerState> MatchEnded;

    TurnPhase Phase { get; }
    PlayerState Writer { get; }
    WordEntry CurrentWord { get; }
    float PhaseTimeLeft { get; }
    int TurnNumber { get; }
    PlayerState Winner { get; }
}
