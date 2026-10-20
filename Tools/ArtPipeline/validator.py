"""Runavelle Art Quality Validator.

Usage:
  python3 Tools/ArtPipeline/validator.py [--art DIR] [--spec artspec.json] [--waivers waivers.json]
                                         [--out Docs/Enhancement/art_validation] [--code Assets/Runavelle/Scripts]

Reads every sprite listed in <art>/manifest.json, runs measurable checks and writes report.json / report.csv /
summary.md / summary.html. Exit code 1 when any BLOCK finding is not waived (CI gate), 0 otherwise.
Aesthetic judgement is NOT automated: the contact sheets + human checklist (ART_BIBLE.md) decide ACCEPT/REWORK.
"""
import argparse
import csv
import datetime
import fnmatch
import glob
import html
import json
import os
import re
import sys

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLOCK, WARN, INFO = "BLOCK", "WARN", "INFO"


def lum(rgb):
    c = np.asarray(rgb, dtype=np.float32) / 255.0
    c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]


def contrast(l1, l2):
    hi, lo = max(l1, l2), min(l1, l2)
    return (hi + 0.05) / (lo + 0.05)


# Machado et al. 2009 (severity 1.0) colour-vision deficiency matrices, linear RGB
CVD = {
    "deuteranopia": np.array([[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.011820, 0.042940, 0.968881]]),
    "protanopia": np.array([[0.152286, 1.052583, -0.204868], [0.114503, 0.786281, 0.099216], [-0.003882, -0.048116, 1.051998]]),
}


class Spec:
    def __init__(self, path):
        with open(path) as f:
            self.d = json.load(f)

    def category(self, key):
        best, cfg = "", None
        for prefix, c in self.d["categories"].items():
            hit = fnmatch.fnmatch(key, prefix + "*") if c.get("_match") == "glob" else key.startswith(prefix)
            if hit and len(prefix) > len(best):
                best, cfg = prefix, c
        return best, cfg or {}

    @property
    def t(self):
        return self.d["thresholds"]


class Waivers:
    def __init__(self, path):
        self.items = []
        if path and os.path.exists(path):
            with open(path) as f:
                self.items = json.load(f).get("waivers", [])
        self.today = datetime.date.today().isoformat()

    def match(self, asset, check):
        for w in self.items:
            if w.get("expires", "9999") < self.today:
                continue
            if fnmatch.fnmatch(asset, w["asset"]) and (w["check"] == check or w["check"] == "*"):
                return w
        return None


def analyse(img):
    """Per-image measurements shared by several checks."""
    a = np.asarray(img.convert("RGBA"), dtype=np.uint8)
    alpha = a[..., 3].astype(np.float32) / 255.0
    rgb = a[..., :3]
    opaque = alpha > 0.5
    m = {"w": a.shape[1], "h": a.shape[0], "alpha_cov": float(opaque.mean()), "has_alpha": bool((alpha < 0.99).any())}
    if opaque.any():
        ys, xs = np.nonzero(opaque)
        m["bbox"] = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
        m["foot_row"] = int(ys.max())
        m["touches_edge"] = bool(xs.min() == 0 or ys.min() == 0 or xs.max() == a.shape[1] - 1 or ys.max() == a.shape[0] - 1)
        m["mean_lum"] = float(lum(rgb[opaque]).mean())
        m["p90_lum"] = float(np.percentile(lum(rgb[opaque]), 90))
        # silhouette rim: opaque pixels with a transparent 4-neighbour
        pad = np.pad(opaque, 1)
        rim = opaque & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
        m["rim_lum"] = float(lum(rgb[rim]).mean()) if rim.any() else m["mean_lum"]
        # white halo: semi-transparent fringe pixels that are much brighter than the rim they surround
        semi = (alpha > 0.04) & (alpha < 0.5)
        near = np.zeros_like(opaque)
        near[1:, :] |= opaque[:-1, :]; near[:-1, :] |= opaque[1:, :]; near[:, 1:] |= opaque[:, :-1]; near[:, :-1] |= opaque[:, 1:]
        clear = alpha < 0.03
        touch_clear = np.zeros_like(opaque)
        touch_clear[1:, :] |= clear[:-1, :]; touch_clear[:-1, :] |= clear[1:, :]; touch_clear[:, 1:] |= clear[:, :-1]; touch_clear[:, :-1] |= clear[:, 1:]
        fringe = semi & near & touch_clear  # a 1-px band between opaque and empty: where matte halos live
        if fringe.sum() >= 8:
            f = rgb[fringe].astype(np.float32) / 255.0
            fl = lum(rgb[fringe])
            sat = f.max(axis=1) - f.min(axis=1)
            m["halo"] = float(((fl > 0.7) & (sat < 0.15) & (fl > m["rim_lum"] + 0.35)).mean())
        else:
            m["halo"] = 0.0
    m["mask"] = opaque
    return m


