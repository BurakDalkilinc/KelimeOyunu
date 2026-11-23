# Hile kontrolleri (sunucu doğrulama listesi)

İlke: **host yetkilidir.** İstemci yalnızca niyetini söyler ("şu yöne basıyorum", "şunu yazdım", "şuna tıkladım");
sonucu, süreyi, canı, konumu ve kuralı host hesaplar. İstemcinin söylediği kimlik asla kullanılmaz: gönderen
bağlantıdan bulunur (`rpcParams.Receive.SenderClientId`).

## İstemciden host'a giden her mesaj

| Mesaj | Kim gönderebilir | Host'ta doğrulananlar | Sınır | Test |
| --- | --- | --- | --- | --- |
| `MoveInputRpc(yön)` (`NetworkPlayer`) | Yalnızca karakterin sahibi (`RpcInvokePermission.Owner`) | Bot karakter için yok sayılır; yön `AvatarMover.SanitizeInput` ile temizlenir (NaN/sonsuz → 0, uzunluk ≤ 1); hız, yavaşlama, duvar, arena sınırı host'ta uygulanır; konum istemciden alınmaz | 30 mesaj patlama, 30/sn yenilenme | `AvatarMoverTests`, `RpcRateLimiterTests` |
| `ShoveRpc()` (`NetworkPlayer`) | Yalnızca karakterin sahibi (`RpcInvokePermission.Owner`) | Bot için yok sayılır; bekleme süresi host'ta (`ShoveCooldown`, 3 sn), yaşıyor ve uçmuyor mu, kimin vurulacağı ve savrulma miktarı host'ta (`ShoveRules`); istemci yön ya da mesafe gönderemez | 3 patlama, 1/sn (ayrıca bekleme süresi) | `ShoveTests` |
| `SubmitWordRpc(metin)` (`NetTurnChannel`) | Herkes | Sıra bu oyuncuda mı, aşama doğru mu (`TurnManager.SubmitWord`); kelime uzunluğu, yasaklı, daha önce kullanılmış, eş anlamlı kilidi; 200 karakterden uzun metin hiç işlenmez | 5 patlama, 2/sn | `NetTurnTests`, `NetBotTakeoverTests` |
| `ChooseTargetRpc(hedef)` | Herkes | Seçim açık mı, gönderen o turun yazanı mı, hedef geçerli mi (kendisi değil, elenmemiş, görünmez değil) | 8 patlama, 4/sn | `NetMatchSimTests` |

Hız sınırı aşılınca mesaj sessizce yok sayılır (normal oyun sınırlara takılmaz: testle doğrulandı).

## Host'un kendi hesapladıkları (istemciden asla alınmaz)

- Zar, sıra, süreler, kelime doğrulaması, kelime kilidi (`NetTurnHost`).
- Yaratıkların doğması, hareketi, hasarı, fırlatma, yavaşlama, kalkan, uçma, görünmezlik (`NetMatchSim`).
- Duvarlar, çözüm alanları, büyük tehditlerin etkileri, yeniden doğuşta kimin döneceği (rastgele).
- Can, eleme, yeniden doğuş, kazanan.
- Oyuncu konumu (`NetworkVariable`; yalnızca host yazar), karakter hızı.

## Bağlantı kuralları

- Maç başladıktan sonra bağlanan oyuncu kabul edilmez (kadroda yeri yok); oda 20 kişiyi aşamaz.
- Bağlantısı kopan insan botlaşır; karakteri host'ta kalır, kimse onu istemciden süremez.
- Oyuncu numarası = bağlantı numarası (host'ta atanır, istemci seçemez); botlar 100'den başlar.

## Bilinen sınırlar (şimdilik)

- Host, oyunu kuran kişidir: host'un kendisi hile yapabilir (host yetkili model). Ayrık sunucu (dedicated server) Aşama 6+ konusu.
- Bağlantı onayı/parola yok: Steam lobisi "yalnızca arkadaşlar" ile korunur; yerel ağda IP'yi bilen bağlanabilir.
- Hız sınırı yalnızca mesaj sayısını sınırlar; bant genişliği DoS'una karşı Steam Relay ve Netcode'un kendi sınırlarına güveniyoruz.
- Düşen yön mesajı: sel durumunda "tuş bırakıldı" mesajı atılırsa karakter bir süre yürümeye devam edebilir (yalnızca saldırgan kendine zarar verir).
