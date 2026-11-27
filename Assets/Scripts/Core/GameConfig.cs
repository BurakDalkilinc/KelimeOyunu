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
    [Min(0f)] public float creatureSpawnMinDistance = 4.5f;
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

    [Header("Yaratık tehlikesi (hedefli saldırı yaratıkları; global tehditlere uygulanmaz)")]
    [Tooltip("Kovalayan yaratığın hızı kelimenin hızının bu katıdır (1 = kelimedeki değer).")]
    [Min(0f)] public float creatureSpeedMultiplier = 1.25f;
    [Tooltip("\"slow\" etiketli yaratığın hızı en az oyuncu hızının bu katı olur (0 = alt sınır yok).")]
    [Range(0f, 1f)] public float creatureSlowMinSpeedFraction = 0.55f;
    [Tooltip("\"slow\" etiketli yaratığın hızı en çok oyuncu hızının bu katı olur: yavaşlar hep oyuncudan yavaş kalır.")]
    [Range(0.1f, 1f)] public float creatureSlowMaxSpeedFraction = 0.9f;
    [Tooltip("Çarpandan sonra hız en çok oyuncu hızının bu katı olur (kelimedeki hızın altına inmez; 0 = sınır yok).")]
    [Min(0f)] public float creatureMaxSpeedFraction = 1.4f;
    [Tooltip("Kovalayan yaratığın ömrü kelimenin süresinin bu katıdır.")]
    [Min(0.1f)] public float creatureDurationMultiplier = 1.4f;

    [Tooltip("Atılma menzili: yaratığın kenarından hedefin merkezine bu kadar yaklaşınca atılmaya hazırlanır (birim, 0 = atılmaz).")]
    [Min(0f)] public float creaturePounceRange = 2.5f;
    [Tooltip("Atılmadan önceki hazırlık: yaratık durur, titrer ve kırmızı yanar (sn). Oyuncunun kaçma fırsatı.")]
    [Min(0f)] public float creaturePounceWindupSeconds = 0.2f;
    [Tooltip("Atılmanın süresi (sn).")]
    [Min(0f)] public float creaturePounceSeconds = 0.35f;
    [Tooltip("Atılırken hız, yaratığın hızının bu katı.")]
    [Min(1f)] public float creaturePounceSpeedMultiplier = 2.2f;
    [Tooltip("Atılmadan sonra yeniden atılabilmek için bekleme (sn).")]
    [Min(0f)] public float creaturePounceCooldownSeconds = 2.5f;
    [Tooltip("Atılma, hazırlık sonundaki hedef noktasını bu kadar geçebilir (birim). Yana kaçan kurtulur.")]
    [Min(0f)] public float creaturePounceOvershoot = 0.6f;
    [Tooltip("Etikete göre ayar çarpanları (atılma menzili, atılma hızı, yürüme hızı, ömür). Birden fazla etiket eşleşirse çarpanlar çarpılır.")]
    public CreatureTagTuning[] creatureTagTunings =
    {
        // Hızlılar zaten oyuncudan hızlı: yürüme hızı ve ömrü az artar, atılmaları biraz uzun.
        new CreatureTagTuning { tag = "fast", pounceRangeMultiplier = 1.2f, pounceSpeedMultiplier = 1.1f, speedMultiplier = 0.85f, durationMultiplier = 0.75f },
        // Avcılar vurup geri çekildiği için atılmaları en uzun ve en hızlı olanlar.
        new CreatureTagTuning { tag = "predator", pounceRangeMultiplier = 1.3f, pounceSpeedMultiplier = 1.15f, speedMultiplier = 1f, durationMultiplier = 1f },
        // Sürü zaten çok kişi: üyeleri atılmaz, hızı ve ömrü eskisi kadar kalır.
        new CreatureTagTuning { tag = "swarm", pounceRangeMultiplier = 0f, pounceSpeedMultiplier = 1f, speedMultiplier = 0.8f, durationMultiplier = 0.72f },
        // Boss zaten uzun yaşar ve dev: ömrü uzamaz, atılması kısa.
        new CreatureTagTuning { tag = "boss", pounceRangeMultiplier = 0.5f, pounceSpeedMultiplier = 1f, speedMultiplier = 1f, durationMultiplier = 0.72f },
    };

    [Tooltip("Yaratığın vurduğu oyuncu bu kadar yavaşlar (sendeleme; 1 = yavaşlamaz).")]
    [Range(0.1f, 1f)] public float creatureHitSlowMultiplier = 0.5f;
    [Tooltip("Sendeleme süresi (sn, 0 = sendeleme yok).")]
    [Min(0f)] public float creatureHitSlowSeconds = 0.6f;

    [Tooltip("Hedefine vuramayan (evde, uçuyor, görünmez, önü kapalı) yaratık bu kadar sonra en yakın vurulabilir oyuncuya döner (sn, 0 = dönmez).")]
    [Min(0f)] public float creatureRetargetSeconds = 1.5f;
    [Tooltip("Bırakılan hedefe bu kadar süre geri dönülmez (sn); yaratık ikisi arasında gidip gelmesin.")]
    [Min(0f)] public float creatureRetargetIgnoreSeconds = 3f;
    [Tooltip("Yaratık istediği adımın bu kadarından azını atabiliyorsa önü kapalı (duvara, eve dayanmış) sayılır.")]
    [Range(0f, 1f)] public float creatureStuckMoveFraction = 0.25f;

    [Tooltip("Botlar atılma menzilinin bu kadarını da tehlike sayıp erken kaçar (0 = hiç, 1 = tamamı). 1'in altı: botlar kusursuz kaçmasın.")]
    [Range(0f, 1f)] public float botPounceAwareness = 0.6f;

    [Header("Korku ipuçları (yalnızca görüntü)")]
    [Tooltip("Yerel oyuncuya bu kadar yakın (birim) tehlikeli yaratık varsa ekran kenarı kırmızı nabız atar.")]
    [Min(0f)] public float dangerVignetteRange = 3.5f;
    [Tooltip("Vinyetin en koyu hâli (0-1).")]
    [Range(0f, 1f)] public float dangerVignetteMaxAlpha = 0.5f;
    [Tooltip("Vinyetin nabız hızı (saniyede atış).")]
    [Min(0f)] public float dangerVignettePulseHz = 2.2f;
    [Tooltip("Vinyet kenarının kalınlığı, ekranın kısa kenarına göre.")]
    [Range(0.01f, 0.5f)] public float dangerVignetteThickness = 0.14f;
    [Tooltip("Ekran dışındaki, seni kovalayan yaratığı gösteren okun boyu (piksel, 1080p'de).")]
    [Min(4f)] public float dangerArrowSize = 34f;
    [Tooltip("Ok ekran kenarından bu kadar içeride durur (piksel, 1080p'de).")]
    [Min(0f)] public float dangerArrowEdgeMargin = 26f;
    [Tooltip("Atılmaya hazırlanan yaratığın parladığı renk.")]
    public Color creaturePounceTint = new Color(1f, 0.22f, 0.18f, 1f);
    [Tooltip("Hazırlıkta titreme genliği (birim).")]
    [Min(0f)] public float creaturePounceTremble = 0.06f;

    [Header("Yaratık saldırı animasyonu (yalnızca görüntü; hasar zamanı kuraldadır)")]
    [Tooltip("Hazırlık (kurulma) süresi: yaratık geri çekilip ezilir, 'ad_atk0' karesi gösterilir (sn). Hasar olay anında işlendiği için kısa tutulur.")]
    [Min(0f)] public float creatureAttackWindupSeconds = 0.12f;
    [Tooltip("Devlerin (boss) hazırlık süresi (sn).")]
    [Min(0f)] public float creatureAttackGiantWindupSeconds = 0.25f;
    [Tooltip("Vuruş süresi: hedefe atılma + uzama, 'ad_atk' karesi; sonunda darbe izi çıkar (sn).")]
    [Min(0.01f)] public float creatureAttackStrikeSeconds = 0.08f;
    [Tooltip("Toparlanma süresi: yaratık yerine döner (sn).")]
    [Min(0.01f)] public float creatureAttackRecoverSeconds = 0.25f;
    [Tooltip("Saldırı karesi ('ad_atk') toparlanmanın bu kadarı boyunca da görünür kalır (0 = yalnızca vuruşta).")]
    [Range(0f, 1f)] public float creatureAttackFrameRecoverFraction = 0.4f;
    [Tooltip("Normal yaratığın hedefe atılma mesafesi (birim).")]
    [Min(0f)] public float creatureAttackLunge = 0.3f;
    [Tooltip("Devin hedefe atılma mesafesi (birim).")]
    [Min(0f)] public float creatureAttackGiantLunge = 0.75f;
    [Tooltip("Hazırlıkta geri çekilme, atılma mesafesinin bu kadarı.")]
    [Range(0f, 1f)] public float creatureAttackPullBack = 0.35f;
    [Tooltip("Hazırlıkta ezilme (yanlara genişleyip basıklaşma) oranı.")]
    [Range(0f, 0.5f)] public float creatureAttackSquash = 0.15f;
    [Tooltip("Vuruşta uzama/büyüme oranı.")]
    [Range(0f, 0.5f)] public float creatureAttackStretch = 0.2f;

    [Header("Vuruş izi (pençe, ısırık, kesik...)")]
    [Tooltip("Etiket → vuruş izi eşlemesi (Resources altındaki JSON, uzantısız).")]
    public string attackFxPath = "Data/attack_fx";
    [Tooltip("Vuruş izinin her karesinin süresi (sn).")]
    [Min(0.01f)] public float attackMarkFrameSeconds = 0.05f;
    [Tooltip("Vuruş izinin boyu, yaratığın sprite boyuna göre kat.")]
    [Min(0.1f)] public float attackMarkScale = 1f;
    [Tooltip("Hedef bilinmiyorsa iz, saldırı yönünde yaratığın bu kadar önünde çıkar (birim; devde sprite boyuyla çarpılır).")]
    [Min(0f)] public float attackMarkForwardOffset = 0.6f;
    [Tooltip("Ağda istemci, vuruş anında yaratığa bu kadar yakın (yaratık + oyuncu yarıçapına ek, birim) en yakın karakteri hedef sayar.")]
    [Min(0f)] public float attackMarkTargetSearchMargin = 0.6f;

    [Header("Vuruş hissi (yalnızca görüntü ve ses; kural ve hasar zamanı değişmez, Time.timeScale'e dokunulmaz)")]
    [Tooltip("Vuruş donması: vuran yaratığın ve vurulan karakterin görüntüsü bu kadar durur (sn).")]
    [Min(0f)] public float hitStopSeconds = 0.07f;
    [Tooltip("Dev (boss) vuruşunda donma (sn).")]
    [Min(0f)] public float hitStopGiantSeconds = 0.12f;
    [Tooltip("Eleme vuruşunda donma (vuran yaratık; sn).")]
    [Min(0f)] public float hitStopDeathSeconds = 0.18f;
    [Tooltip("Boss'a vurulunca boss'un donması (sn).")]
    [Min(0f)] public float hitStopBossHurtSeconds = 0.06f;
    [Tooltip("İtişte (Boşluk) iten ve itilenlerin donması (sn).")]
    [Min(0f)] public float hitStopShoveSeconds = 0.05f;
    [Tooltip("Donma sırasında vurulanın vuruş yönünde titremesi (birim).")]
    [Min(0f)] public float hitStopJitter = 0.035f;
    [Tooltip("Yerel oyuncu vurulunca kamera takibinin donması (sn).")]
    [Min(0f)] public float hitLocalFollowFreezeSeconds = 0.06f;
    [Tooltip("Vurulan karakter, bu kadar yakınındaki (birim) yaklaşan yaratık darbesini kendi vuruşu sayar ve tepkisini darbe anına denk getirir.")]
    [Min(0f)] public float hitImpactMatchRadius = 1.3f;
    [Tooltip("Dev vuruşunda eşleşme yarıçapının katı.")]
    [Min(1f)] public float hitImpactGiantRadiusMultiplier = 2.5f;
    [Tooltip("Can düşmeden en fazla bu kadar önce görünmüş darbe de eşleşir (ağda gecikme için; sn).")]
    [Min(0f)] public float hitImpactLookBackSeconds = 0.15f;
    [Tooltip("Tepki darbe anını en fazla bu kadar bekler (sn).")]
    [Min(0f)] public float hitImpactMaxWaitSeconds = 0.35f;

    [Tooltip("Beyaz parlama süresi (1-2 kare; sn).")]
    [Min(0f)] public float hitFlashWhiteSeconds = 0.035f;
    [Tooltip("Beyazdan sonra sönen renk tonunun süresi (sn).")]
    [Min(0f)] public float hitFlashTintSeconds = 0.22f;
    [Tooltip("Hasar alınca beyazdan sonraki renk tonu.")]
    public Color hitFlashTintColor = new Color(1f, 0.2f, 0.16f, 1f);
    [Tooltip("Renk tonunun en güçlü hâli (0-1).")]
    [Range(0f, 1f)] public float hitFlashTintStrength = 0.75f;
    [Tooltip("Kalkan vuruşu engelleyince beyazdan sonraki renk tonu.")]
    public Color hitFlashBlockColor = new Color(0.55f, 0.95f, 1f, 1f);

    [Tooltip("Vurulanın vuruş yönünde basılma oranı (0,2 = %80'e iner).")]
    [Range(0f, 0.6f)] public float hitSquash = 0.2f;
    [Tooltip("Vurulanın yanlara şişme oranı (0,25 = %125'e çıkar).")]
    [Range(0f, 0.6f)] public float hitBulge = 0.25f;
    [Tooltip("Ezilmenin geri yaylanma sıklığı (saniyede tur).")]
    [Min(0.1f)] public float hitSquashFrequency = 4.5f;
    [Tooltip("Ezilme yayının sönümü (1'den küçük = aşıp geri seker).")]
    [Range(0f, 1.5f)] public float hitSquashDamping = 0.35f;
    [Tooltip("Atılan (pounce) yaratığın gidiş yönünde uzama oranı.")]
    [Range(0f, 0.6f)] public float creatureLungeStretch = 0.22f;

    [Tooltip("Ekran sarsıntısının en büyük kayması (birim).")]
    [Min(0f)] public float cameraShakeMaxOffset = 0.6f;
    [Tooltip("Sarsıntı gürültüsünün hızı (saniyede).")]
    [Min(0.1f)] public float cameraShakeFrequency = 28f;
    [Tooltip("Sarsıntı şiddeti = travma ^ bu üs (2 = karesi).")]
    [Min(1f)] public float cameraShakeExponent = 2f;
    [Tooltip("Kaynak kameraya bu kadar yakınsa (birim) sarsıntı tam.")]
    [Min(0f)] public float cameraShakeFullDistance = 4f;
    [Tooltip("Kaynak bu kadar uzaksa (birim) sarsıntı yok.")]
    [Min(0f)] public float cameraShakeZeroDistance = 16f;
    [Tooltip("Kameranın yön itmesi (kick) yayının sıklığı (saniyede tur).")]
    [Min(0.1f)] public float cameraKickFrequency = 4.5f;
    [Tooltip("Kamera itme yayının sönümü.")]
    [Range(0f, 1.5f)] public float cameraKickDamping = 0.5f;
    [Tooltip("Kamera itmesinin en büyük kayması (birim).")]
    [Min(0f)] public float cameraKickMax = 0.45f;
    [Tooltip("Yerel oyuncu vurulunca kameranın vuruş yönünde itilmesi (birim).")]
    [Min(0f)] public float cameraKickLocalHit = 0.16f;
    [Tooltip("Dev vuruşunda kamera itmesi (birim).")]
    [Min(0f)] public float cameraKickGiantHit = 0.28f;
    [Tooltip("Eleme anında kamera itmesi (birim).")]
    [Min(0f)] public float cameraKickDeath = 0.32f;
    [Tooltip("Eleme anında eklenen sarsıntı travması (0-1).")]
    [Range(0f, 1f)] public float cameraTraumaDeath = 0.45f;
    [Tooltip("Boss ölünce eklenen sarsıntı travması (0-1).")]
    [Range(0f, 1f)] public float cameraTraumaBossDeath = 0.7f;
    [Tooltip("Boss'a vurulunca eklenen sarsıntı travması (0-1).")]
    [Range(0f, 1f)] public float cameraTraumaBossHurt = 0.25f;
    [Tooltip("Patlamada kamera itmesi (birim).")]
    [Min(0f)] public float cameraKickExplosion = 0.3f;
    [Tooltip("Patlamada eklenen sarsıntı travması (0-1).")]
    [Range(0f, 1f)] public float cameraTraumaExplosion = 0.5f;
    [Tooltip("İtişte kameranın itiş yönünde itilmesi (birim; birini ittiysen 2 katı).")]
    [Min(0f)] public float cameraKickShove = 0.06f;
    [Tooltip("Sarsıntılarda travma sönme süresi (sn).")]
    [Min(0.05f)] public float cameraTraumaSeconds = 0.45f;

    [Tooltip("Hasar sayısının ömrü (sn).")]
    [Min(0.1f)] public float damageNumberSeconds = 0.75f;
    [Tooltip("Hasar sayısının yukarı fırlama hızı (birim/sn).")]
    [Min(0f)] public float damageNumberRiseSpeed = 3.4f;
    [Tooltip("Hasar sayısının yana savrulma hızı (en çok; birim/sn).")]
    [Min(0f)] public float damageNumberSideSpeed = 0.8f;
    [Tooltip("Hasar sayısını aşağı çeken yerçekimi (birim/sn²).")]
    [Min(0f)] public float damageNumberGravity = 9f;
    [Tooltip("Hasar sayısı karakterin merkezinden bu kadar yukarıda çıkar (birim).")]
    [Min(0f)] public float damageNumberHeight = 0.55f;
    [Tooltip("Hasar sayısının yazı boyutu (11'in katı net piksel verir).")]
    [Min(1)] public int damageNumberFontSize = 11;
    [Tooltip("Hasar sayısında bir yazı tipi pikselinin dünyadaki boyu (birim).")]
    [Min(0.005f)] public float damageNumberPixelSize = 0.0625f;
    [Tooltip("Hasar sayısı ilk çıktığında bu kadar büyük görünür, hızla normale iner.")]
    [Min(1f)] public float damageNumberPopScale = 1.7f;
    [Tooltip("Büyükten normale inme süresi (sn).")]
    [Min(0f)] public float damageNumberPopSeconds = 0.1f;
    [Tooltip("Ömrün son bu kadarında söner (0-1).")]
    [Range(0f, 1f)] public float damageNumberFadeFraction = 0.35f;
    [Tooltip("Hasar sayısının rengi.")]
    public Color damageNumberColor = new Color(1f, 0.32f, 0.26f, 1f);
    [Tooltip("Kendi hasarının rengi (daha parlak).")]
    public Color damageNumberLocalColor = new Color(1f, 0.85f, 0.3f, 1f);
    [Tooltip("Kalkan engelleyince \"BLOK\" yazısının rengi.")]
    public Color damageNumberBlockColor = new Color(0.55f, 0.95f, 1f, 1f);

    [Tooltip("Yerel oyuncu vurulunca ekran kenarının kırmızı parlaması (sn).")]
    [Min(0f)] public float hurtFlashSeconds = 0.35f;
    [Tooltip("Kırmızı parlamanın en koyu hâli (0-1).")]
    [Range(0f, 1f)] public float hurtFlashMaxAlpha = 0.6f;
    [Tooltip("Kırmızı parlamanın kalınlığı, ekranın kısa kenarına göre.")]
    [Range(0.01f, 0.6f)] public float hurtFlashThickness = 0.3f;
    [Tooltip("Can yazısının sallanma süresi (sn).")]
    [Min(0f)] public float healthShakeSeconds = 0.4f;
    [Tooltip("Can yazısının sallanma genliği (piksel).")]
    [Min(0f)] public float healthShakePixels = 9f;
    [Tooltip("Can yazısının hasar anındaki rengi.")]
    public Color healthHurtColor = new Color(1f, 0.25f, 0.2f, 1f);
    [Tooltip("Can bu kadara (ya da altına) inince düşük can nabzı başlar.")]
    [Min(1)] public int lowHealthThreshold = 1;
    [Tooltip("Düşük can kalp atışı (dakikada).")]
    [Min(1f)] public float lowHealthHeartbeatBpm = 72f;
    [Tooltip("Düşük can nabzının en koyu hâli (0-1).")]
    [Range(0f, 1f)] public float lowHealthPulseAlpha = 0.32f;
    [Tooltip("Kalp atışı için kullanılan mevcut ses (ayrı kalp sesi yok; boş = sessiz).")]
    public string lowHealthHeartbeatSound = "sfx_dice_land";
    [Tooltip("Kalp atışı sesinin perdesi (düşük = boğuk güm).")]
    [Range(0.1f, 3f)] public float lowHealthHeartbeatPitch = 0.42f;
    [Tooltip("Kalp atışı sesinin düzeyi.")]
    [Range(0f, 1f)] public float lowHealthHeartbeatVolume = 0.3f;

    [Tooltip("Elenen karakterin kan/parça patlamasının katı (1 = eski).")]
    [Min(1f)] public float deathGorePower = 1.5f;
    [Tooltip("Tek kişilik maçta son elemede ağır çekimin zaman çarpanı (yalnızca tek kişilik; ağda yok).")]
    [Range(0.05f, 1f)] public float finalSlowMotionScale = 0.3f;
    [Tooltip("Ağır çekimin süresi (gerçek zaman, sn).")]
    [Min(0f)] public float finalSlowMotionSeconds = 0.8f;
    [Tooltip("Ağır çekimin son bu kadarında zaman yumuşakça normale döner (0-1).")]
    [Range(0f, 1f)] public float finalSlowMotionEaseFraction = 0.35f;

    [Tooltip("Vuruş seslerinin perde oynaması (±; 0,08 = %8).")]
    [Range(0f, 0.5f)] public float hitSoundPitchVariation = 0.08f;
    [Tooltip("Aynı vuruş sesi en sık bu aralıkla çalar (sn); art arda vuruşlar duyulsun ama üst üste binmesin.")]
    [Min(0f)] public float hitSoundCooldown = 0.05f;
    [Tooltip("Vuruşun altına eklenen kısa, düşük \"güm\" sesi (mevcut seslerden).")]
    public string hitThumpSound = "sfx_dice_land";
    [Tooltip("Güm sesinin perdesi.")]
    [Range(0.1f, 3f)] public float hitThumpPitch = 0.55f;
    [Tooltip("Güm sesinin düzeyi (ana vuruşa göre).")]
    [Range(0f, 1f)] public float hitThumpVolume = 0.65f;
    [Tooltip("Ağır vuruşta (dev, eleme) eklenen üçüncü katman.")]
    public string hitHeavySound = "sfx_explosion";
    [Tooltip("Ağır katmanın perdesi (yüksek = kısa, sert).")]
    [Range(0.1f, 3f)] public float hitHeavyPitch = 1.5f;
    [Tooltip("Ağır katmanın düzeyi.")]
    [Range(0f, 1f)] public float hitHeavyVolume = 0.35f;
    [Tooltip("İtişte itilen olursa çalan güm sesinin düzeyi.")]
    [Range(0f, 1f)] public float shoveThumpVolume = 0.5f;
    [Tooltip("İtişte itilenin ayağından kalkan tozun boyu.")]
    [Min(0f)] public float shoveDustSize = 0.5f;

    [Tooltip("Ayarlarda ekran sarsıntısı kapalıyken ekran parlamaları bu kadar yumuşar (0-1).")]
    [Range(0f, 1f)] public float reducedScreenFlashFactor = 0.4f;

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

    [Tooltip("Yakın dövüş silahı (melee: kılıç, sopa) savurmadan önceki hazırlık (sn). Bu sürede silah parlar ve önündeki yay görünür; hedef kaçabilir.")]
    [Min(0f)] public float swordWindupSeconds = 0.2f;

    [Tooltip("Yakın dövüş silahının bir vuruşundan sonra yeniden savurmak için bekleme (sn).")]
    [Min(0f)] public float swordSwingCooldownSeconds = 0.8f;

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

    // ---- Arayüz cilası (menü, geçiş, zar, yazı kutusu, maç sonu). Pikseller 1920×1080 ekrana göredir. ----

    [Header("Arayüz cilası")]
    [Tooltip("Sahne geçişindeki piksel kararma/açılma süresi (sn, her yön için).")]
    [Min(0f)] public float uiFadeSeconds = 0.35f;
    [Tooltip("Geçişteki piksel karelerin boyu (1080 yükseklikli ekrana göre).")]
    [Min(4f)] public float uiFadeCellPixels = 40f;
    public Color uiFadeColor = Color.black;

    [Tooltip("Menü düğmesinin genişliği ve yüksekliği.")]
    [Min(100f)] public float uiMenuButtonWidth = 560f;
    [Min(30f)] public float uiMenuButtonHeight = 78f;
    [Tooltip("Menü düğmeleri arasındaki boşluk.")]
    [Min(0f)] public float uiMenuButtonSpacing = 16f;
    [Min(1)] public int uiMenuFontSize = 38;
    [Min(1)] public int uiMenuSmallFontSize = 24;
    public Color uiMenuTextColor = new Color(0.2f, 0.13f, 0.08f, 1f);
    [Tooltip("Üzerine gelinen düğmenin yazı rengi.")]
    public Color uiMenuHoverTextColor = new Color(0.75f, 0.22f, 0.1f, 1f);
    [Tooltip("Düğme resmi yoksa düz renk.")]
    public Color uiMenuButtonFallbackColor = new Color(0.98f, 0.93f, 0.78f, 1f);
    [Tooltip("Üzerine gelince düğmenin büyüme ölçeği.")]
    [Min(1f)] public float uiMenuHoverScale = 1.08f;
    [Tooltip("Basılıyken düğmenin ölçeği.")]
    [Range(0.5f, 1f)] public float uiMenuPressScale = 0.93f;
    [Tooltip("Düğme yayının sertliği (büyük = hızlı).")]
    [Min(1f)] public float uiSpringStiffness = 420f;
    [Tooltip("Düğme yayının sönümü (küçük = daha çok sallanır).")]
    [Min(0f)] public float uiSpringDamping = 20f;
    [Tooltip("Düğmenin üzerine gelince çalan hafif tık sesinin düzeyi (0 = sessiz).")]
    [Range(0f, 1f)] public float uiMenuHoverVolume = 0.2f;

    [Tooltip("Logonun ekran yüksekliğine oranı; logo bu boya en yakın TAM SAYI katında büyütülür (piksel net kalsın).")]
    [Range(0.05f, 0.5f)] public float uiLogoHeightRatio = 0.2f;
    [Tooltip("Logonun üst kenardan uzaklığı (ekran yüksekliği oranı).")]
    [Range(0f, 0.5f)] public float uiLogoTopRatio = 0.07f;
    [Tooltip("Logonun yukarı-aşağı süzülme miktarı (logo pikseli).")]
    [Min(0)] public int uiLogoBobPixels = 2;
    [Tooltip("Logonun süzülme hızı (saniyede kaç kez).")]
    [Min(0f)] public float uiLogoBobSpeed = 0.5f;

    [Tooltip("Menü arka planının iki karo rengi.")]
    public Color uiBackdropColorA = new Color(0.10f, 0.11f, 0.20f, 1f);
    public Color uiBackdropColorB = new Color(0.13f, 0.14f, 0.25f, 1f);
    [Min(8f)] public float uiBackdropTileSize = 72f;
    [Tooltip("Karo deseninin kayma hızı (piksel/sn).")]
    public float uiBackdropScrollSpeed = 14f;
    [Min(0)] public int uiBackdropLetterCount = 26;
    [Tooltip("Uçuşan harflerin yükselme hızı (piksel/sn).")]
    public float uiBackdropLetterSpeed = 26f;
    [Min(1f)] public float uiBackdropLetterMinSize = 40f;
    [Min(1f)] public float uiBackdropLetterMaxSize = 120f;
    public Color uiBackdropLetterColor = new Color(1f, 0.85f, 0.35f, 1f);
    [Range(0f, 1f)] public float uiBackdropLetterAlpha = 0.14f;
    [Tooltip("Çok oyunculu sayfasında Steam kutusunun genişliği.")]
    [Min(100f)] public float uiOnlinePanelWidth = 420f;

    [Tooltip("Ortadaki büyük duyuru yazısının (HAZIR!, SIRA SENDE!, oyuncu adı) boyu.")]
    [Min(1)] public int uiBannerFontSize = 120;
    [Tooltip("Duyurunun ekran ortasından yukarı kayıklığı.")]
    public float uiBannerYOffset = 170f;
    [Min(0f)] public float uiBannerInSeconds = 0.35f;
    [Min(0f)] public float uiBannerHoldSeconds = 0.9f;
    [Min(0f)] public float uiBannerOutSeconds = 0.3f;
    [Tooltip("Duyurunun başlangıç ölçeği (küçükten büyüyerek girer).")]
    [Min(0f)] public float uiBannerStartScale = 0.2f;
    [Tooltip("Girişte hedefi aşma miktarı (OutBack).")]
    [Min(0f)] public float uiBannerOvershoot = 2.4f;
    [Tooltip("Çıkarken yukarı kayma mesafesi.")]
    public float uiBannerSlideDistance = 120f;
    [Tooltip("\"SIRA SENDE!\" sarsıntısının genliği (piksel) ve sıklığı (Hz).")]
    [Min(0f)] public float uiBannerShakePixels = 10f;
    [Min(0f)] public float uiBannerShakeFrequency = 18f;
    public Color uiBannerReadyColor = new Color(0.55f, 1f, 0.6f, 1f);
    public Color uiBannerYourTurnColor = new Color(1f, 0.88f, 0.2f, 1f);
    public Color uiBannerWriterColor = Color.white;

    [Tooltip("Akış yazılarının (\"X yazdı: Kurt\", tehdit uyarısı) giriş süresi, başlangıç ölçeği ve taşması.")]
    [Min(0f)] public float uiTextPunchSeconds = 0.3f;
    [Min(0f)] public float uiTextPunchStartScale = 0.4f;
    [Min(0f)] public float uiTextPunchOvershoot = 2f;
    [Tooltip("Akış yazılarının yukarı kayarak çıkış süresi ve mesafesi.")]
    [Min(0f)] public float uiTextSlideOutSeconds = 0.3f;
    public float uiTextSlideOutDistance = 50f;

    [Tooltip("Zar simgesinin boyu ve dönerken attığı tur sayısı (sonda yavaşlar).")]
    [Min(8f)] public float uiDiceSize = 92f;
    [Min(0f)] public float uiDiceSpinTurns = 4f;
    [Tooltip("İsim her değiştiğinde zar yazısının küçük sıçraması (ölçek artışı) ve süresi.")]
    [Min(0f)] public float uiDiceTickBump = 0.1f;
    [Min(0f)] public float uiDiceTickSeconds = 0.12f;
    [Tooltip("Zar durunca büyük sıçrama (ölçek artışı) ve süresi.")]
    [Min(0f)] public float uiDiceLandBump = 0.5f;
    [Min(0f)] public float uiDiceLandSeconds = 0.55f;
    [Tooltip("Zar durduktan sonra simgenin ekranda kalma süresi.")]
    [Min(0f)] public float uiDiceLingerSeconds = 0.7f;
    public Color uiDiceColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    public Color uiDicePipColor = new Color(0.15f, 0.1f, 0.08f, 1f);

    [Tooltip("Yazılan her harfin küçükten büyüye \"pop\" süresi, başlangıç ölçeği ve taşması.")]
    [Min(0f)] public float uiLetterPopSeconds = 0.14f;
    [Min(0f)] public float uiLetterPopStartScale = 0.3f;
    [Min(0f)] public float uiLetterPopOvershoot = 2.6f;
    [Tooltip("Her tuşta kutunun titremesi (piksel, sn).")]
    [Min(0f)] public float uiKeyJitterPixels = 3f;
    [Min(0f)] public float uiKeyJitterSeconds = 0.08f;
    [Tooltip("Reddedilen kelimede kutunun sağa-sola sallanması (piksel, sn, Hz).")]
    [Min(0f)] public float uiRejectShakePixels = 24f;
    [Min(0f)] public float uiRejectShakeSeconds = 0.4f;
    [Min(0f)] public float uiShakeFrequency = 14f;
    public Color uiRejectColor = new Color(0.75f, 0.1f, 0.08f, 0.9f);
    public Color uiAcceptColor = new Color(0.15f, 0.7f, 0.25f, 0.9f);
    [Tooltip("Kırmızı/yeşil parlamanın sönme süresi (sn).")]
    [Min(0f)] public float uiFlashSeconds = 0.4f;
    [Tooltip("Kabul edilen kelimenin kutudan yukarı uçma süresi ve mesafesi.")]
    [Min(0f)] public float uiWordFlySeconds = 0.8f;
    public float uiWordFlyDistance = 280f;
    [Tooltip("Kutunun açılış (punch) ve kapanış süresi.")]
    [Min(0f)] public float uiTypingOpenSeconds = 0.25f;
    [Min(0f)] public float uiTypingCloseSeconds = 0.3f;
    [Tooltip("Sayacın kırmızı nabız ve tik sesine başladığı son saniyeler.")]
    [Min(0)] public int uiCountdownWarnSeconds = 3;
    public Color uiCountdownColor = new Color(1f, 0.25f, 0.2f, 1f);
    [Tooltip("Tik anında sayaç yazısının büyüme oranı.")]
    [Min(0f)] public float uiCountdownPulseScale = 0.3f;
    [Tooltip("Tik sesi (yazı tuşu sesi) perdesi ve düzeyi.")]
    [Range(0.1f, 3f)] public float uiCountdownTickPitch = 0.6f;
    [Range(0f, 1f)] public float uiCountdownTickVolume = 0.7f;

    [Tooltip("Maç sonu başlığının giriş süresi ve başlangıç ölçeği.")]
    [Min(0f)] public float uiEndTitleSeconds = 0.5f;
    [Min(0f)] public float uiEndTitleStartScale = 0.2f;
    [Tooltip("Maç sonu satırlarının arka arkaya belirme aralığı ve her birinin belirme süresi.")]
    [Min(0f)] public float uiEndRowDelay = 0.22f;
    [Min(0f)] public float uiEndRowFadeSeconds = 0.25f;
    public Color uiWinTitleColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color uiLoseTitleColor = new Color(0.9f, 0.45f, 0.4f, 1f);
    [Tooltip("Maç sonu düğmelerinin boyu ve aralığı.")]
    [Min(50f)] public float uiEndButtonWidth = 380f;
    [Min(30f)] public float uiEndButtonHeight = 70f;
    [Min(0f)] public float uiEndButtonGap = 28f;
    [Tooltip("Zafer konfetisi: parça sayısı, boyu, hız aralığı, yerçekimi, hava direnci.")]
    [Min(0)] public int uiConfettiCount = 160;
    [Min(1f)] public float uiConfettiPieceSize = 10f;
    [Min(0f)] public float uiConfettiMinSpeed = 200f;
    [Min(0f)] public float uiConfettiMaxSpeed = 700f;
    public float uiConfettiGravity = 260f;
    [Min(0f)] public float uiConfettiDrag = 2.2f;
    public Color[] uiConfettiColors =
    {
        new Color(1f, 0.85f, 0.2f), new Color(0.95f, 0.3f, 0.3f), new Color(0.35f, 0.8f, 1f),
        new Color(0.5f, 0.95f, 0.45f), new Color(0.9f, 0.5f, 1f),
    };

    // ---- Bot adları: tek kişilikte gerçek oyuncu gibi takma adlar, ağ maçında açıkça "Bot N" + rozet. ----

    [Header("Bot adları")]
    [Tooltip("Tek kişilik (ağsız) maçta botlar bot_names.json'dan takma ad alsın mı? Kapalıysa \"Bot 1\", \"Bot 2\"...")]
    public bool singlePlayerFakeBotNames = true;
    [Tooltip("Takma ad listesinin Resources yolu (uzantısız).")]
    public string botNamesPath = BotNameGenerator.DefaultResourcePath;
    [Tooltip("Listedeki bir adın en kısa ve en uzun hali (harf). Bunun dışındakiler kullanılmaz.")]
    [Min(1)] public int botNameMinLength = 3;
    [Min(1)] public int botNameMaxLength = 16;
    [Tooltip("Ağ maçında botların isim etiketinde adın yanında [BOT] rozeti çıksın mı? (Yazı dil dosyasında: nametag.bot_badge.)")]
    public bool onlineBotTagEnabled = true;
    [Tooltip("Ağ maçında botların isim etiketi rengi (insanlarınkinden ayrılsın diye gri).")]
    public Color onlineBotNameColor = new Color(0.68f, 0.7f, 0.74f, 1f);

    [Header("Yazı kutusu")]
    [Tooltip("Yazı kutusu ölçüleri 'resim pikseli' cinsindendir; ekran yüksekliği bu kadar olunca 1 resim pikseli = 1 ekran pikseli. 1080p'de 4 kat.")]
    [Min(60f)] public float typeBoxReferenceHeight = 270f;
    [Tooltip("Kutunun ortasının ekrandaki yüksekliği (0 = tepe, 1 = alt). Steam görselindeki gibi ortanın biraz altında.")]
    [Range(0.2f, 0.95f)] public float typeBoxCenterY = 0.66f;
    [Tooltip("Kutunun eni ve boyu (resim pikseli).")]
    [Min(20f)] public float typeBoxWidthPixels = 118f;
    [Min(8f)] public float typeBoxHeightPixels = 20f;
    [Tooltip("Çerçeve resminde 9 dilim kenar payı yoksa kullanılacak pay (resim pikseli).")]
    [Min(0)] public int typeBoxBorderPixels = 3;
    [Tooltip("Kutunun içindeki yazının boyu ve sol boşluğu (resim pikseli). Uzun kelimede yazı en çok 'typeBoxMinFontScale' katına küçülür.")]
    [Min(4f)] public float typeBoxFontPixels = 13f;
    [Min(0f)] public float typeBoxTextPaddingPixels = 5f;
    [Range(0.2f, 1f)] public float typeBoxMinFontScale = 0.45f;
    [Tooltip("Sıra başkasındayken ve zar atılırken kutunun ölçeği ve saydamlığı; hal değişiminde geçiş sertliği.")]
    [Range(0.3f, 1f)] public float typeBoxWatchScale = 0.72f;
    [Range(0.1f, 1f)] public float typeBoxWatchAlpha = 0.8f;
    [Min(0.5f)] public float typeBoxModeSharpness = 14f;
    [Tooltip("Sıra sendeyken kutunun çevresinde nefes alan parıltı: kalınlık (resim pikseli), renk, hız (Hz).")]
    [Min(0f)] public float typeBoxGlowPixels = 2f;
    public Color typeBoxGlowColor = new Color(1f, 0.9f, 0.35f, 0.6f);
    [Min(0f)] public float typeBoxBreatheSpeed = 0.9f;
    [Tooltip("Resimler yoksa çizilen kutu: Steam görselindeki gibi beyaz yüz, açık gri alt şerit, açık mavi üst çizgi, koyu kenar.")]
    public Color typeBoxFaceColor = Color.white;
    public Color typeBoxShadeColor = new Color(0.78f, 0.82f, 0.86f, 1f);
    public Color typeBoxHighlightColor = new Color(0.75f, 0.92f, 1f, 1f);
    public Color typeBoxOutlineColor = new Color(0.1f, 0.11f, 0.17f, 1f);
    [Tooltip("Yazı renkleri: kendi yazdığın, başkasının 'yazıyor...' yazısı, reddedilen harfler.")]
    public Color typeBoxTextColor = new Color(0.1f, 0.11f, 0.17f, 1f);
    public Color typeBoxWatchTextColor = new Color(0.33f, 0.35f, 0.45f, 1f);
    public Color typeBoxBadTextColor = new Color(0.89f, 0.23f, 0.27f, 1f);
    [Tooltip("Reddedilen/kabul edilen kelimede kutu kenarı (resim yoksa) bu renge döner.")]
    public Color typeBoxBadColor = new Color(0.89f, 0.23f, 0.27f, 1f);
    public Color typeBoxOkColor = new Color(0.3f, 0.85f, 0.4f, 1f);
    [Tooltip("Kırmızı/yeşil kutu resminin ekranda kaldığı süre (sn).")]
    [Min(0f)] public float typeBoxResultSeconds = 0.5f;
    [Tooltip("İmleç: yanıp sönme yarı periyodu (sn) ve kalınlığı (resim pikseli).")]
    [Min(0.05f)] public float typeBoxCaretBlinkSeconds = 0.45f;
    [Min(0.5f)] public float typeBoxCaretWidthPixels = 2f;
    [Tooltip("Sayaç çubuğu: kutuya uzaklığı ve kalınlığı (resim pikseli).")]
    [Min(0f)] public float typeBoxTimerGapPixels = 2f;
    [Min(1f)] public float typeBoxTimerHeightPixels = 6f;
    [Tooltip("Sayaç dolgusunun çerçeveden içeri payı (resim pikseli).")]
    [Min(0f)] public float typeBoxTimerInsetPixels = 2f;
    [Tooltip("Sayaç: normalde ui_timer_fill_gold (Steam görselindeki sarı-turuncu); resim yoksa ya da süre azalınca beyaz dolgu bu renklere boyanır: dolu → yeşil, 'YellowAt' oranında sarı, 'RedAt' oranında kırmızı.")]
    public Color typeBoxTimerGreen = new Color(0.35f, 0.85f, 0.35f, 1f);
    public Color typeBoxTimerYellow = new Color(0.99f, 0.8f, 0.25f, 1f);
    public Color typeBoxTimerRed = new Color(0.9f, 0.22f, 0.2f, 1f);
    [Range(0f, 1f)] public float typeBoxTimerYellowAt = 0.55f;
    [Range(0f, 1f)] public float typeBoxTimerRedAt = 0.3f;
    public Color typeBoxTimerBackColor = new Color(0.2f, 0.21f, 0.29f, 1f);
    [Tooltip("Son saniyelerde (uiCountdownWarnSeconds) sayaç ve kutu titremesi (resim pikseli).")]
    [Min(0f)] public float typeBoxTimerShakePixels = 1.2f;
    [Tooltip("Zarlar: boyu, kutu köşesine göre eğikliği (derece), dönerken kare hızı, durunca sıçrama.")]
    [Min(4f)] public float typeBoxDieSizePixels = 24f;
    public float typeBoxDieTiltDegrees = 12f;
    [Min(1f)] public float typeBoxDieRollFps = 16f;
    [Min(0f)] public float typeBoxDieLandBump = 0.45f;
    [Min(0f)] public float typeBoxDieLandSeconds = 0.4f;
    [Tooltip("Tuş kıvılcımı ve zar kıvılcımı: boy (resim pikseli) ve kare süresi (sn).")]
    [Min(1f)] public float typeBoxSparkSizePixels = 16f;
    [Min(0.01f)] public float typeBoxSparkFrameSeconds = 0.05f;
    [Tooltip("Tuş sesinin perdesi harfe göre 1 ± bu kadar, bu kadar basamakta değişir.")]
    [Range(0f, 0.5f)] public float typeBoxKeyPitchSpread = 0.08f;
    [Min(1)] public int typeBoxKeyPitchSteps = 5;
    [Tooltip("Silinen harf: düşüş süresi (sn) ve yerçekimi (resim pikseli/sn²).")]
    [Min(0.05f)] public float typeBoxFallSeconds = 0.45f;
    [Min(0f)] public float typeBoxFallGravityPixels = 160f;
    [Tooltip("Reddedilen kelimede harflerin dağılma mesafesi (resim pikseli) ve süresi (sn).")]
    [Min(0f)] public float typeBoxScatterPixels = 9f;
    [Min(0.05f)] public float typeBoxScatterSeconds = 0.5f;
    [Tooltip("Kabulde kutudan çıkan patlama: boy (resim pikseli), kare süresi (sn).")]
    [Min(1f)] public float typeBoxBurstSizePixels = 60f;
    [Min(0.01f)] public float typeBoxBurstFrameSeconds = 0.06f;
    [Tooltip("'X yazıyor...' noktalarının değişme aralığı (sn).")]
    [Min(0.05f)] public float typeBoxDotsPeriodSeconds = 0.35f;
    [Tooltip("Kutunun çevresinde süzülen beyaz parıltılar: sayı, yayılma (resim pikseli), hız (Hz).")]
    [Min(0)] public int typeBoxSparkleCount = 9;
    [Min(0f)] public float typeBoxSparkleSpreadPixels = 26f;
    [Min(0f)] public float typeBoxSparkleSpeed = 0.35f;
    [Tooltip("Kutunun üstündeki ipucu ve hata yazısının boyu (resim pikseli).")]
    [Min(3f)] public float typeBoxHintFontPixels = 7f;
    public Color typeBoxHintColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Kelime büyüsü")]
    [Tooltip("Harflerin kutudan sırayla kopma aralığı ve kutunun üstündeki bekleme sırasına yükselme süresi (sn).")]
    [Min(0f)] public float spellLeaveStagger = 0.045f;
    [Min(0f)] public float spellRiseSeconds = 0.18f;
    [Tooltip("Bekleyen harflerin kutunun ne kadar üstünde durduğu ve hafif inip kalkması (resim pikseli, Hz).")]
    [Min(0f)] public float spellHoverPixels = 16f;
    [Min(0f)] public float spellHoverBobPixels = 1.5f;
    [Min(0f)] public float spellHoverBobSpeed = 2.5f;
    [Tooltip("Hedef belliyken harf uçuş süresi ve sırayla kalkış aralığı (sn). Son harf hedef belli olduktan en geç 'MaxArrive' sn sonra varır.")]
    [Min(0.01f)] public float spellFlightSeconds = 0.38f;
    [Min(0f)] public float spellFlightStagger = 0.035f;
    [Min(0.05f)] public float spellMaxArriveSeconds = 0.5f;
    [Tooltip("Hedef gelmezse (ör. fareyle hedef seçme sürüyor) harflerin en çok bekleme süresi (sn); hedef seçme süresinden uzun olmalı.")]
    [Min(0.5f)] public float spellHoverMaxSeconds = 4.5f;
    [Tooltip("Kelime olayından hemen önce doğan yaratık da bu büyüye sayılır (sn).")]
    [Min(0f)] public float spellAppearBufferSeconds = 0.6f;
    [Tooltip("Yayın bükülmesi (yol uzunluğunun katı).")]
    [Range(0f, 1f)] public float spellArcBend = 0.3f;
    [Tooltip("Uçan harflerin boyu (resim pikseli), rengi ve kenarı.")]
    [Min(3f)] public float spellLetterFontPixels = 16f;
    public Color spellLetterColor = new Color(1f, 0.95f, 0.55f, 1f);
    public Color spellLetterOutlineColor = new Color(0.1f, 0.11f, 0.17f, 1f);
    [Tooltip("Harf izi: kaç parça, aralarındaki yol farkı (0..1), boy (resim pikseli), kare süresi (sn).")]
    [Min(0)] public int spellTrailCount = 4;
    [Range(0.005f, 0.2f)] public float spellTrailSpacing = 0.045f;
    [Min(1f)] public float spellTrailSizePixels = 12f;
    [Min(0.01f)] public float spellTrailFrameSeconds = 0.05f;
    [Tooltip("Varışta yerde açılan çağırma çemberi: boy (ölçek; 1 = 24 piksellik resim 1,5 birim), kare süresi (sn); resim yoksa bu renkte halka.")]
    [Min(0.1f)] public float spellSummonSize = 1.6f;
    [Min(0.01f)] public float spellSummonFrameSeconds = 0.06f;
    public Color spellSummonColor = new Color(0.75f, 0.92f, 1f, 0.8f);
    [Tooltip("Son harf varınca: beyaz flaş süresi (sn), ekran flaşı saydamlığı, yerdeki flaş boyu (birim).")]
    [Min(0f)] public float spellFlashSeconds = 0.16f;
    [Range(0f, 1f)] public float spellScreenFlashAlpha = 0.2f;
    [Min(0f)] public float spellWorldFlashSize = 2.6f;
    [Tooltip("Son harf varınca kamera itmesi (birim) ve sarsıntı (travma 0..1).")]
    [Min(0f)] public float spellKickDistance = 0.16f;
    [Range(0f, 1f)] public float spellTrauma = 0.12f;
    [Tooltip("Hedef kamera dışındaysa harfler ekran kenarının bu kadar içinde söner (resim pikseli).")]
    [Min(0f)] public float spellEdgeInsetPixels = 10f;
    [Tooltip("Büyük tehdit: ekran kenarındaki uyarı üçgenleri: süre (sn), sayı, boy (resim pikseli), kare süresi (sn), resim yoksa renk.")]
    [Min(0f)] public float spellThreatWarnSeconds = 2.6f;
    [Min(0)] public int spellThreatWarnCount = 10;
    [Min(2f)] public float spellThreatWarnSizePixels = 16f;
    [Min(0.01f)] public float spellThreatWarnFrameSeconds = 0.08f;
    public Color spellThreatWarnColor = new Color(1f, 0.3f, 0.2f, 0.9f);
    [Tooltip("Yaratık süresi bitip kaybolurken çıkan 'puf': boy (yaratık çapının katı), kare süresi (sn), en küçük yarıçap (birim).")]
    [Min(0.1f)] public float spellPoofSize = 1.6f;
    [Min(0.01f)] public float spellPoofFrameSeconds = 0.07f;
    [Min(0f)] public float spellPoofMinRadius = 0.2f;
    public Color spellPoofColor = new Color(0.9f, 0.9f, 0.95f, 0.7f);

    // ---- HUD: sol üstteki piksel gösterge (kalpler, itiş, durum simgeleri). Ölçüler resim pikseli; ekranda tam sayı katına büyür. ----

    [Header("HUD")]
    [Tooltip("Piksel ölçeğinin referans ekran yüksekliği: ekran bunun kaç katıysa (aşağı yuvarlanır) bir resim pikseli o kadar ekran pikseli olur.")]
    [Min(1)] public int hudReferenceHeight = 240;
    [Min(1)] public int hudMinScale = 1;
    [Min(1)] public int hudMaxScale = 6;
    [Tooltip("Panelin ekran kenarına uzaklığı, iç boşluğu ve satır aralığı (resim pikseli).")]
    [Min(0)] public int hudMargin = 4;
    [Min(0)] public int hudPadding = 5;
    [Min(0)] public int hudRowGap = 3;
    [Tooltip("ui_hud_panel resminin 9 dilim kenarı (resim pikseli).")]
    [Min(0)] public int hudPanelBorder = 4;
    public Color hudPanelFallbackColor = new Color(0.08f, 0.07f, 0.12f, 0.78f);
    public Color hudPanelFallbackEdge = new Color(0.55f, 0.5f, 0.42f, 0.9f);

    [Tooltip("Kalp boyu, aralığı, bir kalbin kaç can olduğu (2 ise yarım kalp çıkar), bir satırdaki en çok kalp.")]
    [Min(4)] public int hudHeartSize = 12;
    [Min(0)] public int hudHeartGap = 2;
    [Min(1)] public int hudHealthPerHeart = 1;
    [Min(1)] public int hudHeartsPerRow = 10;
    [Tooltip("Kaybedilen kalbin kırılma kareleri (ui_heart_break_0..3) süresi; ardından parçaların düşme süresi.")]
    [Min(0.01f)] public float hudHeartBreakSeconds = 0.36f;
    [Min(0f)] public float hudHeartShardSeconds = 0.6f;
    [Min(0)] public int hudHeartShardCount = 4;
    [Min(1)] public int hudHeartShardSize = 3;
    [Tooltip("Parçaların fırlama hızı ve yerçekimi (resim pikseli / sn).")]
    [Min(0f)] public float hudHeartShardSpeed = 45f;
    [Min(0f)] public float hudHeartShardGravity = 260f;
    [Tooltip("Can artınca kalbin büyüyerek dolma süresi.")]
    [Min(0.01f)] public float hudHeartFillSeconds = 0.35f;
    [Tooltip("Düşük canda son kalbin atışta büyüme oranı (kalp atışı HitFeelDirector.LowHealthPulse'tan).")]
    [Min(0f)] public float hudLowHealthBeatScale = 0.3f;
    public Color hudHeartFullColor = new Color(0.9f, 0.15f, 0.2f, 1f);
    public Color hudHeartEmptyColor = new Color(0.22f, 0.18f, 0.24f, 1f);

    [Tooltip("İtiş simgesinin boyu, durum simgelerinin boyu, aralık, altlarındaki süre çubuğunun kalınlığı.")]
    [Min(4)] public int hudIconSize = 12;
    [Min(4)] public int hudStatusIconSize = 10;
    [Min(0)] public int hudIconGap = 3;
    [Min(1)] public int hudBarHeight = 2;
    [Tooltip("ui_key_space tuş kapağının 9 dilim kenarları (sol/sağ, üst, alt), yüksekliği ve yazının yanlardaki boşluğu.")]
    [Min(0)] public int hudKeyCapBorder = 3;
    [Min(0)] public int hudKeyCapBorderTop = 2;
    [Min(0)] public int hudKeyCapBorderBottom = 4;
    [Min(4)] public int hudKeyCapHeight = 12;
    [Min(0)] public int hudKeyCapPadding = 3;
    [Tooltip("İtiş hazır olunca simgenin parlayıp büyümesi (\"pop\").")]
    [Min(0.01f)] public float hudShoveReadyPopSeconds = 0.3f;
    [Min(0f)] public float hudShoveReadyPopScale = 0.35f;
    [Tooltip("Bekleyen simgenin rengi (ui_icon_shove_cd yoksa normal simge bu renge boyanır).")]
    public Color hudCooldownTint = new Color(0.45f, 0.45f, 0.5f, 1f);
    public Color hudBarBackColor = new Color(0f, 0f, 0f, 0.6f);
    public Color hudBarFillColor = new Color(0.95f, 0.85f, 0.35f, 1f);
    public Color hudBarWarnColor = new Color(1f, 0.3f, 0.25f, 1f);
    public Color hudReadyColor = new Color(0.55f, 1f, 0.55f, 1f);

    [Tooltip("Etkinin bitmesine bu kadar saniye kala simge yanıp söner; saniyede kaç kez.")]
    [Min(0f)] public float hudStatusWarnSeconds = 2f;
    [Min(0f)] public float hudStatusBlinkHz = 4f;
    [Tooltip("Sayı rozetinin rengi (kalkan bloğu, silah atışı).")]
    public Color hudBadgeColor = new Color(0.12f, 0.1f, 0.16f, 0.95f);
    public Color hudTextColor = new Color(1f, 0.97f, 0.9f, 1f);
    public Color hudDimTextColor = new Color(0.75f, 0.78f, 0.9f, 1f);
    public Color hudGhostColor = new Color(0.75f, 0.85f, 1f, 0.85f);
    [Tooltip("Simge resmi yoksa çizilecek düz kare renkleri, HudStatusKind sırasıyla: kalkan, silah, araç, uçma, kılık, görünmezlik, yanma, yavaş, çekilme.")]
    public Color[] hudStatusFallbackColors =
    {
        new Color(0.45f, 0.9f, 1f), new Color(0.85f, 0.85f, 0.85f), new Color(0.95f, 0.6f, 0.2f),
        new Color(1f, 1f, 0.7f), new Color(0.4f, 0.75f, 0.3f), new Color(0.6f, 0.6f, 0.75f),
        new Color(1f, 0.45f, 0.1f), new Color(0.4f, 0.55f, 1f), new Color(0.8f, 0.4f, 1f),
    };
    [Tooltip("Küçük yazıların (saniye, rozet, seçilme şansı) ölçek düşüşü: küçük yazı = piksel ölçeği eksi bu (en az 1).")]
    [Min(0)] public int hudSmallTextScaleDrop = 1;
}
