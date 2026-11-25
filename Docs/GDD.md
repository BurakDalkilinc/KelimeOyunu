# Oyun Tasarım Dokümanı (GDD)

## Özet
- **Tür:** Çok oyunculu parti oyunu, son kalan kazanır. PC / Steam.
- **Oyuncu:** 4–20, eksikleri botlar doldurur.
- **Maç süresi hedefi:** 10–15 dakika.
- **Kelime sayısı:** 208 (`Assets/Resources/Data/words.json`).

## Tur akışı
1. **Zar (2 sn):** Hayattaki oyunculardan biri seçilir. Animasyon herkesin ekranının üstünde.
   - Seçilme ağırlığı = 1 + 0,5 × (üst üste kaç tur seçilmedi). Seçilen oyuncunun sayacı sıfırlanır. Elenenler seçilmez.
   - HUD'da oyuncunun bir sonraki zardaki seçilme şansı yüzde olarak görünür ("Seçilme şansın: %12" = kendi ağırlığı / hayattakilerin toplam ağırlığı). Elenince gizlenir.
2. **Yazma (10 sn):** Seçilen oyuncunun yazı kutusu açılır, karakteri durur. Diğerleri hareket edebilir.
   - Saldırı kelimesiyse yazan kişi, kelime kabul edildikten sonra 3 sn içinde fareyle bir oyuncuya tıklayarak hedef seçer. Süre dolarsa rastgele bir rakip seçilir; botlar rastgele seçer. Kimse kendini hedefleyemez.
   - Süre biterse tur yanar.
3. **Sonuç (15–20 sn):** Kelime ortaya çıkar. Global tehditse çözümü de haritada belirir. Herkes gerçek zamanlı kaçar.
4. **Eleme:** Canı biten oyuncu hayalet olur.
5. Tek kişi kalana kadar 1'e dön.
6. **Maç sonu:** son kalan kazanır ve o andan itibaren dokunulmaz olur (hasar almaz, lava/uçuruma düşmez); sahada kalan yaratık ve tehditler sonucu değiştiremez.

## Temel değerler (başlangıç, `GameConfig`'te ayarlanır)
| Ayar | Değer |
| --- | --- |
| Başlangıç canı | 3 |
| Yazma süresi | 10 sn |
| Sonuç/kaçış süresi | 15–20 sn (kelimeye göre) |
| Global tehdit uyarı süresi | en az 5 sn |
| Kule/mağara/ev sayısı | kelimedeki sayı 8 oyuncu içindir; hayattaki oyuncu sayısıyla orantılı çoğalır/azalır (en az 1; `zoneReferencePlayers`) |
| Sığınak kapasitesi | hayattaki oyuncuların yarısı (aşağı yuvarla, en az 1) |
| Zar: seçilmeme bonusu | her seçilmeyen tur için +0,5 ağırlık (`diceBonusPerMissedTurn`) |
| Harita | 34 × 19 birim (20 oyuncu için geniş) |
| Kamera | Oyuncuyu yakından izler (384×216 piksel görüş, arenanın dışını göstermez). Hedef seçerken ve çözüm alanı (sığınak, kule, mağara, ev) çıkınca tüm arenayı gösterir (640×360) |
| Yanma süresi | ateşten hasar alınca 2,5 sn yanar (`burnSeconds`) |
| Kömürleşme | her ateş hasarında +0,35 (1 = simsiyah, azalmaz; `charPerFireHit`) |
| Boss canı | kelimenin canı (yoksa 10) + her ek canlı oyuncu için 3 (`bossHealthPerExtraPlayer`) |
| Boss vuruş bekleme süresi | oyuncu 0,5 sn, bot 1,5 sn (`bossHitCooldownSeconds`, `botBossHitCooldownSeconds`) |
| Araç (kelime yazmıyorsa) | hız 9 birim/sn, süre 8 sn, ezme yarıçapı 0,6 birim; patlama yarıçapı 2 birim, hasarı 2 |

