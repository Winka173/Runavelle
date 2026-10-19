"""Terrain tiles, decor and the eight building families (all tiers/branches)."""
import math
import random

from artlib import Canvas, TILE, INK, rgb, lighten, darken, mix, star

GRASS = rgb('#86b85c')
GRASS_D = rgb('#6a9a4a')
DIRT = rgb('#c8a073')
SAND = rgb('#e6d3a0')
PLATEAU = rgb('#a9bf73')
ICE = rgb('#c4e8f3')
WATER = rgb('#3d84b8')
SHALLOW = rgb('#6db2c9')
CHASM = rgb('#261a33')
LAVA = rgb('#e85a2a')
ROCK = rgb('#8d849e')
SNOW = rgb('#f4f1ea')
LEAF = rgb('#3f7e48')
WOOD = rgb('#a8733f')
STONE = rgb('#cfc5b2')
STONE_D = rgb('#9c907e')

ELEMENT_COL = {
    'fire': rgb('#ff7a2e'), 'water': rgb('#3f9bff'), 'lightning': rgb('#ffe14a'), 'ice': rgb('#9ff0ff'),
    'wind': rgb('#8ff0b0'), 'earth': rgb('#c9965a'), 'light': rgb('#fff1a8'), 'shadow': rgb('#a35cf0'), 'none': rgb('#d9d4cc'),
}


# ------------------------------------------------------------------------------------------------ terrain
def tile(kind, variant):
    rng = random.Random(hash((kind, variant)) & 0xffff)
    cv = Canvas(TILE, TILE)
    c = cv.c
    base = {'grass': GRASS, 'dirt': DIRT, 'sand': SAND, 'plateau': PLATEAU, 'ice': ICE, 'water': WATER, 'shallows': SHALLOW,
            'chasm': CHASM, 'lava': LAVA, 'bridge': WOOD, 'mountain': darken(ROCK, 0.1), 'forest': darken(GRASS, 0.25)}[kind]
    c.drawRect(skia_rect(0, 0, TILE, TILE), cv.paint(base))
    if kind in ('grass', 'plateau', 'forest', 'mountain'):
        cv.noise(rng, 60, lighten(base, 0.15), 120, (1, 2.2))
        cv.noise(rng, 40, darken(base, 0.18), 120, (1, 2.5))
        if kind == 'grass':
            for _ in range(5 + variant * 2):
                x, y = rng.uniform(4, 60), rng.uniform(6, 60)
                for k in (-3, 0, 3):
                    cv.line(x, y, x + k * 0.7, y - 6 - rng.uniform(0, 3), darken(GRASS, 0.25), 1.5, 200)
            if variant == 3:
                for _ in range(3):
                    x, y = rng.uniform(8, 56), rng.uniform(8, 56)
                    col = rng.choice([rgb('#fff3c4'), rgb('#ffb3c7'), rgb('#c9e3ff')])
                    for a in range(5):
                        cv.circle(x + math.cos(a * 1.26) * 2.2, y + math.sin(a * 1.26) * 2.2, 1.6, col)
                    cv.circle(x, y, 1.3, rgb('#ffcf4a'))
    elif kind in ('dirt', 'sand'):
        cv.noise(rng, 70, lighten(base, 0.2), 140, (0.8, 2))
        cv.noise(rng, 50, darken(base, 0.2), 140, (0.8, 2.2))
        for _ in range(3):
            x, y = rng.uniform(6, 58), rng.uniform(6, 58)
            cv.ellipse(x, y, rng.uniform(2, 3.5), rng.uniform(1.5, 2.5), darken(base, 0.1), ink=1.2, shade=False)
    elif kind in ('water', 'shallows'):
        for i in range(6):
            y = 8 + i * 10 + variant * 2
            x = rng.uniform(4, 40)
            cv.line(x, y, x + rng.uniform(10, 18), y, lighten(base, 0.45), 2, 150)
    elif kind == 'ice':
        cv.noise(rng, 30, (255, 255, 255), 150, (1, 2.5))
        for _ in range(3):
            x, y = rng.uniform(5, 55), rng.uniform(5, 55)
            cv.line(x, y, x + rng.uniform(-12, 12), y + rng.uniform(-12, 12), darken(ICE, 0.25), 1.2, 200)
    elif kind == 'chasm':
        cv.noise(rng, 40, lighten(base, 0.1), 100, (1, 3))
        for _ in range(4):
            cv.circle(rng.uniform(0, 64), rng.uniform(0, 64), rng.uniform(1, 2), rgb('#6b5a8f'), 120)
    elif kind == 'lava':
        cv.noise(rng, 40, rgb('#ffcf4a'), 160, (1.5, 4))
        cv.noise(rng, 30, darken(LAVA, 0.35), 160, (2, 5))
    elif kind == 'bridge':
        for i in range(8):
            cv.rect(0, i * 8, 64, 8, lighten(WOOD, 0.05 * (i % 2)), r=1, ink=1.2, shade=False)
    cv.save(f"terrain/{kind}_{variant}")


