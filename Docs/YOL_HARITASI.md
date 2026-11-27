# Yol Haritası

Claude `/sonraki-adim` ile buradaki ilk işaretlenmemiş görevi alır.

## Aşama 1 — Kurulum
- [x] Unity 6 2D projesi oluşturuldu, bu paket proje klasörüne kopyalandı
- [x] Git deposu kuruldu (`.gitignore` Unity için), ilk commit
- [x] Unity MCP kuruldu ve Claude Code bağlandı (isteğe bağlı ama önerilir)
- [x] Klasör yapısı `CLAUDE.md`'deki gibi oluşturuldu, `GameConfig` ScriptableObject'i eklendi

## Aşama 2 — Yerel prototip (tek bilgisayar, botlarla)
- [x] Oyun alanı: sınırları olan düz bir 2D arena, kamera
- [x] Oyuncu: WASD hareketi, 3 can, can göstergesi
- [x] 7 bot oyuncu: rastgele dolaşır, tehditten kaçmaya çalışır
- [x] `WordNormalizer`: Türkçe küçük harf, boşluk temizleme, aksansız eşleşme + testleri
- [x] `WordDatabase`: words.json'u yükler, alias ile arar, kullanılan kelimeleri kilitler + testleri
- [x] `TurnManager`: zar → yazma → sonuç → eleme döngüsü + testleri
- [x] Zar UI'ı: ekranın üstünde oyuncu isimleri arasında dönen seçim
- [x] Yazı kutusu UI'ı: 10 sn sayaç, Enter ile gönder
- [x] Hedef seçme: saldırı kelimesinde insan oyuncu 3 sn içinde fareyle bir oyuncuya tıklar (süre dolarsa rastgele), botlar rastgele seçer, kimse kendini seçemez
- [x] Etiket davranışları: `slow`, `fast`, `predator`, `undead`, `swarm`, `instant`, `knockback`
  - [x] Altyapı: yaratık modeli, etiket kayıt defteri, kelimeden yaratık doğması, temas hasarı, botlara tehlike alanı
  - [x] `undead` (kalıcı vuruş), `slow` ve `fast` (hız kelimenin değerinden gelir)
  - [x] `predator` (vur-kaç, ömrün sonunda sıkılıp gider), `swarm` (sürü, üyelerin ilk vuruşu kademeli)
  - [x] `instant` (yerde uyarı alanı, süre dolunca alandaki herkese vurur), `knockback` (kısa süreye yayılan fırlatma)
- [x] Etiket davranışları: `trap`, `slowdown`, `pull`, `explosive`, `area`, `sky`
  - [x] Durum etkileri altyapısı ve `slowdown` (Fare: hasarsız yavaşlatma, göstergeli)
  - [x] `area` (Bulut: hedefi yavaşça takip eden, içindeki herkesi yavaşlatan sürekli alan)
  - [x] `explosive` (Bomba: hedefin yanına düşer, ekranda geri sayım, süre dolunca alandaki herkese hasar)
  - [x] `trap` (Muz kabuğu: hedefin yanına düşer, basan herkes kayar, tek kullanımlık, botlar görmez)
  - [x] `pull` (Mıknatıs: arena merkezine düşer, hedefi çeker; yürüyerek direnilebilir)
  - [x] `sky` (Meteor yağmuruyla birlikte yapıldı: gökten düşen gölge uyarılı yaratıklar)
- [x] Koruma etiketleri: `shield`, `heal`, `wall`, `escape`, `stealth`
  - [x] `shield` (bir sonraki hasarı engeller), `heal` (1 can), `stealth` (6 sn hedef alınamaz), `escape` (5 sn uçma, yerdeki saldırılardan etkilenmez), `flying` (uçan yaratıklar)
  - [x] `wall` (Duvar: yazanın baktığı yönün önüne dik, 4 birim, 10 sn; oyuncu, bot ve yerdeki yaratıkları durdurur, uçanlar geçer)
