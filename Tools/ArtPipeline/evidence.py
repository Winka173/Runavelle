"""Representative scene evidence for art changes: the SAME simulation snapshots rendered with the current sprites.

    python3 Tools/ArtPipeline/evidence.py --tag before|after [--refresh]
    python3 Tools/ArtPipeline/evidence.py --compare before after

Snapshots come from real seeded runs (Tools/Sim `snapshot`) and are cached in Build/evidence/*.json so a before/after
pair uses identical world state, camera, zoom and lighting. Images are drawn by Tools/Art/render_snapshot.py with the
shipped sprites + manifest pivots: a faithful layout preview, NOT a Unity capture (no Unity Editor in this environment).
"""
import argparse
import json
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
CACHE = os.path.join(ROOT, "Build", "evidence")
OUT = os.path.join(ROOT, "reports", "art_evidence")

# name: (map, general, night, seconds, strategy, crop width, crop height)  — seconds 0 = daytime layout before that night
SCENES = {
    "day_kingdom": ("moonstone_valley", "roland", 6, 0, "Balanced", 20, 13),
    "late_kingdom_variant": ("moonstone_valley", "lyra", 12, 0, "Towers", 22, 14),
    "night_siege_breach": ("shattered_crown", "roland", 6, -1, "Balanced", 24, 15),  # -1: 2 s after a breach with >= 6 enemies alive
    "night_crowd": ("ember_highlands", "roland", 9, 40, "Army", 24, 15),
}


def snap(name, refresh):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, name + ".json")
    if refresh or not os.path.exists(path):
        m, g, n, sec, strat, _, _ = SCENES[name]
        r = subprocess.run(["dotnet", "run", "--project", os.path.join(ROOT, "Tools", "Sim"), "-c", "Release", "--",
                            "snapshot", m, g, str(n), str(sec), path, strat], capture_output=True, text=True)
        if r.returncode != 0 or not os.path.exists(path):
            raise RuntimeError(f"snapshot {name} failed: {r.stdout[-400:]} {r.stderr[-400:]}")
    return path


def render(tag, refresh=False):
    os.makedirs(OUT, exist_ok=True)
    made = []
    for name, (_, _, _, _, _, cw, ch) in SCENES.items():
        js = snap(name, refresh)
        d = json.load(open(js))
        castle = next(b for b in d["buildings"] if b["key"].startswith("building/castle"))
        ruin = next((b for b in d["buildings"] if b["key"].startswith("building/ruin_")), None)
        cx, cy = (castle["x"], castle["y"]) if ruin is None else ((castle["x"] + ruin["x"]) / 2, (castle["y"] + ruin["y"]) / 2)
        units = [u for u in d["units"]]
        out = os.path.join(OUT, f"{tag}_{name}.png")
        r = subprocess.run([sys.executable, os.path.join(ROOT, "Tools", "Art", "render_snapshot.py"), js, out, "48",
                            str(cx), str(cy), str(cw), str(ch)], capture_output=True, text=True)
        if r.returncode != 0:
            raise RuntimeError(r.stderr[-600:])
        made.append((out, len(units), sum(1 for u in units if u["team"] == 1), d.get("isNight")))
        print(f"{os.path.relpath(out, ROOT)}  units={len(units)} night={d.get('isNight')}")
    return made


def compare(a, b):
    from PIL import Image, ImageDraw
    rows = []
    for name in SCENES:
        pa, pb = os.path.join(OUT, f"{a}_{name}.png"), os.path.join(OUT, f"{b}_{name}.png")
        if os.path.exists(pa) and os.path.exists(pb):
            rows.append((name, Image.open(pa).convert("RGB"), Image.open(pb).convert("RGB")))
    if not rows:
        print("compare: no pairs")
        return 1
    w = max(r[1].width for r in rows)
    h = sum(max(r[1].height, r[2].height) + 34 for r in rows)
    sheet = Image.new("RGB", (w * 2 + 12, h), (24, 22, 30))
    dr = ImageDraw.Draw(sheet)
    y = 0
    for name, ia, ib in rows:
        dr.text((6, y + 8), f"{name}: {a} (left) / {b} (right) — same snapshot, camera, zoom", fill=(230, 225, 240))
        sheet.paste(ia, (0, y + 30))
        sheet.paste(ib, (w + 12, y + 30))
        y += max(ia.height, ib.height) + 34
    out = os.path.join(OUT, f"compare_{a}_vs_{b}.png")
    sheet.save(out)
    print("compare ->", os.path.relpath(out, ROOT))
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag")
    ap.add_argument("--refresh", action="store_true")
    ap.add_argument("--compare", nargs=2)
    a = ap.parse_args(argv)
    if a.compare:
        return compare(*a.compare)
    render(a.tag or "current", a.refresh)
    return 0


if __name__ == "__main__":
    sys.exit(main())
