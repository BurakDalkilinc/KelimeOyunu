---
name: yeni-kelime
description: Oyuna yeni kelime(ler) ekler. Kullanım - /yeni-kelime ejderha, tornado, kalkan
---

Kullanıcının verdiği kelimeleri oyuna ekle: $ARGUMENTS

1. `kelime-ustasi` ajanını kullan.
2. Her kelime için `Assets/Resources/Data/words.json` içinde zaten var mı (id ya da alias olarak) kontrol et.
3. Yoksa şemaya uygun kayıt oluştur: kategori, kapsam, etiketler, statlar, eş anlamlılar, açıklama. Global tehditse `counter` ekle.
4. Yeni bir etiket gerekiyorsa ekleme; bunun yerine kullanıcıya hangi etiketin gerektiğini ve neden olduğunu sor.
5. JSON'u doğrula, sonra eklenenleri şu tabloyla göster: Kelime | Kategori | Etiketler | Ne yapar | Nasıl kaçılır / çözüm.
