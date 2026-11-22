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

    [Tooltip("Yazma sırası bota gelince, kelimeyi göndermeden önce bu iki değer arasında rastgele bekler (sn).")]
    [Min(0f)] public float botTypingDelayMin = 1.5f;
    [Min(0f)] public float botTypingDelayMax = 6f;

    [Header("Yaratıklar")]
    [Tooltip("Yaratığın yarıçapı (birim). Oyuncuya bu mesafede değince hasar verir.")]
    [Min(0.1f)] public float creatureRadius = 0.5f;
    [Tooltip("Kalıcı yaratığın (ör. zombi) aynı oyuncuya iki vuruşu arasındaki bekleme (sn).")]
    [Min(0.1f)] public float creatureHitCooldownSeconds = 1f;
    [Tooltip("Yaratık, hedefinden en az bu kadar uzakta doğar (birim).")]
    [Min(0f)] public float creatureSpawnMinDistance = 6f;
    [Tooltip("Bilinmeyen kelimenin zararsız '???' yaratığının hızı.")]
    [Min(0f)] public float unknownCreatureSpeed = 1.5f;

    [Tooltip("Avcı (predator) yaratık vurduktan sonra bu kadar süre geri çekilir, sonra tekrar saldırır (sn).")]
    [Min(0f)] public float predatorRetreatSeconds = 2f;
    [Tooltip("Avcı yaratık, ömrünün son bu kadar saniyesinde sıkılıp hedeften uzaklaşır ve vurmaz (sn).")]
    [Min(0f)] public float predatorLeaveSeconds = 1.5f;
    [Tooltip("Sürüdeki her yaratığın ilk vuruşu, sırasına göre bu kadar saniye ertelenir. Böylece sürü hepsi birden vurup anında öldürmez (sn).")]
    [Min(0f)] public float swarmHitStaggerSeconds = 0.8f;
    [Tooltip("Sürü doğarken üyelerin dizildiği halkanın yarıçapı (birim).")]
    [Min(0f)] public float swarmSpawnRingRadius = 0.8f;

    [Tooltip("Fırlatma (knockback) mesafesi (birim).")]
    [Min(0f)] public float knockbackDistance = 3f;
    [Tooltip("Fırlatmanın kaç saniyeye yayılacağı. Ani ışınlanma olmasın diye kısa bir kayma süresi.")]
    [Min(0.05f)] public float knockbackSeconds = 0.25f;
    [Tooltip("Anında vuran (instant) yaratığın uyarı süresi en az bu kadardır (sn). Kelimede daha uzunu yazıyorsa o kullanılır.")]
    [Min(0.1f)] public float strikeMinWarningSeconds = 0.5f;
    [Tooltip("Vuruştan sonra darbe izinin ekranda kalma süresi (sn).")]
    [Min(0f)] public float strikeLingerSeconds = 0.4f;

    [Header("Hedef seçme")]
    [Tooltip("Saldırı kelimesini yazan insan oyuncunun fareyle hedef seçme süresi (sn). Süre dolarsa rastgele bir rakip seçilir.")]
    [Min(0.5f)] public float targetSelectionSeconds = 3f;
    [Tooltip("Fare tıklaması, oyuncunun merkezine bu kadar yakınsa o oyuncu seçilmiş sayılır (birim). Oyuncular küçük olduğu için biraz cömert.")]
    [Min(0.1f)] public float targetClickRadius = 0.9f;

    [Tooltip("Yavaşlatılan oyuncunun hız çarpanı (0.5 = yarı hız). İki yavaşlatma üst üste binmez; en güçlüsü geçerlidir, süre tazelenir.")]
    [Range(0.05f, 1f)] public float slowdownSpeedMultiplier = 0.5f;
    [Tooltip("Kelimede 'slowSeconds' yazmıyorsa yavaşlatma süresi (sn).")]
    [Min(0.1f)] public float slowdownDefaultSeconds = 3f;

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
    [Tooltip("Yazı kutusu açıldıktan sonra bu kadar süre klavye yazısı yok sayılır (sn). Hareket tuşu hâlâ basılıyken kutuya 'wwww' dolmasın diye.")]
    [Min(0f)] public float typingInputGraceSeconds = 0.25f;
    [Tooltip("Reddedilen kelime mesajının (ör. 'Bu kelime kullanıldı') ekranda kalma süresi (sn).")]
    [Min(0.5f)] public float typingFeedbackSeconds = 2f;

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
