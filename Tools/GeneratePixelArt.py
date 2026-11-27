"""
KelimeOyunu: kodla piksel sanat üretici.

Her sprite 16x16 pikseldir (Unity'de 16 PPU = 1 birim). Ortak palet ve otomatik siyah kenar çizgisi kullanılır.
Hareketli sprite'lar için ikinci bir kare (dosyaadi_b.png) de üretilir: aynı çizimin 1 piksel aşağı kaymış hâli.
Yaratıkların saldırı karesi (dosyaadi_atk.png): aynı çizime ATTACK_POSES'taki poz uygulanır (ağız açık, pençe kalkık).
Hazırlık karesi (dosyaadi_atk0.png): WINDUP_POSES'taki kurulma pozu (çömelme, kol/silah geride, ağız kapalı).
Vuruş izi efektleri: fx_<claw|bite|slash|punch|stomp|hit>_0..3.png (4 kare, düz sprite).

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
        _apply_attack_pose(self)            # Saldırı karesi çiziliyorsa poz kenar çizgisinden önce uygulanır
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
    c.poly([(11, 13), (14, 9), (15, 11), (13, 14)], G)   # kuyruk
    c.set(14, 9, w)
    c.ellipse(7.5, 11, 4.0, 3.2, g)         # gövde
    c.ellipse(7.5, 11.5, 1.8, 2.0, w)       # açık renk göğüs
    c.sym_rect(4, 13, 5, 15, g)             # bacaklar
    c.sym_set(4, 15, G)
    c.poly([(3, 0), (6, 3), (3, 5)], g)     # kulaklar
    c.poly([(12, 0), (9, 3), (12, 5)], g)
    c.sym_set(4, 2, PINK)
    c.ellipse(7.5, 5, 3.8, 3.2, g)          # kafa
    c.ellipse(7.5, 7, 2.0, 1.6, w)          # burun bölgesi
    c.sym_set(5, 4, Y)                      # sarı gözler
    c.sym_set(6, 4, K)
    c.set(7, 6, K); c.set(8, 6, K)          # burun
    c.sym_set(6, 8, W)                      # dişler
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
    c.poly([(11, 12), (14, 9), (15, 10), (12, 13)], y)   # kuyruk
    c.rect(13, 7, 14, 8, N)                 # kuyruk püskülü
    c.ellipse(7.5, 11, 4.2, 3.4, y)         # gövde
    c.ellipse(7.5, 11.5, 2.2, 2.0, Y)       # açık renk göğüs
    c.sym_rect(2, 9, 3, 12, y)              # ön pençeler
    c.sym_set(2, 12, W)
    c.sym_rect(4, 14, 5, 15, y)             # arka ayaklar
    c.ellipse(7.5, 5, 4.8, 4.4, N)          # yele
    c.ellipse(7.5, 5.5, 2.8, 2.6, y)        # yüz
    c.sym_set(4, 1, y)                      # kulaklar
    c.sym_set(6, 5, K)                      # gözler
    c.rect(7, 6, 8, 6, n)                   # burun
    c.rect(6, 8, 9, 8, n)                   # ağız
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
    c.ellipse(7.5, 11, 5.0, 3.6, n)         # iri gövde
    c.ellipse(7.5, 11, 2.6, 2.2, N)         # göğüs
    c.sym_rect(3, 13, 4, 15, n)             # bacaklar
    c.sym_rect(3, 15, 4, 15, K)             # toynaklar
    c.ellipse(7.5, 5, 3.6, 3.2, n)          # kafa
    c.sym_rect(2, 3, 3, 4, W)               # boynuzlar (yukarı kıvrık)
    c.sym_rect(1, 1, 2, 2, W)
    c.sym_set(1, 0, w)
    c.ellipse(7.5, 7, 2.2, 1.4, s)          # burun
    c.sym_set(6, 7, K)                      # burun delikleri
    c.sym_set(5, 4, R)                      # öfkeli gözler
    c.set(7, 9, Y); c.set(8, 9, Y)          # halka
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
    c.poly([(11, 12), (15, 9), (15, 11), (12, 14)], l)   # kuyruk
    c.ellipse(7.5, 11.5, 4.4, 3.0, l)       # gövde
    c.ellipse(7.5, 12, 2.4, 1.8, L)         # açık renk karın
    c.sym_rect(2, 13, 3, 15, l)             # bacaklar
    c.sym_set(2, 15, W)
    c.rect(3, 3, 12, 6, L)                  # açık ağız (üst çene)
    c.rect(3, 7, 12, 8, l)                  # alt çene
    c.rect(4, 5, 11, 6, R)                  # ağız içi
    for x in range(4, 12, 2):               # dişler
        c.set(x, 5, W); c.set(x + 1, 6, W)
    c.sym_set(4, 2, Y); c.sym_set(5, 2, L)  # gözler
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
    c.ellipse(7.5, 10.5, 6.0, 4.0, n)        # tüylü gövde
    c.sym_rect(2, 13, 4, 15, n)              # kalın bacaklar
    c.sym_rect(2, 15, 4, 15, N)
    c.ellipse(7.5, 5, 3.6, 3.4, N)           # kafa
    c.ellipse(7.5, 3, 2.2, 1.6, n)           # tüylü tepe
    c.rect(7, 7, 8, 13, N)                   # hortum
    c.set(8, 14, N)
    c.poly([(5, 7), (3, 10), (3, 12), (5, 9)], W)   # kıvrık dişler
    c.poly([(10, 7), (12, 10), (12, 12), (10, 9)], W)
    c.sym_set(6, 5, K)
    for x in (2, 4, 11, 13):                 # tüyler
        c.set(x, 9, N)
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


# ---------------- Kelime paketi 6: yaratıklar (önden; voxel modelleri otomatik) ----------------
def make_tilki():
    c = Canvas()
    c.ellipse(7.5, 10.5, 3.8, 3.0, O)        # gövde
    c.ellipse(7.5, 6.5, 3.2, 2.6, O)         # kafa
    c.poly([(5, 5), (4, 1), (7, 4)], O); c.poly([(10, 5), (11, 1), (8, 4)], O)   # kulaklar
    c.ellipse(7.5, 8, 1.8, 1.0, W)           # beyaz ağız
    c.set(7, 8, K); c.set(8, 8, K)
    c.sym_set(6, 6, K)
    c.ellipse(12.5, 12, 2.2, 1.4, O); c.set(14, 12, W)   # kabarık kuyruk
    c.sym_rect(5, 13, 5, 15, INK); c.sym_rect(6, 13, 6, 15, INK)
    c.outline()
    return c


def make_baykus():
    c = Canvas()
    c.ellipse(7.5, 9, 4.6, 5.4, N)           # gövde
    c.ellipse(7.5, 10.5, 2.8, 3.2, s)        # göğüs
    c.ellipse(5.5, 6, 1.8, 1.8, W); c.ellipse(9.5, 6, 1.8, 1.8, W)   # iri gözler
    c.set(5, 6, K); c.set(9, 6, K); c.set(6, 5, Y); c.set(10, 5, Y)
    c.set(7, 8, y); c.set(8, 8, y)           # gaga
    c.sym_set(4, 3, n); c.sym_set(4, 2, n)   # kulak tüyleri
    c.sym_set(6, 14, y)
    c.outline()
    return c


def make_sincap():
    c = Canvas()
    c.ellipse(11.5, 7, 3.2, 5.4, N)          # dev kuyruk
    c.ellipse(11.5, 7, 1.8, 3.8, s)
    c.ellipse(6, 10.5, 3.0, 3.4, N)          # gövde
    c.ellipse(5.5, 6, 2.4, 2.2, N)           # kafa
    c.set(4, 3, N); c.set(7, 3, N)           # kulaklar
    c.set(5, 5, K); c.set(4, 7, K)
    c.ellipse(5.5, 12, 1.4, 1.0, n); c.set(5, 11, y)   # elinde fındık
    c.outline()
    return c


def make_yengec():
    c = Canvas()
    c.ellipse(7.5, 9.5, 5.0, 3.4, R)         # kabuk
    c.ellipse(7.5, 9, 3.4, 1.8, r)
    c.sym_rect(5, 4, 5, 6, R); c.sym_set(5, 3, K)   # göz sapları
    c.sym_rect(0, 4, 2, 6, R)                # kıskaçlar
    c.set(1, 5, r); c.set(14, 5, r)          # kıskaç ağzı
    for x in (3, 5, 10, 12):                 # bacaklar
        c.set(x, 13, r); c.set(x + (-1 if x < 8 else 1), 14, r)
    c.outline()
    return c


def make_deve():
    c = Canvas()
    c.ellipse(7, 10, 4.6, 2.8, y)            # gövde
    c.ellipse(5, 7, 1.8, 1.6, y); c.ellipse(8.5, 7, 1.8, 1.6, y)   # hörgüçler
    c.rect(11, 3, 12, 9, y)                  # boyun
    c.ellipse(13, 3, 1.8, 1.2, y)            # kafa
    c.set(13, 2, K); c.set(14, 4, n)
    c.rect(4, 12, 4, 15, n); c.rect(6, 12, 6, 15, n); c.rect(9, 12, 9, 15, n); c.rect(11, 12, 11, 15, n)
    c.outline()
    return c


def make_kanguru():
    c = Canvas()
    c.ellipse(7.5, 10, 3.4, 4.0, N)          # gövde
    c.ellipse(7.5, 11, 1.8, 2.2, s)          # kese
    c.ellipse(7.5, 4.5, 2.2, 2.4, N)         # kafa
    c.sym_set(6, 1, N); c.sym_set(6, 2, N)   # kulaklar
    c.sym_set(6, 4, K)
    c.rect(2, 7, 4, 9, R); c.rect(11, 7, 13, 9, R)   # boks eldivenleri
    c.rect(4, 14, 7, 15, n); c.rect(8, 14, 11, 15, n)   # büyük ayaklar
    c.outline()
    return c


def make_panda():
    c = Canvas()
    c.ellipse(7.5, 10.5, 5.0, 4.2, W)        # gövde
    c.sym_rect(1, 8, 3, 12, INK)             # kollar
    c.ellipse(7.5, 5, 3.8, 3.4, W)           # kafa
    c.sym_set(4, 2, INK); c.sym_set(5, 2, INK)   # kulaklar
    c.ellipse(5.5, 5, 1.2, 1.0, INK); c.ellipse(9.5, 5, 1.2, 1.0, INK)   # göz lekeleri
    c.set(5, 5, W); c.set(10, 5, W)
    c.set(7, 7, K); c.set(8, 7, K)
    c.rect(12, 6, 12, 13, L)                 # bambu
    c.sym_rect(5, 14, 6, 15, INK)
    c.outline()
    return c


def make_kirpi():
    c = Canvas()
    for x in range(2, 14):                   # dikenler
        c.set(x, 6 + (x % 2), n); c.set(x, 5 + ((x + 1) % 3), INK)
    c.ellipse(7.5, 10, 5.4, 3.6, n)          # dikenli sırt
    c.ellipse(11.5, 11, 2.4, 2.0, s)         # yüz
    c.set(12, 10, K); c.set(14, 11, K)
    c.sym_rect(4, 13, 5, 14, s)
    c.outline()
    return c


def make_viking():
    c = Canvas()
    c.ellipse(7.5, 3.5, 3.2, 2.2, g)         # miğfer
    c.sym_set(3, 2, W); c.sym_set(2, 1, W)   # boynuzlar
    c.ellipse(7.5, 6.5, 2.6, 2.0, S)
    c.sym_set(6, 6, K)
    c.rect(5, 8, 10, 10, y)                  # sarı sakal
    c.rect(4, 11, 11, 14, n)                 # kürk
    c.rect(12, 7, 13, 14, N); c.rect(12, 6, 14, 8, w)   # balta
    c.sym_rect(5, 15, 6, 15, INK)
    c.outline()
    return c


def make_gladyator():
    c = Canvas()
    c.ellipse(7.5, 4.5, 3.0, 2.6, y)         # tolga
    c.rect(6, 0, 9, 2, R)                    # sorguç
    c.rect(6, 5, 9, 6, K); c.set(7, 5, S); c.set(8, 6, S)   # yüz siperi
    c.rect(4, 8, 11, 13, s)                  # gövde
    c.rect(4, 12, 11, 13, R)                 # etek
    c.ellipse(2.5, 10, 2.0, 2.6, y)          # kalkan
    c.rect(13, 6, 13, 13, w)                 # kılıç
    c.sym_rect(5, 14, 6, 15, n)
    c.outline()
    return c


def make_sumocu():
    c = Canvas()
    c.ellipse(7.5, 10, 6.4, 4.8, s)          # iri gövde
    c.ellipse(7.5, 4.5, 2.6, 2.4, s)         # kafa
    c.rect(6, 1, 9, 2, INK)                  # topuz saç
    c.sym_set(6, 4, K)
    c.rect(2, 11, 13, 12, P)                 # mavaşi kuşak
    c.rect(7, 12, 8, 14, P)
    c.sym_rect(3, 14, 5, 15, s)
    c.outline()
    return c


def make_boksor():
    c = Canvas()
    c.ellipse(7.5, 4.5, 2.6, 2.4, S)         # kafa
    c.rect(5, 2, 10, 3, R)                   # kask bandı
    c.sym_set(6, 4, K)
    c.rect(5, 7, 10, 11, S)                  # çıplak gövde
    c.ellipse(2.5, 7.5, 2.0, 2.0, R); c.ellipse(12.5, 7.5, 2.0, 2.0, R)   # eldivenler
    c.rect(5, 11, 10, 13, B)                 # şort
    c.sym_rect(5, 14, 6, 15, W)
    c.outline()
    return c


def make_futbolcu():
    c = Canvas()
    c.ellipse(7.5, 4, 2.6, 2.4, S)
    c.rect(5, 1, 10, 2, n)                   # saç
    c.sym_set(6, 4, K)
    c.rect(4, 7, 11, 11, R)                  # forma
    c.rect(7, 8, 8, 9, W)                    # numara
    c.rect(5, 12, 10, 13, W)                 # şort
    c.sym_rect(5, 14, 6, 15, INK)            # krampon
    c.ellipse(12.5, 13.5, 1.6, 1.6, W); c.set(12, 13, K); c.set(13, 14, K)   # top
    c.outline()
    return c


def make_asci():
    c = Canvas()
    c.ellipse(7.5, 2.5, 3.4, 2.4, W)         # aşçı şapkası
    c.rect(5, 3, 10, 4, W)
    c.ellipse(7.5, 6.5, 2.6, 2.2, S)
    c.sym_set(6, 6, K)
    c.rect(6, 8, 9, 8, n)                    # bıyık
    c.rect(4, 10, 11, 15, W)                 # önlük
    c.set(7, 11, R); c.set(7, 13, R)         # düğmeler
    c.rect(12, 7, 12, 13, g); c.rect(12, 6, 14, 7, w)   # kepçe
    c.outline()
    return c


# ---------------- Kelime paketi 6: düşen şey simgeleri ----------------
def make_ors():
    c = Canvas()
    c.rect(2, 4, 13, 7, G); c.poly([(13, 4), (16, 5), (13, 6)], G)   # örs başı ve boynuzu
    c.rect(5, 8, 10, 10, G); c.rect(3, 11, 12, 13, G)
    c.rect(2, 4, 13, 4, w)
    c.outline()
    return c


def make_piyano():
    c = Canvas()
    c.rect(1, 3, 14, 12, INK)                # gövde
    c.rect(2, 8, 13, 10, W)                  # tuşlar
    for x in range(3, 13, 2):
        c.set(x, 8, K)
    c.rect(2, 4, 13, 6, n)
    c.rect(2, 13, 3, 15, INK); c.rect(12, 13, 13, 15, INK)
    c.outline(K)
    return c


def make_saksi():
    c = Canvas()
    c.poly([(3, 8), (12, 8), (10, 15), (5, 15)], O)   # toprak saksı
    c.rect(3, 8, 12, 9, r)
    c.ellipse(7.5, 4.5, 2.6, 2.6, PINK); c.set(7, 4, Y); c.set(8, 5, Y)   # çiçek
    c.rect(7, 6, 8, 7, l); c.set(5, 6, L); c.set(10, 6, L)
    c.outline()
    return c


def make_top():
    c = Canvas()
    c.ellipse(7.5, 7.5, 6.0, 6.0, W)         # futbol topu
    c.ellipse(7.5, 7.5, 1.8, 1.8, INK)
    for x, y_ in ((3, 4), (12, 4), (3, 11), (12, 11), (7, 1), (7, 14)):
        c.set(x, y_, INK); c.set(x + 1, y_, INK)
    c.outline()
    return c


def make_bowling():
    c = Canvas()
    c.ellipse(7.5, 7.5, 6.0, 6.0, b)         # bowling topu
    c.ellipse(6, 5, 3.0, 2.0, B)             # parlama
    c.set(6, 6, INK); c.set(8, 5, INK); c.set(8, 7, INK)   # parmak delikleri
    c.outline()
    return c


def make_mermi():
    c = Canvas()
    c.ellipse(7.5, 7.5, 3.6, 3.6, O)        # parlayan mermi
    c.ellipse(7.5, 7.5, 2.2, 2.2, Y)
    c.set(7, 6, W); c.set(8, 6, W); c.set(7, 7, W)       # sıcak çekirdek
    c.outline()
    return c


# ---------------- Kelime paketi 10: yaratıklar ve kılıklar ----------------
def make_maymun():
    c = Canvas()
    c.rect(13, 11, 14, 13, n); c.rect(14, 8, 15, 10, n); c.set(13, 8, n)   # kıvrık kuyruk
    c.sym_rect(3, 8, 4, 13, n)               # uzun kollar
    c.sym_rect(3, 14, 4, 14, s)              # eller
    c.ellipse(7.5, 10.5, 3.2, 3.0, N)        # gövde
    c.ellipse(7.5, 11, 1.8, 1.8, s)          # göbek
    c.ellipse(3.5, 5, 1.6, 1.6, s); c.ellipse(11.5, 5, 1.6, 1.6, s)   # kulaklar
    c.ellipse(7.5, 5, 3.4, 3.2, N)           # kafa
    c.ellipse(7.5, 6, 2.6, 2.0, S)           # yüz
    c.sym_set(6, 5, K)                       # gözler
    c.set(7, 7, n); c.set(8, 7, n)           # ağız
    c.sym_rect(5, 14, 6, 15, n)              # bacaklar
    c.outline()
    return c


def make_papagan():
    c = Canvas()
    c.poly([(0, 6), (6, 8), (6, 12), (1, 11)], L)    # yeşil kanatlar
    c.poly([(15, 6), (9, 8), (9, 12), (14, 11)], L)
    c.poly([(0, 9), (6, 11), (6, 12), (1, 12)], B)   # mavi kanat uçları
    c.poly([(15, 9), (9, 11), (9, 12), (14, 12)], B)
    c.rect(7, 13, 8, 15, B)                  # kuyruk
    c.ellipse(7.5, 9.5, 2.6, 3.4, R)         # kırmızı gövde
    c.ellipse(7.5, 5, 2.6, 2.4, R)           # kafa
    c.sym_set(6, 4, W); c.sym_set(6, 5, K)   # gözler
    c.rect(7, 6, 8, 7, w); c.set(7, 8, K)    # kıvrık gaga
    c.sym_set(6, 13, INK)                    # pençeler
    c.outline()
    return c


def make_su_aygiri():
    c = Canvas()
    c.ellipse(7.5, 11.5, 6.0, 3.4, G)        # iri gövde
    c.sym_rect(2, 13, 4, 15, G)              # kalın bacaklar
    c.sym_rect(3, 0, 4, 1, p)                # küçük kulaklar
    c.ellipse(7.5, 3.5, 3.8, 2.4, g)         # kafa
    c.sym_set(5, 2, W); c.sym_set(5, 3, K)   # gözler
    c.rect(3, 5, 12, 9, g)                   # geniş ağız
    c.rect(4, 6, 11, 8, PINK)                # ağız içi
    c.rect(4, 6, 11, 6, P)
    c.sym_rect(5, 7, 5, 8, W)                # dişler
    c.sym_set(6, 4, K)                       # burun delikleri
    c.rect(3, 9, 12, 9, p)                   # çene gölgesi
    c.outline()
    return c


def make_peri():
    c = Canvas()
    c.ellipse(3.5, 6, 2.6, 3.2, C); c.ellipse(12, 6, 2.6, 3.2, C)       # parlak kanatlar
    c.ellipse(4, 10.5, 1.8, 1.8, C); c.ellipse(11, 10.5, 1.8, 1.8, C)
    c.sym_set(3, 5, W); c.sym_set(2, 6, W)   # kanat parlaması
    c.ellipse(7.5, 5, 2.2, 2.0, S)           # kafa
    c.rect(5, 2, 10, 3, Y)                   # sarı saç
    c.sym_set(6, 5, K)
    c.poly([(6, 7), (10, 7), (11, 12), (4, 12)], PINK)   # elbise
    c.rect(6, 8, 9, 8, P)
    c.sym_rect(6, 13, 6, 13, S)              # bacaklar
    c.set(12, 1, Y); c.set(1, 12, Y); c.set(14, 13, W)   # pırıltılar
    c.outline()
    return c


def make_kovboy():
    c = Canvas()
    c.rect(5, 0, 10, 3, N)                   # şapka
    c.rect(5, 3, 10, 3, n)                   # şapka bandı
    c.rect(2, 4, 13, 4, N); c.sym_set(2, 3, N)   # kıvrık kenar
    c.ellipse(7.5, 6.5, 2.6, 2.0, S)         # yüz
    c.sym_set(6, 6, K)
    c.rect(6, 8, 9, 8, n)                    # bıyık
    c.rect(4, 9, 11, 13, B)                  # gömlek
    c.rect(6, 9, 9, 9, R)                    # bandana
    c.sym_rect(4, 10, 5, 13, n)              # yelek
    c.rect(4, 13, 11, 13, K); c.set(7, 13, Y); c.set(8, 13, Y)   # kemer, toka
    c.rect(12, 11, 13, 12, g)                # tabanca
    c.sym_rect(5, 14, 6, 14, b)              # kot
    c.sym_rect(5, 15, 6, 15, n)              # çizmeler
    c.outline()
    return c


def make_karton_kutu():
    c = Canvas()
    c.poly([(2, 7), (4, 4), (15, 4), (13, 7)], s)    # üst yüz
    c.poly([(13, 7), (15, 4), (15, 13), (13, 15.5)], n)   # yan yüz
    c.rect(2, 7, 12, 15, N)                  # ön yüz
    c.poly([(6, 7), (8, 4), (10, 4), (8, 7)], y)     # bant (üst)
    c.rect(6, 7, 7, 9, y)                    # bant (ön)
    c.rect(4, 11, 6, 13, w)                  # etiket
    c.set(5, 12, INK)
    c.rect(2, 7, 12, 7, n)                   # kenar katı
    c.outline()
    return c


def make_cop_kutusu():
    c = Canvas()
    c.poly([(3, 6), (13, 6), (12, 16), (4, 16)], l)  # yeşil metal gövde
    c.rect(5, 7, 5, 14, L)                   # parlama
    c.rect(7, 7, 7, 14, D); c.rect(10, 7, 10, 14, D)   # oluklar
    c.rect(4, 15, 11, 15, D)                 # dip
    c.rect(2, 4, 13, 5, g)                   # kapak
    c.rect(2, 4, 13, 4, w)
    c.rect(6, 2, 9, 2, G); c.sym_set(6, 3, G)   # kulp
    c.outline()
    return c


def make_ejderha():
    c = Canvas()
    c.poly([(0, 3), (4, 8), (1, 11)], D)    # kanatlar
    c.poly([(16, 3), (12, 8), (15, 11)], D)
    c.poly([(11, 13), (15, 13), (14, 15), (11, 15)], L)   # kuyruk
    c.set(15, 12, O)
    c.ellipse(7.5, 11, 4.0, 3.4, L)         # gövde
    c.ellipse(7.5, 11.5, 2.0, 2.2, Y)       # sarı karın
    for yy in (10, 12):
        c.set(7, yy, y); c.set(8, yy, y)
    c.sym_rect(4, 14, 5, 15, l)             # ayaklar
    c.sym_set(4, 15, W)                     # pençe
    c.poly([(4, 0), (6, 3), (4, 3)], Y)     # boynuzlar
    c.poly([(11, 0), (9, 3), (11, 3)], Y)
    c.ellipse(7.5, 5, 3.4, 2.8, L)          # kafa
    c.ellipse(7.5, 7, 2.2, 1.2, l)          # burun
    c.sym_set(5, 4, O)                      # gözler
    c.sym_set(6, 4, K)
    c.sym_set(6, 7, K)                      # burun delikleri
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


# ---------------- Karlı dağ teması karoları ----------------
import math as _math


def _wset(c, x, y, col):
    """Kenardan taşanı karşı kenara sarar: karo döşenince desen kesilmez."""
    c.px[y % 16][x % 16] = col


def make_t_snow(variant):
    rnd = _random.Random(120 + variant)
    c = Canvas()
    base, shade, light, blue = (232, 238, 246), (208, 218, 234), (250, 252, 255), (188, 204, 226)
    _fill(c, base)
    for _ in range(16):
        c.set(rnd.randrange(16), rnd.randrange(16), shade)
    for _ in range(10):
        c.set(rnd.randrange(16), rnd.randrange(16), light)
    for _ in range(3 + variant * 2):        # rüzgâr birikintileri: üstü parlak, altı gölgeli kısa yaylar
        x, y = rnd.randrange(16), rnd.randrange(16)
        for k in range(4):
            _wset(c, x + k, y, light)
            _wset(c, x + k, y + 1, shade if k in (0, 3) else blue)
    if variant:                              # alt varyant: kardan çıkan birkaç küçük taş
        for _ in range(2):
            x, y = rnd.randrange(16), rnd.randrange(16)
            _wset(c, x, y, (120, 124, 136))
            _wset(c, x + 1, y, (150, 154, 166))
    return c


def make_t_snow_path():
    rnd = _random.Random(141)
    c = Canvas()
    base, dark, light, slush = (196, 206, 222), (170, 182, 202), (216, 224, 236), (150, 140, 128)
    _fill(c, base)
    for _ in range(22):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for _ in range(12):
        c.set(rnd.randrange(16), rnd.randrange(16), light)
    for x, y in ((2, 1), (9, 4), (4, 9), (11, 12)):   # ayak izleri (çift, 2x2)
        for dx, dy in ((0, 0), (3, 1)):
            _wset(c, x + dx, y + dy, dark)
            _wset(c, x + dx + 1, y + dy, dark)
            _wset(c, x + dx, y + dy + 1, (156, 168, 190))
            _wset(c, x + dx + 1, y + dy + 1, (156, 168, 190))
    for _ in range(5):
        c.set(rnd.randrange(16), rnd.randrange(16), slush)   # çamurlu kar lekeleri
    return c


def make_t_stone():
    """Kaldırım taşı: 4x4 hücreler, her sırada 2 kaydırılmış; taş başına ufak ton farkı."""
    rnd = _random.Random(150)
    c = Canvas()
    mortar = (72, 74, 86)
    tones = [((136, 140, 152), (164, 168, 180), (108, 112, 124)),
             ((126, 128, 140), (154, 156, 168), (100, 102, 114)),
             ((146, 146, 154), (172, 172, 180), (116, 116, 124))]
    _fill(c, mortar)
    for row in range(4):
        off = 2 if row % 2 else 0
        for col in range(4):
            main, light, dark = tones[rnd.randrange(3)]
            x0, y0 = col * 4 + off, row * 4
            for dy in range(3):
                for dx in range(3):
                    _wset(c, x0 + dx, y0 + dy, main)
            _wset(c, x0, y0, light); _wset(c, x0 + 1, y0, light); _wset(c, x0, y0 + 1, light)
            _wset(c, x0 + 2, y0 + 2, dark)
    return c


def make_t_stonewall():
    """Gri taş duvar: 4 piksel yüksek sıralar, 8 genişlik bloklar, her sırada 4 kaydırılmış."""
    rnd = _random.Random(160)
    c = Canvas()
    main, light, dark, mortar = (128, 132, 144), (162, 166, 178), (98, 100, 112), (66, 68, 80)
    _fill(c, main)
    for yy in range(16):
        if yy % 4 == 0:
            for xx in range(16):
                c.px[yy][xx] = light
        elif yy % 4 == 3:
            for xx in range(16):
                c.px[yy][xx] = mortar
    for row in range(4):
        off = 4 if row % 2 else 0
        for k in range(2):
            x = off + k * 8
            for yy in range(row * 4, row * 4 + 3):
                _wset(c, x, yy, mortar)
            _wset(c, x + 1, row * 4 + 2, dark)
            _wset(c, x + 7, row * 4 + 2, dark)
    for _ in range(6):                       # taş dokusu
        x, y = rnd.randrange(16), rnd.randrange(16)
        if y % 4 in (1, 2) and c.px[y][x] == main:
            c.px[y][x] = dark
    return c


def make_t_ice():
    rnd = _random.Random(170)
    c = Canvas()
    base, deep, shine, crack = (168, 212, 238), (148, 196, 228), (226, 244, 254), (120, 170, 210)
    _fill(c, base)
    for _ in range(20):
        c.set(rnd.randrange(16), rnd.randrange(16), deep)
    for x0, y0, ln in ((1, 3, 4), (9, 1, 3), (6, 10, 4), (13, 8, 3)):   # parlak çapraz çizgiler (2 piksel kalın)
        for k in range(ln):
            _wset(c, x0 + k, y0 - k, shine)
            _wset(c, x0 + k + 1, y0 - k, shine)
    x, y = 3, 13                              # ince çatlak
    for dx, dy in ((0, 0), (1, 0), (2, -1), (3, -1), (4, 0), (5, 1), (6, 1)):
        _wset(c, x + dx, y + dy, crack)
    return c


def make_t_water(frame):
    rnd = _random.Random(180)
    c = Canvas()
    base, mid, wave, foam = (30, 64, 106), (40, 82, 128), (86, 134, 180), (150, 190, 222)
    ice, ice_dark = (210, 232, 246), (160, 196, 224)
    _fill(c, base)
    for _ in range(26):
        c.set(rnd.randrange(16), rnd.randrange(16), mid)
    shift = frame * 2
    for i, (x, y) in enumerate(((1, 2), (9, 5), (4, 9), (12, 12), (7, 14))):   # dalga tepeleri: karede kayar
        x += shift if i % 2 == 0 else -shift
        _wset(c, x, y, wave); _wset(c, x + 1, y, wave); _wset(c, x + 2, y, wave)
        _wset(c, x + 1, y - 1, foam)
    for x, y in ((11, 1), (2, 12)):          # yüzen buz parçaları (karede 1 piksel salınır)
        x += frame
        _wset(c, x, y, ice); _wset(c, x + 1, y, ice); _wset(c, x, y + 1, ice); _wset(c, x + 1, y + 1, ice_dark)
        _wset(c, x + 2, y + 1, ice_dark)
        _wset(c, x, y + 2, base); _wset(c, x + 1, y + 2, base)
    _wset(c, 6 - frame, 6, ice)
    return c


def _mix(a, b, t):
    return (int(a[0] + (b[0] - a[0]) * t), int(a[1] + (b[1] - a[1]) * t), int(a[2] + (b[2] - a[2]) * t))


def _snow_shingles(c, roof, roof_dark, snow, snow_shade, seed=190, rows=(0, 16)):
    """Karla kaplı kiremit: zemin kar; kar altından birkaç düzensiz yassı lekede kiremit sırası görünür."""
    rnd = _random.Random(seed)
    roof_light = _mix(roof, (255, 255, 255), 0.18)
    _fill(c, snow)
    for _ in range(14):                      # kar dokusu: hafif gölge noktaları
        _wset(c, rnd.randrange(16), rnd.randrange(16), snow_shade)
    for _ in range(3):                       # kiremit lekeleri: 4-6 genişlik, alt satır kısa (düzensiz kenar)
        x, y, ww = rnd.randrange(16), rnd.randrange(rows[0] + 1, rows[1] - 2), rnd.randrange(4, 7)
        cut = rnd.randrange(1, 3)
        for k in range(ww):
            _wset(c, x + k, y - 1, snow_shade)                     # kar kenarının gölgesi
            _wset(c, x + k, y, roof if (x + k) % 4 else roof_dark)
            if cut <= k < ww - 1:
                _wset(c, x + k, y + 1, roof_dark)
        _wset(c, x + 1, y, roof_light)


def make_t_roof_snow(variant):
    c = Canvas()
    if variant == "a":
        _snow_shingles(c, (120, 56, 48), (78, 34, 34), (240, 244, 250), (204, 214, 232))
    else:
        _snow_shingles(c, (104, 48, 44), (66, 30, 32), (228, 234, 244), (194, 204, 224), seed=191)
    return c


def make_t_roof_snow_ridge():
    c = Canvas()
    _snow_shingles(c, (120, 56, 48), (78, 34, 34), (240, 244, 250), (204, 214, 232), seed=192, rows=(12, 16))
    for xx in range(16):                    # sırtta kalın kar yığını
        for yy in range(5, 11):
            c.px[yy][xx] = (250, 252, 255)
        c.px[5][xx] = (255, 255, 255)
        c.px[10][xx] = (196, 208, 228)
        c.px[11][xx] = (150, 164, 192)
    for xx in (2, 9, 13):
        c.px[7][xx] = (228, 236, 248)
    return c


def make_t_roof_snow_eave():
    c = Canvas()
    _snow_shingles(c, (104, 48, 44), (66, 30, 32), (228, 234, 244), (194, 204, 224), seed=193, rows=(0, 11))
    ice, ice_light = (176, 220, 246), (224, 244, 255)
    for xx in range(16):                    # saçak: kar kenarı, koyu çıkıntı, sarkan buzlar
        c.px[12][xx] = (240, 244, 250)
        c.px[13][xx] = (84, 36, 36)
        c.px[14][xx] = (60, 24, 28)
        c.px[15][xx] = (38, 18, 22)
    for xx, ln in ((1, 3), (4, 2), (6, 3), (9, 2), (12, 3), (14, 2)):
        for k in range(ln):
            c.px[13 + k][xx] = ice_light if k == 0 else ice
    return c


# ---------------- Çöl kasabası teması karoları ----------------
def make_t_sand(variant):
    rnd = _random.Random(200 + variant)
    c = Canvas()
    base, dark, light = (226, 190, 122), (204, 166, 100), (242, 212, 150)
    _fill(c, base)
    for _ in range(18):
        c.set(rnd.randrange(16), rnd.randrange(16), dark if rnd.random() < 0.6 else light)
    phase = variant * 5
    for y0 in ((3, 11) if variant == 0 else (6, 14)):   # rüzgâr dalgacıkları (16 pikselde tam dalga: döşenir)
        for xx in range(16):
            y = y0 + int(round(1.4 * _math.sin(2 * _math.pi * (xx + phase) / 16.0)))
            _wset(c, xx, y, dark)
            _wset(c, xx, y - 1, light)
    if variant:
        for _ in range(2):                   # alt varyant: ufak çakıl
            x, y = rnd.randrange(16), rnd.randrange(16)
            _wset(c, x, y, (160, 128, 92)); _wset(c, x + 1, y, (184, 152, 112))
    return c


def make_t_sand_path():
    rnd = _random.Random(220)
    c = Canvas()
    base, dark, light = (184, 140, 94), (160, 118, 78), (204, 162, 114)
    _fill(c, base)
    for _ in range(22):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for _ in range(10):
        c.set(rnd.randrange(16), rnd.randrange(16), light)
    for _ in range(3):                       # gömülü çakıllar
        x, y = rnd.randrange(16), rnd.randrange(16)
        _wset(c, x, y, (214, 196, 166)); _wset(c, x + 1, y, (214, 196, 166))
        _wset(c, x, y + 1, (136, 110, 84)); _wset(c, x + 1, y + 1, (136, 110, 84))
    for xx in range(16):                     # kuru çatlak izi
        if xx % 5 in (0, 1):
            _wset(c, xx, 7 + (xx // 5) % 2, dark)
    return c


def make_t_adobe():
    """Kerpiç ev zemini: 8x8 pişmiş toprak kareler, açık derz, karo başına ufak ton farkı."""
    rnd = _random.Random(230)
    c = Canvas()
    grout = (156, 108, 72)
    tones = [((200, 134, 88), (220, 158, 110), (176, 114, 76)),
             ((190, 124, 82), (210, 148, 102), (166, 106, 70)),
             ((206, 144, 96), (224, 166, 118), (180, 122, 82))]
    _fill(c, grout)
    for ty in range(2):
        for tx in range(2):
            main, light, dark = tones[(tx + ty * 2 + 1) % 3]
            x0, y0 = tx * 8, ty * 8
            c.rect(x0, y0, x0 + 6, y0 + 6, main)
            c.rect(x0, y0, x0 + 6, y0, light)
            c.rect(x0, y0, x0, y0 + 6, light)
            c.rect(x0 + 1, y0 + 6, x0 + 6, y0 + 6, dark)
            for _ in range(3):
                c.set(x0 + rnd.randrange(1, 6), y0 + rnd.randrange(1, 6), dark)
    return c


def make_t_adobewall():
    """Kerpiç duvar: sıva altından seçilen kerpiç tuğlalar, ufak çatlaklar ve saman çöpleri."""
    rnd = _random.Random(240)
    c = Canvas()
    main, light, dark, seam = (214, 162, 110), (232, 186, 136), (178, 126, 84), (190, 138, 92)
    crack, straw = (138, 92, 60), (240, 214, 136)
    _fill(c, main)
    for yy in range(16):
        if yy % 4 == 0:
            for xx in range(16):
                c.px[yy][xx] = light
        elif yy % 4 == 3:
            for xx in range(16):
                c.px[yy][xx] = seam
    for row in range(4):
        off = 4 if row % 2 else 0
        for k in range(2):
            x = off + k * 8
            for yy in range(row * 4 + 1, row * 4 + 3):
                _wset(c, x, yy, seam)
    for _ in range(8):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for x, y in ((3, 1), (10, 6), (6, 13), (13, 10)):   # saman çöpleri (2 piksel yatay)
        _wset(c, x, y, straw); _wset(c, x + 1, y, straw)
    for dx, dy in ((0, 0), (1, 1), (1, 2), (2, 3)):     # çatlak
        _wset(c, 11 + dx, 1 + dy, crack)
    for dx, dy in ((0, 0), (0, 1), (1, 2)):
        _wset(c, 2 + dx, 8 + dy, crack)
    return c


def make_t_quicksand(frame):
    """Koyu bataklık kumu: ortada dönen sarmal; kenarlara doğru söner, karo kenarları sade (döşenir)."""
    rnd = _random.Random(250)
    c = Canvas()
    base, dark, deep, light, bubble = (138, 106, 66), (112, 84, 52), (88, 64, 40), (158, 126, 84), (176, 146, 102)
    _fill(c, base)
    for _ in range(18):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for yy in range(16):
        for xx in range(16):
            dx, dy = xx - 7.5, yy - 7.5
            r = _math.hypot(dx, dy)
            if r > 6.6:
                continue
            a = _math.atan2(dy, dx) / (2 * _math.pi)        # -0.5..0.5
            band = (r * 0.85 - a * 3.0 + frame * 1.5) % 3.0
            if band < 0.9:
                c.px[yy][xx] = deep if r < 4 else dark
            elif band < 1.5 and r < 5.5:
                c.px[yy][xx] = light
    c.rect(7, 7, 8, 8, deep)
    for x, y in ((2, 2), (13, 3), (3, 13)) if frame == 0 else ((2, 3), (12, 2), (13, 13)):
        c.set(x, y, bubble)                  # kenarda kabarcıklar
    return c


def make_t_cactus():
    """Yukarıdan yuvarlak kaktüs (saydam zemin): koyu kenar, ışınsal kaburgalar, açık diken noktaları.
    Duvar gibi üst üste katmanlanıp silindir olarak yükselir."""
    c = Canvas()
    c.ellipse(7.5, 7.5, 6.1, 6.1, l)
    c.ellipse(6.6, 6.6, 3.8, 3.8, L)        # üstten gelen ışık (sol üst)
    for k in range(8):                       # kaburga olukları: merkezden dışa sürekli çizgi
        ang = k * _math.pi / 4 + _math.pi / 8
        for t in range(4, 13):
            rr = t * 0.5
            x, y = int(_math.floor(7.5 + rr * _math.cos(ang))), int(_math.floor(7.5 + rr * _math.sin(ang)))
            if c.px[y][x] is not None:
                c.px[y][x] = D
    c.outline(D)
    for k in range(8):                       # dikenler: oluklar arasındaki çıkıntıların ucunda
        ang = k * _math.pi / 4
        x, y = int(_math.floor(7.5 + 6.6 * _math.cos(ang))), int(_math.floor(7.5 + 6.6 * _math.sin(ang)))
        c.set(x, y, W)
    c.rect(7, 7, 8, 8, PINK)                 # tepede çiçek (yukarıdan bakınca görünür)
    c.set(7, 7, Y)
    return c


def make_t_roof_adobe(variant):
    rnd = _random.Random(260 + (variant == "b"))
    c = Canvas()
    if variant == "a":
        base, dark, light = (204, 150, 100), (184, 132, 88), (222, 172, 122)
    else:
        base, dark, light = (192, 140, 94), (170, 122, 80), (210, 160, 112)
    _fill(c, base)
    for _ in range(22):
        c.set(rnd.randrange(16), rnd.randrange(16), dark)
    for _ in range(12):
        c.set(rnd.randrange(16), rnd.randrange(16), light)
    for x, y in ((3, 4), (11, 11)):          # düz damda sıva yamaları
        _wset(c, x, y, light); _wset(c, x + 1, y, light); _wset(c, x + 2, y, light)
        _wset(c, x, y + 1, dark); _wset(c, x + 1, y + 1, dark); _wset(c, x + 2, y + 1, dark)
    return c


def make_t_roof_adobe_ridge():
    c = make_t_roof_adobe("a")
    for xx in range(16):                    # ortada alçak korkuluk (parapet) şeridi
        for yy in range(6, 10):
            c.px[yy][xx] = (226, 178, 126)
        c.px[6][xx] = (240, 198, 148)
        c.px[10][xx] = (160, 112, 74)
        c.px[11][xx] = (138, 94, 62)
    for xx in (3, 11):
        c.px[8][xx] = (196, 144, 98)
    return c


def make_t_roof_adobe_eave():
    c = make_t_roof_adobe("b")
    wood, wood_dark, wood_light = (139, 90, 43), (90, 58, 30), (166, 112, 60)
    for xx in range(16):                    # kenar: sıva dudağı, gölge
        c.px[11][xx] = (226, 178, 126)
        c.px[12][xx] = (160, 112, 74)
        c.px[13][xx] = (96, 64, 44)
        c.px[14][xx] = (66, 44, 32)
        c.px[15][xx] = (44, 30, 24)
    for x0 in (1, 5, 9, 13):                 # dışarı taşan ahşap kiriş uçları (2 piksel genişlik)
        c.px[12][x0] = wood_light; c.px[12][x0 + 1] = wood
        c.px[13][x0] = wood; c.px[13][x0 + 1] = wood
        c.px[14][x0] = wood_dark; c.px[14][x0 + 1] = wood_dark
    return c


# ---------------- Ev girişleri: kapı eşiği, paspas, saçak ----------------
# Kapı iki karedir; 32 piksellik tuvale tek parça çizilip sol/sağ karoya bölünür.
# Yön: içerisi karonun ÜST tarafı, dışarısı ALT tarafı (alt duvardaki kapıyı kod dikey çevirir).
def _split(wide):
    """32 genişlikteki tuvali iki 16'lık karoya böler."""
    left, right = Canvas(), Canvas()
    for yy in range(16):
        for xx in range(16):
            left.px[yy][xx] = wide.px[yy][xx]
            right.px[yy][xx] = wide.px[yy][xx + 16]
    return left, right


