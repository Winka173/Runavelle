"""Phase IV enemy + boss sprites (6 factions, 8 bosses). Humanoids reuse units.humanoid; beasts/machines are bespoke."""
import math
from artlib import Canvas, rgb, darken, lighten, INK
from units import humanoid, EL, STEEL, WOOD, GOLD

BONE = rgb('#e6dcc4')
ROT = rgb('#7f9a6a')
STONE = rgb('#9a94a6')
ASH = rgb('#4a3a3a')
MAGMA = rgb('#ff7a2e')
FROST = rgb('#bfe8ff')
TEMPEST = rgb('#7ab8d8')

HUMANOIDS = {
    # Undead Legion
    'zombie_shambler': dict(body=rgb('#5a6a4a'), skin=ROT, head='none', weapon_kind='none', scale=0.95, belly=1.1, eye=rgb('#d8ff7a'), legs=rgb('#4a5a3a')),
    'bone_archer': dict(body=BONE, skin=BONE, head='skull', weapon_kind='bow', scale=0.85, legs=BONE),
    'necromancer': dict(body=rgb('#2a2a3a'), skin=BONE, head='hood', hood=rgb('#1a3a2a'), weapon_kind='orbstaff', el=rgb('#7aff9a'), robe=True, scale=0.95, eye=rgb('#7aff9a')),
    'ghoul': dict(body=rgb('#6a6a7a'), skin=rgb('#a8b0a0'), head='none', ears=True, weapon_kind='twin', weapon_col=BONE, scale=0.85, eye=rgb('#ff5a5a')),
    'banshee': dict(body=rgb('#8aa8b8'), skin=rgb('#d8e8f0'), head='hair', hood=rgb('#e8f0f8'), weapon_kind='none', robe=True, wings=rgb('#a8c0d0'), scale=0.85, eye=rgb('#9fe7ff')),
    'crypt_knight': dict(body=rgb('#3a3a4a'), skin=BONE, head='helm', helm_col=rgb('#4a4a5a'), plume=rgb('#5a8a5a'), weapon_kind='greatsword', weapon_col=rgb('#9ab0a0'), shield=rgb('#2a3a2a'),
                         cape=rgb('#1f3a2a'), scale=1.2, belly=1.15, size=104, eye=rgb('#7aff9a')),
    # Siege Constructs (humanoid-shaped machines)
    'siege_golem': dict(body=STONE, skin=lighten(STONE, 0.1), head='none', weapon_kind='club', weapon_col=darken(STONE, 0.2), belly=1.7, scale=1.3, size=112, eye=rgb('#ffb43a'), legs=darken(STONE, 0.2)),
    'rune_sentinel': dict(body=rgb('#6a7a9a'), skin=rgb('#8a9ab8'), head='helm', helm_col=rgb('#5a6a8a'), weapon_kind='spear', shield=rgb('#9fd0f0'), belly=1.3, scale=1.05, eye=rgb('#9fe7ff')),
    # Ashborn
    'ember_runner': dict(body=ASH, skin=rgb('#e05a2a'), head='none', weapon_kind='none', el=MAGMA, scale=0.75, eye=rgb('#ffe14a')),
    'magma_brute': dict(body=rgb('#3a2a2a'), skin=rgb('#c84a2a'), head='none', horns=True, tusks=True, weapon_kind='club', weapon_col=MAGMA, el=MAGMA, belly=1.5, scale=1.25, size=104, eye=rgb('#ffe14a')),
    'cinder_caster': dict(body=rgb('#5a2a2a'), skin=rgb('#d8a080'), head='hood', hood=rgb('#3a1a1a'), weapon_kind='orbstaff', el=MAGMA, robe=True, scale=0.9, eye=rgb('#ffb43a')),
    'ash_burrower': dict(body=rgb('#6a4a3a'), skin=rgb('#9a6a4a'), head='none', ears=True, weapon_kind='twin', weapon_col=rgb('#d8a080'), belly=1.2, scale=0.95, eye=rgb('#ffb43a')),
    'flame_priest': dict(body=rgb('#c8a050'), skin=rgb('#d8a080'), head='hat', hood=rgb('#a83a2a'), weapon_kind='orbstaff', el=MAGMA, robe=True, cape=rgb('#ff7a2e'), scale=0.95),
    # Frostbound
    'rime_caster': dict(body=rgb('#5f8fb8'), skin=rgb('#d8e8f0'), head='hood', hood=rgb('#3f6f98'), weapon_kind='orbstaff', el=EL['ice'], robe=True, scale=0.9, eye=FROST),
    'glacier_golem': dict(body=rgb('#8fc8e8'), skin=FROST, head='none', weapon_kind='club', weapon_col=rgb('#9fd8f0'), el=EL['ice'], belly=1.7, scale=1.35, size=120, eye=rgb('#3f6fff'),
                          legs=rgb('#6fa8c8')),
    'yeti': dict(body=rgb('#e8eef4'), skin=rgb('#a8b8c8'), head='none', horns=True, weapon_kind='none', belly=1.5, scale=1.2, size=104, legs=rgb('#d8e0e8')),
    'frost_wraith': dict(body=rgb('#9fd8f0'), skin=rgb('#e8f6ff'), head='hood', hood=rgb('#6fa8c8'), weapon_kind='none', el=EL['ice'], robe=True, wings=rgb('#bfe8ff'), scale=0.9, eye=rgb('#3f6fff')),
    # Shadow Court
    'mana_leech': dict(body=rgb('#3a2a5a'), skin=rgb('#8a6ab8'), head='none', ears=True, weapon_kind='dagger', weapon_col=rgb('#6fd0ff'), el=rgb('#6fd0ff'), scale=0.85, eye=rgb('#6fd0ff')),
    'void_shifter': dict(body=rgb('#2a2236'), skin=rgb('#4a3a6a'), head='hood', hood=rgb('#1a1428'), weapon_kind='orbstaff', el=rgb('#e8e0ff'), robe=True, cape=rgb('#8a5ad0'), scale=0.95, eye=rgb('#ffffff')),
    # Wild Tempest
    'storm_harpy': dict(body=rgb('#4a6a8a'), skin=rgb('#a8c8d8'), head='hair', hood=rgb('#2a4a6a'), weapon_kind='dagger', wings=TEMPEST, el=EL['lightning'], scale=0.85, eye=rgb('#ffe14a')),
    'gale_raider': dict(body=rgb('#5a8a7a'), skin=rgb('#d8b890'), head='bandana', hood=rgb('#9fe7c8'), weapon_kind='twin', weapon_col=rgb('#d8f0e8'), el=EL['wind'], scale=0.9),
    'cyclone_shaman': dict(body=rgb('#4a7a6a'), skin=rgb('#c8a880'), head='hat', hood=rgb('#2a5a4a'), weapon_kind='orbstaff', el=EL['wind'], robe=True, cape=rgb('#9fe7c8'), scale=0.95),
    'sky_lancer': dict(body=rgb('#3a5a8a'), skin=rgb('#d8b890'), head='helm', helm_col=rgb('#9fb8d8'), plume=rgb('#ffe14a'), weapon_kind='lance', el=EL['lightning'], mount='griffin', scale=1.0, size=112),
}

