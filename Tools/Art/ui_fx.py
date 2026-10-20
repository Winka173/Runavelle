"""Icons, element glyphs (shape + colour, colour-blind safe), VFX sprites, UI panels and the title backdrop."""
import math
import os
import random

import skia

from artlib import Canvas, INK, rgb, lighten, darken, mix, star, OUT_ROOT
from terrain_buildings import ELEMENT_COL, GRASS, ROCK, SNOW, STONE, WOOD, LEAF

FONT_DIR = os.path.join(OUT_ROOT, "..", "Fonts")


def typeface(name):
    return skia.Typeface.MakeFromFile(os.path.join(FONT_DIR, name))


# ------------------------------------------------------------------------------------------------ glyphs
def glyph(cv, kind, cx, cy, r, col, ink=2.5):
    if kind == 'flame':
        cv.smooth([(cx, cy - r), (cx + r * 0.55, cy - r * 0.1), (cx + r * 0.6, cy + r * 0.45), (cx, cy + r * 0.8), (cx - r * 0.6, cy + r * 0.45), (cx - r * 0.45, cy - r * 0.15), (cx - r * 0.1, cy + r * 0.05)], col, ink=ink)
        cv.smooth([(cx, cy - r * 0.2), (cx + r * 0.3, cy + r * 0.3), (cx, cy + r * 0.6), (cx - r * 0.3, cy + r * 0.3)], lighten(col, 0.6), ink=0)
    elif kind == 'drop':
        cv.smooth([(cx, cy - r), (cx + r * 0.6, cy + r * 0.15), (cx + r * 0.45, cy + r * 0.7), (cx, cy + r * 0.85), (cx - r * 0.45, cy + r * 0.7), (cx - r * 0.6, cy + r * 0.15)], col, ink=ink)
    elif kind == 'bolt':
        cv.poly([(cx + r * 0.2, cy - r), (cx - r * 0.55, cy + r * 0.1), (cx - r * 0.05, cy + r * 0.1), (cx - r * 0.25, cy + r), (cx + r * 0.55, cy - r * 0.15), (cx + r * 0.05, cy - r * 0.15)], col, ink=ink)
    elif kind == 'flake':
        for a in range(6):
            ang = a * math.pi / 3
            cv.inked_line(cx, cy, cx + math.cos(ang) * r, cy + math.sin(ang) * r, col, r * 0.22)
        cv.circle(cx, cy, r * 0.22, lighten(col, 0.5))
    elif kind == 'swirl':
        p = skia.Path()
        for i in range(40):
            t = i / 39 * 4.2
            rr = r * (0.15 + 0.85 * i / 39)
            x, y = cx + math.cos(t) * rr, cy + math.sin(t) * rr
            p.moveTo(x, y) if i == 0 else p.lineTo(x, y)
        cv.c.drawPath(p, cv.paint(INK, stroke=r * 0.32 + 3))
        cv.c.drawPath(p, cv.paint(col, stroke=r * 0.32))
    elif kind == 'rock':
        cv.poly([(cx - r * 0.85, cy + r * 0.55), (cx - r * 0.55, cy - r * 0.4), (cx, cy - r * 0.8), (cx + r * 0.7, cy - r * 0.3), (cx + r * 0.85, cy + r * 0.55)], col, ink=ink)
        cv.line(cx - r * 0.1, cy - r * 0.6, cx + r * 0.1, cy + r * 0.5, darken(col, 0.4), 2)
    elif kind == 'star':
        cv.poly(star(cx, cy, r, r * 0.45, 4), col, ink=ink)
    elif kind == 'eye':
        p = skia.Path()
        p.moveTo(cx - r, cy)
        p.quadTo(cx, cy - r * 0.95, cx + r, cy)
        p.quadTo(cx, cy + r * 0.95, cx - r, cy)
        p.close()
        cv.shape(p, col, ink=ink)
        cv.circle(cx, cy, r * 0.35, darken(col, 0.6))
        cv.circle(cx - r * 0.1, cy - r * 0.1, r * 0.1, (255, 255, 255))
    elif kind == 'dot':
        cv.circle(cx, cy, r * 0.5, col)