_DOOR_THEMES = {
    # zemin, kasa (ana, açık, koyu), kanat (ana, açık, koyu), eşik (ana, açık, koyu)
    "koy": (make_t_wood, ((214, 206, 190), (246, 242, 232), (140, 130, 118)),
            ((62, 137, 72), (99, 199, 77), (38, 92, 66)),
            ((150, 102, 58), (186, 134, 82), (90, 58, 30))),
    "kar": (make_t_stone, ((139, 90, 43), (176, 124, 72), (90, 58, 30)),
            ((162, 38, 51), (228, 59, 68), (104, 30, 40)),
            ((176, 180, 192), (214, 218, 228), (110, 112, 124))),
    "col": (make_t_adobe, ((139, 90, 43), (176, 124, 72), (90, 58, 30)),
            ((31, 79, 191), (79, 164, 240), (24, 52, 128)),
            ((226, 210, 178), (246, 236, 210), (150, 124, 92))),
}


def make_t_door(theme):
    floor_fn, (jm, jl, jd), (lm, ll, ld), (sm, sl, sd) = _DOOR_THEMES[theme]
    floor = floor_fn()
    glow = (254, 231, 97)
    c = Canvas(32, 16)
    for yy in range(16):
        for xx in range(32):
            c.px[yy][xx] = floor.px[yy][xx % 16]
    # içeriden sızan sıcak ışık: üstte güçlü, eşiğe doğru söner; en üst sıra çatı gölgesi
    for yy in range(12):
        for xx in range(32):
            t = 0.75 - yy * 0.035 - abs(xx - 15.5) * 0.012
            c.px[yy][xx] = _mix(c.px[yy][xx], glow, t)
    for xx in range(32):
        c.px[0][xx] = _mix(c.px[0][xx], K, 0.6)
        c.px[1][xx] = _mix(c.px[1][xx], K, 0.3)
    # eşik: dış kenarda boydan boya taş/tahta basamak
    c.rect(0, 12, 31, 12, sl)
    c.rect(0, 13, 31, 14, sm)
    c.rect(10, 12, 21, 12, _mix(sl, glow, 0.5))     # eşiğe düşen ışık
    c.rect(12, 13, 19, 13, _mix(sm, glow, 0.35))
    c.rect(0, 15, 31, 15, sd)
    for xx in (9, 22):
        c.set(xx, 13, sd); c.set(xx, 14, sd)
    if theme == "kar":                       # eşikte biraz kar
        for xx in (5, 6, 7, 18, 19, 25, 26):
            c.set(xx, 12, (240, 244, 250))
        c.set(6, 13, (204, 214, 232)); c.set(19, 13, (204, 214, 232))
    # kalın kasa/pervaz: dış kenarlarda 4 piksel, duvardan farklı renk; dışa doğru taşan baş
    for x0, x1 in ((0, 3), (28, 31)):
        c.rect(x0, 0, x1, 15, jm)
    c.rect(1, 0, 1, 15, jl); c.rect(29, 0, 29, 15, jl)          # parlak yüz
    c.rect(0, 0, 0, 15, K); c.rect(31, 0, 31, 15, K)            # duvarla ayrım çizgisi
    c.rect(3, 0, 3, 15, jd); c.rect(28, 0, 28, 15, jd)          # iç tarafa gölge
    c.rect(4, 1, 4, 15, _mix(jd, K, 0.4)); c.rect(27, 1, 27, 15, _mix(jd, K, 0.4))
    c.rect(0, 15, 3, 15, jd); c.rect(28, 15, 31, 15, jd)
    # içe doğru açılmış iki kanat (yukarıdan ince levha), menteşesi kasada
    for x0 in (5, 25):
        c.rect(x0, 2, x0 + 1, 10, lm)
        c.rect(x0, 2, x0, 10, ll)
        c.rect(x0 + 1, 2, x0 + 1, 10, ld)
        c.set(x0, 1, K); c.set(x0 + 1, 1, K)
        c.set(x0, 11, ld); c.set(x0 + 1, 11, ld)
    c.set(6, 9, (254, 231, 97)); c.set(25, 9, (254, 231, 97))  # kapı kolu
    return _split(c)


