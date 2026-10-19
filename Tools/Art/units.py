"""Characters: soldiers (12 classes x base/branch A/B), enemies, bosses, 16 Generals + portraits.
Each unit gets two frames: <key>_0 (idle/walk) and <key>_1 (attack pose). Facing right; Unity flips."""
import math
import random

from artlib import Canvas, INK, rgb, lighten, darken, mix, star

SKIN = rgb('#f2c9a0')
STEEL = rgb('#c9ced8')
GOLD = rgb('#e8c25a')
LEATHER = rgb('#8a5a3a')
WOOD = rgb('#a8733f')
EL = {
    'fire': rgb('#ff7a2e'), 'water': rgb('#3f9bff'), 'lightning': rgb('#ffe14a'), 'ice': rgb('#9ff0ff'),
    'wind': rgb('#8ff0b0'), 'earth': rgb('#c9965a'), 'light': rgb('#fff1a8'), 'shadow': rgb('#a35cf0'), 'none': None,
}


def weapon(cv, kind, hx, hy, s, col=STEEL, atk=False, el=None):
    """Draw weapon held at hand (hx, hy). s = scale."""
    a = -0.9 if atk else -2.2  # angle (radians, screen space): attack = forward/down swing
    def tip(length, ang):
        return hx + math.cos(ang) * length * s, hy + math.sin(ang) * length * s
    if kind == 'sword':
        x1, y1 = tip(26, a)
        cv.inked_line(hx, hy, x1, y1, col, 4 * s)
        cv.inked_line(hx - 5 * s * math.sin(a), hy + 5 * s * math.cos(a), hx + 5 * s * math.sin(a), hy - 5 * s * math.cos(a), GOLD, 3 * s)
    elif kind == 'greatsword':
        x1, y1 = tip(40, a)
        cv.inked_line(hx, hy, x1, y1, col, 6 * s)
    elif kind == 'axe' or kind == 'hammer' or kind == 'club':
        ang = a
        x1, y1 = tip(28, ang)
        cv.inked_line(hx, hy, x1, y1, WOOD, 3.5 * s)
        if kind == 'axe':
            cv.poly([(x1, y1), (x1 + 10 * s, y1 - 2 * s), (x1 + 8 * s, y1 + 10 * s)], col, ink=2.2)
        elif kind == 'hammer':
            cv.rect(x1 - 8 * s, y1 - 6 * s, 16 * s, 12 * s, col, r=2, ink=2.2)
        else:
            cv.ellipse(x1, y1, 7 * s, 9 * s, darken(WOOD, 0.1), ink=2.2)
    elif kind in ('spear', 'lance', 'trident'):
        ang = -0.25 if atk else -1.3
        x0, y0 = hx - math.cos(ang) * 14 * s, hy - math.sin(ang) * 14 * s
        x1, y1 = tip(38 if kind != 'lance' else 46, ang)
        cv.inked_line(x0, y0, x1, y1, WOOD if kind != 'lance' else col, 3 * s)
        if kind == 'trident':
            for d in (-4, 0, 4):
                cv.inked_line(x1 + d * s * math.sin(ang), y1 - d * s * math.cos(ang), x1 + math.cos(ang) * 7 * s + d * s * math.sin(ang), y1 + math.sin(ang) * 7 * s - d * s * math.cos(ang), col, 2 * s)
        else:
            cv.poly([(x1 + math.cos(ang) * 10 * s, y1 + math.sin(ang) * 10 * s), (x1 + 4 * s * math.sin(ang), y1 - 4 * s * math.cos(ang)), (x1 - 4 * s * math.sin(ang), y1 + 4 * s * math.cos(ang))], col, ink=2)
    elif kind in ('bow', 'crossbow'):
        if kind == 'bow':
            r = 14 * s
            cv.c.drawArc(__import__('skia').Rect.MakeXYWH(hx - r * 0.4, hy - r, r, r * 2), -80, 160, False, cv.paint(INK, stroke=5 * s))
            cv.c.drawArc(__import__('skia').Rect.MakeXYWH(hx - r * 0.4, hy - r, r, r * 2), -80, 160, False, cv.paint(WOOD, stroke=3 * s))
            cv.line(hx + r * 0.1, hy - r * 0.98, hx + r * 0.1, hy + r * 0.98, (240, 230, 210), 1.2)
            if atk:
                cv.inked_line(hx - 12 * s, hy, hx + 14 * s, hy, lighten(WOOD, 0.3), 1.5)
        else:
            cv.rect(hx - 4 * s, hy - 3 * s, 22 * s, 6 * s, WOOD, r=2, ink=2)
            cv.inked_line(hx + 14 * s, hy - 10 * s, hx + 14 * s, hy + 10 * s, darken(WOOD, 0.2), 3 * s)
    elif kind in ('staff', 'orbstaff'):
        cv.inked_line(hx, hy + 14 * s, hx + 4 * s, hy - 26 * s, darken(WOOD, 0.1), 3.5 * s)
        oc = el or rgb('#b79cff')
        cv.glow(hx + 4 * s, hy - 30 * s, (14 if atk else 9) * s, oc, 200)
        cv.ellipse(hx + 4 * s, hy - 30 * s, 5 * s, 5 * s, lighten(oc, 0.3), ink=2)
    elif kind == 'dagger':
        x1, y1 = tip(14, a)
        cv.inked_line(hx, hy, x1, y1, col, 3 * s)
    elif kind == 'sling':
        cv.line(hx, hy, hx + 10 * s, hy - (12 if atk else 4) * s, LEATHER, 2)
        cv.circle(hx + 10 * s, hy - (12 if atk else 4) * s, 3 * s, rgb('#8d849e'))
    elif kind == 'scythe':
        x1, y1 = tip(34, a)
        cv.inked_line(hx, hy, x1, y1, darken(WOOD, 0.3), 3 * s)
        cv.c.drawArc(__import__('skia').Rect.MakeXYWH(x1 - 14 * s, y1 - 6 * s, 20 * s, 14 * s), 180, 160, False, cv.paint(INK, stroke=5 * s))
        cv.c.drawArc(__import__('skia').Rect.MakeXYWH(x1 - 14 * s, y1 - 6 * s, 20 * s, 14 * s), 180, 160, False, cv.paint(col, stroke=3 * s))
    elif kind == 'twin':
        weapon(cv, 'dagger', hx, hy, s, col, atk)
        weapon(cv, 'dagger', hx - 10 * s, hy + 3 * s, s, col, not atk)
    if el and kind not in ('staff', 'orbstaff', 'bow'):
        cv.glow(hx + math.cos(a) * 20 * s, hy + math.sin(a) * 20 * s, 10 * s, el, 140)


