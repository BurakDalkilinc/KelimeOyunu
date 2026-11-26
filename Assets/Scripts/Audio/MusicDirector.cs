/// <summary>
/// Duruma göre hangi müziğin çalacağına karar verir: menüde ve maç dışında sakin menü müziği, maçta tempolu maç müziği,
/// büyük tehdit (scope "global") sahadayken gerilim müziği. Düz hesap, test edilebilir; çalma işi <see cref="AudioService"/>'te.
/// </summary>
public static class MusicDirector
{
    /// <summary>
    /// <paramref name="inMatch"/>: sahnede çalışan bir tur görünümü var mı. <paramref name="currentWord"/>: o turun kelimesi
    /// (bilinmeyen kelimede null).
    /// </summary>
    public static MusicTrack Choose(bool inMatch, TurnPhase phase, WordEntry currentWord)
    {
        if (!inMatch) return MusicTrack.Menu;

        switch (phase)
        {
            case TurnPhase.Dice:
            case TurnPhase.Typing:
                return MusicTrack.Match;
            case TurnPhase.Result:
                return currentWord != null && currentWord.scope == WordEntry.ScopeGlobal ? MusicTrack.Threat : MusicTrack.Match;
            default:
                return MusicTrack.Menu; // Maç başlamadı (lobi) ya da bitti
        }
    }

    /// <summary>Müziğin ses dosyası adı; müzik yoksa null.</summary>
    public static string ClipName(MusicTrack track)
    {
        switch (track)
        {
            case MusicTrack.Menu: return SoundId.MusicMenu;
            case MusicTrack.Match: return SoundId.MusicMatch;
            case MusicTrack.Threat: return SoundId.MusicThreat;
            default: return null;
        }
    }
}