def make_t_doormat(theme):
    c = Canvas(32, 16)
    if theme == "koy":                        # hasır paspas: örgü deseni, koyu bordür
        edge, main, alt_, dark = (139, 90, 43), (229, 190, 110), (200, 156, 80), (90, 58, 30)
        c.rect(3, 3, 28, 12, main)
        for yy in range(4, 12):
            for xx in range(4, 28):
                if (xx // 2 + yy // 2) % 2 == 0:
                    c.px[yy][xx] = alt_
        c.rect(4, 4, 27, 4, edge); c.rect(4, 11, 27, 11, edge)
        c.rect(4, 4, 4, 11, edge); c.rect(27, 4, 27, 11, edge)
        c.outline(dark)
        for xx in range(3, 29, 2):            # püsküller
            c.set(xx, 13, (229, 190, 110))
    elif theme == "kar":                      # taş basamak, üstünde düzensiz kar
        top, face, dark, snow, shade = (150, 154, 166), (120, 124, 136), (72, 74, 86), (240, 244, 250), (204, 214, 232)
        c.rect(2, 2, 29, 10, top)
        c.rect(2, 2, 29, 2, (176, 180, 192))
        c.rect(2, 11, 29, 12, face)
        c.rect(2, 13, 29, 13, dark)
        for xx in (10, 21):
            c.rect(xx, 3, xx, 12, dark)
        for x0, y0, ww in ((3, 3, 5), (4, 4, 3), (14, 3, 4), (24, 3, 5), (25, 4, 4), (17, 8, 3), (6, 9, 2)):
            c.rect(x0, y0, x0 + ww - 1, y0, snow)
        for x0, y0, ww in ((4, 5, 2), (25, 5, 3), (15, 4, 2), (17, 9, 3)):
            c.rect(x0, y0, x0 + ww - 1, y0, shade)
        c.outline(K)
    else:                                     # renkli kilim: kırmızı zemin, sarı bordür, ortada baklava
        c.rect(3, 3, 28, 12, (228, 59, 68))
        c.rect(4, 4, 27, 11, (247, 118, 34))
        c.rect(5, 5, 26, 10, (162, 38, 51))
        c.poly([(16, 5), (21, 8), (16, 11), (11, 8)], (254, 231, 97))
        c.poly([(16, 6.5), (18.5, 8), (16, 9.5), (13.5, 8)], (31, 79, 191))
        for cx in (8, 24):                    # yanlarda küçük baklavalar
            c.poly([(cx, 6), (cx + 2, 8), (cx, 10), (cx - 2, 8)], (254, 231, 97))
            c.set(cx - 1, 7, (99, 199, 77)); c.set(cx, 8, (99, 199, 77)); c.set(cx - 1, 8, (99, 199, 77)); c.set(cx, 7, (99, 199, 77))
        c.outline((104, 56, 108))
        for xx in range(3, 29, 2):            # püsküller iki uçta
            c.set(xx, 14, (242, 232, 210))
            c.set(xx, 1, (242, 232, 210))
    return _split(c)


def make_t_awning(theme):
    """Kapı boşluğunun üstüne saçak: üst 6-7 piksel dolu, alt kenar koyu gölgeli, saydam zemin. Yan yana döşenir."""
    c = Canvas()
    if theme == "koy":
        _shingles(c, (172, 74, 56), (206, 104, 74), (116, 46, 42))
        c.erase_rect(0, 6, 15, 15)
        c.rect(0, 3, 15, 3, (206, 104, 74))
        c.rect(0, 4, 15, 4, (116, 46, 42))
        c.rect(0, 5, 15, 5, (38, 18, 22))
        for xx in (2, 7, 12):                 # kiriş uçları
            c.set(xx, 5, (139, 90, 43))
    elif theme == "kar":
        snow, shade = (240, 244, 250), (204, 214, 232)
        c.rect(0, 0, 15, 2, snow)
        for xx in (1, 6, 7, 12):
            c.set(xx, 1, (255, 255, 255))
        c.rect(0, 2, 15, 2, shade)
        c.rect(0, 3, 15, 3, (120, 56, 48))
        for xx in (3, 4, 10):                 # kar kenardan sarkar
            c.set(xx, 3, snow)
        c.rect(0, 4, 15, 4, (84, 36, 36))
        c.rect(0, 5, 15, 5, (38, 18, 22))
        for xx, ln in ((2, 2), (8, 2), (13, 2)):   # sarkan buzlar
            for k in range(ln):
                c.set(xx, 5 + k, (224, 244, 255) if k == 0 else (176, 220, 246))
    else:                                     # çizgili kumaş tente, alt kenarı dalgalı
        for xx in range(16):
            red = (xx // 4) % 2 == 0
            main, shade, dark = ((228, 59, 68), (196, 48, 58), (104, 30, 40)) if red else \
                ((242, 232, 210), (214, 200, 172), (150, 124, 92))
            c.rect(xx, 1, xx, 3, main)
            c.set(xx, 4, shade)
            if xx % 4 in (1, 2):              # sarkan dilimler
                c.set(xx, 5, shade)
                c.set(xx, 6, dark)
            else:
                c.set(xx, 5, dark)
        c.rect(0, 0, 15, 0, (139, 90, 43))    # duvara bağlandığı ahşap çıta
    return c


# ================= Arayüz: Koleksiyon defteri =================
# Düz arayüz resimleri (16x16 değil). 9-slice sınırları (sol, alt, sağ, üst; piksel):
#   ui_book_page 64x64 -> 8; ui_card_frame* 32x40 -> 6. Unity'de sprite border olarak ayarlanır.
# Kâğıt/krem tonları paletteki S ve W'nin karışımından türetilir (yeni renk uydurulmaz).

# Renk karışımı için yukarıdaki ortak _mix kullanılır.
PAPER = _mix(_mix(S, W, 0.55), Y, 0.22)       # eski, sararmış kâğıt zemini
PAPER_LT = _mix(PAPER, W, 0.35)                # açık lif
PAPER_DK = _mix(PAPER, s, 0.25)                # hafif gölge lif
CREAM = _mix(_mix(S, W, 0.75), Y, 0.12)        # kart içi açık krem


def make_ui_book_page():
    """64x64 eski defter sayfası: kenara doğru koyulaşan bant (8 px), ortası sakin lifli doku, köşelerde lekeler."""
    size, edge = 64, 8
    rnd = _random.Random(404)
    c = Canvas(size, size)
    # Kenardan uzaklığa göre ton: 0 = koyu çerçeve çizgisi, 1..7 = koyudan kâğıda geçiş
    ramp = [n, N, s, _mix(s, S, 0.5), S, _mix(S, PAPER, 0.4), _mix(S, PAPER, 0.75), PAPER]
    for yy in range(size):
        for xx in range(size):
            d = min(xx, yy, size - 1 - xx, size - 1 - yy)
            c.px[yy][xx] = ramp[d] if d < edge else PAPER
    # Yıpranmış kenar: bandın içinde rastgele birkaç piksel bir ton koyu
    for _ in range(90):
        xx, yy = rnd.randrange(size), rnd.randrange(size)
        d = min(xx, yy, size - 1 - xx, size - 1 - yy)
        if 2 <= d < edge - 1:
            c.px[yy][xx] = ramp[d - 1]
    # Orta doku: seyrek, düşük kontrastlı lifler (gerilince/döşenince göze batmaz)
    for _ in range(70):
        xx, yy = rnd.randrange(edge, size - edge), rnd.randrange(edge, size - edge)
        c.px[yy][xx] = PAPER_LT if rnd.random() < 0.6 else PAPER_DK
    # Lekeler: köşe karelerinde (9-slice'ta gerilmez)
    for cx, cy, r_ in ((5, 5, 2.6), (58, 57, 3.2), (57, 6, 1.8)):
        for yy in range(size):
            for xx in range(size):
                dd = ((xx - cx) ** 2 + (yy - cy) ** 2) ** 0.5
                if dd <= r_ - 0.8:
                    c.px[yy][xx] = _mix(c.px[yy][xx], s, 0.45)
                elif dd <= r_:
                    c.px[yy][xx] = _mix(c.px[yy][xx], N, 0.35)   # lekenin koyu halkası
    # Köşeleri 1 piksel yuvarla
    for xx, yy in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1)):
        c.px[yy][xx] = None
    return c


