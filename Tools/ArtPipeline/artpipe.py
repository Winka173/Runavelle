"""Runavelle art pipeline CLI (reproducible, transactional).

  python3 Tools/ArtPipeline/artpipe.py generate [--only PREFIX ...]   procedural 2D generators  -> Build/art_staging
  python3 Tools/ArtPipeline/artpipe.py render   [--only NAME ...]     Blender headless 3D->2D    -> Build/art_staging
  python3 Tools/ArtPipeline/artpipe.py validate [--staging]           Art Quality Validator (exit 1 on BLOCK)
  python3 Tools/ArtPipeline/artpipe.py preview  [--staging] [--keys K ...]  actual-zoom day/night/colour-blind sheets
  python3 Tools/ArtPipeline/artpipe.py diff                           staging vs game: changed keys + before/after sheet
  python3 Tools/ArtPipeline/artpipe.py pack                           sprite-atlas packing report (pages, padding, memory)
  python3 Tools/ArtPipeline/artpipe.py import   [--only PREFIX ...] [--force]  validate staging, back up, copy atomically
  python3 Tools/ArtPipeline/artpipe.py rollback [ID]                  restore the backup taken by an import (default: last)
  python3 Tools/ArtPipeline/artpipe.py history                        list imports

Staging (Build/art_staging) and backups (Build/art_backups) are git-ignored; Tools/ArtPipeline/history.json and the
per-asset provenance in manifest.json ("src", "sha256") are committed. Git remains the long-term rollback.
"""
import argparse
import datetime
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
GAME_ART = os.path.join(ROOT, "Assets", "Runavelle", "Resources", "Art")
STAGING = os.path.join(ROOT, "Build", "art_staging")
BACKUPS = os.path.join(ROOT, "Build", "art_backups")
HISTORY = os.path.join(HERE, "history.json")
sys.path.insert(0, HERE)


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()[:16]


def load_manifest(d):
    p = os.path.join(d, "manifest.json")
    if not os.path.exists(p):
        return {}
    with open(p) as f:
        return json.load(f)


def save_manifest(d, m):
    tmp = os.path.join(d, "manifest.json.tmp")
    with open(tmp, "w") as f:
        json.dump(m, f, indent=0, sort_keys=True)
    os.replace(tmp, os.path.join(d, "manifest.json"))


def matches(key, only):
    return not only or any(key.startswith(o) for o in only)


# ------------------------------------------------------------------------------------------------ generate / render
def cmd_generate(a):
    """Runs the procedural generators into staging. Generators are deterministic (fixed seeds)."""
    os.makedirs(STAGING, exist_ok=True)
    env = dict(os.environ, RUNAVELLE_ART_OUT=STAGING)
    r = subprocess.run([sys.executable, os.path.join(ROOT, "Tools", "Art", "build_art.py")], env=env, capture_output=True, text=True)
    log(r)
    if r.returncode != 0:
        print("generate FAILED (see Build/art_pipeline.log)")
        return 1
    m = load_manifest(STAGING)
    for k in m:
        p = os.path.join(STAGING, k + ".png")
        if os.path.exists(p):
            m[k]["sha256"] = sha(p)
    if a.only:  # keep only requested prefixes in staging so an import cannot touch the rest
        for k in list(m):
            if not matches(k, a.only):
                p = os.path.join(STAGING, k + ".png")
                if os.path.exists(p):
                    os.remove(p)
                del m[k]
    save_manifest(STAGING, m)
    print(f"generate: {len(m)} sprites in {os.path.relpath(STAGING, ROOT)}")
    return 0


