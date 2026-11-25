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


def make_godzilla():
    c = Canvas()
    c.poly([(2, 15), (5, 11), (6, 13)], l)  # kuyruk
    c.ellipse(8, 10.5, 4.6, 4.2, l)         # gövde
    c.ellipse(9.5, 10.5, 2.4, 3.0, L)       # karın
    c.ellipse(10, 4.8, 3.6, 3.0, l)         # kafa
    c.rect(11, 5, 14, 7, l)                 # burun
    c.rect(12, 7, 14, 7, W)                 # dişler
    c.set(13, 7, K)
    c.set(11, 3, Y); c.set(12, 3, R)        # kızgın göz
    c.rect(11, 9, 13, 10, l)                # kol
    c.set(14, 10, W)                        # pençe
    c.rect(6, 14, 7, 15, D); c.rect(10, 14, 11, 15, D)   # bacaklar
    for xx, yy in ((6, 2), (5, 4), (4, 6), (3, 8), (3, 10)):   # sırt plakaları
        c.set(xx, yy, C); c.set(xx + 1, yy + 1, C)
    c.outline()
    return c


# ---------------- Kelime paketi 2: silah mermileri ----------------
def make_ok():
    c = Canvas()
    for i in range(3, 13):                  # ok gövdesi (çapraz)
        c.set(i, 15 - i, N)
    c.poly([(12, 1), (14, 1), (14, 3)], w)  # ok ucu
    c.set(13, 2, w)
    c.set(2, 12, R); c.set(3, 13, R); c.set(2, 14, R); c.set(1, 13, R)   # tüyler
    c.outline()
    return c


def make_bumerang():
    c = Canvas()
    c.rect(3, 3, 6, 12, N)                  # dikey kol
    c.rect(3, 9, 12, 12, N)                 # yatay kol
    c.rect(4, 4, 5, 10, y); c.rect(5, 10, 11, 11, y)
    c.outline()
    return c


def make_lazer():
    c = Canvas()
    c.rect(2, 6, 13, 9, R)                  # ışın
    c.rect(2, 7, 13, 8, PINK)
    c.rect(1, 7, 14, 8, W)
    c.outline(r)
    return c


def make_kartopu():
    c = Canvas()
    c.ellipse(7.5, 7.5, 3.8, 3.8, W)
    c.ellipse(8.5, 8.5, 2.4, 2.4, C)
    c.set(6, 6, W)
    c.outline(G)
    return c


def make_tas():
    c = Canvas()
    c.ellipse(7.5, 8, 3.2, 2.8, g)
    c.ellipse(7, 7.5, 1.6, 1.2, w)
    c.outline()
    return c


# ---------------- Kelime paketi 2: bosslar ----------------
def make_kingkong():
    c = Canvas()
    c.ellipse(7.5, 10, 5.4, 4.6, n)         # gövde
    c.ellipse(7.5, 10.5, 3.0, 3.2, N)       # göğüs
    c.ellipse(7.5, 4.8, 3.8, 3.4, n)        # kafa
    c.ellipse(7.5, 5.6, 2.6, 2.0, s)        # yüz
    c.sym_set(6, 4, K)                      # gözler
    c.rect(6, 7, 9, 7, K)                   # ağız
    c.sym_rect(1, 9, 2, 14, n)              # uzun kollar
    c.sym_rect(5, 14, 6, 15, n)             # bacaklar
    c.outline()
    return c


def make_robot():
    c = Canvas()
    c.rect(4, 6, 11, 13, g)                 # gövde
    c.rect(5, 1, 10, 5, w)                  # kafa
    c.sym_rect(6, 3, 6, 3, R)               # kırmızı gözler
    c.set(7, 0, Y); c.set(7, 1, G)          # anten
    c.rect(6, 8, 9, 10, B)                  # göğüs paneli
    c.set(7, 9, C)
    c.sym_rect(2, 7, 3, 12, G)              # kollar
    c.sym_rect(5, 14, 6, 15, G)             # bacaklar
    c.outline()
    return c


def make_yeti():
    c = Canvas()
    c.ellipse(7.5, 9.5, 5.6, 5.0, W)        # tüylü gövde
    c.ellipse(7.5, 5, 3.8, 3.2, W)          # kafa
    c.ellipse(7.5, 6, 2.2, 1.6, C)          # yüz
    c.sym_set(6, 5, B)                      # gözler
    c.rect(6, 7, 9, 7, K)
    c.sym_set(6, 8, W)                      # dişler
    c.sym_rect(1, 9, 2, 13, w)
    c.sym_rect(5, 14, 6, 15, w)
    c.outline()
    return c


def make_orumcek():
    c = Canvas()
    for x0, y0, x1, y1 in ((1, 5, 4, 7), (1, 9, 4, 9), (1, 13, 4, 11), (0, 2, 3, 5)):   # bacaklar
        for t in range(6):
            xx = round(x0 + (x1 - x0) * t / 5); yy = round(y0 + (y1 - y0) * t / 5)
            c.set(xx, yy, INK); c.set(15 - xx, yy, INK)
    c.ellipse(7.5, 10, 3.6, 3.4, INK)       # karın
    c.ellipse(7.5, 5.5, 2.6, 2.2, INK)      # baş
    c.rect(6, 9, 9, 11, R)                  # kırmızı işaret
    c.sym_set(6, 5, R); c.sym_set(7, 4, R)  # gözler
    c.outline(K)
    return c


def make_tepegoz():
    c = Canvas()
    c.ellipse(7.5, 10, 5.4, 4.8, P)         # gövde
    c.ellipse(7.5, 5, 4.2, 3.8, P)          # kafa
    c.ellipse(7.5, 4.5, 2.2, 2.0, W)        # tek göz
    c.ellipse(7.5, 4.5, 1.0, 1.0, K)
    c.rect(5, 8, 10, 8, p)                  # ağız
    c.set(6, 8, W); c.set(9, 8, W)
    c.sym_rect(1, 9, 2, 13, P)
    c.rect(12, 6, 13, 12, N)                # sopa
    c.sym_rect(5, 14, 6, 15, p)
    c.outline()
    return c


# ---------------- Kelime paketi 2: kovalayanlar ----------------
def make_aslan():
    c = Canvas()
    c.ellipse(7.5, 8, 6.0, 5.6, N)          # yele
    c.ellipse(7.5, 8.5, 3.8, 3.6, y)        # yüz
    c.sym_set(6, 7, K)                      # gözler
    c.rect(7, 9, 8, 9, n)                   # burun
    c.rect(6, 11, 9, 11, n)                 # ağız
    c.sym_set(4, 4, y)                      # kulaklar
    c.outline()
    return c


def make_yilan():
    c = Canvas()
    pts = [(2, 13), (3, 12), (4, 12), (5, 13), (6, 13), (7, 12), (8, 11), (8, 10), (7, 9), (6, 8), (6, 7), (7, 6), (8, 5), (9, 5)]
    for xx, yy in pts:
        c.set(xx, yy, L); c.set(xx, yy + 1, l)
    c.ellipse(10.5, 4.5, 2.0, 1.6, L)       # kafa
    c.set(11, 4, K)
    c.set(13, 5, R); c.set(14, 5, R)        # dil
    c.outline()
    return c


def make_hayalet():
    c = Canvas()
    c.ellipse(7.5, 6.5, 5.0, 5.0, W)
    c.rect(3, 7, 12, 12, W)
    for xx in (3, 6, 9, 12):                # dalgalı etek
        c.set(xx, 13, W)
    c.recolor(W, w, 11, 13)
    c.sym_rect(5, 5, 6, 7, K)               # oyuk gözler
    c.ellipse(7.5, 9.5, 1.0, 1.2, K)        # ağız
    c.outline(G)
    return c


def make_yarasa():
    c = Canvas()
    c.poly([(0, 5), (6, 7), (5, 11), (3, 9), (1, 10)], INK)   # sol kanat
    c.poly([(15, 5), (9, 7), (10, 11), (12, 9), (14, 10)], INK)
    c.ellipse(7.5, 8.5, 2.4, 3.0, p)        # gövde
    c.sym_set(6, 5, p)                      # kulaklar
    c.sym_set(6, 8, R)                      # gözler
    c.sym_set(7, 10, W)                     # dişler
    c.outline(K)
    return c


def make_karinca():
    c = Canvas()
    c.ellipse(4, 9, 2.0, 1.8, r)            # baş
    c.ellipse(7.5, 9, 1.6, 1.4, r)          # göğüs
    c.ellipse(11.5, 9, 2.8, 2.2, r)         # karın
    for xx in (6, 8, 10):                   # bacaklar
        c.set(xx, 11, K); c.set(xx - 1, 12, K); c.set(xx, 7, K); c.set(xx - 1, 6, K)
    c.set(3, 6, K); c.set(2, 5, K)          # anten
    c.set(3, 8, W)
    c.outline()
    return c


def make_boga():
    c = Canvas()
    c.ellipse(7.5, 9, 5.0, 4.4, n)          # kafa
    c.poly([(2, 7), (0, 2), (4, 6)], W)     # boynuzlar
    c.poly([(13, 7), (15, 2), (11, 6)], W)
    c.ellipse(7.5, 11.5, 2.8, 2.0, s)       # burun
    c.sym_set(6, 11, K)                     # burun delikleri
    c.sym_set(5, 8, R)                      # öfkeli gözler
    c.set(7, 13, Y); c.set(8, 13, Y)        # halka
    c.outline()
    return c


def make_ninja():
    c = Canvas()
    c.ellipse(7.5, 6, 3.8, 3.6, INK)        # kafa
    c.rect(4, 5, 11, 6, S)                  # göz aralığı
    c.sym_set(6, 5, K)
    c.rect(5, 9, 10, 13, INK)               # gövde
    c.rect(5, 10, 10, 10, R)                # kuşak
    c.set(12, 4, R); c.set(13, 5, R); c.set(14, 4, R)   # uçuşan bant
    c.sym_rect(5, 14, 6, 15, INK)
    c.outline(K)
    return c