EL_GLYPH = {'fire': 'flame', 'water': 'drop', 'lightning': 'bolt', 'ice': 'flake', 'wind': 'swirl', 'earth': 'rock', 'light': 'star', 'shadow': 'eye', 'none': 'dot'}


def icons():
    for el, g in EL_GLYPH.items():
        cv = Canvas(64, 64)
        if el == 'shadow':
            cv.circle(32, 32, 30.5, rgb('#d9ccff'))  # pale rim: a dark-violet disc vanished on night ground (validator: readability)
        cv.circle(32, 32, 28, INK)
        cv.circle(32, 32, 25, darken(ELEMENT_COL[el], 0.55))
        glyph(cv, g, 32, 33, 17, ELEMENT_COL[el])
        cv.save(f"icon/el_{el}")
    simple = {
        'gold': lambda cv: (cv.ellipse(32, 36, 20, 20, rgb('#e8b64a')), cv.ellipse(32, 36, 13, 13, rgb('#f6d27a'), ink=2), cv.text("V", 25, 44, 22, darken(rgb('#e8b64a'), 0.5))),
        'mana': lambda cv: glyph(cv, 'drop', 32, 34, 24, rgb('#5fb0ff')),
        'heart': lambda cv: cv.smooth([(32, 54), (10, 30), (16, 14), (32, 22), (48, 14), (54, 30)], rgb('#e0505a')),
        'castle': lambda cv: (cv.rect(14, 24, 36, 30, STONE), cv.poly([(10, 26), (32, 6), (54, 26)], rgb('#4e74c4'))),
        'sword': lambda cv: (cv.inked_line(16, 48, 46, 18, rgb('#d9dee8'), 6), cv.inked_line(14, 36, 28, 50, rgb('#e8c25a'), 5)),
        'shield': lambda cv: cv.smooth([(14, 14), (50, 14), (50, 34), (32, 54), (14, 34)], rgb('#4e74c4')),
        'skull': lambda cv: (cv.ellipse(32, 30, 18, 18, (236, 230, 214)), cv.circle(25, 30, 4, INK), cv.circle(39, 30, 4, INK), cv.rect(24, 44, 16, 8, (236, 230, 214), r=2, ink=2)),
        'flyer': lambda cv: (cv.poly([(32, 30), (6, 16), (14, 34), (32, 40), (50, 34), (58, 16)], rgb('#9a7cc0'))),
        'siege': lambda cv: (cv.rect(10, 26, 44, 18, WOOD), cv.ellipse(18, 48, 7, 7, darken(WOOD, 0.3)), cv.ellipse(46, 48, 7, 7, darken(WOOD, 0.3)), cv.ellipse(54, 34, 6, 8, ROCK)),
        'boss': lambda cv: (cv.poly([(10, 46), (12, 16), (22, 30), (32, 10), (42, 30), (52, 16), (54, 46)], rgb('#e8c25a'))),
        'burrow': lambda cv: (cv.ellipse(32, 44, 22, 8, darken(rgb('#c8a073'), 0.3)), cv.poly([(20, 44), (32, 14), (44, 44)], rgb('#c8a073'))),
        'stealth': lambda cv: glyph(cv, 'eye', 32, 32, 22, rgb('#7a6aa0')),
        'drain': lambda cv: (glyph(cv, 'drop', 32, 34, 20, rgb('#5fb0ff')), cv.inked_line(14, 50, 50, 14, rgb('#e0505a'), 5)),
        'armor': lambda cv: (cv.smooth([(14, 14), (50, 14), (50, 34), (32, 54), (14, 34)], rgb('#9aa6b8')), cv.rect(26, 22, 12, 20, rgb('#6a7488'), r=2, ink=2)),
        'elite': lambda cv: cv.poly(star(32, 32, 24, 11), rgb('#d04a6a')),
        'summon': lambda cv: (cv.ellipse(32, 44, 22, 8, rgb('#7a5ab0')), cv.ellipse(32, 26, 9, 9, (236, 230, 214))),
        'swarm': lambda cv: [cv.ellipse(x, y, 7, 7, rgb('#7fb04a')) for x, y in ((20, 24), (40, 22), (30, 38), (16, 44), (46, 42))],
        'ult': lambda cv: cv.poly(star(32, 32, 26, 11, 8), rgb('#ffd04a')),
        'dash': lambda cv: [cv.poly([(x, 16), (x + 18, 32), (x, 48), (x + 6, 32)], rgb('#9fe7ff')) for x in (14, 30)],
        'pause': lambda cv: (cv.rect(16, 14, 11, 36, (240, 236, 226)), cv.rect(37, 14, 11, 36, (240, 236, 226))),
        'speed': lambda cv: [cv.poly([(x, 16), (x + 16, 32), (x, 48)], (240, 236, 226)) for x in (14, 32)],
        'undo': lambda cv: (cv.c.drawArc(skia.Rect.MakeXYWH(14, 16, 36, 34), 200, 250, False, cv.paint(INK, stroke=9)), cv.c.drawArc(skia.Rect.MakeXYWH(14, 16, 36, 34), 200, 250, False, cv.paint((240, 236, 226), stroke=5)), cv.poly([(8, 30), (22, 22), (20, 38)], (240, 236, 226))),
        'upgrade': lambda cv: cv.poly([(32, 8), (54, 32), (40, 32), (40, 54), (24, 54), (24, 32), (10, 32)], rgb('#7fd06a')),
        'sell': lambda cv: (cv.ellipse(32, 36, 20, 20, rgb('#e8b64a')), cv.inked_line(14, 14, 50, 50, rgb('#e0505a'), 5)),
        'build': lambda cv: (cv.inked_line(20, 50, 40, 22, WOOD, 6), cv.rect(30, 10, 26, 14, rgb('#9aa6b8'), r=3)),
        'clock': lambda cv: (cv.ellipse(32, 32, 22, 22, (240, 236, 226)), cv.inked_line(32, 32, 32, 18, INK, 2), cv.inked_line(32, 32, 42, 36, INK, 2)),
        'star': lambda cv: cv.poly(star(32, 32, 24, 11), rgb('#ffd04a')),
        'decree': lambda cv: (cv.rect(14, 10, 36, 44, rgb('#f2e6cc'), r=4), [cv.line(20, 20 + i * 8, 44, 20 + i * 8, darken(rgb('#f2e6cc'), 0.4), 2) for i in range(4)], cv.ellipse(42, 48, 8, 8, rgb('#c4553e'))),
        'night': lambda cv: (cv.circle(32, 32, 22, rgb('#f2ead6')), cv.circle(40, 26, 18, rgb('#2a2440'))),
        'day': lambda cv: (cv.poly(star(32, 32, 26, 16, 10), rgb('#ffd04a')), cv.ellipse(32, 32, 15, 15, rgb('#ffe58a'))),
        'lock': lambda cv: (cv.rect(16, 28, 32, 24, rgb('#9aa6b8'), r=4), cv.c.drawArc(skia.Rect.MakeXYWH(20, 12, 24, 30), 180, 180, False, cv.paint(INK, stroke=6))),
    }
    for k, fn in simple.items():
        cv = Canvas(64, 64)
        fn(cv)
        cv.save(f"icon/{k}")
    # family icons reuse building sprites downscaled at runtime; draw simple badges anyway for the build bar
    fam = {'castle': rgb('#4e74c4'), 'house': rgb('#c9583f'), 'mana_well': rgb('#3f9bff'), 'wall': STONE, 'archer_tower': rgb('#3f8f5a'),
           'magic_tower': rgb('#7a5ad0'), 'barracks': rgb('#a8473a'), 'shrine': rgb('#e8c768')}
    for k, col in fam.items():
        cv = Canvas(64, 64)
        cv.rect(4, 4, 56, 56, darken(col, 0.45), r=12, ink=3)
        cv.rect(8, 8, 48, 24, lighten(col, 0.1), r=10, ink=0, shade=False)
        cv.save(f"icon/fam_{k}")


