"""
WordRoyale: Steam mağaza görselleri (logo, kapsüller, kütüphane, simge) kodla üretilir.

Her şey küçük çözünürlükte çizilir ve yalnızca tam sayı katla, en yakın komşu (NEAREST) ile büyütülür: bulanık ölçekleme yok.
Sahne parçaları oyunun kendi sprite'larıdır (Assets/Resources/Sprites/*.png); palet ve Canvas Tools/GeneratePixelArt.py'den gelir.
Yazı tipi: Assets/Resources/Fonts/PixelifySans.ttf (SIL OFL).

Çalıştırma (proje klasöründen):
    python Tools/GenerateStoreArt.py

Çıktı: Steam/Store/*.png (+ _preview.png). Assets/ altına hiçbir şey yazmaz.
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import GeneratePixelArt as gpa  # noqa: E402  (main() korumalı; import bir şey üretmez)
from GeneratePixelArt import (Canvas, K, W, w, g, G, R, r, O, Y, y, L, l, D, B, b, C, P, p,  # noqa: E402,F401
                              S, s, N, n, PINK, INK)

ROOT = gpa.ROOT
SPRITES = os.path.join(ROOT, "Assets", "Resources", "Sprites")
FONT = os.path.join(ROOT, "Assets", "Resources", "Fonts", "PixelifySans.ttf")
OUT = os.path.join(ROOT, "Steam", "Store")


# ================================================================ yardımcılar
def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def canvas_img(cv):
    img = Image.new("RGBA", (cv.w, cv.h), (0, 0, 0, 0))
    for yy in range(cv.h):
        for xx in range(cv.w):
            c = cv.px[yy][xx]
            if c is not None:
                img.putpixel((xx, yy), rgba(c))
    return img


def from_rows(rows, cmap):
    """Karakter dizisinden Canvas: '.' boş, diğerleri cmap'ten renk."""
    cv = Canvas(len(rows[0]), len(rows))
    for yy, row in enumerate(rows):
        for xx, ch in enumerate(row):
            if ch != ".":
                cv.set(xx, yy, cmap[ch])
    return cv


_cache = {}


def sprite(name):
    if name not in _cache:
        _cache[name] = Image.open(os.path.join(SPRITES, name + ".png")).convert("RGBA")
    return _cache[name]


def up(img, k):
    return img if k == 1 else img.resize((img.width * k, img.height * k), Image.NEAREST)


def tint(img, c):
    """Beyaz karakter sprite'ını oyundaki gibi renkle çarpar."""
    a = np.array(img).astype(np.float32)
    for i in range(3):
        a[..., i] = a[..., i] * c[i] / 255.0
    return Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA")


def shade_arr(a, f):
    a = a.copy()
    a[..., :3] = (a[..., :3].astype(np.float32) * f).clip(0, 255).astype(np.uint8)
    return a


def to_final(base, k, size):
    """Taban resmi k katına büyütür ve tam hedef boyuta (ortadan) kırpar."""
    big = up(base, k)
    ox = (big.width - size[0]) // 2
    oy = (big.height - size[1]) // 2
    return big.crop((ox, oy, ox + size[0], oy + size[1]))


# ---- maske işlemleri (numpy bool) ----
def shift(m, dx, dy):
    out = np.zeros_like(m)
    h, w_ = m.shape
    xs0, xs1 = max(0, -dx), min(w_, w_ - dx)
    ys0, ys1 = max(0, -dy), min(h, h - dy)
    out[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx] = m[ys0:ys1, xs0:xs1]
    return out


def dilate(m, rad, round_=True):
    out = m.copy()
    for dy in range(-rad, rad + 1):
        for dx in range(-rad, rad + 1):
            if round_ and rad > 1 and abs(dx) == rad and abs(dy) == rad:
                continue                    # köşeleri kırp: daha yumuşak dış hat
            out |= shift(m, dx, dy)
    return out


BAYER = np.array([[0, 2], [3, 1]]) / 4.0


def band_darken(a, y0, y1, lo, hi, bands=5):
    """y0..y1 arası satırları lo'dan hi'ye basamaklı (2x2 dither'lı) koyulaştırır. y0 tarafı lo."""
    h, w_ = a.shape[:2]
    out = a.copy()
    span = max(1, y1 - y0)
    xs = np.arange(w_)
    for yy in range(h):
        t = (yy - y0) / span
        t = min(1.0, max(0.0, t))
        lev = t * bands
        bnd = int(lev)
        frac = lev - bnd
        row_thr = BAYER[yy % 2][xs % 2]
        bsel = np.where(row_thr < frac, bnd + 1, bnd).clip(0, bands)
        f = lo + (hi - lo) * bsel / bands
        out[yy, :, :3] = (a[yy, :, :3].astype(np.float32) * f[:, None]).clip(0, 255).astype(np.uint8)
    return out


