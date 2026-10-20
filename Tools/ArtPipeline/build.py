"""Runavelle art build — reproducible, incremental, validated, transactional.

    python3 Tools/ArtPipeline/build.py --scope golden --quality release
    python3 Tools/ArtPipeline/build.py --scope all --quality release --incremental
    python3 Tools/ArtPipeline/build.py --scope building/archer_,building/magic_ --dry-run
    python3 Tools/ArtPipeline/build.py --scope all --force          # ignore the cache, re-render everything

Stages: procedural generation (skia, Tools/Art) → Blender headless renders (Tools/ArtPipeline/blender/render_assets.py,
Cycles CPU, fixed camera/light/seed) → premultiplied 2x→1x downsample + pivot-preserving trim → strict validation of the
staged scope (any unwaived BLOCK aborts: nothing is imported) → transactional import with backup (artpipe.py import) →
logs `reports/art_build_<id>.json` and appends `reports/art_replaced_assets.csv`.

Incremental: a Blender key is re-rendered only when hash(render script + Blender version + quality + key) changed or its raw
render is missing (`Build/art_cache.json`). Procedural generation is cheap (~3 s for all) and always re-run; unchanged PNGs
are detected by content hash and not re-imported. Blender renders take precedence over procedural fallbacks.
"""
import argparse
import csv
import datetime
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
import artpipe  # noqa: E402

CONFIG = os.path.join(HERE, "pipeline_config.json")
CACHE = os.path.join(ROOT, "Build", "art_cache.json")
REPORTS = os.path.join(ROOT, "reports")
RENDER = os.path.join(HERE, "blender", "render_assets.py")
TOOL_VERSION = "build.py/1"


def h(*parts):
    m = hashlib.sha256()
    for p in parts:
        m.update(p if isinstance(p, bytes) else str(p).encode())
    return m.hexdigest()[:20]


def file_bytes(path):
    with open(path, "rb") as f:
        return f.read()


def blender_keys(blender):
    r = subprocess.run([blender, "-b", "--factory-startup", "--python", RENDER, "--", "--list"], capture_output=True, text=True, timeout=300)
    line = next((ln for ln in r.stdout.splitlines() if ln.startswith("ASSET_KEYS ")), None)
    if not line:
        raise RuntimeError("could not list Blender assets: " + r.stderr[-400:])
    return json.loads(line.split(" ", 1)[1])