## Kurallar
- Zar sadece kimin yazacağını seçer; uzun süredir seçilmeyenin şansı artar (adalet için).
- Bir kelime ve eş anlamlıları maçta bir kez kullanılır.
- Global tehdit = haritada çözüm de çıkar.
- Bilinmeyen kelime: komik "???" yaratığı (zararsız), tur yanmaz.
- Alan etkili saldırılar (ör. Yumruk): yerde uyarı alanı çıkar (hedefin o anki yerinde, hedefi takip etmez), uyarı süresi dolunca alandaki herkese vurur, yazan dahil. Fırlatma (knockback) kısa süreye yayılır.
- Gecikmeli patlayıcılar (ör. Bomba, explosive): hedefin 1,5 birim yanına düşer, fitil süresince ekranda geri sayım görünür, süre dolunca yarıçapındaki herkese hasar verir (fırlatmaz). Fitil süresinde alandan çıkılarak kaçılır.
- Tuzaklar (ör. Muz kabuğu, trap): hedefin 2 birim yanına düşer ve yerinde bekler (15 sn). Yarıçapına kim basarsa (hedef, yazan ya da başkası) kayar ve tuzak kaybolur (tek kullanımlık). Botlar tuzakları görmez, onlardan kaçmaz.
- Koruma kelimeleri yazana uygulanır: Kalkan (bir sonraki hasarı engeller, en çok 30 sn), Can (1 can, başlangıç canını aşmaz), Görünmezlik (6 sn saldırı kelimesinin hedefi seçilemez, peşindeki yaratıklar bırakmaz), Kanat (5 sn uçma: hız ×1,2, yerdeki yaratıkların temasından ve tuzaklardan etkilenmez; alan vuruşları, patlamalar ve uçan yaratıklar yine etkiler).
- Global tehdit akışı: kelime kabul edilince uyarı süresi başlar (en az 5 sn, atom bombası 8 sn), ekranın üstünde alarm ("ATOM BOMBASI! Sığınağa gir! (5 sn)") çıkar ve haritada çözüm alanları belirir. Alanın kapasitesi sınırlıdır: içeridekiler arasında merkeze en yakın olanlar önce girer, önce girenin yeri korunur. Sığınak kapasitesi hayattaki oyuncuların yarısı (aşağı yuvarla, en az 1). Süre dolunca etki başlar; alanın dışında kalan ve kapasiteye sığmayan herkes etkilenir. Kalkan atom bombasını bir kez engeller. Botlar uyarıda en yakın boş alana koşar.
- Tsunami: 5 sn uyarıdan sonra arenanın rastgele bir kenarından saniyede 4 birim hızla dev dalga gelir; önü geçtiği herkes elenir (kalkan bir kez engeller). Kule sayısı oyuncu sayısına bağlıdır (8 oyuncuda 3, 20 oyuncuda 8; her biri 3 kişi alır); kuledekiler kurtulur.
- Meteor yağmuru: haritada sığınak yoktur; 12 sn boyunca 30 meteor eşit aralıkla düşer. Her meteor önce yerde 1,5 sn bir gölge gösterir (yarıçap 1,5), sonra gölgedeki herkese 1 hasar verir. Meteorların bir kısmı (%40) rastgele bir oyuncunun yakınına, kalanı rastgele yere düşer. Çözüm gölgeleri görüp kaçmaktır; kalkan bir meteoru engeller.
- Yaratık tehditleri (Zombi sürüsü, Ejderha): uyarı süresi beklenmez, yaratıklar hemen görünür. Her yaratık en yakınındaki oyuncuya saldırır (hedefi ölünce ya da başka biri belirgin şekilde yaklaşınca en yakına geçer; görünmezlik alan oyuncular hedef alınamaz) ve yaklaşık 10 sn sürer. Zombi sürüsü: kenarlardan 20 zombi (hız 2,5, saniyede 1 hasar, ilk vuruşlar en çok 4 sn kademeli). Ejderha: uçan tek yaratık (hız 6), yarıçapı 2 olan alanındaki herkese saniyede 1 ateş hasarı verir. Evler ve mağaralar isteğe bağlı sığınaktır: içindekilere yaratıklar vuramaz, ama girmek zorunlu değildir. Evdeki/mağaradaki bot tehlike görse de yerinde kalır.
- Duvar (wall): yazanın baktığı yönün 1,8 birim önünde, o yöne dik, 4 birim uzunluğunda, 10 sn duran engel. Oyuncular, botlar ve yerdeki (uçmayan) yaratıklar içinden geçemez; uçan yaratıklar (Arı), kanatla uçan oyuncular ve yağmur bulutu geçer. Yaratıklar duvarın çevresinden dolaşmayı bilmez, duvara takılırlar.
- Çekme (Mıknatıs, pull): arenanın merkezine düşer ve süresi boyunca (4 sn) hedefi saniyede 4 birim hızla oraya çeker. Oyuncu hızı 5 olduğu için ters yönde yürüyerek direnebilir. Botlar mıknatıstan kaçmaz.
- Yavaşlatma (slowdown): hız yarıya iner. Alan yaratıkları (Bulut) içindeki herkesi yavaşlatır; alandan çıkınca etki 0,6 sn daha sürer.
- Vur-kaç yaratıklar (ör. Kurt, predator): vurduktan sonra 2 sn geri çekilir, ömrünün son 1,5 sn'sinde sıkılıp gider. Sürü (swarm) üyelerinin ilk vuruşları 0,8 sn arayla kademelidir.
- Kovalayanlar herkesi avlar (`creaturesHuntEveryone`, varsayılan açık): yürüyen/kovalayan yaratık bir kişiyi vurup kaybolmaz; hep en yakındakine geçer, hedefi ölünce yenisini seçer ve süresi bitene kadar sahada kalır. Hedefin üstüne/yanına düşenler (Yumruk, Bomba), alan, tuzak, çekim ve mermiler bu kurala girmez, olduğu gibi çalışır.
- Yanma ve kömürleşme: ateş hasarı (Ateş, Ejderha alanı, yanan kare, patlayan araç) can götürdüyse karakter 2,5 sn tutuşur (üstünde alev) ve her hasarda kömürleşmesi 0,35 artar (karakter kararır, kendiliğinden geçmez). Kalkanın engellediği vuruş tutuşturmaz. Kömürleşmiş ya da yanarken ölen karakter parçalanmaz: simsiyah ceset olarak yana yatar, birkaç saniye tüter, çevresine kül saçılır ve maç boyunca yerde kalır.
- Kan ve parçalanma (yalnızca görüntü, kurala dokunmaz): vurulana kan sıçrar, damlalar yerde kalıcı leke olur. Ateş dışı ölümde karakter 6 parçaya (2×3) ayrılıp savrulur, sekip yerde kalır. Lavda ölende kıvılcım, duman ve kömür parçaları çıkar. Çok birikirse en eskiler silinir (en çok 1500 leke, 300 parça/ceset). Yeni maç başlarken bütün kan, parça ve cesetler temizlenir.
- İtiş (Boşluk tuşu, her karakterde): bakılan yönde 1,9 birim içindekileri (65° yelpaze) güçlü savurur (yakındakini ~3,5 birim). Bekleme süresi 3 sn; boşa atılsa da süre başlar. Elenen, uçan ve yazı sırasındaki (yazarken karakter sabit) oyuncu itiş atamaz; hayalet ve uçanlar itilmez. Botlar önlerinde biri varken kendiliğinden itiş atar. İtiş hasar vermez; kuleden/sığınaktan çıkarmak için kullanılır.
- Karakterler birbirinin içinden geçemez: hareket eden öndekini iter, kalabalıkta itme yayılır. İtilen karakter kuleden/sığınaktan çıkarsa korumasını kaybeder, boşalan yere başkası girebilir. Hayaletler ve uçanlar (kanat) iç içe geçer (`charactersPushEachOther`).
- Hayaletler: elenen oyuncu hayalet olur: yarı saydam görünür (yalnızca kendisi görür; başkalarının hayaleti ekranda görünmez), arenada gezip maçı izler; duvarlar, yaratıklar ve tehditler ona işlemez, hedef alınamaz, zarda seçilmez. Yeniden doğuş: yazan oyuncu "Yeniden canlanma" (revive) kelimesini yazarsa elenenlerden rastgele biri 1 canla hemen geri döner (tek kişilik ve ağ maçında aynı). Hayalet yoksa kelime boşa gider; kelime de bu maçta bir daha kullanılamaz. Yanarak (ateş, kömürleşme) ölenin cesedi simsiyah yere yatar ve maç boyunca kalır; diğer ölümlerde karakter kanlı parçalara ayrılır. Elenen botların hayaleti hiç görünmez ve gezmez; geriye yalnızca yerdeki parçaları ya da kömür cesedi kalır (dirilirse yeniden görünür).