- [x] Global tehdit altyapısı + Atom bombası ve sığınak (sınırlı kapasite): uyarı süresi, alarm yazısı, çözüm alanları, botlar sığınır, patlama
- [x] Kalan 4 global tehdit ve çözümleri
  - [x] Tsunami: rastgele kenardan dalga, kuleler (3 × 3 kişi) korur
  - [x] Meteor yağmuru: 12 sn'de 30 meteor, her biri 1,5 sn gölge gösterir, çözüm gölgeleri görüp kaçmak
  - [x] Zombi sürüsü: hemen belirir (uyarı yok), kenarlardan 20 zombi en yakındaki oyuncuya saldırır (10 sn); evler isteğe bağlı sığınak, evdeki bot evinde kalır
  - [x] Ejderha: uyarıdan (7 sn) sonra kenardan gelir, en yakındaki oyuncuya saldırır, alanında saniyede 1 ateş hasarı (10 sn); mağaralar isteğe bağlı sığınak
- [x] Bilinmeyen kelime → "???" yaratığı
- [x] Hayalet modu (eleme sonrası izleme): yarı saydam, WASD ile gezer, duvarlar/yaratıklar/tehditler işlemez, hedef alınamaz (yeniden doğuş için bkz. Aşama 4)
- [x] Maç sonu ekranı: kazanan, yazılan kelimeler listesi, R ile yeniden başlama
- [x] Piksel sanat görünümü: kodla üretilen 16x16 sprite'lar (`Tools/GeneratePixelArt.py`), Pixel Perfect Camera, piksel yazı tipi (Pixelify Sans). Beğenilmeyen sprite aynı adlı PNG ile değiştirilebilir
- [ ] **Test:** 3 arkadaş oynadı, notlar `Docs/TEST_NOTLARI.md`'ye yazıldı

