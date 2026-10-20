"""Sprite-atlas packing report: shelf-packs sprites per atlas group (2 px padding, 2048² pages) to estimate pages,
fill rate and texture memory (RGBA32 vs ASTC 6x6 / DXT5). The actual SpriteAtlas assets are created in the Editor
by the menu `Runavelle/Art/Create Sprite Atlases` (Editor/RunavelleEditor.cs) using the same groups."""
import json
import os

PAD = 2
PAGE = 2048
GROUPS = {"Characters": ("unit/", "enemy/", "general/"), "Buildings": ("building/",), "Terrain": ("terrain/", "decor/"),
          "UI": ("ui/", "icon/", "portrait/"), "FX": ("fx/",)}


def shelf_pack(sizes, PAGE=PAGE):
    pages, x, y, shelf_h, used = 1, 0, 0, 0, 0
    for w, h in sorted(sizes, key=lambda s: -s[1]):
        w, h = w + PAD * 2, h + PAD * 2
        if w > PAGE or h > PAGE:
            pages += 1
            continue
        if x + w > PAGE:
            x, y, shelf_h = 0, y + shelf_h, 0
        if y + h > PAGE:
            pages, x, y, shelf_h = pages + 1, 0, 0, 0
        x += w
        shelf_h = max(shelf_h, h)
        used += w * h
    return pages, used


def layout_pack(items, PAGE=PAGE):
    """Real shelf layout: items [(key, w, h)] → [(key, x, y, w, h, page)] with PAD on every side (validated by validator.atlas_layout_check)."""
    out, page, x, y, shelf_h = [], 0, PAD, PAD, 0
    for key, w, h in sorted(items, key=lambda s: (-s[2], s[0])):
        if w + 2 * PAD > PAGE or h + 2 * PAD > PAGE:
            out.append((key, PAD, PAD, w, h, f"solo:{key}"))
            continue
        if x + w + PAD > PAGE:
            x, y, shelf_h = PAD, y + shelf_h, 0
        if y + h + PAD > PAGE:
            page, x, y, shelf_h = page + 1, PAD, PAD, 0
        out.append((key, x, y, w, h, page))
        x += w + 2 * PAD
        shelf_h = max(shelf_h, h + 2 * PAD)
    return out


def layouts(art, manifest):
    groups = {}
    for k, e in manifest.items():
        g = next((name for name, pre in GROUPS.items() if k.startswith(pre)), "Other")
        groups.setdefault(g, []).append((k, e["w"], e["h"]))
    return {g: layout_pack(items) for g, items in groups.items()}


def report(art, out_md):
    with open(os.path.join(art, "manifest.json")) as f:
        man = json.load(f)
    lines = ["# Sprite atlas packing report", "", f"Page {PAGE}², padding {PAD}px (bleed), shelf packing (conservative).", "",
             "| Group | Sprites | Pages | Fill | RGBA32 MB | ASTC 6x6 MB | DXT5/BC3 MB |", "|---|---|---|---|---|---|---|"]
    tot = [0, 0.0, 0.0, 0.0]
    for g, prefixes in GROUPS.items():
        sizes = [(v["w"], v["h"]) for k, v in man.items() if k.startswith(prefixes) and not k.startswith("ui/title_bg")]
        page = PAGE
        for cand in (256, 512, 1024, 2048):  # Unity shrinks single pages to the smallest power of two that fits
            p1, _ = shelf_pack(sizes, cand)
            if p1 == 1:
                page = cand
                break
        pages, used = shelf_pack(sizes, page)
        px = pages * page * page
        rgba = px * 4 / 1048576
        astc = px * (128 / 36) / 8 / 1048576
        dxt = px / 1048576
        fill = used / px
        lines.append(f"| {g} | {len(sizes)} | {pages}×{page}² | {fill:.0%} | {rgba:.1f} | {astc:.1f} | {dxt:.1f} |")
        tot[0] += pages; tot[1] += rgba; tot[2] += astc; tot[3] += dxt
    lines += [f"| **Total** | {len(man)} | {tot[0]} | | {tot[1]:.1f} | {tot[2]:.1f} | {tot[3]:.1f} |", "",
              "Mobile recommendation: ASTC 6x6 (Android/iOS), BC7/DXT5 on Windows, mipmaps off for 2D, bilinear filtering, "
              "tight packing off for sprites with outline AA (avoids bleeding). Measured on device: NOT RUN."]
    with open(out_md, "w") as f:
        f.write("\n".join(lines) + "\n")
    return "\n".join(lines)