def check_asset(key, entry, path, spec, out):
    cat, cfg = spec.category(key)

    def add(sev, check, msg):
        out.append({"asset": key, "category": cat, "severity": sev, "check": check, "message": msg})

    if not os.path.exists(path):
        add(BLOCK, "missing_file", f"manifest lists {key} but {os.path.relpath(path, ROOT)} does not exist")
        return None
    try:
        img = Image.open(path)
        img.load()
    except Exception as e:  # noqa: BLE001
        add(BLOCK, "unreadable", f"cannot decode PNG: {e}")
        return None
    m = analyse(img)
    if (m["w"], m["h"]) != (entry.get("w"), entry.get("h")):
        add(BLOCK, "manifest_size", f"manifest says {entry.get('w')}x{entry.get('h')}, file is {m['w']}x{m['h']}")
    max_dim = cfg.get("max_dim", spec.t["max_dim"])
    if max(m["w"], m["h"]) > max_dim:
        add(BLOCK, "max_dim", f"{m['w']}x{m['h']} exceeds {max_dim} px")
    if entry.get("ppu") != spec.d["ppu"]:
        add(BLOCK, "ppu", f"ppu {entry.get('ppu')} != project ppu {spec.d['ppu']}")
    px, py = entry.get("px", 0.5), entry.get("py", 0.5)
    if not (0 <= px <= 1 and 0 <= py <= 1):
        add(BLOCK, "pivot_bounds", f"pivot ({px}, {py}) outside the sprite")
    if m["alpha_cov"] == 0:
        add(BLOCK, "empty", "sprite is fully transparent")
        return m
    if cfg.get("opaque") and m["has_alpha"]:
        add(WARN, "opaque_expected", "tile should be fully opaque (seams at runtime)")
    if "size" in cfg and [m["w"], m["h"]] != cfg["size"]:
        add(BLOCK, "tile_size", f"expected {cfg['size']}, got {[m['w'], m['h']]}")
    if not cfg.get("opaque") and not m["has_alpha"]:
        add(WARN, "no_alpha", "sprite has no transparency (box silhouette)")
    if not cfg.get("edge_ok") and m.get("touches_edge"):
        add(WARN, "clipped", f"opaque pixels touch the canvas edge (bbox {m['bbox']}) — silhouette may be cropped")
    if m.get("halo", 0) > spec.t["halo_max"]:
        add(WARN, "halo", f"bright fringe on {m['halo']:.0%} of edge pixels (white halo / un-premultiplied edge)")
    if cfg.get("anchor") == "footprint":
        # buildings are positioned at their footprint centre: the pivot must sit above the base, inside the footprint
        above = py * m["h"] - (m["h"] - 1 - m["foot_row"])
        lo, hi = cfg.get("pivot_above_foot_px", [8, 112])
        if not lo <= above <= hi:
            add(WARN, "foot_pivot", f"pivot {above:.0f}px above the lowest opaque pixel, expected {lo}..{hi}px (footprint centre)")
    if cfg.get("anchor") == "foot":
        pivot_from_bottom = py * m["h"]
        foot_from_bottom = m["h"] - 1 - m["foot_row"]
        tol = cfg.get("foot_tol_px", 8)
        if abs(pivot_from_bottom - foot_from_bottom) > tol:
            add(WARN, "foot_pivot", f"pivot {pivot_from_bottom:.0f}px vs lowest opaque pixel {foot_from_bottom}px from bottom (> {tol}px): units will float or sink")
    if "world_h" in cfg and "bbox" in m:
        h_world = (m["bbox"][3] - m["bbox"][1] + 1) / spec.d["ppu"]
        lo, hi = cfg["world_h"]
        if not lo <= h_world <= hi:
            add(WARN, "scale", f"visible height {h_world:.2f} world units outside [{lo}, {hi}]")
    if not cfg.get("tint") and not cfg.get("no_readability") and "mean_lum" in m and not cfg.get("opaque"):
        worst = 99.0
        for name, bg in spec.d["backgrounds"].items():
            bl = float(lum(np.array(bg, dtype=np.uint8)))
            score = max(contrast(m["rim_lum"], bl), contrast(m["p90_lum"], bl))
            worst = min(worst, score)
        m["readability"] = worst
        if worst < spec.t["readability_min"]:
            add(WARN, "readability", f"low contrast vs day/night ground (min ratio {worst:.2f} < {spec.t['readability_min']})")
    return m


