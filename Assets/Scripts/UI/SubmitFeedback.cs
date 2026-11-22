/// <summary>
/// Reddedilen kelimeye oyuncuya gösterilecek kısa Türkçe mesaj.
/// Kabul edilen ya da ilgisiz durumlar için boş metin döner (mesaj gösterilmez).
/// </summary>
public static class SubmitFeedback
{
    public static string GetMessage(SubmitStatus status, int maxWordLength)
    {
        switch (status)
        {
            case SubmitStatus.Empty: return "Bir kelime yazmalısın.";
            case SubmitStatus.TooLong: return $"Çok uzun! En fazla {maxWordLength} karakter.";
            case SubmitStatus.Blocked: return "Bu kelime yasak.";
            case SubmitStatus.AlreadyUsed: return "Bu kelime bu maçta zaten kullanıldı!";
            case SubmitStatus.NotWriter: return "Şu an sıra sende değil.";
            default: return string.Empty; // Accepted, AcceptedUnknown, WrongPhase
        }
    }
}