def resolve_scope(scope, cfg, all_keys):
    if scope == "all":
        return sorted(all_keys)
    if scope == "golden":
        return sorted(k for k in all_keys if k in set(cfg["golden"]))
    prefixes = [p.strip() for p in scope.split(",") if p.strip()]
    return sorted(k for k in all_keys if any(k == p or k.startswith(p) for p in prefixes))


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--scope", default="golden", help="golden | all | comma-separated key prefixes")
    ap.add_argument("--quality", default="release", choices=["release", "draft"])
    ap.add_argument("--incremental", action="store_true", help="(default behaviour; kept for the documented interface)")
    ap.add_argument("--force", action="store_true", help="ignore the render cache")
    ap.add_argument("--dry-run", action="store_true", help="print the plan only")
    ap.add_argument("--no-import", action="store_true", help="stage + validate only")
    a = ap.parse_args(argv)
    t0 = time.time()
    cfg = json.load(open(CONFIG))
    bid = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    log = {"id": bid, "scope": a.scope, "quality": a.quality, "force": a.force, "dry_run": a.dry_run, "stages": []}

    blender = shutil.which("blender")
    bkeys = blender_keys(blender) if blender else []
    if not blender:
        log["blender"] = "BLOCKED: blender not installed — Blender-owned keys keep their current art"
    bver = subprocess.run([blender, "--version"], capture_output=True, text=True).stdout.splitlines()[0] if blender else "none"

    # 1) procedural stage (always; deterministic, ~3 s)
    if a.dry_run:
        proc_keys = sorted(artpipe.load_manifest(artpipe.GAME_ART))
    else:
        rc = artpipe.cmd_generate(argparse.Namespace(only=None))
        if rc:
            print("build FAILED in procedural stage")
            return rc
        proc_keys = sorted(artpipe.load_manifest(artpipe.STAGING))
    all_keys = set(proc_keys) | set(bkeys)
    keys = resolve_scope(a.scope, cfg, all_keys)
    bscope = [k for k in keys if k in set(bkeys)]

    # 2) Blender stage (incremental)
    cache = json.load(open(CACHE)) if os.path.exists(CACHE) else {}
    script_hash = h(file_bytes(RENDER))
    need, skipped = [], []
    for k in bscope:
        ih = h(TOOL_VERSION, script_hash, bver, a.quality, k)
        raw = os.path.join(artpipe.STAGING, "_raw", k + ".png")
        if not a.force and cache.get(k, {}).get("input") == ih and os.path.exists(raw):
            skipped.append(k)
        else:
            need.append((k, ih))
    plan = {"scope_keys": len(keys), "blender_keys": len(bscope), "render": len(need), "render_cached": len(skipped),
            "procedural_keys": len([k for k in keys if k not in set(bkeys)])}
    log["plan"] = plan
    print("plan:", json.dumps(plan))
    if a.dry_run:
        for k, _ in need[:40]:
            print("  would render", k)
        return 0
    if need:
        ts = time.time()
        cmd = [blender, "-b", "--factory-startup", "--python", RENDER, "--", "--out", artpipe.STAGING, "--quality", a.quality, "--exact", "--only"] + [k for k, _ in need]
        r = subprocess.run(cmd, capture_output=True, text=True)
        artpipe.log(r)
        rendered = [ln.split(" ", 1)[1] for ln in r.stdout.splitlines() if ln.startswith("RENDERED ")]
        failed = sorted(set(k for k, _ in need) - set(rendered))
        for k, ih in need:
            if k in rendered:
                cache[k] = {"input": ih, "date": bid}
        os.makedirs(os.path.dirname(CACHE), exist_ok=True)
        json.dump(cache, open(CACHE, "w"), indent=1, sort_keys=True)
        log["stages"].append({"stage": "blender", "rendered": len(rendered), "failed": failed, "seconds": round(time.time() - ts, 1)})
        print(f"blender: rendered {len(rendered)} in {time.time() - ts:.0f}s, failed {len(failed)}")
        if failed:
            print("build FAILED: Blender did not produce", failed[:10], "(see Build/art_pipeline.log); nothing imported")
            json.dump(log, open(os.path.join(REPORTS, f"art_build_{bid}.json"), "w"), indent=1)
            return 3
    # 3) post-process every Blender key in scope (precedence over the procedural fallback)
    m = artpipe.load_manifest(artpipe.STAGING)
    if bscope:
        m.update(artpipe.downsample_raw(artpipe.STAGING, [k for k in bscope]))
    for k in m:
        p = os.path.join(artpipe.STAGING, k + ".png")
        if os.path.exists(p):
            m[k]["sha256"] = artpipe.sha(p)
    artpipe.save_manifest(artpipe.STAGING, m)

    # 4) strict validation of the staged scope
    import validator
    summary, findings, _ = validator.run(artpipe.STAGING, os.path.join(HERE, "artspec.json"), os.path.join(HERE, "waivers.json"),
                                         os.path.join(REPORTS, "art_validation_staging"), None)
    scoped = set(keys)
    block = [f for f in findings if f["severity"] == "BLOCK" and not f["waived"] and (f["asset"] in scoped or f["asset"] == "*")]
    warn = [f for f in findings if f["severity"] == "WARN" and not f["waived"] and f["asset"] in scoped]
    log["stages"].append({"stage": "validate", "block": len(block), "warn": len(warn), "examples": [f"{f['asset']}: {f['check']}" for f in (block + warn)[:15]]})
    print(f"validate (scope): BLOCK {len(block)} WARN {len(warn)}")
    if block:
        print("build FAILED validation; nothing imported:", [f"{f['asset']}: {f['message']}" for f in block[:5]])
        json.dump(log, open(os.path.join(REPORTS, f"art_build_{bid}.json"), "w"), indent=1)
        return 2

    # 5) import (transactional, backup) — only scoped keys whose pixels or metadata changed
    changed = [(k, why) for k, why in artpipe.changed_keys(None) if k in scoped]
    old = artpipe.load_manifest(artpipe.GAME_ART)
    old_sha = {k: (artpipe.sha(os.path.join(artpipe.GAME_ART, k + ".png")) if os.path.exists(os.path.join(artpipe.GAME_ART, k + ".png")) else "") for k, _ in changed}
    if a.no_import or not changed:
        log["stages"].append({"stage": "import", "imported": 0, "reason": "--no-import" if a.no_import else "no changes"})
        print("import: skipped (" + ("--no-import" if a.no_import else "nothing changed — idempotent") + ")")
    else:
        rc = artpipe.cmd_import(argparse.Namespace(only=[k for k, _ in changed], force=False))
        hist = artpipe.read_history()
        backup = hist[-1]["id"] if hist and rc == 0 else None
        log["stages"].append({"stage": "import", "imported": len(changed) if rc == 0 else 0, "rc": rc, "backup": backup})
        if rc != 0:
            print("build FAILED in import (rolled back)")
            return rc
        new = artpipe.load_manifest(artpipe.GAME_ART)
        path = os.path.join(REPORTS, "art_replaced_assets.csv")
        first = not os.path.exists(path)
        with open(path, "a", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            if first:
                w.writerow(["build_id", "key", "change", "old_sha256", "new_sha256", "old_size", "new_size", "source", "backup_id"])
            for k, why in changed:
                o, n = old.get(k, {}), new.get(k, {})
                w.writerow([bid, k, why, old_sha.get(k, ""), artpipe.sha(os.path.join(artpipe.GAME_ART, k + ".png")),
                            f"{o.get('w', '')}x{o.get('h', '')}", f"{n.get('w')}x{n.get('h')}", n.get("src", ""), backup])
        print(f"import: {len(changed)} sprites (backup {backup})")
    log["seconds"] = round(time.time() - t0, 1)
    os.makedirs(REPORTS, exist_ok=True)
    json.dump(log, open(os.path.join(REPORTS, f"art_build_{bid}.json"), "w"), indent=1)
    print(f"build {bid} OK in {log['seconds']}s")
    return 0


if __name__ == "__main__":
    sys.exit(main())
