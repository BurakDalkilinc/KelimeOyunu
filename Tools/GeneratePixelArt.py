"""
KelimeOyunu: kodla piksel sanat üretici.

Her sprite 16x16 pikseldir (Unity'de 16 PPU = 1 birim). Ortak palet ve otomatik siyah kenar çizgisi kullanılır.
Hareketli sprite'lar için ikinci bir kare (dosyaadi_b.png) de üretilir: aynı çizimin 1 piksel aşağı kaymış hâli.

Çalıştırma (proje klasöründen):
    python Tools/GeneratePixelArt.py

Çıktı: Assets/Resources/Sprites/*.png
Bir sprite'ı kendi çiziminle değiştirmek için aynı adlı PNG'yi (16x16, şeffaf arka plan) bu klasöre koyman yeterli;
Unity piksel ayarlarını (Point filtre, 16 PPU, sıkıştırma yok) kendisi uygular. Betik, var olan dosyaların üzerine yazar;
değiştirdiklerini korumak için betikten ilgili satırı sil ya da dosyayı yedekle.
"""
import math
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites")

# ---- Palet ----
K = (26, 28, 44)        # kenar çizgisi
W = (255, 255, 255)
w = (200, 208, 220)
g = (139, 147, 167)
G = (86, 96, 117)
R = (228, 59, 68)
r = (162, 38, 51)
O = (247, 118, 34)
Y = (254, 231, 97)
y = (229, 165, 10)
L = (99, 199, 77)
l = (62, 137, 72)
D = (38, 92, 66)
B = (79, 164, 240)
b = (31, 79, 191)
C = (190, 235, 255)
P = (181, 80, 136)
p = (104, 56, 108)
S = (242, 201, 160)
s = (217, 154, 115)
N = (139, 90, 43)
n = (90, 58, 30)
PINK = (255, 163, 172)
INK = (52, 54, 74)