## Etiket listesi (v1)
| Etiket | Anlamı |
| --- | --- |
| `slow` | Yavaş hareket |
| `fast` | Hızlı hareket |
| `flying` | Engellerin üstünden geçer |
| `swarm` | Birden fazla küçük birim |
| `undead` | Ölümsüz, kovalayıcı |
| `predator` | Hedefi kovalar, süre dolunca gider |
| `instant` | Anında etki, uyarı işaretiyle |
| `knockback` | Hedefi iter/fırlatır |
| `trap` | Yere konur, basanı etkiler |
| `slowdown` | Hedefi yavaşlatır |
| `pull` | Hedefi bir noktaya çeker |
| `armored` | Yüksek hasar, çok yavaş |
| `explosive` | Gecikmeli alan hasarı |
| `area` | Alan etkisi |
| `water` | Su; tsunami/yüzme etkileşimleri |
| `fire` | Ateş |
| `sky` | Gökten düşer, gölge uyarısı verir |
| `map_wide` | Bütün haritayı etkiler |
| `shield` | Bir hasarı engeller |
| `heal` | Can verir |
| `wall` | Engel oluşturur |
| `escape` | Geçici uçma/kaçış |
| `stealth` | Hedef alınamaz |
| `revive` | Elenenlerden biri yeniden doğar |
| `ignite` | Haritadaki tahtaları tutuşturur (yangın yayılır) |
| `weapon` | Yazana silah verir: Boşluk itiş yerine ateş eder, mermi değdiği ilk rakibe vurur (kalkan korur, duvar durdurur) |
| `melee` | `weapon` ile birlikte: silah ateş etmez, Boşluk ile önündeki yay içindekilere (menzil `stats.radius`) kelimenin hasarı kadar vurur (Kılıç, Sopa). Yakın silahlı bot en yakın rakibi kovalar |
| `vehicle` | Yazan araca biner (Motor, Araba): süresi bitene kadar hiç durmaz, WASD yön verir, arena kenarından seker, değdiğini kelimenin hasarı kadar ezer (kalkan korur; aynı kişi bir sürüşte bir kez ezilir; uçanlar ezilmez). `stats.count` araçta "kaç kişiyi ezince durur" demektir (ör. araba 1): sınır dolunca araç durur, sürücü sağ salim iner; 0 = sınırsız. `explosive` ile birlikte binaya/duvara çarpınca patlar: sürücü ölür (kalkan kurtarmaz), 2 birim içindekiler 2 ateş hasarı alıp (tutuşur) savrulur, tahtalar tutuşur. `explosive` etiketi olmayan araç eve/duvara çarpınca yalnızca durur, sürücü iner. Botlar en yakın rakibin üstüne sürer |
| `boss` | Can barlı dev (Godzilla): en yakına saldırır; herkes Boşluk ile vurup canını eritir, son vuran +1 can (canı tamsa kalkan) alır. Canı kalabalığa göre ölçeklenir: kelimenin canı + (canlı oyuncu − 1) × 3. Canı en az 2 olan botlar da yakındaki (14 birim) boss'a gidip vurur (silahlı ya da canı az bot kaçar); botlar 1,5 sn'de bir, oyuncular 0,5 sn'de bir vurur, böylece son vuruşu oyuncunun yapma şansı kalır |