# ---------------- Kelime paketi 2: alanlar ve tuzaklar ----------------
def make_yildirim():
    c = Canvas()
    c.poly([(9, 0), (4, 8), (8, 8), (5, 15), (12, 6), (8, 6), (11, 0)], Y)
    c.outline(O)
    return c


def make_mayin():
    c = Canvas()
    c.ellipse(7.5, 9, 5.0, 3.4, G)
    c.ellipse(7.5, 8.5, 3.4, 2.0, g)
    c.rect(7, 5, 8, 6, INK)                 # tetik
    c.set(7, 4, R); c.set(8, 4, R)
    c.outline()
    return c


def make_ag():
    c = Canvas()
    for i in range(16):                     # ağ telleri
        c.set(i, 7, W); c.set(7, i, W); c.set(i, i, w); c.set(i, 15 - i, w)
    for r_ in (3, 6):
        for t in range(32):
            import math
            a = t * math.pi * 2 / 32
            c.set(round(7.5 + r_ * math.cos(a)), round(7.5 + r_ * math.sin(a)), W)
    return c


# ---------------- Kelime paketi 3: kamuflaj ve yakın dövüş silahları ----------------
def make_cali():
    c = Canvas()
    c.ellipse(7.5, 9.5, 6.6, 5.4, D)        # çalı gövdesi
    c.ellipse(4.5, 7, 3.4, 3.0, l)          # yapraklı tümsekler
    c.ellipse(10.5, 6.5, 3.6, 3.2, l)
    c.ellipse(7.5, 5, 3.0, 2.6, L)
    c.set(4, 6, L); c.set(11, 5, L); c.set(6, 10, l); c.set(10, 11, l)
    c.set(5, 9, R); c.set(10, 9, R)         # küçük meyveler
    c.outline()
    return c


def make_kilic():
    c = Canvas()
    for i in range(2, 11):                  # çapraz bıçak
        c.set(i + 2, 13 - i, w); c.set(i + 3, 13 - i, W)
    c.set(14, 1, W)
    c.rect(2, 10, 6, 11, y)                 # balçak
    c.set(3, 12, N); c.set(2, 13, N); c.set(1, 14, Y)   # kabza ve topuz
    c.outline()
    return c


def make_sopa():
    c = Canvas()
    for i in range(0, 12):                  # kalınlaşan tahta sopa
        x = 2 + i; y_ = 13 - i
        c.set(x, y_, N); c.set(x + 1, y_, N)
        if i > 4: c.set(x, y_ - 1, N); c.set(x + 1, y_ - 1, n)
    c.set(2, 14, n); c.set(1, 14, n)
    for xx, yy in ((9, 5), (11, 4), (12, 2)):   # çiviler
        c.set(xx, yy, w)
    c.outline()
    return c


# ---------------- Kelime paketi 4: araçlar (yandan görünüş) ----------------
def make_motor():
    c = Canvas()
    c.ellipse(3.5, 11.5, 2.6, 2.6, INK)     # arka teker
    c.ellipse(12, 11.5, 2.6, 2.6, INK)      # ön teker
    c.ellipse(3.5, 11.5, 1.0, 1.0, g)
    c.ellipse(12, 11.5, 1.0, 1.0, g)
    c.rect(4, 8, 11, 10, R)                 # gövde
    c.rect(5, 7, 8, 7, r)                   # sele
    c.rect(9, 6, 11, 8, R)                  # depo
    c.set(12, 5, g); c.set(13, 4, g); c.set(12, 4, INK)   # gidon
    c.rect(3, 10, 6, 10, w)                 # egzoz
    c.set(13, 8, Y)                         # far
    c.outline()
    return c


def make_araba():
    c = Canvas()
    c.rect(1, 7, 14, 11, B)                 # gövde
    c.rect(4, 4, 11, 7, B)                  # kabin
    c.rect(5, 5, 7, 6, C); c.rect(9, 5, 10, 6, C)   # camlar
    c.rect(1, 9, 14, 9, b)                  # yan şerit
    c.set(14, 8, Y); c.set(1, 8, R)         # far ve stop
    c.ellipse(4, 12, 2.0, 2.0, INK)         # tekerler
    c.ellipse(11.5, 12, 2.0, 2.0, INK)
    c.set(4, 12, g); c.set(11, 12, g)
    c.outline()
    return c


# ---------------- Kelime paketi 3: yaratıklar (önden; voxel modelleri otomatik) ----------------
def make_kopek():
    c = Canvas()
    c.ellipse(7.5, 10.5, 4.2, 3.4, N)       # gövde
    c.ellipse(7.5, 6, 3.4, 3.0, N)          # kafa
    c.sym_rect(3, 3, 4, 7, n)               # sarkık kulaklar
    c.ellipse(7.5, 7.5, 1.8, 1.2, s)        # burun çevresi
    c.rect(7, 7, 8, 7, K)                   # burun
    c.sym_set(6, 5, K)                      # gözler
    c.set(7, 9, R); c.set(8, 9, R)          # dil
    c.sym_rect(5, 13, 6, 15, n)             # bacaklar
    c.outline()
    return c


def make_kedi():
    c = Canvas()
    c.ellipse(7.5, 11, 3.8, 3.2, G)         # gövde
    c.ellipse(7.5, 6.5, 3.6, 3.2, G)        # kafa
    c.poly([(4, 5), (4, 1), (7, 4)], G)     # kulaklar
    c.poly([(11, 5), (11, 1), (8, 4)], G)
    c.sym_set(5, 3, PINK)
    c.sym_rect(5, 6, 6, 6, L)               # yeşil gözler
    c.sym_set(6, 6, K)
    c.set(7, 8, PINK); c.set(8, 8, PINK)    # burun
    c.sym_set(2, 8, w); c.sym_set(3, 9, w)  # bıyık
    c.sym_rect(5, 14, 6, 15, g)
    c.outline()
    return c


def make_timsah():
    c = Canvas()
    c.rect(2, 9, 13, 12, l)                 # gövde
    c.rect(2, 5, 13, 8, L)                  # açık ağız (üst çene)
    c.rect(3, 7, 12, 8, R)                  # ağız içi
    for x in range(3, 13, 2):               # dişler
        c.set(x, 7, W); c.set(x + 1, 8, W)
    c.sym_set(4, 4, Y); c.sym_set(4, 3, L)  # gözler
    c.rect(2, 13, 3, 14, l); c.rect(12, 13, 13, 14, l)
    c.outline()
    return c


def make_ayi():
    c = Canvas()
    c.ellipse(7.5, 10.5, 5.0, 4.4, n)       # gövde
    c.ellipse(7.5, 10.5, 2.8, 2.6, N)       # göbek
    c.ellipse(7.5, 5, 3.8, 3.4, n)          # kafa
    c.sym_set(4, 2, n); c.sym_set(4, 1, n)  # kulaklar
    c.ellipse(7.5, 6.2, 1.8, 1.2, N)        # burun çevresi
    c.rect(7, 6, 8, 6, K)
    c.sym_set(6, 4, K)
    c.sym_rect(1, 9, 2, 12, n)              # pençeler
    c.sym_set(1, 12, W)
    c.outline()
    return c


def make_iskelet():
    c = Canvas()
    c.ellipse(7.5, 4.5, 3.4, 3.2, W)        # kafatası
    c.sym_rect(5, 4, 6, 5, K)               # göz boşlukları
    c.set(7, 6, K); c.set(8, 6, K)
    c.rect(6, 7, 9, 7, w)                   # çene
    c.rect(7, 8, 8, 13, w)                  # omurga
    for y_ in (9, 11):                      # kaburgalar
        c.rect(4, y_, 11, y_, W)
    c.sym_rect(2, 9, 3, 9, W)               # kollar
    c.sym_set(2, 10, W)
    c.sym_rect(5, 14, 5, 15, W)             # bacaklar
    c.outline()
    return c


def make_cadi():
    c = Canvas()
    c.poly([(7, 0), (3, 6), (12, 6)], INK)  # sivri şapka
    c.rect(1, 6, 14, 6, INK)                # şapka kenarı
    c.rect(7, 2, 8, 2, P)                   # şapka bandı
    c.ellipse(7.5, 8.5, 2.6, 2.2, L)        # yeşil yüz
    c.sym_set(6, 8, K)
    c.set(8, 10, l)                         # sivri burun
    c.rect(4, 11, 11, 15, p)                # pelerin
    c.rect(12, 9, 12, 15, N); c.set(12, 8, y)   # süpürge
    c.outline()
    return c


def make_korsan():
    c = Canvas()
    c.rect(3, 2, 12, 4, INK)                # şapka
    c.set(7, 3, W); c.set(8, 3, W)          # kurukafa
    c.ellipse(7.5, 7, 3.0, 2.6, S)          # yüz
    c.rect(5, 6, 7, 6, K); c.set(9, 6, K)   # göz bandı ve göz
    c.rect(5, 9, 10, 10, n)                 # sakal
    c.rect(4, 11, 11, 15, R)                # gömlek
    c.rect(4, 13, 11, 13, K)                # kemer
    c.rect(13, 9, 13, 14, w)                # kılıç
    c.outline()
    return c


def make_kartal():
    c = Canvas()
    c.poly([(0, 6), (6, 8), (6, 12), (2, 11)], N)    # kanatlar
    c.poly([(15, 6), (9, 8), (9, 12), (13, 11)], N)
    c.ellipse(7.5, 9.5, 2.8, 3.4, n)        # gövde
    c.ellipse(7.5, 5, 2.4, 2.2, W)          # beyaz kafa
    c.sym_set(6, 4, K)
    c.poly([(7, 6), (9, 6), (8, 8)], Y)     # gaga
    c.sym_set(6, 13, y)                     # pençeler
    c.outline()
    return c


def make_akrep():
    c = Canvas()
    c.ellipse(7.5, 11, 3.4, 2.4, O)         # gövde
    for xx, yy in ((7, 7), (8, 5), (9, 3), (8, 2), (7, 2)):   # kuyruk
        c.set(xx, yy, O); c.set(xx, yy + 1, O)
    c.set(6, 2, R)                          # iğne
    c.sym_rect(1, 8, 3, 9, O)               # kıskaçlar
    c.sym_set(1, 7, O); c.sym_set(3, 7, O)
    for x in (4, 6, 9, 11):                 # bacaklar
        c.set(x, 13, y); c.set(x, 14, y)
    c.sym_set(6, 10, K)
    c.outline()
    return c