def humanoid(key, body, *, skin=SKIN, head='helm', helm_col=STEEL, weapon_kind='sword', weapon_col=STEEL, shield=None, cape=None,
             scale=1.0, size=96, ears=False, tusks=False, el=None, mount=None, plume=None, wings=None, belly=1.0, legs=None, eye=INK,
             crown=False, halo=False, horns=False, robe=False, hood=None, base_bottom=8):
    for frame in (0, 1):
        atk = frame == 1
        cv = Canvas(size, size)
        s = scale
        cx = size / 2 - 4 * s
        ground = size - base_bottom
        cv.shadow(cx + 2 * s, ground - 2, 16 * s * (1.6 if mount else 1), 5 * s, 80)
        y_hip = ground - (14 * s if not mount else 26 * s)
        if mount:
            mount_draw(cv, mount, cx, ground, s, atk)
        if wings:
            for side in (-1, 1):
                wy = y_hip - 30 * s
                cv.poly([(cx, wy), (cx - 26 * s, wy - (18 if atk else 8) * s), (cx - 30 * s, wy + 4 * s), (cx - 18 * s, wy + 6 * s), (cx - 22 * s, wy + 14 * s), (cx - 6 * s, wy + 8 * s)], wings, ink=2.5)
        if cape:
            cv.poly([(cx - 9 * s, y_hip - 28 * s), (cx - 16 * s, y_hip + 4 * s), (cx + 2 * s, y_hip + 2 * s), (cx + 6 * s, y_hip - 28 * s)], darken(cape, 0.1), ink=2.5)
        # legs
        if not mount:
            lc = legs or darken(body, 0.35)
            step = 3 * s if atk else 0
            cv.rect(cx - 9 * s - step, y_hip - 2 * s, 7 * s, 15 * s, lc, r=3 * s, ink=2.5)
            cv.rect(cx + 2 * s + step, y_hip - 2 * s, 7 * s, 15 * s, lc, r=3 * s, ink=2.5)
            cv.rect(cx - 10 * s - step, ground - 5 * s, 9 * s, 5 * s, darken(LEATHER, 0.2), r=2 * s, ink=2)
            cv.rect(cx + 1 * s + step, ground - 5 * s, 9 * s, 5 * s, darken(LEATHER, 0.2), r=2 * s, ink=2)
        # torso
        tw = 15 * s * belly
        top = y_hip - 28 * s * (1 + (belly - 1) * 0.35)
        if robe:
            cv.poly([(cx - tw * 0.75, top), (cx + tw * 0.75, top), (cx + tw * 1.15, y_hip + 12 * s), (cx - tw * 1.15, y_hip + 12 * s)], body, ink=3)
        else:
            cv.smooth([(cx - tw, top + 4 * s), (cx + tw, top + 4 * s), (cx + tw * 1.05, y_hip - 4 * s), (cx + tw * 0.8, y_hip + 4 * s), (cx - tw * 0.8, y_hip + 4 * s), (cx - tw * 1.05, y_hip - 4 * s)], body, ink=3)
            cv.rect(cx - tw * 0.9, y_hip - 6 * s, tw * 1.8, 5 * s, darken(LEATHER, 0.1), r=2, ink=2)
            cv.rect(cx - 3 * s, y_hip - 7 * s, 6 * s, 7 * s, GOLD, r=1.5, ink=1.5)
        # back arm + shield
        if shield:
            sx, sy = cx - tw - 2 * s, y_hip - 14 * s
            cv.smooth([(sx - 9 * s, sy - 12 * s), (sx + 7 * s, sy - 12 * s), (sx + 8 * s, sy + 4 * s), (sx - 1 * s, sy + 14 * s), (sx - 10 * s, sy + 4 * s)], shield, ink=3)
            cv.poly(star(sx - 1 * s, sy - 1 * s, 5 * s, 2.2 * s), GOLD, ink=1.5)
        # head
        hr = 11 * s * (1 + (belly - 1) * 0.6)
        hy = top - hr + 1 * s
        if ears:
            cv.poly([(cx - hr + 1, hy - 2 * s), (cx - hr - 12 * s, hy - 8 * s), (cx - hr + 3, hy + 4 * s)], skin, ink=2.5)
            cv.poly([(cx + hr - 1, hy - 2 * s), (cx + hr + 12 * s, hy - 8 * s), (cx + hr - 3, hy + 4 * s)], skin, ink=2.5)
        cv.ellipse(cx, hy, hr, hr * 0.98, skin, ink=3)
        # eyes (looking right)
        cv.ellipse(cx + 3 * s, hy + 1 * s, 1.8 * s, 2.6 * s, eye, ink=0, shade=False)
        cv.ellipse(cx + 8 * s, hy + 1 * s, 1.6 * s, 2.4 * s, eye, ink=0, shade=False)
        if eye != INK:
            cv.glow(cx + 5 * s, hy + 1 * s, 8 * s, eye, 120)
        if tusks:
            cv.poly([(cx + 4 * s, hy + 6 * s), (cx + 6 * s, hy + 1 * s), (cx + 8 * s, hy + 6 * s)], (250, 245, 230), ink=1.5)
        if head == 'helm':
            cv.c.save()
            import skia
            p = skia.Path(); p.addOval(skia.Rect.MakeLTRB(cx - hr - 1, hy - hr - 2, cx + hr + 1, hy + hr * 0.2))
            cv.shape(p, helm_col, ink=3)
            cv.c.restore()
            cv.rect(cx - 1 * s, hy - hr * 0.3, 10 * s, 2.5 * s, darken(helm_col, 0.3), r=1, ink=0, shade=False)
        elif head == 'hood':
            cv.smooth([(cx - hr - 3, hy + 6 * s), (cx - hr - 2, hy - 8 * s), (cx - 2 * s, hy - hr - 6 * s), (cx + hr + 2, hy - 6 * s), (cx + hr - 2, hy - 2 * s), (cx - 2 * s, hy - 4 * s)], hood or body, ink=3)
        elif head == 'hat':
            cv.poly([(cx - hr - 8 * s, hy - 4 * s), (cx + hr + 8 * s, hy - 4 * s), (cx + 2 * s, hy - hr - 22 * s)], hood or body, ink=3)
        elif head == 'hair':
            cv.smooth([(cx - hr - 2, hy + 4 * s), (cx - hr, hy - 8 * s), (cx, hy - hr - 4 * s), (cx + hr, hy - 8 * s), (cx + hr * 0.4, hy - 5 * s), (cx - hr * 0.3, hy - 2 * s), (cx - hr * 0.6, hy + 8 * s)], hood or rgb('#5a3a2a'), ink=2.5)
        elif head == 'bandana':
            cv.rect(cx - hr, hy - hr * 0.6, hr * 2, 6 * s, hood or rgb('#c4553e'), r=3, ink=2)
        elif head == 'skull':
            cv.ellipse(cx + 3 * s, hy + 1 * s, 2.6 * s, 3 * s, INK, ink=0, shade=False)
            cv.ellipse(cx + 9 * s, hy + 1 * s, 2.4 * s, 2.8 * s, INK, ink=0, shade=False)
        if plume:
            cv.smooth([(cx - 2 * s, hy - hr), (cx - 10 * s, hy - hr - 12 * s), (cx - 2 * s, hy - hr - 18 * s), (cx + 4 * s, hy - hr - 8 * s)], plume, ink=2.5)
        if horns:
            for d in (-1, 1):
                cv.poly([(cx + d * 6 * s, hy - hr + 3 * s), (cx + d * 16 * s, hy - hr - 12 * s), (cx + d * 10 * s, hy - hr + 4 * s)], (240, 230, 210), ink=2)
        if crown:
            cv.poly([(cx - 9 * s, hy - hr + 2 * s), (cx - 9 * s, hy - hr - 7 * s), (cx - 4 * s, hy - hr - 2 * s), (cx, hy - hr - 9 * s), (cx + 4 * s, hy - hr - 2 * s), (cx + 9 * s, hy - hr - 7 * s), (cx + 9 * s, hy - hr + 2 * s)], GOLD, ink=2)
        if halo:
            cv.c.drawOval(__import__('skia').Rect.MakeLTRB(cx - 10 * s, hy - hr - 9 * s, cx + 10 * s, hy - hr - 3 * s), cv.paint(GOLD, stroke=2.5 * s))
            cv.glow(cx, hy - hr - 6 * s, 14 * s, rgb('#fff1a8'), 120)
        # front arm + weapon
        hx, hy2 = cx + tw * 0.9, y_hip - 14 * s - (4 * s if atk else 0)
        weapon(cv, weapon_kind, hx, hy2, s, weapon_col, atk, el)
        cv.ellipse(hx, hy2, 4 * s, 4 * s, skin if not robe else lighten(body, 0.1), ink=2)
        if el and frame == 1:
            cv.glow(hx + 10 * s, hy2 - 6 * s, 16 * s, el, 110)
        cv.save(f"{key}_{frame}", pivot=(cx, ground - 4), ppu=64)


