"""Previews at actual on-screen size: sprites placed on their pivots over day ground, night ground (the game's night
overlay) and a deuteranopia simulation, at 1080p (camera size 9 → 60 px per world unit) and 720p (40 px/unit)."""
import json
import os

import numpy as np
from PIL import Image, ImageDraw

DEFAULT_KEYS = ["building/castle_1", "building/castle_2", "building/castle_3a", "building/castle_3b",
                "building/wall_stone_10", "building/gate_stone_h", "building/archer_1", "building/magic_2_fire",
                "unit/swordsman_0", "unit/mage_1", "general/roland_0", "general/lyra_1", "enemy/orc_brute_0",
                "enemy/ram_crew_0", "enemy/boss_gatebreaker_0"]
DAY = (96, 140, 70)
NIGHT_OVERLAY = (0.08, 0.07, 0.24, 0.42)  # WorldView night tint (colour, alpha)
DEUT = np.array([[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.011820, 0.042940, 0.968881]])


def _load(art):
    with open(os.path.join(art, "manifest.json")) as f:
        return json.load(f)


def strip(art, keys, px_per_unit, bg):
    man = _load(art)
    items = []
    for k in keys:
        p = os.path.join(art, k + ".png")
        if k not in man or not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        s = px_per_unit / man[k]["ppu"]
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
        items.append((k, im, man[k]["px"], man[k]["py"]))
    if not items:
        return Image.new("RGB", (64, 64), bg)
    H = max(i.height for _, i, _, _ in items) + 30
    W = sum(i.width + 16 for _, i, _, _ in items) + 16
    canvas = Image.new("RGBA", (W, H), bg + (255,))
    base = H - 18
    x = 16
    d = ImageDraw.Draw(canvas)
    for k, im, px, py in items:
        ox = x
        oy = base - im.height + round(py * im.height)  # pivot sits on the baseline
        canvas.alpha_composite(im, (ox, max(0, oy)))
        d.line([(ox, base), (ox + im.width, base)], fill=(0, 0, 0, 60))
        x += im.width + 16
    return canvas


def night(img):
    a = np.asarray(img.convert("RGB"), np.float32)
    c = np.array(NIGHT_OVERLAY[:3]) * 255
    out = a * (1 - NIGHT_OVERLAY[3]) + c * NIGHT_OVERLAY[3]
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))


def deuteranopia(img):
    a = np.asarray(img.convert("RGB"), np.float32) / 255
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    sim = np.clip(lin @ DEUT.T, 0, 1)
    srgb = np.where(sim <= 0.0031308, sim * 12.92, 1.055 * sim ** (1 / 2.4) - 0.055)
    return Image.fromarray((srgb * 255).clip(0, 255).astype(np.uint8))


def actual_zoom_sheet(art, keys, out_dir, tag="game"):
    os.makedirs(out_dir, exist_ok=True)
    rows = []
    for ppu_screen, label in ((60, "1080p (camera 9)"), (40, "720p")):
        day = strip(art, keys, ppu_screen, DAY).convert("RGB")
        rows += [(f"{label} · day", day), (f"{label} · night", night(day)), (f"{label} · deuteranopia", deuteranopia(day))]
    W = max(r.width for _, r in rows)
    H = sum(r.height + 22 for _, r in rows)
    sheet = Image.new("RGB", (W, H), (30, 28, 40))
    d = ImageDraw.Draw(sheet)
    y = 0
    for label, r in rows:
        d.text((6, y + 4), label, fill=(240, 240, 240))
        sheet.paste(r, (0, y + 20))
        y += r.height + 22
    out = os.path.join(out_dir, f"actual_zoom_{tag}.png")
    sheet.save(out)
    return out


def before_after(game_art, staging_art, keys, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    a = strip(game_art, keys, 60, DAY).convert("RGB")
    b = strip(staging_art, keys, 60, DAY).convert("RGB")
    W = max(a.width, b.width)
    sheet = Image.new("RGB", (W, a.height + b.height + 44), (30, 28, 40))
    d = ImageDraw.Draw(sheet)
    d.text((6, 4), "BEFORE (game)", fill=(240, 240, 240))
    sheet.paste(a, (0, 20))
    d.text((6, a.height + 24), "AFTER (staging)", fill=(240, 240, 240))
    sheet.paste(b, (0, a.height + 42))
    out = os.path.join(out_dir, "before_after.png")
    sheet.save(out)
    return out
