# Test Notları (Aşama 2: Yerel prototip)

Yol haritasındaki son madde: **3 arkadaş oynadı, notlar buraya yazıldı.**
Oyun şu an tek bilgisayarda oynanıyor (sen + 7 bot). Arkadaşlar sırayla klavyeye geçip oynayabilir ya da başını izleyebilir.

## Nasıl denenir
1. Unity'de `Assets/Scenes/Arena` sahnesini aç, **Play**'e bas, Game penceresine bir kez tıkla.
2. Sıran gelince bir kelime yaz (Türkçe ya da İngilizce), Enter'a bas. Saldırı kelimesiyse 3 sn içinde fareyle bir oyuncuya tıkla.
3. **Boşluk** tuşu: bakığın yönde yakındakini güçlü iter (3 sn bekleme). Sol üstte "İtiş (Boşluk): hazır" yazar.
4. Maç bitince **R** ile yeniden başla.
5. Her arkadaşa en az 2 maç oynat. Onlara bir şey anlatma, sadece izle.

## Denenecek kelimeler
| Tür | Kelimeler |
| --- | --- |
| Saldırı | zombi, kurt, arı, yumruk, bomba, bulut, fare, muz kabuğu, mıknatıs, kaplumbağa |
| Koruma | kalkan, can, duvar, kanat, görünmezlik |
| Büyük tehdit | atom bombası, tsunami, meteor yağmuru, zombi sürüsü, ejderha |

## Sorular (maçtan sonra sor)
- Ne yapacağını ilk 30 saniyede anladı mı? Nerede takıldı?
- Kelime yazarken 10 saniye yetti mi? Çok mu az, çok mu fazla?
- Hangi kelime en çok güldürdü? Hangisi "haksız" hissettirdi?
- Büyük tehditte sığınağa/kuleye yetişebildi mi? Uyarı süresi yeterli miydi?
- Botlar nasıldı: çok mu aptal, çok mu zeki?
- Elenince (hayalet olunca) sıkıldı mı? Beklerken ne yaptı?
- Ekranda okunamayan, anlaşılmayan bir şey var mıydı?
- Aklına gelen yeni kelime fikri?

## Kayıt

### Oyuncu 1: (isim)
- Tarih:
- Maç sayısı:
- Notlar:

### Oyuncu 2: (isim)
- Tarih:
- Maç sayısı:
- Notlar:

### Oyuncu 3: (isim)
- Tarih:
- Maç sayısı:
- Notlar:

## Sonuç: neyi değiştireceğiz?
- (Buraya, testlerden çıkan en önemli 3 değişiklik yazılacak. GDD sayılarını (süreler, hasarlar) `GameConfig` ve `words.json` üzerinden ayarlarız.)