def mount_draw(cv, kind, cx, ground, s, atk):
    if kind == 'horse':
        col = rgb('#8a5a3a')
        bx = cx + 4 * s
        for lx in (-18, -8, 10, 18):
            cv.rect(bx + lx * s - 3 * s, ground - 18 * s + (3 * s if atk and lx > 0 else 0), 6 * s, 18 * s, darken(col, 0.15), r=2, ink=2.2)
        cv.ellipse(bx, ground - 22 * s, 24 * s, 11 * s, col, ink=3)
        cv.poly([(bx + 18 * s, ground - 26 * s), (bx + 30 * s, ground - 44 * s), (bx + 40 * s, ground - 38 * s), (bx + 26 * s, ground - 20 * s)], col, ink=3)
        cv.poly([(bx + 26 * s, ground - 46 * s), (bx + 20 * s, ground - 34 * s), (bx + 24 * s, ground - 28 * s)], rgb('#3a2a20'), ink=2)
        cv.rect(bx - 10 * s, ground - 34 * s, 18 * s, 8 * s, rgb('#b8452e'), r=2, ink=2)
    elif kind == 'wolf':
        col = rgb('#7d7a8c')
        bx = cx + 4 * s
        for lx in (-16, -8, 8, 16):
            cv.rect(bx + lx * s - 3 * s, ground - 14 * s, 5 * s, 14 * s, darken(col, 0.15), r=2, ink=2)
        cv.ellipse(bx, ground - 18 * s, 21 * s, 9 * s, col, ink=3)
        cv.poly([(bx + 14 * s, ground - 22 * s), (bx + 30 * s, ground - 26 * s), (bx + 34 * s, ground - 18 * s), (bx + 18 * s, ground - 14 * s)], col, ink=2.5)
        cv.poly([(bx + 20 * s, ground - 25 * s), (bx + 22 * s, ground - 34 * s), (bx + 26 * s, ground - 25 * s)], col, ink=2)
        cv.circle(bx + 28 * s, ground - 22 * s, 1.6 * s, rgb('#ffcf4a'))
    elif kind == 'griffin':
        col = rgb('#d8b27a')
        bx = cx + 2 * s
        cv.poly([(bx - 6 * s, ground - 30 * s), (bx - 34 * s, ground - (52 if atk else 40) * s), (bx - 30 * s, ground - 22 * s)], rgb('#f2e6cc'), ink=2.5)
        cv.ellipse(bx, ground - 24 * s, 22 * s, 10 * s, col, ink=3)
        cv.ellipse(bx + 22 * s, ground - 34 * s, 8 * s, 8 * s, rgb('#f2e6cc'), ink=2.5)
        cv.poly([(bx + 28 * s, ground - 34 * s), (bx + 36 * s, ground - 31 * s), (bx + 28 * s, ground - 29 * s)], GOLD, ink=2)
        for lx in (-12, 12):
            cv.rect(bx + lx * s - 2 * s, ground - 16 * s, 5 * s, 12 * s, darken(col, 0.2), r=2, ink=2)