# ------------------------------------------------------------------------------------------------ fx
def fx():
    cv = Canvas(48, 16)
    cv.inked_line(4, 8, 38, 8, rgb('#a8733f'), 3)
    cv.poly([(44, 8), (36, 3), (36, 13)], rgb('#d9dee8'), ink=2)
    cv.poly([(4, 8), (0, 3), (10, 8), (0, 13)], (240, 240, 240), ink=1.5)
    cv.save("fx/arrow")
    cv = Canvas(32, 32)
    cv.glow(16, 16, 16, (255, 255, 255), 200)
    cv.circle(16, 16, 7, (255, 255, 255))
    cv.save("fx/orb")
    cv = Canvas(32, 32)
    cv.smooth([(8, 20), (10, 10), (20, 7), (26, 16), (22, 25)], ROCK, ink=2.5)
    cv.save("fx/rock")
    cv = Canvas(64, 16)
    pts = [(0, 8), (12, 3), (22, 12), (34, 4), (46, 12), (64, 7)]
    for i in range(len(pts) - 1):
        cv.inked_line(*pts[i], *pts[i + 1], (255, 255, 255), 3)
    cv.save("fx/bolt")
    for k, (r, w) in {'ring': (28, 4), 'ring_thick': (26, 8)}.items():
        cv = Canvas(64, 64)
        cv.c.drawCircle(32, 32, r, cv.paint((255, 255, 255), stroke=w))
        cv.save(f"fx/{k}")
    cv = Canvas(128, 128)
    cv.glow(64, 64, 64, (255, 255, 255), 255)
    cv.save("fx/glow")
    cv = Canvas(64, 64)
    cv.c.drawCircle(32, 32, 31, cv.paint((255, 255, 255)))
    cv.save("fx/disc")
    cv = Canvas(32, 32)
    cv.poly(star(16, 16, 15, 4, 4), (255, 255, 255), ink=0)
    cv.save("fx/spark")
    cv = Canvas(48, 48)
    for (x, y, r) in ((18, 26, 12), (30, 22, 13), (26, 32, 11)):
        cv.circle(x, y, r, (255, 255, 255), 200, blur=2)
    cv.save("fx/smoke")
    cv = Canvas(64, 32)
    cv.c.drawArc(skia.Rect.MakeXYWH(4, 2, 56, 56), 200, 140, False, cv.paint((255, 255, 255), stroke=7))
    cv.save("fx/slash")
    cv = Canvas(8, 8)
    cv.c.drawRect(skia.Rect.MakeXYWH(0, 0, 8, 8), cv.paint((255, 255, 255)))
    cv.save("fx/pixel")
    cv = Canvas(64, 32)
    cv.c.drawOval(skia.Rect.MakeXYWH(2, 2, 60, 28), cv.paint((0, 0, 0), 255, blur=3))
    cv.save("fx/shadow")
    cv = Canvas(64, 64)
    cv.c.drawCircle(32, 32, 30, cv.paint((255, 255, 255), 60))
    cv.c.drawCircle(32, 32, 30, cv.paint((255, 255, 255), stroke=3))
    cv.save("fx/zone")
    cv = Canvas(48, 48)
    cv.poly([(24, 4), (44, 40), (24, 32), (4, 40)], (255, 255, 255), ink=3)
    cv.save("fx/arrowhead")