## Harita ve arazi
Harita 34 × 19 karelik bir ızgaradır (kare = 1 birim, 16×16 piksel). Her maçta tohumdan üretilir (`terrainSeed` 0 ise rastgele); ağ maçında host üretir, herkese durum gider.
- **Tahta köyler** (3 adet): tahta meydan ve yürüyüş yolları, içinden geçilemeyen **tahta ev duvarları** (kapılı evler), tahta zemin. Köyler patikalarla bağlanır.
- **Lav havuzu** (2) ve **uçurum yarığı** (2): içine giren (itilen ya da kendi yürüyen) düşme animasyonu oynar (~1 sn) ve **bütün canını kaybeder** (ölür); kalkan engelleyemez. Lav bitişiğindeki tahtayı ara sıra tutuşturur. Kanatla uçan ve hayaletler etkilenmez.
- **Yangın:** "Ateş" kelimesi (uyarılı alev, hasar verir) ve Ejderha tahtaları tutuşturur. Yanan tahta komşu tahtalara saniyede ~%45 olasılıkla yayılır, 5 sn yanar, sonra **kömürleşir** (tahta zemin kömür zemin olur, tahta ev duvarı çöker ve geçilebilir olur). Yanan karede duran saniyede 1 hasar alır. Çimen/patika/kömür yanmaz (üstüne düşen alev 2,5 sn kalır, yayılmaz).
- **Evler:** 5×4 karelik, 2 kare genişliğinde kapılı, sokaklar 2 kare geniş (karakterler takılmaz). Çatı kiremitle çizilir; içine girince çatı yumuşakça kaybolur, çıkınca geri gelir. Ev yanarken ya da yanıp çökünce çatı açık kalır.
- Lav/uçuruma itilen bot oradan en yakın güvenli kareye koşar (itilmenin etkisi korunur, hasar alır).
- Botlar kendi isteğiyle lava, uçuruma, yanan karelere girmez ve alevden kaçar; ama itilirlerse girebilir. Kule/sığınak lav, uçurum ve duvar üstüne konmaz.

