# Oyun Tasarım Dokümanı (GDD)

## Özet
- **Tür:** Çok oyunculu parti oyunu, son kalan kazanır. PC / Steam.
- **Oyuncu:** 4–20, eksikleri botlar doldurur.
- **Maç süresi hedefi:** 10–15 dakika.

## Tur akışı
1. **Zar (2 sn):** Hayattaki oyunculardan biri seçilir. Animasyon herkesin ekranının üstünde.
   - Seçilme ağırlığı = 1 + 0,5 × (üst üste kaç tur seçilmedi). Seçilen oyuncunun sayacı sıfırlanır. Elenenler seçilmez.
2. **Yazma (10 sn):** Seçilen oyuncunun yazı kutusu açılır, karakteri durur. Diğerleri hareket edebilir.
   - Saldırı kelimesiyse yazan kişi, kelime kabul edildikten sonra 3 sn içinde fareyle bir oyuncuya tıklayarak hedef seçer. Süre dolarsa rastgele bir rakip seçilir; botlar rastgele seçer. Kimse kendini hedefleyemez.
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
| Zar: seçilmeme bonusu | her seçilmeyen tur için +0,5 ağırlık (`diceBonusPerMissedTurn`) |

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
- Tsunami: 5 sn uyarıdan sonra arenanın rastgele bir kenarından saniyede 4 birim hızla dev dalga gelir; önü geçtiği herkes elenir (kalkan bir kez engeller). 3 kule çıkar, her biri 3 kişi alır; kuledekiler kurtulur.
- Meteor yağmuru: haritada sığınak yoktur; 12 sn boyunca 30 meteor eşit aralıkla düşer. Her meteor önce yerde 1,5 sn bir gölge gösterir (yarıçap 1,5), sonra gölgedeki herkese 1 hasar verir. Meteorların bir kısmı (%40) rastgele bir oyuncunun yakınına, kalanı rastgele yere düşer. Çözüm gölgeleri görüp kaçmaktır; kalkan bir meteoru engeller.
- Duvar (wall): yazanın baktığı yönün 1,8 birim önünde, o yöne dik, 4 birim uzunluğunda, 10 sn duran engel. Oyuncular, botlar ve yerdeki (uçmayan) yaratıklar içinden geçemez; uçan yaratıklar (Arı), kanatla uçan oyuncular ve yağmur bulutu geçer. Yaratıklar duvarın çevresinden dolaşmayı bilmez, duvara takılırlar.
- Çekme (Mıknatıs, pull): arenanın merkezine düşer ve süresi boyunca (4 sn) hedefi saniyede 4 birim hızla oraya çeker. Oyuncu hızı 5 olduğu için ters yönde yürüyerek direnebilir. Botlar mıknatıstan kaçmaz.
- Yavaşlatma (slowdown): hız yarıya iner. Alan yaratıkları (Bulut) içindeki herkesi yavaşlatır; alandan çıkınca etki 0,6 sn daha sürer.
- Vur-kaç yaratıklar (ör. Kurt, predator): vurduktan sonra 2 sn geri çekilir, ömrünün son 1,5 sn'sinde sıkılıp gider. Sürü (swarm) üyelerinin ilk vuruşları 0,8 sn arayla kademelidir.
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
- Hayaletler tam olarak ne yapacak?
- Yazan kişi kendi tehdidinden etkilenecek mi?
- Ağ kütüphanesi seçimi (Aşama 3)
