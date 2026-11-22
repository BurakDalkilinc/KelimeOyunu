/// <summary>Bir oyuncunun kelime göndermesine verilen cevap. Doğrulama sunucuda yapılır.</summary>
public enum SubmitStatus
{
    /// <summary>Şu an yazma aşaması değil.</summary>
    WrongPhase,

    /// <summary>Sıra bu oyuncuda değil.</summary>
    NotWriter,

    /// <summary>Boş yazı. Süre işlemeye devam eder, oyuncu tekrar deneyebilir.</summary>
    Empty,

    /// <summary>Çok uzun yazı. Tekrar denenebilir.</summary>
    TooLong,

    /// <summary>Yasaklı kelime. Tekrar denenebilir.</summary>
    Blocked,

    /// <summary>Kelime (ya da eş anlamlısı) bu maçta zaten kullanıldı. Tekrar denenebilir.</summary>
    AlreadyUsed,

    /// <summary>Kelime kabul edildi, sonuç aşaması başladı.</summary>
    Accepted,

    /// <summary>Bilinmeyen kelime kabul edildi; zararsız "???" yaratığı çıkar, tur yanmaz.</summary>
    AcceptedUnknown
}
