"""
Kelime Battle Royale - retro (chiptune) ses ureticisi.

Yalnizca Python standart kutuphanesiyle (wave, struct, math, random, array) efektleri ve muzik dongulerini sentezler,
44100 Hz 16-bit mono WAV olarak Assets/Resources/Audio/ altina yazar. Internetten hicbir sey indirmez.
Sabit random seed kullanir: tekrar calistirinca birebir ayni dosyalar cikar.

Kullanim (proje klasorunde):  python Tools/GenerateSounds.py
Bir sesi begenmezsen ayni adli WAV dosyasini kendi sesinle degistirebilirsin; oyun ada gore yukler.
"""

import math
import os
import random
import struct
import wave
from array import array

SAMPLE_RATE = 44100
PEAK = 10 ** (-3.0 / 20.0)      # -3 dBFS tepe
FADE_SECONDS = 0.004            # Efektlerin basinda/sonunda tik olmasin diye
SEED = 20261126

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Audio")


# --------------------------------------------------------------------------------------------
# Temel yapi taslari
# --------------------------------------------------------------------------------------------

def midi_to_freq(note):
    return 440.0 * (2.0 ** ((note - 69) / 12.0))


def osc(kind, phase, duty=0.5):
    """Tek ornek: phase 0..1 arasi dongu konumu."""
    p = phase - math.floor(phase)
    if kind == "square":
        return 1.0 if p < duty else -1.0
    if kind == "triangle":
        return 4.0 * p - 1.0 if p < 0.5 else 3.0 - 4.0 * p
    if kind == "saw":
        return 2.0 * p - 1.0
    if kind == "sine":
        return math.sin(2.0 * math.pi * p)
    raise ValueError(kind)


def envelope(i, length, attack, release, sustain_level=1.0, decay=0.0):
    """Basit ADSR (ornek sayisi cinsinden). decay: atak sonrasi sustain seviyesine inis."""
    if i < attack:
        return i / max(1, attack)
    if decay > 0 and i < attack + decay:
        k = (i - attack) / decay
        return 1.0 + (sustain_level - 1.0) * k
    level = sustain_level
    if i > length - release:
        level *= max(0.0, (length - i) / max(1, release))
    return level


class Lowpass:
    """Tek kutuplu alcak geciren suzgec (retro boguk ses icin)."""

    def __init__(self, cutoff):
        self.y = 0.0
        self.set(cutoff)

    def set(self, cutoff):
        cutoff = max(10.0, min(cutoff, SAMPLE_RATE * 0.45))
        rc = 1.0 / (2 * math.pi * cutoff)
        dt = 1.0 / SAMPLE_RATE
        self.a = dt / (rc + dt)

    def __call__(self, x):
        self.y += self.a * (x - self.y)
        return self.y


class Highpass:
    def __init__(self, cutoff):
        rc = 1.0 / (2 * math.pi * cutoff)
        dt = 1.0 / SAMPLE_RATE
        self.a = rc / (rc + dt)
        self.prev_x = 0.0
        self.y = 0.0

    def __call__(self, x):
        self.y = self.a * (self.y + x - self.prev_x)
        self.prev_x = x
        return self.y


def seconds(s):
    return int(round(s * SAMPLE_RATE))


def silence(s):
    return [0.0] * seconds(s)


def mix_into(buf, samples, start, gain=1.0, wrap=False):
    n = len(buf)
    for i, v in enumerate(samples):
        j = start + i
        if wrap:
            j %= n
        elif j >= n:
            break
        buf[j] += v * gain


def tone(freq, dur, kind="square", duty=0.5, attack=0.005, release=0.05, sustain=1.0, decay=0.0,
         slide_to=None, vibrato=0.0, vibrato_rate=6.0, gain=1.0):
    """Perde kaydirmali (slide_to), titresimli (vibrato) tek nota."""
    n = seconds(dur)
    out = [0.0] * n
    phase = 0.0
    a, r, d = seconds(attack), seconds(release), seconds(decay)
    for i in range(n):
        t = i / SAMPLE_RATE
        f = freq if slide_to is None else freq * ((slide_to / freq) ** (i / max(1, n - 1)))
        if vibrato > 0:
            f *= 1.0 + vibrato * math.sin(2 * math.pi * vibrato_rate * t)
        phase += f / SAMPLE_RATE
        out[i] = osc(kind, phase, duty) * envelope(i, n, a, r, sustain, d) * gain
    return out


