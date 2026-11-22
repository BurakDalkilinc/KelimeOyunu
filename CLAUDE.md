# Kelime Battle Royale — Proje Kuralları

Bu dosyayı Claude her oturumda okur. Projenin "anayasası" budur.

## Oyun tek cümlede
20 oyuncu bir alanda hayatta kalmaya çalışır. Her tur zar atılır, seçilen tek oyuncu 10 saniyede bir kelime yazar.
Kelime yaratığa/olaya dönüşür, diğerleri gerçek zamanlı kaçar. Son kalan kazanır.

Ayrıntılar: `Docs/GDD.md`. Sıradaki işler: `Docs/YOL_HARITASI.md`.

## Değişmez oyun kuralları
- Zar sadece **kimin yazacağını** seçer. Kelimenin gücünü belirlemez.
- Her kelime (ve eş anlamlıları) maçta **bir kez** kullanılır.
- Büyük tehdit (`scope: "global"`) geldiğinde haritada **çözümü de çıkar** (atom bombası → sınırlı yerli sığınak).
- Elenenler hayalet olur ve oylamayla oyuna katılmaya devam eder.
- Kelime doğrulaması ve sonuçlar **sunucuda** hesaplanır; istemciye güvenilmez.

## Teknik
- Unity 6, 2D, C#. Platform: PC (Steam).
- Kelimeler kodda değil, `Assets/Resources/Data/words.json` içinde. Yeni kelime = JSON'a satır, kod değişikliği değil.
- Davranışlar **etiketlerden** gelir (`fast`, `flying`, `explosive`...). Kelimeye özel `if (word == "zombi")` kodu YASAK.
- Türkçe metin normalizasyonu: küçük harfe çevirirken `CultureInfo("tr-TR")` kullan ("I" → "ı", "İ" → "i").

## Klasör yapısı
```
Assets/
  Scripts/
    Core/        # GameManager, TurnManager, Dice, MatchState
    Words/       # WordDatabase, WordNormalizer, TagBehaviours
    Creatures/   # Creature, CreatureSpawner, tag davranışları
    Threats/     # Global tehditler ve çözümleri (Shelter, Tower...)
    Player/      # PlayerController, Health, Ghost
    Net/         # Ağ kodu (Aşama 3'ten itibaren)
    UI/          # Yazı kutusu, zar animasyonu, skor
  Resources/Data/words.json
  Tests/EditMode/
Docs/
```

## Kod kuralları
- Sınıf, metot ve değişken adları İngilizce; yorumlar ve oyuncuya görünen metinler Türkçe.
- Bir dosya = bir sınıf. MonoBehaviour'ları küçük tut, mantığı test edilebilir düz C# sınıflarına koy.
- Sihirli sayı yok: süreler ve değerler `GameConfig` ScriptableObject'inde.
- Oyun mantığını (tur, zar, kelime kilitleme, eleme) ağdan bağımsız yaz; Aşama 3'te ağ katmanı bunu saracak.

## Çalışma şekli
- Büyük bir işe başlamadan önce kısa bir plan yaz ve onay al.
- Her özellikten sonra: derleme hatası yok mu kontrol et, varsa EditMode testi ekle/çalıştır.
- İşi bitirince `Docs/YOL_HARITASI.md` içindeki ilgili kutucuğu işaretle.
- Kullanıcı yeni başlıyor: yaptığın şeyi 2-3 cümleyle, sade Türkçe açıkla ve Unity Editor'da elle yapması gereken adımları numaralı yaz.

## Ajanlar (`.claude/agents/`)
| Ajan | Ne zaman |
| --- | --- |
| `oyun-tasarimcisi` | Kural, denge, yeni mekanik kararı |
| `kelime-ustasi` | Kelime ekleme, etiket, eş anlamlı, denge sayıları |
| `unity-gelistirici` | C# kodu, sahne, prefab, UI |
| `ag-uzmani` | Çok oyunculu, lobi, Steam, senkronizasyon |
| `test-kontrol` | Kod incelemesi, test, hata ayıklama |
