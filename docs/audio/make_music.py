#!/usr/bin/env python3
"""Overgrown music (replaces the 8-bit chip tracks): warm acoustic-style tunes for nature / ruins / water levels.

Pure python, deterministic. Voices: harp / kalimba / marimba / music-box plucks, a soft flute, a sine pad, a round bass,
hand-drum + shaker + water drops. Melodies are built from the mode's pentatonic notes (always consonant), phrased A A' B A'',
16 bars, looping seamlessly (the reverb is rendered over two passes and the second one is kept). No vibrato, chorus or detune.

Usage:  cd docs/audio && python3 make_music.py [track ...]      (default: all)
Writes  Game/Assets/Audio/Music/{music_menu,music_gameplay,music_autumn,music_rain,music_l4..music_l10}.wav
"""
import math, os, random, sys
from make_audio import SR, TAU, ROOT, hz, buf, add, write

MAJOR = (0, 2, 4, 5, 7, 9, 11); MINOR = (0, 2, 3, 5, 7, 8, 10); DORIAN = (0, 2, 3, 5, 7, 9, 10)
LYDIAN = (0, 2, 4, 6, 7, 9, 11); MIXO = (0, 2, 4, 5, 7, 9, 10)
PENT = {MAJOR: (0, 1, 2, 4, 5), LYDIAN: (0, 1, 2, 4, 5), MIXO: (0, 1, 2, 4, 5), MINOR: (0, 2, 3, 4, 6), DORIAN: (0, 2, 3, 4, 5)}


# ------------------------------------------------------------------ voices
def _env(t, dur, a, rel=0.04):
    return min(1.0, t / a) * min(1.0, max(0.0, (dur - t) / rel))


def pluck(freq, dur, vol=1.0, bright=1.0, decay=3.2):
    """harp-like: harmonic partials, the upper ones die faster"""
    n = int(dur * SR); out = []
    for i in range(n):
        t = i / SR
        v = (math.sin(TAU * freq * t) * math.exp(-decay * t)
             + 0.35 * bright * math.sin(TAU * 2 * freq * t) * math.exp(-decay * 1.8 * t)
             + 0.12 * bright * math.sin(TAU * 3 * freq * t) * math.exp(-decay * 2.8 * t))
        out.append(v * vol * min(1.0, t / 0.003) * min(1.0, max(0.0, (dur - t) / 0.03)))
    return out


def kalimba(freq, dur, vol=1.0):
    n = int(dur * SR); out = []
    for i in range(n):
        t = i / SR
        v = math.sin(TAU * freq * t) * math.exp(-5.0 * t) + 0.25 * math.sin(TAU * 4 * freq * t) * math.exp(-14 * t)
        out.append(v * vol * min(1.0, t / 0.002) * min(1.0, max(0.0, (dur - t) / 0.03)))
    return out


def marimba(freq, dur, vol=1.0):
    n = int(dur * SR); out = []
    for i in range(n):
        t = i / SR
        v = math.sin(TAU * freq * t) * math.exp(-6.0 * t) + 0.4 * math.sin(TAU * 4 * freq * t) * math.exp(-18 * t)
        out.append(v * vol * min(1.0, t / 0.002) * min(1.0, max(0.0, (dur - t) / 0.02)))
    return out


def musicbox(freq, dur, vol=1.0):
    n = int(dur * SR); out = []
    for i in range(n):
        t = i / SR
        v = math.sin(TAU * freq * t) * math.exp(-3.0 * t) + 0.3 * math.sin(TAU * 2 * freq * t) * math.exp(-5 * t) + 0.12 * math.sin(TAU * 3 * freq * t) * math.exp(-8 * t)
        out.append(v * vol * min(1.0, t / 0.002) * min(1.0, max(0.0, (dur - t) / 0.03)))
    return out


def flute(freq, dur, vol=1.0, rng=None):
    n = int(dur * SR); out = []; lp = 0.0
    a = 0.07; rel = min(0.12, dur * 0.4)
    for i in range(n):
        t = i / SR
        br = 0.0
        if rng is not None:
            lp += (rng.uniform(-1, 1) - lp) * 0.2; br = lp * 0.05
        v = math.sin(TAU * freq * t) + 0.18 * math.sin(TAU * 2 * freq * t) + 0.05 * math.sin(TAU * 3 * freq * t) + br
        out.append(v * vol * _env(t, dur, a, rel) * (0.75 + 0.25 * math.exp(-2.0 * t)))
    return out


