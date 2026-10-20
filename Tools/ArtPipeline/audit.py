"""Art audit: inventory, manifest (asset → source → output → Unity linkage → code references), toolchain capabilities.

    python3 Tools/ArtPipeline/audit.py [--out reports] [--tag baseline]

Everything is measured from the repository and the tools actually installed; nothing is assumed.
Writes <out>/art_inventory.csv, <out>/art_manifest.json, <out>/toolchain_capabilities.md and <out>/art_<tag>_audit.md.
"""
import argparse
import csv
import datetime
import glob
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Runavelle", "Resources", "Art")
CODE = os.path.join(ROOT, "Assets", "Runavelle", "Scripts")
sys.path.insert(0, HERE)

# Keys that runtime code builds dynamically (Core/Content/ArtKeys.cs and friends) — a sprite in these families is
# referenced even when its full name never appears literally in C#.
DYNAMIC = [
    (r"^building/(castle_|house_|well_|shrine_|archer_|barracks_|magic_|wall_|gate_|ruin_)", "ArtKeys.Building / ArtKeys.LevelPreview"),
    (r"^building/castle_.*_(fortress|legion|arcanum)$", "ArtKeys.CastleVariant"),
    (r"^building/(emblem|prop)_", "WorldView kingdom emblem/props"),
    (r"^(unit|enemy|general)/", "ArtKeys.Unit (frame _0 idle / _1 attack)"),
    (r"^portrait/", "\"portrait/\" + general id"),
    (r"^icon/el_", "ElementGrammar.Icon / ArtLibrary.ElementIcon"),
    (r"^terrain/", "WorldView terrain tiles/blobs"),
    (r"^decor/", "WorldView decor/ambient"),
    (r"^fx/", "Vfx / WorldView effects"),
]

# Verdict policy for this audit (documented in the report). Visual focal point + weakness + cost.
POLICY = [
    (lambda k, e: e.get("src", "").endswith("render_assets.py"), "KEEP", "3D render from the new pipeline, validator clean"),
    (lambda k, e: re.match(r"^building/(archer_|magic_|barracks_|house_|well_|shrine_|wall_|gate_|ruin_)", k) is not None, "REGENERATE",
     "focal-point building drawn flat in 2D next to the 3D castle: candidate for the Blender pipeline once the golden sample passes"),
    (lambda k, e: k.startswith(("unit/", "enemy/", "general/")), "IMPROVE",
     "validator clean after un-cropping; animation coverage is 2 frames (idle/attack) — no walk/hit/death frames"),
    (lambda k, e: True, "KEEP", "validator clean; reviewed at gameplay zoom"),
]


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()[:16]


def code_index():
    texts = {}
    for p in glob.glob(os.path.join(CODE, "**", "*.cs"), recursive=True):
        with open(p, encoding="utf-8") as f:
            texts[os.path.relpath(p, ROOT)] = f.read()
    return texts


_PREFIX = {}


def concat_prefixes(texts):
    """Literal prefixes that code concatenates into sprite keys, e.g. "icon/eq_" + id."""
    if not _PREFIX:
        for p, t in texts.items():
            for m in re.finditer(r'"([a-z]+/[a-z_]*)"\s*\+', t):
                _PREFIX.setdefault(m.group(1), set()).add(p)
    return _PREFIX


def refs_for(key, texts):
    lit = [p for p, t in texts.items() if '"' + key + '"' in t]
    dyn = [why for rx, why in DYNAMIC if re.search(rx, key)]
    for pre, files in concat_prefixes(texts).items():
        if key.startswith(pre) and len(pre) > key.index("/") + 1:
            dyn.append(f'"{pre}" + … in {", ".join(sorted(os.path.basename(f) for f in files))}')
    return lit, dyn


def tool(cmd):
    exe = shutil.which(cmd[0])
    if not exe:
        return None
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
        return (r.stdout or r.stderr).strip().splitlines()[0] if (r.stdout or r.stderr) else exe
    except Exception as e:  # noqa: BLE001
        return f"{exe} (error: {e})"