# ------------------------------------------------------------------------------------------------ soldiers
BLUE = rgb('#4e74c4')
SOLDIERS = {
    'swordsman': dict(body=BLUE, head='helm', weapon_kind='sword', shield=rgb('#c4553e')),
    'spearman': dict(body=rgb('#5f8f4f'), head='helm', weapon_kind='spear'),
    'archer': dict(body=rgb('#5a8a45'), head='hood', hood=rgb('#4f7a3a'), weapon_kind='bow'),
    'mage': dict(body=rgb('#6a4fb0'), head='hat', hood=rgb('#5a3fa0'), weapon_kind='orbstaff', robe=True),
    'knight': dict(body=STEEL, head='helm', plume=rgb('#c4553e'), weapon_kind='sword', shield=BLUE, belly=1.15),
    'cavalry': dict(body=BLUE, head='helm', plume=GOLD, weapon_kind='lance', mount='horse'),
    'shieldguardian': dict(body=rgb('#7d8fa8'), head='helm', weapon_kind='dagger', shield=rgb('#d9c06a'), belly=1.2),
    'crossbowman': dict(body=rgb('#8a6a45'), head='bandana', hood=rgb('#4e74c4'), weapon_kind='crossbow'),
    'battlemage': dict(body=rgb('#4f5fa8'), head='hood', hood=rgb('#3a4a90'), weapon_kind='sword', robe=True, cape=rgb('#7a5fd0')),
    'elementalknight': dict(body=rgb('#8d849e'), head='helm', plume=rgb('#ff7a2e'), weapon_kind='greatsword', belly=1.15, cape=rgb('#c4553e')),
    'griffinrider': dict(body=rgb('#e0c07a'), head='helm', helm_col=GOLD, weapon_kind='spear', mount='griffin'),
    'runegolem': dict(body=rgb('#8d849e'), skin=rgb('#a69cb8'), head='none', weapon_kind='club', weapon_col=rgb('#8d849e'), belly=1.6, eye=rgb('#9fe7ff')),
}
BRANCH_EL = {
    'swordsman': ('fire', None), 'spearman': ('earth', None), 'archer': ('lightning', 'ice'), 'mage': ('fire', 'water'),
    'knight': ('earth', 'light'), 'cavalry': ('wind', 'lightning'), 'shieldguardian': ('light', 'earth'), 'crossbowman': ('shadow', 'fire'),
    'battlemage': ('lightning', 'ice'), 'elementalknight': ('fire', 'water'), 'griffinrider': ('wind', 'lightning'), 'runegolem': ('earth', 'ice'),
}