def vignette(a, strength=0.35, steps=4):
    """Kenarlara doğru basamaklı karartma."""
    h, w_ = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w_]
    dx = (xx - (w_ - 1) / 2) / (w_ / 2)
    dy = (yy - (h - 1) / 2) / (h / 2)
    d = np.clip(np.maximum(np.abs(dx), np.abs(dy)) ** 3, 0, 1)
    lev = d * steps
    bnd = np.floor(lev)
    frac = lev - bnd
    thr = BAYER[yy % 2, xx % 2]
    bnd = np.where(thr < frac, bnd + 1, bnd)
    f = 1.0 - strength * bnd / steps
    out = a.copy()
    out[..., :3] = (a[..., :3].astype(np.float32) * f[..., None]).clip(0, 255).astype(np.uint8)
    return out


# ================================================================ logo
LOGO_PAD = 4


# Logo harfleri elle çizildi (kalın: 3 piksel dikey / 2 piksel yatay çizgi). Satır 0-13 büyük harf yüksekliği,
# küçük harfler satır 4'ten başlar, satır 14-17 alt uzantı (y). Pixelify Sans bu boyutta çok ince kaldığı için logo onu kullanmaz.
GLYPHS = {
    "W": ["###.........###"] * 4 + ["###...###...###"] * 6 + [
          "###..#####..###",
          "###.###.###.###",
          ".#####...#####.",
          "..###.....###.."],
    "o": [".#########.", "###########"] + ["###.....###"] * 6 + ["###########", ".#########."],
    "r": ["###.#####", "#########", "#####....", "####....."] + ["###......"] * 6,
    "d": ["........###"] * 4 + [".##########", "###########"] + ["###.....###"] * 6 + ["###########", ".##########"],
    "R": ["##########.", "###########"] + ["###.....###"] * 3 + ["###########", "##########.",
          "###..###...", "###...###..", "###....###."] + ["###.....###"] * 4,
    "y": ["###.....###"] * 7 + ["###########", ".##########", "........###",
          "........###", "###########", ".#########."],
    "a": ["#########.", "##########", ".......###", ".#########", "##########"] + ["###....###"] * 3 + [
          "##########", ".#########"],
    "l": ["###."] * 12 + ["####", ".###"],
    "e": [".#########.", "###########", "###.....###", "###.....###", "###########", "###########",
          "###........", "###........", "###########", ".##########"],
}
GLYPH_TOP = {"W": 0, "d": 0, "R": 0, "l": 0}          # diğerleri küçük harf: satır 4'ten başlar
GLYPH_GAP = 2


def _lettering(text):
    """Metni elle çizilmiş harflerle maske olarak döndürür; ayrıca her harfin x aralığı."""
    widths = [len(GLYPHS[ch][0]) for ch in text]
    total = sum(widths) + GLYPH_GAP * (len(text) - 1)
    m = np.zeros((18, total), bool)
    spans = []
    x = 0
    for ch, wd in zip(text, widths):
        y0 = GLYPH_TOP.get(ch, 4)
        for yy, row in enumerate(GLYPHS[ch]):
            assert len(row) == wd, (ch, yy, row)
            for xx, c_ in enumerate(row):
                if c_ == "#":
                    m[y0 + yy, x + xx] = True
        spans.append((x, x + wd - 1))
        x += wd + GLYPH_GAP
    return m, spans


CROWN = [
    ".......W.......",
    "......WYW......",
    ".W....YRY....W.",
    "WYW...YYY...WYW",
    ".Y...YYYYY...Y.",
    ".YY.YYYYYYY.YY.",
    ".YYYYYYYYYYYYY.",
    ".yyyyyyyyyyyyy.",
    ".yByyyRRRyyyBy.",
    ".yyyyyyyyyyyyy.",
]


def _outside(solid):
    """Kenardan dolu piksellere değmeden ulaşılabilen boş pikseller (taşma doldurma)."""
    reach = np.zeros_like(solid)
    reach[0, :] = reach[-1, :] = True
    reach[:, 0] = reach[:, -1] = True
    reach &= ~solid
    while True:
        nxt = (reach | shift(reach, 1, 0) | shift(reach, -1, 0) | shift(reach, 0, 1) | shift(reach, 0, -1)) & ~solid
        if (nxt == reach).all():
            return reach
        reach = nxt