# ------------------------------------------------------------------------------------------------ ui
PARCH = rgb('#f3e7cc')
NIGHT = rgb('#2c2843')


def panel(key, fill, border, inner=None, r=18, size=96):
    cv = Canvas(size, size)
    cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(3, 5, size - 6, size - 6), r, r), cv.paint((10, 8, 20), 110, blur=3))
    cv.rect(3, 3, size - 6, size - 8, fill, r=r, ink=4, gradient=cv.vgrad(3, size, lighten(fill, 0.08), darken(fill, 0.08)))
    cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(9, 9, size - 18, size - 20), r - 6, r - 6), cv.paint(inner or border, 170, stroke=2))
    cv.save(f"ui/{key}")


def ui():
    panel("panel", PARCH, rgb('#8a6a45'))
    panel("panel_dark", NIGHT, rgb('#8a7ac0'), rgb('#6a5aa0'))
    panel("tooltip", rgb('#22202e'), rgb('#e8c25a'), r=12, size=64)
    for k, col in {'button': rgb('#e8c25a'), 'button_hover': rgb('#f4d77a'), 'button_down': rgb('#c9a03a'), 'button_off': rgb('#9a958a'),
                   'button_blue': rgb('#5f86d0'), 'button_red': rgb('#d0604a'), 'button_green': rgb('#6ab05a'), 'button_purple': rgb('#8a6ad0')}.items():
        cv = Canvas(96, 64)
        cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(3, 7, 90, 54), 16, 16), cv.paint((10, 8, 20), 120, blur=2))
        cv.rect(3, 3, 90, 54, col, r=16, ink=4, gradient=cv.vgrad(3, 57, lighten(col, 0.25), darken(col, 0.12)))
        cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(10, 8, 76, 14), 7, 7), cv.paint((255, 255, 255), 70))
        cv.save(f"ui/{k}")
    cv = Canvas(256, 256)
    cv.circle(128, 128, 120, (20, 18, 32), 110)
    cv.c.drawCircle(128, 128, 118, cv.paint((240, 236, 226), 160, stroke=5))
    cv.save("ui/joystick_base")
    cv = Canvas(112, 112)
    cv.circle(56, 56, 48, (240, 236, 226), 220)
    cv.c.drawCircle(56, 56, 48, cv.paint(INK, stroke=4))
    cv.save("ui/joystick_knob")
    cv = Canvas(160, 160)
    cv.ellipse(80, 80, 72, 72, rgb('#e8a83a'), ink=5, gradient=cv.rgrad(70, 64, 80, rgb('#ffe08a'), rgb('#c87a2a')))
    cv.poly(star(80, 80, 44, 18, 8), rgb('#fff4c8'), ink=3)
    cv.save("ui/ult_button")
    # bars
    cv = Canvas(64, 16)
    cv.rect(1, 1, 62, 14, (30, 26, 40), r=7, ink=2, shade=False)
    cv.save("ui/bar_bg")
    cv = Canvas(64, 16)
    cv.rect(1, 1, 62, 14, (255, 255, 255), r=7, ink=0, shade=False, gradient=cv.vgrad(0, 16, (255, 255, 255), (200, 200, 200)))
    cv.save("ui/bar_fill")
    logo()
    title_backdrop()


