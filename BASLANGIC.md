# Başlangıç Rehberi — Oyunu Claude ile Yapmak

Bu paket, Claude Code'un oyunu senin için adım adım yapmasını sağlar. Paketin içinde şunlar var:

| Dosya | Ne işe yarar |
| --- | --- |
| `CLAUDE.md` | Projenin kuralları. Claude her oturumda okur. |
| `.claude/agents/` | 5 uzman ajan: tasarımcı, kelime ustası, Unity geliştirici, ağ uzmanı, test kontrol |
| `.claude/skills/` | Kısayol komutları: `/sonraki-adim`, `/yeni-kelime` |
| `Docs/GDD.md` | Oyun tasarım dokümanı (kurallar, sayılar, etiketler) |
| `Docs/YOL_HARITASI.md` | Yapılacaklar listesi; Claude buradan sıradaki işi alır |
| `Assets/Resources/Data/words.json` | İlk 20 kelime |
| `.gitignore` | Unity için hazır git ayarı |

---

## 1. Gerekenleri kur
1. **Unity Hub** ve içinden **Unity 6** (LTS sürümü). Kurarken "Windows Build Support" seçili olsun.
2. **Git** (kodun yedeği ve geri alma için şart).
3. **Claude Code** — kurulum: https://docs.claude.com/en/docs/claude-code/overview

## 2. Unity projesini oluştur
1. Unity Hub → **New project** → **Universal 2D** şablonu.
2. Proje adı örneğin `KelimeOyunu`, konum boşluksuz bir yol olsun (`C:\Oyunlar\KelimeOyunu`).
3. Proje açılınca Unity'yi kapatabilirsin.

## 3. Paketi projeye kopyala
Bu paketin **içindekileri** proje klasörüne kopyala. Sonuç şöyle görünmeli:
```
KelimeOyunu/
  Assets/            ← Unity'nin klasörü (Resources/Data/words.json eklenmiş olacak)
  Packages/
  ProjectSettings/
  .claude/           ← gizli klasör olabilir, Windows'ta "Gizli öğeleri göster"i aç
  Docs/
  CLAUDE.md
  BASLANGIC.md
  .gitignore
```

## 4. (Önerilir) Unity MCP — Claude'un Unity'yi doğrudan kullanması
MCP olmadan Claude kodu yazar, sahneyi sen elle kurarsın. MCP ile Claude sahneye obje ekleyebilir, Console hatalarını okuyabilir, testleri çalıştırabilir.
Seçenekler:
- Unity'nin kendi MCP desteği (Unity AI paketleri içinde; Unity dokümanlarına bak)
- Topluluk projesi **MCP for Unity**: https://github.com/CoplayDev/unity-mcp

Hangisini seçersen onun kendi sayfasındaki "Claude Code" kurulum adımlarını izle. Takılırsan Claude'a sayfanın linkini verip "bunu kurmama yardım et" diyebilirsin.

## 5. Claude'u başlat
Proje klasöründe bir terminal aç ve `claude` yaz. İlk mesaj olarak şunu gönder:

> CLAUDE.md, Docs/GDD.md ve Docs/YOL_HARITASI.md dosyalarını oku. Sonra git deposunu kur, ilk commit'i at ve /sonraki-adim ile başla.

## 6. Günlük kullanım
- **`/sonraki-adim`** → Yol haritasındaki sıradaki işi doğru ajanla yapar, sonra nasıl deneyeceğini anlatır.
- **`/yeni-kelime ejderha, tornado, kalkan`** → Kelimeleri doğru etiketlerle oyuna ekler.
- Ajanı adıyla çağır:
  - "oyun-tasarimcisi ajanıyla hayaletlerin ne yapacağını tasarla"
  - "test-kontrol ajanıyla TurnManager'ı incele"
  - "ag-uzmani ajanıyla Mirror ile Fish-Net'i karşılaştır"

## 7. Altın kurallar
1. **Her adımdan sonra Unity'de Play'e bas ve dene.** Claude çalıştığını söylese bile kendin gör.
2. **Her çalışan adımdan sonra commit at:** "çalışıyor, commit at" demen yeter. Bir şey bozulursa geri dönebilirsin.
3. **Hatayı olduğu gibi yapıştır.** Unity Console'daki kırmızı hatayı kopyalayıp Claude'a ver.
4. **Bir seferde tek özellik iste.** "Bütün oyunu yap" yerine "zar sistemini yap".
5. **Kural değişikliğini önce tasarımcıya söylet.** Böylece GDD güncel kalır ve diğer ajanlar da doğru bilgiyle çalışır.