def blob(kind, variant):
    """Soft-edged 1.6-tile patch of a terrain texture, overlaid on the grass base to hide the grid."""
    import skia
    rng = random.Random(hash((kind, 'blob', variant)) & 0xffff)
    size = 104
    base = {'dirt': DIRT, 'sand': SAND, 'plateau': PLATEAU, 'ice': ICE, 'water': WATER, 'shallows': SHALLOW, 'chasm': CHASM, 'lava': LAVA}[kind]
    src = skia.Surface(size, size)
    sc = Canvas.__new__(Canvas)
    sc.w = sc.h = size; sc.surface = src; sc.c = src.getCanvas()
    sc.c.clear(skia.Color(*base, 255))
    sc.noise(rng, 160, lighten(base, 0.18), 130, (0.8, 2.2))
    sc.noise(rng, 120, darken(base, 0.2), 130, (0.8, 2.4))
    if kind in ('water', 'shallows'):
        for i in range(9):
            y = 8 + i * 11
            x = rng.uniform(6, 70)
            sc.line(x, y, x + rng.uniform(10, 22), y, lighten(base, 0.45), 2, 150)
    if kind == 'lava':
        sc.noise(rng, 50, rgb('#ffcf4a'), 170, (1.5, 4))
    tex = src.makeImageSnapshot()
    cv = Canvas(size, size)
    pts = []
    n = 18
    for i in range(n):
        a = i / n * 2 * math.pi
        r = size * 0.43 * (0.86 + rng.uniform(-0.07, 0.1))
        pts.append((size / 2 + math.cos(a) * r, size / 2 + math.sin(a) * r))
    path = skia.Path(); path.moveTo(*pts[0])
    for i in range(n):
        p0, p1, p2, p3 = pts[(i - 1) % n], pts[i], pts[(i + 1) % n], pts[(i + 2) % n]
        path.cubicTo(p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6, p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6, *p2)
    path.close()
    soft = kind not in ('chasm', 'lava')
    paint = skia.Paint(AntiAlias=True)
    paint.setShader(tex.makeShader(skia.TileMode.kClamp, skia.TileMode.kClamp, skia.SamplingOptions()))
    if soft:
        paint.setMaskFilter(skia.MaskFilter.MakeBlur(skia.kNormal_BlurStyle, 5))
    cv.c.drawPath(path, paint)
    if kind == 'chasm':
        cv.c.drawPath(path, cv.paint(rgb('#7a5f48'), stroke=5))
    elif kind in ('water', 'lava'):
        cv.c.drawPath(path, cv.paint((235, 245, 255) if kind == 'water' else rgb('#ffcf4a'), 150, stroke=3))
    cv.save(f"terrain/{kind}_blob_{variant}")


def skia_rect(x, y, w, h):
    import skia
    return skia.Rect.MakeXYWH(x, y, w, h)