def logo():
    cv = Canvas(1024, 256)
    tf = typeface("YoungSerif-Regular.ttf")
    font = skia.Font(tf, 150)
    text = "Runavelle"
    w = font.measureText(text)
    x = (1024 - w) / 2
    blob = skia.TextBlob.MakeFromString(text, font)
    cv.c.drawTextBlob(blob, x + 6, 178, cv.paint((10, 8, 30), 160, blur=6))
    cv.c.drawTextBlob(blob, x, 172, cv.paint(INK, stroke=16))
    grad = skia.GradientShader.MakeLinear([skia.Point(0, 50), skia.Point(0, 180)], [skia.Color(255, 244, 200), skia.Color(232, 178, 74)])
    p = cv.paint((255, 255, 255))
    p.setShader(grad)
    cv.c.drawTextBlob(blob, x, 172, p)
    cv.c.drawTextBlob(blob, x, 172, cv.paint(lighten(rgb('#e8c25a'), 0.5), 200, stroke=2))
    for sx in (x - 40, x + w + 40):
        cv.poly(star(sx, 120, 18, 7, 4), rgb('#ffe08a'), ink=3)
    cv.save("ui/logo")


def title_backdrop():
    """Original painted-style dusk scene: valley, mountains, castle on a hill, glowing towers."""
    W, H = 1280, 720
    cv = Canvas(W, H)
    sky = skia.GradientShader.MakeLinear([skia.Point(0, 0), skia.Point(0, H * 0.7)], [skia.Color(44, 40, 92), skia.Color(150, 96, 140), skia.Color(246, 170, 120)], [0, 0.55, 1])
    cv.c.drawRect(skia.Rect.MakeWH(W, H), cv.paint((0, 0, 0), shader=sky))
    rng = random.Random(7)
    for _ in range(80):
        cv.circle(rng.uniform(0, W), rng.uniform(0, H * 0.35), rng.uniform(0.6, 1.8), (255, 250, 230), rng.randint(120, 230))
    cv.circle(1040, 140, 56, (255, 240, 210), 230)
    cv.glow(1040, 140, 160, (255, 220, 180), 90)
    # mountain ranges
    for layer, (col, base, amp) in enumerate([(rgb('#5a4f86'), 420, 170), (rgb('#463e70'), 470, 140), (rgb('#353058'), 520, 110)]):
        pts = [(0, H)]
        x = 0
        while x <= W:
            pts.append((x, base - abs(math.sin(x * 0.006 + layer * 1.7)) * amp - rng.uniform(0, 30)))
            x += 40
        pts.append((W, H))
        cv.poly(pts, col, ink=3 if layer == 2 else 0, shade=False)
        if layer == 0:
            for px in range(80, W, 260):
                y = base - abs(math.sin(px * 0.006)) * amp
                cv.poly([(px - 30, y + 30), (px, y - 4), (px + 34, y + 34)], SNOW, ink=0, shade=False)
    # hills
    hill = skia.GradientShader.MakeLinear([skia.Point(0, 480), skia.Point(0, H)], [skia.Color(96, 140, 86), skia.Color(40, 64, 52)])
    cv.smooth([(-50, H + 40), (-40, 560), (300, 500), (640, 470), (980, 520), (1330, 560), (1340, H + 40)], GRASS, ink=4, gradient=hill)
    # castle silhouette with lit windows
    cx, cy = 640, 470
    for dx, h in ((-120, 150), (120, 150), (-60, 210), (60, 210), (0, 270)):
        cv.rect(cx + dx - 26, cy - h, 52, h, rgb('#3a3560'), r=4, ink=4, shade=False)
        cv.poly([(cx + dx - 34, cy - h + 4), (cx + dx, cy - h - 60), (cx + dx + 34, cy - h + 4)], rgb('#2a2550'), ink=4, shade=False)
        for k in range(3):
            cv.rect(cx + dx - 6, cy - h + 30 + k * 40, 12, 18, rgb('#ffd98a'), r=4, ink=0, shade=False)
            cv.glow(cx + dx, cy - h + 39 + k * 40, 24, rgb('#ffcf6a'), 120)
    cv.rect(cx - 160, cy - 70, 320, 70, rgb('#3a3560'), r=4, ink=4, shade=False)
    for i in range(12):
        cv.rect(cx - 156 + i * 27, cy - 84, 16, 18, rgb('#3a3560'), r=2, ink=3, shade=False)
    cv.glow(cx, cy - 300, 50, rgb('#b79cff'), 200)
    # foreground trees
    for x in list(range(-20, 260, 70)) + list(range(1030, 1320, 70)):
        h = rng.uniform(120, 200)
        cv.poly([(x - 40, H), (x, H - h), (x + 40, H)], rgb('#1f2a2a'), ink=3, shade=False)
    # fireflies
    for _ in range(40):
        x, y = rng.uniform(0, W), rng.uniform(H * 0.6, H)
        cv.glow(x, y, 10, rgb('#ffe58a'), 150)
    cv.save("ui/title_bg", ppu=100)


