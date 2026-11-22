# Oyun Tasarım Dokümanı (GDD)

## Özet
- **Tür:** Çok oyunculu parti oyunu, son kalan kazanır. PC / Steam.
- **Oyuncu:** 4–20, eksikleri botlar doldurur.
- **Maç süresi hedefi:** 10–15 dakika.

## Tur akışı
1. **Zar (2 sn):** Hayattaki oyunculardan biri seçilir. Animasyon herkesin ekranının üstünde.
2. **Yazma (10 sn):** Seçilen oyuncunun yazı kutusu açılır, karakteri durur. Diğerleri hareket edebilir.
   - Saldırı kelimesiyse yazan kişi hedef seçer (yazmadan önce ya da sonra 3 sn).
   - Süre biterse tur yanar.
3. **Sonuç (15–20 sn):** Kelime ortaya çıkar. Global tehditse çözümü de haritada belirir. Herkes gerçek zamanlı kaçar.
4. **Eleme:** Canı biten oyuncu hayalet olur.
5. Tek kişi kalana kadar 1'e dön.

## Temel değerler (başlangıç, `GameConfig`'te ayarlanır)
| Ayar | Değer |
| --- | --- |
| Başlangıç canı | 3 |
| Yazma süresi | 10 sn |
| Sonuç/kaçış süresi | 15–20 sn (kelimeye göre) |
| Global tehdit uyarı süresi | en az 5 sn |
| Sığınak kapasitesi | hayattaki oyuncuların yarısı (aşağı yuvarla, en az 1) |

## Kurallar
- Zar sadece kimin yazacağını seçer.
- Bir kelime ve eş anlamlıları maçta bir kez kullanılır.
- Global tehdit = haritada çözüm de çıkar.
- Bilinmeyen kelime: komik "???" yaratığı (zararsız), tur yanmaz.
- Hayaletler: (karar bekliyor) oylama ile sonraki turu etkiler.

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

## Çözüm (counter) türleri
| Tür | Kullanan tehdit |
| --- | --- |
| `shelter` | Atom bombası |
| `tower` | Tsunami |
| `cave` | Ejderha |
| `shadows` | Meteor yağmuru (çözüm = gölgeleri görmek) |
| `houses` | Zombi sürüsü |

## Açık sorular
- Oyunun adı
- Grafik tarzı
- Zar tamamen rastgele mi, uzun süredir seçilmeyenin şansı artsın mı?
- Hayaletler tam olarak ne yapacak?
- Yazan kişi kendi tehdidinden etkilenecek mi?
- Ağ kütüphanesi seçimi (Aşama 3)