def cmd_render(a):
    """Blender headless (Cycles CPU) renders of 3D-modelled assets into staging."""
    blender = shutil.which("blender")
    if not blender:
        print("render BLOCKED: blender not installed (apt-get install blender)")
        return 2
    os.makedirs(STAGING, exist_ok=True)
    script = os.path.join(HERE, "blender", "render_assets.py")
    cmd = [blender, "-b", "--factory-startup", "--python", script, "--", "--out", STAGING]
    if a.only:
        cmd += ["--only"] + a.only
    r = subprocess.run(cmd, capture_output=True, text=True)
    log(r)
    ok = r.returncode == 0 and "RENDER_DONE" in r.stdout
    if not ok:
        print("render FAILED (see Build/art_pipeline.log)")
        return 1
    m = load_manifest(STAGING)
    for k in m:
        p = os.path.join(STAGING, k + ".png")
        if os.path.exists(p):
            m[k]["sha256"] = sha(p)
    save_manifest(STAGING, m)
    print([ln for ln in r.stdout.splitlines() if ln.startswith("RENDER_DONE")][-1])
    return 0


def log(r):
    os.makedirs(os.path.join(ROOT, "Build"), exist_ok=True)
    with open(os.path.join(ROOT, "Build", "art_pipeline.log"), "a") as f:
        f.write(f"\n==== {datetime.datetime.now().isoformat()} {' '.join(r.args)}\n{r.stdout}\n{r.stderr}\n")


# ------------------------------------------------------------------------------------------------ validate / preview / diff
def cmd_validate(a):
    import validator
    art = STAGING if a.staging else GAME_ART
    out = os.path.join(ROOT, "Docs", "Enhancement", "art_validation" + ("_staging" if a.staging else ""))
    summary, _, _ = validator.run(art, os.path.join(HERE, "artspec.json"), os.path.join(HERE, "waivers.json"), out,
                                  None if a.staging else os.path.join(ROOT, "Assets", "Runavelle", "Scripts"))
    print(f"validate {os.path.relpath(art, ROOT)}: BLOCK={summary['block']} WARN={summary['warn']} waived={summary['waived']} -> {os.path.relpath(out, ROOT)}")
    return 1 if summary["block"] else 0


def cmd_preview(a):
    import preview
    art = STAGING if a.staging else GAME_ART
    out = preview.actual_zoom_sheet(art, a.keys or preview.DEFAULT_KEYS, os.path.join(ROOT, "Docs", "Enhancement", "previews"), tag="staging" if a.staging else "game")
    print("preview ->", os.path.relpath(out, ROOT))
    return 0


def changed_keys(only=None):
    st, gm = load_manifest(STAGING), load_manifest(GAME_ART)
    out = []
    for k in sorted(st):
        if not matches(k, only):
            continue
        sp = os.path.join(STAGING, k + ".png")
        gp = os.path.join(GAME_ART, k + ".png")
        if not os.path.exists(sp):
            continue
        if not os.path.exists(gp):
            out.append((k, "new"))
        elif sha(sp) != sha(gp) or {kk: v for kk, v in st[k].items() if kk in ("w", "h", "px", "py", "ppu")} != {kk: v for kk, v in gm.get(k, {}).items() if kk in ("w", "h", "px", "py", "ppu")}:
            out.append((k, "changed"))
    return out


def cmd_diff(a):
    import preview
    ch = changed_keys(a.only)
    print(f"diff: {len(ch)} changed/new sprites")
    for k, why in ch[:60]:
        print(f"  {why:8s} {k}")
    if ch:
        out = preview.before_after(GAME_ART, STAGING, [k for k, _ in ch][:40], os.path.join(ROOT, "Docs", "Enhancement", "previews"))
        print("before/after ->", os.path.relpath(out, ROOT))
    return 0


def cmd_pack(a):
    import pack
    rep = pack.report(GAME_ART, os.path.join(ROOT, "Docs", "Enhancement", "atlas_report.md"))
    print(rep)
    return 0


# ------------------------------------------------------------------------------------------------ import / rollback
def read_history():
    if not os.path.exists(HISTORY):
        return []
    with open(HISTORY) as f:
        return json.load(f)