def decor():
    # mountain peaks (placed on mountain tiles), 3 variants, 96x112 px, pivot at base centre
    for v in range(4):
        rng = random.Random(500 + v)
        cv = Canvas(112, 120)
        base_y = 108
        w = 44 + v * 4
        cx = 56 + rng.uniform(-4, 4)
        peak = (cx + rng.uniform(-6, 6), 18 + v * 6)
        left = (cx - w, base_y)
        right = (cx + w, base_y)
        p = cv.poly([left, (cx - w * 0.5, base_y - 40), peak, (cx + w * 0.55, base_y - 46), right], ROCK)
        # shaded right face
        cv.poly([peak, (cx + w * 0.55, base_y - 46), right, (cx + 6, base_y)], darken(ROCK, 0.25), ink=0)
        cv.poly([left, (cx - w * 0.5, base_y - 40), peak, (cx + 6, base_y)], ROCK, ink=0)
        cv.poly([left, (cx - w * 0.5, base_y - 40), peak, (cx + w * 0.55, base_y - 46), right], ROCK, ink=3, shade=False, gradient=None) if False else None
        import skia
        path = skia.Path(); path.moveTo(*left); path.lineTo(cx - w * 0.5, base_y - 40); path.lineTo(*peak); path.lineTo(cx + w * 0.55, base_y - 46); path.lineTo(*right); path.close()
        cv.c.drawPath(path, cv.paint(INK, stroke=3))
        # snow cap
        sc = [(peak[0] - 14, peak[1] + 18), (peak[0] - 6, peak[1] + 12), peak, (peak[0] + 9, peak[1] + 13), (peak[0] + 16, peak[1] + 20), (peak[0] + 4, peak[1] + 16), (peak[0] - 4, peak[1] + 22)]
        cv.poly(sc, SNOW, ink=2)
        cv.line(peak[0] + 2, peak[1] + 6, cx + 6, base_y - 6, darken(ROCK, 0.4), 2, 160)
        cv.save(f"decor/mountain_{v}", pivot=(56, 104))
    # trees
    for v in range(4):
        rng = random.Random(700 + v)
        cv = Canvas(72, 96)
        cv.shadow(36, 86, 18, 6)
        cv.rect(32, 62, 8, 24, WOOD, r=2, ink=2.5)
        leaf = mix(LEAF, rgb('#5f9a45'), v / 4)
        if v % 2 == 0:  # round tree
            for (x, y, r) in [(24, 50, 15), (48, 50, 15), (36, 34, 19), (36, 52, 16)]:
                cv.ellipse(x, y, r, r * 0.92, leaf, ink=2.5)
            cv.noise(rng, 14, lighten(leaf, 0.3), 200, (1.5, 2.5), (18, 22, 54, 50))
        else:  # pine
            for i, y in enumerate([66, 52, 38]):
                w = 24 - i * 5
                cv.poly([(36 - w, y), (36, y - 26), (36 + w, y)], darken(leaf, 0.1), ink=2.5)
        cv.save(f"decor/tree_{v}", pivot=(36, 84))
    # rocks / reeds / crystals
    cv = Canvas(48, 40)
    cv.shadow(24, 33, 16, 5)
    cv.smooth([(10, 30), (14, 16), (26, 10), (38, 16), (40, 30)], ROCK, ink=2.5)
    cv.save("decor/rock_0", pivot=(24, 32))
    cv = Canvas(48, 48)
    for i, x in enumerate([14, 22, 30, 36]):
        cv.line(x, 44, x + (i - 1.5) * 3, 14 + i * 3, darken(GRASS, 0.3), 3)
        cv.ellipse(x + (i - 1.5) * 3, 14 + i * 3, 2.5, 6, rgb('#8a5a3a'), ink=1.5)
    cv.save("decor/reeds_0", pivot=(24, 44))
    cv = Canvas(48, 56)
    cv.shadow(24, 50, 14, 5)
    for (x, h, col) in [(16, 30, rgb('#9a7cf0')), (28, 40, rgb('#b79cff')), (36, 22, rgb('#7d61d6'))]:
        cv.poly([(x - 6, 50), (x - 5, 50 - h + 8), (x, 50 - h), (x + 5, 50 - h + 8), (x + 6, 50)], col, ink=2.5)
    cv.save("decor/crystal_0", pivot=(24, 50))


# ------------------------------------------------------------------------------------------------ buildings
def foundation(cv, x, y, w, h, col=STONE_D):
    cv.shadow(x + w / 2, y + h - 4, w / 2 + 4, h * 0.22, 80)
    cv.rect(x, y + h - 16, w, 16, col, r=5, ink=2.5)


def box_building(cv, x, y, w, body_h, wall, roof, roof_h, door=True, windows=2, roof_style='gable', trim=None):
    """Front-facing 3/4 building: wall box + roof. (x,y) = top-left of wall front face."""
    cv.rect(x, y, w, body_h, wall, r=3, ink=3)
    # side shading
    cv.rect(x + w - 10, y + 2, 8, body_h - 4, darken(wall, 0.18), r=2, ink=0, shade=False)
    if trim:
        cv.rect(x - 2, y + body_h - 8, w + 4, 8, trim, r=2, ink=2.5)
    if door:
        dw = max(12, w * 0.2)
        cv.rect(x + w / 2 - dw / 2, y + body_h - 26, dw, 26, darken(WOOD, 0.25), r=6, ink=2.5)
        cv.circle(x + w / 2 + dw / 4, y + body_h - 13, 1.6, rgb('#f2c14e'))
    for i in range(windows):
        wx = x + (i + 1) * w / (windows + 1) - 6
        if door and abs(wx + 6 - (x + w / 2)) < 14:
            continue
        cv.rect(wx, y + 12, 12, 13, rgb('#ffd98a'), r=3, ink=2.5, shade=False)
        cv.line(wx + 6, y + 12, wx + 6, y + 25, INK, 1.5)
    if roof_style == 'gable':
        cv.poly([(x - 8, y + 4), (x + w / 2, y - roof_h), (x + w + 8, y + 4)], roof, ink=3)
        cv.poly([(x + w / 2, y - roof_h), (x + w + 8, y + 4), (x + w / 2 + 6, y + 4)], darken(roof, 0.2), ink=0, shade=False)
        for i in range(3):
            t = (i + 1) / 4
            cv.line(x - 8 + (w / 2 + 8) * t, y + 4 - (roof_h + 4) * t, x + w + 8 - (w / 2 + 8) * t, y + 4 - (roof_h + 4) * t, darken(roof, 0.3), 1.5, 160)
    elif roof_style == 'flat':
        cv.rect(x - 4, y - 8, w + 8, 12, roof, r=3, ink=3)
        for i in range(5):
            cv.rect(x - 4 + i * (w + 8 - 10) / 4, y - 16, 10, 10, roof, r=2, ink=2.5)