BOSS_HUMANOIDS = {
    'boss_siege_titan': dict(body=STONE, skin=lighten(STONE, 0.15), head='helm', helm_col=darken(STONE, 0.15), weapon_kind='hammer', weapon_col=rgb('#5a5a6a'), belly=1.8,
                             scale=2.0, size=176, eye=rgb('#ffb43a'), legs=darken(STONE, 0.25)),
    'boss_void_commander': dict(body=rgb('#241c34'), skin=rgb('#6a5a8a'), head='helm', helm_col=rgb('#2a2236'), horns=True, weapon_kind='scythe', weapon_col=rgb('#c9a8ff'), el=EL['shadow'],
                                cape=rgb('#4a2a7a'), scale=1.9, belly=1.2, size=168, eye=rgb('#c27aff')),
    'boss_frost_queen': dict(body=rgb('#9fd0f0'), skin=rgb('#e8f6ff'), head='hair', hood=rgb('#f4fbff'), crown=True, weapon_kind='orbstaff', el=EL['ice'], robe=True, cape=rgb('#5fa8d8'),
                             scale=2.0, size=176, eye=rgb('#3f6fff')),
    'boss_corrupted_king': dict(body=rgb('#5a3a5a'), skin=rgb('#a8a0b8'), head='helm', helm_col=rgb('#3a2a3a'), crown=True, weapon_kind='greatsword', weapon_col=rgb('#b79cff'), el=EL['shadow'],
                                cape=rgb('#7a1f3a'), shield=rgb('#3a2a3a'), scale=2.1, belly=1.3, size=184, eye=rgb('#ff4a6a')),
    'boss_bone_colossus': dict(body=BONE, skin=BONE, head='skull', horns=True, weapon_kind='club', weapon_col=darken(BONE, 0.2), belly=1.6, scale=2.2, size=184, legs=BONE),
    'boss_hollow_emperor': dict(body=rgb('#1a1424'), skin=rgb('#3a2f4a'), head='helm', helm_col=rgb('#2a2236'), crown=True, halo=True, weapon_kind='greatsword', weapon_col=rgb('#ffd04a'),
                                el=EL['shadow'], cape=rgb('#e8c25a'), scale=2.3, belly=1.35, size=192, eye=rgb('#ffd04a')),
}