def cmd_import(a):
    import validator
    ch = changed_keys(a.only)
    if not ch:
        print("import: nothing to import")
        return 0
    # 1) staging must pass the validator (blocking findings abort the import)
    summary, findings, _ = validator.run(STAGING, os.path.join(HERE, "artspec.json"), os.path.join(HERE, "waivers.json"),
                                         os.path.join(ROOT, "Docs", "Enhancement", "art_validation_staging"), None)
    keys = {k for k, _ in ch}
    blocking = [f for f in findings if f["severity"] == "BLOCK" and not f["waived"] and (f["asset"] in keys or f["asset"] == "*")]
    if blocking and not a.force:
        print(f"import ABORTED: {len(blocking)} blocking findings in staging (e.g. {blocking[0]['asset']}: {blocking[0]['message']})")
        return 1
    # 2) backup everything we are about to overwrite
    bid = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    bdir = os.path.join(BACKUPS, bid)
    os.makedirs(bdir, exist_ok=True)
    gm = load_manifest(GAME_ART)
    shutil.copy2(os.path.join(GAME_ART, "manifest.json"), os.path.join(bdir, "manifest.json"))
    for k in keys:
        gp = os.path.join(GAME_ART, k + ".png")
        if os.path.exists(gp):
            dst = os.path.join(bdir, k + ".png")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(gp, dst)
    # 3) copy to temp names first, then rename (a failure leaves the game folder untouched)
    st = load_manifest(STAGING)
    staged = []
    try:
        for k in sorted(keys):
            src = os.path.join(STAGING, k + ".png")
            dst = os.path.join(GAME_ART, k + ".png")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            tmp = dst + ".importing"
            shutil.copy2(src, tmp)
            staged.append((tmp, dst))
        for tmp, dst in staged:
            os.replace(tmp, dst)
        for k in keys:
            gm[k] = st[k]
        save_manifest(GAME_ART, gm)
    except Exception as e:  # noqa: BLE001
        for tmp, _ in staged:
            if os.path.exists(tmp):
                os.remove(tmp)
        print("import FAILED, rolling back:", e)
        restore(bid)
        return 1
    hist = read_history()
    hist.append({"id": bid, "date": datetime.datetime.now().isoformat(timespec="seconds"), "keys": sorted(keys), "forced": bool(a.force)})
    with open(HISTORY, "w") as f:
        json.dump(hist, f, indent=1)
    print(f"import {bid}: {len(keys)} sprites (backup {os.path.relpath(bdir, ROOT)})")
    return 0


def restore(bid):
    bdir = os.path.join(BACKUPS, bid)
    if not os.path.isdir(bdir):
        print("rollback: no backup", bid)
        return 1
    hist = [h for h in read_history() if h["id"] == bid]
    keys = hist[0]["keys"] if hist else []
    for k in keys:
        b = os.path.join(bdir, k + ".png")
        g = os.path.join(GAME_ART, k + ".png")
        if os.path.exists(b):
            shutil.copy2(b, g)
        elif os.path.exists(g):
            os.remove(g)  # the import created it
    shutil.copy2(os.path.join(bdir, "manifest.json"), os.path.join(GAME_ART, "manifest.json"))
    print(f"rollback {bid}: restored {len(keys)} sprites + manifest")
    return 0


def cmd_rollback(a):
    hist = read_history()
    bid = a.id or (hist[-1]["id"] if hist else None)
    if not bid:
        print("rollback: no imports recorded")
        return 1
    rc = restore(bid)
    if rc == 0:
        with open(HISTORY, "w") as f:
            json.dump([h for h in hist if h["id"] != bid], f, indent=1)
    return rc


def cmd_history(a):
    for h in read_history():
        print(h["id"], len(h["keys"]), "sprites", "(forced)" if h.get("forced") else "")
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("generate", "render", "validate", "preview", "diff", "pack", "import", "rollback", "history"):
        p = sub.add_parser(name)
        p.add_argument("--only", nargs="*")
        p.add_argument("--staging", action="store_true")
        p.add_argument("--keys", nargs="*")
        p.add_argument("--force", action="store_true")
        p.add_argument("id", nargs="?")
    a = ap.parse_args(argv)
    return {"generate": cmd_generate, "render": cmd_render, "validate": cmd_validate, "preview": cmd_preview, "diff": cmd_diff,
            "pack": cmd_pack, "import": cmd_import, "rollback": cmd_rollback, "history": cmd_history}[a.cmd](a)


if __name__ == "__main__":
    sys.exit(main())
