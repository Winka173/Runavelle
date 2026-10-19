"""Runavelle audio: every SFX and music loop is synthesized here (numpy only, no samples, original work).
Run: python3 Tools/Audio/build_audio.py   -> Assets/Runavelle/Resources/Audio/*.wav|*.ogg"""
import os

import numpy as np
import soundfile as sf

SR = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Runavelle", "Resources", "Audio")
rng = np.random.default_rng(7)


def t(sec):
    return np.arange(int(sec * SR)) / SR


def env(n, a=0.005, d=0.1, s=0.0, r=0.05, hold=0.0):
    a_n, d_n, r_n, h_n = int(a * SR), int(d * SR), int(r * SR), int(hold * SR)
    sus = max(0, n - a_n - d_n - r_n - h_n)
    e = np.concatenate([np.linspace(0, 1, a_n, False), np.ones(h_n), np.linspace(1, s, d_n, False), np.full(sus, s), np.linspace(s, 0, r_n)])
    return np.pad(e, (0, max(0, n - len(e))))[:n]


def osc(freq, dur, kind="sine"):
    tt = t(dur)
    f = np.full_like(tt, freq) if np.isscalar(freq) else freq
    ph = 2 * np.pi * np.cumsum(f) / SR
    if kind == "sine":
        return np.sin(ph)
    if kind == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    if kind == "saw":
        return 2 * ((ph / (2 * np.pi)) % 1) - 1
    if kind == "square":
        return np.sign(np.sin(ph))
    raise ValueError(kind)


def noise(dur):
    return rng.uniform(-1, 1, int(dur * SR))


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def lp_fast(x, cutoff):
    # one-pole via cumulative filter using scipy-free approach: repeated moving average approximation
    k = max(1, int(SR / cutoff / 2))
    ker = np.ones(k) / k
    return np.convolve(x, ker, mode="same")


def norm(x, peak=0.85):
    m = np.max(np.abs(x)) or 1
    return x / m * peak


def save(name, x, ogg=False):
    os.makedirs(OUT, exist_ok=True)
    x = np.clip(x, -1, 1).astype(np.float32)
    if ogg:
        sf.write(os.path.join(OUT, name + ".ogg"), x, SR, format="OGG", subtype="VORBIS")
    else:
        sf.write(os.path.join(OUT, name + ".wav"), x, SR, subtype="PCM_16")


def mtof(m):
    return 440.0 * 2 ** ((m - 69) / 12)