def round_tower(cv, cx, base_y, r, h, wall, roof, roof_h, crenel=False, cone=True):
    import skia
    p = skia.Path()
    p.moveTo(cx - r, base_y - h)
    p.lineTo(cx - r, base_y)
    p.arcTo(skia.Rect.MakeLTRB(cx - r, base_y - r * 0.45, cx + r, base_y + r * 0.45), 180, -180, False)
    p.lineTo(cx + r, base_y - h)
    p.close()
    sh = cv.c  # noqa
    grad = __import__('skia').GradientShader.MakeLinear([__import__('skia').Point(cx - r, 0), __import__('skia').Point(cx + r, 0)],
                                                      [__c(lighten(wall, 0.2)), __c(wall), __c(darken(wall, 0.3))], [0, 0.45, 1])
    cv.shape(p, wall, ink=3, gradient=grad)
    for i in range(1, 4):
        yy = base_y - h * i / 4
        cv.line(cx - r + 2, yy, cx + r - 2, yy, darken(wall, 0.25), 1.2, 140)
    if crenel:
        cv.ellipse(cx, base_y - h, r + 3, r * 0.45, darken(wall, 0.1), ink=3)
        for i in range(-2, 3):
            cv.rect(cx + i * r * 0.42 - 4, base_y - h - 12 + abs(i) * 2, 8, 10, wall, r=1.5, ink=2.2)
    if cone:
        cv.poly([(cx - r - 6, base_y - h + 2), (cx, base_y - h - roof_h), (cx + r + 6, base_y - h + 2)], roof, ink=3)
        cv.poly([(cx, base_y - h - roof_h), (cx + r + 6, base_y - h + 2), (cx + 4, base_y - h + 2)], darken(roof, 0.22), ink=0, shade=False)


def __c(col):
    import skia
    return skia.Color(col[0], col[1], col[2], 255)


def flag(cv, x, y, col, h=20):
    cv.inked_line(x, y, x, y - h, darken(WOOD, 0.3), 2)
    cv.poly([(x, y - h), (x + 14, y - h + 4), (x, y - h + 9)], col, ink=2)


def crystal(cv, cx, cy, s, col):
    cv.poly([(cx, cy - s * 1.4), (cx + s * 0.7, cy - s * 0.2), (cx, cy + s * 0.6), (cx - s * 0.7, cy - s * 0.2)], col, ink=2.5)
    cv.poly([(cx, cy - s * 1.4), (cx + s * 0.7, cy - s * 0.2), (cx, cy + s * 0.6)], darken(col, 0.2), ink=0, shade=False)
    cv.glow(cx, cy - s * 0.3, s * 2.2, col, 120)


def building_canvas(size, extra=72):
    w = size * TILE + 24
    h = size * TILE + extra
    return Canvas(w, h), w, h


def pivot_for(size, w, h):
    # pivot = centre of footprint: footprint occupies bottom size*TILE px (top-down), centred horizontally
    return (w / 2, h - size * TILE / 2 - 4)


def castle(level):
    cv, w, h = building_canvas(3, 104)
    base = h - 12
    roofc = {'1': rgb('#4e74c4'), '2': rgb('#3f62b0'), '3a': rgb('#2f4f9a'), '3b': rgb('#7a4fc4')}[level]
    wall = STONE if level != '3b' else rgb('#d6cbe6')
    foundation(cv, 14, base - 150, w - 28, 150)
    # back towers
    round_tower(cv, 40, base - 60, 20, 90, wall, roofc, 34, crenel=level != '1')
    round_tower(cv, w - 40, base - 60, 20, 90, wall, roofc, 34, crenel=level != '1')
    # keep
    box_building(cv, 54, base - 140, w - 108, 100, wall, roofc, 46, door=False, windows=3, roof_style='flat' if level == '3a' else 'gable', trim=STONE_D)
    if level in ('2', '3a', '3b'):
        round_tower(cv, w / 2, base - 116, 18, 52, wall, roofc, 40, crenel=True)
    # front wall + gate
    cv.rect(18, base - 56, w - 36, 46, wall, r=4, ink=3)
    for i in range(9):
        cv.rect(22 + i * (w - 52) / 8, base - 66, 9, 12, wall, r=1.5, ink=2.2)
    cv.rect(w / 2 - 15, base - 48, 30, 38, darken(WOOD, 0.3), r=10, ink=3)
    for i in range(3):
        cv.line(w / 2 - 9 + i * 9, base - 44, w / 2 - 9 + i * 9, base - 12, INK, 1.5)
    flag(cv, 40, base - 160, rgb('#e8b64a'))
    flag(cv, w - 40, base - 160, rgb('#e8b64a'))
    if level == '3b':
        crystal(cv, w / 2, base - 196, 12, ELEMENT_COL['lightning'])
    if level == '3a':
        for x in (30, w - 30):
            cv.rect(x - 7, base - 40, 14, 28, STONE_D, r=3, ink=2.5)
    cv.save(f"building/castle_{level}", pivot=pivot_for(3, w, h))


