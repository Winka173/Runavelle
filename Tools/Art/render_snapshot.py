"""Headless scene preview: renders a simulation snapshot (Tools/Sim snapshot JSON) with the generated sprites,
mirroring WorldView's layering (terrain < night tint < lights < y-sorted sprites < projectiles/telegraphs).
This is NOT a Unity capture; it exists so composition/readability can be reviewed without the Editor.
Usage: python3 Tools/Art/render_snapshot.py snap.json out.png [ppu=48] [crop_cx crop_cy crop_w crop_h]"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageEnhance

ART = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Runavelle", "Resources", "Art")
MAN = json.load(open(os.path.join(ART, "manifest.json")))
EL = {0: (230, 225, 215), 1: (255, 122, 46), 2: (63, 155, 255), 3: (255, 225, 74), 4: (159, 240, 255), 5: (143, 240, 176), 6: (201, 150, 90), 7: (255, 241, 168), 8: (163, 92, 240)}
EL_KEY = {1: 'fire', 2: 'water', 3: 'lightning', 4: 'ice', 5: 'wind', 6: 'earth', 7: 'light', 8: 'shadow'}
_cache = {}


def sprite(key, scale):
    k = (key, scale)
    if k not in _cache:
        im = Image.open(os.path.join(ART, key + ".png")).convert("RGBA")
        if scale != 1:
            im = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.LANCZOS)
        _cache[k] = im
    return _cache[k]


def main():
    snap = json.load(open(sys.argv[1]))
    out = sys.argv[2]
    ppu = int(sys.argv[3]) if len(sys.argv) > 3 else 48
    s = ppu / 64.0
    W, H = snap["w"], snap["h"]
    img = Image.new("RGBA", (W * ppu, H * ppu), (40, 60, 40, 255))
    rng = random.Random(hash(snap["map"]) & 0xffff)

    def px(x, y):
        return x * ppu, (H - y) * ppu

    def paste(key, x, y, scale=1.0, flip=False, alpha=1.0):
        if key not in MAN:
            return
        m = MAN[key]
        im = sprite(key, s * scale)
        if flip:
            im = im.transpose(Image.FLIP_LEFT_RIGHT)
        if alpha < 1:
            im = im.copy()
            im.putalpha(im.getchannel("A").point(lambda a: int(a * alpha)))
        cx, cy = px(x, y)
        pxv = m["px"] if not flip else 1 - m["px"]
        img.alpha_composite(im, (int(cx - pxv * im.width), int(cy - (1 - m["py"]) * im.height)))

    tiles = snap["tiles"]
    tkey = {'Grass': 'grass', 'Dirt': 'dirt', 'Sand': 'sand', 'Plateau': 'plateau', 'Ice': 'ice', 'Water': 'water', 'Shallows': 'shallows', 'Chasm': 'chasm',
            'Lava': 'lava', 'Bridge': 'bridge', 'Mountain': 'mountain', 'Forest': 'grass'}
    decor = []
    blobs = []
    BLOB = ['plateau', 'sand', 'dirt', 'ice', 'shallows', 'water', 'lava', 'chasm']
    for y in range(H):
        for x in range(W):
            t = tiles[y * W + x]
            k = tkey[t]
            v = 0 if k == 'bridge' else (3 if k == 'grass' and rng.random() < 0.08 else rng.randrange(3 if k == 'grass' else 2))
            edge = k in BLOB and any(0 <= x + dx < W and 0 <= y + dy < H and tiles[(y + dy) * W + x + dx] != t for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            base = 'grass_0'
            if edge:
                nb = [tiles[(y + dy) * W + x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= x + dx < W and 0 <= y + dy < H and tiles[(y + dy) * W + x + dx] != t]
                nk = tkey[max(set(nb), key=nb.count)] if nb else 'grass'
                if nk not in ('mountain', 'forest', 'grass', 'bridge') and BLOB.index(nk) < BLOB.index(k):
                    base = nk + '_0'
            tile = sprite(f"terrain/{base if edge else k + '_' + str(v)}", s)
            img.alpha_composite(tile, (x * ppu, (H - 1 - y) * ppu))
            if edge:
                blobs.append((BLOB.index(k), f"terrain/{k}_blob_{rng.randrange(3)}", x + 0.5, y + 0.5))
            if t == 'Mountain' and rng.random() < 0.55:
                decor.append((y + 0.3, f"decor/mountain_{rng.randrange(4)}", x + 0.5, y + 0.3, 0.9 + rng.random() * 0.5))
            elif t == 'Forest' and rng.random() < 0.85:
                decor.append((y + 0.25, f"decor/tree_{rng.randrange(4)}", x + 0.5, y + 0.25, 0.9 + rng.random() * 0.3))
    for _, key, bx, by in sorted(blobs):
        paste(key, bx, by)
    night = snap["isNight"]
    if night:
        tint = Image.new("RGBA", img.size, (20, 18, 60, 107))
        img.alpha_composite(tint)
        for b in snap["buildings"]:
            if "magic" in b["key"] or "castle" in b["key"] or "well" in b["key"] or "shrine" in b["key"] or "archer" in b["key"]:
                col = EL.get(b["el"], (255, 210, 130)) if "magic" in b["key"] else (255, 210, 130)
                g = sprite("fx/glow", s * 2.6)
                g = g.copy(); r, gg, bb, a = g.split()
                tinted = Image.merge("RGBA", (r.point(lambda v: int(v * col[0] / 255)), gg.point(lambda v: int(v * col[1] / 255)), bb.point(lambda v: int(v * col[2] / 255)), a.point(lambda v: int(v * 0.55))))
                cx, cy = px(b["x"], b["y"])
                img.alpha_composite(tinted, (int(cx - tinted.width / 2), int(cy - tinted.height / 2)))
    over = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    for z in snap["zones"]:
        cx, cy = px(z["x"], z["y"]); r = z["r"] * ppu
        c = EL.get(z["el"], (255, 140, 60))
        od.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c + (70,), outline=c + (190,), width=3)
    for t in snap["telegraphs"]:
        cx, cy = px(t["x"], t["y"]); r = t["r"] * ppu
        od.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 60, 40, 70), outline=(255, 60, 40, 230), width=5)
    img.alpha_composite(over)
    d = ImageDraw.Draw(img, "RGBA")
    items = list(decor)
    for b in snap["buildings"]:
        items.append((b["oy"] + 0.05, b["key"], b["x"], b["y"], 1.0, b))
    for u in snap["units"]:
        items.append((u["y"] - (100 if u["fly"] else 0), u["key"], u["x"], u["y"], 1.0, u))
    items.sort(key=lambda it: -it[0])
    for it in items:
        key, x, y, sc = it[1], it[2], it[3], it[4]
        meta = it[5] if len(it) > 5 else None
        if meta is not None and "team" in meta:
            cx, cy = px(x, y)
            rr = 0.32 * ppu
            col = (255, 80, 60, 150) if meta["team"] == 1 else ((255, 215, 90, 220) if meta["gen"] else (90, 190, 255, 140))
            d.ellipse([cx - rr * 1.3, cy - rr * 0.55, cx + rr * 1.3, cy + rr * 0.55], fill=(0, 0, 0, 70))
            d.ellipse([cx - rr * 1.3, cy - rr * 0.55, cx + rr * 1.3, cy + rr * 0.55], outline=col, width=2)
            paste(key, x, y + (0.55 if meta["fly"] else 0), sc, meta["flip"])
            if meta["hp"] < 0.999 or meta["gen"]:
                bw = 0.7 * ppu
                hy = cy - 1.25 * ppu - (0.55 * ppu if meta["fly"] else 0)
                d.rectangle([cx - bw / 2 - 2, hy - 4, cx + bw / 2 + 2, hy + 4], fill=(25, 20, 35, 220))
                hc = (240, 80, 60) if meta["team"] == 1 else ((255, 210, 80) if meta["gen"] else (110, 215, 100))
                d.rectangle([cx - bw / 2, hy - 2, cx - bw / 2 + bw * meta["hp"], hy + 2], fill=hc + (255,))
            if meta["aura"] in EL_KEY:
                paste("icon/el_" + EL_KEY[meta["aura"]], x + 0.45, y + 1.35 + (0.55 if meta["fly"] else 0), 0.38)
        else:
            paste(key, x, y, sc)
            if meta is not None and meta.get("hp", 1) < 0.999 and "ruin" not in key:
                cx, cy = px(x, y)
                bw = 1.2 * ppu
                hy = cy - 1.6 * ppu
                d.rectangle([cx - bw / 2 - 2, hy - 4, cx + bw / 2 + 2, hy + 4], fill=(25, 20, 35, 220))
                d.rectangle([cx - bw / 2, hy - 2, cx - bw / 2 + bw * meta["hp"], hy + 2], fill=(245, 215, 100, 255))
    for p in snap["projectiles"]:
        key = {0: "fx/arrow", 1: "fx/orb", 2: "fx/rock", 3: "fx/bolt"}[p["v"]]
        im = sprite(key, s * 0.7)
        ang = math.degrees(math.atan2(p["ty"] - p["y"], p["tx"] - p["x"]))
        im = im.rotate(ang, expand=True)
        if p["el"]:
            c = EL[p["el"]]
            r, g, b, a = im.split()
            im = Image.merge("RGBA", (r.point(lambda v: int(v * c[0] / 255)), g.point(lambda v: int(v * c[1] / 255)), b.point(lambda v: int(v * c[2] / 255)), a))
        cx, cy = px(p["x"], p["y"] + 0.45)
        img.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))
    if len(sys.argv) > 7:
        ccx, ccy, cw, ch = map(float, sys.argv[4:8])
        l, t = px(ccx - cw / 2, ccy + ch / 2)
        img = img.crop((int(l), int(t), int(l + cw * ppu), int(t + ch * ppu)))
    img.convert("RGB").save(out)
    print("rendered", out, img.size)


if __name__ == "__main__":
    main()