def soldiers():
    for cls, recipe in SOLDIERS.items():
        size = 96 if recipe.get('mount') or cls == 'runegolem' else 80
        scale = 0.95 if cls != 'runegolem' else 1.15
        base = dict(recipe)
        humanoid(f"unit/{cls}", scale=scale, size=size, **base)
        for i, br in enumerate(('a', 'b')):
            el = BRANCH_EL[cls][i]
            r = dict(recipe)
            if el:
                col = EL[el]
                r['el'] = col
                if 'cape' in r or cls in ('knight', 'swordsman', 'elementalknight'):
                    r['cape'] = darken(col, 0.15)
                r['body'] = mix(r['body'], col, 0.35)
            else:
                r['body'] = darken(r['body'], 0.1)
                r['cape'] = rgb('#c4553e')
            if cls == 'swordsman' and br == 'b':
                r['shield'] = rgb('#d9c06a')
            humanoid(f"unit/{cls}_{br}", scale=scale, size=size, **r)


# ------------------------------------------------------------------------------------------------ enemies
GOB = rgb('#7fb04a')
ENEMIES = {
    'goblin': dict(body=rgb('#8a5a3a'), skin=GOB, head='none', ears=True, weapon_kind='dagger', scale=0.8, eye=rgb('#ffdd55')),
    'orc_brute': dict(body=rgb('#6b4a3a'), skin=rgb('#6f9a4a'), head='bandana', hood=rgb('#8a2f2f'), tusks=True, weapon_kind='axe', scale=1.15, belly=1.3, size=96),
    'kobold_slinger': dict(body=rgb('#9a6a3a'), skin=rgb('#c98a4a'), head='none', ears=True, weapon_kind='sling', scale=0.78),
    'wolf_rider': dict(body=rgb('#8a5a3a'), skin=GOB, head='none', ears=True, weapon_kind='spear', scale=0.8, mount='wolf', size=96),
    'fire_imp': dict(body=rgb('#d0452e'), skin=rgb('#e0603a'), head='none', horns=True, weapon_kind='orbstaff', el=EL['fire'], scale=0.75, wings=rgb('#a83a2a'), eye=rgb('#ffe14a')),
    'shieldbearer': dict(body=rgb('#5a5a6a'), skin=rgb('#6f9a4a'), head='helm', helm_col=rgb('#6a6a7a'), weapon_kind='sword', shield=rgb('#4a4a5a'), scale=1.0, belly=1.2),
    'harpy': dict(body=rgb('#8a6aa8'), skin=rgb('#c9a8d8'), head='hair', hood=rgb('#5a3a6a'), weapon_kind='dagger', wings=rgb('#7a5a98'), scale=0.85, eye=rgb('#ff5a5a')),
    'hex_shaman': dict(body=rgb('#7a4a2a'), skin=GOB, head='hat', hood=rgb('#3a2a4a'), ears=True, weapon_kind='orbstaff', el=EL['fire'], robe=True, scale=0.9),
    'ogre_gatesmasher': dict(body=rgb('#7a5a3a'), skin=rgb('#a8a07a'), head='none', tusks=True, weapon_kind='club', scale=1.45, belly=1.5, size=128),
    'bone_summoner': dict(body=rgb('#3a2a4a'), skin=rgb('#e8e0cc'), head='hood', hood=rgb('#2a1f3a'), weapon_kind='orbstaff', el=EL['shadow'], robe=True, scale=0.95, eye=rgb('#a35cf0')),
    'skeleton': dict(body=rgb('#d8d0bc'), skin=rgb('#ece6d6'), head='skull', weapon_kind='sword', weapon_col=rgb('#8d849e'), scale=0.85, legs=rgb('#d8d0bc')),
    'dark_champion': dict(body=rgb('#2f2a3a'), skin=rgb('#5a5a6a'), head='helm', helm_col=rgb('#2f2a3a'), horns=True, weapon_kind='greatsword', weapon_col=rgb('#8a5ab0'), cape=rgb('#5a1f2f'), scale=1.25, belly=1.2, size=112, eye=rgb('#ff4a4a')),
}