def house(level):
    cv, w, h = building_canvas(2, 64)
    base = h - 10
    foundation(cv, 14, base - 92, w - 28, 92, rgb('#a89a84'))
    roof = {'1': rgb('#c9583f'), '2': rgb('#b8473a'), '3a': rgb('#a63b45'), '3b': rgb('#6d58c4')}[level]
    wall = rgb('#efe2c4') if level != '3b' else rgb('#e4dcf2')
    if level == '1':
        box_building(cv, 30, base - 70, 84, 56, wall, rgb('#c9a24a'), 36, windows=2)  # thatched
        for i in range(6):
            cv.line(26 + i * 16, base - 70, 72, base - 102, darken(rgb('#c9a24a'), 0.2), 1.5, 150)
    else:
        box_building(cv, 22, base - 82, 100, 68, wall, roof, 42, windows=3, trim=rgb('#b49a76'))
        cv.rect(92, base - 132, 12, 26, rgb('#a5998a'), r=2, ink=2.5)
    if level == '3a':
        cv.rect(10, base - 34, 26, 22, rgb('#d9a04a'), r=3, ink=2.5)  # market stall
        cv.poly([(6, base - 36), (23, base - 48), (40, base - 36)], rgb('#e05a4a'), ink=2.5)
        for i in range(3):
            cv.circle(16 + i * 7, base - 22, 3, rgb('#f2c14e'))
    if level == '3b':
        crystal(cv, w / 2, base - 136, 9, ELEMENT_COL['shadow'])
    cv.save(f"building/house_{level}", pivot=pivot_for(2, w, h))


def well(level):
    cv, w, h = building_canvas(2, 70)
    base = h - 10
    foundation(cv, 18, base - 88, w - 36, 88, rgb('#9d93ad'))
    stone = rgb('#b9b2c9')
    cv.ellipse(w / 2, base - 40, 40, 18, darken(stone, 0.1))
    cv.rect(w / 2 - 40, base - 40, 80, 26, stone, r=4, ink=3)
    cv.ellipse(w / 2, base - 40, 34, 13, rgb('#3a7fe0'), ink=2.5, shade=False,
               gradient=__import__('skia').GradientShader.MakeRadial(__import__('skia').Point(w / 2, base - 42), 34, [__c(rgb('#9fe7ff')), __c(rgb('#2a5fd0'))]))
    glowc = rgb('#7fd8ff') if level != '3b' else rgb('#d6a8ff')
    cv.glow(w / 2, base - 44, 46, glowc, 110)
    if level == '1':
        cv.inked_line(w / 2 - 34, base - 40, w / 2 - 34, base - 92, WOOD, 4)
        cv.inked_line(w / 2 + 34, base - 40, w / 2 + 34, base - 92, WOOD, 4)
        cv.poly([(w / 2 - 44, base - 88), (w / 2, base - 112), (w / 2 + 44, base - 88)], rgb('#5a7fc0'), ink=3)
    else:
        for dx in (-30, 30):
            crystal(cv, w / 2 + dx, base - 70, 9 if level == '2' else 12, glowc)
        crystal(cv, w / 2, base - 96, 13 if level == '2' else 17, glowc)
    if level == '3b':
        for k in range(3):
            cv.c.drawCircle(w / 2, base - 60, 52 + k * 6, cv.paint(glowc, 90 - k * 25, stroke=2))
    cv.save(f"building/well_{level}", pivot=pivot_for(2, w, h))