def quadruped(key, col, *, scale=1.0, size=96, mane=None, eye=rgb('#ffcf4a'), el=None, horn=False):
    for frame in (0, 1):
        atk = frame == 1
        cv = Canvas(size, size)
        s = scale
        g = size - 8
        bx = size / 2 - 4 * s
        cv.shadow(bx + 2 * s, g - 2, 26 * s, 6 * s, 80)
        for i, lx in enumerate((-16, -8, 8, 16)):
            lift = 4 * s if atk and i % 2 == 0 else 0
            cv.rect(bx + lx * s - 3 * s, g - 16 * s - lift, 6 * s, 16 * s, darken(col, 0.18), r=2, ink=2.2)
        cv.poly([(bx - 20 * s, g - 22 * s), (bx - 32 * s, g - (30 if atk else 26) * s), (bx - 22 * s, g - 18 * s)], darken(col, 0.1), ink=2.2)  # tail
        cv.ellipse(bx, g - 22 * s, 23 * s, 10 * s, col, ink=3)
        hx = bx + (26 if atk else 22) * s
        cv.poly([(bx + 14 * s, g - 28 * s), (hx + 12 * s, g - 30 * s), (hx + 14 * s, g - 22 * s), (bx + 18 * s, g - 16 * s)], col, ink=2.5)
        cv.poly([(hx - 2 * s, g - 30 * s), (hx, g - 40 * s), (hx + 5 * s, g - 30 * s)], col, ink=2)
        if mane:
            cv.smooth([(bx + 8 * s, g - 32 * s), (bx + 20 * s, g - 34 * s), (bx + 18 * s, g - 22 * s), (bx + 6 * s, g - 24 * s)], mane, ink=2)
        if horn:
            cv.poly([(hx + 4 * s, g - 32 * s), (hx + 10 * s, g - 44 * s), (hx + 8 * s, g - 30 * s)], rgb('#f0e8d8'), ink=2)
        cv.circle(hx + 8 * s, g - 27 * s, 1.8 * s, eye)
        if atk:
            cv.poly([(hx + 12 * s, g - 24 * s), (hx + 18 * s, g - 22 * s), (hx + 12 * s, g - 20 * s)], (250, 245, 235), ink=1.5)
        if el:
            cv.glow(bx, g - 22 * s, 22 * s, el, 70 if not atk else 120)
        cv.save(f"enemy/{key}_{frame}", pivot=(bx, g - 4))


def bird(key, col, *, scale=1.0, size=96, el=None, belly=None, crest=None):
    for frame in (0, 1):
        atk = frame == 1
        cv = Canvas(size, size)
        s = scale
        cx, cy = size / 2, size / 2 + 4 * s
        cv.shadow(cx, size - 6, 16 * s, 4 * s, 60)
        up = -1 if atk else 1
        for side in (-1, 1):
            cv.poly([(cx - 4 * s, cy - 4 * s), (cx - 34 * s, cy - 22 * s * up - 4 * s), (cx - 28 * s, cy + 2 * s), (cx - 16 * s, cy + 6 * s)], darken(col, 0.1 if side < 0 else 0), ink=2.5)
        cv.ellipse(cx, cy, 16 * s, 10 * s, col, ink=3)
        if belly:
            cv.ellipse(cx + 2 * s, cy + 4 * s, 10 * s, 5 * s, belly, ink=0)
        cv.poly([(cx - 14 * s, cy), (cx - 26 * s, cy + 8 * s), (cx - 14 * s, cy + 6 * s)], darken(col, 0.2), ink=2)
        cv.ellipse(cx + 16 * s, cy - 8 * s, 8 * s, 7 * s, col, ink=2.5)
        cv.poly([(cx + 22 * s, cy - 9 * s), (cx + 32 * s, cy - 6 * s), (cx + 22 * s, cy - 4 * s)], GOLD, ink=2)
        if crest:
            cv.poly([(cx + 12 * s, cy - 14 * s), (cx + 8 * s, cy - 26 * s), (cx + 18 * s, cy - 14 * s)], crest, ink=2)
        cv.circle(cx + 18 * s, cy - 9 * s, 1.8 * s, INK)
        if el:
            cv.glow(cx, cy, 24 * s, el, 90 if not atk else 150)
        cv.save(f"enemy/{key}_{frame}", pivot=(cx, size - 8))