def _card_frame(outer, rim, rim_hi, rim_lo, inner_line, fill, corner_gem):
    """32x40 kart çerçevesi, 6 px kenar: koyu dış çizgi, kabartmalı (üst-sol açık, alt-sağ koyu) kenar, iç çizgi, dolgu."""
    w_, h_ = 32, 40
    c = Canvas(w_, h_)
    c.rect(0, 0, w_ - 1, h_ - 1, outer)                 # d = 0
    c.rect(1, 1, w_ - 2, h_ - 2, rim)                   # d = 1..4 kenar gövdesi
    c.rect(1, 1, w_ - 2, 1, rim_hi)                     # üst ışık
    c.rect(1, 1, 1, h_ - 2, rim_hi)                     # sol ışık
    c.rect(1, h_ - 2, w_ - 2, h_ - 2, rim_lo)           # alt gölge
    c.rect(w_ - 2, 1, w_ - 2, h_ - 2, rim_lo)           # sağ gölge
    c.rect(4, 4, w_ - 5, h_ - 5, rim_lo)                # iç oluk (d = 4)
    c.rect(5, 5, w_ - 6, h_ - 6, inner_line)            # iç çizgi (d = 5)
    c.rect(6, 6, w_ - 7, h_ - 7, fill)                  # içerik alanı
    # Köşe perçinleri (köşe karelerinin içinde: 2..3)
    for x0, y0 in ((2, 2), (w_ - 4, 2), (2, h_ - 4), (w_ - 4, h_ - 4)):
        c.rect(x0, y0, x0 + 1, y0 + 1, corner_gem)
        c.set(x0, y0, rim_hi)
    # Köşeleri 1 piksel yuvarla
    for xx, yy in ((0, 0), (w_ - 1, 0), (0, h_ - 1), (w_ - 1, h_ - 1)):
        c.px[yy][xx] = None
    return c


def make_ui_card_frame():
    """Keşfedilmiş kelime kartı: altın kenar, kahve çizgiler, açık krem iç."""
    return _card_frame(outer=n, rim=y, rim_hi=Y, rim_lo=N, inner_line=n, fill=CREAM, corner_gem=O)


def make_ui_card_frame_seen():
    """Görüldü ama keşfedilmedi: soluk gri-mavi kenar, soğuk açık iç."""
    return _card_frame(outer=G, rim=g, rim_hi=w, rim_lo=G, inner_line=G, fill=_mix(w, W, 0.45), corner_gem=_mix(g, B, 0.35))


def _draw_question(c, ox, oy, col, shadow):
    """6x9 kabartma soru işareti; gölge 1 piksel sağ-alta düşer."""
    glyph = [".####.",
             "##..##",
             "....##",
             "...##.",
             "..##..",
             "..##..",
             "......",
             "..##..",
             "..##.."]
    for pass_col, d in ((shadow, 1), (col, 0)):
        for yy, row in enumerate(glyph):
            for xx, ch in enumerate(row):
                if ch == "#":
                    c.set(ox + xx + d, oy + yy + d, pass_col)


def make_ui_card_frame_locked(with_mark=True):
    """Kilitli kart: koyu mürekkep tonları, ortada kabartma '?'."""
    c = _card_frame(outer=K, rim=G, rim_hi=g, rim_lo=INK, inner_line=K, fill=INK, corner_gem=K)
    if with_mark:
        _draw_question(c, 13, 15, g, K)
    return c


def make_ui_icon_book():
    """12x12 defter düğmesi: kahve kapak, solda koyu sırt, sağda krem sayfa kenarı, altın amblem."""
    c = Canvas(12, 12)
    c.rect(2, 1, 9, 10, N)            # kapak
    c.rect(2, 1, 3, 10, n)            # sırt
    c.rect(10, 2, 10, 10, CREAM)      # sayfa kenarı
    c.rect(4, 10, 10, 10, CREAM)      # alt sayfa kenarı
    c.rect(5, 3, 8, 3, Y)             # başlık bandı
    c.rect(6, 5, 7, 7, Y)             # amblem
    c.set(7, 7, y)
    c.rect(4, 1, 9, 1, _mix(N, S, 0.3))   # üst ışık
    c.outline(K)
    return c


def make_ui_icon_lock():
    """12x12 kilit: gri halka, altın gövde, koyu anahtar deliği."""
    c = Canvas(12, 12)
    c.rect(3, 1, 8, 2, g)             # halka üstü
    c.rect(3, 1, 4, 5, g)             # halka sol bacak
    c.rect(7, 1, 8, 5, g)             # halka sağ bacak
    c.rect(4, 1, 7, 1, w)             # halka ışığı
    c.rect(2, 5, 9, 10, y)            # gövde
    c.rect(2, 5, 9, 5, Y)             # gövde üst ışık
    c.rect(2, 10, 9, 10, N)           # gövde alt gölge
    c.rect(5, 7, 6, 8, K)             # anahtar deliği
    c.set(5, 9, K)
    c.set(6, 9, K)
    c.erase_rect(0, 0, 11, 0)
    c.outline(K)
    return c


# ---------------- Saldırı kareleri (<ad>_atk) ----------------
# Her yaratığın saldırı pozu kendi çiziminden türetilir: make_<ad>() çalışırken kenar çizgisinden hemen önce poz uygulanır
# (ağız açılır, pençe/kol/silah kalkar). Aynı tuval, aynı ölçek; voxel modeli de otomatik çıkar. Oyun saldırı anında
# "<ad>_atk" varsa onu gösterir (CreatureAttackAnimator). Pozu olmayan yaratığa genel "atılma" pozu (_lunge) verilir.
_attack_pose = None


def draw_attack(make_fn, pose):
    """make_fn'in çizimine, kenar çizgisinden hemen önce saldırı pozunu uygular."""
    global _attack_pose
    _attack_pose = pose
    try:
        return make_fn()
    finally:
        _attack_pose = None


def _apply_attack_pose(cv):
    """Canvas.outline çağırır: saldırı karesi çiziliyorsa poz bir kez uygulanır."""
    global _attack_pose
    if _attack_pose is not None:
        pose, _attack_pose = _attack_pose, None
        pose(cv)


def _move(c, x0, y0, x1, y1, dx, dy, mirror=True):
    """Dikdörtgendeki pikselleri (dx, dy) kadar taşır; mirror açıksa sağdaki aynası da (dx ters) taşınır."""
    boxes = [(x0, x1, dx)]
    if mirror:
        boxes.append((c.w - 1 - x1, c.w - 1 - x0, -dx))
    moved = []
    for bx0, bx1, ddx in boxes:
        for yy in range(y0, y1 + 1):
            for xx in range(bx0, bx1 + 1):
                if 0 <= xx < c.w and 0 <= yy < c.h and c.px[yy][xx] is not None:
                    moved.append((xx + ddx, yy + dy, c.px[yy][xx]))
                    c.px[yy][xx] = None
    for xx, yy, col in moved:
        c.set(xx, yy, col)


def _bite(c, x0, x1, y):
    """Açık ağız (2 satır): koyu boğaz, kırmızı dil, köşelerde sivri dişler."""
    c.rect(x0, y, x1, y + 1, K)
    c.rect(x0 + 1, y + 1, x1 - 1, y + 1, R)
    c.set(x0, y, W); c.set(x1, y, W)
    c.set(x0 + 1, y + 1, W); c.set(x1 - 1, y + 1, W)


def _claws(c, x, y_):
    """Kalkmış pençenin ucuna beyaz tırnaklar (aynalı)."""
    c.sym_set(x, y_, W)
    c.sym_set(x + 1, y_, W)


def _lunge(c):
    """Genel poz: üst yarı 1 piksel yukarı uzar (gerinip atılma)."""
    if any(p is not None for p in c.px[0]):
        return
    mid = c.h // 2
    _move(c, 0, 0, c.w - 1, mid - 1, 0, -1, mirror=False)
    for xx in range(c.w):                   # açılan satırı alttan doldur: gövde kopmasın
        if c.px[mid - 1][xx] is None and c.px[mid][xx] is not None and c.px[mid - 2][xx] is not None:
            c.px[mid - 1][xx] = c.px[mid][xx]


def _charge(c, puff=w):
    """Boynuzla hücum: kafa 1 piksel eğilir, burundan buhar çıkar."""
    _move(c, 0, 0, c.w - 1, 8, 0, 1, mirror=False)
    c.sym_set(2, 9, puff); c.sym_set(1, 8, puff)


def _pose_ejderha(c):
    _move(c, 0, 3, 4, 11, 0, -2)            # kanatlar kalkar
    _bite(c, 6, 9, 7)
    c.rect(6, 9, 9, 9, O); c.rect(7, 10, 8, 10, Y)   # ağızdan ateş


def _pose_timsah(c):
    _move(c, 0, 0, c.w - 1, 4, 0, -1, mirror=False)  # üst çene açılır
    c.rect(3, 4, 12, 4, L)
    c.rect(4, 4, 11, 6, R)
    for x in range(4, 12, 2):
        c.set(x, 4, W); c.set(x + 1, 6, W)


def _pose_godzilla(c):
    c.rect(11, 6, 14, 7, R)                 # açık çene
    c.rect(12, 5, 14, 5, W); c.rect(12, 8, 14, 8, W)
    _move(c, 11, 9, 14, 10, 0, -1, mirror=False)   # pençe kalkar


def _pose_fil(c):
    c.rect(7, 9, 8, 13, g); c.erase_rect(7, 14, 8, 15)  # hortum havaya kalkar
    c.rect(9, 3, 10, 8, g); c.rect(10, 3, 10, 8, G); c.rect(11, 2, 11, 3, g)
    c.rect(7, 9, 8, 10, PINK); c.set(7, 10, R)          # açık ağız (borazan)


def _pose_mamut(c):
    c.rect(7, 9, 8, 13, n); c.erase_rect(7, 14, 8, 14)
    c.rect(9, 3, 10, 8, N); c.rect(11, 2, 11, 3, N)
    c.rect(7, 8, 8, 9, PINK); c.set(7, 9, R)


def _pose_iskelet(c):
    c.erase_rect(2, 9, 3, 10); c.erase_rect(12, 9, 13, 10)   # kemik kollar kalkar
    c.sym_rect(2, 6, 3, 8, W); c.sym_set(3, 9, W)
    c.rect(6, 7, 9, 7, K); c.rect(6, 8, 9, 8, w)             # çene düşer


def _pose_bulut(c):
    c.erase_rect(0, 11, 15, 15)                             # yağmur yerine şimşek
    c.poly([(8, 10), (5, 13.5), (7.5, 13.5), (6, 16), (10.5, 12), (8, 12), (10, 10)], Y)


def _pose_piranha(c):
    c.rect(1, 8, 7, 10, R)
    for x in (1, 3, 5, 7):
        c.set(x, 8, W)
    for x in (2, 4, 6):
        c.set(x, 10, W)


def _pose_kopekbaligi(c):
    c.rect(1, 9, 6, 11, R)
    for x in (1, 3, 5):
        c.set(x, 9, W)
    for x in (2, 4, 6):
        c.set(x, 11, W)


def _pose_kirpi(c):
    for x in range(3, 13, 2):               # dikenler kabarır
        c.rect(x, 3, x, 4, INK)


def _pose_peri(c):
    c.rect(11, 6, 11, 8, S)                 # kalkan kol ve asa
    c.rect(12, 3, 12, 6, y)
    c.rect(12, 2, 14, 2, Y); c.set(13, 1, Y); c.set(13, 3, Y)