def frame_checks(results, manifest, spec, out):
    groups = {}
    for key in manifest:
        mm = re.match(r"^(.*)_(\d+)$", key)
        cat, cfg = spec.category(key)
        if mm and cfg.get("frames"):
            groups.setdefault(mm.group(1), []).append(key)
    for base, keys in sorted(groups.items()):
        keys.sort(key=lambda k: int(k.rsplit("_", 1)[1]))
        idx = [int(k.rsplit("_", 1)[1]) for k in keys]
        if idx != list(range(len(idx))):
            out.append({"asset": base, "category": "frames", "severity": BLOCK, "check": "frame_sequence", "message": f"frames {idx} are not a contiguous 0..n sequence"})
        dims = {(manifest[k]["w"], manifest[k]["h"], manifest[k]["px"], manifest[k]["py"]) for k in keys}
        if len(dims) > 1:
            out.append({"asset": base, "category": "frames", "severity": BLOCK, "check": "frame_consistency", "message": f"frames differ in size/pivot: {sorted(dims)}"})
            continue
        masks = [results[k]["mask"] for k in keys if k in results and results[k] is not None and "mask" in results[k]]
        for a, b, ka, kb in zip(masks, masks[1:], keys, keys[1:]):
            inter = (a & b).sum()
            union = (a | b).sum() or 1
            iou = inter / union
            if iou < spec.t["frame_iou_min"]:
                out.append({"asset": base, "category": "frames", "severity": WARN, "check": "frame_jump", "message": f"{os.path.basename(ka)}→{os.path.basename(kb)} silhouette IoU {iou:.2f} < {spec.t['frame_iou_min']} (identity drift / popping)"})


def manifest_checks(art, manifest, out):
    pngs = {os.path.relpath(p, art)[:-4].replace(os.sep, "/") for p in glob.glob(os.path.join(art, "**", "*.png"), recursive=True)}
    for k in sorted(pngs - set(manifest)):
        out.append({"asset": k, "category": "manifest", "severity": BLOCK, "check": "unlisted_file", "message": "PNG exists but is missing from manifest.json (no pivot/ppu for Unity)"})
    lower = {}
    for k in manifest:
        lower.setdefault(k.lower(), []).append(k)
    for k, v in lower.items():
        if len(v) > 1:
            out.append({"asset": k, "category": "manifest", "severity": BLOCK, "check": "duplicate_name", "message": f"case-insensitive duplicates {v} (collide on Windows/macOS)"})


