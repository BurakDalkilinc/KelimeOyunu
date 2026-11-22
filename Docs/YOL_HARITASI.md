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
- [ ] Etiket davranışları: `trap`, `slowdown`, `pull`, `explosive`, `area`, `sky`
  - [x] Durum etkileri altyapısı ve `slowdown` (Fare: hasarsız yavaşlatma, göstergeli)
  - [x] `area` (Bulut: hedefi yavaşça takip eden, içindeki herkesi yavaşlatan sürekli alan)
  - [x] `explosive` (Bomba: hedefin yanına düşer, ekranda geri sayım, süre dolunca alandaki herkese hasar)
  - [x] `trap` (Muz kabuğu: hedefin yanına düşer, basan herkes kayar, tek kullanımlık, botlar görmez)
  - [x] `pull` (Mıknatıs: arena merkezine düşer, hedefi çeker; yürüyerek direnilebilir)
  - [ ] `sky`: global tehdit adımında (Meteor yağmuru) yapılacak
- [x] Koruma etiketleri: `shield`, `heal`, `wall`, `escape`, `stealth`
  - [x] `shield` (bir sonraki hasarı engeller), `heal` (1 can), `stealth` (6 sn hedef alınamaz), `escape` (5 sn uçma, yerdeki saldırılardan etkilenmez), `flying` (uçan yaratıklar)
  - [x] `wall` (Duvar: yazanın baktığı yönün önüne dik, 4 birim, 10 sn; oyuncu, bot ve yerdeki yaratıkları durdurur, uçanlar geçer)
- [x] Global tehdit altyapısı + Atom bombası ve sığınak (sınırlı kapasite): uyarı süresi, alarm yazısı, çözüm alanları, botlar sığınır, patlama
- [ ] Kalan 4 global tehdit ve çözümleri
  - [x] Tsunami: rastgele kenardan dalga, kuleler (3 × 3 kişi) korur
  - [x] Meteor yağmuru: 12 sn'de 30 meteor, her biri 1,5 sn gölge gösterir, çözüm gölgeleri görüp kaçmak
  - [ ] Zombi sürüsü (evler 4 × 2 kişi)
  - [ ] Ejderha (mağaralar 2 × 4 kişi, `fire`)
- [ ] Bilinmeyen kelime → "???" yaratığı
- [ ] Hayalet modu (eleme sonrası izleme)
- [ ] Maç sonu ekranı: kazanan, yazılan kelimeler listesi
- [ ] **Test:** 3 arkadaş oynadı, notlar `Docs/TEST_NOTLARI.md`'ye yazıldı

## Aşama 3 — Online 4–8 kişi
- [ ] Ağ kütüphanesi seçildi ve GDD'ye yazıldı
- [ ] Steamworks entegrasyonu, Steam lobisi, arkadaş daveti
- [ ] TurnManager sunucuda çalışıyor, istemciler olayları alıyor
- [ ] Oyuncu hareketi senkronizasyonu
- [ ] Yaratık ve tehdit senkronizasyonu
- [ ] Kopan oyuncu → bot devralır
- [ ] **Test:** Arkadaşlarla kopmadan 10 maç

## Aşama 4 — 20 kişi
- [ ] Lobi 4–20 kişi, eksikler bot
- [ ] Hayalet oylama sistemi
- [ ] 20 oyuncu performans testi
- [ ] Hile kontrolleri (sunucu doğrulama listesi)

## Aşama 5 — Steam sayfası ve demo
- [ ] Oyun adı, logo, kapsül görselleri
- [ ] 30 sn fragman
- [ ] Steam sayfası yayında, wishlist toplanıyor
- [ ] Next Fest demosu

## Aşama 6 — İçerik ve çıkış
- [ ] 300+ kelime
- [ ] 3 farklı harita
- [ ] Koleksiyon defteri
- [ ] Ayarlar, ses, Türkçe/İngilizce dil desteği
- [ ] Çıkış