ATTACK_POSES = {
    "zombi": lambda c: (_move(c, 2, 8, 4, 9, 0, -2), _bite(c, 6, 9, 8)),
    "ari": lambda c: (_move(c, 1, 3, 3, 7, 0, -1), c.rect(7, 14, 8, 15, K)),
    "fare": lambda c: _bite(c, 6, 9, 10),
    "kurt": lambda c: _bite(c, 6, 9, 8),
    "bulut": _pose_bulut,
    "kaplumbaga": lambda c: (_move(c, 11, 7, 15, 12, 1, 0, mirror=False), c.set(15, 10, R)),
    "ejderha": _pose_ejderha,
    "unknown": _lunge,
    "godzilla": _pose_godzilla,
    "kingkong": lambda c: (_move(c, 1, 9, 2, 14, 0, -5), _bite(c, 6, 9, 7)),
    "robot": lambda c: (_move(c, 2, 7, 3, 12, 0, -3), c.sym_set(6, 3, Y), c.rect(6, 8, 9, 10, R), c.set(7, 9, Y)),
    "yeti": lambda c: (_move(c, 1, 9, 2, 13, 0, -4), _claws(c, 1, 4), _bite(c, 6, 9, 7)),
    "orumcek": lambda c: (_move(c, 0, 1, 4, 5, 0, -1), c.sym_set(6, 8, W), c.sym_set(7, 8, W)),
    "tepegoz": lambda c: (_move(c, 12, 6, 14, 13, 0, -4, mirror=False), _bite(c, 5, 10, 8)),
    "aslan": lambda c: (_move(c, 2, 9, 3, 12, 0, -4), _claws(c, 2, 5), _bite(c, 6, 9, 7)),
    "yilan": lambda c: (c.set(12, 4, W), c.set(12, 5, R), c.set(12, 6, W), c.rect(13, 5, 14, 5, R), c.set(15, 4, R), c.set(15, 6, R)),
    "hayalet": lambda c: (c.ellipse(7.5, 9.5, 1.6, 2.0, K), c.sym_rect(1, 6, 2, 9, W)),
    "yarasa": lambda c: (_move(c, 0, 5, 6, 11, 0, -2), _bite(c, 6, 9, 9)),
    "karinca": lambda c: (c.rect(0, 7, 1, 7, W), c.rect(0, 11, 1, 11, W), c.set(2, 9, R)),
    "boga": lambda c: _charge(c),
    "ninja": lambda c: (c.erase_rect(12, 4, 14, 5), c.rect(11, 9, 12, 10, INK), c.rect(12, 2, 13, 8, w), c.rect(11, 8, 14, 8, y)),
    "kopek": lambda c: _bite(c, 6, 9, 8),
    "kedi": lambda c: (c.sym_rect(2, 7, 3, 10, G), _claws(c, 2, 6), _bite(c, 6, 9, 9)),
    "timsah": _pose_timsah,
    "ayi": lambda c: (_move(c, 1, 9, 2, 12, 0, -5), _claws(c, 1, 3), _bite(c, 6, 9, 7)),
    "iskelet": _pose_iskelet,
    "cadi": lambda c: (_move(c, 12, 8, 12, 15, 0, -3, mirror=False), c.set(11, 4, P), c.set(13, 4, P), c.set(12, 3, Y), c.set(8, 10, K)),
    "korsan": lambda c: (_move(c, 13, 9, 13, 14, 0, -6, mirror=False), c.rect(12, 9, 13, 10, S), c.rect(7, 9, 8, 9, K)),
    "kartal": lambda c: (_move(c, 0, 6, 6, 12, 0, -2), c.sym_rect(5, 13, 6, 14, y), c.set(8, 7, R)),
    "akrep": lambda c: (_move(c, 0, 7, 3, 9, 0, -2), c.rect(5, 1, 6, 2, R)),
    "hamamboecegi": lambda c: (_move(c, 0, 5, 7, 15, -1, 0), c.rect(7, 6, 8, 14, INK)),
    "piranha": _pose_piranha,
    "kraken": lambda c: (_move(c, 1, 9, 3, 14, 0, -4), c.rect(7, 8, 8, 8, K)),
    "tavuk": lambda c: (_move(c, 6, 1, 13, 8, 1, 0, mirror=False), c.set(13, 6, K), c.rect(2, 6, 4, 8, w)),
    "at": lambda c: (_move(c, 10, 12, 12, 15, 1, -3, mirror=False), c.set(14, 5, R)),
    "inek": lambda c: _charge(c, g),
    "domuz": lambda c: (c.rect(6, 10, 9, 11, K), c.rect(7, 11, 8, 11, R), c.sym_set(5, 10, W), c.sym_set(5, 6, R)),
    "keci": lambda c: _charge(c, g),
    "tavsan": lambda c: (c.sym_rect(2, 8, 3, 10, W), c.sym_set(2, 7, w), c.rect(7, 9, 8, 10, W), c.sym_set(6, 7, R)),
    "kurbaga": lambda c: (c.rect(4, 10, 11, 10, K), c.rect(7, 11, 8, 14, PINK), c.rect(6, 14, 9, 15, R)),
    "kertenkele": lambda c: (_move(c, 10, 4, 15, 9, 0, -1, mirror=False), c.set(14, 8, R), c.set(15, 8, W), c.set(15, 6, R)),
    "kopekbaligi": _pose_kopekbaligi,
    "karga": lambda c: (_move(c, 0, 7, 6, 12, 0, -2), c.set(8, 7, R)),
    "guvercin": lambda c: (_move(c, 1, 8, 3, 11, 0, -3), c.rect(7, 6, 8, 6, R)),
    "palyaco": lambda c: (c.rect(5, 8, 10, 9, K), c.rect(6, 8, 9, 8, W), c.rect(6, 9, 9, 9, R), c.sym_rect(2, 6, 3, 8, W)),
    "asker": lambda c: (_move(c, 12, 8, 13, 13, 0, -3, mirror=False), c.rect(12, 3, 13, 4, Y), c.set(12, 2, O), c.set(13, 2, O)),
    "polis": lambda c: (c.rect(12, 8, 12, 9, S), c.rect(12, 3, 13, 7, INK)),
    "uzayli": lambda c: (c.erase_rect(3, 11, 4, 12), c.erase_rect(11, 11, 12, 12), c.sym_rect(3, 8, 4, 10, L), c.recolor(K, R)),
    "goblin": lambda c: (_move(c, 11, 9, 11, 14, 0, -4, mirror=False), c.rect(12, 9, 12, 10, l), _bite(c, 6, 9, 9)),
    "balina": lambda c: (c.rect(1, 10, 5, 11, K), c.rect(2, 11, 4, 11, R), c.rect(8, 0, 10, 3, C)),
    "minotor": lambda c: (_move(c, 11, 6, 14, 14, 0, -4, mirror=False), c.sym_set(2, 8, w), c.sym_set(1, 7, w)),
    "medusa": lambda c: (c.sym_rect(5, 5, 6, 6, Y), c.sym_set(6, 5, W), c.rect(6, 8, 9, 8, K), c.rect(7, 8, 8, 8, R)),
    "golem": lambda c: (_move(c, 1, 7, 3, 12, 0, -4), c.sym_set(6, 4, R), c.rect(6, 9, 9, 10, O)),
    "gargoyle": lambda c: (_move(c, 0, 3, 5, 12, 0, -2), _bite(c, 6, 9, 7)),
    "fil": _pose_fil,
    "mamut": _pose_mamut,
    "cakal": lambda c: _bite(c, 6, 9, 8),
    "sirtlan": lambda c: _bite(c, 6, 9, 7),
    "penguen": lambda c: (_move(c, 1, 8, 2, 11, 0, -3), c.set(8, 7, R)),
    "sumuklu": lambda c: (c.set(1, 7, s), c.set(4, 7, s), c.set(1, 6, K), c.set(4, 6, K), c.rect(0, 14, 15, 15, C)),
    "solucan": lambda c: (_move(c, 8, 3, 13, 8, 0, -1, mirror=False), c.rect(10, 6, 11, 6, K), c.rect(10, 7, 11, 7, R)),
    "denizanasi": lambda c: c.recolor(P, Y, 9, 15),
    "pterodaktil": lambda c: (_move(c, 0, 6, 6, 10, 0, -2), c.rect(10, 5, 13, 5, R)),
    "hirsiz": lambda c: _move(c, 10, 9, 15, 14, 0, -4, mirror=False),
    "tilki": lambda c: _bite(c, 6, 9, 8),
    "baykus": lambda c: (c.sym_rect(1, 7, 2, 11, n), c.sym_rect(5, 13, 6, 14, y), c.set(8, 9, R)),
    "sincap": lambda c: _move(c, 3, 11, 7, 13, 0, -6, mirror=False),
    "yengec": lambda c: _move(c, 0, 4, 2, 6, 0, -2),
    "deve": lambda c: (c.set(14, 4, R), c.rect(15, 2, 15, 4, C)),
    "kanguru": lambda c: _move(c, 2, 7, 4, 9, 1, -2),
    "panda": lambda c: (_move(c, 1, 8, 3, 12, 0, -4), _claws(c, 1, 4), _bite(c, 6, 9, 7)),
    "kirpi": _pose_kirpi,
    "viking": lambda c: (_move(c, 12, 6, 14, 14, 0, -4, mirror=False), c.rect(7, 8, 8, 8, K), c.rect(7, 9, 8, 9, R)),
    "gladyator": lambda c: (_move(c, 13, 6, 13, 13, 0, -5, mirror=False), c.rect(12, 8, 13, 9, s)),
    "sumocu": lambda c: (_move(c, 3, 14, 5, 15, -1, -3, mirror=False), c.rect(13, 6, 15, 8, s)),
    "boksor": lambda c: _move(c, 0, 5, 4, 9, 2, -2, mirror=False),
    "futbolcu": lambda c: (_move(c, 10, 11, 15, 15, 1, -3, mirror=False), _move(c, 9, 14, 10, 15, 1, -1, mirror=False)),
    "asci": lambda c: (_move(c, 12, 6, 14, 13, 0, -4, mirror=False), c.rect(7, 8, 8, 8, K)),
    "maymun": lambda c: (_move(c, 3, 8, 4, 14, 0, -5), _bite(c, 6, 9, 7)),
    "papagan": lambda c: (_move(c, 0, 6, 6, 12, 0, -2), c.rect(7, 8, 8, 8, R)),
    "su_aygiri": lambda c: (c.rect(3, 10, 12, 10, g), c.rect(4, 6, 11, 9, PINK), c.rect(4, 6, 11, 6, P), c.rect(5, 9, 10, 9, P),
                            c.sym_rect(4, 7, 4, 8, W), c.sym_set(6, 7, W), c.rect(3, 11, 12, 11, p)),
    "peri": _pose_peri,
    "kovboy": lambda c: (_move(c, 12, 11, 13, 12, 0, -4, mirror=False), c.set(14, 7, Y), c.set(15, 7, O)),
}