def build_logo():
    """'WordRoyale' logosu, yerel (1x) çözünürlükte saydam RGBA. Her kapsülde bu resim tam sayı katla büyütülür."""
    body, spans = _lettering("WordRoyale")
    split = spans[4][0]                               # "R" başlangıcı
    # Taç için üstte yer, dış hat ve gölge için kenar boşluğu
    crown_h = len(CROWN)
    top = LOGO_PAD + crown_h + 1
    H = body.shape[0] + top + LOGO_PAD + 4
    Wd = body.shape[1] + 2 * LOGO_PAD
    M = np.zeros((H, Wd), bool)
    M[top:top + body.shape[0], LOGO_PAD:LOGO_PAD + body.shape[1]] = body
    split += LOGO_PAD
    word = M.copy(); word[:, split:] = False
    royale = M & ~word

    # Taç: "R" harfinin üstüne, hafif sola yaslı otursun
    r_left, r_right = spans[4][0] + LOGO_PAD, spans[4][1] + LOGO_PAD
    crown_cv = from_rows(CROWN, {"W": W, "Y": Y, "y": O, "R": R, "B": B})
    cw = crown_cv.w
    cx0 = (r_left + r_right) // 2 - cw // 2
    cy0 = top - crown_h - 1
    crown_mask = np.zeros_like(M)
    crown_col = {}
    for yy in range(crown_cv.h):
        for xx in range(crown_cv.w):
            col = crown_cv.px[yy][xx]
            if col is not None:
                crown_mask[cy0 + yy, cx0 + xx] = True
                crown_col[(cy0 + yy, cx0 + xx)] = col

    solid = M | crown_mask
    outside = _outside(solid)
    holes = ~solid & ~outside                         # harf içi boşluklar (o, d, a, R...)
    ring1 = dilate(solid, 1, round_=False)            # her yerde 1 piksel siyah hat
    ring2 = dilate(solid, 2) & ~holes                 # dışarıda daha kalın hat
    ext = ring2.copy()
    for d in range(1, 4):                             # aşağı doğru 3B kalınlık
        ext |= shift(ring2, 0, d)
    ext &= ~holes
    outer = dilate(ext, 1, round_=False) & ~holes     # en dışta ince siyah hat

    img = np.zeros((H, Wd, 4), np.uint8)

    def paint(mask, col):
        img[mask] = rgba(col)

    # Kalınlık yalnız şeklin altında görünsün: altında yakında yine hat olan aralıklar (taç ile R arası gibi) siyah kalır
    below = np.zeros_like(ring2)
    for d in range(1, 5):
        below |= shift(ring2, 0, -d)
    side = ext & ~ring2 & ~below
    paint(outer, K)
    paint(ext, K)
    paint(side, p)
    paint(side & ~shift(ext, 0, -1), INK)             # kalınlığın alt sırası daha koyu
    paint(ring2 | ring1, K)

    # Harf dolguları: üst yarı açık, alt yarı koyu, en üst sıra parlak
    mid = top + 9                                     # küçük harf gövdesinin ortası
    yy_idx = np.arange(H)[:, None] * np.ones((1, Wd), int)
    top_edge = M & ~shift(M, 0, 1)
    for part, hi_c, lo_c, edge_hi in ((word, W, C, W), (royale, Y, O, W)):
        paint(part & (yy_idx < mid), hi_c)
        paint(part & (yy_idx >= mid), lo_c)
        paint(part & top_edge & (yy_idx < mid), edge_hi)
    for (yy, xx), col in crown_col.items():
        img[yy, xx] = rgba(col)
    out = Image.fromarray(img, "RGBA")
    return out.crop(out.getbbox())


# ================================================================ motifler
def make_die(face=5, size=14):
    """Kuşbakışı zar: krem yüz, alt kenarda kalınlık, kırmızı noktalar."""
    cv = Canvas(size + 2, size + 3)
    s_ = size
    cv.rect(1, 1, s_, s_, W)
    cv.rect(1, s_ - 2, s_, s_ + 1, w)                 # alt yüzey (kalınlık)
    cv.rect(1, s_ - 2, s_, s_ - 2, W)
    cv.rect(2, s_, s_ - 1, s_ + 1, g)
    for (x_, y_) in ((1, 1), (s_, 1), (1, s_ + 1), (s_, s_ + 1)):
        cv.px[y_][x_] = None                          # yuvarlak köşe
    cv.set(2, 2, W)
    pip = {1: [(0, 0)], 2: [(-1, -1), (1, 1)], 3: [(-1, -1), (0, 0), (1, 1)],
           4: [(-1, -1), (1, -1), (-1, 1), (1, 1)], 5: [(-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1)],
           6: [(-1, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1)]}[face]
    cxy = (s_ + 1) / 2.0
    step = (s_ - 4) / 3.0
    for dx, dy in pip:
        px_ = int(round(cxy + dx * step - 0.5))
        py_ = int(round(cxy - 1 + dy * step - 0.5))
        cv.rect(px_, py_, px_ + 1, py_ + 1, R)
        cv.set(px_ + 1, py_ + 1, r)
    cv.outline(K)
    return canvas_img(cv)