def noise(dur, rng, attack=0.002, release=0.05, cutoff=None, hold=1, gain=1.0, sustain=1.0, decay=0.0):
    """Beyaz gurultu; hold>1 ise ornek-tut (retro 'kirli' gurultu), cutoff ile boguklasir."""
    n = seconds(dur)
    out = [0.0] * n
    lp = Lowpass(cutoff) if cutoff else None
    a, r, d = seconds(attack), seconds(release), seconds(decay)
    value = 0.0
    for i in range(n):
        if i % hold == 0:
            value = rng.uniform(-1.0, 1.0)
        v = lp(value) if lp else value
        out[i] = v * envelope(i, n, a, r, sustain, d) * gain
    return out


def finish(samples, loop=False):
    """Tepeyi -3 dBFS'e getirir; efektte bas/son fade, dongude fade yok (dongu kesintisiz kalsin)."""
    if not loop:
        f = seconds(FADE_SECONDS)
        n = len(samples)
        for i in range(min(f, n)):
            samples[i] *= i / f
            samples[n - 1 - i] *= i / f
    # Dogru akim (DC) kaymasini al: hoparlorde 'tok' sesi olmasin
    mean = sum(samples) / max(1, len(samples))
    samples = [s - mean for s in samples]
    if not loop:
        samples[0] = 0.0
        samples[-1] = 0.0
    peak = max(1e-9, max(abs(s) for s in samples))
    k = PEAK / peak
    return [s * k for s in samples]


def write_wav(name, samples, loop=False):
    samples = finish(samples, loop)
    data = array("h", (max(-32767, min(32767, int(round(s * 32767)))) for s in samples))
    if struct.pack("<h", 1) != array("h", [1]).tobytes():
        data.byteswap()  # WAV little-endian
    path = os.path.join(OUT_DIR, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SAMPLE_RATE)
        w.writeframes(data.tobytes())
    print(f"{name}.wav  {len(samples) / SAMPLE_RATE:.2f} sn")
    return len(samples) / SAMPLE_RATE


# --------------------------------------------------------------------------------------------
# Efektler
# --------------------------------------------------------------------------------------------

def sfx_dice_roll(rng):
    # Zar takirtisi: sik baslayip yavaslayan kucuk tikirtilar
    buf = silence(1.3)
    t = 0.0
    gap = 0.035
    while t < 1.15:
        click = noise(0.025, rng, attack=0.001, release=0.02, cutoff=rng.uniform(3000, 6000), gain=0.6)
        blip = tone(rng.choice([900, 1100, 1300, 1500]), 0.03, "square", duty=0.25, release=0.025, gain=0.25)
        mix_into(buf, click, seconds(t))
        mix_into(buf, blip, seconds(t))
        t += gap * rng.uniform(0.8, 1.2)
        gap *= 1.07
    return buf


def sfx_dice_land(rng):
    buf = silence(0.3)
    mix_into(buf, tone(200, 0.22, "triangle", release=0.18, slide_to=55), 0)
    mix_into(buf, noise(0.06, rng, release=0.05, cutoff=2500, gain=0.5), 0)
    mix_into(buf, tone(1320, 0.12, "square", duty=0.25, attack=0.002, release=0.1, gain=0.2), seconds(0.02))
    return buf


def sfx_key(rng):
    buf = tone(1250, 0.05, "square", duty=0.25, attack=0.001, release=0.04, gain=0.5)
    mix_into(buf, noise(0.02, rng, release=0.018, cutoff=5000, gain=0.25), 0)
    return buf


def sfx_word_ok(rng):
    buf = silence(0.5)
    for k, note in enumerate([72, 76, 79, 84]):  # Do-Mi-Sol-Do
        mix_into(buf, tone(midi_to_freq(note), 0.16 if k < 3 else 0.26, "square", duty=0.25, release=0.1, gain=0.5),
                 seconds(0.06 * k))
    return buf


def sfx_word_bad(rng):
    buf = silence(0.42)
    # Ince kare + ucgen karisimi, boguk: "olmadi" der ama kulak tirmalamaz
    mix_into(buf, tone(311, 0.16, "square", duty=0.125, release=0.06, gain=0.25), 0)
    mix_into(buf, tone(311, 0.16, "triangle", release=0.06, gain=0.4), 0)
    mix_into(buf, tone(233, 0.24, "square", duty=0.125, release=0.16, slide_to=200, gain=0.25), seconds(0.17))
    mix_into(buf, tone(233, 0.24, "triangle", release=0.16, slide_to=200, gain=0.4), seconds(0.17))
    lp = Lowpass(1300)
    return [lp(s) for s in buf]


