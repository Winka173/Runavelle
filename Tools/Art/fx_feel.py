"""Game-feel sprites: per-element particle silhouettes, wall damage/rubble/scorch decals, ambient critters and
castle-transformation props. White/neutral particles are tinted at runtime; decals carry their own colour."""
import math
import random

import skia

from artlib import Canvas, INK, rgb, lighten, darken


def particles():
    # ember: teardrop flame flick (rises)
    cv = Canvas(32, 32)
    cv.smooth([(16, 3), (23, 15), (21, 25), (16, 29), (11, 25), (9, 15)], (255, 255, 255), ink=0, shade=False)
    cv.save("fx/ember")
    # droplet: round with tail
    cv = Canvas(32, 32)
    cv.smooth([(16, 4), (24, 18), (22, 26), (16, 29), (10, 26), (8, 18)], (255, 255, 255), ink=0, shade=False)
    cv.circle(13, 19, 3, (255, 255, 255), 255)
    cv.save("fx/drop")
    # shard: long thin crystal (aligned to velocity)
    cv = Canvas(48, 16)
    cv.poly([(2, 8), (14, 2), (46, 8), (14, 14)], (255, 255, 255), ink=0, shade=False)
    cv.save("fx/shard")
    # ray: tapered beam (aligned)
    cv = Canvas(64, 16)
    p = cv.paint((255, 255, 255))
    path = skia.Path()
    path.moveTo(0, 8); path.lineTo(64, 3); path.lineTo(64, 13); path.close()
    cv.c.drawPath(path, p)
    cv.save("fx/ray")
    # arc segment: jagged bolt segment (aligned)
    cv = Canvas(64, 24)
    pts = [(0, 12), (14, 5), (26, 17), (40, 6), (52, 16), (64, 11)]
    path = skia.Path()
    path.moveTo(*pts[0])
    for q in pts[1:]:
        path.lineTo(*q)
    cv.c.drawPath(path, cv.paint((255, 255, 255), stroke=4))
    cv.save("fx/arcseg")
    # wisp: soft curling smoke tongue
    cv = Canvas(48, 48)
    for i in range(6):
        cv.circle(24 + math.sin(i) * 6, 40 - i * 6, 9 - i, (255, 255, 255), 150, blur=3)
    cv.save("fx/wisp")
    # swirl: open spiral stroke
    cv = Canvas(48, 48)
    path = skia.Path()
    for i in range(50):
        t = i / 49 * 5.5
        r = 4 + 16 * i / 49
        x, y = 24 + math.cos(t) * r, 24 + math.sin(t) * r
        path.moveTo(x, y) if i == 0 else path.lineTo(x, y)
    cv.c.drawPath(path, cv.paint((255, 255, 255), stroke=3.5))
    cv.save("fx/swirl")
    # fragment: angular rock chip
    cv = Canvas(24, 24)
    cv.poly([(4, 8), (12, 3), (20, 9), (18, 19), (8, 21)], (255, 255, 255), ink=0, shade=False)
    cv.save("fx/chip")


