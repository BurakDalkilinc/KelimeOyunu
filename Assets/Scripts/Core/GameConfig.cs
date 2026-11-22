using UnityEngine;

/// <summary>
/// Oyunun tüm ayarları burada durur (süreler, can, kapasite).
/// Kodda sabit sayı yazılmaz; değerler bu dosyadan okunur.
/// Varsayılanlar Docs/GDD.md içindeki "Temel değerler" tablosundan gelir.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "KelimeOyunu/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Oyuncu")]
    [Min(1)] public int startingHealth = 3;
    [Min(1)] public int minPlayers = 4;
    [Min(1)] public int maxPlayers = 20;

    [Header("Hareket ve arena")]
    [Tooltip("Oyuncunun saniyedeki hızı (birim/sn).")]
    [Min(0.1f)] public float playerMoveSpeed = 5f;
    [Tooltip("Arenanın genişliği ve yüksekliği (birim).")]
    public Vector2 arenaSize = new Vector2(20f, 12f);
    [Tooltip("Oyuncunun merkezinin arena kenarından uzak durması gereken mesafe.")]
    [Min(0f)] public float playerRadius = 0.5f;
    [Tooltip("Kameranın arenanın etrafında bırakacağı boşluk (birim).")]
    [Min(0f)] public float cameraPadding = 1f;

    [Header("Botlar")]
    [Tooltip("Oyuncuyla birlikte maça katılan bot sayısı (1 oyuncu + 7 bot = 8 kişi).")]
    [Min(0)] public int botCount = 7;
    [Min(0.1f)] public float botMoveSpeed = 4f;
    [Tooltip("Botun gezinme hedeflerini arena kenarından ne kadar içeride seçeceği (birim).")]
    [Min(0f)] public float botWanderMargin = 1.5f;
    [Tooltip("Hedefe bu kadar yaklaşınca bot vardı sayılır (birim).")]
    [Min(0.05f)] public float botArrivalDistance = 0.3f;
    [Tooltip("Bot hedefe varınca bu iki değer arasında rastgele bekler (sn).")]
    [Min(0f)] public float botPauseMin = 0.3f;
    [Min(0f)] public float botPauseMax = 1.5f;
    [Tooltip("Bot, tehlike alanının kenarına bu kadar yaklaşınca kaçmaya başlar (birim).")]
    [Min(0f)] public float botFleeMargin = 2f;

    [Tooltip("Kaçarken bot, duvara bu mesafeden yakınsa duvardan uzaklaşmaya başlar (birim). Köşeye sıkışmayı önler.")]
    [Min(0f)] public float botWallAvoidRange = 3f;
    [Tooltip("Duvardan uzak durma kuvveti (kaçış kuvvetine göre).")]
    [Min(0f)] public float botWallAvoidWeight = 1.5f;

    [Header("Tur süreleri (saniye)")]
    [Min(0.5f)] public float diceSeconds = 2f;
    [Min(1f)] public float typingSeconds = 10f;
    [Min(1f)] public float resultSecondsMin = 15f;
    [Min(1f)] public float resultSecondsMax = 20f;

    [Header("Zar")]
    [Tooltip("Üst üste seçilmeyen her tur için oyuncunun seçilme ağırlığına eklenen değer. Temel ağırlık 1'dir.")]
    [Min(0f)] public float diceBonusPerMissedTurn = 0.5f;

    [Header("Kelime")]
    [Tooltip("Oyuncunun yazabileceği en uzun kelime (karakter). Daha uzunu reddedilir.")]
    [Min(1)] public int maxWordLength = 50;

    [Header("Global tehdit")]
    [Tooltip("Tehdit geldikten sonra çözüme ulaşmak için en az süre.")]
    [Min(1f)] public float globalThreatWarningSeconds = 5f;

    [Header("Sığınak")]
    [Tooltip("Kapasite = hayattaki oyuncu sayısı x bu oran (aşağı yuvarlanır).")]
    [Range(0.1f, 1f)] public float shelterCapacityRatio = 0.5f;
    [Min(1)] public int shelterMinCapacity = 1;

    [Header("Rastgelelik")]
    [Tooltip("0 ise her maç farklı bir seed alır. Sıfırdan farklıysa maç tekrar oynatılabilir.")]
    public int randomSeed = 0;

    /// <summary>
    /// Maç için rastgele sayı üretici. Seed 0 ise her seferinde farklı; değilse maç tekrar oynatılabilir.
    /// Oyun mantığı tek bir üretici kullanır, böylece ileride ağda senkronlanabilir.
    /// </summary>
    public System.Random CreateRandom()
    {
        return randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
    }

    /// <summary>Hayattaki oyuncu sayısına göre sığınak kapasitesini hesaplar.</summary>
    public int GetShelterCapacity(int alivePlayers)
    {
        int capacity = Mathf.FloorToInt(alivePlayers * shelterCapacityRatio);
        return Mathf.Max(shelterMinCapacity, capacity);
    }
}