class Canvas:
    def __init__(self, w_=16, h_=16):
        self.w = w_
        self.h = h_
        self.px = [[None] * w_ for _ in range(h_)]

    # -- temel --
    def set(self, x, y_, c):
        if 0 <= x < self.w and 0 <= y_ < self.h:
            self.px[y_][x] = c

    def rect(self, x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                self.set(xx, yy, c)

    def sym_rect(self, x0, y0, x1, y1, c):
        """Sol yarıya çizer ve sağa aynalar (16 genişlikte x -> 15-x)."""
        self.rect(x0, y0, x1, y1, c)
        self.rect(self.w - 1 - x1, y0, self.w - 1 - x0, y1, c)

    def sym_set(self, x, y_, c):
        self.set(x, y_, c)
        self.set(self.w - 1 - x, y_, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for yy in range(self.h):
            for xx in range(self.w):
                if ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1.0:
                    self.px[yy][xx] = c

    def erase_ellipse(self, cx, cy, rx, ry):
        for yy in range(self.h):
            for xx in range(self.w):
                if ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1.0:
                    self.px[yy][xx] = None

    def erase_rect(self, x0, y0, x1, y1):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                if 0 <= xx < self.w and 0 <= yy < self.h:
                    self.px[yy][xx] = None

    def poly(self, pts, c):
        for yy in range(self.h):
            for xx in range(self.w):
                if self._inside(xx + 0.5, yy + 0.5, pts):
                    self.px[yy][xx] = c

    @staticmethod
    def _inside(x, y_, pts):
        inside = False
        j = len(pts) - 1
        for i in range(len(pts)):
            xi, yi = pts[i]
            xj, yj = pts[j]
            if (yi > y_) != (yj > y_) and x < (xj - xi) * (y_ - yi) / (yj - yi + 1e-9) + xi:
                inside = not inside
            j = i
        return inside

    def recolor(self, old, new, y0=0, y1=None):
        y1 = self.h - 1 if y1 is None else y1
        for yy in range(y0, y1 + 1):
            for xx in range(self.w):
                if self.px[yy][xx] == old:
                    self.px[yy][xx] = new

    def outline(self, c=K):
        src = [row[:] for row in self.px]
        for yy in range(self.h):
            for xx in range(self.w):
                if src[yy][xx] is not None:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = xx + dx, yy + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and src[ny][nx] is not None:
                        self.px[yy][xx] = c
                        break

    def shifted(self, dy):
        out = Canvas(self.w, self.h)
        for yy in range(self.h):
            ny = yy + dy
            if 0 <= ny < self.h:
                out.px[ny] = self.px[yy][:]
        return out

    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for yy in range(self.h):
            for xx in range(self.w):
                c = self.px[yy][xx]
                if c is not None:
                    img.putpixel((xx, yy), (c[0], c[1], c[2], 255))
        img.save(os.path.join(OUT, name + ".png"))


def save_animated(cv, name):
    """Ana kare + 1 piksel aşağı kaymış ikinci kare (zıplama)."""
    cv.save(name)
    cv.shifted(1).save(name + "_b")


# ---------------- Karakter (beyaz gövde: Unity'de renkle boyanır) ----------------
def make_char():
    c = Canvas()
    c.ellipse(7.5, 8.5, 5.6, 5.2, W)
    c.recolor(W, w, 11, 15)                 # alt kısım gölge
    c.sym_rect(4, 13, 6, 14, w)             # ayaklar
    c.sym_rect(5, 7, 6, 8, K)               # gözler (2x2)
    c.sym_set(5, 7, W)                      # göz parlaması
    c.outline()
    return c


# ---------------- Yaratıklar ----------------
def make_zombi():
    c = Canvas()
    c.sym_rect(2, 8, 4, 9, L)               # kollar (öne uzanmış)
    c.ellipse(7.5, 6.5, 4.6, 4.2, L)        # kafa
    c.rect(5, 10, 10, 13, l)                # gövde (yırtık gömlek)
    c.sym_rect(5, 14, 6, 14, D)             # bacaklar
    c.sym_rect(5, 5, 5, 6, R)               # kırmızı gözler
    c.rect(6, 8, 9, 8, K)                   # ağız
    c.sym_set(6, 8, W)
    c.set(7, 10, D); c.set(8, 11, D)        # yırtık izi
    c.outline()
    return c


def make_ari():
    c = Canvas()
    c.sym_rect(1, 4, 3, 7, C)               # kanatlar
    c.sym_rect(2, 3, 2, 3, C)
    c.ellipse(7.5, 6, 3.2, 2.8, Y)          # kafa
    c.ellipse(7.5, 10.5, 3.6, 3.2, Y)       # gövde
    for yy in (10, 12):                     # çizgiler
        for xx in range(16):
            if c.px[yy][xx] == Y:
                c.px[yy][xx] = K
    c.sym_set(6, 6, K)                      # gözler
    c.sym_set(5, 5, W)
    c.set(7, 14, K); c.set(8, 14, K)        # iğne
    c.outline()
    return c


def make_fare():
    c = Canvas()
    c.sym_set(2, 6, PINK)
    c.ellipse(4, 5, 2.2, 2.2, g)            # kulaklar
    c.ellipse(11, 5, 2.2, 2.2, g)
    c.ellipse(4, 5, 1.1, 1.1, PINK)
    c.ellipse(11, 5, 1.1, 1.1, PINK)
    c.ellipse(7.5, 8, 4.4, 3.4, g)          # kafa
    c.ellipse(7.5, 11.6, 3.6, 2.4, w)       # gövde
    c.sym_set(5, 7, K)                      # gözler
    c.sym_set(7, 9, PINK)                   # burun
    c.set(8, 9, PINK)
    for xx, yy in ((12, 12), (13, 12), (14, 11), (14, 10)):   # kuyruk
        c.set(xx, yy, PINK)
    c.sym_set(3, 9, w)                      # bıyık
    c.outline()
    return c


def make_kurt():
    c = Canvas()
    c.poly([(2, 1), (7, 6), (2, 8)], g)     # kulaklar
    c.poly([(14, 1), (9, 6), (14, 8)], g)
    c.poly([(3, 3), (6, 6), (3, 7)], PINK)
    c.poly([(13, 3), (10, 6), (13, 7)], PINK)
    c.ellipse(7.5, 9, 5, 4.6, g)            # kafa
    c.ellipse(7.5, 12, 2.8, 2.2, w)         # burun bölgesi
    c.sym_set(5, 8, Y)                      # sarı gözler
    c.sym_set(6, 8, K)
    c.set(7, 11, K); c.set(8, 11, K)        # burun
    c.sym_set(6, 13, W)                     # dişler
    c.outline()
    return c


def make_bulut():
    c = Canvas()
    c.ellipse(7.5, 5.5, 3.6, 3.0, w)
    c.ellipse(11, 7.5, 3.2, 2.6, w)
    c.ellipse(4, 7.5, 3.2, 2.6, w)
    c.rect(3, 8, 12, 9, w)
    c.recolor(w, g, 9, 9)                   # alt gölge
    for xx, yy in ((6, 4), (7, 4), (4, 6), (10, 6)):        # parlak noktalar
        c.set(xx, yy, W)
    for xx in (4, 8, 11):                   # yağmur damlaları
        c.set(xx, 12, B); c.set(xx, 13, B)
    c.outline()
    return c


def make_kaplumbaga():
    c = Canvas()
    c.sym_rect(3, 12, 4, 13, L)             # ayaklar
    c.ellipse(7.5, 8.5, 5.2, 4.2, l)        # kabuk
    c.ellipse(7.5, 8, 2.6, 1.8, L)          # kabuk deseni
    c.recolor(l, D, 11, 13)
    c.ellipse(13, 9.5, 1.8, 1.8, L)         # kafa (sağda)
    c.set(13, 9, K)                         # göz
    c.set(2, 11, L)                         # kuyruk
    for xx, yy in ((5, 6), (10, 6), (7, 11), (8, 11)):
        c.set(xx, yy, D)
    c.outline()
    return c


def make_yumruk():
    c = Canvas()
    c.rect(4, 3, 11, 9, S)                  # yumruk
    c.rect(3, 7, 5, 10, S)                  # başparmak
    for xx in (6, 8, 10):                   # parmak araları
        for yy in range(3, 7):
            c.set(xx, yy, s)
    c.rect(11, 5, 11, 9, s)                 # gölge
    c.rect(5, 11, 10, 13, B)                # kol (kol ağzı)
    c.rect(5, 11, 10, 11, b)
    c.set(2, 2, Y); c.set(13, 2, Y); c.set(1, 5, Y); c.set(14, 5, Y)   # darbe çizgileri
    c.outline()
    return c


def make_muz():
    c = Canvas()
    c.ellipse(7.5, 9, 6.4, 4.4, Y)
    c.erase_ellipse(7.5, 6, 5.4, 3.4)       # hilal şekli
    c.recolor(Y, y, 11, 13)                 # alt gölge
    for yy in range(16):                    # uçlar kahverengi
        xs = [xx for xx in range(16) if c.px[yy][xx] is not None]
        if xs and yy in (6, 7):
            c.set(xs[0], yy, N)
            c.set(xs[-1], yy, N)
    c.outline()
    return c


def make_miknatis():
    c = Canvas()
    c.rect(3, 4, 5, 11, R)                  # sol bacak
    c.rect(10, 4, 12, 11, R)                # sağ bacak
    c.rect(3, 11, 12, 13, R)                # taban
    c.rect(6, 4, 9, 10, None)               # iç boşluk
    c.rect(10, 7, 12, 11, B)                # sağ bacağın alt yarısı mavi
    c.rect(3, 2, 5, 4, w)                   # gümüş uçlar
    c.rect(10, 2, 12, 4, w)
    c.recolor(R, r, 13, 13)
    c.set(2, 1, Y); c.set(13, 1, Y)         # çekim kıvılcımı
    c.outline()
    return c


def make_bomba():
    c = Canvas()
    c.ellipse(7.5, 10, 5.2, 4.8, INK)
    c.set(5, 8, w); c.set(6, 8, w); c.set(5, 9, w)      # parlama
    c.rect(9, 4, 10, 5, N)                  # fitil
    c.set(11, 3, N); c.set(12, 2, N)
    c.set(13, 1, Y); c.set(12, 1, O); c.set(13, 2, O)   # kıvılcım
    c.outline()
    return c


def make_ejderha():
    c = Canvas()
    c.poly([(0, 5), (4, 8), (0, 11)], D)    # kanatlar
    c.poly([(16, 5), (12, 8), (16, 11)], D)
    c.poly([(3, 0), (6, 5), (3, 5)], Y)     # boynuzlar
    c.poly([(13, 0), (10, 5), (13, 5)], Y)
    c.ellipse(7.5, 9, 5, 4.4, L)            # kafa
    c.ellipse(7.5, 12, 3.2, 2.2, l)         # burun
    c.sym_set(5, 8, O)                      # gözler
    c.sym_set(6, 8, K)
    c.sym_set(6, 12, K)                     # burun delikleri
    c.set(7, 14, O); c.set(8, 14, O)        # ateş
    c.outline()
    return c


def make_unknown():
    c = Canvas()
    c.ellipse(7.5, 8, 5, 5, P)
    c.rect(3, 9, 12, 13, P)
    for xx in (5, 8, 10):                   # dalgalı alt kenar
        c.erase_rect(xx, 13, xx, 13)
    for xx, yy in ((7, 4), (8, 4), (6, 5), (9, 5), (9, 6), (8, 7), (7, 8), (7, 10)):   # soru işareti
        c.set(xx, yy, W)
    c.outline()
    return c


# ---------------- Çözüm alanı simgeleri ----------------
def make_shelter():
    c = Canvas()
    c.ellipse(7.5, 11, 6.2, 6, g)           # kubbe
    c.erase_rect(0, 12, 15, 15)
    c.rect(1, 11, 14, 12, G)                # taban
    c.rect(6, 8, 9, 12, INK)                # kapı
    c.set(7, 6, w); c.set(8, 6, w)          # havalandırma
    c.set(4, 8, w); c.set(11, 8, w)
    c.outline()
    return c


def make_tower():
    c = Canvas()
    c.rect(6, 4, 9, 13, N)                  # kule gövdesi
    c.rect(3, 2, 12, 3, n)                  # platform
    c.sym_rect(3, 1, 3, 1, n)               # korkuluk
    c.sym_rect(4, 1, 4, 1, n)
    c.rect(5, 13, 10, 14, n)                # taban
    for yy in (6, 8, 10):                   # merdiven
        c.rect(7, yy, 8, yy, n)
    c.outline()
    return c


def make_cave():
    c = Canvas()
    c.poly([(0, 14), (8, 1), (16, 14)], N)  # dağ
    c.poly([(4, 14), (8, 7), (12, 14)], n)  # mağara ağzının kenarı
    c.poly([(5, 14), (8, 9), (11, 14)], INK)  # mağara ağzı
    c.sym_set(6, 5, s)
    c.sym_set(3, 11, s)
    c.outline()
    return c


def make_houses():
    c = Canvas()
    c.rect(3, 8, 12, 13, S)                 # duvarlar
    c.poly([(1, 8), (8, 2), (15, 8)], R)    # çatı
    c.rect(7, 10, 9, 13, N)                 # kapı
    c.rect(4, 9, 5, 10, B)                  # pencere
    c.rect(10, 9, 11, 10, B)
    c.outline()
    return c


# ---------------- Zemin / duvar karoları (kenar çizgisi yok, tam opak) ----------------
def make_floor():
    c = Canvas()
    base_a, base_b = (40, 45, 72), (35, 40, 66)
    for yy in range(16):
        for xx in range(16):
            c.px[yy][xx] = base_a if ((xx // 8) + (yy // 8)) % 2 == 0 else base_b
    for xx, yy in ((1, 1), (9, 9), (4, 12), (12, 4)):       # küçük detaylar
        c.px[yy][xx] = (52, 58, 90)
    return c


def make_wall():
    c = Canvas()
    brick, mortar = (166, 98, 62), (90, 58, 30)
    for yy in range(16):
        for xx in range(16):
            row = yy // 4
            c.px[yy][xx] = brick
            if yy % 4 == 3:
                c.px[yy][xx] = mortar
            offset = 0 if row % 2 == 0 else 4
            if (xx + offset) % 8 == 7 and yy % 4 != 3:
                c.px[yy][xx] = mortar
            if yy % 4 == 0 and c.px[yy][xx] == brick:
                c.px[yy][xx] = (190, 120, 80)               # üst parlama
    return c


def make_border():
    c = Canvas()
    stone, dark = (112, 120, 144), (70, 76, 100)
    for yy in range(16):
        for xx in range(16):
            c.px[yy][xx] = stone
            if yy % 8 == 7 or (xx + (0 if yy < 8 else 4)) % 8 == 7:
                c.px[yy][xx] = dark
    return c


def main():
    animated = {
        "char": make_char, "zombi": make_zombi, "ari": make_ari, "fare": make_fare, "kurt": make_kurt,
        "bulut": make_bulut, "kaplumbaga": make_kaplumbaga, "ejderha": make_ejderha, "unknown": make_unknown,
    }
    static = {
        "yumruk": make_yumruk, "muz_kabugu": make_muz, "miknatis": make_miknatis, "bomba": make_bomba,
        "shelter": make_shelter, "tower": make_tower, "cave": make_cave, "houses": make_houses,
        "floor": make_floor, "wall": make_wall, "border": make_border,
    }
    for name, fn in animated.items():
        save_animated(fn(), name)
    for name, fn in static.items():
        fn().save(name)
    print("Üretildi:", len(animated) * 2 + len(static), "PNG ->", OUT)


if __name__ == "__main__":
    main()