## Aşama 3 — Online 4–8 kişi
- [x] Ağ kütüphanesi seçildi ve GDD'ye yazıldı (Netcode for GameObjects, host yetkili)
- [x] Steamworks entegrasyonu, Steam lobisi, arkadaş daveti (Facepunch.Steamworks + Steam aktarım katmanı; Steam'e bağlanma, lobi kurma ve host denendi; arkadaşın katılması iki Steam hesabıyla denenecek)
- [x] TurnManager sunucuda çalışıyor, istemciler olayları alıyor (`NetTurnHost`/`NetTurnClient`)
- [x] Oyuncu hareketi senkronizasyonu (host yetkili, `AvatarMover`)
- [x] Yaratık ve tehdit senkronizasyonu (`NetMatchSim`; host tarafı Play'de denendi, iki pencereyle denemesi sürüyor)
- [x] Kopan oyuncu → bot devralır (host tarafı denendi; gerçek bağlantı kopması iki pencereyle denenecek)
- [ ] **Test:** Arkadaşlarla kopmadan 10 maç

## Aşama 4 — 20 kişi
- [x] Lobi 4–20 kişi, eksikler bot (host lobide oyuncu sayısını ayarlar; Steam odası 20 kişi; geç bağlanan reddedilir)
- [x] Hayalet katılımı: "Yeniden canlanma" kelimesi elenenlerden rastgele birini 1 canla geri getirir (oylama yok, karar: rastgele)
- [x] 20 oyuncu performans testi (`NetPerformanceTests`; Editor'da 20 oyuncu + 49 yaratıkla ~167 kare/sn)
- [x] Hile kontrolleri (sunucu doğrulama listesi: `Docs/HILE_KONTROLLERI.md`, hız sınırı, boyut sınırı)

## Aşama 5 — Steam sayfası ve demo
- [x] Oyun adı, logo, kapsül görselleri (ad: WordRoyale; görseller `Steam/Store/`, üretici `Tools/GenerateStoreArt.py`)
- [ ] 30 sn fragman
- [ ] Steam sayfası yayında, wishlist toplanıyor
- [ ] Next Fest demosu

## Aşama 6 — İçerik ve çıkış
- [x] 300+ kelime
- [x] 3 farklı harita: Tahta Köy (lav, yangın), Karlı Dağ (taş evler, buz, dondurucu su), Çöl Kasabası (kerpiç evler, bataklık kumu, kaktüs); tema maç tohumundan seçilir, `GameConfig.forcedMapTheme` ile zorlanır
- [x] Boss'lar evleri yıkar (duvar yıkımı, oranı aşınca ev çöker, sığınak evi tehdit sürerken korunur; ağda host hesaplar), yaratıklar evde/tehlikede doğmaz, kapılar hep güney duvarında
- [x] Yaratık vuruş animasyonu: hazırlık (`ad_atk0`, geri çekilip ezilme) → vuruş (`ad_atk`, atılma) → toparlanma; süreler `GameConfig`'te; hedefte etiketlerden seçilen 4 karelik vuruş izi (`Resources/Data/attack_fx.json`); ağda istemcide de aynı
- [x] Yaratıklar daha tehlikeli (oyuncu geri bildirimi): hız/ömür çarpanı, `slow` alt sınırı, atılma (hazırlıkta kırmızı yanar), vuruşta sendeleme, vuramadığı hedeften dönme, doğma mesafesi 4,5; etikete göre çarpanlar `GameConfig.creatureTagTunings`; korku ipuçları (yakın tehlikede kırmızı vinyet, ekran dışı kovalayana ok, sert doğma sesi); ölçüm testi `CreatureMenaceMeasurementTests` (önce/sonra)
- [x] Ejderha ve kılıç dengesi (oyuncu geri bildirimi): mağaradakine ateşin hiçbir türü işlemez (ateş alanı, yerdeki ateş; mağaraya giren söner); ejderha 7 sn uyarıdan sonra kenardan gelir, hız 3,6, 3 mağara; kılıç 2 hasar / menzil 1,3 / 10 sn, savuruştan önce 0,2 sn parlayan hazırlık ve 0,8 sn bekleme; ölçüm testi `DragonRefugeMeasurementTests` (önce/sonra)
- [x] Koleksiyon defteri (menüde "Defter"; kendi yazdığın kelime keşfedilir, başkasınınki görülür; kayıt `persistentDataPath/collection.json`, etiket adları `Resources/Data/tag_names.json`)
- [x] Ayarlar, ses, Türkçe/İngilizce dil desteği
  - [x] Ses ve müzik: kodla üretilen retro efektler ve 3 müzik döngüsü (`Tools/GenerateSounds.py`), kelime sesi etiketlerden (`Resources/Data/sfx_tags.json`), tehditte siren ve gerilim müziği; ağ maçında istemcide de duyulur
  - [x] Ayarlar ekranı (menüde "Ayarlar", maçta Esc): ses düzeyleri, tam ekran, çözünürlük, VSync, isim etiketleri; kayıt `persistentDataPath/settings.json`
  - [x] Türkçe/İngilizce dil desteği: ayarlarda Türkçe ↔ English, metinler `Resources/Data/lang_tr.json`/`lang_en.json`, kelimelerin İngilizce adı ve açıklaması; kelime her dilde yazılabilir, ağda herkes kendi dilinde görür
- [x] Arayüz ve maç akışı cilası: ana menü (WordRoyale piksel logosu, kayan karo + süzülen harf arka planı, yaylı büyüyen düğmeler, "Çok oyunculu" sayfasında yerel ağ + Steam, katlanır "Gelişmiş" IP alanı, Çıkış), sahne geçişinde piksel kararma (`ScreenFader`), maç başında "HAZIR!", zar simgesi dönüp yavaşlar ve durunca sıçrar, ortada "SIRA SENDE!"/yazanın adı, yazı kutusunda harf "pop", titreme, red sallanması, kabulde yeşil parlama ve uçan kelime, son 3 saniyede kırmızı nabız ve tik sesi, maç sonunda başlık punch, sırayla beliren satırlar, zafer konfetisi, "Tekrar oyna"/"Ana menü"; akış ve tehdit yazıları punch ile girip kayarak çıkar. Hesaplar `Easing`/`UiTween` (testli), sayılar `GameConfig` "Arayüz cilası"
- [x] Vuruş hissi (çıkmış aksiyon oyunlarındaki "juice"; yalnızca görüntü/ses, kural ve hasar zamanı değişmez): vuruş donması (vuran yaratık ve vurulanın görüntüsü 70 ms, devde 120 ms; `Time.timeScale`'e dokunmadan `HitStop`), 1-2 kare beyaz parlama (`SpriteFlash` shader) + sönen kırmızı ton (kalkanda camgöbeği), vuruş yönünde ezilip yaylanma, atılan yaratık uzar; tepki yaratığın darbe anına denk gelir (`StrikeRegistry`); trauma tabanlı kamera sarsıntısı + yönlü itme (Pixel Perfect yüzünden zoom yok), yerel oyuncu vurulunca takip 60 ms durur; "-1"/"BLOK" hasar sayıları; yerel hasarda ekran kenarı kırmızı parlar, can yazısı sallanır, can 1'de kalp atışı; elemede güçlü sarsıntı ve büyük kan/parça patlaması, tek kişilikte son elemede 0,8 sn ağır çekim; katmanlı vuruş sesi (±%8 perde); itişte donma, parlama, toz, kamera itmesi; ayarlarda "Ekran sarsıntısı" aç/kapa + şiddet. Hesaplar `SpringValue`/`Trauma`/`HitFeelMath` (testli), sayılar `GameConfig` "Vuruş hissi"
- [x] Bot adları: tek kişilikte botlar gerçek oyuncu gibi takma ad alır (`Resources/Data/bot_names.json`, 178 ad; maç tohumundan, tekrarsız, senin adınla çakışmaz; liste bozuksa "Bot N"), ağ maçında "Bot N" + isim etiketinde gri "[BOT]" rozeti. Seçim `BotNameGenerator` (testli), ayarlar `GameConfig` "Bot adları"
- [x] Piksel HUD (sol üst, `PlayerHud`; eski "Can: 3 / 3", "İtiş (Boşluk): hazır", "Kalkan var" yazıları kodla gizlenir): panel içinde kalpler (kaybedilen kalp kırılıp parçalanır, can gelince büyüyerek dolar, düşük canda son kalp atar, vurulunca sallanır), elenince "HAYALET"; itiş simgesi + dolan bekleme çubuğu + hazır olunca "pop", "BOŞLUK" tuş kapağı (silah varken silah simgesi + kalan atış); durum simgeleri (kalkan, silah, araç, uçma, kılık, görünmezlik, yanma, yavaş, çekilme) süre çubuğu, sayı rozeti ve bitmeden yanıp sönme; ağda da yerel oyuncuya göre (süreler bilinmediği için yalnızca simge). Tam sayı piksel ölçeği. Hesaplar `HudRules`/`HeartRowAnimator`/`DurationGauge`/`HudStatusCollector` (testli), sayılar `GameConfig` "HUD"
- [x] Yazı kutusu ve kelime büyüsü (Steam görselindeki kutu; yalnızca görüntü/ses, yazma süresi ve kabul/ret kuralı değişmez): ekranın ortasının biraz altında beyaz kutu (`ui_typebox`, sıra sendeyken `_hot` + nefes alan parıltı, redde `_bad` + sallanma + harfler kırmızı dağılır, kabulde `_ok` + `fx_word_burst`), yanıp sönen imleç, altında kutu genişliğinde sayaç (sarı-turuncu, sonda kırmızı ve titrer), üst köşelerin dışında eğik iki zar (atılırken `ui_die_roll`, durunca süs yüz + kıvılcım), süzülen parıltılar; başkası yazarken küçük/soluk "X yazıyor..." (harfler ağda gönderilmez); her tuşta harf pop + `ui_key_spark` + titreme + harfe göre perdeli tuş sesi, silinen harf düşer. Kelime büyüsü: harfler kutudan sırayla kopar, hedef belli olunca `fx_letter_trail` iziyle Bezier yaylarla uçar (son harf en geç 0,5 sn), yaratık 0'dan büyüyerek belirir, yerde `fx_summon`, beyaz flaş, kamera itmesi; kamera dışı hedefte ekran kenarında söner; koruma kelimesinde yazana, küresel tehditte ekranın tepesine uçar ve kenarda `fx_threat_warn` üçgenleri; süresi biten yaratıkta `fx_poof`. Ağ istemcisinde de aynı. Hesaplar `TimerBarRules`/`TypingFxMath`/`ArcPath`/`WordSpellPlan`/`WordSpellRules`/`SpellScreenMath` (testli), sayılar `GameConfig` "Yazı kutusu" ve "Kelime büyüsü"
- [ ] Çıkış