## Çözüm (counter) türleri
| Tür | Kullanan tehdit |
| --- | --- |
| `shelter` | Atom bombası |
| `tower` | Tsunami |
| `cave` | Ejderha |
| `shadows` | Meteor yağmuru (çözüm = gölgeleri görmek) |
| `houses` | Zombi sürüsü |

## Görsel
- **Tarz:** 3D piksel. Karakterler ve araçlar katmanlı piksel dilimlerinden (sprite stacking) çizilir ve gittikleri yöne döner (gözler öne bakar, yürürken hafifçe zıplar). Yaratıklar voxel modeldir ve hep kameraya bakar (yalnızca sağa-sola aynalanır), üstten bakan kamerada çarpık görünmesinler diye.
- **Sıralama:** karakterler, yaratıklar, araçlar ve ev çatıları ekrandaki yüksekliğe göre sıralanır: aşağıda duran öndekidir.
- **Derinlik:** uçurum yarığı ve lav havuzunun kenarındaki zemin aşağı inen bir yüz olarak çizilir; çukur gerçekten derin görünür.
- **Saldırı animasyonu:** yaratık her vuruşta hedefe atılır ve büyür, sonra geri çekilir; vurduğu yerde toz halkası ve saçılan toprak çıkar, kamera sarsılır (devlerde sert). Devler (boss, King Kong...) yürürken her adımda yer sarsılır ve ayaklarından toz kalkar. Yalnızca görüntüdür.

## Menü ve deneme araçları
- **Ana menü:** "Host (oyunu kur)", "Katıl" ve "Tek kişilik oyna (botlarla)". Tek kişilik oyun build'de de açılır.
- **Esc:** tek kişilik maçtan ana menüye döner.
- **F1 deneme paneli:** yalnızca Unity Editor'da ve geliştirme (development) build'inde vardır; oyuncuya giden sürümde yoktur. Kelimeyi sırayı beklemeden "ben yazmış gibi" ya da "bot yazmış gibi" oynatır (kilitlemez); ölümsüzlük, zaman hızı, botları dondurma/yanına toplama, herkesi diriltme, yaratıkları ve kanı temizleme içerir.

## Ağ mimarisi (Aşama 3 kararı)
- **Kütüphane:** Netcode for GameObjects (Unity'nin resmi çözümü). Bağlantı katmanı (transport) önce Unity Transport ile yerel ağda denenir; Steam lobisi/daveti en sona kalır (Steamworks entegrasyonu, test için App ID 480).
- **Model:** Host yetkilidir (sunucu + oyuncu aynı kopyada). Zar, tur sırası, kelime doğrulaması, yaratık/tehdit sonuçları ve eleme sadece host'ta hesaplanır; istemci yalnızca girdi (hareket, yazılan kelime, hedef tıklaması) gönderir ve olayları alıp gösterir.
- **Kod kuralı:** Oyun mantığı (TurnManager, Dice, CreatureModel, ThreatDirector...) ağdan bağımsız düz C# olarak kalır; ağ katmanı onu sarar, mantığa ağ kodu karışmaz.
- **Kopan oyuncu:** Bot devralır (BotController).
- **Sıra:** 1) paket + yerel host/istemci bağlantısı, 2) TurnManager host'ta, olaylar istemcilere, 3) hareket senkronu, 4) yaratık/tehdit senkronu, 5) Steam lobisi, 6) kopan oyuncu → bot.

## Açık sorular
- Oyunun adı
- ~~Grafik tarzı~~ — karar verildi: 3D piksel (bkz. "Görsel")
- Hayaletler tam olarak ne yapacak?
- Yazan kişi kendi tehdidinden etkilenecek mi?