def archer(level, element='none'):
    cv, w, h = building_canvas(2, 112)
    base = h - 10
    foundation(cv, 22, base - 90, w - 44, 90)
    timber = WOOD if level in ('1',) else STONE
    roof = rgb('#3f8f5a') if level != '3a' else rgb('#2f6d85')
    if level == '1':
        # wooden watchtower on stilts
        for x in (44, w - 44):
            cv.inked_line(x, base - 26, x + (6 if x < w / 2 else -6), base - 120, WOOD, 6)
        cv.inked_line(44, base - 60, w - 44, base - 90, darken(WOOD, 0.1), 3)
        cv.inked_line(w - 44, base - 60, 44, base - 90, darken(WOOD, 0.1), 3)
        cv.rect(34, base - 138, w - 68, 26, WOOD, r=3, ink=3)
        cv.poly([(28, base - 136), (w / 2, base - 172), (w - 28, base - 136)], roof, ink=3)
    else:
        round_tower(cv, w / 2, base - 28, 30, 104 + (16 if level != '2' else 0), STONE, roof, 40, crenel=True, cone=level != '3b')
        cv.rect(w / 2 - 6, base - 110, 12, 18, INK, r=5, ink=0, shade=False)
        if level == '3a':  # sniper: long ballista arm
            cv.inked_line(w / 2 - 34, base - 168, w / 2 + 40, base - 182, darken(WOOD, 0.2), 5)
        if level == '3b':  # multishot: open platform with three bows
            for dx in (-18, 0, 18):
                cv.c.drawArc(__import__('skia').Rect.MakeXYWH(w / 2 + dx - 8, base - 176, 16, 22), 200, 140, False, cv.paint(darken(WOOD, 0.2), stroke=3))
    if element != 'none':
        crystal(cv, w / 2, base - (180 if level != '1' else 176), 8, ELEMENT_COL[element])
    flag(cv, w / 2 + 22, base - (150 if level == '1' else 168), rgb('#e8b64a'), 18)
    cv.save(f"building/archer_{level}", pivot=pivot_for(2, w, h))


def magic(level, el):
    cv, w, h = building_canvas(2, 120)
    base = h - 10
    col = ELEMENT_COL[el]
    foundation(cv, 22, base - 90, w - 44, 90, rgb('#8f86a8'))
    wall = rgb('#bdb3d6')
    roof = darken(mix(rgb('#5b3fa8'), col, 0.35), 0.1)
    tall = {'1': 92, '2': 112, 'spire': 132, 'catalyst': 104}[level]
    round_tower(cv, w / 2, base - 26, 26, tall, wall, roof, 46 if level != 'catalyst' else 0, crenel=False, cone=level != 'catalyst')
    for k in range(3):
        cv.c.drawArc(__import__('skia').Rect.MakeXYWH(w / 2 - 26, base - 40 - k * 28, 52, 14), 0, 180, False, cv.paint(col, 160, stroke=2))
    top = base - 26 - tall
    if level == 'catalyst':
        cv.ellipse(w / 2, top, 32, 10, darken(wall, 0.15))
        for a in range(3):
            ang = a * 2.094
            crystal(cv, w / 2 + math.cos(ang) * 22, top - 10 + math.sin(ang) * 6, 8, col)
        crystal(cv, w / 2, top - 34, 13, col)
        cv.c.drawCircle(w / 2, top - 28, 28, cv.paint(col, 140, stroke=2.5))
    else:
        crystal(cv, w / 2, top - 46 - (14 if level == 'spire' else 0), 10 + (4 if level != '1' else 0), col)
    cv.glow(w / 2, top - 30, 44, col, 90)
    cv.save(f"building/magic_{level}_{el}", pivot=pivot_for(2, w, h))


def barracks(level):
    cv, w, h = building_canvas(2, 76)
    base = h - 10
    foundation(cv, 12, base - 92, w - 24, 92)
    roof = {'1': rgb('#a8473a'), '2': rgb('#93362f'), 'legion': rgb('#b8452e'), 'elite': rgb('#3f4f8a')}[level]
    box_building(cv, 16, base - 84, w - 32, 70, rgb('#d9c7a6'), roof, 34, door=True, windows=2, roof_style='gable', trim=rgb('#8f6b45'))
    # training dummy / weapon rack
    cv.inked_line(w - 18, base - 10, w - 18, base - 44, WOOD, 4)
    cv.inked_line(w - 30, base - 34, w - 6, base - 34, WOOD, 3)
    cv.ellipse(w - 18, base - 50, 7, 7, rgb('#d9b27a'), ink=2.5)
    flag(cv, 26, base - 104, roof, 26)
    if level in ('legion',):
        flag(cv, w - 40, base - 112, rgb('#e8b64a'), 30)
        flag(cv, 50, base - 112, rgb('#e8b64a'), 30)
    if level == 'elite':
        cv.poly(star(w / 2, base - 128, 12, 5), rgb('#f2d36b'), ink=2.5)
    cv.save(f"building/barracks_{level}", pivot=pivot_for(2, w, h))