def make_hamamboecegi():
    c = Canvas()
    c.ellipse(7.5, 9.5, 3.2, 4.6, n)        # kabuk
    c.rect(7, 5, 8, 14, K)                  # kabuk çizgisi
    c.ellipse(7.5, 4.5, 2.0, 1.6, N)        # baş
    c.set(6, 3, K); c.set(5, 1, K); c.set(9, 3, K); c.set(10, 1, K)   # antenler
    for y_ in (7, 10, 13):                  # bacaklar
        c.sym_set(3, y_, K); c.sym_set(2, y_ + 1, K)
    c.set(6, 7, N)
    c.outline()
    return c


def make_piranha():
    c = Canvas()
    c.ellipse(7.5, 8, 5.4, 4.4, g)          # gövde
    c.ellipse(7.5, 10, 4.4, 2.4, R)         # kırmızı karın
    c.poly([(13, 8), (16, 4), (16, 12)], g) # kuyruk
    c.rect(2, 9, 7, 9, K)                   # ağız
    for x in (2, 4, 6):
        c.set(x, 8, W); c.set(x + 1, 10, W) # dişler
    c.set(4, 6, Y); c.set(4, 6, K)          # göz
    c.set(5, 6, Y)
    c.outline()
    return c


def make_kraken():
    c = Canvas()
    c.ellipse(7.5, 5, 4.4, 4.2, P)          # baş
    c.sym_rect(5, 4, 6, 5, Y); c.sym_set(6, 5, K)   # gözler
    for x0 in (2, 5, 9, 12):                # kollar
        for t in range(6):
            xx = x0 + (1 if t % 2 else 0) * (1 if x0 < 8 else -1)
            c.set(xx, 9 + t, P); c.set(xx + 1, 9 + t, p)
    c.sym_set(3, 14, PINK); c.sym_set(6, 13, PINK)  # vantuzlar
    c.outline()
    return c


# ---------------- Kelime paketi 4: yaratıklar (önden; voxel modelleri otomatik) ----------------
def make_tavuk():
    c = Canvas()
    c.ellipse(7.5, 10, 4.0, 3.6, W)          # gövde
    c.ellipse(8.5, 5.5, 2.4, 2.4, W)         # kafa
    c.rect(8, 2, 9, 3, R); c.set(10, 3, R)   # ibik
    c.poly([(11, 5), (13, 6), (11, 7)], O)   # gaga
    c.set(10, 8, R)                          # sakal
    c.set(9, 5, K)
    c.rect(2, 8, 4, 11, w)                   # kuyruk
    c.sym_set(6, 14, y); c.sym_set(6, 15, y) # ayaklar
    c.outline()
    return c


def make_at():
    c = Canvas()
    c.ellipse(7.5, 10, 4.4, 3.4, n)          # gövde
    c.rect(9, 3, 12, 9, n)                   # boyun
    c.ellipse(11.5, 4, 2.6, 2.0, n)          # kafa
    c.rect(8, 2, 9, 8, INK)                  # yele
    c.set(12, 3, K); c.set(14, 5, K)         # göz, burun
    c.rect(3, 12, 3, 15, n); c.rect(5, 12, 5, 15, n); c.rect(10, 12, 10, 15, n); c.rect(12, 12, 12, 15, n)
    c.rect(1, 8, 2, 12, INK)                 # kuyruk
    c.outline()
    return c


def make_inek():
    c = Canvas()
    c.ellipse(7.5, 10, 5.4, 3.8, W)          # gövde
    c.ellipse(5, 9, 1.8, 1.4, INK); c.ellipse(10, 11, 1.6, 1.2, INK)   # benekler
    c.ellipse(7.5, 4.8, 3.0, 2.6, W)         # kafa
    c.ellipse(7.5, 6.5, 2.2, 1.4, PINK)      # burun
    c.sym_set(6, 7, K)
    c.sym_set(5, 4, K)
    c.sym_set(3, 2, w); c.sym_set(4, 2, w)   # boynuzlar
    c.sym_rect(4, 14, 4, 15, g); c.sym_rect(6, 14, 6, 15, g)
    c.outline()
    return c


def make_domuz():
    c = Canvas()
    c.ellipse(7.5, 9.5, 5.4, 4.4, PINK)      # gövde
    c.ellipse(7.5, 9, 2.0, 1.4, s)           # burun
    c.sym_set(6, 9, n)
    c.sym_set(5, 6, K)                       # gözler
    c.poly([(3, 5), (3, 2), (6, 4)], PINK); c.poly([(12, 5), (12, 2), (9, 4)], PINK)   # kulaklar
    c.sym_rect(4, 13, 5, 15, s)
    c.outline()
    return c


def make_keci():
    c = Canvas()
    c.ellipse(7.5, 10.5, 4.2, 3.2, w)        # gövde
    c.ellipse(7.5, 5.5, 2.6, 2.6, w)         # kafa
    c.poly([(5, 4), (2, 0), (4, 0), (6, 3)], g); c.poly([(10, 4), (13, 0), (11, 0), (9, 3)], g)   # boynuzlar
    c.sym_set(6, 5, K)
    c.rect(7, 8, 8, 9, W)                    # sakal
    c.sym_rect(4, 13, 4, 15, g); c.sym_rect(6, 13, 6, 15, g)
    c.outline()
    return c


def make_tavsan():
    c = Canvas()
    c.ellipse(7.5, 11, 3.8, 3.2, W)          # gövde
    c.ellipse(7.5, 7, 3.0, 2.6, W)           # kafa
    c.sym_rect(5, 0, 6, 5, W); c.sym_set(6, 2, PINK); c.sym_set(6, 3, PINK)   # uzun kulaklar
    c.sym_set(6, 7, R)                       # kırmızı gözler
    c.set(7, 8, PINK); c.set(8, 8, PINK)
    c.set(7, 9, W); c.set(8, 9, W)           # dişler
    c.sym_rect(5, 14, 6, 15, w)
    c.outline()
    return c


def make_kurbaga():
    c = Canvas()
    c.ellipse(7.5, 10, 5.6, 3.8, L)          # gövde
    c.ellipse(4.5, 5, 1.8, 1.8, L); c.ellipse(10.5, 5, 1.8, 1.8, L)   # göz tümsekleri
    c.set(4, 5, K); c.set(10, 5, K); c.set(5, 4, W); c.set(11, 4, W)
    c.rect(4, 10, 11, 10, l)                 # ağız
    c.rect(6, 11, 9, 11, PINK)               # dil
    c.sym_rect(1, 12, 3, 14, l)              # ayaklar
    c.outline()
    return c


