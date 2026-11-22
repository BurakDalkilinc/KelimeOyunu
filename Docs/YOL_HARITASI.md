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
- [ ] Zar UI'ı: ekranın üstünde oyuncu isimleri arasında dönen seçim
- [ ] Yazı kutusu UI'ı: 10 sn sayaç, Enter ile gönder, hedef seçme
- [ ] Etiket davranışları: `slow`, `fast`, `predator`, `undead`, `swarm`, `instant`, `knockback`
- [ ] Etiket davranışları: `trap`, `slowdown`, `pull`, `explosive`, `area`, `sky`
- [ ] Koruma etiketleri: `shield`, `heal`, `wall`, `escape`, `stealth`
- [ ] Global tehdit altyapısı + Atom bombası ve sığınak (sınırlı kapasite)
- [ ] Kalan 4 global tehdit ve çözümleri
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