def emblems():
    specs = {'fortress': (rgb('#8d8a9e'), 'shield'), 'legion': (rgb('#c4553e'), 'swords'), 'arcanum': (rgb('#7a5ad0'), 'crystal')}
    for k, (col, kind) in specs.items():
        cv = Canvas(96, 128)
        cv.inked_line(48, 124, 48, 18, rgb('#8a5a3a'), 4)
        cv.poly([(50, 20), (92, 26), (86, 60), (92, 92), (50, 86)], col, ink=3)
        if kind == 'shield':
            cv.smooth([(58, 36), (80, 36), (80, 54), (69, 70), (58, 54)], lighten(col, 0.35), ink=2.5)
        elif kind == 'swords':
            cv.inked_line(58, 72, 80, 36, rgb('#e8e8f0'), 3)
            cv.inked_line(80, 72, 58, 36, rgb('#e8e8f0'), 3)
        else:
            cv.poly([(69, 32), (79, 52), (69, 76), (59, 52)], rgb('#c9b0ff'), ink=2.5)
            cv.glow(69, 54, 26, rgb('#c9b0ff'), 150)
        cv.poly(star(48, 14, 9, 4), rgb('#e8c25a'), ink=2)
        cv.save(f"building/emblem_{k}", pivot=(48, 124))