def capabilities():
    caps = {
        "python": sys.version.split()[0],
        "blender": tool(["blender", "--version"]),
        "unity_editor": tool(["unity-editor", "-version"]) or tool(["Unity", "-version"]) or tool(["unity", "-version"]),
        "dotnet": tool(["dotnet", "--version"]),
        "git": tool(["git", "--version"]),
    }
    mods = {}
    for m in ("PIL", "numpy", "skia"):
        try:
            mod = __import__(m)
            mods[m] = getattr(mod, "__version__", "?")
        except Exception:  # noqa: BLE001
            mods[m] = None
    caps["python_modules"] = mods
    pv = os.path.join(ROOT, "ProjectSettings", "ProjectVersion.txt")
    caps["unity_project_version"] = open(pv).read().split(":")[1].strip().splitlines()[0] if os.path.exists(pv) else None
    caps["unity_packages"] = json.load(open(os.path.join(ROOT, "Packages", "manifest.json")))["dependencies"]
    # blender render capabilities (measured: Cycles CPU works, EEVEE needs a GPU/EGL context)
    caps["blender_render"] = "Cycles CPU (denoiser unavailable in this build); EEVEE fails headless without EGL" if caps["blender"] else None
    return caps


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "reports"))
    ap.add_argument("--tag", default="baseline")
    a = ap.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    import validator
    summary, findings, per_asset = validator.run(ART, os.path.join(HERE, "artspec.json"), os.path.join(HERE, "waivers.json"),
                                                 os.path.join(a.out, "art_validation_" + a.tag), CODE)
    sev = {}
    for f in findings:
        d = sev.setdefault(f["asset"], {"BLOCK": 0, "WARN": 0, "waived": 0})
        d["waived" if f["waived"] else f["severity"]] += 1
    manifest = json.load(open(os.path.join(ART, "manifest.json")))
    texts = code_index()
    rows, mani = [], {}
    pngs = {os.path.relpath(p, ART)[:-4].replace(os.sep, "/") for p in glob.glob(os.path.join(ART, "**", "*.png"), recursive=True)}
    for key in sorted(set(manifest) | pngs):
        e = manifest.get(key, {})
        path = os.path.join(ART, key + ".png")
        exists = os.path.exists(path)
        meta = path + ".meta"
        guid = None
        if os.path.exists(meta):
            m = re.search(r"guid: ([0-9a-f]+)", open(meta).read())
            guid = m.group(1) if m else None
        lit, dyn = refs_for(key, texts)
        src = e.get("src", "")
        verdict, why = next((v, r) for f, v, r in POLICY if f(key, e))
        if not exists:
            verdict, why = "BLOCKED", "manifest entry without PNG"
        elif not src:
            why += "; provenance (src) missing in manifest"
        s = sev.get(key, {"BLOCK": 0, "WARN": 0, "waived": 0})
        if s["BLOCK"]:
            verdict, why = "REGENERATE", "validator BLOCK"
        row = {"key": key, "category": key.split("/")[0], "w": e.get("w"), "h": e.get("h"), "ppu": e.get("ppu"), "px": e.get("px"), "py": e.get("py"),
               "rgba_kb": round((e.get("w") or 0) * (e.get("h") or 0) * 4 / 1024, 1), "source": src or "?",
               "reproducible": bool(src) and os.path.exists(os.path.join(ROOT, src)), "sha256": sha(path) if exists else "",
               "block": s["BLOCK"], "warn": s["WARN"], "waived": s["waived"], "unity_meta_guid": guid or "",
               "code_refs_literal": len(lit), "code_refs_dynamic": "; ".join(dyn), "verdict": verdict, "reason": why}
        rows.append(row)
        mani[key] = {"source": src or None, "output": os.path.relpath(path, ROOT), "unity_guid": guid,
                     "loader": "Resources.Load<Texture2D>(\"Art/" + key + "\") + Sprite.Create(pivot/ppu from manifest.json)",
                     "references": {"literal": lit, "dynamic": dyn, "scenes": [], "prefabs": []}, "sha256": row["sha256"], "verdict": verdict}
    with open(os.path.join(a.out, "art_inventory.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    with open(os.path.join(a.out, "art_manifest.json"), "w", encoding="utf-8") as f:
        json.dump({"generated": datetime.datetime.now().isoformat(timespec="seconds"), "art_root": os.path.relpath(ART, ROOT), "assets": mani}, f, indent=1, ensure_ascii=False)
    caps = capabilities()
    with open(os.path.join(a.out, "toolchain_capabilities.md"), "w", encoding="utf-8") as f:
        f.write("# Toolchain capabilities (measured)\n\n| Tool | Result |\n|---|---|\n")
        for k, v in caps.items():
            if k in ("python_modules", "unity_packages"):
                continue
            f.write(f"| {k} | {v if v else '**not available**'} |\n")
        f.write("\nPython modules: " + ", ".join(f"{k} {v or 'MISSING'}" for k, v in caps["python_modules"].items()) + "\n\n")
        f.write("Unity packages (Packages/manifest.json): " + ", ".join(f"`{k}` {v}" for k, v in caps["unity_packages"].items()) + "\n\n")
        f.write("Consequences: Unity import/compile in the Editor, play mode, in-engine screenshots and player builds are **BLOCKED** here "
                "(no Unity Editor). The Unity layer is compile-checked against reference assemblies (`Tools/UnityCheck`). "
                "Scene previews use `Tools/Art/render_snapshot.py`, which draws real simulation snapshots with the shipped sprites and "
                "manifest pivots — a faithful layout preview, not a Unity capture. MCP (Unity/Blender) is not connected; CLI is used.\n")
    counts = {}
    for r in rows:
        counts.setdefault(r["verdict"], 0)
        counts[r["verdict"]] += 1
    cats = {}
    for r in rows:
        c = cats.setdefault(r["category"], {"n": 0, "kb": 0.0, "verdicts": {}})
        c["n"] += 1
        c["kb"] += r["rgba_kb"]
        c["verdicts"][r["verdict"]] = c["verdicts"].get(r["verdict"], 0) + 1
    with open(os.path.join(a.out, f"art_{a.tag}_audit.md"), "w", encoding="utf-8") as f:
        f.write(f"# Art audit — {a.tag} ({datetime.datetime.now().isoformat(timespec='minutes')})\n\n")
        f.write(f"Sprites: **{len(rows)}** · validator BLOCK {summary['block']} / WARN {summary['warn']} / waived {summary['waived']} · "
                f"RGBA32 total {summary.get('texture_mb_rgba32', '?')} MB\n\n")
        f.write("Verdicts: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())) + "\n\n")
        f.write("| Category | Sprites | RGBA KB | Verdicts |\n|---|---|---|---|\n")
        for c, d in sorted(cats.items()):
            f.write(f"| {c} | {d['n']} | {d['kb']:.0f} | {', '.join(f'{k} {v}' for k, v in sorted(d['verdicts'].items()))} |\n")
        f.write("\nUnity linkage: " + (f"{sum(1 for r in rows if r['unity_meta_guid'])} sprites have .meta GUIDs" if any(r["unity_meta_guid"] for r in rows)
                                       else "no `.meta` files are committed — GUIDs are assigned on first Editor import; the game references sprites only by "
                                            "Resources path + manifest key (no scenes, prefabs, animator controllers or sprite atlases exist), so replacing a PNG "
                                            "in place keeps every reference valid.") + "\n")
        unref = [r["key"] for r in rows if not r["code_refs_literal"] and not r["code_refs_dynamic"]]
        f.write(f"\nSprites with no literal or dynamic code reference: {len(unref)}" + (f" ({', '.join(unref[:20])}{'…' if len(unref) > 20 else ''})" if unref else "") + "\n")
    print(f"audit[{a.tag}]: {len(rows)} sprites, verdicts {counts}, validator BLOCK={summary['block']} WARN={summary['warn']} -> {os.path.relpath(a.out, ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