def sfx_spawn(rng):
    buf = silence(0.6)
    mix_into(buf, tone(180, 0.5, "triangle", attack=0.01, release=0.3, slide_to=760), 0)
    mix_into(buf, noise(0.45, rng, attack=0.05, release=0.35, cutoff=1200, gain=0.35), 0)
    mix_into(buf, tone(midi_to_freq(84), 0.15, "square", duty=0.125, release=0.1, gain=0.15), seconds(0.35))
    return buf


def sfx_explosion(rng):
    buf = silence(1.25)
    # Ornek-tut gurultu (retro catirti), zamanla boguklasir
    n = seconds(1.2)
    lp = Lowpass(4000)
    value = 0.0
    for i in range(n):
        if i % 3 == 0:
            value = rng.uniform(-1, 1)
        lp.set(4000 * (1 - i / n) ** 2 + 150)
        buf[i] += lp(value) * envelope(i, n, seconds(0.003), seconds(0.9), 0.6, seconds(0.25))
    mix_into(buf, tone(110, 0.6, "sine", attack=0.002, release=0.5, slide_to=35, gain=0.9), 0)
    return buf


def sfx_hit(rng):
    buf = silence(0.2)
    mix_into(buf, noise(0.08, rng, attack=0.001, release=0.07, cutoff=3500, gain=0.6), 0)
    mix_into(buf, tone(420, 0.16, "square", duty=0.5, attack=0.001, release=0.12, slide_to=90, gain=0.45), 0)
    return buf


def sfx_shield(rng):
    buf = silence(0.75)
    for k, note in enumerate([67, 71, 74, 79]):
        mix_into(buf, tone(midi_to_freq(note), 0.6 - 0.05 * k, "triangle", attack=0.02, release=0.35,
                           vibrato=0.012, vibrato_rate=9, gain=0.35), seconds(0.04 * k))
    mix_into(buf, tone(midi_to_freq(91), 0.4, "square", duty=0.125, attack=0.05, release=0.3, gain=0.08), seconds(0.2))
    return buf


def sfx_heal(rng):
    buf = silence(0.85)
    for k, note in enumerate([72, 76, 79, 83, 84, 88]):
        mix_into(buf, tone(midi_to_freq(note), 0.3, "sine", attack=0.005, release=0.25, gain=0.45), seconds(0.08 * k))
        mix_into(buf, tone(midi_to_freq(note + 12), 0.15, "triangle", release=0.12, gain=0.12), seconds(0.08 * k))
    return buf


def sfx_fire(rng):
    buf = silence(1.0)
    # Ugultu: boguk gurultu
    mix_into(buf, noise(1.0, rng, attack=0.1, release=0.4, cutoff=600, gain=0.7), 0)
    # Citirtilar
    for _ in range(26):
        at = rng.uniform(0.02, 0.85)
        mix_into(buf, noise(0.012, rng, attack=0.0005, release=0.01, cutoff=7000, gain=rng.uniform(0.3, 0.7)), seconds(at))
    return buf


def sfx_water(rng):
    buf = silence(1.0)
    mix_into(buf, noise(1.0, rng, attack=0.15, release=0.5, cutoff=450, gain=0.45), 0)
    # Kabarciklar: kisa, yukari kayan sinusler
    for _ in range(14):
        at = rng.uniform(0.0, 0.8)
        f = rng.uniform(350, 900)
        mix_into(buf, tone(f, 0.06, "sine", attack=0.002, release=0.04, slide_to=f * 2.2, gain=0.5), seconds(at))
    return buf


def sfx_fly(rng):
    # Kanat cirpma: 12 Hz genlik vuruslu boguk gurultu
    n = seconds(0.6)
    lp = Lowpass(900)
    out = [0.0] * n
    for i in range(n):
        t = i / SAMPLE_RATE
        flap = max(0.0, math.sin(2 * math.pi * 11 * t)) ** 2
        out[i] = lp(rng.uniform(-1, 1)) * flap * envelope(i, n, seconds(0.03), seconds(0.25))
    mix_into(out, tone(160, 0.6, "triangle", attack=0.05, release=0.3, vibrato=0.08, vibrato_rate=11, gain=0.12), 0)
    return out