def pad(freqs, dur, vol=1.0):
    """pure stacked sines, slow swell (no detune, so it never swirls)"""
    n = int(dur * SR); out = []
    a = min(1.2, dur * 0.35); rel = min(1.4, dur * 0.4)
    for i in range(n):
        t = i / SR
        s = sum(math.sin(TAU * f * t) + 0.2 * math.sin(TAU * 2 * f * t) for f in freqs) / len(freqs)
        out.append(s * vol * min(1.0, t / a) * min(1.0, max(0.0, (dur - t) / rel)))
    return out


def bass(freq, dur, vol=1.0):
    n = int(dur * SR); out = []
    for i in range(n):
        t = i / SR
        v = math.sin(TAU * freq * t) + 0.25 * math.sin(TAU * 2 * freq * t) * math.exp(-4 * t)
        out.append(v * vol * min(1.0, t / 0.01) * math.exp(-1.6 * t) * min(1.0, max(0.0, (dur - t) / 0.05)))
    return out


def drum(rng, kind):
    if kind == "low":      # soft hand drum
        return [math.sin(TAU * (95 + 70 * math.exp(-i / SR * 40)) * i / SR) * math.exp(-i / SR * 14) for i in range(int(0.22 * SR))]
    if kind == "high":
        return [math.sin(TAU * (210 + 90 * math.exp(-i / SR * 50)) * i / SR) * math.exp(-i / SR * 28) * 0.7 for i in range(int(0.12 * SR))]
    if kind == "wood":
        return [math.sin(TAU * 820 * i / SR) * math.exp(-i / SR * 70) * 0.6 for i in range(int(0.06 * SR))]
    lp = 0.0; out = []   # shaker: band-limited noise burst
    for i in range(int(0.07 * SR)):
        x = rng.uniform(-1, 1); lp += (x - lp) * 0.6
        out.append((x - lp) * math.exp(-i / SR * 55) * 0.5 * min(1.0, i / 60))
    return out


def drop(freq):
    """water plink: a short sine that glides up"""
    n = int(0.35 * SR); out = []; ph = 0.0
    for i in range(n):
        t = i / SR
        ph += TAU * freq * (1 + 0.5 * (1 - math.exp(-t * 40))) / SR
        out.append(math.sin(ph) * math.exp(-t * 11) * min(1.0, t / 0.002))
    return out


def reverb(x, wet, seconds=1.0):
    """Schroeder reverb; two passes over the loop, the second is returned, so the tail wraps seamlessly"""
    n = len(x); y = x + x
    combs = [(int(SR * d), g) for d, g in ((0.0297, 0.80), (0.0371, 0.78), (0.0411, 0.76), (0.0437, 0.74))]
    acc = [0.0] * len(y)
    for d, g in combs:
        g = g ** (1.0 / max(0.3, seconds)); b = [0.0] * d; lp = 0.0
        for i, s in enumerate(y):
            o = b[i % d]; lp += (o - lp) * 0.45          # damped feedback: darker tail
            b[i % d] = s + lp * g
            acc[i] += o
    for d, g in ((int(SR * 0.005), 0.7), (int(SR * 0.0017), 0.7)):
        b = [0.0] * d
        for i, s in enumerate(acc):
            o = b[i % d]; v = s + o * g; b[i % d] = v; acc[i] = o - v * g
    return [x[i] * (1 - wet * 0.5) + acc[n + i] * wet * 0.25 for i in range(n)]