def shrine(level):
    cv, w, h = building_canvas(2, 92)
    base = h - 10
    foundation(cv, 18, base - 88, w - 36, 88, rgb('#d7cfbf'))
    col = {'1': rgb('#ffe9a6'), '2': rgb('#fff0c0'), '3a': rgb('#fff6d8'), '3b': rgb('#ff8a5a')}[level]
    marble = rgb('#f1ebe0')
    cv.rect(w / 2 - 40, base - 34, 80, 20, marble, r=4, ink=2.5)
    for dx in (-30, 30):
        cv.rect(w / 2 + dx - 6, base - 96, 12, 64, marble, r=3, ink=2.5)
    cv.poly([(w / 2 - 46, base - 92), (w / 2, base - 124), (w / 2 + 46, base - 92)], rgb('#e8c768') if level != '3b' else rgb('#c4553e'), ink=3)
    cv.rect(w / 2 - 46, base - 96, 92, 8, marble, r=2, ink=2.5)
    if level == '3b':
        cv.poly([(w / 2 - 10, base - 34), (w / 2 + 10, base - 34), (w / 2 + 6, base - 58), (w / 2 - 6, base - 58)], rgb('#8d849e'), ink=2.5)
        for k in range(4):
            cv.poly([(w / 2 - 6 + k * 4, base - 58), (w / 2 - 2 + k * 3, base - 74 - (k % 2) * 6), (w / 2 + 2 + k * 3, base - 58)], rgb('#ff8a3a'), ink=1.5)
    else:
        cv.poly(star(w / 2, base - 62, 13 + (3 if level != '1' else 0), 6), col, ink=2.5)
    cv.glow(w / 2, base - 62, 48, col, 120 if level != '1' else 80)
    cv.save(f"building/shrine_{level}", pivot=pivot_for(2, w, h))


WALL_MATS = {'wood': (WOOD, darken(WOOD, 0.25)), 'stone': (STONE, STONE_D), 'bastion': (rgb('#b9b0a0'), rgb('#7d7365')), 'rune': (rgb('#b8b0d8'), rgb('#7a6fb0'))}


def wall(mat, mask):
    """Auto-joining wall piece. mask bits: 1=N, 2=E, 4=S, 8=W. Canvas 64x112, pivot at tile centre."""
    cv = Canvas(64, 112)
    top_col, side_col = WALL_MATS[mat]
    cy = 112 - 32
    hgt = {'wood': 30, 'stone': 34, 'bastion': 40, 'rune': 36}[mat]
    th = 16 if mat == 'wood' else 20
    cv.shadow(32, cy + 8, 30 if mask & 10 else 16, 8, 60)

    def texture(x, y, w, h, vertical_logs):
        if mat == 'wood':
            n = max(1, int(w / 8))
            for i in range(n + 1):
                xx = x + i * w / max(1, n)
                cv.line(xx, y + 2, xx, y + h - 2, darken(side_col, 0.35), 1.4, 200)
        else:
            rows = max(1, int(h / 8))
            for r in range(rows):
                yy = y + r * h / rows
                cv.line(x + 1, yy, x + w - 1, yy, darken(side_col, 0.3), 1.0, 170)
                off = 0 if r % 2 == 0 else 6
                k = x + off
                while k < x + w:
                    cv.line(k, yy, k, yy + h / rows, darken(side_col, 0.3), 1.0, 150)
                    k += 12

    def hseg(x0, x1):
        # front face + top cap of a horizontal run
        cv.rect(x0, cy - hgt + th / 2, x1 - x0, hgt, side_col, r=1, ink=2.5, shade=False)
        texture(x0, cy - hgt + th / 2, x1 - x0, hgt, True)
        cv.rect(x0, cy - hgt - th / 2, x1 - x0, th, top_col, r=1, ink=2.5)
        if mat == 'wood':
            n = int((x1 - x0) / 8)
            for i in range(n):
                xx = x0 + 4 + i * 8
                cv.poly([(xx - 4, cy - hgt - th / 2 + 1), (xx, cy - hgt - th / 2 - 7), (xx + 4, cy - hgt - th / 2 + 1)], top_col, ink=2)

    def vseg(y0, y1):
        cv.rect(32 - th / 2, y0 - hgt, th, y1 - y0, top_col, r=1, ink=2.5)
        cv.rect(32 + th / 2 - 4, y0 - hgt + 2, 4, y1 - y0 - 4, darken(top_col, 0.25), r=1, ink=0, shade=False)

    if mask & 1:
        vseg(cy - 34, cy)
    if mask & 8:
        hseg(-3, 32)
    if mask & 2:
        hseg(32, 67)
    # central post / tower (straight runs stay continuous)
    pw = th + (6 if mat != 'wood' else 4)
    straight = mask in (5, 10)
    if mask == 5:
        vseg(cy - 1, cy + 1)
    if not straight:
        cv.rect(32 - pw / 2, cy - hgt - th / 2 - 4, pw, hgt + th, side_col, r=3, ink=2.5, shade=False)
        texture(32 - pw / 2, cy - hgt + th / 2, pw, hgt - th / 2, True)
        cv.rect(32 - pw / 2 - 1, cy - hgt - th / 2 - 8, pw + 2, th, top_col, r=3, ink=2.5)
        if mat in ('stone', 'bastion', 'rune'):
            for i in (-1, 1):
                cv.rect(32 + i * (pw / 2 - 3) - 4, cy - hgt - th / 2 - 15, 8, 9, top_col, r=1.5, ink=2)
        else:
            cv.poly([(32 - pw / 2, cy - hgt - th / 2 - 8), (32, cy - hgt - th / 2 - 20), (32 + pw / 2, cy - hgt - th / 2 - 8)], lighten(top_col, 0.1), ink=2.5)
    if mask & 4:
        vseg(cy, cy + 34)
        cv.rect(32 - th / 2, cy + 34 - hgt, th, 0.1, top_col, ink=0)
    if mat == 'rune':
        cv.glow(32, cy - hgt + 2, 16, rgb('#b79cff'), 170)
        cv.poly(star(32, cy - hgt + 2, 5, 2.5, 4), rgb('#efe6ff'), ink=1.2)
    if mat == 'bastion':
        cv.rect(32 - pw / 2 - 3, cy - 6, pw + 6, 10, darken(side_col, 0.15), r=2, ink=2)
    cv.save(f"building/wall_{mat}_{mask}", pivot=(32, cy))


