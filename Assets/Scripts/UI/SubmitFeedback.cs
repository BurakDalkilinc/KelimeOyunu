/// <summary>
/// Reddedilen kelimeye oyuncuya gösterilecek kısa mesaj (seçili dilde).
/// Kabul edilen ya da ilgisiz durumlar için boş metin döner (mesaj gösterilmez).
/// </summary>
public static class SubmitFeedback
{
    public static string GetMessage(SubmitStatus status, int maxWordLength)
    {
        switch (status)
        {
            case SubmitStatus.Empty: return Loc.T("feedback.empty");
            case SubmitStatus.TooLong: return Loc.F("feedback.too_long", maxWordLength);
            case SubmitStatus.Blocked: return Loc.T("feedback.blocked");
            case SubmitStatus.AlreadyUsed: return Loc.T("feedback.already_used");
            case SubmitStatus.NotWriter: return Loc.T("feedback.not_writer");
            default: return string.Empty; // Accepted, AcceptedUnknown, WrongPhase
        }
    }
}
