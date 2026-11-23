/// <summary>Host'tan istemcilere giden tur mesajının türü.</summary>
public enum TurnMessageKind
{
    /// <summary>Durum fotoğrafı (aşama değişti ya da düzenli kalp atışı).</summary>
    State,

    /// <summary>Zar atıldı, yazacak oyuncu belli oldu.</summary>
    WriterSelected,

    /// <summary>Kelime kabul edildi (kelime bilinmiyorsa <see cref="TurnMessage.WordId"/> boştur).</summary>
    WordPlayed,

    /// <summary>Yazan oyuncu süreyi kaçırdı; tur yandı.</summary>
    TurnSkipped,

    /// <summary>Maç bitti.</summary>
    MatchEnded,
}

/// <summary>
/// Host'un istemcilere gönderdiği tur durumu: her mesaj hem olayı hem o anki fotoğrafı taşır,
/// böylece bir mesaj kaybolsa bile sonrakiyle istemci düzelir. Düz veri; ağ kütüphanesinden bağımsız.
/// </summary>
public struct TurnMessage
{
    public const int NoPlayer = -1;

    public TurnMessageKind Kind;
    public TurnPhase Phase;
    public int TurnNumber;

    /// <summary>Yazacak oyuncunun numarası; yoksa <see cref="NoPlayer"/>.</summary>
    public int WriterId;

    /// <summary>Aşamanın bitmesine kalan süre (sn).</summary>
    public float PhaseTimeLeft;

    /// <summary>Oynanan kelimenin kimliği; kelime yok ya da bilinmiyorsa boş metin (null değil).</summary>
    public string WordId;

    /// <summary>Kazananın numarası; yoksa <see cref="NoPlayer"/>.</summary>
    public int WinnerId;
}