def gate(mat, horizontal):
    cv = Canvas(64, 112)
    cy = 112 - 32
    top_col, side_col = WALL_MATS[mat]
    cv.shadow(32, cy + 10, 28, 8, 70)
    if horizontal:
        for x in (6, 58):
            cv.rect(x - 8, cy - 58, 16, 66, top_col, r=3, ink=2.5)
        cv.rect(12, cy - 44, 40, 48, darken(WOOD, 0.2) if mat != 'rune' else rgb('#5a4a9a'), r=6, ink=3)
        for i in range(4):
            cv.line(17 + i * 10, cy - 40, 17 + i * 10, cy, INK, 1.5)
        cv.rect(4, cy - 64, 56, 10, top_col, r=2, ink=2.5)
    else:
        cv.rect(22, cy - 46, 20, 76, side_col, r=3, ink=2.5)
        cv.rect(18, cy - 54, 28, 12, top_col, r=2, ink=2.5)
        cv.rect(24, cy - 30, 16, 40, darken(WOOD, 0.2), r=4, ink=2.5)
    if mat == 'rune':
        cv.glow(32, cy - 30, 18, rgb('#b79cff'), 150)
    if mat == 'bastion':
        for x in (6, 58):
            cv.circle(x, cy - 30, 3, rgb('#e8c768'))
    cv.save(f"building/gate_{mat}_{'h' if horizontal else 'v'}", pivot=(32, cy))


def ruins():
    for size, key in ((1, 'ruin_1'), (2, 'ruin_2'), (3, 'ruin_3')):
        cv = Canvas(size * TILE + 24, size * TILE + 24)
        w, h = cv.w, cv.h
        rng = random.Random(size)
        cv.shadow(w / 2, h - size * 32, size * 28, size * 10, 60)
        for _ in range(6 * size):
            x, y = rng.uniform(12, w - 12), rng.uniform(h - size * TILE + 8, h - 8)
            cv.smooth([(x - 6, y + 3), (x - 3, y - 4), (x + 4, y - 5), (x + 7, y + 2)], rng.choice([STONE_D, darken(WOOD, 0.3), ROCK]), ink=2)
        for _ in range(3 * size):
            x, y = rng.uniform(12, w - 12), rng.uniform(h - size * TILE + 8, h - 8)
            cv.circle(x, y, rng.uniform(2, 5), (60, 55, 70), 120, blur=2)
        cv.save(f"building/{key}", pivot=(w / 2, h - size * TILE / 2 - 4))


def run():
    for k, n in (('grass', 4), ('dirt', 2), ('sand', 2), ('plateau', 2), ('ice', 2), ('water', 2), ('shallows', 2), ('chasm', 2), ('lava', 2), ('bridge', 1), ('mountain', 2), ('forest', 2)):
        for v in range(n):
            tile(k, v)
    for k in ('dirt', 'sand', 'plateau', 'ice', 'water', 'shallows', 'chasm', 'lava'):
        for v in range(3):
            blob(k, v)
    decor()
    for lv in ('1', '2', '3a', '3b'):
        castle(lv); house(lv); well(lv); archer(lv); shrine(lv)
    for lv in ('1', '2', 'legion', 'elite'):
        barracks(lv)
    for el in ('fire', 'water', 'lightning', 'ice', 'wind', 'earth', 'light', 'shadow'):
        for lv in ('1', '2', 'spire', 'catalyst'):
            magic(lv, el)
    for mat in WALL_MATS:
        for m in range(16):
            wall(mat, m)
        gate(mat, True); gate(mat, False)
    ruins()