def ram_crew():
    for frame in (0, 1):
        cv = Canvas(112, 96)
        g = 88
        cv.shadow(56, g - 2, 44, 8, 90)
        push = 6 if frame else 0
        for x in (24, 88):
            cv.ellipse(x, g - 12, 11, 11, darken(WOOD, 0.2), ink=3)
        cv.rect(14, g - 46, 84, 30, rgb('#8a5a3a'), r=4, ink=3)
        cv.poly([(10, g - 46), (56, g - 70), (102, g - 46)], rgb('#6b4a3a'), ink=3)
        cv.rect(30 + push, g - 38, 84, 12, WOOD, r=5, ink=3)
        cv.ellipse(112 - 4 + push - 4, g - 32, 8, 9, rgb('#8d849e'), ink=3)
        for x in (32, 52):
            cv.ellipse(x, g - 54, 7, 7, GOB, ink=2.5)
        cv.save(f"enemy/ram_crew_{frame}", pivot=(56, g - 4))


def enemies():
    for k, r in ENEMIES.items():
        r = dict(r)
        size = r.pop('size', 96 if r.get('mount') else 80)
        humanoid(f"enemy/{k}", size=size, **r)
    ram_crew()


def bosses():
    humanoid("enemy/boss_gatebreaker", body=rgb('#6b4a3a'), skin=rgb('#8aa05a'), head='bandana', hood=rgb('#8a2f2f'), tusks=True, weapon_kind='hammer', weapon_col=rgb('#8d849e'),
             scale=2.0, belly=1.6, size=176, eye=rgb('#ff5a3a'))
    humanoid("enemy/boss_rift_warlock", body=rgb('#2a1f4a'), skin=rgb('#a69cb8'), head='hood', hood=rgb('#1f1636'), weapon_kind='orbstaff', el=EL['shadow'], robe=True,
             scale=1.8, size=160, eye=rgb('#c27aff'), cape=rgb('#3a2a6a'))
    humanoid("enemy/boss_siege_lord", body=rgb('#3a3036'), skin=rgb('#5a4a4a'), head='helm', helm_col=rgb('#3a3036'), horns=True, crown=True, weapon_kind='greatsword', weapon_col=rgb('#ff7a2e'),
             el=EL['fire'], cape=rgb('#8a2f1f'), scale=2.2, belly=1.5, size=192, eye=rgb('#ffb43a'))