# ------------------------------------------------------------------ composition
def compose(name, bpm, tonic, scale, prog, seed, lead="harp", accomp="arp", pad_vol=0.5, bass_style="root", perc=None,
            melody_oct=0, density=1.0, drops=0.0, wet=0.3, rev=1.0, rests=0.12):
    """prog: 4 scale degrees (chord roots, diatonic triads) of 2 bars each, played twice = 16 bars."""
    rng = random.Random(seed)
    beat = 60.0 / bpm; step = beat / 2                    # eighth note
    bars = 16; loop = bars * 4 * beat
    out = buf(loop)
    deg = lambda i: tonic + scale[i % 7] + 12 * (i // 7)
    pent = PENT[scale]
    pool = [d + 7 * o for o in (0, 1, 2) for d in pent]    # pentatonic scale-degree indices over 3 octaves
    base = 7 * 1 + 7 * melody_oct                           # lowest melody degree index ~ tonic + 12 (+12)
    pool = [p for p in pool if base - 2 <= p + 7 <= base + 10]
    chords = [[p, p + 2, p + 4] for p in prog]

    def chord_for_bar(bar): return chords[(bar // 2) % 4]
    rhythm_pool = {
        "calm":  [[(0, 3), (3, 1), (4, 3), (7, 1)], [(0, 2), (2, 2), (4, 4)], [(0, 4), (4, 2), (6, 2)], [(0, 2), (2, 1), (3, 1), (4, 4)]],
        "lilt":  [[(0, 2), (2, 1), (3, 1), (4, 2), (6, 2)], [(0, 1), (1, 1), (2, 2), (4, 2), (6, 2)], [(0, 3), (3, 1), (4, 2), (6, 1), (7, 1)]],
        "drive": [[(0, 1), (1, 1), (2, 1), (3, 1), (4, 2), (6, 2)], [(0, 2), (2, 1), (3, 1), (4, 1), (5, 1), (6, 2)], [(0, 1), (1, 1), (2, 2), (4, 1), (5, 1), (6, 2)]],
    }
    rp = rhythm_pool["drive" if density >= 1.4 else "lilt" if density >= 0.9 else "calm"]

    def phrase(first_idx):
        notes = []; cur = first_idx
        for b in range(4):
            rhy = rng.choice(rp)
            chord = chord_for_bar(b if b < 2 else b)       # phrase starts on a chord pair
            for k, (pos, ln) in enumerate(rhy):
                if k == 0:
                    tones = [p for p in pool if (p % 7) in {c % 7 for c in chord_for_bar(b)}] or pool
                    cur = min(tones, key=lambda p: abs(p - cur) + rng.random())
                else:
                    cands = [p for p in pool if p != cur and abs(p - cur) <= 4]
                    cur = min(cands, key=lambda p: abs(p - cur) + rng.random() * 2.2) if cands else cur
                notes.append((b, pos, ln, cur, rng.random() < rests and k > 0))
        return notes

    start = pool[len(pool) // 2]
    A = phrase(start); B = phrase(pool[len(pool) // 2 + 1])
    Ap = [(b, p, l, n, r) for (b, p, l, n, r) in A]
    # A' : same tune, last bar re-voiced; A'': same tune, ends on the tonic
    last = max(x[0] for x in A)
    Ap = [x if x[0] != last else (x[0], x[1], x[2], pool[min(range(len(pool)), key=lambda j: abs(pool[j] - (x[3] + 2)))], x[4]) for x in A]
    tonic_idx = min(pool, key=lambda p: abs(p - start))
    App = [x for x in A[:-1]] + [(last, 0, 8, [p for p in pool if p % 7 == 0][0], False)]
    sections = [A, Ap, B, App]

    inst = {"harp": lambda f, d, v: pluck(f, d, v, 1.0, 3.0), "kalimba": kalimba, "marimba": marimba, "box": musicbox,
            "flute": lambda f, d, v: flute(f, d, v, rng), "harp_soft": lambda f, d, v: pluck(f, d, v, 0.6, 2.2)}[lead]
    for si, sec in enumerate(sections):
        for (b, pos, ln, idx, rest) in sec:
            if rest: continue
            bar = si * 4 + b
            at = (bar * 4 * beat) + pos * step
            dur = ln * step * (1.0 if lead == "flute" else 1.6)
            add(out, inst(hz(deg(idx)), dur, 0.55), at, wrap=True)

    for bar in range(bars):
        ch = chord_for_bar(bar); at = bar * 4 * beat
        if bar % 2 == 0 and pad_vol > 0:
            add(out, pad([hz(deg(c) - 12 + 12) for c in ch], 8 * beat * 1.0 + 0.6, pad_vol * 0.35), at, wrap=True)
        notes = [deg(c) for c in ch]
        if accomp == "arp":
            order = [0, 1, 2, 1, 0, 1, 2, 1]
            for k, o in enumerate(order): add(out, pluck(hz(notes[o]), step * 3, 0.17, 0.5, 4.0), at + k * step, wrap=True)
        elif accomp == "slow":
            for k, o in enumerate((0, 2, 1, 2)): add(out, pluck(hz(notes[o] + 12), beat * 1.6, 0.15, 0.5, 2.4), at + k * beat, wrap=True)
        elif accomp == "strum":
            for k in (0, 3):
                for j, nn in enumerate(notes): add(out, pluck(hz(nn), beat * 1.5, 0.15, 0.6, 3.0), at + k * beat * 0.5 + j * 0.03, wrap=True)
        r = deg(ch[0]) - 12
        if bass_style == "root":
            for k, off in ((0, 0), (2, 7 if bar % 2 else 0)): add(out, bass(hz(r + off), beat * 1.8, 0.45), at + k * beat, wrap=True)
        elif bass_style == "walk":
            for k, nn in enumerate((r, deg(ch[1]) - 12, deg(ch[2]) - 12, deg(ch[1]) - 12)): add(out, bass(hz(nn), beat * 0.95, 0.4), at + k * beat, wrap=True)
        elif bass_style == "drone":
            add(out, bass(hz(r), beat * 3.9, 0.4), at, wrap=True)
        elif bass_style == "bounce":
            for k in range(4): add(out, bass(hz(r + (12 if k % 2 else 0)), beat * 0.9, 0.38), at + k * beat, wrap=True)
        if perc:
            for k in range(8):
                t = at + k * step
                if perc in ("soft", "drive"):
                    if k == 0 or (perc == "drive" and k == 4): add(out, drum(rng, "low"), t, 0.4, wrap=True)
                    if k in (2, 6) and perc == "drive": add(out, drum(rng, "high"), t, 0.25, wrap=True)
                    if perc == "soft" and k == 4: add(out, drum(rng, "high"), t, 0.22, wrap=True)
                    if perc == "drive" or k % 2: add(out, drum(rng, "shaker"), t, 0.2 if k % 2 else 0.12, wrap=True)
                elif perc == "wood":
                    if k in (0, 3, 6): add(out, drum(rng, "wood"), t, 0.35, wrap=True)
                    if k == 4: add(out, drum(rng, "low"), t, 0.3, wrap=True)
                elif perc == "shaker":
                    if k % 2: add(out, drum(rng, "shaker"), t, 0.14, wrap=True)
    if drops:
        for _ in range(int(drops * bars)):
            note = deg(rng.choice(pool)) + 12
            add(out, drop(hz(note)), rng.uniform(0, loop - 0.4), 0.2, wrap=True)
    out = reverb(out, wet, rev)
    write(os.path.join(ROOT, "Music", name + ".wav"), out, 0.55)


TRACKS = {
    # name: (bpm, tonic MIDI, mode, 4-chord progression, seed, kwargs)
    "music_gameplay": (112, 60, MAJOR, [0, 4, 5, 3], 7, dict(lead="kalimba", accomp="arp", bass_style="root", perc="soft", density=1.0, wet=0.28)),                 # level 1 + generated: sunny garden
    "music_menu":     (88, 67, MAJOR, [0, 5, 3, 4], 3, dict(lead="harp", accomp="slow", pad_vol=0.7, bass_style="drone", density=0.7, wet=0.4, rev=1.4)),             # title: calm
    "music_autumn":   (96, 62, MIXO, [0, 6, 3, 0], 33, dict(lead="flute", accomp="strum", bass_style="walk", perc="soft", density=0.9, wet=0.32)),                    # level 2: golden, folky
    "music_rain":     (84, 57, DORIAN, [0, 3, 0, 4], 21, dict(lead="harp_soft", accomp="slow", pad_vol=0.7, bass_style="drone", drops=0.9, density=0.7, wet=0.45, rev=1.5)),  # level 3: rain
    "music_l4":       (104, 62, LYDIAN, [0, 1, 4, 0], 41, dict(lead="flute", accomp="arp", pad_vol=0.7, bass_style="root", perc="shaker", density=0.8, wet=0.4, rev=1.5)),  # Skyfall: airy, soaring
    "music_l5":       (116, 65, MAJOR, [0, 5, 3, 4], 52, dict(lead="marimba", accomp="strum", bass_style="bounce", perc="wood", density=1.2, wet=0.22)),                # Stepping Stones: hopping
    "music_l6":       (100, 64, MAJOR, [0, 4, 5, 3], 63, dict(lead="flute", accomp="arp", pad_vol=0.6, bass_style="walk", perc="soft", density=0.9, wet=0.35)),         # There and Back: sunrise
    "music_l7":       (80, 71, MAJOR, [0, 3, 0, 4], 74, dict(lead="box", accomp="slow", pad_vol=0.7, bass_style="drone", density=0.7, wet=0.45, rev=1.5)),               # Snowed In: music box
    "music_l8":       (124, 57, DORIAN, [0, 6, 3, 4], 85, dict(lead="flute", accomp="arp", bass_style="bounce", perc="drive", density=1.4, wet=0.25)),                  # Windy Summit: driving
    "music_l9":       (72, 60, LYDIAN, [0, 1, 0, 4], 96, dict(lead="harp_soft", accomp="slow", pad_vol=0.9, bass_style="drone", drops=0.7, density=0.6, wet=0.55, rev=1.8)),  # Into the Mist: dreamy water
    "music_l10":      (116, 55, DORIAN, [0, 5, 6, 0], 107, dict(lead="marimba", accomp="arp", bass_style="bounce", perc="drive", density=1.3, wet=0.28)),                # Storm Season: energetic
}


def all_tracks(only=None):
    for name, (bpm, tonic, scale, prog, seed, kw) in TRACKS.items():
        if only and name not in only and name.replace("music_", "") not in only: continue
        compose(name, bpm, tonic, scale, prog, seed, **kw)


if __name__ == "__main__":
    all_tracks(sys.argv[1:] or None)