def make_kertenkele():
    c = Canvas()
    c.rect(3, 8, 12, 10, L)                  # gövde
    c.ellipse(13, 7, 2.2, 1.6, L)            # kafa
    c.set(13, 6, K); c.set(15, 7, R)         # göz, dil
    for x in range(1, 4):                    # kuyruk
        c.set(x, 11 - (x // 2), l)
    c.set(0, 12, l)
    for x in (4, 10):                        # bacaklar
        c.set(x, 11, l); c.set(x - 1, 12, l); c.set(x, 7, l); c.set(x - 1, 6, l)
    for x in (5, 7, 9):                      # sırt benekleri
        c.set(x, 8, Y)
    c.outline()
    return c


def make_kopekbaligi():
    c = Canvas()
    c.ellipse(7.5, 9, 6.4, 3.6, g)           # gövde
    c.ellipse(7.5, 10.5, 5.4, 1.8, w)        # açık karın
    c.poly([(6, 6), (8, 1), (10, 6)], g)     # sırt yüzgeci
    c.poly([(13, 9), (16, 5), (16, 13)], g)  # kuyruk
    c.rect(1, 10, 6, 10, K)                  # ağız
    for x in (1, 3, 5):
        c.set(x, 9, W); c.set(x + 1, 11, W)  # dişler
    c.set(3, 7, K)
    c.outline()
    return c


def make_karga():
    c = Canvas()
    c.poly([(0, 7), (6, 8), (6, 12), (1, 11)], INK)   # kanatlar
    c.poly([(15, 7), (9, 8), (9, 12), (14, 11)], INK)
    c.ellipse(7.5, 9.5, 2.6, 3.4, INK)       # gövde
    c.ellipse(7.5, 5, 2.2, 2.0, INK)         # kafa
    c.sym_set(6, 4, W)                       # gözler
    c.poly([(7, 6), (9, 6), (8, 8)], G)      # gaga
    c.outline(K)
    return c


def make_guvercin():
    c = Canvas()
    c.ellipse(7.5, 9.5, 4.0, 3.4, g)         # gövde
    c.ellipse(7.5, 5, 2.4, 2.2, G)           # kafa
    c.rect(5, 7, 10, 7, l); c.rect(6, 8, 9, 8, P)    # yanardöner boyun
    c.sym_set(6, 4, O)
    c.set(7, 6, PINK); c.set(8, 6, PINK)     # gaga
    c.sym_rect(1, 8, 3, 11, w)               # kanatlar
    c.sym_set(6, 13, PINK)
    c.outline()
    return c


def make_palyaco():
    c = Canvas()
    c.ellipse(4, 4, 2.2, 2.2, R); c.ellipse(11, 4, 2.2, 2.2, R)   # kırmızı saç
    c.ellipse(7.5, 6, 3.2, 3.0, W)           # beyaz yüz
    c.sym_set(6, 5, K)
    c.ellipse(7.5, 7, 1.0, 1.0, R)           # kırmızı burun
    c.rect(5, 8, 10, 8, R)                   # gülümseme
    c.rect(4, 10, 11, 15, Y)                 # kostüm
    c.set(6, 11, R); c.set(9, 12, B); c.set(6, 14, L)   # benekler
    c.sym_rect(3, 9, 4, 10, W)               # yaka
    c.outline()
    return c


def make_asker():
    c = Canvas()
    c.ellipse(7.5, 3.5, 3.4, 2.4, D)         # kask
    c.rect(4, 4, 11, 4, l)
    c.ellipse(7.5, 6.5, 2.6, 2.2, S)         # yüz
    c.sym_set(6, 6, K)
    c.rect(4, 9, 11, 13, D)                  # üniforma
    c.rect(6, 10, 7, 11, l); c.rect(9, 11, 10, 12, l)   # kamuflaj
    c.rect(12, 8, 13, 13, INK)               # tüfek
    c.sym_rect(5, 14, 6, 15, INK)            # botlar
    c.outline()
    return c


def make_polis():
    c = Canvas()
    c.rect(4, 1, 11, 3, b)                   # şapka
    c.set(7, 2, Y); c.set(8, 2, Y)           # rozet
    c.rect(3, 4, 12, 4, INK)                 # siperlik
    c.ellipse(7.5, 6.5, 2.6, 2.0, S)
    c.sym_set(6, 6, K)
    c.rect(4, 9, 11, 13, b)                  # üniforma
    c.set(9, 10, Y)                          # rozet
    c.rect(4, 12, 11, 12, INK)               # kemer
    c.sym_rect(5, 14, 6, 15, INK)
    c.outline()
    return c


def make_uzayli():
    c = Canvas()
    c.ellipse(7.5, 5.5, 4.4, 3.8, L)         # büyük kafa
    c.ellipse(5, 6, 1.6, 1.0, K); c.ellipse(10, 6, 1.6, 1.0, K)   # iri kara gözler
    c.set(5, 5, W); c.set(10, 5, W)
    c.rect(7, 8, 8, 8, l)
    c.rect(6, 10, 9, 13, L)                  # ince gövde
    c.sym_set(4, 11, L); c.sym_set(3, 12, L) # kollar
    c.sym_rect(6, 14, 6, 15, l)
    c.set(7, 0, Y); c.set(7, 1, L); c.set(8, 1, L)   # anten
    c.outline()
    return c


def make_goblin():
    c = Canvas()
    c.ellipse(7.5, 6.5, 3.4, 3.0, l)         # kafa
    c.poly([(4, 6), (0, 3), (4, 8)], l); c.poly([(11, 6), (15, 3), (11, 8)], l)   # sivri kulaklar
    c.sym_set(6, 6, Y); c.sym_set(6, 7, K)   # sarı gözler
    c.rect(6, 9, 9, 9, K); c.set(7, 9, W)    # sırıtış
    c.rect(5, 10, 10, 14, n)                 # paçavra
    c.rect(11, 9, 11, 14, g)                 # bıçak
    c.sym_rect(5, 15, 6, 15, l)
    c.outline()
    return c


def make_balina():
    c = Canvas()
    c.ellipse(7.5, 9, 7.0, 4.6, B)           # gövde
    c.ellipse(7.5, 11.5, 6.0, 2.0, C)        # açık karın
    for x in range(3, 13, 2):
        c.set(x, 11, w)                      # karın çizgileri
    c.set(4, 7, K); c.set(4, 6, W)           # göz
    c.rect(1, 10, 5, 10, b)                  # ağız
    c.rect(9, 1, 9, 3, C); c.set(8, 1, C); c.set(10, 1, C)   # su fışkırtma
    c.outline()
    return c


# ---------------- Kelime paketi 5: yaratıklar (önden; voxel modelleri otomatik) ----------------
def make_minotor():
    c = Canvas()
    c.ellipse(7.5, 10.5, 5.2, 4.2, n)        # kaslı gövde
    c.ellipse(7.5, 10, 2.6, 2.4, N)          # göğüs
    c.ellipse(7.5, 4.8, 3.2, 2.8, n)         # boğa kafası
    c.poly([(4, 4), (0, 0), (2, 3)], W); c.poly([(11, 4), (15, 0), (13, 3)], W)   # boynuzlar
    c.sym_set(6, 4, R)                       # kızgın gözler
    c.rect(6, 6, 9, 7, s); c.sym_set(6, 7, K)  # burun
    c.set(7, 8, Y); c.set(8, 8, Y)           # halka
    c.rect(12, 7, 13, 14, g); c.rect(11, 6, 14, 7, w)   # balta
    c.sym_rect(5, 14, 6, 15, n)
    c.outline()
    return c


def make_medusa():
    c = Canvas()
    for x0, y0 in ((3, 3), (5, 1), (8, 1), (10, 2), (12, 4), (2, 6), (13, 7)):   # yılan saçlar
        c.set(x0, y0, L); c.set(x0, y0 + 1, l); c.set(x0 + (1 if x0 < 8 else -1), y0, L)
    c.ellipse(7.5, 6.5, 3.0, 2.8, L)         # yeşil yüz
    c.sym_set(6, 6, Y); c.sym_set(6, 5, Y)   # parlayan gözler
    c.rect(7, 8, 8, 8, r)
    c.rect(4, 10, 11, 15, D)                 # elbise
    c.rect(5, 10, 10, 10, Y)                 # altın kolye
    c.outline()
    return c


def make_golem():
    c = Canvas()
    c.rect(3, 6, 12, 13, g)                  # taş gövde
    c.rect(5, 2, 10, 6, G)                   # kafa
    c.sym_set(6, 4, C)                       # parlayan gözler
    c.rect(1, 7, 3, 12, G); c.rect(12, 7, 14, 12, G)   # kollar
    c.set(5, 8, G); c.set(9, 10, G); c.set(7, 12, G); c.set(10, 7, w)   # çatlaklar
    c.rect(6, 9, 9, 10, C)                   # rün
    c.sym_rect(4, 14, 6, 15, G)
    c.outline()
    return c


def make_gargoyle():
    c = Canvas()
    c.poly([(0, 3), (5, 7), (4, 12), (1, 9)], G)     # taş kanatlar
    c.poly([(15, 3), (10, 7), (11, 12), (14, 9)], G)
    c.ellipse(7.5, 9.5, 2.8, 3.4, g)         # gövde
    c.ellipse(7.5, 5, 2.4, 2.2, g)           # kafa
    c.sym_set(6, 2, w); c.sym_set(5, 1, w)   # boynuzlar
    c.sym_set(6, 5, R)
    c.rect(6, 7, 9, 7, K); c.sym_set(6, 8, W)  # dişler
    c.sym_rect(5, 13, 6, 14, G)
    c.outline()
    return c


def make_fil():
    c = Canvas()
    c.ellipse(7.5, 9.5, 5.0, 4.2, g)         # gövde/kafa
    c.ellipse(2.5, 7, 2.4, 3.4, w); c.ellipse(12.5, 7, 2.4, 3.4, w)   # kulaklar
    c.rect(7, 9, 8, 14, g); c.set(7, 15, g)  # hortum
    c.sym_set(6, 7, K)
    c.set(5, 11, W); c.set(10, 11, W)        # dişler
    c.sym_rect(4, 13, 5, 15, G)
    c.outline()
    return c


def make_mamut():
    c = Canvas()
    c.ellipse(7.5, 9, 6.0, 5.0, n)           # tüylü gövde
    c.ellipse(7.5, 7, 3.4, 3.0, N)
    c.rect(7, 9, 8, 14, n)                   # hortum
    c.poly([(5, 10), (2, 13), (2, 15), (4, 12)], W)   # kıvrık dişler
    c.poly([(10, 10), (13, 13), (13, 15), (11, 12)], W)
    c.sym_set(6, 7, K)
    for x in (3, 5, 10, 12):                 # tüyler
        c.set(x, 4, N)
    c.outline()
    return c


def make_cakal():
    c = Canvas()
    c.ellipse(7.5, 10.5, 4.0, 3.2, y)        # gövde
    c.ellipse(7.5, 6, 3.0, 2.6, y)           # kafa
    c.poly([(5, 4), (4, 0), (7, 3)], y); c.poly([(10, 4), (11, 0), (8, 3)], y)   # dik kulaklar
    c.ellipse(7.5, 7.4, 1.4, 1.0, W)
    c.set(7, 7, K); c.set(8, 7, K)
    c.sym_set(6, 5, K)
    c.sym_rect(5, 13, 5, 15, N); c.sym_rect(6, 13, 6, 14, N)
    c.outline()
    return c


def make_sirtlan():
    c = Canvas()
    c.ellipse(7.5, 10, 4.4, 3.4, s)          # gövde
    for x in (4, 6, 9, 11):                  # benekler
        c.set(x, 10, n); c.set(x + 1, 11, n)
    c.ellipse(7.5, 5.5, 3.0, 2.6, s)
    c.sym_set(5, 3, n)                       # kulaklar
    c.sym_set(6, 5, K)
    c.rect(6, 7, 9, 7, K); c.set(7, 7, W); c.set(9, 7, W)   # sırıtış
    c.rect(7, 2, 8, 4, n)                    # yele
    c.sym_rect(5, 13, 5, 15, n)
    c.outline()
    return c


def make_penguen():
    c = Canvas()
    c.ellipse(7.5, 9, 4.4, 5.4, INK)         # siyah gövde
    c.ellipse(7.5, 10, 3.0, 4.0, W)          # beyaz karın
    c.ellipse(7.5, 5, 2.2, 1.6, W)           # yüz
    c.sym_set(6, 4, K)
    c.poly([(7, 6), (9, 6), (8, 8)], O)      # gaga
    c.sym_rect(1, 8, 2, 11, INK)             # kanatlar
    c.sym_rect(5, 14, 6, 15, O)              # ayaklar
    c.outline(K)
    return c


def make_sumuklu():
    c = Canvas()
    c.ellipse(9, 8, 3.6, 3.6, N)             # kabuk
    c.ellipse(9, 8, 1.6, 1.6, n)
    c.set(9, 8, y)
    c.rect(1, 11, 14, 13, s)                 # yapışkan gövde
    c.set(2, 9, s); c.set(1, 8, s); c.set(4, 9, s); c.set(4, 8, s)   # boynuzlar
    c.set(1, 7, K); c.set(4, 7, K)
    c.rect(0, 14, 15, 14, C)                 # sümük izi
    c.outline()
    return c


def make_solucan():
    c = Canvas()
    pts = [(2, 13), (3, 12), (4, 11), (5, 11), (6, 12), (7, 13), (8, 13), (9, 12), (10, 11), (10, 10), (10, 9), (10, 8), (10, 7)]
    for xx, yy in pts:
        c.set(xx, yy, PINK); c.set(xx, yy + 1, s)
    c.ellipse(10.5, 5.5, 1.6, 1.8, PINK)     # kafa
    c.set(10, 5, K); c.set(11, 5, K)
    c.set(5, 11, r); c.set(9, 13, r)         # halkalar
    c.outline()
    return c


def make_denizanasi():
    c = Canvas()
    c.ellipse(7.5, 5, 5.4, 3.8, PINK)        # şemsiye
    c.ellipse(7.5, 4, 3.4, 2.0, W)           # parlama
    c.recolor(PINK, P, 7, 8)
    for x in (3, 5, 7, 9, 11):               # dokunaçlar
        for yy in range(9, 15):
            if (yy + x) % 3: c.set(x, yy, PINK)
            else: c.set(x + 1, yy, P)
    c.sym_set(6, 5, K)
    c.outline()
    return c


def make_pterodaktil():
    c = Canvas()
    c.poly([(0, 6), (6, 7), (6, 10), (2, 9)], r)    # deri kanatlar
    c.poly([(15, 6), (9, 7), (9, 10), (13, 9)], r)
    c.ellipse(7.5, 9, 2.0, 2.6, O)           # gövde
    c.ellipse(7.5, 5, 2.0, 1.6, O)           # kafa
    c.poly([(9, 4), (14, 5), (9, 6)], y)     # uzun gaga
    c.poly([(6, 4), (3, 2), (6, 3)], r)      # tepelik
    c.set(7, 4, K)
    c.sym_set(6, 12, y)
    c.outline()
    return c


def make_hirsiz():
    c = Canvas()
    c.rect(4, 2, 11, 3, INK)                 # bere
    c.ellipse(7.5, 6, 3.0, 2.6, S)           # yüz
    c.rect(4, 5, 11, 6, INK); c.sym_set(6, 5, W); c.sym_set(6, 6, W)   # maske
    c.rect(4, 9, 11, 13, INK)                # kazak
    c.rect(4, 10, 11, 10, W); c.rect(4, 12, 11, 12, W)   # çizgili
    c.ellipse(12.5, 11.5, 2.0, 2.4, n)       # çuval
    c.set(12, 11, Y)                         # $
    c.sym_rect(5, 14, 6, 15, INK)
    c.outline()
    return c


def make_mermi():
    c = Canvas()
    c.ellipse(7.5, 7.5, 3.6, 3.6, O)        # parlayan mermi
    c.ellipse(7.5, 7.5, 2.2, 2.2, Y)
    c.set(7, 6, W); c.set(8, 6, W); c.set(7, 7, W)       # sıcak çekirdek
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


# ---------------- Arazi karoları (tam opak, t_ öneki: harita zemini) ----------------
import random as _random


def _fill(c, color):
    for yy in range(16):
        for xx in range(16):
            c.px[yy][xx] = color


def make_t_grass(variant):
    rnd = _random.Random(10 + variant)
    c = Canvas()
    base, dark, light = (52, 98, 62), (44, 86, 56), (66, 120, 74)
    _fill(c, base)
    for _ in range(14):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for _ in range(9):                      # ot tutamları (2 piksel yükseklik)
        x, y = rnd.randrange(15), rnd.randrange(1, 16)
        c.set(x, y, light)
        c.set(x, y - 1, light)
    return c


def make_t_path():
    rnd = _random.Random(31)
    c = Canvas()
    base, dark, light = (148, 112, 78), (126, 94, 64), (170, 134, 98)
    _fill(c, base)
    for _ in range(18):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for _ in range(8):
        c.set(rnd.randrange(16), rnd.randrange(16), light)
    for _ in range(3):                      # küçük taşlar
        x, y = rnd.randrange(1, 14), rnd.randrange(1, 14)
        c.rect(x, y, x + 1, y, (176, 168, 156))
        c.rect(x, y + 1, x + 1, y + 1, (120, 112, 104))
    return c


def make_t_wood():
    c = Canvas()
    plank, gap, light, nail = (176, 124, 72), (112, 74, 42), (196, 146, 90), (84, 56, 32)
    _fill(c, plank)
    for yy in range(16):
        if yy % 4 == 3:                     # tahta araları
            for xx in range(16):
                c.px[yy][xx] = gap
        elif yy % 4 == 0:                   # tahta üst parlaması
            for xx in range(16):
                c.px[yy][xx] = light
    for row in range(4):                    # dikey birleşim çizgileri (satır başına kaydırılmış)
        x = (3 + row * 5) % 16
        for yy in range(row * 4, row * 4 + 3):
            c.px[yy][x] = gap
        c.px[row * 4 + 1][(x + 8) % 16] = nail
        c.px[row * 4 + 1][(x + 11) % 16] = nail
    return c


def make_t_woodwall():
    c = Canvas()
    log, dark, light, end = (122, 78, 44), (78, 48, 26), (158, 106, 64), (96, 60, 34)
    _fill(c, log)
    for yy in range(16):
        if yy % 4 == 0:
            for xx in range(16):
                c.px[yy][xx] = light
        elif yy % 4 == 3:
            for xx in range(16):
                c.px[yy][xx] = dark
    for row in range(4):                    # kütük uçları (yuvarlak kesit)
        y = row * 4 + 1
        c.px[y][0] = end
        c.px[y][1] = dark
        c.px[y + 1][0] = end
        c.px[y][15] = end
        c.px[y][14] = dark
        c.px[y + 1][15] = end
    return c


def make_t_lava(frame):
    rnd = _random.Random(77)
    c = Canvas()
    base, hot, crust, bright = (214, 72, 28), (247, 140, 36), (112, 30, 22), (254, 214, 90)
    _fill(c, base)
    for _ in range(26):
        x, y = rnd.randrange(16), rnd.randrange(16)
        c.set(x, y, hot)
    for i in range(5):                      # kabuk lekeleri
        x, y = rnd.randrange(1, 14), rnd.randrange(1, 14)
        c.rect(x, y, x + 1, y + 1, crust)
    shift = frame * 2
    for i in range(6):                      # parlak damarlar: iki karede kayar
        x, y = (i * 5 + shift) % 16, (i * 7 + frame * 3) % 16
        c.set(x, y, bright)
        c.set((x + 1) % 16, y, bright)
    return c


def make_t_chasm(edge):
    rnd = _random.Random(55)
    c = Canvas()
    deep, mid, shadow = (14, 12, 26), (24, 20, 40), (6, 5, 14)
    _fill(c, deep)
    for yy in range(16):
        for xx in range(16):
            if rnd.random() < 0.18:
                c.px[yy][xx] = mid
    if edge:                                 # üstteki karo çimen/zemin: kaya dudağı ve gölge
        for xx in range(16):
            c.px[0][xx] = (104, 90, 80)
            c.px[1][xx] = (84, 72, 66)
            c.px[2][xx] = (46, 40, 44)
            c.px[3][xx] = shadow
    return c


def make_t_charred():
    rnd = _random.Random(91)
    c = Canvas()
    base, dark, ember = (50, 44, 46), (34, 30, 32), (122, 64, 44)
    _fill(c, base)
    for _ in range(30):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for yy in range(16):                    # yanmış tahta izleri
        if yy % 4 == 3:
            for xx in range(16):
                if rnd.random() < 0.7:
                    c.px[yy][xx] = dark
    for _ in range(5):
        c.set(rnd.randrange(16), rnd.randrange(16), ember)
    return c


def _shingles(c, main, light, dark):
    """Kiremit deseni: dört piksel yüksekliğinde sıralar, her sırada kaydırılmış dikey ayrımlar."""
    _fill(c, main)
    for yy in range(16):
        row = yy // 4
        if yy % 4 == 0:
            for xx in range(16):
                c.px[yy][xx] = light            # kiremit üst parlaması
        elif yy % 4 == 3:
            for xx in range(16):
                c.px[yy][xx] = dark             # kiremit alt gölgesi
        offset = 3 if row % 2 else 0
        for xx in range(16):
            if (xx + offset) % 6 == 5 and yy % 4 in (1, 2):
                c.px[yy][xx] = dark             # kiremit araları


def make_t_roof(variant):
    c = Canvas()
    if variant == "a":
        _shingles(c, (172, 74, 56), (206, 104, 74), (116, 46, 42))
    else:
        _shingles(c, (146, 60, 50), (178, 84, 62), (96, 38, 38))
    return c


def make_t_roof_ridge():
    c = Canvas()
    _shingles(c, (172, 74, 56), (206, 104, 74), (116, 46, 42))
    for xx in range(16):                    # çatı sırtı: ortada parlak şerit
        for yy in range(5, 11):
            c.px[yy][xx] = (222, 132, 92)
        c.px[5][xx] = (244, 168, 116)
        c.px[10][xx] = (110, 42, 40)
        c.px[11][xx] = (84, 32, 34)
    return c


def make_t_roof_eave():
    c = Canvas()
    _shingles(c, (146, 60, 50), (178, 84, 62), (96, 38, 38))
    for xx in range(16):                    # saçak: alt kenarda koyu çıkıntı ve gölge
        c.px[13][xx] = (84, 32, 34)
        c.px[14][xx] = (60, 24, 28)
        c.px[15][xx] = (38, 18, 22)
    return c


def make_t_fire(frame):
    """Karonun üstüne bindirilen alev (saydam zemin): iki karede dilleri oynar."""
    c = Canvas()
    red, orange, yellow = (228, 59, 68), (247, 118, 34), (254, 231, 97)
    tongues = [(3, 10), (8, 13), (12, 9)] if frame == 0 else [(4, 12), (7, 9), (12, 12)]
    for cx, height in tongues:
        for k in range(height):
            y = 15 - k
            half = max(0, int(2.2 * (1 - k / float(height))))
            for xx in range(cx - half, cx + half + 1):
                if 0 <= xx < 16 and 0 <= y < 16:
                    c.px[y][xx] = red if k < height * 0.45 else orange
            if k > 1 and k < height - 3:
                c.set(cx, y, orange)
        c.set(cx, 15 - height // 2, yellow)
        c.set(cx, 15 - height // 2 + 1, yellow)
    for xx in range(1, 15):                 # tabanda kor
        c.set(xx, 15, red)
        if xx % 2 == frame:
            c.set(xx, 14, orange)
    return c


# ================= Sprite stacking (katmanlı 3D piksel) =================
# Her nesne yukarıdan görünen yatay dilimlerdir (0 = en alt). Hepsi aynı tuval boyunda, ortak merkezli.
# Araçlar 0 derecede +x yönüne (sağa) bakar; oyunda gittiği yöne döndürülür.
STACK_OUT = os.path.join(ROOT, "Assets", "Resources", "Stacks")


def save_stack(slices, name):
    """Dilimleri yatay şerit olarak kaydeder: genişlik = dilim boyu x dilim sayısı."""
    os.makedirs(STACK_OUT, exist_ok=True)
    w_, h_ = slices[0].w, slices[0].h
    img = Image.new("RGBA", (w_ * len(slices), h_), (0, 0, 0, 0))
    for i, cv in enumerate(slices):
        for yy in range(h_):
            for xx in range(w_):
                c = cv.px[yy][xx]
                if c is not None:
                    img.putpixel((i * w_ + xx, yy), (c[0], c[1], c[2], 255))
    img.save(os.path.join(STACK_OUT, name + ".png"))


def make_araba_stack():
    """Yukarıdan araba (burnu sağda): gövde x 1-14, y 4-11; tekerler gövdenin iki yanından taşar.
    10 dilim: tekerler, şasi, gövde (yuvarlak köşe, kenar çizgisi, tamponlar), kaput + farlar, kabin camları, tavan."""
    out = []

    def body_shape(c, col, edge):
        c.rect(1, 4, 14, 11, col)
        for x, y_ in ((1, 4), (14, 4), (1, 11), (14, 11)):       # yuvarlak köşeler
            c.set(x, y_, None)
        c.rect(2, 4, 13, 4, edge); c.rect(2, 11, 13, 11, edge)   # yan kenar çizgileri
        c.rect(1, 5, 1, 10, edge); c.rect(14, 5, 14, 10, edge)

    def wheels(c):
        for x0 in (3, 10):
            c.rect(x0, 3, x0 + 2, 3, INK); c.rect(x0, 12, x0 + 2, 12, INK)

    c = Canvas(); wheels(c); c.rect(3, 4, 12, 11, K); out.append(c)                       # 0: teker altı + şasi gölgesi
    c = Canvas(); wheels(c); c.rect(2, 4, 13, 11, K); out.append(c)                       # 1: şasi
    c = Canvas(); wheels(c); body_shape(c, b, K); c.rect(15, 6, 15, 9, g); c.rect(0, 6, 0, 9, g); out.append(c)  # 2: alt gövde + tamponlar
    c = Canvas(); wheels(c); body_shape(c, B, b); c.rect(15, 6, 15, 9, w); c.rect(0, 6, 0, 9, g); out.append(c)  # 3: gövde
    c = Canvas(); body_shape(c, B, b); out.append(c)                                      # 4: gövde
    c = Canvas(); body_shape(c, B, b)                                                     # 5: kaput üstü, farlar, stoplar
    c.set(14, 5, Y); c.set(14, 10, Y); c.set(1, 5, R); c.set(1, 10, R)
    c.rect(12, 7, 13, 8, C); out.append(c)                                                # kaput parlaması
    for _ in range(2):                                                                    # 6-7: kabin camları
        c = Canvas(); c.rect(4, 5, 10, 10, C); c.rect(4, 5, 4, 10, b); c.rect(10, 5, 10, 10, INK)
        c.rect(5, 5, 9, 5, b); c.rect(5, 10, 9, 10, b); out.append(c)
    c = Canvas(); c.rect(4, 5, 9, 10, B); c.rect(4, 5, 9, 5, b); c.rect(4, 10, 9, 10, b); c.rect(5, 7, 8, 8, W); out.append(c)  # 8: tavan
    c = Canvas(); c.rect(5, 6, 8, 9, B); c.rect(6, 7, 7, 8, W); out.append(c)               # 9: tavan tepesi
    return out


def make_char_stack():
    """Yukarıdan karakter (beyaz: oyunda oyuncunun rengiyle boyanır), yüzü sağa bakar. 8 dilim:
    ayaklar (kenar çizgili, gölge gibi), yuvarlak gövde, öne bakan iri gözler, kubbe tepe.
    Kenar çizgisi yalnızca en alttadır: her dilimde olsa üst üste binince koyu şeritler oluşur."""
    out = []
    def blob(r, col=W):
        c = Canvas(); c.ellipse(7.5, 7.5, r, r, col); return c

    c = blob(5.0, w); c.ellipse(10.5, 5.0, 1.8, 1.4, w); c.ellipse(10.5, 10.0, 1.8, 1.4, w); c.outline(); out.append(c)  # 0: ayaklar + taban
    c = blob(5.4, w); out.append(c)                                                         # 1: gövde altı
    c = blob(5.8); out.append(c)                                                            # 2: gövde
    for _ in range(2):                                                                      # 3-4: iri gözler (önde)
        c = blob(5.8); c.rect(11, 4, 12, 6, W); c.rect(11, 9, 12, 11, W)
        c.rect(12, 5, 13, 6, K); c.rect(12, 9, 13, 10, K); out.append(c)
    c = blob(5.4); out.append(c)                                                            # 5: omuz
    c = blob(4.6); out.append(c)                                                            # 6: kubbe
    c = blob(3.2); c.rect(6, 6, 7, 7, W); out.append(c)                                     # 7: tepe
    return out



def make_tank_stack():
    """Yukarıdan tank (namlu sağda): paletler, gövde, dönen kule, uzun namlu. 9 dilim."""
    out = []
    def tracks(c):
        c.rect(1, 2, 14, 4, INK); c.rect(1, 11, 14, 13, INK)
        for x in range(1, 15, 2):
            c.set(x, 3, G); c.set(x, 12, G)
    c = Canvas(); tracks(c); out.append(c)
    c = Canvas(); tracks(c); c.rect(2, 4, 13, 11, D); out.append(c)
    c = Canvas(); tracks(c); c.rect(2, 4, 13, 11, l); c.rect(2, 4, 13, 4, D); c.rect(2, 11, 13, 11, D); out.append(c)
    c = Canvas(); c.rect(2, 4, 13, 11, l); c.rect(12, 6, 13, 9, D); out.append(c)
    c = Canvas(); c.rect(3, 5, 12, 10, L); c.set(4, 6, Y); out.append(c)
    for _ in range(2):                                   # kule ve namlu
        c = Canvas(); c.ellipse(6.5, 7.5, 3.2, 3.0, l); c.rect(9, 7, 15, 8, G); out.append(c)
    c = Canvas(); c.ellipse(6.5, 7.5, 2.6, 2.4, L); c.rect(9, 7, 15, 8, g); out.append(c)
    c = Canvas(); c.ellipse(6.5, 7.5, 1.4, 1.2, D); c.set(6, 7, W); out.append(c)
    return out


def make_kamyon_stack():
    """Yukarıdan kamyon (burnu sağda): uzun kasa, ön kabin. 10 dilim."""
    out = []
    def wheels(c):
        for x0 in (2, 5, 11):
            c.rect(x0, 2, x0 + 1, 2, INK); c.rect(x0, 13, x0 + 1, 13, INK)
    c = Canvas(); wheels(c); c.rect(1, 3, 14, 12, K); out.append(c)
    c = Canvas(); wheels(c); c.rect(1, 3, 14, 12, g); out.append(c)
    for i in range(4):                                   # kasa (yüksek) + kabin
        c = Canvas(); c.rect(1, 3, 9, 12, w if i < 3 else W); c.rect(1, 3, 9, 3, g); c.rect(1, 12, 9, 12, g)
        c.rect(10, 4, 14, 11, R); c.rect(10, 4, 14, 4, r); c.rect(10, 11, 14, 11, r)
        if i == 1: c.set(14, 5, Y); c.set(14, 10, Y)
        out.append(c)
    for _ in range(2):
        c = Canvas(); c.rect(1, 3, 9, 12, W); c.rect(1, 3, 9, 3, g); c.rect(11, 5, 14, 10, C); c.rect(11, 5, 11, 10, r); out.append(c)
    c = Canvas(); c.rect(1, 3, 9, 12, W); c.rect(2, 6, 8, 6, g); c.rect(2, 9, 8, 9, g); c.rect(11, 5, 13, 10, R); out.append(c)
    c = Canvas(); c.rect(11, 6, 13, 9, r); out.append(c)
    return out


def make_bisiklet_stack():
    """Yukarıdan bisiklet (burnu sağda): ince tekerler, kadro, sele, gidon. 5 dilim."""
    out = []
    c = Canvas(); c.rect(1, 7, 4, 8, INK); c.rect(11, 7, 14, 8, INK); out.append(c)
    c = Canvas(); c.rect(1, 7, 4, 8, INK); c.rect(11, 7, 14, 8, INK); c.rect(4, 7, 11, 8, B); out.append(c)
    c = Canvas(); c.rect(5, 7, 10, 8, B); c.rect(7, 6, 8, 9, g); out.append(c)
    c = Canvas(); c.rect(4, 7, 6, 8, INK); out.append(c)                                  # sele
    c = Canvas(); c.rect(11, 4, 11, 11, g); c.set(11, 4, INK); c.set(11, 11, INK); out.append(c)   # gidon
    return out


def make_kaykay_stack():
    """Yukarıdan kaykay (burnu sağda): dört küçük teker ve renkli tahta. 3 dilim."""
    out = []
    c = Canvas(); c.rect(3, 5, 4, 5, W); c.rect(3, 10, 4, 10, W); c.rect(11, 5, 12, 5, W); c.rect(11, 10, 12, 10, W); out.append(c)
    c = Canvas(); c.rect(2, 6, 13, 9, INK); c.set(1, 7, INK); c.set(1, 8, INK); c.set(14, 7, INK); c.set(14, 8, INK); out.append(c)
    c = Canvas(); c.rect(2, 6, 13, 9, O); c.rect(5, 7, 10, 8, Y); out.append(c)
    return out



def make_otobus_stack():
    """Yukarıdan otobüs (burnu sağda): uzun sarı gövde, cam sıraları. 9 dilim."""
    out = []
    def wheels(c):
        for x0 in (2, 11):
            c.rect(x0, 2, x0 + 1, 2, INK); c.rect(x0, 13, x0 + 1, 13, INK)
    c = Canvas(); wheels(c); c.rect(0, 3, 15, 12, K); out.append(c)
    for i in range(3):
        c = Canvas(); wheels(c) if i == 0 else None; c.rect(0, 3, 15, 12, Y); c.rect(0, 3, 15, 3, y); c.rect(0, 12, 15, 12, y)
        if i == 1: c.set(15, 4, W); c.set(15, 11, W); c.set(0, 4, R); c.set(0, 11, R)
        out.append(c)
    for _ in range(3):                                   # cam sırası
        c = Canvas(); c.rect(0, 3, 15, 12, Y); c.rect(1, 3, 14, 3, C); c.rect(1, 12, 14, 12, C)
        for x in range(2, 15, 3): c.set(x, 3, y); c.set(x, 12, y)
        c.rect(15, 4, 15, 11, C); out.append(c)
    c = Canvas(); c.rect(0, 3, 15, 12, y); c.rect(1, 4, 14, 11, Y); out.append(c)        # tavan
    c = Canvas(); c.rect(3, 6, 6, 9, w); c.rect(9, 6, 12, 9, w); out.append(c)            # tavan pencereleri
    return out


def make_traktor_stack():
    """Yukarıdan traktör (burnu sağda): büyük arka tekerler, küçük ön tekerler, kabin. 7 dilim."""
    out = []
    def wheels(c):
        c.rect(1, 1, 5, 3, INK); c.rect(1, 12, 5, 14, INK); c.rect(11, 3, 13, 4, INK); c.rect(11, 11, 13, 12, INK)
    c = Canvas(); wheels(c); out.append(c)
    c = Canvas(); wheels(c); c.rect(2, 4, 14, 11, l); out.append(c)
    c = Canvas(); wheels(c); c.rect(2, 4, 14, 11, L); c.rect(9, 5, 14, 10, L); c.set(14, 6, Y); c.set(14, 9, Y); out.append(c)
    c = Canvas(); c.rect(2, 4, 8, 11, L); c.rect(9, 6, 14, 9, l); c.rect(12, 6, 12, 6, g); out.append(c)     # motor kaputu
    for _ in range(2):
        c = Canvas(); c.rect(2, 4, 7, 11, C); c.rect(2, 4, 2, 11, l); c.rect(7, 4, 7, 11, l); c.rect(12, 6, 12, 6, INK); out.append(c)   # kabin camı, egzoz
    c = Canvas(); c.rect(2, 4, 7, 11, l); c.rect(3, 5, 6, 10, L); out.append(c)
    return out


def make_buldozer_stack():
    """Yukarıdan buldozer (bıçağı sağda): paletler, sarı gövde, önde geniş bıçak. 7 dilim."""
    out = []
    def tracks(c):
        c.rect(1, 2, 11, 4, INK); c.rect(1, 11, 11, 13, INK)
        for x in range(1, 12, 2): c.set(x, 3, G); c.set(x, 12, G)
    c = Canvas(); tracks(c); c.rect(13, 1, 14, 14, g); out.append(c)
    c = Canvas(); tracks(c); c.rect(2, 4, 11, 11, y); c.rect(13, 1, 14, 14, w); c.rect(12, 7, 12, 8, G); out.append(c)
    c = Canvas(); tracks(c); c.rect(2, 4, 11, 11, Y); c.rect(13, 1, 14, 14, g); c.rect(12, 5, 12, 10, G); out.append(c)
    c = Canvas(); c.rect(2, 5, 11, 10, Y); c.rect(9, 7, 9, 8, INK); out.append(c)
    for _ in range(2):
        c = Canvas(); c.rect(2, 5, 6, 10, C); c.rect(2, 5, 2, 10, y); c.rect(6, 5, 6, 10, y); out.append(c)
    c = Canvas(); c.rect(2, 5, 6, 10, y); c.rect(3, 6, 5, 9, Y); out.append(c)
    return out


def make_go_kart_stack():
    """Yukarıdan go-kart (burnu sağda): alçak, dört teker, açık koltuk. 4 dilim."""
    out = []
    c = Canvas(); c.rect(3, 3, 5, 4, INK); c.rect(3, 11, 5, 12, INK); c.rect(11, 4, 12, 5, INK); c.rect(11, 10, 12, 11, INK); out.append(c)
    c = Canvas(); c.rect(3, 3, 5, 4, INK); c.rect(3, 11, 5, 12, INK); c.rect(11, 4, 12, 5, INK); c.rect(11, 10, 12, 11, INK); c.rect(2, 5, 13, 10, G); out.append(c)
    c = Canvas(); c.rect(2, 5, 13, 10, L); c.rect(12, 6, 14, 9, R); c.rect(5, 6, 8, 9, INK); out.append(c)   # gövde, burun, koltuk
    c = Canvas(); c.rect(9, 6, 9, 9, g); c.rect(1, 6, 2, 9, R); out.append(c)                                # direksiyon, spoiler
    return out



def make_ambulans_stack():
    """Yukarıdan ambulans (burnu sağda): beyaz kasa, kırmızı şerit ve haç, tepede siren. 9 dilim."""
    out = []
    def wheels(c):
        for x0 in (3, 10):
            c.rect(x0, 3, x0 + 2, 3, INK); c.rect(x0, 12, x0 + 2, 12, INK)
    c = Canvas(); wheels(c); c.rect(1, 4, 14, 11, K); out.append(c)
    for i in range(4):
        c = Canvas(); wheels(c) if i == 0 else None; c.rect(1, 4, 14, 11, W); c.rect(1, 4, 14, 4, w); c.rect(1, 11, 14, 11, w)
        if i == 2: c.rect(1, 7, 10, 8, R)
        if i == 1: c.set(14, 5, Y); c.set(14, 10, Y); c.set(1, 5, R); c.set(1, 10, R)
        out.append(c)
    c = Canvas(); c.rect(1, 4, 10, 11, W); c.rect(11, 5, 13, 10, C); out.append(c)
    c = Canvas(); c.rect(1, 4, 10, 11, W); c.rect(4, 6, 6, 9, R); c.rect(3, 7, 7, 8, R); out.append(c)   # tavan haçı
    c = Canvas(); c.rect(8, 6, 9, 9, B); c.set(8, 6, R); c.set(9, 9, R); out.append(c)                   # siren
    return out


def make_tren_stack():
    """Yukarıdan lokomotif (burnu sağda): uzun gövde, ön tampon, baca. 9 dilim."""
    out = []
    c = Canvas(); c.rect(0, 3, 15, 4, INK); c.rect(0, 11, 15, 12, INK); out.append(c)                   # raylar/tekerler
    for i in range(3):
        c = Canvas(); c.rect(0, 4, 15, 11, R if i else r); c.rect(0, 4, 15, 4, r); c.rect(0, 11, 15, 11, r)
        if i == 1: c.poly([(15, 4), (16, 7), (15, 11)], g); c.set(15, 7, Y); c.set(15, 8, Y)
        out.append(c)
    for _ in range(2):
        c = Canvas(); c.rect(0, 4, 9, 11, INK); c.rect(1, 5, 8, 10, G); c.rect(10, 5, 15, 10, R); out.append(c)   # kabin ve kazan
    c = Canvas(); c.rect(0, 4, 9, 11, INK); c.rect(10, 6, 15, 9, r); c.rect(12, 7, 13, 8, INK); out.append(c)  # baca
    c = Canvas(); c.rect(1, 5, 8, 10, r); c.rect(12, 7, 13, 8, G); out.append(c)
    c = Canvas(); c.rect(12, 7, 13, 8, g); out.append(c)
    return out


def make_limuzin_stack():
    """Yukarıdan limuzin (burnu sağda): uzun siyah gövde, koyu camlar. 7 dilim."""
    out = []
    def wheels(c):
        for x0 in (1, 12):
            c.rect(x0, 4, x0 + 2, 4, INK); c.rect(x0, 11, x0 + 2, 11, INK)
    c = Canvas(); wheels(c); c.rect(0, 5, 15, 10, K); out.append(c)
    for i in range(3):
        c = Canvas(); wheels(c) if i == 0 else None; c.rect(0, 5, 15, 10, INK); c.rect(0, 5, 15, 5, G); c.rect(0, 10, 15, 10, G)
        if i == 1: c.set(15, 6, W); c.set(15, 9, W); c.set(0, 6, R); c.set(0, 9, R)
        out.append(c)
    c = Canvas(); c.rect(2, 5, 12, 10, G); c.rect(3, 5, 11, 5, C); c.rect(3, 10, 11, 10, C); out.append(c)
    c = Canvas(); c.rect(3, 6, 11, 9, INK); c.rect(5, 7, 9, 8, g); out.append(c)
    c = Canvas(); c.rect(6, 7, 8, 8, w); out.append(c)
    return out


def make_at_arabasi_stack():
    """Yukarıdan at arabası (önde at): at, oklar, tahta kasa, iki büyük teker. 6 dilim."""
    out = []
    c = Canvas(); c.rect(2, 2, 5, 3, n); c.rect(2, 12, 5, 13, n); c.rect(12, 6, 14, 9, n); out.append(c)   # tekerler, at bacakları
    c = Canvas(); c.rect(2, 2, 5, 3, N); c.rect(2, 12, 5, 13, N); c.rect(1, 4, 8, 11, n); c.rect(11, 6, 15, 9, n); c.rect(8, 6, 11, 6, N); c.rect(8, 9, 11, 9, N); out.append(c)
    c = Canvas(); c.rect(1, 4, 8, 11, N); c.rect(1, 4, 8, 4, n); c.rect(1, 11, 8, 11, n); c.rect(11, 6, 15, 9, n); out.append(c)
    c = Canvas(); c.rect(1, 4, 1, 11, n); c.rect(8, 4, 8, 11, n); c.rect(1, 4, 8, 4, n); c.rect(1, 11, 8, 11, n); c.rect(12, 6, 15, 9, n); c.rect(11, 7, 11, 8, INK); out.append(c)
    c = Canvas(); c.rect(13, 6, 15, 9, n); c.rect(12, 7, 13, 8, INK); out.append(c)    # atın sırtı, yele
    c = Canvas(); c.rect(14, 7, 15, 8, n); c.set(15, 7, K); out.append(c)              # atın başı
    return out


def voxelize(cv, max_half_depth=5):
    """Önden çizilmiş bir resmi katmanlı (voxel) modele çevirir: resmin her satırı bir kat olur (en alt satır = dilim 0).
    Satırdaki her piksel, yukarıdan bakınca yanal eksende (dilimin y'si) yerini korur; derinlik ekseninde (dilimin x'i)
    gövdenin kenarına uzaklığa göre kalınlaşır (kenarlar ince, orta kalın: yuvarlak gövde). Ön yüz (+x, yani gittiği yön)
    resmin asıl rengini taşır, yanlar ve arka biraz koyudur. Böylece yaratık dönebilir ve kameraya dönünce yüzü görünür."""
    def shade(c, f):
        return (int(c[0] * f), int(c[1] * f), int(c[2] * f))

    slices = []
    for row in range(cv.h - 1, -1, -1):
        sl = Canvas(cv.w, cv.h)
        filled = [x for x in range(cv.w) if cv.px[row][x] is not None]
        if filled:
            for ix in filled:
                # Bu pikselin satırdaki dolu bölümün kenarına uzaklığı -> yarı derinlik.
                left = ix
                while left - 1 >= 0 and cv.px[row][left - 1] is not None:
                    left -= 1
                right = ix
                while right + 1 < cv.w and cv.px[row][right + 1] is not None:
                    right += 1
                edge = min(ix - left, right - ix)
                half = max(1 if right > left else 0, min(max_half_depth, int(edge * 1.5 + 0.5)))
                c = cv.px[row][ix]
                y_ = cv.w - 1 - ix              # yanal eksen (kameraya dönünce resim aynalanmasın)
                for d in range(-half, half + 1):
                    x_ = 8 + d                  # derinlik ekseni (ortada)
                    if d == half:
                        sl.set(x_, y_, c)       # ön yüz: asıl renk
                    else:
                        sl.set(x_, y_, shade(c, 0.78 if d > -half else 0.62))
        slices.append(sl)
    # Katman sayısı hep resmin yüksekliği: animasyon kareleri aynı sayıda dilimle değiştirilebilsin.
    return slices


def make_motor_stack():
    """Yukarıdan motor (burnu sağda): uzun ve ince (x 1-14, y 6-9). 6 dilim."""
    out = []
    c = Canvas(); c.rect(1, 7, 4, 8, INK); c.rect(11, 7, 14, 8, INK); out.append(c)                          # 0: tekerler
    c = Canvas(); c.rect(1, 7, 4, 8, INK); c.rect(11, 7, 14, 8, INK); c.rect(4, 7, 11, 8, G); out.append(c)  # 1: şasi
    c = Canvas(); c.rect(2, 7, 3, 8, INK); c.rect(12, 7, 13, 8, INK); c.rect(4, 6, 11, 9, r); c.rect(2, 9, 6, 9, w); out.append(c)  # 2: alt gövde, egzoz
    c = Canvas(); c.rect(4, 6, 11, 9, R); c.rect(4, 6, 11, 6, r); c.rect(4, 9, 11, 9, r); c.set(14, 7, Y); c.set(14, 8, Y); c.rect(12, 7, 13, 8, g); out.append(c)  # 3: gövde, far
    c = Canvas(); c.rect(4, 7, 7, 8, INK); c.rect(8, 6, 10, 9, R); c.rect(9, 7, 9, 8, W); out.append(c)      # 4: sele, depo parlaması
    c = Canvas(); c.rect(11, 3, 11, 12, g); c.set(11, 3, INK); c.set(11, 12, INK); c.set(12, 7, C); c.set(12, 8, C); out.append(c)  # 5: gidon, cam
    return out



def main():
    animated = {
        "char": make_char, "zombi": make_zombi, "ari": make_ari, "fare": make_fare, "kurt": make_kurt,
        "bulut": make_bulut, "kaplumbaga": make_kaplumbaga, "ejderha": make_ejderha, "unknown": make_unknown, "godzilla": make_godzilla,
        "kingkong": make_kingkong, "robot": make_robot, "yeti": make_yeti, "orumcek": make_orumcek,
        "tepegoz": make_tepegoz, "aslan": make_aslan, "yilan": make_yilan, "hayalet": make_hayalet,
        "yarasa": make_yarasa, "karinca": make_karinca, "boga": make_boga, "ninja": make_ninja,
        "kopek": make_kopek, "kedi": make_kedi, "timsah": make_timsah, "ayi": make_ayi, "iskelet": make_iskelet,
        "cadi": make_cadi, "korsan": make_korsan, "kartal": make_kartal, "akrep": make_akrep,
        "hamamboecegi": make_hamamboecegi, "piranha": make_piranha, "kraken": make_kraken,
        "tavuk": make_tavuk, "at": make_at, "inek": make_inek, "domuz": make_domuz, "keci": make_keci,
        "tavsan": make_tavsan, "kurbaga": make_kurbaga, "kertenkele": make_kertenkele, "kopekbaligi": make_kopekbaligi,
        "karga": make_karga, "guvercin": make_guvercin, "palyaco": make_palyaco, "asker": make_asker,
        "polis": make_polis, "uzayli": make_uzayli, "goblin": make_goblin, "balina": make_balina,
        "minotor": make_minotor, "medusa": make_medusa, "golem": make_golem, "gargoyle": make_gargoyle, "fil": make_fil, "mamut": make_mamut, "cakal": make_cakal, "sirtlan": make_sirtlan, "penguen": make_penguen, "sumuklu": make_sumuklu, "solucan": make_solucan, "denizanasi": make_denizanasi, "pterodaktil": make_pterodaktil, "hirsiz": make_hirsiz,
    }
    static = {
        "yumruk": make_yumruk, "muz_kabugu": make_muz, "miknatis": make_miknatis, "bomba": make_bomba, "mermi": make_mermi, "motor": make_motor, "araba": make_araba, "cali": make_cali, "kilic": make_kilic, "sopa": make_sopa,
        "ok": make_ok, "bumerang": make_bumerang, "lazer": make_lazer, "kartopu": make_kartopu, "tas": make_tas,
        "yildirim": make_yildirim, "mayin": make_mayin, "ag": make_ag,
        "shelter": make_shelter, "tower": make_tower, "cave": make_cave, "houses": make_houses,
        "floor": make_floor, "wall": make_wall, "border": make_border,
        "t_grass": lambda: make_t_grass(0), "t_grass_alt": lambda: make_t_grass(1), "t_path": make_t_path,
        "t_wood": make_t_wood, "t_woodwall": make_t_woodwall,
        "t_lava": lambda: make_t_lava(0), "t_lava_b": lambda: make_t_lava(1),
        "t_chasm": lambda: make_t_chasm(False), "t_chasm_edge": lambda: make_t_chasm(True),
        "t_charred": make_t_charred,
        "t_fire": lambda: make_t_fire(0), "t_fire_b": lambda: make_t_fire(1),
        "t_roof_a": lambda: make_t_roof("a"), "t_roof_b": lambda: make_t_roof("b"),
        "t_roof_ridge": make_t_roof_ridge, "t_roof_eave": make_t_roof_eave,
    }
    for name, fn in animated.items():
        save_animated(fn(), name)
    for name, fn in static.items():
        fn().save(name)
    stacks = {"araba": make_araba_stack, "motor": make_motor_stack, "char": make_char_stack,
              "tank": make_tank_stack, "kamyon": make_kamyon_stack, "bisiklet": make_bisiklet_stack, "kaykay": make_kaykay_stack,
              "otobus": make_otobus_stack, "traktor": make_traktor_stack, "buldozer": make_buldozer_stack, "go_kart": make_go_kart_stack,
              "ambulans": make_ambulans_stack, "tren": make_tren_stack, "limuzin": make_limuzin_stack, "at_arabasi": make_at_arabasi_stack}
    for name, fn in stacks.items():
        slices = fn()
        save_stack(slices, name)
        # Düz resmi olmayan araçlara katmanlardan birleştirilmiş düz bir resim (oyun aracı önce düz adıyla bulur).
        if name not in static and name not in animated:
            flat = Canvas(slices[0].w, slices[0].h)
            for sl in slices:
                for yy in range(sl.h):
                    for xx in range(sl.w):
                        if sl.px[yy][xx] is not None:
                            flat.px[yy][xx] = sl.px[yy][xx]
            flat.save(name)
    # Yaratıklar: önden çizimleri otomatik voxel modele çevrilir (her animasyon karesi ayrı).
    voxels = 0
    for name, fn in animated.items():
        if name == "char":
            continue                    # Karakterin elle çizilmiş katmanlı modeli var
        cv = fn()
        save_stack(voxelize(cv), name)
        save_stack(voxelize(cv.shifted(1)), name + "_b")
        voxels += 2
    print("Üretildi:", len(animated) * 2 + len(static), "PNG ->", OUT, "+", len(stacks) + voxels, "katmanlı nesne ->", STACK_OUT)


if __name__ == "__main__":
    main()
