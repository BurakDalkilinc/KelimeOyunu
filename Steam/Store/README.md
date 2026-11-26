# WordRoyale — Steam mağaza görselleri

Hepsi kodla üretilir: `python Tools/GenerateStoreArt.py` (proje klasöründen). Elle düzenleme yapma; betiği değiştirip yeniden çalıştır.
Küçük çözünürlükte çizilip yalnız tam sayı katla (NEAREST) büyütülür, yani pikseller nettir. Sahnede yalnız oyunun kendi sprite'ları var.
Logo elle çizilmiş piksel harflerden oluşur (Pixelify Sans yalnız önizleme etiketlerinde kullanılır).

## Steamworks'te nereye yüklenir

**Store Admin → Graphical Assets (Mağaza)**

| Dosya | Boyut | Steamworks alanı |
| --- | --- | --- |
| `header_capsule.png` | 920x430 | Header Capsule |
| `small_capsule.png` | 462x174 | Small Capsule |
| `main_capsule.png` | 1232x706 | Main Capsule |
| `vertical_capsule.png` | 748x896 | Vertical Capsule |
| `page_background.png` | 1438x810 | Page Background (isteğe bağlı) |

**Store Admin → Library Assets (Kütüphane)**

| Dosya | Boyut | Steamworks alanı |
| --- | --- | --- |
| `library_capsule.png` | 600x900 | Library Capsule |
| `library_hero.png` | 3840x1240 | Library Hero (yazısız; Steam logoyu üstüne koyar) |
| `logo.png` | 1280x720, saydam | Library Logo (yükledikten sonra logonun hero üzerindeki yerini Steamworks önizlemesinde ayarla) |
| `header_capsule.png` | 920x430 | Library Header Capsule (aynı dosya kullanılabilir) |

**App Admin → Client Images / Community (Simgeler)**

| Dosya | Boyut | Not |
| --- | --- | --- |
| `icon.png` | 256x256 | Community Icon için kaynak (Steam 184x184 JPG ister: 256'yı oraya küçültmek bulanıklaştırır; gerekirse betiğe 184 sürümü eklenir) |
| `icon_32.png` | 32x32 | Client Icon (.ico) ve oyunun .exe simgesi için kaynak |

## Diğer dosyalar
- `_logo_1x.png`: logonun yerel (1x) hâli; başka boyut gerekirse bunu tam sayı katla büyüt.
- `_preview.png`: hepsinin küçültülmüş kolajı, yalnız göz atmak için. Steam'e yüklenmez.

## Kurallar (Steam)
Kapsüllerde logodan başka yazı yok (indirim, "yakında", ödül vb. eklenmez). Yazı kutusu motifi bile harfsizdir.