# ------------------------------------------------------------------------------------------------ SFX
def sfx():
    n = lambda d: int(d * SR)
    # ui click: soft wooden tick
    x = osc(1800, 0.06, "tri") * env(n(0.06), 0.001, 0.05) + noise(0.06) * env(n(0.06), 0.001, 0.02) * 0.3
    save("ui_click", norm(x, 0.5))
    # hit: thud + crunch
    x = osc(np.linspace(180, 60, n(0.12)), 0.12) * env(n(0.12), 0.001, 0.1) + lp_fast(noise(0.12), 2500) * env(n(0.12), 0.001, 0.05) * 0.8
    save("hit", norm(x, 0.6))
    # death: poof
    x = lp_fast(noise(0.35), 1200) * env(n(0.35), 0.01, 0.3) + osc(np.linspace(300, 90, n(0.35)), 0.35, "tri") * env(n(0.35), 0.005, 0.25) * 0.4
    save("death", norm(x, 0.55))
    # swing: whoosh
    x = lp_fast(noise(0.18), 3000) * np.sin(np.linspace(0, np.pi, n(0.18)))
    save("swing", norm(x, 0.4))
    # arrow: twang + whistle
    x = osc(np.linspace(900, 600, n(0.15)), 0.15, "tri") * env(n(0.15), 0.001, 0.12) * 0.6 + lp_fast(noise(0.15), 5000) * env(n(0.15), 0.01, 0.1) * 0.3
    save("arrow", norm(x, 0.4))
    # magic: sparkle sweep
    f = np.linspace(600, 1500, n(0.3))
    x = (osc(f, 0.3) + 0.5 * osc(f * 1.5, 0.3)) * env(n(0.3), 0.01, 0.28)
    save("magic", norm(x, 0.4))
    # zap: electric buzz
    x = osc(np.linspace(1200, 300, n(0.22)), 0.22, "square") * env(n(0.22), 0.001, 0.2) * (0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 60 * t(0.22))))
    save("zap", norm(lp_fast(x, 4000), 0.4))
    # reaction: bright chord stab
    x = sum(osc(mtof(m), 0.6, "tri") for m in (72, 76, 79, 84)) * env(n(0.6), 0.003, 0.55) + lp_fast(noise(0.6), 3000) * env(n(0.6), 0.001, 0.08) * 0.5
    save("reaction", norm(x, 0.55))
    # breach: heavy crumble
    x = lp_fast(noise(1.0), 700) * env(n(1.0), 0.005, 0.9) + osc(np.linspace(90, 40, n(1.0)), 1.0) * env(n(1.0), 0.002, 0.7) * 0.8
    save("breach", norm(x, 0.85))
    # castle hit: stone boom
    x = osc(np.linspace(120, 50, n(0.4)), 0.4) * env(n(0.4), 0.001, 0.35) + lp_fast(noise(0.4), 900) * env(n(0.4), 0.001, 0.2) * 0.6
    save("castle_hit", norm(x, 0.75))
    # ultimate: rising swell + impact
    sw = osc(np.linspace(200, 900, n(0.5)), 0.5, "saw") * np.linspace(0, 1, n(0.5)) * 0.4
    imp = osc(np.linspace(160, 50, n(0.7)), 0.7) * env(n(0.7), 0.001, 0.6) + lp_fast(noise(0.7), 2000) * env(n(0.7), 0.001, 0.3) * 0.7
    save("ultimate", norm(np.concatenate([lp_fast(sw, 2500), imp]), 0.85))
    # dash: quick whoosh up
    x = lp_fast(noise(0.2), 4000) * env(n(0.2), 0.01, 0.18) + osc(np.linspace(400, 1200, n(0.2)), 0.2) * env(n(0.2), 0.01, 0.18) * 0.3
    save("dash", norm(x, 0.45))
    # summon: dark shimmer
    f = np.linspace(220, 110, n(0.8))
    x = (osc(f, 0.8, "saw") + osc(f * 1.01, 0.8, "saw")) * env(n(0.8), 0.05, 0.7)
    save("summon", norm(lp_fast(x, 1500), 0.5))
    # horn: night begins (brass-like)
    seg = []
    for m, d in ((50, 0.5), (57, 0.9)):
        tt = t(d)
        f0 = mtof(m)
        x = sum(osc(f0 * k, d, "saw") / k for k in (1, 2, 3)) * env(len(tt), 0.08, 0.2, 0.7, 0.25)
        seg.append(lp_fast(x, 1400))
    save("horn", norm(np.concatenate(seg), 0.7))
    # warning: two-tone ping
    x = np.concatenate([osc(880, 0.12) * env(n(0.12), 0.002, 0.1), osc(660, 0.18) * env(n(0.18), 0.002, 0.16)])
    save("warning", norm(x, 0.45))
    # build: hammer knocks
    seg = []
    for _ in range(3):
        k = osc(np.linspace(500, 250, n(0.07)), 0.07, "tri") * env(n(0.07), 0.001, 0.06) + noise(0.07) * env(n(0.07), 0.001, 0.02) * 0.4
        seg += [k, np.zeros(n(0.06))]
    save("build", norm(np.concatenate(seg), 0.55))
    # upgrade: arpeggio up
    seg = [osc(mtof(m), 0.09, "tri") * env(n(0.09), 0.002, 0.085) for m in (67, 71, 74, 79)]
    save("upgrade", norm(np.concatenate(seg), 0.5))
    # error
    x = osc(220, 0.18, "square") * env(n(0.18), 0.002, 0.16)
    save("error", norm(lp_fast(x, 1500), 0.35))
    # victory jingles
    def jingle(notes, dur):
        out = []
        for m in notes:
            out.append(sum(osc(mtof(m + k), dur, "tri") for k in (0, 12)) * env(n(dur), 0.005, dur * 0.9))
        return np.concatenate(out)
    save("victory_small", norm(jingle([72, 76, 79, 84], 0.14), 0.55))
    save("victory", norm(np.concatenate([jingle([67, 72, 76, 79], 0.18), jingle([84], 0.9)]), 0.65))
    save("defeat", norm(jingle([67, 63, 60, 55], 0.35), 0.55))


