# Runavelle art pipeline

Reproducible 3D→2D (Blender) + procedural (skia) sprite pipeline with a blocking quality validator.
The master prompt names this folder `tools/art_pipeline/`; in this repository it is **`Tools/ArtPipeline/`**
(project convention: tools live under `Tools/`).

## Setup (pinned versions measured in this repo's CI/container)

| Tool | Version | Notes |
|---|---|---|
| Python | 3.12–3.13 | `pip install numpy==2.* pillow==12.* skia-python==144.*` (skia only for the procedural stage) |
| Blender | 4.0.2 | headless `blender -b`; Cycles CPU. This Blender build has no OpenImageDenoiser (denoise off) and EEVEE needs EGL/GPU, so Cycles is used |
| .NET SDK | 8.0 | simulation harness / snapshots / tests |
| Unity | 6000.4.8f1 | not needed by the pipeline; required for Editor import, play mode, player builds |

No network, paid AI service or credential is used. All sources are scripts in this repo:
`Tools/Art/*.py` (procedural), `Tools/ArtPipeline/blender/render_assets.py` (3D models built from primitives + materials,
fixed camera/light rig, deterministic seeds).

## Commands

```bash
python3 Tools/ArtPipeline/build.py --scope golden --quality release          # benchmark slice (stage+validate+import)
python3 Tools/ArtPipeline/build.py --scope all --quality release --incremental
python3 Tools/ArtPipeline/build.py --scope building/magic_ --dry-run          # plan only
python3 Tools/ArtPipeline/build.py --scope all --force                        # ignore the render cache
python3 Tools/ArtPipeline/build.py --scope all --no-import                    # stage + validate only
python3 Tools/ArtPipeline/validate.py --strict                                # shipped art; exit 1 on BLOCK (strict: also WARN)
python3 Tools/ArtPipeline/report.py --before reports/art_baseline.md          # before/after numbers
python3 Tools/ArtPipeline/audit.py --tag after                                # inventory / manifest / toolchain
python3 Tools/ArtPipeline/evidence.py --tag after && python3 Tools/ArtPipeline/evidence.py --compare before after
python3 Tools/ArtPipeline/artpipe.py history | rollback [ID]                  # transactional import log / undo
python3 -m unittest discover -s Tools/ArtPipeline/tests -v                    # validator self-tests (broken fixtures)
```

## Stages

1. **Procedural** (`Tools/Art/build_art.py` into `Build/art_staging`): characters, enemies, terrain, icons, UI, FX.
   Canvas has an invisible 64 px safety margin; sprites grow only where content spills, animation frames are unified, pivots compensated.
2. **Blender** (`render_assets.py`): castles (4 tiers × base/Fortress/Legion/Arcanum), archer/magic towers (8 shape-coded
   element toppers), houses, wells, shrines, barracks, walls (4 materials × 16 auto-join masks), gates, ruins.
   Camera: orthographic, 35° elevation, azimuth 0 (grid-aligned), ground depth ×1/sin35° on positions; key light 12–20° off zenith,
   sky fill, rim light without shadows; Freestyle ink #2b2233; shadow catcher sized to the footprint.
   Walls: E/W arms are cut by the 1-tile frame, the N arm runs under a holdout box → seamless joins without outline seams.
3. **Post**: 2× render → premultiplied Lanczos 1× (no fringe), pivot-preserving trim, manifest entry with `src` provenance.
4. **Validate** (`validator.py`, spec `artspec.json`, waivers with reason+expiry `waivers.json`): missing/unreadable/unlisted,
   illegal dimensions, empty, alpha, RGB under zero alpha, halo, clipping, pivot bounds/foot/footprint, scale, readability vs day/night
   ground, frame sequence/missing attack frame/identical frames/size drift/IoU popping, colour-blind element distinctness,
   missing code references, Unity `.meta` linkage (missing/duplicate GUID), atlas bleed/bounds, texture budget.
   Any unwaived BLOCK in the build scope aborts the build — nothing is imported.
5. **Import**: backup to `Build/art_backups/<id>`, copy to temp names, atomic rename, manifest merge, history; `rollback` restores.

Incremental cache: `Build/art_cache.json` — key → hash(render script, Blender version, quality, key). Outputs land in
`Assets/Runavelle/Resources/Art/<key>.png` (stable paths ⇒ Unity GUIDs survive content replacement once `.meta` files exist).

## Unity import settings

`Assets/Runavelle/Scripts/Editor/RunavelleEditor.cs` (AssetPostprocessor): Sprite, alphaIsTransparency, no NPOT scaling,
uncompressed (desktop/dev). Mobile compression (ASTC) is estimated in `Docs/Enhancement/atlas_report.md`; applying platform
overrides and creating Sprite Atlases needs the Unity Editor (BLOCKED in this container).