def element_cvd_check(art, manifest, out, spec_t):
    """Element status icons must stay distinguishable for deuteranopia/protanopia by colour OR shape."""
    keys = [k for k in manifest if k.startswith("icon/el_") and k != "icon/el_none"]
    data = {}
    for k in keys:
        p = os.path.join(art, k + ".png")
        if not os.path.exists(p):
            continue
        a = np.asarray(Image.open(p).convert("RGBA"), dtype=np.float32) / 255.0
        mask = a[..., 3] > 0.5
        lin = np.where(a[..., :3] <= 0.04045, a[..., :3] / 12.92, ((a[..., :3] + 0.055) / 1.055) ** 2.4)
        L = (0.2126 * lin[..., 0] + 0.7152 * lin[..., 1] + 0.0722 * lin[..., 2]) * mask
        data[k] = (mask, lin[mask].mean(axis=0) if mask.any() else np.zeros(3), L)
    for kind, M in CVD.items():
        ks = sorted(data)
        for i in range(len(ks)):
            for j in range(i + 1, len(ks)):
                ma, ca, la = data[ks[i]]
                mb, cb, lb = data[ks[j]]
                cdist = float(np.linalg.norm(M @ ca - M @ cb))
                corr = 1.0
                if la.shape == lb.shape:
                    xa, xb = la - la.mean(), lb - lb.mean()
                    den = float(np.sqrt((xa * xa).sum() * (xb * xb).sum())) or 1.0
                    corr = float((xa * xb).sum() / den)
                if cdist < spec_t["cvd_color_min"] and corr > spec_t["cvd_pattern_max"]:
                    out.append({"asset": f"{ks[i]} vs {ks[j]}", "category": "icon", "severity": BLOCK, "check": "cvd_" + kind,
                                "message": f"indistinguishable under {kind}: colour distance {cdist:.3f} and glyph pattern correlation {corr:.2f}"})


def code_reference_check(code_dir, manifest, out):
    """Literal sprite keys used by C# must exist (dynamic keys are covered by the NUnit ArtKeys test)."""
    if not code_dir or not os.path.isdir(code_dir):
        return
    pat = re.compile(r'"((?:terrain|building|unit|enemy|general|portrait|icon|ui|fx|decor)/[A-Za-z0-9_]+)"')
    for f in glob.glob(os.path.join(code_dir, "**", "*.cs"), recursive=True):
        with open(f, encoding="utf-8") as fh:
            for ln, line in enumerate(fh, 1):
                for key in pat.findall(line):
                    if key not in manifest and not key.endswith("_"):
                        nxt = line[line.find(key) + len(key) + 1:].lstrip()
                        if nxt.startswith("+"):
                            continue  # prefix of a concatenated key
                        out.append({"asset": key, "category": "code", "severity": BLOCK, "check": "missing_reference",
                                    "message": f"referenced in {os.path.relpath(f, ROOT)}:{ln} but not in manifest"})


def provenance_check(art, out):
    fonts = os.path.join(art, "..", "Fonts")
    for ttf in glob.glob(os.path.join(fonts, "*.ttf")):
        fam = os.path.basename(ttf).split("-")[0]
        if not glob.glob(os.path.join(fonts, fam + "*OFL*")) and not glob.glob(os.path.join(fonts, fam + "*LICENSE*")):
            out.append({"asset": os.path.basename(ttf), "category": "license", "severity": BLOCK, "check": "license", "message": "font without licence file"})


def run(art, spec_path, waivers_path, out_dir, code_dir=None, write=True):
    spec = Spec(spec_path)
    waivers = Waivers(waivers_path)
    with open(os.path.join(art, "manifest.json")) as f:
        manifest = json.load(f)
    findings, results = [], {}
    manifest_checks(art, manifest, findings)
    total_bytes = 0
    for key in sorted(manifest):
        m = check_asset(key, manifest[key], os.path.join(art, key + ".png"), spec, findings)
        results[key] = m
        if m:
            total_bytes += m["w"] * m["h"] * 4
    frame_checks(results, manifest, spec, findings)
    element_cvd_check(art, manifest, findings, spec.t)
    code_reference_check(code_dir, manifest, findings)
    provenance_check(art, findings)
    mb = total_bytes / 1048576
    if mb > spec.t["texture_budget_mb"]:
        findings.append({"asset": "*", "category": "budget", "severity": BLOCK, "check": "texture_budget", "message": f"uncompressed RGBA32 {mb:.1f} MB > {spec.t['texture_budget_mb']} MB"})
    for f in findings:
        w = waivers.match(f["asset"], f["check"])
        f["waived"] = bool(w)
        if w:
            f["waiver"] = w["reason"]
    blocking = [f for f in findings if f["severity"] == BLOCK and not f["waived"]]
    summary = {
        "generated": datetime.datetime.now().isoformat(timespec="seconds"),
        "assets": len(manifest), "findings": len(findings),
        "block": len(blocking), "warn": sum(1 for f in findings if f["severity"] == WARN and not f["waived"]),
        "waived": sum(1 for f in findings if f["waived"]),
        "texture_mb_rgba32": round(mb, 2),
        "by_check": {},
    }
    for f in findings:
        if not f["waived"]:
            summary["by_check"][f["check"]] = summary["by_check"].get(f["check"], 0) + 1
    per_asset = {k: {kk: vv for kk, vv in (v or {}).items() if kk not in ("mask",)} for k, v in results.items()}
    if write:
        os.makedirs(out_dir, exist_ok=True)
        with open(os.path.join(out_dir, "report.json"), "w") as f:
            json.dump({"summary": summary, "findings": findings, "assets": per_asset}, f, indent=1, ensure_ascii=False)
        with open(os.path.join(out_dir, "report.csv"), "w", newline="", encoding="utf-8") as f:
            wr = csv.writer(f)
            wr.writerow(["asset", "category", "severity", "check", "waived", "message"])
            for x in findings:
                wr.writerow([x["asset"], x["category"], x["severity"], x["check"], x["waived"], x["message"]])
        write_summary(out_dir, summary, findings)
    return summary, findings, per_asset


