"""Before/after art report from two audits.

    python3 Tools/ArtPipeline/report.py --before reports/art_inventory_baseline.csv [--after reports/art_inventory.csv]

Prints/writes reports/art_report_delta.md: per-category sprite counts, RGBA memory, verdicts, sources and how many
sprites changed (sha) — numbers only; visual quality is judged from the evidence images, not from these metrics.
"""
import argparse
import csv
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))


def load(p):
    with open(p, encoding="utf-8") as f:
        return {r["key"]: r for r in csv.DictReader(f)}


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--before", required=True)
    ap.add_argument("--after", default=os.path.join(ROOT, "reports", "art_inventory.csv"))
    ap.add_argument("--out", default=os.path.join(ROOT, "reports", "art_report_delta.md"))
    a = ap.parse_args(argv)
    if a.before.endswith(".md"):  # accept the baseline markdown path from the documented interface
        a.before = os.path.join(os.path.dirname(a.before), "art_inventory_baseline.csv")
    b, n = load(a.before), load(a.after)
    cats = sorted({r["category"] for r in list(b.values()) + list(n.values())})
    lines = ["# Art before/after (measured)", "", "| Category | sprites before → after | changed (sha) | RGBA KB before → after | Blender-rendered after |", "|---|---|---|---|---|"]
    tot_changed = 0
    for c in cats:
        kb = [k for k, r in b.items() if r["category"] == c]
        kn = [k for k, r in n.items() if r["category"] == c]
        ch = sum(1 for k in kn if k in b and b[k]["sha256"] != n[k]["sha256"]) + sum(1 for k in kn if k not in b)
        tot_changed += ch
        kbb = sum(float(b[k]["rgba_kb"]) for k in kb)
        knb = sum(float(n[k]["rgba_kb"]) for k in kn)
        bl = sum(1 for k in kn if n[k]["source"].endswith("render_assets.py"))
        lines.append(f"| {c} | {len(kb)} → {len(kn)} | {ch} | {kbb:.0f} → {knb:.0f} | {bl} |")
    def verdicts(d):
        out = {}
        for r in d.values():
            out[r["verdict"]] = out.get(r["verdict"], 0) + 1
        return ", ".join(f"{k} {v}" for k, v in sorted(out.items()))
    lines += ["", f"Verdicts before: {verdicts(b)}", f"Verdicts after: {verdicts(n)}", f"Sprites changed: {tot_changed}",
              f"Removed keys: {sorted(set(b) - set(n)) or 'none'}", f"New keys: {sorted(set(n) - set(b)) or 'none'}"]
    md = "\n".join(lines) + "\n"
    with open(a.out, "w", encoding="utf-8") as f:
        f.write(md)
    print(md)
    return 0


if __name__ == "__main__":
    sys.exit(main())