# ------------------------------------------------------------------------------------------------ generals
GENERALS = {
    'roland': dict(body=rgb('#9aa6b8'), head='helm', plume=rgb('#4e74c4'), weapon_kind='hammer', cape=rgb('#4e74c4'), shield=rgb('#4e74c4'), belly=1.2),
    'lyra': dict(body=rgb('#4f6fc8'), head='hair', hood=rgb('#e8e0ff'), weapon_kind='orbstaff', el=EL['lightning'], robe=True, cape=rgb('#2f4f9a')),
    'kaelen': dict(body=rgb('#b8452e'), head='hair', hood=rgb('#e86a2a'), weapon_kind='sword', weapon_col=rgb('#ffcf6a'), el=EL['fire'], cape=rgb('#8a2f1f')),
    'elowen': dict(body=rgb('#4f8a5a'), head='hood', hood=rgb('#3f7a48'), weapon_kind='bow', el=EL['wind'], cape=rgb('#8ff0b0')),
    'seraphine': dict(body=rgb('#f2ead6'), head='hair', hood=rgb('#f2d36b'), halo=True, weapon_kind='orbstaff', el=EL['light'], robe=True, cape=rgb('#e8c25a')),
    'varric': dict(body=rgb('#3a2f4a'), head='helm', helm_col=rgb('#3a2f4a'), horns=True, weapon_kind='scythe', weapon_col=rgb('#b79cff'), el=EL['shadow'], cape=rgb('#5a3a8a')),
    'tharion': dict(body=rgb('#9a8a6a'), skin=rgb('#d8b890'), head='helm', helm_col=rgb('#8d849e'), weapon_kind='club', weapon_col=rgb('#8d849e'), shield=rgb('#8d849e'), belly=1.45, el=EL['earth']),
    'aurelia': dict(body=rgb('#e8c25a'), head='helm', helm_col=GOLD, plume=rgb('#fff1a8'), weapon_kind='spear', shield=rgb('#f2ead6'), el=EL['light'], cape=rgb('#f2ead6')),
    'nyxara': dict(body=rgb('#2a2236'), head='hood', hood=rgb('#3a2a52'), weapon_kind='twin', weapon_col=rgb('#c9a8ff'), el=EL['shadow'], eye=rgb('#c27aff')),
    'isolde': dict(body=rgb('#9fd8f0'), head='hair', hood=rgb('#e8f6ff'), weapon_kind='orbstaff', el=EL['ice'], robe=True, cape=rgb('#5fa8d8')),
    'maren': dict(body=rgb('#2f8a9a'), head='hair', hood=rgb('#1f5a7a'), weapon_kind='trident', weapon_col=rgb('#9fe7ff'), el=EL['water'], cape=rgb('#3f9bff')),
    'brannoc': dict(body=rgb('#c9a03a'), head='helm', helm_col=rgb('#e8d07a'), plume=rgb('#ffe14a'), weapon_kind='lance', el=EL['lightning'], mount='horse'),
    'pyra': dict(body=rgb('#8a2f3a'), head='hat', hood=rgb('#5a1f2a'), weapon_kind='orbstaff', el=EL['fire'], robe=True, cape=rgb('#ff7a2e')),
    'corvin': dict(body=rgb('#5a7a8a'), head='hood', hood=rgb('#3a5a6a'), weapon_kind='spear', el=EL['wind'], cape=rgb('#d8e8f0'), wings=rgb('#c8d8e0')),
    'hilda': dict(body=rgb('#7fa8c8'), head='helm', helm_col=rgb('#c8e8f8'), horns=True, weapon_kind='axe', weapon_col=rgb('#c8f0ff'), shield=rgb('#5f8fb8'), el=EL['ice'], belly=1.25),
    'maelis': dict(body=rgb('#3f6fa8'), head='helm', helm_col=rgb('#9fd0f0'), weapon_kind='trident', el=EL['water'], shield=rgb('#3f9bff'), cape=rgb('#9fd0f0'), crown=True),
}