def sfx_siren(rng):
    # Iki tam 'vuu-vii' dongusu; kare dalga ince (duty 0.25) + ucgen, boguk: kulak tirmalamaz
    n = seconds(1.5)
    out = [0.0] * n
    phase = 0.0
    lp = Lowpass(2200)
    for i in range(n):
        t = i / SAMPLE_RATE
        f = 620 + 260 * (0.5 - 0.5 * math.cos(2 * math.pi * t / 0.75))
        phase += f / SAMPLE_RATE
        v = 0.45 * osc("square", phase, 0.25) + 0.55 * osc("triangle", phase)
        out[i] = lp(v) * envelope(i, n, seconds(0.03), seconds(0.15))
    return out


def sfx_eliminated(rng):
    buf = silence(0.95)
    n = seconds(0.9)
    phase = 0.0
    for i in range(n):
        t = i / SAMPLE_RATE
        f = 660 * ((110 / 660) ** (i / n))
        phase += f / SAMPLE_RATE
        trem = 0.7 + 0.3 * math.sin(2 * math.pi * 14 * t)
        v = 0.35 * osc("square", phase, 0.25) + 0.65 * osc("triangle", phase)
        buf[i] += v * trem * envelope(i, n, seconds(0.005), seconds(0.45), 0.6, seconds(0.3)) * 0.5
    lp = Lowpass(1600)
    return [lp(s) for s in buf]


def sfx_victory(rng):
    buf = silence(1.55)
    melody = [(0.0, 0.12, 67), (0.13, 0.12, 72), (0.26, 0.12, 76), (0.39, 0.3, 79), (0.72, 0.12, 76), (0.85, 0.65, 79)]
    for at, dur, note in melody:
        mix_into(buf, tone(midi_to_freq(note), dur, "square", duty=0.25, release=0.06, gain=0.4), seconds(at))
    for at, dur, note in [(0.0, 0.7, 48), (0.72, 0.8, 55)]:
        mix_into(buf, tone(midi_to_freq(note), dur, "triangle", release=0.2, gain=0.55), seconds(at))
    mix_into(buf, tone(midi_to_freq(84), 0.6, "triangle", attack=0.02, release=0.4, vibrato=0.01, gain=0.2), seconds(0.88))
    return buf


def sfx_defeat(rng):
    buf = silence(1.55)
    for k, note in enumerate([67, 66, 65]):
        mix_into(buf, tone(midi_to_freq(note), 0.32, "triangle", release=0.08, gain=0.55), seconds(0.34 * k))
    mix_into(buf, tone(midi_to_freq(64), 0.5, "triangle", release=0.3, vibrato=0.015, vibrato_rate=5, gain=0.55), seconds(1.02))
    for k, note in enumerate([43, 42, 41, 40]):
        mix_into(buf, tone(midi_to_freq(note), 0.32 if k < 3 else 0.5, "square", duty=0.5, release=0.1, gain=0.15),
                 seconds(0.34 * k))
    lp = Lowpass(1800)
    return [lp(s) for s in buf]


def sfx_ui_click(rng):
    buf = tone(1800, 0.06, "triangle", attack=0.001, release=0.05, gain=0.6)
    mix_into(buf, noise(0.012, rng, release=0.01, cutoff=6000, gain=0.25), 0)
    return buf


def sfx_book_page(rng):
    # Sayfa hisirtisi: bant gecirgen gurultu, once kabarir sonra soner
    n = seconds(0.35)
    lp = Lowpass(3500)
    hp = Highpass(700)
    out = [0.0] * n
    for i in range(n):
        k = i / n
        amp = math.sin(math.pi * k) ** 1.5
        out[i] = hp(lp(rng.uniform(-1, 1))) * amp
    mix_into(out, noise(0.03, rng, release=0.025, cutoff=2000, gain=0.4), seconds(0.27))
    return out


EFFECTS = [
    ("sfx_dice_roll", sfx_dice_roll), ("sfx_dice_land", sfx_dice_land), ("sfx_key", sfx_key),
    ("sfx_word_ok", sfx_word_ok), ("sfx_word_bad", sfx_word_bad), ("sfx_spawn", sfx_spawn),
    ("sfx_explosion", sfx_explosion), ("sfx_hit", sfx_hit), ("sfx_shield", sfx_shield), ("sfx_heal", sfx_heal),
    ("sfx_fire", sfx_fire), ("sfx_water", sfx_water), ("sfx_fly", sfx_fly), ("sfx_siren", sfx_siren),
    ("sfx_eliminated", sfx_eliminated), ("sfx_victory", sfx_victory), ("sfx_defeat", sfx_defeat),
    ("sfx_ui_click", sfx_ui_click), ("sfx_book_page", sfx_book_page),
]