# ---------------- Hazırlık kareleri (<ad>_atk0) ----------------
# Vuruştan hemen önceki "kurulma" pozu: gövde çöker (üst yarı 1 piksel aşağı), kol/pençe/silah geriye-dışa çekilir,
# ağız kapanır, kaşlar çatılır. Saldırı karesiyle (_atk) zıt hisseder: önce toplanır, sonra atılır. Aynı tuval ve voxel
# katman sayısı. Pozu olmayan yaratığa genel çömelme (_recoil) verilir.
def _crouch(c, n=1):
    """Üst yarı n piksel aşağı çöker (kafa geride, gövde gerilmiş)."""
    _move(c, 0, 0, c.w - 1, c.h // 2 - 1, 0, n, mirror=False)


def _recoil(c):
    """Genel hazırlık pozu: çömelme."""
    _crouch(c)


def _tilt(c, x0, y0, x1, y1, grip_y, k=0.4, dy=0):
    """Kutudaki silahı tutma noktası (grip_y) etrafında dışa (sağa) yatırır: üst uç geriye savrulmuş gibi."""
    moved = []
    for yy in range(y0, y1 + 1):
        for xx in range(x0, x1 + 1):
            if 0 <= xx < c.w and 0 <= yy < c.h and c.px[yy][xx] is not None:
                moved.append((xx + int(round((grip_y - yy) * k)), yy + dy, c.px[yy][xx]))
                c.px[yy][xx] = None
    for xx, yy, col in moved:
        c.set(xx, yy, col)


def _brows(c, x, y_, col=K):
    """Çatık kaş: gözün üstüne 2 piksel (aynalı)."""
    c.sym_set(x, y_, col)
    c.sym_set(x + 1, y_, col)


def _paw_back(c):
    """Yer tepen hayvan: ön ayak kalkar, arkasında toz."""
    _move(c, 2, 12, 5, 15, 0, -1, mirror=False)
    c.set(1, 15, g); c.set(0, 15, w)


def _pose0_godzilla(c):
    c.rect(12, 7, 14, 7, l)                         # ağız kapanır
    _move(c, 11, 9, 14, 10, -2, 1, mirror=False)    # pençe geri çekilir
    _move(c, 9, 0, 15, 7, -1, 1, mirror=False)      # kafa geriye-aşağı
    _move(c, 2, 11, 6, 15, 0, -1, mirror=False)     # kuyruk kalkar


def _pose0_ninja(c):
    _crouch(c)
    c.rect(11, 10, 12, 11, INK)                     # kol omuz üstünde
    c.set(13, 9, y); c.set(12, 10, y)               # kabza
    for xx, yy in ((13, 8), (14, 7), (14, 6), (15, 5), (15, 4)):   # kılıç geriye-yukarı yatık
        c.set(xx, yy, w)
    c.set(14, 8, W)


def _pose0_bulut(c):
    c.recolor(g, G); c.recolor(w, g); c.recolor(W, w)   # bulut kararır, yük toplar
    c.erase_rect(0, 11, 15, 15)
    _crouch(c)
    c.set(5, 11, Y); c.set(10, 11, Y); c.set(8, 12, y)  # şimşek öncesi kıvılcım


def _pose0_timsah(c):
    c.rect(3, 3, 12, 6, L)                          # çene kapanır
    c.rect(4, 6, 11, 6, K)
    for x in (4, 7, 10):
        c.set(x, 6, W)
    _crouch(c)


def _pose0_piranha(c):
    for x in range(2, 8):
        c.set(x, 8, g); c.set(x, 10, R)
    c.rect(2, 9, 7, 9, K)
    c.set(2, 9, W); c.set(7, 9, W)
    _crouch(c)


def _pose0_kopekbaligi(c):
    for x in range(1, 7):
        c.set(x, 9, g); c.set(x, 11, w)
    c.rect(1, 10, 6, 10, K)
    _crouch(c)


def _pose0_su_aygiri(c):
    c.rect(3, 5, 12, 8, g); c.rect(3, 9, 12, 9, p)  # dev ağız kapanır
    c.rect(4, 7, 11, 7, p)
    c.sym_set(5, 8, W)
    c.sym_set(6, 4, K)
    _crouch(c)


def _pose0_panda(c):
    c.erase_rect(12, 6, 12, 13)
    _move(c, 1, 8, 3, 12, -1, -1)
    c.rect(14, 2, 14, 8, L); c.set(14, 4, l)        # bambu geriye kalkık
    _crouch(c)


def _pose0_yilan(c):
    c.erase_rect(13, 5, 15, 5)                      # dil içeride
    _move(c, 8, 2, 15, 7, -1, 1, mirror=False)      # baş geriye çekilir
    _crouch(c, 1)                                   # boyun S yayı sıkışır


WINDUP_POSES = {
    "zombi": lambda c: (_move(c, 2, 8, 4, 9, -1, 1), c.set(6, 8, K), c.set(9, 8, K), _crouch(c)),
    "ari": lambda c: (_move(c, 1, 3, 3, 7, 0, 1), c.erase_rect(7, 14, 8, 14), _crouch(c)),
    "fare": lambda c: (_brows(c, 5, 6, G), _crouch(c)),
    "kurt": lambda c: (c.sym_set(6, 8, w), _brows(c, 5, 3, G), _move(c, 2, 0, 5, 2, -1, 1), _crouch(c)),
    "bulut": _pose0_bulut,
    "kaplumbaga": lambda c: _move(c, 11, 8, 15, 11, -2, 0, mirror=False),
    "ejderha": lambda c: (_move(c, 0, 3, 4, 11, 0, 1), c.sym_set(6, 7, O), _crouch(c)),
    "unknown": _recoil,
    "godzilla": _pose0_godzilla,
    "kingkong": lambda c: (_move(c, 1, 9, 2, 14, -1, -1), _brows(c, 5, 3), _crouch(c)),
    "robot": lambda c: (_move(c, 2, 7, 3, 12, -1, 1), c.sym_set(6, 3, O), c.rect(6, 10, 9, 10, O), _crouch(c)),
    "yeti": lambda c: (_move(c, 1, 9, 2, 13, -1, -1), _brows(c, 5, 4, G), _crouch(c)),
    "orumcek": lambda c: (_move(c, 0, 1, 4, 5, 1, 1), _crouch(c)),
    "tepegoz": lambda c: (_tilt(c, 12, 6, 13, 12, 12, 0.35), c.rect(6, 3, 9, 3, p), c.set(6, 8, p), c.set(9, 8, p), _crouch(c)),
    "aslan": lambda c: (_move(c, 2, 9, 3, 12, -1, -1), c.sym_set(6, 4, n), _crouch(c)),
    "yilan": _pose0_yilan,
    "hayalet": lambda c: (c.ellipse(7.5, 9.5, 1.0, 1.2, W), c.rect(7, 9, 8, 9, K), c.sym_set(4, 5, K), _crouch(c)),
    "yarasa": lambda c: (_move(c, 0, 5, 6, 11, 0, 1), c.sym_set(7, 10, p), _crouch(c)),
    "karinca": lambda c: _move(c, 0, 5, 5, 11, 1, 0, mirror=False),
    "boga": _paw_back,
    "ninja": _pose0_ninja,
    "kopek": lambda c: (c.set(7, 9, N), c.set(8, 9, N), c.sym_set(6, 4, n), _crouch(c)),
    "kedi": lambda c: (_move(c, 3, 1, 6, 3, -1, 1), c.sym_rect(5, 6, 6, 6, Y), _crouch(c)),
    "timsah": _pose0_timsah,
    "ayi": lambda c: (_move(c, 1, 9, 2, 12, -1, -1), c.sym_set(6, 3, K), _crouch(c)),
    "iskelet": lambda c: (_move(c, 2, 9, 3, 10, -1, 1), c.sym_set(6, 5, R), _crouch(c)),
    "cadi": lambda c: (_tilt(c, 12, 8, 12, 15, 15, 0.3), c.set(14, 7, P), _crouch(c)),
    "korsan": lambda c: (_tilt(c, 13, 9, 13, 14, 14, 0.4), c.set(12, 14, S), _crouch(c)),
    "kartal": lambda c: (_move(c, 0, 6, 6, 12, 0, 1), _crouch(c)),
    "akrep": lambda c: (_move(c, 0, 7, 3, 9, 0, 1), _crouch(c)),
    "hamamboecegi": _recoil,
    "piranha": _pose0_piranha,
    "kraken": lambda c: (_move(c, 1, 9, 3, 14, -1, 0), c.sym_rect(5, 4, 6, 4, P), _crouch(c)),
    "tavuk": lambda c: (_move(c, 6, 1, 13, 8, -1, 0, mirror=False), _crouch(c)),
    "at": lambda c: (_move(c, 8, 0, 15, 9, -1, 1, mirror=False), _move(c, 2, 12, 4, 15, 0, -1, mirror=False)),
    "inek": _paw_back,
    "domuz": lambda c: (_brows(c, 4, 5, s), _paw_back(c)),
    "keci": _paw_back,
    "tavsan": lambda c: (_move(c, 5, 0, 6, 2, -1, 1), _crouch(c)),
    "kurbaga": lambda c: _crouch(c, 2),
    "kertenkele": lambda c: (c.set(15, 7, None), _move(c, 10, 4, 15, 9, -1, 1, mirror=False)),
    "kopekbaligi": _pose0_kopekbaligi,
    "karga": lambda c: (_move(c, 0, 7, 6, 12, 0, 1), _crouch(c)),
    "guvercin": lambda c: (_move(c, 1, 8, 3, 11, -1, 1), _crouch(c)),
    "palyaco": lambda c: (c.rect(5, 8, 10, 8, W), c.rect(6, 8, 9, 8, R), _brows(c, 5, 4), _crouch(c)),
    "asker": lambda c: (_tilt(c, 12, 8, 13, 13, 13, 0.3), _crouch(c)),
    "polis": lambda c: (c.rect(12, 11, 12, 14, INK), c.set(12, 10, S), _crouch(c)),
    "uzayli": lambda c: (_move(c, 3, 11, 4, 12, -1, 1), c.set(7, 0, W), _crouch(c)),
    "goblin": lambda c: (_tilt(c, 11, 9, 11, 14, 14, 0.5), c.set(7, 9, K), _crouch(c)),
    "balina": lambda c: (c.erase_rect(8, 0, 10, 3), _crouch(c)),
    "minotor": lambda c: (_move(c, 11, 6, 14, 14, 1, 2, mirror=False), c.sym_set(2, 9, w), _crouch(c)),
    "medusa": lambda c: (c.sym_set(6, 5, L), c.sym_set(6, 6, l), _crouch(c)),
    "golem": lambda c: (_move(c, 1, 7, 3, 12, -1, 1), c.rect(6, 9, 9, 10, B), _crouch(c)),
    "gargoyle": lambda c: (_move(c, 0, 3, 5, 12, 0, 1), c.sym_set(6, 8, g), _crouch(c)),
    "fil": lambda c: (c.erase_rect(7, 14, 8, 15), c.sym_set(6, 6, G), _crouch(c)),
    "mamut": lambda c: (c.erase_rect(7, 13, 8, 14), c.sym_set(6, 4, n), _crouch(c)),
    "cakal": lambda c: (_brows(c, 5, 4, N), _move(c, 3, 0, 6, 3, -1, 1), _crouch(c)),
    "sirtlan": lambda c: (c.set(7, 7, K), c.set(9, 7, K), _brows(c, 5, 4, N), _crouch(c)),
    "penguen": lambda c: (_move(c, 1, 8, 2, 11, -1, 1), _crouch(c)),
    "sumuklu": lambda c: (c.erase_rect(0, 6, 5, 9), c.set(2, 10, s), c.set(4, 10, s), c.set(2, 9, K), c.set(4, 9, K)),
    "solucan": lambda c: _move(c, 8, 3, 13, 8, -1, 2, mirror=False),
    "denizanasi": lambda c: (c.erase_rect(0, 14, 15, 15), _crouch(c, 2)),
    "pterodaktil": lambda c: (_move(c, 0, 6, 6, 10, 0, 1), _crouch(c)),
    "hirsiz": lambda c: (_move(c, 10, 9, 15, 14, 1, 1, mirror=False), _crouch(c)),
    "tilki": lambda c: (_brows(c, 5, 5, r), _move(c, 3, 0, 6, 3, -1, 1), _crouch(c)),
    "baykus": lambda c: (c.rect(4, 5, 7, 5, N), c.rect(8, 5, 11, 5, N), _crouch(c)),
    "sincap": lambda c: (_move(c, 3, 11, 7, 13, -1, 1, mirror=False), _crouch(c)),
    "yengec": lambda c: (_move(c, 0, 4, 2, 6, 0, 2), _crouch(c)),
    "deve": lambda c: (_move(c, 10, 0, 15, 5, -1, 1, mirror=False), c.set(13, 4, w)),
    "kanguru": lambda c: (_move(c, 2, 7, 4, 9, -1, 1), _crouch(c)),
    "panda": _pose0_panda,
    "kirpi": _recoil,
    "viking": lambda c: (_move(c, 12, 6, 14, 14, 1, 1, mirror=False), _crouch(c)),
    "gladyator": lambda c: (_move(c, 0, 7, 4, 13, 1, -1, mirror=False), _tilt(c, 13, 6, 13, 13, 13, 0.3), _crouch(c)),
    "sumocu": lambda c: _crouch(c, 2),
    "boksor": lambda c: (_move(c, 0, 5, 4, 9, 1, -1), _crouch(c)),
    "futbolcu": lambda c: (_move(c, 9, 14, 10, 15, 1, -1, mirror=False), _crouch(c)),
    "asci": lambda c: (_move(c, 12, 6, 14, 13, 1, 1, mirror=False), _crouch(c)),
    "maymun": lambda c: (_move(c, 3, 8, 4, 14, -1, -1), _crouch(c)),
    "papagan": lambda c: (_move(c, 0, 6, 6, 12, 0, 1), _crouch(c)),
    "su_aygiri": _pose0_su_aygiri,
    "peri": lambda c: (c.set(11, 9, S), c.rect(12, 10, 13, 10, y), c.set(14, 11, Y), _crouch(c)),
    "kovboy": lambda c: (_move(c, 12, 11, 13, 12, 0, 1, mirror=False), c.set(12, 11, S), _crouch(c)),
}


# ---------------- Vuruş izi efektleri (fx_<tür>_0..3) ----------------
# Hedefin üstünde oynayan 4 karelik kısa animasyon. Düz sprite (voxel yok), kenar çizgisi yok: parıltı gibi görünsün.
def _fx_line(c, pts, col):
    for xx, yy in pts:
        c.set(xx, yy, col)


def _seg(x0, y0, x1, y1):
    """İki nokta arası piksel listesi."""
    n_ = max(abs(x1 - x0), abs(y1 - y0))
    return [(int(round(x0 + (x1 - x0) * t / n_)), int(round(y0 + (y1 - y0) * t / n_))) for t in range(n_ + 1)]


def make_fx_claw(frame):
    """3 çapraz pençe izi: beliriyor -> parlıyor -> soluyor."""
    c = Canvas()
    for i, (sx, sy) in enumerate(((7, 1), (10, 2), (13, 3))):
        seg = _seg(sx, sy, sx - 6, sy + 11)
        n_ = len(seg)
        if frame == 0:
            part = seg[:n_ // 2]
            _fx_line(c, [(x + 1, y_) for x, y_ in part], Y)
            _fx_line(c, part, W)
        elif frame == 1:
            _fx_line(c, [(x + 1, y_) for x, y_ in seg], Y)
            _fx_line(c, seg, W)
            _fx_line(c, [seg[0], seg[-1]], R)
            c.set(seg[-1][0] + 1, seg[-1][1], r)
        elif frame == 2:
            _fx_line(c, [(x + 1, y_) for x, y_ in seg[2:-2]], O)
            _fx_line(c, seg[1:-1], Y)
            _fx_line(c, [seg[1], seg[-2]], R)
        else:
            _fx_line(c, [p_ for j, p_ in enumerate(seg[2:-2]) if (j + i) % 3 != 0], r)
            _fx_line(c, [seg[n_ // 2]], O)
    return c


def _teeth_row(c, y_, down, col_gum, shift=0):
    """Diş sırası: down=True üst çene (dişler aşağı bakar)."""
    c.rect(1, y_, 14, y_, col_gum)
    tip = 1 if down else -1
    for x in range(2 + shift, 14, 3):
        c.rect(x, y_ + tip, x + 1, y_ + tip, W)
        c.set(x + (0 if down else 1), y_ + 2 * tip, w if down else W)


def make_fx_bite(frame):
    """Üst ve alt diş sırası kapanıyor, sonunda küçük kıvılcım."""
    c = Canvas()
    if frame < 3:
        gap = (5, 3, 1)[frame]
        top, bot = 7 - gap, 8 + gap
        c.rect(2, top + 1, 13, bot - 1, INK)        # koyu ağız içi
        if gap > 2:
            c.rect(3, top + 2, 12, bot - 2, p)
        _teeth_row(c, top, True, r)
        _teeth_row(c, bot, False, r, 1)
        c.rect(2, top - 1, 13, top - 1, R); c.rect(2, bot + 1, 13, bot + 1, R)
    if frame >= 2:
        for xx, yy in ((7, 2), (8, 1), (12, 3), (3, 13), (13, 13), (8, 14)):
            c.set(xx, yy, Y if frame == 2 else O)
        c.set(8, 2, W); c.set(13, 12, W)
    if frame == 3:
        c.rect(3, 7, 12, 8, INK)                    # kapanmış çene izi
        for x in (4, 7, 10):
            c.set(x, 7, W); c.set(x + 1, 8, W)
    return c


def make_fx_slash(frame):
    """Tek kavisli kılıç kesiği, soldan sağa."""
    c = Canvas()
    seen = []
    for t in range(0, 41):
        a = math.radians(160 - t * 3.5)            # 160 -> 20 derece
        p_ = (int(round(8 + 8.5 * math.cos(a))), int(round(14 - 9.5 * math.sin(a))))
        if p_ not in seen:
            seen.append(p_)
    n_ = len(seen)
    a0, a1 = {0: (0, n_ * 2 // 5), 1: (0, n_ * 4 // 5), 2: (n_ // 4, n_), 3: (n_ * 3 // 5, n_)}[frame]
    for i in range(a0, a1):
        x, y_ = seen[i]
        head = (i - a0) / max(1, a1 - a0 - 1)       # 0 = kuyruk, 1 = uç
        if frame == 3:
            if i % 2 == 0:
                c.set(x, y_, B if head < 0.5 else C)
            continue
        c.set(x, y_, W if head > 0.4 else C)
        if 0.25 < head < 0.95:
            c.set(x, y_ + 1, C if head > 0.5 else B)
            if (frame == 1 and 0.35 < head < 0.9) or (frame == 2 and 0.45 < head < 0.8):
                c.set(x, y_ + 2, B)
    if frame in (1, 2):
        x, y_ = seen[a1 - 1]
        c.set(x + 1, y_, W); c.set(x, y_ - 1, C)
    return c


def _star(c, cx, cy, r_out, r_in, points, col, rot=0.0):
    pts = []
    for i in range(points * 2):
        a = rot + math.pi * i / points
        rad = r_out if i % 2 == 0 else r_in
        pts.append((cx + rad * math.cos(a), cy + rad * math.sin(a)))
    c.poly(pts, col)


def make_fx_punch(frame):
    """Yumruk darbesi yıldızı: büyüyüp söner (sarı-turuncu)."""
    c = Canvas()
    cx = cy = 8.0
    if frame == 0:
        _star(c, cx, cy, 3.6, 1.6, 4, Y, 0.4)
        c.ellipse(7.5, 7.5, 1.0, 1.0, W)
    elif frame == 1:
        _star(c, cx, cy, 7.6, 3.4, 7, O, 0.2)
        _star(c, cx, cy, 5.6, 2.6, 7, Y, 0.2)
        c.ellipse(7.5, 7.5, 1.8, 1.8, W)
    elif frame == 2:
        _star(c, cx, cy, 8.4, 4.0, 7, r, 0.5)
        _star(c, cx, cy, 7.4, 3.4, 7, O, 0.5)
        _star(c, cx, cy, 5.2, 2.4, 7, Y, 0.5)
        c.erase_ellipse(7.5, 7.5, 2.2, 2.2)
    else:
        for i in range(7):
            a = 0.5 + 2 * math.pi * i / 7
            c.set(int(round(7.5 + 6.6 * math.cos(a))), int(round(7.5 + 6.6 * math.sin(a))), O)
            c.set(int(round(7.5 + 5.4 * math.cos(a))), int(round(7.5 + 5.4 * math.sin(a))), r)
    return c


def make_fx_stomp(frame):
    """Dev adımı: yer çatlağı ve genişleyen toz halkası (kahverengi-gri)."""
    c = Canvas()
    crack = [(7, 10), (8, 10), (6, 10), (9, 11), (5, 11), (10, 11), (4, 11), (11, 10), (3, 12), (12, 11),
             (7, 9), (9, 12), (6, 8), (10, 13), (5, 12)]
    reach = (5, 15, 15, 15)[frame]
    rx, ry = (2.5, 5.0, 6.8, 7.6)[frame], (1.4, 2.6, 3.4, 3.8)[frame]
    ring_c = (s, s, g, w)[frame]
    for a10 in range(0, 360, 12):                  # toz halkası (yerde yatık elips)
        if frame == 3 and (a10 // 12) % 2:
            continue
        a = math.radians(a10)
        x, y_ = int(round(7.5 + rx * math.cos(a))), int(round(10.5 + ry * math.sin(a)))
        c.set(x, y_, ring_c)
        if frame in (1, 2):
            c.set(x, y_ - 1 if y_ < 10 else y_ + 1, g if frame == 2 else N)
    if frame == 0:
        c.ellipse(7.5, 10.5, 2.0, 1.0, s)
    col = (n, INK, INK, G)[frame]
    for xx, yy in crack[:reach]:
        c.set(xx, yy, col)
    if frame in (1, 2):                             # havaya fırlayan toprak parçaları
        for xx, yy in ((3, 5), (12, 4), (8, 3) if frame == 2 else (9, 6)):
            c.set(xx, yy, N); c.set(xx + 1, yy + 1, n)
    return c


def make_fx_hit(frame):
    """Genel küçük darbe kıvılcımı."""
    c = Canvas()
    if frame == 0:
        c.rect(7, 7, 8, 8, W)
        c.set(7, 6, Y); c.set(8, 9, Y); c.set(6, 8, Y); c.set(9, 7, Y)
    elif frame == 1:
        c.rect(7, 3, 8, 12, Y); c.rect(3, 7, 12, 8, Y)
        c.rect(7, 5, 8, 10, W); c.rect(5, 7, 10, 8, W)
        for d in (2, 3):
            c.set(7 - d, 7 - d, O); c.set(8 + d, 7 - d, O); c.set(7 - d, 8 + d, O); c.set(8 + d, 8 + d, O)
    elif frame == 2:
        for d in range(3, 6):
            col = Y if d < 5 else O
            c.set(7 - d, 7 - d, col); c.set(8 + d, 7 - d, col); c.set(7 - d, 8 + d, col); c.set(8 + d, 8 + d, col)
        for xx, yy in ((7, 1), (8, 1), (7, 14), (8, 14), (1, 7), (1, 8), (14, 7), (14, 8)):
            c.set(xx, yy, Y)
        c.set(7, 7, W); c.set(8, 8, W)
    else:
        for xx, yy in ((1, 1), (14, 1), (1, 14), (14, 14), (7, 0), (15, 8), (0, 7), (8, 15)):
            c.set(xx, yy, O if (xx + yy) % 2 else r)
    return c


FX = {"claw": make_fx_claw, "bite": make_fx_bite, "slash": make_fx_slash,
      "punch": make_fx_punch, "stomp": make_fx_stomp, "hit": make_fx_hit}



# ================= Arayüz: yazı kutusu =================
# Steam kapsülündeki yazı kutusunun (Tools/GenerateStoreArt.py, make_textbox / make_die) oyun içi hâli, aynı renk ve oranlar:
# beyaz iç, ince koyu (K) dış hat (köşeleri boş = 1 px yuvarlak), içte 2. satırda açık mavi (C) parıltı, altta 2 satır gri (w)
# derinlik; altında kutu genişliğinde ince INK yuva içinde sarı-turuncu sayaç; krem/beyaz zar, kırmızı noktalar.
# Kutuların çevresinde 1 px saydam pay var: renkli sürümlerin (hot/bad/ok) dış parıltısı oraya çizilir, boyları aynı kalır.
# 9-slice sınırları (sol, alt, sağ, üst; piksel) UI_BORDERS'ta. Unity spriteBorder = (x=sol, y=alt, z=sağ, w=üst).
# Yarı saydam parıltılar: renk (r, g, b, a) dörtlüsü verilirse ACanvas alfa ile kaydeder (yeni renk değil, paletin saydamı).
UI_BORDERS = {
    "ui_typebox": (4, 4, 4, 4), "ui_typebox_hot": (4, 4, 4, 4),
    "ui_typebox_bad": (4, 4, 4, 4), "ui_typebox_ok": (4, 4, 4, 4),
    "ui_timer_bg": (2, 2, 2, 2), "ui_timer_fill": (1, 0, 1, 0), "ui_timer_fill_gold": (1, 0, 1, 0),
}
TEAL = _mix(B, L, 0.4)                                       # turkuaz (mavi + yeşil karışımı)
TEAL_LT = _mix(C, L, 0.2)


class ACanvas(Canvas):
    """Alfa destekli tuval: renk dörtlü ise dördüncü değer saydamlıktır (0-255)."""

    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for yy in range(self.h):
            for xx in range(self.w):
                c = self.px[yy][xx]
                if c is None:
                    continue
                a = c[3] if len(c) > 3 else 255
                if a > 0:
                    img.putpixel((xx, yy), (c[0], c[1], c[2], int(a)))
        img.save(os.path.join(OUT, name + ".png"))


def _a(col, alpha):
    return (col[0], col[1], col[2], int(alpha))


def _halo(c, col):
    """Dolu piksellerin dışına 1 piksellik hale; çapraz komşulara yarı saydamı."""
    src = [row[:] for row in c.px]
    for yy in range(c.h):
        for xx in range(c.w):
            if src[yy][xx] is not None:
                continue

            def filled(dirs):
                return any(0 <= xx + dx < c.w and 0 <= yy + dy < c.h and src[yy + dy][xx + dx] is not None
                           for dx, dy in dirs)
            if filled(((1, 0), (-1, 0), (0, 1), (0, -1))):
                c.px[yy][xx] = col
            elif len(col) > 3 and filled(((1, 1), (-1, 1), (1, -1), (-1, -1))):
                c.px[yy][xx] = _a(col, col[3] // 2)


def _fade(c, alpha):
    """Tüm pikselleri verilen saydamlık oranına indirir (sönme kareleri)."""
    for yy in range(c.h):
        for xx in range(c.w):
            p_ = c.px[yy][xx]
            if p_ is not None:
                a0 = p_[3] if len(p_) > 3 else 255
                c.px[yy][xx] = _a(p_, a0 * alpha / 255.0)


def _ring(c, cx, cy, rx, ry, th, col, keep=None):
    """Elips halka (kalınlık th piksel). keep(açı) False dönerse o kısım çizilmez (kesik halka, ön/arka yarı)."""
    irx, iry = rx - th, ry - th
    for yy in range(c.h):
        for xx in range(c.w):
            dx, dy = xx - cx, yy - cy
            if (dx / rx) ** 2 + (dy / ry) ** 2 > 1.0:
                continue
            if irx > 0 and iry > 0 and (dx / irx) ** 2 + (dy / iry) ** 2 <= 1.0:
                continue
            if keep is not None and not keep(math.atan2(dy / ry, dx / rx)):
                continue
            c.px[yy][xx] = col


def _plus(c, x, y_, core, arm):
    c.set(x, y_, core)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        c.set(x + dx, y_ + dy, arm)


def _k_outline(c):
    """Kenar çizgisi (Canvas.outline gibi 4 komşulu, köşeler boş kalır) ama saldırı pozu kancası olmadan."""
    _halo(c, K)


def make_ui_typebox(rim=None, glow=None):
    """50x20 yazı kutusu (store art make_textbox'ın kutusu, 1 px saydam payla).
    rim: hattın hemen içindeki vurgu halkası (sıra sende / kabul / hata), glow: pay alanındaki dış parıltı."""
    c = ACanvas(50, 20)
    x0, y0, x1, y1 = 2, 2, 47, 17                                 # beyaz yüz (store: rect(1, 1, wd, ht, W))
    c.rect(x0, y0, x1, y1, W)
    c.rect(x0, y1 - 1, x1, y1, w)                                 # alt 2 satır derinlik (store: w)
    c.rect(x0 + 1, y0 + 1, x1 - 1, y0 + 1, C)                     # üstte açık mavi parıltı (store: rect(2, 2, wd-1, 2, C))
    if rim is not None:
        main, top, bot = rim
        c.rect(x0, y0, x1, y0, top)
        c.rect(x0, y0, x0, y1, main)
        c.rect(x1, y0, x1, y1, main)
        c.rect(x0, y1, x1, y1, bot)
    _k_outline(c)                                                 # ince koyu dış hat, köşeler boş
    if glow is not None:
        _halo(c, glow)
    return c


def make_ui_typebox_hot():
    return make_ui_typebox(rim=(Y, _mix(Y, W, 0.55), y), glow=_a(Y, 170))


def make_ui_typebox_bad():
    return make_ui_typebox(rim=(R, _mix(R, PINK, 0.6), r), glow=_a(R, 150))


def make_ui_typebox_ok():
    return make_ui_typebox(rim=(L, _mix(L, W, 0.4), l), glow=_a(L, 150))


def make_ui_timer_bg():
    """40x6 sayaç yuvası (store: kutu altında INK çubuk + K hat, köşeler boş)."""
    c = ACanvas(40, 6)
    c.rect(1, 1, 38, 4, INK)
    _k_outline(c)
    return c


def make_ui_timer_fill():
    """8x2 dolgu, yuvanın içine 1 px içeriden oturur (store: 2 satır). Beyaz: kod renklendirir; alt satır biraz koyu."""
    c = ACanvas(8, 2)
    c.rect(0, 0, 7, 0, W)
    c.rect(0, 1, 7, 1, _mix(W, w, 0.6))
    return c


def make_ui_timer_fill_gold():
    """8x2 dolgu, store art renkleriyle birebir: üst satır Y, alt satır O (renklendirmeden kullanmak için)."""
    c = ACanvas(8, 2)
    c.rect(0, 0, 7, 0, Y)
    c.rect(0, 1, 7, 1, O)
    return c


def make_ui_caret(frame):
    """2x8 imleç (store: 2 genişlik, kutu yüksekliğinin yarısı): 0 = tam siyah, 1 = sönük."""
    c = ACanvas(2, 8)
    c.rect(0, 0, 1, 7, K if frame == 0 else _a(INK, 110))
    return c


_PIPS = {1: [(0, 0)], 2: [(-1, -1), (1, 1)], 3: [(-1, -1), (0, 0), (1, 1)],
         4: [(-1, -1), (1, -1), (-1, 1), (1, 1)], 5: [(-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1)],
         6: [(-1, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1)]}


def make_ui_die(face):
    """12x13 zar (store art make_die, size=10): beyaz yüz, altta w/g kalınlık, 2x2 kırmızı noktalar (sağ altı koyu)."""
    c = ACanvas(12, 13)
    s_ = 10
    c.rect(1, 1, s_, s_, W)
    c.rect(1, s_ - 1, s_, s_ + 1, w)                              # alt yüzey (kalınlık)
    c.rect(2, s_, s_ - 1, s_ + 1, g)
    c.rect(1, s_, 1, s_, w); c.rect(s_, s_, s_, s_, w)
    for xx, yy in ((1, 1), (s_, 1), (1, s_ + 1), (s_, s_ + 1)):
        c.px[yy][xx] = None                                       # yuvarlak köşe
    cxy, step = (s_ + 1) / 2.0, (s_ - 4) / 3.0                  # store art make_die nokta yerleşimi
    for dx, dy in _PIPS[face]:
        px_ = int(round(cxy + dx * step - 0.5))
        py_ = int(round(cxy - 1 + dy * step - 0.5))
        if face == 6:
            py_ = 4 + dy * 3                                      # 6'da sütunlar bitişmesin: 1 px aralıklı satırlar
        c.rect(px_, py_, px_ + 1, py_ + 1, R)
        c.set(px_ + 1, py_ + 1, r)
    _k_outline(c)
    return c


def make_ui_die_roll(frame):
    """Dönen zar 12x13: yana yatmış / köşesi üstünde / takla atarken basık / ters yatmış (4 kare), bulanık iz ile."""
    c = ACanvas(12, 13)
    cx, cy = 5.5, 5.5

    def square(ang, hs, col):
        pts = []
        for k in range(4):
            a = ang + math.pi / 4 + k * math.pi / 2
            pts.append((cx + 0.5 + hs * math.sqrt(2) * math.cos(a), cy + 0.5 + hs * math.sqrt(2) * math.sin(a)))
        c.poly(pts, col)

    def dot(ang, dx, dy, col=R):
        ca, sa = math.cos(ang), math.sin(ang)
        c.set(int(round(cx + dx * ca - dy * sa)), int(round(cy + dx * sa + dy * ca)), col)

    if frame == 2:                                                # takla: üst yüz basık, ön yüz görünür
        c.rect(1, 3, 10, 5, W)
        c.rect(1, 6, 10, 9, w)
        c.rect(2, 8, 9, 9, g)
        for xx in (3, 7):
            c.rect(xx, 4, xx + 1, 4, R)
        c.rect(5, 7, 6, 7, r)
        for xx, yy in ((1, 3), (10, 3), (1, 9), (10, 9)):
            c.px[yy][xx] = None
        _k_outline(c)
        for xx in range(1, 11, 3):                                # dikey hız izi
            c.set(xx, 0, _a(w, 120)); c.set(xx + 1, 12, _a(w, 120))
        return c
    ang, hs, face = {0: (math.radians(20), 3.6, 3), 1: (math.radians(45), 3.4, 5), 3: (math.radians(-25), 3.6, 2)}[frame]
    square(ang, hs, W)
    pips = {3: [(-1.9, -1.9), (0, 0), (1.9, 1.9)], 5: [(-1.9, -1.9), (1.9, -1.9), (0, 0), (-1.9, 1.9), (1.9, 1.9)],
            2: [(-1.9, -1.9), (1.9, 1.9)]}[face]
    for dx, dy in pips:
        dot(ang, dx, dy)
    for xx in range(12):                                          # alt kenarda kalınlık
        for yy in range(11, 0, -1):
            if c.px[yy][xx] is not None:
                if c.px[yy - 1][xx] is not None:
                    c.set(xx, yy + 1, w)
                break
    _k_outline(c)
    trail = _a(w, 130)                                            # dönüş yönünde bulanık iz
    src = [row[:] for row in c.px]
    for yy in range(13):
        for xx in range(12):
            if src[yy][xx] is None and (xx + yy) % 2 == 0:
                if (xx + 1 < 12 and src[yy][xx + 1] == K) or (yy > 0 and src[yy - 1][xx] == K):
                    c.px[yy][xx] = trail
    return c


def make_ui_key_spark(frame):
    """8x8 tuş kıvılcımı: merkezden açılır, köşelere savrulup söner (beyaz-sarı)."""
    c = ACanvas(8, 8)
    if frame == 0:
        c.rect(3, 3, 4, 4, W)
        for xx, yy in ((3, 2), (4, 2), (3, 5), (4, 5), (2, 3), (2, 4), (5, 3), (5, 4)):
            c.set(xx, yy, Y)
    elif frame == 1:
        c.rect(3, 3, 4, 4, W)
        for k in (1, 2):
            for xx, yy in ((3, 3 - k), (4, 4 + k), (3 - k, 4), (4 + k, 3)):
                c.set(xx, yy, Y if k == 1 else W)
        for xx, yy in ((1, 1), (6, 1), (1, 6), (6, 6)):
            c.set(xx, yy, Y)
    elif frame == 2:
        for xx, yy in ((3, 0), (7, 3), (4, 7), (0, 4)):
            c.set(xx, yy, W)
        for xx, yy in ((0, 0), (7, 0), (0, 7), (7, 7)):
            c.set(xx, yy, Y)
        for xx, yy in ((1, 1), (6, 1), (1, 6), (6, 6)):
            c.set(xx, yy, _a(y, 200))
    else:
        for xx, yy in ((0, 0), (7, 0), (0, 7), (7, 7)):
            c.set(xx, yy, _a(y, 140))
        for xx, yy in ((3, 0), (7, 3), (4, 7), (0, 4)):
            c.set(xx, yy, _a(Y, 90))
    return c


# ================= Efekt: kelime büyüsü =================
# Kelime yazılıp kabul edildiğinde oynayan düz sprite'lar (voxel yok, saydam arka plan).
_RUNES = [((0, 0), (1, 0), (1, 1)), ((0, 1), (1, 0), (2, 1)), ((0, 0), (0, 1), (1, 1)),
          ((0, 0), (2, 0), (1, 1)), ((1, 0), (0, 1), (1, 1)), ((0, 0), (1, 1), (2, 0))]


def _runes(c, cx, cy, rx, ry, count, rot, col, hi):
    for i in range(count):
        a = rot + 2 * math.pi * i / count
        bx, by = int(round(cx + rx * math.cos(a))) - 1, int(round(cy + ry * math.sin(a)))
        glyph = _RUNES[i % len(_RUNES)]
        for j, (gx, gy) in enumerate(glyph):
            c.set(bx + gx, by + gy, hi if j == 0 else col)


def make_fx_summon(frame):
    """24x24 büyü çemberi: yerde halka açılır, rünler döner, ışık sütunu yükselir, söner (mor-turkuaz-beyaz)."""
    c = ACanvas(24, 24)
    cx, cy = 11.5, 17.5
    front = (lambda a: math.sin(a) >= 0)
    back = (lambda a: math.sin(a) < 0)
    if frame == 0:
        c.ellipse(cx, cy, 3.0, 1.4, _a(P, 120))
        _ring(c, cx, cy, 4.0, 2.0, 1.0, P)
        c.rect(11, 17, 12, 17, W)
        for xx, yy in ((9, 13), (14, 12)):
            c.set(xx, yy, _a(C, 200))
        return c
    if frame == 1:
        c.ellipse(cx, cy, 7.0, 3.2, _a(p, 130))
        _ring(c, cx, cy, 8.0, 3.8, 1.1, P)
        _ring(c, cx, cy, 5.5, 2.5, 1.0, TEAL, keep=lambda a: int((a + math.pi) / (math.pi / 4)) % 2 == 0)
        c.ellipse(cx, cy, 2.0, 0.9, _a(C, 220))
        c.rect(11, 17, 12, 17, W)
        _runes(c, cx, cy - 0.5, 6.8, 3.0, 4, 0.3, TEAL_LT, W)
        for xx, yy in ((6, 12), (16, 11), (11, 9)):
            _plus(c, xx, yy, W, _a(C, 150))
        return c
    rot = {2: 0.0, 3: 0.52, 4: 1.05, 5: 1.4}[frame]
    disc_a = {2: 150, 3: 160, 4: 130, 5: 70}[frame]
    c.ellipse(cx, cy, 11.0, 5.2, _a(p, disc_a))
    if frame == 5:
        _ring(c, cx, cy, 11.5, 5.6, 1.0, _a(P, 170), keep=lambda a: int((a + math.pi) / (math.pi / 6)) % 2 == 0)
        _ring(c, cx, cy, 8.0, 3.6, 1.0, _a(TEAL, 110), keep=lambda a: int((a + math.pi + rot) / (math.pi / 4)) % 2 == 0)
        for xx, yy, col in ((5, 9, _a(C, 170)), (17, 6, _a(TEAL_LT, 150)), (11, 3, _a(W, 140)), (14, 11, _a(C, 120))):
            c.set(xx, yy, col)
        return c
    dash = (lambda a, r0=rot: int((a + math.pi + r0) / (math.pi / 5)) % 2 == 0)
    # arka yarı (sütunun arkasında kalır)
    _ring(c, cx, cy, 11.5, 5.6, 1.3, P, keep=back)
    _ring(c, cx, cy, 8.3, 3.8, 1.0, TEAL, keep=lambda a: back(a) and dash(a))
    _runes(c, cx, cy - 0.5, 10.0, 4.6, 6, rot, TEAL_LT, W)
    if frame in (3, 4):                                           # ışık sütunu
        half = 3 if frame == 3 else 2
        for yy in range(0, 18):
            fade = min(1.0, (yy + 1) / 5.0)
            for xx in range(12 - half - 1, 12 + half + 1):
                d = abs(xx + 0.5 - 12)
                if d > half + 0.5:
                    continue
                if d < 1.0:
                    col = _a(W, 235 * fade)
                elif d < half - 0.4:
                    col = _a(C, 200 * fade)
                else:
                    col = _a(TEAL_LT, 140 * fade)
                if frame == 4:
                    col = _a(col, col[3] * 0.7)
                c.set(xx, yy, col)
        c.ellipse(cx, cy, half + 2.0, 1.6, _a(W, 230))
    else:
        c.ellipse(cx, cy, 3.0, 1.3, _a(C, 230))
        c.rect(11, 17, 12, 17, W)
    # ön yarı
    _ring(c, cx, cy, 11.5, 5.6, 1.3, P, keep=front)
    _ring(c, cx, cy, 11.5, 5.6, 0.6, _mix(P, PINK, 0.5), keep=lambda a: front(a) and abs(math.cos(a)) < 0.5)
    _ring(c, cx, cy, 8.3, 3.8, 1.0, TEAL, keep=lambda a: front(a) and dash(a))
    sparks = {2: ((4, 10), (19, 9), (11, 6)), 3: ((5, 7), (18, 5), (8, 2), (16, 13)),
              4: ((6, 3), (18, 1), (4, 12), (19, 10))}[frame]
    for xx, yy in sparks:
        _plus(c, xx, yy, W, _a(TEAL_LT, 170))
    return c


def make_fx_letter_trail(frame):
    """6x6 uçan harf izi: beyaz çekirdekli sarı parıltı, küçülüp söner."""
    c = ACanvas(6, 6)
    if frame == 0:
        for xx, yy in ((2, 1), (3, 1), (1, 2), (1, 3), (4, 2), (4, 3), (2, 4), (3, 4)):
            c.set(xx, yy, Y)
        c.rect(2, 2, 3, 3, W)
        for xx, yy in ((2, 0), (5, 2), (3, 5), (0, 3)):
            c.set(xx, yy, _a(Y, 120))
    elif frame == 1:
        c.rect(2, 2, 3, 3, Y)
        c.set(2, 2, W)
        for xx, yy in ((2, 1), (4, 2), (3, 4), (1, 3)):
            c.set(xx, yy, _a(Y, 150))
    elif frame == 2:
        c.set(2, 2, Y); c.set(3, 3, _a(y, 200))
        c.set(3, 2, _a(Y, 110)); c.set(2, 3, _a(Y, 110))
    else:
        c.set(3, 3, _a(y, 120))
    return c


def make_fx_word_burst(frame):
    """20x20 kelime kabul patlaması: altın-beyaz yıldız açılır, ışınlara ve kıvılcımlara dağılır."""
    c = ACanvas(20, 20)
    cx = cy = 9.5
    if frame == 0:
        _star(c, cx + 0.5, cy + 0.5, 4.5, 1.8, 4, Y, 0.0)
        c.ellipse(cx, cy, 1.4, 1.4, W)
    elif frame == 1:
        _star(c, cx + 0.5, cy + 0.5, 9.2, 3.8, 8, y, 0.2)
        _star(c, cx + 0.5, cy + 0.5, 7.4, 3.0, 8, Y, 0.2)
        c.ellipse(cx, cy, 2.4, 2.4, W)
    elif frame == 2:
        _star(c, cx + 0.5, cy + 0.5, 9.8, 4.6, 8, y, 0.6)
        _star(c, cx + 0.5, cy + 0.5, 8.4, 3.8, 8, Y, 0.6)
        c.erase_ellipse(cx, cy, 3.2, 3.2)
        _ring(c, cx, cy, 4.2, 4.2, 1.0, W)
    elif frame == 3:
        for i in range(8):
            a = 0.6 + 2 * math.pi * i / 8
            for rr in (5.5, 6.5, 7.5, 8.5):
                col = W if rr > 8 else (Y if rr > 6 else _a(y, 200))
                c.set(int(round(cx + rr * math.cos(a))), int(round(cy + rr * math.sin(a))), col)
        for xx, yy in ((4, 4), (15, 5), (5, 15), (14, 14)):
            _plus(c, xx, yy, W, Y)
    else:
        for i in range(8):
            a = 0.6 + 2 * math.pi * i / 8
            c.set(int(round(cx + 9.2 * math.cos(a))), int(round(cy + 9.2 * math.sin(a))), _a(Y, 170))
        for xx, yy in ((3, 3), (16, 4), (4, 16), (15, 15)):
            _plus(c, xx, yy, _a(W, 200), _a(y, 110))
    return c


_POOF = {
    0: [(7.5, 9.0, 2.6), (5.2, 10.2, 1.9), (9.8, 10.2, 1.9)],
    1: [(7.5, 7.5, 3.6), (4.5, 9.5, 2.8), (10.5, 9.5, 2.8), (7.5, 11.0, 2.6)],
    2: [(7.5, 6.2, 4.0), (3.6, 9.0, 3.1), (11.4, 9.0, 3.1), (5.8, 12.0, 2.6), (9.4, 12.0, 2.6)],
    3: [(3.2, 3.8, 2.3), (11.8, 3.6, 2.3), (2.6, 11.4, 2.0), (12.6, 11.6, 2.0), (7.5, 7.5, 1.8)],
    4: [(2.0, 2.0, 1.3), (13.0, 1.6, 1.3), (1.4, 13.0, 1.2), (13.8, 13.4, 1.2), (7.5, 4.0, 1.0)],
}


def make_fx_poof(frame):
    """16x16 duman pufu: bulut toplar şişer, dağılıp söner (gri-beyaz, gri kenar)."""
    c = ACanvas(16, 16)
    blobs = _POOF[frame]
    for bx, by, br in blobs:
        c.ellipse(bx, by, br, br, w)
    src = [row[:] for row in c.px]
    for yy in range(15):                                          # alt kenar gölgesi
        for xx in range(16):
            if src[yy][xx] is not None and src[yy + 1][xx] is None:
                c.px[yy][xx] = g
    for bx, by, br in blobs:
        if br > 1.5:
            c.ellipse(bx - 0.6, by - 0.8, br * 0.55, br * 0.55, W)
    if frame == 2:
        c.erase_ellipse(7.5, 9.5, 1.3, 1.0)                      # ortası açılmaya başlar
    if frame <= 3:
        _halo(c, G)
    if frame == 3:
        _fade(c, 190)
    elif frame == 4:
        _fade(c, 130)
    return c


def make_fx_threat_warn(frame):
    """16x16 kırmızı uyarı üçgeni, dikey eksende dönüyor (tam -> yan -> kenar -> yan) ve nabız gibi parlıyor."""
    c = ACanvas(16, 16)
    sx = (1.0, 0.75, 0.22, 0.75)[frame]
    mx = 8.0
    c.poly([(mx, 0.6), (mx + 7.2 * sx, 14.0), (mx - 7.2 * sx, 14.0)], R)
    if sx > 0.4:
        c.poly([(mx, 3.6), (mx + 5.0 * sx, 12.6), (mx - 5.0 * sx, 12.6)], Y)
        c.rect(7, 5, 8, 8, K)                                     # ünlem
        c.rect(7, 10, 8, 11, K)
        if frame != 0:
            side = 1 if frame == 1 else -1                        # dönerken karanlıkta kalan yarı
            for yy in range(16):
                for xx in range(16):
                    if c.px[yy][xx] == R and (xx + 0.5 - mx) * side > 0:
                        c.px[yy][xx] = r
    else:
        for yy in range(16):
            for xx in range(16):
                if c.px[yy][xx] == R and xx + 0.5 > mx:
                    c.px[yy][xx] = r
    for xx in range(16):                                          # taban gölgesi
        if c.px[13][xx] == R:
            c.px[13][xx] = r
    _k_outline(c)
    if frame == 0:
        _halo(c, _a(R, 170))
    elif frame == 3:
        _halo(c, _a(O, 110))
    return c


def typebox_fx_sprites():
    """Yazı kutusu arayüzü ve kelime büyüsü efektleri: ad -> çizim fonksiyonu."""
    out = {"ui_typebox": make_ui_typebox, "ui_typebox_hot": make_ui_typebox_hot,
           "ui_typebox_bad": make_ui_typebox_bad, "ui_typebox_ok": make_ui_typebox_ok,
           "ui_timer_bg": make_ui_timer_bg, "ui_timer_fill": make_ui_timer_fill,
           "ui_timer_fill_gold": make_ui_timer_fill_gold}
    for i in range(2):
        out["ui_caret_%d" % i] = (lambda f=i: make_ui_caret(f))
    for i in range(1, 7):
        out["ui_die_%d" % i] = (lambda f=i: make_ui_die(f))
    for i in range(4):
        out["ui_die_roll_%d" % i] = (lambda f=i: make_ui_die_roll(f))
        out["ui_key_spark_%d" % i] = (lambda f=i: make_ui_key_spark(f))
        out["fx_letter_trail_%d" % i] = (lambda f=i: make_fx_letter_trail(f))
        out["fx_threat_warn_%d" % i] = (lambda f=i: make_fx_threat_warn(f))
    for i in range(6):
        out["fx_summon_%d" % i] = (lambda f=i: make_fx_summon(f))
    for i in range(5):
        out["fx_word_burst_%d" % i] = (lambda f=i: make_fx_word_burst(f))
        out["fx_poof_%d" % i] = (lambda f=i: make_fx_poof(f))
    return out



# ================= Arayüz: HUD (can, itiş, durumlar) =================
# Sol üstteki düz yazılar yerine simgeler. Tümü düz sprite, ince K kenar; panel ve tuş 9-slice (UI_BORDERS).
UI_BORDERS.update({"ui_hud_panel": (4, 4, 4, 4), "ui_key_space": (3, 4, 3, 2)})
HUD_BG = _mix(INK, p, 0.25)                                  # store art koyu mor-INK tonu


def _heart_fill():
    """12x11 kalp gövdesi (kenar çizgisiz): iki lob + sivri alt."""
    c = ACanvas(12, 11)
    c.ellipse(3.5, 3.3, 2.6, 2.4, R)
    c.ellipse(7.5, 3.3, 2.6, 2.4, R)
    c.poly([(1.0, 3.8), (11.0, 3.8), (6.0, 9.6)], R)
    return c


def _heart_shade(c, body, dark, hi, hi2):
    for yy in range(c.h):
        for xx in range(c.w):
            if c.px[yy][xx] is None:
                continue
            below = yy + 1 >= c.h or c.px[yy + 1][xx] is None
            right = xx + 1 >= c.w or c.px[yy][xx + 1] is None
            c.px[yy][xx] = dark if (below or right) and xx > 3 else body
    c.set(2, 2, hi); c.set(3, 2, hi2); c.set(2, 3, hi2)


def make_ui_heart(kind):
    """Kalp: full = parlak kırmızı, empty = koyu gri, half = sol yarı dolu."""
    c = _heart_fill()
    if kind == "full":
        _heart_shade(c, R, r, W, PINK)
    else:
        _heart_shade(c, INK, _mix(INK, K, 0.5), G, G)
        if kind == "half":
            full = _heart_fill()
            _heart_shade(full, R, r, W, PINK)
            for yy in range(c.h):
                for xx in range(6):
                    if full.px[yy][xx] is not None:
                        c.px[yy][xx] = full.px[yy][xx]
    _k_outline(c)
    return c


_CRACK = [6, 6, 5, 6, 7, 6, 5, 6, 6, 6, 6]                     # her satırda kırığın x'i (sol yarı x < değer)


def make_ui_heart_break(frame):
    """Kalp kırılır: çatlak + parlama, iki yarı ayrılır, düşer, söner (4 kare, 12x11)."""
    base = _heart_fill()
    _heart_shade(base, R, r, W, PINK)
    c = ACanvas(12, 11)
    if frame == 0:
        c.px = [row[:] for row in base.px]
        for yy, cx_ in enumerate(_CRACK):
            if c.px[yy][cx_] is not None:
                c.px[yy][cx_] = W if yy < 4 else K
        _k_outline(c)
        return c
    dx, dy, rot_drop = {1: (1, 0, 1), 2: (2, 1, 2), 3: (2, 2, 2)}[frame]
    for yy in range(11):
        for xx in range(12):
            col = base.px[yy][xx]
            if col is None:
                continue
            left = xx < _CRACK[yy]
            if xx == _CRACK[yy]:
                continue
            # yarılar dışa ve aşağı kayar; dış kenarları biraz daha düşer (devrilme hissi)
            ox = -dx if left else dx
            oy = dy + (rot_drop if (left and xx < 3) or (not left and xx > 8) else 0) // 2
            c.set(xx + ox, yy + oy, col)
    # küçük parçalar
    shards = {1: ((6, 0, W), (5, 10, R)), 2: ((6, 1, R), (1, 1, PINK), (10, 0, R)), 3: ((6, 3, r), (0, 2, r))}[frame]
    for xx, yy, col in shards:
        c.set(xx, yy, col)
    _k_outline(c)
    if frame == 3:
        _fade(c, 120)
    return c


def make_ui_hud_panel():
    """16x16 yarı saydam koyu panel: K hat, mor-INK kenar halkası (üstü açık, altı koyu), köşelerde altın perçin."""
    c = ACanvas(16, 16)
    c.rect(1, 1, 14, 14, _mix(HUD_BG, G, 0.25))
    c.rect(2, 1, 13, 1, _mix(HUD_BG, g, 0.35))
    c.rect(1, 14, 14, 14, _mix(HUD_BG, K, 0.5))
    c.rect(14, 2, 14, 13, _mix(HUD_BG, K, 0.3))
    c.rect(2, 2, 13, 13, _a(HUD_BG, 185))
    for xx, yy in ((2, 2), (13, 2), (2, 13), (13, 13)):
        c.set(xx, yy, y)
    _k_outline(c)
    return c


def make_ui_icon_shove(gray=False):
    """12x12 itiş simgesi: iki kalın sağ ok (>>). gray=True: bekleme süresindeki gri sürüm."""
    c = ACanvas(12, 12)
    body, top, dark = (g, w, G) if gray else (O, Y, r)
    for x0 in (1.0, 5.0):
        c.poly([(x0, 1.5), (x0 + 2.6, 1.5), (x0 + 5.8, 6.0), (x0 + 2.6, 10.5), (x0, 10.5), (x0 + 3.2, 6.0)], body)
    for yy in range(12):
        for xx in range(12):
            if c.px[yy][xx] is not None:
                if yy < 5:
                    c.px[yy][xx] = top
                elif yy > 7:
                    c.px[yy][xx] = dark
    _k_outline(c)
    return c


def make_ui_status(kind):
    """10x10 durum simgeleri; içerik 1..8 piksel, ince K kenar."""
    c = ACanvas(10, 10)
    if kind == "shield":
        c.poly([(1, 1), (9, 1), (9, 5.2), (5, 9.2), (1, 5.2)], B)
        c.poly([(1, 1), (5, 1), (5, 9.2), (1, 5.2)], C)
        c.rect(2, 2, 2, 4, W)
        c.rect(5, 1, 8, 1, b)
    elif kind == "stealth":
        c.ellipse(4.5, 5.0, 4.2, 2.8, W)                           # yarı kapalı göz
        c.ellipse(4.5, 5.4, 1.7, 1.9, p)
        c.rect(4, 5, 5, 6, K)
        for xx in range(10):
            for yy in range(0, 5):
                if c.px[yy][xx] is not None:
                    c.px[yy][xx] = g
        c.rect(1, 4, 8, 4, G)
    elif kind == "slow":
        c.ellipse(5.0, 4.0, 3.2, 3.2, s)
        c.ellipse(5.0, 4.0, 1.6, 1.6, N)
        c.set(5, 4, s)
        c.rect(1, 7, 8, 8, Y)
        c.rect(1, 8, 8, 8, y)
        c.rect(1, 5, 2, 6, Y)
        c.set(1, 3, Y)
    elif kind == "burn":
        c.poly([(5, 0.5), (8.6, 5.0), (8.0, 8.4), (5, 9.4), (2.0, 8.4), (1.4, 5.0), (3.0, 3.6), (3.6, 5.0)], O)
        c.poly([(5.2, 3.5), (7.0, 6.5), (6.4, 8.6), (3.6, 8.6), (3.2, 6.8)], Y)
        c.rect(4, 7, 5, 8, W)
        c.set(8, 6, r); c.set(7, 8, r)
    elif kind == "fly":
        for i, (x0, y0, ln) in enumerate(((1, 2, 7), (1, 4, 6), (1, 6, 4))):
            c.rect(x0, y0, x0 + ln, y0 + 1, W if i == 0 else w)
        c.rect(1, 2, 1, 8, w)
        c.rect(1, 7, 3, 8, g)
        c.set(8, 2, w); c.set(7, 4, g); c.set(5, 6, g)
    elif kind == "disguise":
        c.poly([(0.6, 3.0), (9.4, 3.0), (9.0, 6.4), (6.0, 7.2), (5.0, 6.0), (4.0, 7.2), (1.0, 6.4)], P)
        c.rect(1, 3, 8, 3, PINK)
        c.rect(2, 4, 3, 5, None)
        c.rect(6, 4, 7, 5, None)
        c.set(4, 6, p); c.set(5, 6, p)
    elif kind == "weapon":
        for i in range(6):                                         # 2 px kalın çapraz bıçak
            c.set(3 + i, 6 - i, W); c.set(4 + i, 6 - i, w)
        c.set(8, 1, W); c.set(9, 0, None)
        c.rect(1, 5, 3, 5, y); c.rect(3, 6, 3, 8, y)                # balçak
        c.rect(1, 7, 2, 8, N)                                      # kabza
    elif kind == "vehicle":
        c.ellipse(4.5, 4.5, 4.0, 4.0, INK)
        c.ellipse(4.5, 4.5, 2.4, 2.4, g)
        c.ellipse(4.5, 4.5, 1.0, 1.0, W)
        c.set(2, 2, G); c.set(7, 7, G); c.set(7, 2, G); c.set(2, 7, G)
        c.rect(2, 0, 7, 0, None)
        c.set(3, 1, G); c.set(6, 1, G)
    elif kind == "pull":
        c.rect(1, 1, 3, 6, R); c.rect(6, 1, 8, 6, R)               # mıknatıs U
        c.rect(1, 6, 8, 8, R)
        c.rect(4, 7, 5, 8, r)
        c.erase_rect(4, 1, 5, 6)
        c.rect(1, 1, 3, 2, W); c.rect(6, 1, 8, 2, W)               # uçlar
        c.rect(1, 8, 8, 8, r)
    _k_outline(c)
    return c


def make_ui_key_space():
    """24x9 boşluk tuşu kapağı (metinsiz; kod üstüne yazar): açık yüz, altta 2 satır kalınlık."""
    c = ACanvas(24, 9)
    c.rect(1, 1, 22, 5, _mix(W, w, 0.35))
    c.rect(2, 1, 21, 1, W)
    c.rect(1, 6, 22, 6, g)
    c.rect(1, 7, 22, 7, G)
    c.rect(1, 5, 22, 5, _mix(W, w, 0.7))
    for xx, yy in ((1, 1), (22, 1), (1, 7), (22, 7)):
        c.px[yy][xx] = None
    _k_outline(c)
    return c


STATUS_KINDS = ("shield", "stealth", "slow", "burn", "fly", "disguise", "weapon", "vehicle", "pull")


def hud_sprites():
    """HUD simgeleri: ad -> çizim fonksiyonu."""
    out = {"ui_heart_full": lambda: make_ui_heart("full"), "ui_heart_empty": lambda: make_ui_heart("empty"),
           "ui_heart_half": lambda: make_ui_heart("half"), "ui_hud_panel": make_ui_hud_panel,
           "ui_icon_shove": make_ui_icon_shove, "ui_icon_shove_cd": lambda: make_ui_icon_shove(True),
           "ui_key_space": make_ui_key_space}
    for i in range(4):
        out["ui_heart_break_%d" % i] = (lambda f=i: make_ui_heart_break(f))
    for k_ in STATUS_KINDS:
        out["ui_status_" + k_] = (lambda k2=k_: make_ui_status(k2))
    return out


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



def make_kizak_stack():
    """Yukarıdan kızak (burnu sağda): iki kızak demiri, tahta oturak, kıvrık ön. 3 dilim."""
    out = []
    c = Canvas(); c.rect(1, 5, 13, 5, g); c.rect(1, 10, 13, 10, g); c.set(14, 6, g); c.set(14, 9, g); out.append(c)
    c = Canvas(); c.rect(2, 5, 12, 10, N); c.rect(2, 5, 12, 5, n); c.rect(2, 10, 12, 10, n); out.append(c)
    c = Canvas(); c.rect(3, 6, 11, 9, N)
    for x in range(4, 12, 2): c.rect(x, 6, x, 9, n)
    out.append(c)
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
        "tilki": make_tilki, "baykus": make_baykus, "sincap": make_sincap, "yengec": make_yengec, "deve": make_deve, "kanguru": make_kanguru, "panda": make_panda, "kirpi": make_kirpi, "viking": make_viking, "gladyator": make_gladyator, "sumocu": make_sumocu, "boksor": make_boksor, "futbolcu": make_futbolcu, "asci": make_asci,
        "minotor": make_minotor, "medusa": make_medusa, "golem": make_golem, "gargoyle": make_gargoyle, "fil": make_fil, "mamut": make_mamut, "cakal": make_cakal, "sirtlan": make_sirtlan, "penguen": make_penguen, "sumuklu": make_sumuklu, "solucan": make_solucan, "denizanasi": make_denizanasi, "pterodaktil": make_pterodaktil, "hirsiz": make_hirsiz,
        "maymun": make_maymun, "papagan": make_papagan, "su_aygiri": make_su_aygiri, "peri": make_peri, "kovboy": make_kovboy,
    }
    static = {
        "yumruk": make_yumruk, "muz_kabugu": make_muz, "miknatis": make_miknatis, "bomba": make_bomba, "mermi": make_mermi, "ors": make_ors, "piyano": make_piyano, "saksi": make_saksi, "top": make_top, "bowling": make_bowling, "motor": make_motor, "araba": make_araba, "cali": make_cali, "kilic": make_kilic, "sopa": make_sopa,
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
        # Karlı dağ teması
        "t_snow": lambda: make_t_snow(0), "t_snow_alt": lambda: make_t_snow(1), "t_snow_path": make_t_snow_path,
        "t_stone": make_t_stone, "t_stonewall": make_t_stonewall, "t_ice": make_t_ice,
        "t_water": lambda: make_t_water(0), "t_water_b": lambda: make_t_water(1),
        "t_roof_snow_a": lambda: make_t_roof_snow("a"), "t_roof_snow_b": lambda: make_t_roof_snow("b"),
        "t_roof_snow_ridge": make_t_roof_snow_ridge, "t_roof_snow_eave": make_t_roof_snow_eave,
        # Çöl kasabası teması
        "t_sand": lambda: make_t_sand(0), "t_sand_alt": lambda: make_t_sand(1), "t_sand_path": make_t_sand_path,
        "t_adobe": make_t_adobe, "t_adobewall": make_t_adobewall,
        "t_quicksand": lambda: make_t_quicksand(0), "t_quicksand_b": lambda: make_t_quicksand(1),
        "t_cactus": make_t_cactus,
        "t_roof_adobe_a": lambda: make_t_roof_adobe("a"), "t_roof_adobe_b": lambda: make_t_roof_adobe("b"),
        "t_roof_adobe_ridge": make_t_roof_adobe_ridge, "t_roof_adobe_eave": make_t_roof_adobe_eave,
        "karton_kutu": make_karton_kutu, "cop_kutusu": make_cop_kutusu,

        # Arayüz: koleksiyon defteri
        "ui_book_page": make_ui_book_page, "ui_card_frame": make_ui_card_frame,
        "ui_card_frame_seen": make_ui_card_frame_seen, "ui_card_frame_locked": make_ui_card_frame_locked,
        "ui_card_frame_locked_plain": lambda: make_ui_card_frame_locked(False),
        "ui_icon_book": make_ui_icon_book, "ui_icon_lock": make_ui_icon_lock,
    }
    # Arayüz: yazı kutusu + HUD, Efekt: kelime büyüsü
    static.update(typebox_fx_sprites())
    static.update(hud_sprites())
    # Ev girişleri: iki karelik kapı/paspas tek parça çizilir, sol/sağ olarak kaydedilir.
    for theme in ("koy", "kar", "col"):
        for kind, fn in (("door", make_t_door), ("doormat", make_t_doormat)):
            static["t_%s_%s_l" % (kind, theme)] = (lambda f=fn, t=theme: f(t)[0])
            static["t_%s_%s_r" % (kind, theme)] = (lambda f=fn, t=theme: f(t)[1])
        static["t_awning_" + theme] = (lambda t=theme: make_t_awning(t))
    for name, fn in animated.items():
        save_animated(fn(), name)
    # Saldırı kareleri: her yaratığın "<ad>_atk" pozu (karakter hariç).
    attack_frames = [name for name in animated if name != "char"]
    for name in attack_frames:
        draw_attack(animated[name], ATTACK_POSES.get(name, _lunge)).save(name + "_atk")
        draw_attack(animated[name], WINDUP_POSES.get(name, _recoil)).save(name + "_atk0")
    for name, fn in static.items():
        fn().save(name)
    # Vuruş izi efektleri: her tür 4 kare (fx_<tür>_0..3), düz sprite.
    for kind, fn in FX.items():
        for frame in range(4):
            fn(frame).save("fx_%s_%d" % (kind, frame))
    stacks = {"araba": make_araba_stack, "motor": make_motor_stack, "char": make_char_stack,
              "tank": make_tank_stack, "kamyon": make_kamyon_stack, "bisiklet": make_bisiklet_stack, "kaykay": make_kaykay_stack,
              "otobus": make_otobus_stack, "traktor": make_traktor_stack, "buldozer": make_buldozer_stack, "go_kart": make_go_kart_stack,
              "ambulans": make_ambulans_stack, "tren": make_tren_stack, "limuzin": make_limuzin_stack, "at_arabasi": make_at_arabasi_stack,
              "kizak": make_kizak_stack}
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
        save_stack(voxelize(draw_attack(fn, ATTACK_POSES.get(name, _lunge))), name + "_atk")
        save_stack(voxelize(draw_attack(fn, WINDUP_POSES.get(name, _recoil))), name + "_atk0")
        voxels += 4
    print("Üretildi:", len(animated) * 2 + len(attack_frames) * 2 + len(static) + len(FX) * 4, "PNG ->", OUT, "+", len(stacks) + voxels, "katmanlı nesne ->", STACK_OUT)


if __name__ == "__main__":
    main()
