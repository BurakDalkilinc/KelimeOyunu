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

    [Header("İtiş (Boşluk tuşu)")]
    [Tooltip("İtişin vurduğu mesafe (birim).")]
    [Min(0.5f)] public float shoveRange = 1.9f;

    [Tooltip("İtişin yelpazesi: bakılan yönün iki yanında kaç derece.")]
    [Range(10f, 180f)] public float shoveHalfAngleDegrees = 65f;

    [Tooltip("İtilenin savrulacağı mesafe (birim, yakında daha çok). Sıradan çarpışma itişinden çok güçlüdür.")]
    [Min(0.5f)] public float shoveDistance = 3.6f;

    [Tooltip("İtişin bekleme süresi (sn).")]
    [Min(0.5f)] public float shoveCooldownSeconds = 3f;

    [Tooltip("Botların, önlerinde biri varken saniyede itiş atma şansı (0-1+).")]
    [Min(0f)] public float botShoveChancePerSecond = 0.6f;

    [Header("Arazi ve yangın")]
    [Tooltip("Harita tohumu. 0 ise her maçta rastgele bir harita üretilir; başka bir sayı hep aynı haritayı verir.")]
    public int terrainSeed = 0;

    [Tooltip("Yanan tahta komşu tahta karoya saniyede bu olasılıkla (≈) yayılır.")]
    [Min(0f)] public float fireSpreadChancePerSecond = 0.45f;

    [Tooltip("Tahta karonun yanma süresi (sn); sonra kömürleşir.")]
    [Min(0.5f)] public float fireBurnSeconds = 5f;

    [Tooltip("Çimen/patika/kömür üstüne düşen alevin süresi (sn); yayılmaz.")]
    [Min(0.5f)] public float groundFireSeconds = 2.5f;

    [Tooltip("Lavın bitişiğindeki tahta karonun saniyede tutuşma olasılığı.")]
    [Min(0f)] public float lavaIgniteChancePerSecond = 0.1f;

    [Tooltip("Ejderha gibi sürekli ateş püskürten yaratığın alanındaki karoların saniyede tutuşma olasılığı.")]
    [Min(0f)] public float creatureIgniteChancePerSecond = 1.5f;

    [Min(0)] public int terrainFireDamage = 1;
    [Tooltip("Yanan karoda duran oyuncuya hasar aralığı (sn).")]
    [Min(0.1f)] public float terrainFireDamageIntervalSeconds = 1f;

    [Min(0)] public int lavaDamage = 1;
    [Tooltip("Lavda duran oyuncuya hasar aralığı (sn).")]
    [Min(0.1f)] public float lavaDamageIntervalSeconds = 1f;

    [Tooltip("Uçuruma düşenin kaybettiği can (sonra en yakın güvenli kareye döner).")]
    [Min(0)] public int chasmFallDamage = 1;
    [Min(0.1f)] public float chasmFallCooldownSeconds = 1.5f;

    [Header("Harita temaları (köy, karlı dağ, çöl)")]
    [Tooltip("Boşsa her maçta tema harita tohumundan rastgele seçilir. \"koy\" (tahta köy), \"kar\" (karlı dağ) ya da \"col\" (çöl kasabası) yazılırsa hep o tema gelir.")]
    public string forcedMapTheme = "";

    [Tooltip("Buzda hızın değişme hızı (birim/sn²). Küçük değer: zor hızlanır, uzun kayar. Normal zeminde hız anında değişir.")]
    [Min(0.1f)] public float iceAcceleration = 6f;

    [Tooltip("Dondurucu suda yürüme hızı çarpanı (1 = normal).")]
    [Range(0.1f, 1f)] public float freezingWaterSpeedMultiplier = 0.6f;
    [Tooltip("Dondurucu suda duran oyuncunun aldığı hasar.")]
    [Min(0)] public int freezingWaterDamage = 1;
    [Tooltip("Dondurucu suda hasar aralığı (sn). Girişte hemen bir kez vurur.")]
    [Min(0.1f)] public float freezingWaterDamageIntervalSeconds = 1.5f;

    [Tooltip("Bataklık kumunda yürüme hızı çarpanı (1 = normal).")]
    [Range(0.1f, 1f)] public float quicksandSpeedMultiplier = 0.45f;
    [Tooltip("Bataklık kumunda duran oyuncunun aldığı hasar.")]
    [Min(0)] public int quicksandDamage = 1;
    [Tooltip("Bataklık kumunda hasar aralığı (sn). İlk hasar da bu kadar bekledikten sonra gelir (yavaş batar).")]
    [Min(0.1f)] public float quicksandDamageIntervalSeconds = 2.5f;

    [Tooltip("Kaktüse dokunan oyuncunun aldığı hasar (0 = kaktüs yalnızca engel).")]
    [Min(0)] public int cactusDamage = 1;
    [Tooltip("Kaktüs batmasından sonra aynı oyuncuya yeniden batabilmesi için geçen süre (sn). Batma yalnızca temas başlarken olur.")]
    [Min(0.1f)] public float cactusDamageCooldownSeconds = 3f;
    [Tooltip("Kaktüs batınca karakterin kaktüsten geri itildiği mesafe (birim).")]
    [Min(0f)] public float cactusPushDistance = 0.4f;
    [Tooltip("Botların kendi isteğiyle yürürken kaktüsten uzak durduğu ek mesafe (birim). Dokunma mesafesinden büyük olmalı.")]
    [Min(0f)] public float botCactusClearance = 0.3f;
    [Tooltip("Karakterin kenarı kaktüs karosuna bu kadar yaklaşınca (birim) dokunmuş sayılır.")]
    [Min(0f)] public float cactusTouchDistance = 0.08f;

    [Header("Karakter itişmesi")]
    [Tooltip("Açıksa karakterler birbirinin içinden geçemez, hareket eden öndekini iter (kuleden de itilebilir). Hayalet ve uçan karakterler geçer.")]
    public bool charactersPushEachOther = true;

    [Header("Hayalet dirilişi")]
    [Tooltip("\"Yeniden canlanma\" kelimesiyle dirilen oyuncunun canı.")]
    [Min(1)] public int reviveHealth = 1;
    [Min(1)] public int minPlayers = 4;
    [Min(1)] public int maxPlayers = 20;

    [Header("Hareket ve arena")]
    [Tooltip("Oyuncunun saniyedeki hızı (birim/sn).")]
    [Min(0.1f)] public float playerMoveSpeed = 5f;
    [Tooltip("Arenanın genişliği ve yüksekliği (birim).")]
    public Vector2 arenaSize = new Vector2(34f, 19f);
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

    [Tooltip("Boss kelimesi can yazmıyorsa boss'un canı (kaç vuruşta ölür).")]
    [Min(1)] public int bossDefaultHealth = 10;

    [Tooltip("Boşluk'la boss'a bir vuruşun verdiği hasar.")]
    [Min(1)] public int bossHitDamage = 1;

    [Tooltip("Boss'a vururken iki vuruş arası bekleme (sn). İtişin bekleme süresinden kısa: boss dövmek hızlı olsun.")]
    [Min(0.1f)] public float bossHitCooldownSeconds = 0.5f;

    [Tooltip("Boss'u bitirenin kazandığı can (canı tamsa onun yerine bir vuruşluk kalkan alır).")]
    [Min(0)] public int bossKillReward = 1;

    [Tooltip("Açıksa kovalayan yaratıklar tek kişiyi vurup kaybolmaz: hep en yakındakine geçer, süresi bitene kadar herkesi avlar.")]
    public bool creaturesHuntEveryone = true;

    [Tooltip("Ateşten hasar alan karakterin yanma (alev görüntüsü) süresi (sn).")]
    [Min(0f)] public float burnSeconds = 2.5f;

    [Tooltip("Her ateş hasarında kömürleşmenin artışı (1 = simsiyah).")]
    [Range(0f, 1f)] public float charPerFireHit = 0.35f;

    [Tooltip("Araç kelimesi hız yazmıyorsa aracın hızı (birim/sn).")]
    [Min(0.1f)] public float vehicleDefaultSpeed = 9f;

    [Tooltip("Araç kelimesi süre yazmıyorsa sürüş süresi (sn).")]
    [Min(0.1f)] public float vehicleDefaultSeconds = 8f;

    [Tooltip("Araç kelimesi yarıçap yazmıyorsa aracın ezme yarıçapı (birim).")]
    [Min(0.1f)] public float vehicleDefaultRadius = 0.6f;

    [Tooltip("Araç süren botun ezmek için rakip aradığı mesafe (birim).")]
    [Min(0f)] public float botVehiclePreyRange = 60f;

    [Tooltip("Patlayan aracın patlama yarıçapı (birim): içindekiler hasar alır, tahtalar tutuşur.")]
    [Min(0.1f)] public float vehicleCrashRadius = 2f;

    [Tooltip("Patlamanın çevredekilere verdiği hasar (sürücü her durumda ölür).")]
    [Min(0)] public int vehicleCrashDamage = 2;

    [Tooltip("Boss'un canına, sahadaki her ek canlı oyuncu için eklenen can (kalabalık maçta boss hemen ölmesin).")]
    [Min(0)] public int bossHealthPerExtraPlayer = 3;

    [Tooltip("Botların boss'a iki vuruşu arasındaki bekleme (sn). Oyuncununkinden uzun: son vuruşu oyuncu da yapabilsin.")]
    [Min(0.1f)] public float botBossHitCooldownSeconds = 1.5f;

    [Tooltip("Bot en az bu kadar canı varsa boss'a saldırır; daha azsa ondan kaçar.")]
    [Min(1)] public int botBossAttackMinHealth = 2;

    [Tooltip("Bot bu mesafedeki (birim) boss'u fark edip saldırmaya gider.")]
    [Min(0f)] public float botBossAggroRange = 14f;

    [Tooltip("Saldıran botun boss'un gövdesine değmeden durduğu boşluk (birim). Vuruş menzili bundan uzun olmalı.")]
    [Min(0f)] public float botBossStandoff = 0.6f;

    [Tooltip("Bir evin duvar karelerinin bu oranından fazlası yıkılınca (boss) ev tamamen çöker: kalan duvarlar ve çatı gider.")]
    [Range(0f, 1f)] public float bossHouseCollapseRatio = 0.35f;

    [Tooltip("Boss gövdesinin yarıçapına eklenen yıkma payı (birim): duvara yaslanan boss duvarı yıksın (duvar onu bu kadar dışarıda tutar).")]
    [Min(0f)] public float bossWallBreakReach = 0.25f;

    [Tooltip("Ev duvarı yıkılınca kamera sarsıntısı (güç, sn).")]
    [Min(0f)] public float houseBreakShakeAmplitude = 0.18f;
    [Min(0f)] public float houseBreakShakeSeconds = 0.25f;

    [Tooltip("Ev tamamen çökünce kamera sarsıntısı (güç, sn).")]
    [Min(0f)] public float houseCollapseShakeAmplitude = 0.4f;
    [Min(0f)] public float houseCollapseShakeSeconds = 0.5f;

    [Tooltip("Yıkılan her duvar karesindeki toz bulutunun büyüklüğü ve kalma süresi (sn).")]
    [Min(0f)] public float houseBreakDustSize = 1.1f;
    [Min(0.1f)] public float houseBreakDustSeconds = 1.2f;

    [Tooltip("Yıkılan her duvar karesinden saçılan tahta/taş/kerpiç kırıntısı sayısı.")]
    [Min(0)] public int houseBreakDebrisPerCell = 5;

    [Tooltip("Bir yıkımda en çok kaç karede toz/kırıntı gösterilir (ev çökünce çok kare birden yıkılır).")]
    [Min(1)] public int houseBreakMaxFxCells = 10;

    [Tooltip("Duvar yıkılma sesinin düzeyi (0-1).")]
    [Range(0f, 1f)] public float houseBreakVolume = 0.8f;

    [Tooltip("Silah kelimesi süre yazmıyorsa silahın elde kalma süresi (sn).")]
    [Min(1f)] public float weaponDefaultSeconds = 15f;

    [Tooltip("Merminin gidebileceği en uzak mesafe (birim). Botlar da bu mesafenin içindekilere nişan alır.")]
    [Min(1f)] public float weaponRange = 12f;

    [Tooltip("Silah kelimesi hız yazmıyorsa merminin hızı (birim/sn).")]
    [Min(1f)] public float projectileDefaultSpeed = 16f;

    [Tooltip("İki atış arası en kısa süre (sn).")]
    [Min(0f)] public float weaponShotIntervalSeconds = 0.35f;

    [Tooltip("Lava ya da uçuruma itilen karakterin düşme animasyonu süresi (sn); bitince canı tamamen gider.")]
    [Min(0.2f)] public float fallSeconds = 0.9f;
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

    [Tooltip("Alan yaratığının (ör. yağmur bulutu) içindeki yavaşlatma, alandan çıkınca bu kadar süre daha sürer (sn). Kısa tutulur ki alandan kaçan hemen kurtulsun.")]
    [Min(0.1f)] public float areaLingerSeconds = 0.6f;

    [Tooltip("Patlayıcı (explosive) yaratık, hedefin bu kadar yanına düşer (birim). Yumruk gibi tam üstüne değil.")]
    [Min(0f)] public float explosiveDropDistance = 1.5f;

    [Tooltip("Tuzak (trap) hedefin bu kadar yanına düşer (birim).")]
    [Min(0f)] public float trapDropDistance = 2f;

    [Tooltip("Çekme (pull) etkisi, her karede tazelenir ve mıknatıs kaybolunca bu kadar süre daha sürer (sn).")]
    [Min(0.05f)] public float pullLingerSeconds = 0.3f;
    [Tooltip("Kelimede 'pullForce' yazmıyorsa çekme gücü (birim/sn). Oyuncu hızından (5) düşük olduğu için yürüyerek direnilebilir.")]
    [Min(0.1f)] public float pullDefaultForce = 4f;

    [Tooltip("Kelimede 'duration' yazmıyorsa kalkanın en uzun kalma süresi (sn).")]
    [Min(1f)] public float shieldDefaultSeconds = 30f;
    [Tooltip("Kelimede 'duration' yazmıyorsa görünmezlik süresi (sn).")]
    [Min(1f)] public float stealthDefaultSeconds = 6f;
    [Tooltip("Kelimede 'duration' yazmıyorsa uçma (kanat) süresi (sn).")]
    [Min(1f)] public float flightDefaultSeconds = 5f;

    [Tooltip("Duvar, yazanın baktığı yönün bu kadar önünde belirir (birim).")]
    [Min(0.5f)] public float wallDistanceInFront = 1.8f;
    [Tooltip("Duvarın kalınlığı (birim).")]
    [Min(0.1f)] public float wallThickness = 0.5f;
    [Tooltip("Kelimede 'length' yazmıyorsa duvarın uzunluğu (birim).")]
    [Min(0.5f)] public float wallDefaultLength = 4f;
    [Tooltip("Kelimede 'duration' yazmıyorsa duvarın duracağı süre (sn).")]
    [Min(1f)] public float wallDefaultSeconds = 10f;

    [Tooltip("Meteorun, rastgele bir oyuncunun yakınına düşme olasılığı (0-1). Kalanı arenanın rastgele bir yerine düşer.")]
    [Range(0f, 1f)] public float meteorAimedChance = 0.4f;
    [Tooltip("Oyuncuyu hedefleyen meteor, oyuncudan en fazla bu kadar uzağa düşer (birim).")]
    [Min(0f)] public float meteorAimSpread = 2f;

    [Tooltip("Sürüde (swarm) bir üyenin ilk vuruş gecikmesi en çok bu kadar olur (sn). 20 kişilik sürüde sondakiler çok beklemesin diye.")]
    [Min(0f)] public float swarmMaxStaggerSeconds = 4f;

    [Tooltip("Ateş (fire) alanındaki bir oyuncuya iki yanma hasarı arasındaki süre (sn).")]
    [Min(0.1f)] public float fireDamageIntervalSeconds = 1f;

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

    [Header("Çözüm alanları (sığınak, kule, mağara, ev)")]
    [Tooltip("Çözüm alanı sayısı (kule, mağara, ev) kelimedeki sayı bu oyuncu sayısı için ayarlıdır; hayattaki oyuncu sayısıyla orantılı büyür/küçülür.")]
    [Min(1)] public int zoneReferencePlayers = 8;

    [Tooltip("Çözüm alanının yarıçapı (birim). Oyuncunun merkezi bu daireye girince korunur (kapasite varsa).")]
    [Min(0.5f)] public float safeZoneRadius = 1.6f;
    [Tooltip("Çözüm alanları arena kenarından en az bu kadar içeride çıkar (birim).")]
    [Min(0f)] public float safeZoneEdgeMargin = 2.5f;
    [Tooltip("Aynı tehdidin çözüm alanları birbirinden en az bu kadar uzakta çıkar (birim).")]
    [Min(0f)] public float safeZoneSeparation = 5f;
    [Tooltip("Tehdit bittikten sonra çözüm alanlarının ekranda kalma süresi (sn).")]
    [Min(0f)] public float safeZoneLingerSeconds = 2f;

    [Header("Sığınak")]
    [Tooltip("Kapasite = hayattaki oyuncu sayısı x bu oran (aşağı yuvarlanır).")]
    [Range(0.1f, 1f)] public float shelterCapacityRatio = 0.5f;
    [Min(1)] public int shelterMinCapacity = 1;

    [Header("İsim etiketleri (karakterlerin üstünde)")]
    [Tooltip("Karakterlerin başının üstünde adları yazsın mı?")]
    public bool nameTagsEnabled = true;
    [Tooltip("Yazının karakterin merkezinden ne kadar yukarıda duracağı (birim).")]
    [Min(0f)] public float nameTagHeight = 0.95f;
    [Tooltip("Yazının dokuya çizildiği boyut. Piksel yazı tipinin ızgarası 11: 11'in katı olursa harfler net piksel olur (11, 22, 33).")]
    [Min(1)] public int nameTagFontSize = 11;
    [Tooltip("Yazı tipinin bir pikselinin dünyadaki boyu (birim). 1/16 = harita resimlerinin bir pikseli; 1/32 = yarısı (küçük yazı).")]
    [Min(0.005f)] public float nameTagPixelSize = 0.0625f;
    [Tooltip("En uzun isim (harf). Daha uzunu kısaltılıp sonuna nokta konur.")]
    [Min(1)] public int nameTagMaxLength = 14;
    [Tooltip("Ağ maçında oyuncunun bildirdiği adın en uzun hali (harf). Host daha uzununu keser; etiket ayrıca nameTagMaxLength ile kısaltır.")]
    [Min(1)] public int playerNameMaxLength = PlayerNameSanitizer.DefaultMaxLength;
    [Tooltip("Kendi karakterinin adının rengi.")]
    public Color nameTagOwnColor = new Color(1f, 0.87f, 0.25f, 1f);
    [Tooltip("Diğer karakterlerin adının rengi.")]
    public Color nameTagOtherColor = Color.white;
    [Tooltip("Yazının 1 piksellik koyu kenarının rengi (okunaklı olsun diye).")]
    public Color nameTagOutlineColor = new Color(0.07f, 0.05f, 0.1f, 0.9f);
    [Tooltip("Soluk isim (kendi hayaletin, görünmezken kendi adın) saydamlığı.")]
    [Range(0f, 1f)] public float nameTagFaintAlpha = 0.45f;

    [Header("Koleksiyon defteri")]
    [Tooltip("Defterin kaydedildiği dosyanın adı (Application.persistentDataPath içinde).")]
    public string collectionFileName = "collection.json";
    [Tooltip("Etiketlerin Türkçe adlarının durduğu dosya (Resources içinde, uzantısız).")]
    public string collectionTagNamesPath = "Data/tag_names";
    [Tooltip("Defterde bir sayfadaki kart sayısı.")]
    [Min(1)] public int collectionCardsPerPage = 15;
    [Tooltip("Kart ızgarasında bir satırdaki kart sayısı.")]
    [Min(1)] public int collectionColumns = 5;
    [Tooltip("Ekran bu yüksekliğe göre tasarlandı; başka çözünürlükte her şey orantılı büyür/küçülür (piksel).")]
    [Min(100f)] public float collectionReferenceHeight = 1080f;
    [Tooltip("Defter sayfasının ekran kenarına uzaklığı (piksel, referans yüksekliğe göre).")]
    [Min(0f)] public float collectionMargin = 40f;
    [Tooltip("Sayfanın iç boşluğu (piksel).")]
    [Min(0f)] public float collectionPadding = 36f;
    [Tooltip("Kartlar arası boşluk (piksel).")]
    [Min(0f)] public float collectionCardSpacing = 12f;
    [Tooltip("Sağdaki ayrıntı panelinin genişliği (piksel).")]
    [Min(50f)] public float collectionDetailWidth = 380f;
    [Tooltip("Üst satırın (başlık, ilerleme, sekmeler) yüksekliği (piksel).")]
    [Min(10f)] public float collectionHeaderHeight = 110f;
    [Tooltip("Alt satırın (sayfa düğmeleri, Kapat) yüksekliği (piksel).")]
    [Min(10f)] public float collectionFooterHeight = 56f;
    [Tooltip("Kart içinde resmin kartın yüksekliğine oranı.")]
    [Range(0.1f, 0.9f)] public float collectionCardImageRatio = 0.6f;
    [Tooltip("Sayfa resminin (ui_book_page) gerilmeyen kenar kalınlığı (resim pikseli).")]
    [Min(0)] public int collectionPageBorder = 8;
    [Tooltip("Kart çerçevesi resimlerinin (ui_card_frame*) gerilmeyen kenar kalınlığı (resim pikseli).")]
    [Min(0)] public int collectionFrameBorder = 6;
    [Tooltip("Çerçeve resimlerinin bir pikselinin ekrandaki boyu (piksel). Büyüdükçe kenarlar kalınlaşır, piksel görünüm korunur.")]
    [Min(1f)] public float collectionFramePixelScale = 3f;
    [Tooltip("Kartın en/boy oranı (ui_card_frame 32x40 = 0.8).")]
    [Range(0.2f, 2f)] public float collectionCardAspect = 0.8f;
    [Tooltip("Başlıktaki defter simgesinin boyu (piksel).")]
    [Min(1f)] public float collectionIconSize = 36f;
    [Tooltip("Ekranın arkasına çekilen karartmanın rengi.")]
    public Color collectionBackdropColor = new Color(0f, 0f, 0f, 0.75f);
    [Tooltip("Normal yazı boyutu.")]
    [Min(1)] public int collectionFontSize = 33;
    [Tooltip("Başlık yazı boyutu.")]
    [Min(1)] public int collectionTitleFontSize = 44;
    [Tooltip("Kart üstündeki küçük yazı boyutu.")]
    [Min(1)] public int collectionSmallFontSize = 22;
    [Tooltip("Görülmüş ama keşfedilmemiş kelimenin resim saydamlığı (soluk).")]
    [Range(0f, 1f)] public float collectionSeenAlpha = 0.4f;
    [Tooltip("Sayfa resmi yoksa arka plan rengi.")]
    public Color collectionPageColor = new Color(0.93f, 0.86f, 0.7f, 1f);
    [Tooltip("Resim yoksa keşfedilmiş kart rengi.")]
    public Color collectionDiscoveredColor = new Color(0.98f, 0.93f, 0.78f, 1f);
    [Tooltip("Resim yoksa görülmüş kart rengi.")]
    public Color collectionSeenColor = new Color(0.82f, 0.76f, 0.62f, 1f);
    [Tooltip("Resim yoksa kilitli kart rengi.")]
    public Color collectionLockedColor = new Color(0.55f, 0.5f, 0.42f, 1f);
    [Tooltip("Seçili kartın çevresindeki vurgu rengi.")]
    public Color collectionSelectedColor = new Color(0.85f, 0.3f, 0.15f, 1f);
    [Tooltip("Defter yazılarının rengi.")]
    public Color collectionTextColor = new Color(0.2f, 0.13f, 0.08f, 1f);
    /// <summary>Kilitli (koyu) kartın üstündeki "???" ve ipucu yazısının rengi.</summary>
    public Color collectionLockedTextColor = new Color(0.86f, 0.8f, 0.66f, 1f);
    [Tooltip("Seçili kart vurgusunun kalınlığı (piksel).")]
    [Min(0f)] public float collectionSelectedOutline = 4f;

    [Header("Ses ve müzik")]
    [Tooltip("Seslerin Resources altındaki klasörü (Resources/Audio/sfx_hit.wav → \"Audio\").")]
    public string audioFolder = "Audio";
    [Tooltip("Etiket → ses eşleme dosyası (Resources altında, uzantısız).")]
    public string audioSfxTagsPath = "Data/sfx_tags";
    [Tooltip("Aynı anda çalabilecek en fazla efekt sayısı (fazlası o an atlanır).")]
    [Min(1)] public int audioMaxSimultaneousSfx = 10;
    [Tooltip("Aynı efekt bu süreden (sn) önce yeniden istenirse çalınmaz; aynı karede üst üste binmesin diye.")]
    [Min(0f)] public float audioSameSoundCooldown = 0.06f;
    [Tooltip("Vuruş sesi için ayrı bekleme süresi (sn): sürüde onlarca vuruş aynı anda gelir.")]
    [Min(0f)] public float audioHitCooldown = 0.12f;
    [Tooltip("Yazı tuşu sesi için bekleme süresi (sn).")]
    [Min(0f)] public float audioKeyCooldown = 0.03f;
    [Tooltip("Ses kaydırıcısı sürüklenirken tık sesleri arasındaki en kısa süre (sn).")]
    [Min(0f)] public float audioSliderClickInterval = 0.09f;
    [Tooltip("Efektlerin perdesi her çalışta ± bu oranda rastgele değişir (0.05 = %5); hep aynı ses bıktırmasın.")]
    [Range(0f, 0.5f)] public float audioPitchVariation = 0.05f;
    [Tooltip("İki müzik arasındaki çapraz geçiş süresi (sn).")]
    [Min(0f)] public float audioMusicCrossfadeSeconds = 1.5f;
    [Tooltip("Müziğin efektlere göre temel ses düzeyi (ayarlardaki müzik düzeyiyle çarpılır).")]
    [Range(0f, 1f)] public float audioMusicBaseVolume = 0.45f;
    [Tooltip("Yazı tuşu sesinin düzeyi.")]
    [Range(0f, 1f)] public float audioKeyVolume = 0.35f;
    [Tooltip("Başka bir oyuncu vurulunca vuruş sesinin düzeyi (kendi vuruşun tam düzeyde).")]
    [Range(0f, 1f)] public float audioOtherHitVolume = 0.45f;
    [Tooltip("Arayüz tık sesinin düzeyi.")]
    [Range(0f, 1f)] public float audioUiClickVolume = 0.6f;

    [Header("Ayarlar ekranı")]
    [Tooltip("Ayarların kayıt dosyası (Application.persistentDataPath altında).")]
    public string settingsFileName = "settings.json";
    [Tooltip("Ayarlar panelinin genişliği (1080 piksel yükseklikli ekrana göre).")]
    [Min(200f)] public float settingsPanelWidth = 980f;
    [Tooltip("Ayarlar panelindeki bir satırın yüksekliği.")]
    [Min(20f)] public float settingsRowHeight = 62f;
    [Tooltip("Satır adının (\"Müzik\", \"Tam ekran\"...) kapladığı genişlik oranı.")]
    [Range(0.2f, 0.7f)] public float settingsLabelRatio = 0.42f;
    [Tooltip("Satırdaki değer yazısının (\"%80\", \"1920 × 1080\") genişliği.")]
    [Min(20f)] public float settingsValueWidth = 140f;

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
