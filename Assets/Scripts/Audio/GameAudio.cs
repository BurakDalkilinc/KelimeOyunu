/// <summary>
/// Ses çalmanın kısa yolu: <c>GameAudio.Play(SoundId.Hit)</c>. Ses servisi yoksa (ör. EditMode testi) hiçbir şey yapmaz.
/// </summary>
public static class GameAudio
{
    /// <summary>Efekti çalar (bkz. <see cref="AudioService.Play"/>).</summary>
    public static void Play(string id, float volume = 1f, float cooldown = -1f)
    {
        AudioService service = AudioService.Instance;
        if (service != null) service.Play(id, volume, cooldown);
    }

    /// <summary>Arayüz düğmesi tık sesi.</summary>
    public static void Click() => Play(SoundId.UiClick, GameConfigLoader.Get().audioUiClickVolume);

    /// <summary>Defter sayfa çevirme sesi.</summary>
    public static void PageTurn() => Play(SoundId.BookPage);
}
