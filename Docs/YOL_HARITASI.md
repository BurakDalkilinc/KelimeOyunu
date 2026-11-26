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
  - [x] Ejderha: hemen belirir, en yakındaki oyuncuya saldırır, alanında saniyede 1 ateş hasarı (10 sn); mağaralar isteğe bağlı sığınak
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
- [ ] Oyun adı, logo, kapsül görselleri
- [ ] 30 sn fragman
- [ ] Steam sayfası yayında, wishlist toplanıyor
- [ ] Next Fest demosu

## Aşama 6 — İçerik ve çıkış
- [x] 300+ kelime
- [x] 3 farklı harita: Tahta Köy (lav, yangın), Karlı Dağ (taş evler, buz, dondurucu su), Çöl Kasabası (kerpiç evler, bataklık kumu, kaktüs); tema maç tohumundan seçilir, `GameConfig.forcedMapTheme` ile zorlanır
- [x] Koleksiyon defteri (menüde "Defter"; kendi yazdığın kelime keşfedilir, başkasınınki görülür; kayıt `persistentDataPath/collection.json`, etiket adları `Resources/Data/tag_names.json`)
- [x] Ayarlar, ses, Türkçe/İngilizce dil desteği
  - [x] Ses ve müzik: kodla üretilen retro efektler ve 3 müzik döngüsü (`Tools/GenerateSounds.py`), kelime sesi etiketlerden (`Resources/Data/sfx_tags.json`), tehditte siren ve gerilim müziği; ağ maçında istemcide de duyulur
  - [x] Ayarlar ekranı (menüde "Ayarlar", maçta Esc): ses düzeyleri, tam ekran, çözünürlük, VSync, isim etiketleri; kayıt `persistentDataPath/settings.json`
  - [x] Türkçe/İngilizce dil desteği: ayarlarda Türkçe ↔ English, metinler `Resources/Data/lang_tr.json`/`lang_en.json`, kelimelerin İngilizce adı ve açıklaması; kelime her dilde yazılabilir, ağda herkes kendi dilinde görür
- [ ] Çıkış