def generals():
    from artlib import Canvas as C
    for gid, r in GENERALS.items():
        r = dict(r)
        size = 128 if r.get('mount') else 112
        humanoid(f"general/{gid}", scale=1.25, size=size, **r)
        # portrait: framed bust (reuse body render scaled)
        import skia
        src = skia.Image.open(f"{__import__('artlib').OUT_ROOT}/general/{gid}_0.png")
        cv = C(128, 128)
        cv.rect(4, 4, 120, 120, darken(r['body'], 0.45), r=18, ink=4,
                gradient=skia.GradientShader.MakeRadial(skia.Point(64, 50), 90, [skia.Color(*lighten(r.get('cape', r['body']), 0.2), 255), skia.Color(*darken(r['body'], 0.5), 255)]))
        cv.c.save()
        cv.c.clipRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(6, 6, 116, 116), 16, 16), True)
        k = 2.0
        cv.c.drawImageRect(src, skia.Rect.MakeXYWH(64 - src.width() * k / 2 + 6, 128 - src.height() * k + 92, src.width() * k, src.height() * k), skia.SamplingOptions(skia.FilterMode.kLinear))
        cv.c.restore()
        cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(4, 4, 120, 120), 18, 18), cv.paint(INK, stroke=4))
        cv.c.drawRRect(skia.RRect.MakeRectXY(skia.Rect.MakeXYWH(8, 8, 112, 112), 14, 14), cv.paint(GOLD, 180, stroke=2))
        cv.save(f"portrait/{gid}")


def run():
    soldiers()
    enemies()
    bosses()
    generals()