def catapult():
    for frame in (0, 1):
        cv = Canvas(112, 96)
        g = 88
        cv.shadow(56, g - 2, 42, 8, 90)
        cv.rect(16, g - 30, 80, 14, WOOD, r=3, ink=3)
        for x in (26, 86):
            cv.ellipse(x, g - 12, 10, 10, darken(WOOD, 0.25), ink=3)
        cv.poly([(50, g - 30), (58, g - 58), (66, g - 30)], darken(WOOD, 0.1), ink=3)
        a = -0.4 if frame == 0 else 1.0
        ex, ey = 58 + math.cos(math.pi + a) * 40, g - 54 + math.sin(math.pi + a) * 40
        cv.inked_line(58, g - 54, ex, ey, WOOD, 6)
        cv.ellipse(ex, ey, 7, 7, STONE, ink=2.5)
        cv.ellipse(84, g - 40, 7, 7, rgb('#6f9a4a'), ink=2.5)
        cv.save(f"enemy/catapult_{frame}", pivot=(56, g - 4))


def boulder():
    for frame in (0, 1):
        cv = Canvas(80, 80)
        g = 72
        cv.shadow(40, g - 2, 20, 5, 90)
        cv.ellipse(40, g - 22, 20, 20, STONE, ink=3)
        rot = 0.6 * frame
        for i in range(3):
            a = rot + i * 2.1
            cv.line(40 + math.cos(a) * 6, g - 22 + math.sin(a) * 6, 40 + math.cos(a) * 16, g - 22 + math.sin(a) * 16, darken(STONE, 0.35), 2.5)
        cv.glow(40, g - 22, 10, rgb('#ffb43a'), 90 if frame else 50)
        cv.save(f"enemy/boulder_roller_{frame}", pivot=(40, g - 4))


def drone():
    # scale 1.6: at 1.0 the drone read as 0.34 world units — smaller than a soldier's head (validator: scale)
    k = 1.6
    for frame in (0, 1):
        cv = Canvas(80, 80)
        cx, cy = 40, 34
        cv.shadow(cx, 74, 10 * k, 3 * k, 60)
        cv.ellipse(cx, cy, 12 * k, 9 * k, rgb('#b8954a'), ink=2.5)
        for side in (-1, 1):
            cv.ellipse(cx + side * 14 * k, cy - (8 + (4 if frame else 0)) * k, 9 * k, 3 * k, rgb('#d8e8f0'), ink=1.5)
        cv.circle(cx + 6 * k, cy - k, 3 * k, rgb('#ff5a3a'))
        cv.glow(cx + 6 * k, cy - k, 6 * k, rgb('#ff5a3a'), 120)
        cv.save(f"enemy/clockwork_drone_{frame}", pivot=(40, 72))


def serpent():
    for frame in (0, 1):
        size = 176
        cv = Canvas(size, size)
        cv.shadow(88, size - 8, 40, 8, 70)
        col = rgb('#3f7aa8')
        pts = []
        for i in range(9):
            t = i / 8
            pts.append((30 + t * 110, 110 + math.sin(t * 6 + frame) * 18 - t * 30))
        for i, (x, y) in enumerate(pts):
            r = 16 - i * 0.8
            cv.ellipse(x, y, r, r, col if i % 2 else lighten(col, 0.1), ink=2.5)
        hx, hy = pts[-1]
        cv.ellipse(hx + 8, hy - 6, 18, 13, col, ink=3)
        cv.poly([(hx + 18, hy - 2), (hx + 34, hy + (8 if frame else 2)), (hx + 18, hy + 4)], rgb('#e8f6ff'), ink=2)
        cv.poly([(hx, hy - 16), (hx - 6, hy - 34), (hx + 8, hy - 18)], GOLD, ink=2)
        cv.circle(hx + 12, hy - 8, 2.6, rgb('#ffe14a'))
        cv.glow(hx + 10, hy - 6, 30, EL['lightning'], 110 if frame else 70)
        cv.save(f"enemy/boss_storm_serpent_{frame}", pivot=(88, size - 12))


def run():
    for k, r in HUMANOIDS.items():
        r = dict(r)
        size = r.pop('size', 96 if r.get('mount') else 80)
        humanoid(f"enemy/{k}", size=size, **r)
    quadruped('frost_wolf', rgb('#c8dcec'), mane=rgb('#f4fbff'), eye=rgb('#3f6fff'), el=EL['ice'])
    bird('thunder_bird', rgb('#3f5a9a'), scale=1.1, el=EL['lightning'], belly=rgb('#ffe14a'), crest=rgb('#ffe14a'))
    catapult()
    boulder()
    drone()
    for k, r in BOSS_HUMANOIDS.items():
        r = dict(r)
        humanoid(f"enemy/{k}", **r)
    serpent()
    bird('boss_tempest_roc', rgb('#8a7a6a'), scale=2.4, size=176, el=EL['wind'], belly=rgb('#e8e0d0'), crest=rgb('#9fe7c8'))