def make_textbox(wd=72, ht=16, timer=0.6):
    """Yazı kutusu: boş, yanıp sönen imleç ve 10 saniye sayacı çubuğu. Harf yok (Steam: logodan başka yazı yok)."""
    cv = Canvas(wd + 2, ht + 9)
    cv.rect(1, 1, wd, ht, W)
    cv.rect(1, ht - 1, wd, ht, w)
    cv.rect(2, 2, wd - 1, 2, C)
    cv.rect(6, 5, 7, ht - 4, K)                       # imleç
    cv.rect(10, ht - 4, 18, ht - 4, w)                # yazı çizgisi (harfsiz)
    # sayaç
    cv.rect(1, ht + 3, wd, ht + 6, INK)
    fill = int((wd - 2) * timer)
    cv.rect(2, ht + 4, 1 + fill, ht + 5, O)
    cv.rect(2, ht + 4, 1 + fill, ht + 4, Y)
    cv.outline(K)
    return canvas_img(cv)


def make_burst(rad=14):
    """Patlama yıldızı."""
    size = rad * 2 + 3
    cv = Canvas(size, size)
    c0 = size / 2.0
    pts = []
    for i in range(16):
        a = i * math.pi / 8
        rr = rad if i % 2 == 0 else rad * 0.55
        pts.append((c0 + math.cos(a) * rr, c0 + math.sin(a) * rr))
    cv.poly(pts, O)
    cv.ellipse(c0 - 0.5, c0 - 0.5, rad * 0.55, rad * 0.55, Y)
    cv.ellipse(c0 - 0.5, c0 - 0.5, rad * 0.28, rad * 0.28, W)
    cv.outline(r)
    return canvas_img(cv)


def make_puff():
    cv = from_rows([".ww.", "wWWw", ".ww."], {"w": w, "W": W})
    return canvas_img(cv)


def make_speed_lines():
    cv = from_rows(["WWWW....", "........", "..WWWWWW", "........", "WWWWW..."], {"W": W})
    img = canvas_img(cv)
    a = np.array(img)
    a[..., 3] = (a[..., 3] * 0.7).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