def equipment_icons():
    weapons = {'dawnblade': 'light', 'duskfang': 'shadow', 'stormbrand': 'lightning', 'frostbite': 'ice', 'emberheart': 'fire', 'tideglaive': 'water'}
    for k, el in weapons.items():
        cv = Canvas(64, 64)
        cv.circle(32, 32, 28, darken(ELEMENT_COL[el], 0.6))
        cv.inked_line(16, 48, 46, 18, rgb('#d9dee8'), 6)
        cv.inked_line(14, 38, 26, 50, rgb('#e8c25a'), 5)
        cv.glow(42, 22, 14, ELEMENT_COL[el], 220)
        cv.save(f"icon/eq_{k}")
    cv = Canvas(64, 64); cv.circle(32, 32, 28, rgb('#3a3036')); cv.inked_line(18, 50, 40, 26, rgb('#8a5a3a'), 5); cv.rect(30, 10, 26, 22, rgb('#9aa6b8'), r=4); cv.save("icon/eq_giants_maul")
    cv = Canvas(64, 64); cv.circle(32, 32, 28, rgb('#3a3036')); cv.inked_line(14, 50, 52, 12, rgb('#e8eef8'), 3); cv.c.drawCircle(18, 46, 8, cv.paint(rgb('#e8c25a'), stroke=3)); cv.save("icon/eq_swift_rapier")
    relic = {
        'phoenix_charm': lambda cv: cv.smooth([(32, 10), (50, 30), (44, 50), (32, 56), (20, 50), (14, 30)], rgb('#ff7a2e')),
        'marshal_seal': lambda cv: (cv.ellipse(32, 34, 20, 20, rgb('#c4553e')), cv.poly(star(32, 34, 12, 5), rgb('#e8c25a'), ink=2)),
        'golden_hourglass': lambda cv: (cv.poly([(18, 12), (46, 12), (32, 32)], rgb('#e8c25a')), cv.poly([(18, 52), (46, 52), (32, 32)], rgb('#e8c25a'))),
        'ancestral_ward': lambda cv: cv.smooth([(14, 14), (50, 14), (50, 34), (32, 54), (14, 34)], rgb('#9aa6b8')),
        'windstep_boots': lambda cv: (cv.rect(18, 16, 16, 30, rgb('#8a5a3a'), r=4), cv.rect(18, 40, 30, 12, rgb('#8a5a3a'), r=5), glyph(cv, 'swirl', 46, 22, 10, ELEMENT_COL['wind'])),
        'treasure_crown': lambda cv: cv.poly([(10, 46), (12, 18), (22, 32), (32, 12), (42, 32), (52, 18), (54, 46)], rgb('#e8c25a')),
        'resonance_gem': lambda cv: (cv.poly([(32, 8), (52, 28), (32, 56), (12, 28)], rgb('#b79cff')), cv.glow(32, 30, 24, rgb('#b79cff'), 150)),
        'seer_lantern': lambda cv: (cv.rect(20, 18, 24, 32, rgb('#3a3560'), r=6), cv.glow(32, 34, 20, rgb('#ffe58a'), 220), cv.c.drawArc(skia.Rect.MakeXYWH(22, 6, 20, 20), 180, 180, False, cv.paint(INK, stroke=4))),
    }
    for k, fn in relic.items():
        cv = Canvas(64, 64)
        cv.circle(32, 32, 30, rgb('#2c2843'), 200)
        fn(cv)
        cv.save(f"icon/eq_{k}")


def run():
    icons()
    fx()
    ui()
    emblems()
    equipment_icons()
