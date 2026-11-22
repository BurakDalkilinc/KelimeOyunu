/// <summary>Bir kelime aramasının / kullanımının sonucu.</summary>
public enum WordLookupStatus
{
    /// <summary>Boş ya da yalnızca boşluk/emoji yazıldı.</summary>
    Empty,

    /// <summary>Yazı izin verilen en uzun uzunluğu aştı.</summary>
    TooLong,

    /// <summary>Yasaklı kelime.</summary>
    Blocked,

    /// <summary>Kelime veritabanında yok. Oyunda zararsız "???" yaratığı olur, tur yanmaz.</summary>
    Unknown,

    /// <summary>Kelime bulundu (kullanımda: kabul edildi ve maç için kilitlendi).</summary>
    Found,

    /// <summary>Kelime (ya da eş anlamlısı) bu maçta zaten kullanılmış.</summary>
    AlreadyUsed
}