# --------------------------------------------------------------------------------------------
# Muzik: dongu tamponuna notalar 'sarilarak' yazilir; son notanin kuyrugu basa tasar, dongu kesintisiz olur
# --------------------------------------------------------------------------------------------

CHORDS = {
    "C": [48, 52, 55], "Am": [45, 48, 52], "F": [41, 45, 48], "G": [43, 47, 50], "Em": [40, 43, 47],
    "Dm": [38, 41, 45], "Bb": [46, 50, 53], "A": [45, 49, 52], "Gm": [43, 46, 50], "Eb": [39, 43, 46],
}


class Song:
    def __init__(self, bpm, bars):
        self.beat = 60.0 / bpm
        self.length = int(round(bars * 4 * self.beat * SAMPLE_RATE))
        self.buf = [0.0] * self.length

    def at(self, beat):
        return int(round(beat * self.beat * SAMPLE_RATE)) % self.length

    def note(self, beat, beats, midi, kind, gain, duty=0.5, attack=0.005, release=0.06, vibrato=0.0):
        dur = beats * self.beat
        mix_into(self.buf, tone(midi_to_freq(midi), dur, kind, duty=duty, attack=attack, release=min(release, dur * 0.8),
                                vibrato=vibrato), self.at(beat), gain, wrap=True)

    def kick(self, beat, gain):
        mix_into(self.buf, tone(130, 0.16, "sine", attack=0.001, release=0.12, slide_to=40), self.at(beat), gain, wrap=True)

    def snare(self, beat, rng, gain):
        s = noise(0.14, rng, attack=0.001, release=0.12, cutoff=5000, hold=2)
        mix_into(s, tone(190, 0.08, "triangle", release=0.06, gain=0.5), 0)
        mix_into(self.buf, s, self.at(beat), gain, wrap=True)

    def hat(self, beat, rng, gain, dur=0.03):
        hp = Highpass(6000)
        s = [hp(v) for v in noise(dur, rng, attack=0.0005, release=dur * 0.8)]
        mix_into(self.buf, s, self.at(beat), gain, wrap=True)

    def tom(self, beat, gain, pitch=90):
        mix_into(self.buf, tone(pitch, 0.3, "triangle", attack=0.002, release=0.25, slide_to=pitch * 0.55),
                 self.at(beat), gain, wrap=True)