def decals():
    rnd = random.Random(42)
    # wall crack overlays (two severities): dark ink fissures
    for k, n in (("crack_1", 2), ("crack_2", 5)):
        cv = Canvas(64, 64)
        for _ in range(n):
            x, y = rnd.uniform(14, 50), rnd.uniform(10, 30)
            path = skia.Path()
            path.moveTo(x, y)
            for _s in range(4):
                x += rnd.uniform(-8, 8); y += rnd.uniform(5, 10)
                path.lineTo(x, y)
            cv.c.drawPath(path, cv.paint(INK, 230, stroke=2.6))
            cv.c.drawPath(path, cv.paint((210, 200, 190), 120, stroke=0.8))
        cv.save(f"fx/{k}")
    # rubble decal: scattered stones + dust (ground)
    for v in range(3):
        cv = Canvas(96, 64)
        cv.ellipse(48, 36, 40, 18, (120, 110, 100), ink=0, shade=False)
        cv.c.drawOval(skia.Rect.MakeXYWH(8, 18, 80, 36), cv.paint((150, 138, 120), 120, blur=6))
        for _ in range(14):
            x, y, r = rnd.uniform(14, 82), rnd.uniform(22, 50), rnd.uniform(3, 8)
            col = rnd.choice([(150, 142, 150), (128, 118, 128), (170, 160, 150), (120, 90, 60)])
            cv.poly([(x - r, y), (x - r * 0.3, y - r * 0.8), (x + r, y - r * 0.4), (x + r * 0.7, y + r * 0.6), (x - r * 0.5, y + r * 0.7)], col, ink=1.8)
        cv.save(f"fx/rubble_{v}")
    # scorch decal: burnt ground
    cv = Canvas(96, 64)
    cv.c.drawOval(skia.Rect.MakeXYWH(6, 10, 84, 44), cv.paint((30, 22, 20), 170, blur=10))
    for _ in range(10):
        a = rnd.uniform(0, math.tau)
        cv.inked_line(48, 32, 48 + math.cos(a) * 40, 32 + math.sin(a) * 22, (40, 28, 24), 3)
    cv.save("fx/scorch")
    # frost decal: ice slick
    cv = Canvas(96, 64)
    cv.c.drawOval(skia.Rect.MakeXYWH(6, 10, 84, 44), cv.paint((200, 235, 255), 150, blur=5))
    for _ in range(6):
        x, y = rnd.uniform(20, 76), rnd.uniform(20, 44)
        cv.inked_line(x, y, x + rnd.uniform(-14, 14), y + rnd.uniform(-6, 6), (255, 255, 255), 2)
    cv.save("fx/frost")
    # wet decal: puddle
    cv = Canvas(96, 64)
    cv.c.drawOval(skia.Rect.MakeXYWH(10, 14, 76, 36), cv.paint((80, 140, 220), 150, blur=4))
    cv.c.drawOval(skia.Rect.MakeXYWH(24, 22, 30, 10), cv.paint((220, 240, 255), 160, blur=2))
    cv.save("fx/puddle")


def ambient():
    # bird (2 frames, wings up/down) and leaf
    for f in (0, 1):
        cv = Canvas(32, 24)
        wy = 6 if f == 0 else 18
        path = skia.Path()
        path.moveTo(2, wy); path.quadTo(9, 10, 16, 13); path.quadTo(23, 10, 30, wy)
        cv.c.drawPath(path, cv.paint((60, 50, 60), stroke=2.6))
        cv.save(f"decor/bird_{f}")
    cv = Canvas(24, 24)
    cv.smooth([(4, 18), (10, 6), (20, 4), (16, 14), (8, 20)], (255, 255, 255), ink=1.5)
    cv.save("fx/leaf")


def castle_props():
    # Fortress: extra battlement crown; Legion: drill banners; Arcanum: floating crystals
    cv = Canvas(96, 48)
    for i in range(5):
        cv.rect(6 + i * 18, 10, 12, 16, rgb('#8d849e'), r=2, ink=2.5)
    cv.rect(4, 22, 88, 18, rgb('#7a7290'), r=3, ink=2.5)
    cv.save("building/prop_fortress", pivot=(48, 44))
    cv = Canvas(48, 96)
    cv.rect(21, 10, 6, 84, rgb('#5a3a2a'), r=2, ink=2)
    cv.poly([(27, 12), (46, 18), (40, 28), (46, 38), (27, 42)], rgb('#c4553e'), ink=2.5)
    cv.save("building/prop_legion", pivot=(24, 92))
    cv = Canvas(48, 64)
    cv.poly([(24, 4), (36, 28), (24, 60), (12, 28)], rgb('#a68cff'), ink=2.5)
    cv.poly([(24, 4), (30, 28), (24, 60)], lighten(rgb('#a68cff'), 0.35), ink=0)
    cv.save("building/prop_arcanum", pivot=(24, 60))


def run():
    particles()
    decals()
    ambient()
    castle_props()