def write_summary(out_dir, summary, findings):
    lines = [f"# Art validation — {summary['generated']}", "",
             f"Assets: **{summary['assets']}** · BLOCK: **{summary['block']}** · WARN: **{summary['warn']}** · waived: {summary['waived']} · "
             f"texture memory (RGBA32, uncompressed): {summary['texture_mb_rgba32']} MB", "",
             "| Check | Count |", "|---|---|"]
    for k, v in sorted(summary["by_check"].items(), key=lambda kv: -kv[1]):
        lines.append(f"| {k} | {v} |")
    lines += ["", "## Findings (not waived)", "", "| Severity | Asset | Check | Message |", "|---|---|---|---|"]
    for f in sorted(findings, key=lambda x: (x["severity"] != BLOCK, x["check"], x["asset"])):
        if not f["waived"]:
            lines.append(f"| {f['severity']} | `{f['asset']}` | {f['check']} | {f['message']} |")
    with open(os.path.join(out_dir, "summary.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")
    rows = "".join(f"<tr class='{html.escape(f['severity'])}'><td>{html.escape(f['severity'])}</td><td><code>{html.escape(f['asset'])}</code></td>"
                   f"<td>{html.escape(f['check'])}</td><td>{html.escape(f['message'])}</td></tr>" for f in findings if not f["waived"])
    page = f"""<!doctype html><meta charset="utf-8"><title>Runavelle art validation</title>
<style>body{{font:14px system-ui;margin:24px}}td,th{{border-bottom:1px solid #ddd;padding:4px 8px;text-align:left}}tr.BLOCK td:first-child{{color:#b00;font-weight:700}}tr.WARN td:first-child{{color:#a60}}</style>
<h1>Art validation</h1><p>{summary['assets']} assets · BLOCK {summary['block']} · WARN {summary['warn']} · waived {summary['waived']} · {summary['texture_mb_rgba32']} MB RGBA32</p>
<table><tr><th>Severity</th><th>Asset</th><th>Check</th><th>Message</th></tr>{rows}</table>"""
    with open(os.path.join(out_dir, "summary.html"), "w", encoding="utf-8") as fh:
        fh.write(page)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--art", default=os.path.join(ROOT, "Assets", "Runavelle", "Resources", "Art"))
    ap.add_argument("--spec", default=os.path.join(os.path.dirname(__file__), "artspec.json"))
    ap.add_argument("--waivers", default=os.path.join(os.path.dirname(__file__), "waivers.json"))
    ap.add_argument("--out", default=os.path.join(ROOT, "Docs", "Enhancement", "art_validation"))
    ap.add_argument("--code", default=os.path.join(ROOT, "Assets", "Runavelle", "Scripts"))
    a = ap.parse_args()
    summary, findings, _ = run(a.art, a.spec, a.waivers, a.out, a.code)
    print(f"assets={summary['assets']} BLOCK={summary['block']} WARN={summary['warn']} waived={summary['waived']} "
          f"texMB={summary['texture_mb_rgba32']} -> {os.path.relpath(a.out, ROOT)}")
    for k, v in sorted(summary["by_check"].items(), key=lambda kv: -kv[1]):
        print(f"  {k}: {v}")
    sys.exit(1 if summary["block"] else 0)


if __name__ == "__main__":
    main()