# ------------------------------------------------------------------------------------------------ music
def music_loop(name, bpm, chords, melody_scale, mood):
    beat = 60.0 / bpm
    bars = len(chords)
    total = bars * 4 * beat
    N = int(total * SR)
    mix = np.zeros(N)
    r = np.random.default_rng(hash(name) & 0xffff)

    def place(x, start):
        i = int(start * SR)
        j = min(N, i + len(x))
        mix[i:j] += x[: j - i]

    for b, chord in enumerate(chords):
        t0 = b * 4 * beat
        # pad
        dur = 4 * beat
        pad = sum(osc(mtof(m), dur, "tri") * 0.25 + osc(mtof(m) * 1.003, dur, "saw") * 0.06 for m in chord)
        place(lp_fast(pad, 1200 if mood != "boss" else 1800) * env(int(dur * SR), 0.3, 0.1, 0.9, 0.4), t0)
        # bass
        for k in range(4 if mood != "title" else 2):
            bd = beat * (1 if mood != "title" else 2)
            bass = osc(mtof(chord[0] - 12), bd * 0.9, "tri") * env(int(bd * 0.9 * SR), 0.01, 0.2, 0.5, 0.1)
            place(bass * 0.6, t0 + k * bd)
        # percussion
        if mood in ("night", "boss", "day"):
            for k in range(4):
                kick = osc(np.linspace(110, 45, int(0.18 * SR)), 0.18) * env(int(0.18 * SR), 0.001, 0.17)
                if mood == "day" and k % 2 == 1:
                    continue
                place(kick * (0.8 if mood == "boss" else 0.55), t0 + k * beat)
                if mood != "day":
                    hat = lp_fast(noise(0.05), 6000) * env(int(0.05 * SR), 0.001, 0.04)
                    place(hat * 0.18, t0 + k * beat + beat / 2)
            if mood == "boss":
                for k in (1, 3):
                    snare = lp_fast(noise(0.2), 3000) * env(int(0.2 * SR), 0.001, 0.15) + osc(190, 0.2, "tri") * env(int(0.2 * SR), 0.001, 0.1) * 0.4
                    place(snare * 0.45, t0 + k * beat)
        # melody (pluck)
        steps = 8
        for k in range(steps):
            if r.random() < (0.35 if mood == "title" else 0.55):
                m = int(r.choice(melody_scale)) + (12 if r.random() < 0.3 else 0)
                d = beat / 2 * (2 if r.random() < 0.3 else 1)
                pl = (osc(mtof(m), d, "tri") + 0.3 * osc(mtof(m) * 2, d)) * env(int(d * SR), 0.003, d * 0.9)
                place(pl * 0.28, t0 + k * beat / 2)
    # gentle reverb-ish echo
    delay = int(beat * 0.75 * SR)
    echo = np.zeros(N)
    echo[delay:] = mix[:-delay] * 0.28
    mix = mix + echo
    # seamless loop: crossfade tail into head
    fade = int(0.25 * SR)
    mix[:fade] = mix[:fade] * np.linspace(0.6, 1, fade)
    mix[-fade:] = mix[-fade:] * np.linspace(1, 0.6, fade)
    save(name, norm(mix, 0.7), ogg=True)


def music():
    # chords as MIDI notes; original progressions
    music_loop("music_title", 72, [[57, 60, 64], [53, 57, 60], [55, 59, 62], [52, 55, 59]] * 2, [69, 71, 72, 74, 76, 79], "title")
    music_loop("music_day", 96, [[60, 64, 67], [57, 60, 64], [53, 57, 60], [55, 59, 62]] * 2, [72, 74, 76, 79, 81], "day")
    music_loop("music_night", 108, [[57, 60, 64], [57, 60, 64], [53, 57, 60], [52, 56, 59]] * 2, [69, 71, 72, 76, 77], "night")
    music_loop("music_boss", 132, [[50, 53, 57], [50, 53, 58], [48, 52, 55], [49, 52, 56]] * 2, [62, 63, 65, 67, 69, 70], "boss")


if __name__ == "__main__":
    sfx()
    music()
    files = sorted(os.listdir(OUT))
    print(len(files), "audio files ->", os.path.abspath(OUT))