# ================================================================ arena
class Scene:
    def __init__(self, wb, hb, seed=1):
        self.w, self.h = wb, hb
        self.rnd = random.Random(seed)
        self.img = Image.new("RGBA", (wb, hb), rgba(D))
        self.tw = wb // 16 + 2
        self.th = hb // 16 + 2
        self.ox = (wb - self.tw * 16) // 2               # kareler ortalansın
        self.oy = (hb - self.th * 16) // 2
        self.grid = [[("t_grass_alt" if self.rnd.random() < 0.3 else "t_grass") for _ in range(self.tw)]
                     for _ in range(self.th)]
        self.over = []                                   # (tx, ty, sprite) zemin üstü kareler

    # -- karo yerleşimi (kesirli konumdan kare indeksi) --
    def t(self, fx, fy):
        return int(fx * self.tw), int(fy * self.th)

    def path_h(self, fy, x0=0.0, x1=1.0):
        _, ty = self.t(0, fy)
        for tx in range(int(x0 * self.tw), int(math.ceil(x1 * self.tw))):
            self._set(tx, ty, "t_path")

    def path_v(self, fx, y0=0.0, y1=1.0):
        tx, _ = self.t(fx, 0)
        for ty in range(int(y0 * self.th), int(math.ceil(y1 * self.th))):
            self._set(tx, ty, "t_path")

    def _set(self, tx, ty, name):
        if 0 <= tx < self.tw and 0 <= ty < self.th:
            self.grid[ty][tx] = name

    def area(self, fx, fy, wt, ht, name, edge=None):
        tx0, ty0 = self.t(fx, fy)
        for yy in range(ht):
            for xx in range(wt):
                self._set(tx0 + xx, ty0 + yy, name)

    def house(self, fx, fy, wt=3, ht=4, door=1):
        """Oyundaki gibi çatı: üst sıra, sırt, ara sıra(lar), saçak; altında ahşap duvar ve kapı saçağı."""
        tx0, ty0 = self.t(fx, fy)
        for cy in range(ht):
            from_top = cy
            part = "a" if from_top == 0 else "ridge" if from_top == 1 else "eave" if cy == ht - 1 else "b"
            for cx in range(wt):
                self._set(tx0 + cx, ty0 + cy, "t_roof_" + part)
        for cx in range(wt):
            self._set(tx0 + cx, ty0 + ht, "t_woodwall")
        if door is not None:
            self.over.append((tx0 + door, ty0 + ht, "t_awning_koy"))

    def fire_patch(self, fx, fy):
        tx0, ty0 = self.t(fx, fy)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1)):
            self._set(tx0 + dx, ty0 + dy, "t_charred")
        for dx, dy in ((1, 0), (-1, 0), (0, -1), (1, 1)):
            self.over.append((tx0 + dx, ty0 + dy, "t_fire"))

    def render_ground(self):
        for ty in range(self.th):
            for tx in range(self.tw):
                self.img.alpha_composite(sprite(self.grid[ty][tx]), (self.ox + tx * 16, self.oy + ty * 16))
        for tx, ty, name in self.over:
            self.img.alpha_composite(sprite(name), (self.ox + tx * 16, self.oy + ty * 16))

    # -- nesneler --
    def shadow(self, cx, by, wdt, alpha=90):
        sh = Image.new("RGBA", (wdt, max(3, wdt // 4)), (0, 0, 0, 0))
        cv = Canvas(sh.width, sh.height)
        cv.ellipse((sh.width - 1) / 2, (sh.height - 1) / 2, sh.width / 2, sh.height / 2, K)
        im = canvas_img(cv)
        a = np.array(im)
        a[..., 3] = (a[..., 3] > 0) * alpha
        self.img.alpha_composite(Image.fromarray(a.astype(np.uint8), "RGBA"),
                                 (int(cx - sh.width // 2), int(by - sh.height // 2 - 1)))

    def put(self, img, cx, by, shadow=True, flip=False, alpha=None):
        """img'yi alt-ortası (cx, by) olacak şekilde koyar."""
        if flip:
            img = img.transpose(Image.FLIP_LEFT_RIGHT)
        bb = img.getbbox()
        if shadow and bb:
            self.shadow(cx, by, max(6, int((bb[2] - bb[0]) * 0.8)))
        if alpha is not None:
            a = np.array(img)
            a[..., 3] = (a[..., 3].astype(np.float32) * alpha).astype(np.uint8)
            img = Image.fromarray(a, "RGBA")
        bottom = bb[3] if bb else img.height
        self.img.alpha_composite(img, (int(cx - img.width // 2), int(by - bottom)))

    def creature(self, name, fx, fy, scale=2, flip=False, shadow=True, alpha=None):
        self.put(up(sprite(name), scale), fx * self.w, fy * self.h, shadow, flip, alpha)

    def player(self, color, fx, fy, flip=False, puff=True, scale=1):
        cx, by = fx * self.w, fy * self.h
        if puff:
            pf = up(make_puff(), scale)
            off = (10 if flip else -10) * scale
            self.img.alpha_composite(pf, (int(cx + off - pf.width // 2), int(by - 3 * scale)))
            self.img.alpha_composite(pf, (int(cx + off * 1.6 - pf.width // 2), int(by - 6 * scale)))
        self.put(up(tint(sprite("char"), color), scale), cx, by, True, flip)

    def obj(self, img, fx, fy, shadow=True):
        self.put(img, fx * self.w, fy * self.h, shadow)

    def array(self):
        return np.array(self.img)


PLAYER_COLORS = [R, B, L, Y, P, O, PINK, C, S, g]


def arena(wb, hb, seed, houses, ponds=(), fires=(), paths=((None, 0.62),), paths_v=((0.5, None),)):
    sc = Scene(wb, hb, seed)
    for _, fy in paths:
        sc.path_h(fy)
    for fx, _ in paths_v:
        sc.path_v(fx)
    for fx, fy, wt, ht in ponds:
        sc.area(fx, fy, wt, ht, "t_water")
    for h_ in houses:
        sc.house(*h_)
    for fx, fy in fires:
        sc.fire_patch(fx, fy)
    sc.render_ground()
    return sc


def place_actors(sc, actors):
    """actors: (tür, ...) listesi; y'ye göre sıralanıp çizilir (önde olan sonra)."""
    for a in sorted(actors, key=lambda t: t[2]):
        kind = a[0]
        if kind == "c":
            _, name, fx, fy, scale, flip = a[:6]
            alpha = a[6] if len(a) > 6 else None
            sc.creature(name, fx, fy, scale, flip, shadow=alpha is None, alpha=alpha)
        elif kind == "p":
            _, ci, fx, fy, flip = a[:5]
            scale = a[5] if len(a) > 5 else 2
            sc.player(PLAYER_COLORS[ci % len(PLAYER_COLORS)], fx, fy, flip, scale=scale)
        elif kind == "o":
            _, img, fx, fy = a[:4]
            shadow = a[4] if len(a) > 4 else True
            sc.obj(img, fx, fy, shadow)


def paste_logo(final, logo, m, cx, top):
    big = up(logo, m)
    final.alpha_composite(big, (int(cx - big.width // 2), int(top)))
    return big.size


# ================================================================ çıktılar
def out_path(name):
    return os.path.join(OUT, name)


def save(img, name, size):
    assert img.size == size, (name, img.size, size)
    img.save(out_path(name))
    return name


def scene_standard(wb, hb, seed, rich=False):
    """Kapsüllerin ortak sahnesi: köy arenası, solda godzilla, sağda ejderha, ortada yazı kutusu ve zar, kaçışan oyuncular."""
    houses = [(0.02, 0.02, 3, 3, 1), (0.80, 0.0, 3, 3, 1)]
    ponds = [(0.86, 0.74, 3, 2)]
    fires = [(0.30, 0.82)]
    if rich:
        houses += [(0.30, -0.02, 2, 3, 0), (0.60, 0.0, 3, 3, 2)]
        ponds += [(0.0, 0.78, 2, 2)]
        fires += [(0.64, 0.86)]
    sc = arena(wb, hb, seed, houses, ponds, fires, paths=((None, 0.62),), paths_v=((0.5, None),))
    return sc


def header_like(size, k, logo, m, seed, rich=False, logo_top=None):
    wb, hb = -(-size[0] // k), -(-size[1] // k)
    sc = scene_standard(wb, hb, seed, rich)
    actors = [
        ("c", "godzilla", 0.15, 0.96, 4, False),
        ("c", "ejderha", 0.86, 0.66, 3, True),
        ("c", "zombi", 0.30, 0.70, 2, False),
        ("c", "kurt", 0.70, 0.98, 2, True),
        ("c", "bomba", 0.39, 0.97, 2, False),
        ("c", "ari", 0.95, 0.92, 2, True),
        ("c", "hayalet", 0.06, 0.50, 2, False, 0.75),
        ("p", 0, 0.47, 0.88, False), ("p", 1, 0.60, 0.97, False), ("p", 2, 0.22, 0.62, True),
        ("p", 3, 0.66, 0.80, True), ("p", 4, 0.74, 0.62, True), ("p", 5, 0.55, 0.84, False),
    ]
    if rich:
        actors += [
            ("c", "iskelet", 0.45, 0.80, 2, False),
            ("c", "kraken", 0.95, 0.70, 2, True),
            ("c", "pterodaktil", 0.70, 0.50, 2, True),
            ("p", 6, 0.33, 0.56, True), ("p", 7, 0.82, 0.92, False), ("p", 8, 0.92, 0.56, False),
        ]
    # patlama bombanın arkasında
    sc.obj(make_burst(12), 0.39, 0.93, shadow=False)
    place_actors(sc, actors)
    # Ortada yazı kutusu ve zarlar
    tb = make_textbox(78 if rich else 70)
    die_a, die_b = make_die(5), make_die(3)
    cx, cy = wb // 2, int(hb * 0.66)
    sc.img.alpha_composite(tb, (cx - tb.width // 2, cy - tb.height // 2))
    sc.img.alpha_composite(die_a, (cx - tb.width // 2 - die_a.width + 2, cy - tb.height // 2 - die_a.height + 6))
    sc.img.alpha_composite(die_b, (cx + tb.width // 2 - 4, cy - tb.height // 2 - die_b.height + 2))
    a = sc.array()
    a = band_darken(a, 0, int(hb * 0.52), 0.40, 1.0, bands=5)
    a = vignette(a, 0.30)
    final = to_final(Image.fromarray(a, "RGBA"), k, size)
    top = logo_top if logo_top is not None else int(size[1] * 0.04)
    paste_logo(final, logo, m, size[0] // 2, top)
    return final.convert("RGB")


def make_logo_png(logo):
    size = (1280, 720)
    m = 1
    while (m + 1) * logo.width <= size[0] - 80 and (m + 1) * logo.height <= size[1] - 80:
        m += 1
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    big = up(logo, m)
    img.alpha_composite(big, ((size[0] - big.width) // 2, (size[1] - big.height) // 2))
    return save(img, "logo.png", size)


def make_small(logo):
    size = (462, 174)
    k = 2
    wb, hb = -(-size[0] // k), -(-size[1] // k)
    sc = Scene(wb, hb, 7)
    sc.render_ground()
    a = sc.array()
    a = shade_arr(a, 0.55)
    a = vignette(a, 0.45)
    final = to_final(Image.fromarray(a, "RGBA"), k, size)
    m = 3
    big = up(logo, m)
    final.alpha_composite(big, ((size[0] - big.width) // 2, (size[1] - big.height) // 2))
    return save(final.convert("RGB"), "small_capsule.png", size)


def make_vertical(logo, size, name, m, seed, logo_frac=0.04):
    """Dikey kapak: logo üstte, altında arena ve büyük yaratıklar (3 kat piksel: yakın plan)."""
    k = 3
    wb, hb = -(-size[0] // k), -(-size[1] // k)
    houses = [(0.0, 0.0, 3, 3, 1), (0.70, 0.02, 3, 3, 1), (0.02, 0.50, 3, 3, 1)]
    ponds = [(0.72, 0.80, 3, 2)]
    fires = [(0.30, 0.86)]
    sc = arena(wb, hb, seed, houses, ponds, fires, paths=((None, 0.70),), paths_v=((0.5, None),))
    sc.obj(make_burst(12), 0.28, 0.95, shadow=False)
    actors = [
        ("c", "godzilla", 0.27, 0.74, 4, False),
        ("c", "ejderha", 0.78, 0.62, 3, True),
        ("c", "zombi", 0.88, 0.75, 2, True),
        ("c", "kurt", 0.66, 0.98, 2, True),
        ("c", "bomba", 0.28, 0.985, 2, False),
        ("c", "hayalet", 0.88, 0.40, 2, True, 0.75),
        ("c", "ari", 0.10, 0.85, 2, False),
        ("p", 0, 0.46, 0.76, True), ("p", 1, 0.58, 0.80, False), ("p", 2, 0.44, 0.62, True),
        ("p", 3, 0.55, 0.95, False), ("p", 4, 0.14, 0.42, True), ("p", 5, 0.92, 0.95, False),
        ("p", 6, 0.68, 0.40, False),
    ]
    place_actors(sc, actors)
    tb = make_textbox(min(90, int(wb * 0.36)))
    die_a, die_b = make_die(6), make_die(2)
    cx, cy = wb // 2, int(hb * 0.48)
    sc.img.alpha_composite(tb, (cx - tb.width // 2, cy - tb.height // 2))
    sc.img.alpha_composite(die_a, (cx - tb.width // 2 - die_a.width + 4, cy - tb.height // 2 - die_a.height + 6))
    sc.img.alpha_composite(die_b, (cx + tb.width // 2 - 4, cy - tb.height // 2 - die_b.height + 2))
    a = sc.array()
    a = band_darken(a, 0, int(hb * 0.36), 0.38, 1.0, bands=5)
    a = vignette(a, 0.30)
    final = to_final(Image.fromarray(a, "RGBA"), k, size)
    paste_logo(final, logo, m, size[0] // 2, int(size[1] * logo_frac))
    return save(final.convert("RGB"), name, size)


def make_hero():
    """Kütüphane kahraman görseli: yazısız. Önemli öğeler ortada (Steam logoyu üstüne koyar)."""
    size = (3840, 1240)
    k = 4
    wb, hb = size[0] // k, size[1] // k
    houses = [(0.04, 0.05, 4, 3, 1), (0.18, 0.55, 3, 3, 1), (0.78, 0.04, 4, 3, 2), (0.88, 0.50, 3, 3, 1),
              (0.30, 0.02, 3, 3, 1), (0.64, 0.60, 3, 3, 1)]
    ponds = [(0.08, 0.80, 4, 2), (0.70, 0.10, 3, 2)]
    fires = [(0.40, 0.80), (0.86, 0.86)]
    sc = arena(wb, hb, 21, houses, ponds, fires, paths=((None, 0.45),), paths_v=((0.5, None),))
    sc.obj(make_burst(14), 0.40, 0.98, shadow=False)
    actors = [
        ("c", "godzilla", 0.42, 0.72, 5, False),
        ("c", "ejderha", 0.58, 0.62, 4, True),
        ("c", "zombi", 0.30, 0.66, 2, False),
        ("c", "kurt", 0.68, 0.92, 2, True),
        ("c", "bomba", 0.40, 0.99, 2, False),
        ("c", "kingkong", 0.22, 0.42, 3, False),
        ("c", "kraken", 0.80, 0.40, 3, True),
        ("c", "ari", 0.62, 0.30, 2, True),
        ("c", "iskelet", 0.74, 0.70, 2, True),
        ("c", "hayalet", 0.12, 0.40, 2, False, 0.7),
        ("c", "hayalet", 0.92, 0.30, 2, True, 0.7),
        ("p", 0, 0.48, 0.86, True), ("p", 1, 0.53, 0.94, False), ("p", 2, 0.35, 0.86, True),
        ("p", 3, 0.63, 0.76, False), ("p", 4, 0.26, 0.92, True), ("p", 5, 0.72, 0.55, False),
        ("p", 6, 0.50, 0.40, False), ("p", 7, 0.33, 0.48, True), ("p", 8, 0.58, 0.98, False),
        ("p", 9, 0.15, 0.70, True),
    ]
    place_actors(sc, actors)
    # ortada zarlar (yazı kutusu yok: tamamen yazısız sahne)
    d1, d2 = up(make_die(5), 2), up(make_die(6), 2)
    cx = wb // 2
    sc.img.alpha_composite(d1, (cx - d1.width - 2, int(hb * 0.30)))
    sc.img.alpha_composite(d2, (cx + 4, int(hb * 0.30) + 8))
    a = vignette(sc.array(), 0.35)
    final = to_final(Image.fromarray(a, "RGBA"), k, size)
    return save(final.convert("RGB"), "library_hero.png", size)


def make_page_bg():
    """Mağaza sayfa arka planı: sönük, koyu arena deseni."""
    size = (1438, 810)
    k = 2
    wb, hb = -(-size[0] // k), -(-size[1] // k)
    houses = [(0.05, 0.10, 3, 3, 1), (0.80, 0.05, 3, 3, 1), (0.10, 0.70, 3, 3, 1), (0.75, 0.66, 3, 3, 1)]
    sc = arena(wb, hb, 33, houses, [(0.45, 0.80, 4, 2)], [], paths=((None, 0.55),), paths_v=((0.5, None),))
    a = sc.array().astype(np.float32)
    # Koyu mor-laciverte doğru soldur
    base = np.array(INK, np.float32)
    a[..., :3] = a[..., :3] * 0.22 + base * 0.30
    a = vignette(a.clip(0, 255).astype(np.uint8), 0.5)
    final = to_final(Image.fromarray(a, "RGBA"), k, size)
    return save(final.convert("RGB"), "page_background.png", size)


def make_icon():
    """32x32 simge: üzerinde logodaki W harfi olan zar. 256x256 sürümü 8 kat NEAREST büyütme."""
    cv = Canvas(32, 32)
    cv.rect(2, 2, 29, 29, W)
    cv.rect(2, 24, 29, 29, w)                         # zarın alt kalınlığı
    cv.rect(3, 28, 28, 29, g)
    for (x_, y_) in ((2, 2), (29, 2), (2, 29), (29, 29), (3, 2), (2, 3), (28, 2), (29, 3), (2, 28), (3, 29), (29, 28), (28, 29)):
        cv.px[y_][x_] = None                          # yuvarlak köşeler
    cv.rect(4, 3, 7, 3, C)                            # parlama
    cv.rect(3, 4, 3, 6, C)
    glyph = GLYPHS["W"]                               # logodaki W (15x14)
    wx, wy = (32 - len(glyph[0])) // 2, 6
    for yy, row in enumerate(glyph):
        for xx, ch in enumerate(row):
            if ch == "#":
                cv.set(wx + xx, wy + yy, R if yy < 8 else r)
    cv.rect(4, 19, 5, 20, INK)                        # zar noktaları (köşelerde)
    cv.rect(26, 19, 27, 20, INK)
    cv.outline(K)
    img = canvas_img(cv)
    img.save(out_path("icon_32.png"))
    big = up(img, 8)
    return save(big, "icon.png", (256, 256))


def make_preview(names):
    """Tüm çıktıların küçültülmüş kolajı (yalnız inceleme için)."""
    thumbs = []
    for nm in names:
        im = Image.open(out_path(nm)).convert("RGBA")
        f = max(1, math.ceil(max(im.width / 640, im.height / 360)))
        th = im.reduce(f) if f > 1 else im
        thumbs.append((nm, th))
    pad = 12
    cols = 3
    cw = max(t.width for _, t in thumbs) + pad
    rows_ = [thumbs[i:i + cols] for i in range(0, len(thumbs), cols)]
    rh = [max(t.height for _, t in row) + pad + 18 for row in rows_]
    sheet = Image.new("RGBA", (cols * cw + pad, sum(rh) + pad), rgba(INK))
    # saydamlık görünsün diye dama deseni
    font = ImageFont.truetype(FONT, 14)
    d = ImageDraw.Draw(sheet)
    y0 = pad
    for row, h_ in zip(rows_, rh):
        x0 = pad
        for nm, th in row:
            chk = Image.new("RGBA", th.size, rgba(G))
            ca = np.array(chk)
            yy, xx = np.mgrid[0:th.height, 0:th.width]
            ca[((yy // 8 + xx // 8) % 2 == 0)] = rgba(g)
            chk = Image.fromarray(ca, "RGBA")
            chk.alpha_composite(th)
            sheet.alpha_composite(chk, (x0, y0 + 18))
            full = Image.open(out_path(nm))
            d.text((x0, y0), "%s  %dx%d" % (nm, full.width, full.height), font=font, fill=rgba(W))
            x0 += cw
        y0 += h_
    sheet.convert("RGB").save(out_path("_preview.png"))


EXPECTED = {
    "logo.png": (1280, 720), "header_capsule.png": (920, 430), "small_capsule.png": (462, 174),
    "main_capsule.png": (1232, 706), "vertical_capsule.png": (748, 896), "library_capsule.png": (600, 900),
    "library_hero.png": (3840, 1240), "page_background.png": (1438, 810), "icon.png": (256, 256),
    "icon_32.png": (32, 32),
}


def main():
    os.makedirs(OUT, exist_ok=True)
    logo = build_logo()
    logo.save(out_path("_logo_1x.png"))       # yerel çözünürlük (başka boyutlar için tam sayı katla büyüt)
    make_logo_png(logo)
    save(header_like((920, 430), 2, logo, 5, 5), "header_capsule.png", (920, 430))
    make_small(logo)
    save(header_like((1232, 706), 2, logo, 7, 11, rich=True), "main_capsule.png", (1232, 706))
    make_vertical(logo, (748, 896), "vertical_capsule.png", 5, 13)
    make_vertical(logo, (600, 900), "library_capsule.png", 4, 17, logo_frac=0.05)
    make_hero()
    make_page_bg()
    make_icon()
    bad = []
    for nm, sz in EXPECTED.items():
        got = Image.open(out_path(nm)).size
        print("%-22s %4dx%-4d %s" % (nm, got[0], got[1], "OK" if got == sz else "HATA beklenen %s" % (sz,)))
        if got != sz:
            bad.append(nm)
    make_preview([nm for nm in EXPECTED if nm != "icon_32.png"])
    print("Logo yerel boyutu:", logo.size)
    if bad:
        raise SystemExit("Boyutu yanlış: " + ", ".join(bad))


if __name__ == "__main__":
    main()
