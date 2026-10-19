"""Contact sheets of generated sprites for visual QA (Docs/Previews/art_*.png)."""
import json
import os

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Runavelle", "Resources", "Art")
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Previews")


def make(prefixes=None):
    os.makedirs(OUT, exist_ok=True)
    man = json.load(open(os.path.join(ROOT, "manifest.json")))
    groups = {}
    for k in sorted(man):
        cat = k.split("/")[0]
        if prefixes and not any(k.startswith(p) for p in prefixes):
            continue
        groups.setdefault(cat, []).append(k)
    for cat, keys in groups.items():
        imgs = [Image.open(os.path.join(ROOT, k + ".png")) for k in keys]
        cell = max(max(i.width for i in imgs), max(i.height for i in imgs)) + 8
        cols = max(1, min(12, 1600 // cell))
        rows = (len(imgs) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * cell, rows * cell), (92, 128, 74, 255))
        for n, im in enumerate(imgs):
            x, y = (n % cols) * cell + (cell - im.width) // 2, (n // cols) * cell + (cell - im.height) // 2
            sheet.alpha_composite(im, (x, y))
        sheet.convert("RGB").save(os.path.join(OUT, f"art_{cat}.png"))
        print("sheet", cat, len(keys))


if __name__ == "__main__":
    make()
