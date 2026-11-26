/// <summary>
/// Oyundaki seslerin adları (Resources/Audio içindeki WAV dosyalarının uzantısız adı). Kod sesi bu adlarla ister;
/// dosya yoksa ses sessizce atlanır. Sesler Tools/GenerateSounds.py ile üretilir.
/// </summary>
public static class SoundId
{
    public const string DiceRoll = "sfx_dice_roll";
    public const string DiceLand = "sfx_dice_land";
    public const string Key = "sfx_key";
    public const string WordOk = "sfx_word_ok";
    public const string WordBad = "sfx_word_bad";
    public const string Spawn = "sfx_spawn";
    public const string Explosion = "sfx_explosion";
    public const string Hit = "sfx_hit";
    public const string Shield = "sfx_shield";
    public const string Heal = "sfx_heal";
    public const string Fire = "sfx_fire";
    public const string Water = "sfx_water";
    public const string Fly = "sfx_fly";
    public const string Siren = "sfx_siren";
    public const string Eliminated = "sfx_eliminated";
    public const string Victory = "sfx_victory";
    public const string Defeat = "sfx_defeat";
    public const string UiClick = "sfx_ui_click";
    public const string BookPage = "sfx_book_page";

    public const string MusicMenu = "music_menu";
    public const string MusicMatch = "music_match";
    public const string MusicThreat = "music_threat";

    /// <summary>Bütün efekt adları (testte her birinin dosyası var mı diye bakılır).</summary>
    public static readonly string[] AllEffects =
    {
        DiceRoll, DiceLand, Key, WordOk, WordBad, Spawn, Explosion, Hit, Shield, Heal, Fire, Water, Fly, Siren,
        Eliminated, Victory, Defeat, UiClick, BookPage
    };

    /// <summary>Bütün müzik adları.</summary>
    public static readonly string[] AllMusic = { MusicMenu, MusicMatch, MusicThreat };
}