def melody_line(song, rng, chords, bar_beats, rhythms, low, high, kind, gain, duty=0.25, start_note=None, vibrato=0.0):
    """Akor seslerinden, cogunlukla adim adim ilerleyen basit bir melodi uretir (seed'li, hep ayni)."""
    prev = start_note
    for bar, chord in enumerate(chords):
        tones = sorted({n + 12 * o for n in CHORDS[chord] for o in range(-1, 5) if low <= n + 12 * o <= high})
        rhythm = rhythms[bar % len(rhythms)]
        for offset, length in rhythm:
            if prev is None:
                pick = tones[len(tones) // 2]
            else:
                near = sorted(tones, key=lambda n: abs(n - prev))[:3]
                pick = rng.choice(near) if rng.random() < 0.8 else rng.choice(tones)
            prev = pick
            song.note(bar * bar_beats + offset, length, pick, kind, gain, duty=duty, release=0.08, vibrato=vibrato)


def music_menu(rng):
    song = Song(bpm=84, bars=8)
    chords = ["C", "Am", "F", "G", "C", "Em", "F", "G"]
    for bar, chord in enumerate(chords):
        root = CHORDS[chord][0]
        song.note(bar * 4, 1.9, root - 12, "triangle", 0.5, release=0.3)
        song.note(bar * 4 + 2, 1.9, root - 5, "triangle", 0.4, release=0.3)
        # Yumusak arpej
        notes = CHORDS[chord] + [CHORDS[chord][0] + 12]
        for k in range(8):
            song.note(bar * 4 + k * 0.5, 0.45, notes[k % 4] + 12, "square", 0.07, duty=0.125, release=0.15)
        song.hat(bar * 4 + 1.5, rng, 0.05)
        song.hat(bar * 4 + 3.5, rng, 0.05)
    rhythms = [[(0, 1.5), (1.5, 0.5), (2, 2)], [(0, 1), (1, 1), (2, 1.5), (3.5, 0.5)], [(0, 3), (3, 1)], [(0, 2), (2, 2)]]
    melody_line(song, rng, chords, 4, rhythms, 64, 81, "triangle", 0.35, vibrato=0.004)
    return song.buf


def music_match(rng):
    song = Song(bpm=140, bars=16)
    chords = ["Am", "F", "C", "G"] * 4
    for bar, chord in enumerate(chords):
        root = CHORDS[chord][0] - 12
        pattern = [0, 12, 0, 12, 0, 12, 7, 12]
        for k, off in enumerate(pattern):
            song.note(bar * 4 + k * 0.5, 0.45, root + off, "triangle", 0.5, release=0.05)
        # Davul: kick 1-3, snare 2-4, hat sekizlik
        song.kick(bar * 4 + 0, 0.8)
        song.kick(bar * 4 + 2, 0.8)
        if bar % 4 == 3:
            song.kick(bar * 4 + 3.5, 0.6)
        song.snare(bar * 4 + 1, rng, 0.35)
        song.snare(bar * 4 + 3, rng, 0.35)
        for k in range(8):
            song.hat(bar * 4 + k * 0.5, rng, 0.08 if k % 2 else 0.05)
        # Ikinci yarida hafif onaltilik arpej
        if bar >= 8:
            notes = CHORDS[chord]
            for k in range(16):
                song.note(bar * 4 + k * 0.25, 0.22, notes[k % 3] + 24, "square", 0.05, duty=0.125, release=0.05)
    rhythm_a = [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 0.5), (2.5, 0.5), (3, 1)], [(0, 1.5), (1.5, 0.5), (2, 2)]]
    rhythm_b = [[(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 1), (3, 0.5), (3.5, 0.5)], [(0, 1), (1, 1), (2, 2)]]
    melody_line(song, rng, chords[:8], 4, rhythm_a, 64, 83, "square", 0.22, duty=0.25)
    # Ikinci yari (B bolumu) melodisi ayri bir tampona yazilip 9. olcuden itibaren eklenir
    second = Song(bpm=140, bars=8)
    melody_line(second, rng, chords[8:], 4, rhythm_b, 67, 86, "square", 0.22, duty=0.25)
    mix_into(song.buf, second.buf, song.at(32), 1.0, wrap=True)
    return song.buf


def music_threat(rng):
    song = Song(bpm=120, bars=12)
    chords = ["Dm", "Dm", "Bb", "A", "Dm", "Dm", "Gm", "A", "Bb", "Gm", "A", "A"]
    for bar, chord in enumerate(chords):
        root = CHORDS[chord][0] - 12 if CHORDS[chord][0] > 40 else CHORDS[chord][0]
        # Israrci sekizlik bas, son vuruşta yarim ton gerilim
        for k in range(8):
            off = 1 if k == 7 else 0
            song.note(bar * 4 + k * 0.5, 0.4, root + off, "square", 0.22, duty=0.5, release=0.05)
        # Alcak uzun ses (drone)
        song.note(bar * 4, 3.9, root + 12, "triangle", 0.25, attack=0.2, release=0.4)
        # Tom ve tikirti
        song.tom(bar * 4, 0.7)
        song.tom(bar * 4 + 2.5, 0.45, 110)
        if bar % 2 == 1:
            song.tom(bar * 4 + 3.5, 0.45, 120)
        for k in range(16):
            song.hat(bar * 4 + k * 0.25, rng, 0.06 if k % 4 == 2 else 0.025, dur=0.02)
    # Seyrek, yuksek gerilim melodisi
    rhythms = [[(0, 2), (2, 2)], [(0, 3), (3, 1)], [(0, 1), (1, 1), (2, 2)]]
    melody_line(song, rng, chords, 4, rhythms, 69, 86, "square", 0.13, duty=0.125, vibrato=0.006)
    return song.buf


MUSIC = [("music_menu", music_menu), ("music_match", music_match), ("music_threat", music_threat)]


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, make in EFFECTS:
        write_wav(name, make(random.Random(f"{SEED}-{name}")))
    for name, make in MUSIC:
        write_wav(name, make(random.Random(f"{SEED}-{name}")), loop=True)


if __name__ == "__main__":
    main()
