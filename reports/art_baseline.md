# Art baseline (Phase 0) — measured

Commands (all reproducible):
- `python3 Tools/ArtPipeline/audit.py --tag baseline` → `reports/art_inventory.csv`, `reports/art_manifest.json`, `reports/toolchain_capabilities.md`, `reports/art_baseline_audit.md`, `reports/art_validation_baseline/`
- `python3 Tools/ArtPipeline/evidence.py --tag before --refresh` → `reports/art_evidence/before_*.png` (snapshots cached in `Build/evidence/*.json`, reused for "after")

State found:
- Branch `main`, Unity project version 6000.4.8f1, packages: 2D Sprite + built-in modules only (no URP, no Addressables, no SpriteAtlas package usage).
- No Unity Editor in this container → Editor import, play mode, in-engine screenshots, player builds and device profiling are **BLOCKED** here.
- 556 sprites in `Assets/Runavelle/Resources/Art` (+ `manifest.json` with pivot/ppu). 0 `.meta` files committed, 0 scenes/prefabs/animator controllers/sprite atlases: the game is code-booted, sprites load via `Resources.Load` by key; `RunavelleAssetImport` (AssetPostprocessor) forces Sprite/uncompressed/no-NPOT-scale import.
- Validator: BLOCK 0 / WARN 0 / 16 reasoned waivers; RGBA32 total 35.32 MB.
- Sources: 539 procedural (skia, `Tools/Art/*.py`) + 16 Blender renders (castles) + 1 procedural rally flag; 88 manifest entries had lost their `src` field (import only refreshed changed PNGs — fixed in `artpipe.py import`).
- Verdicts (policy in `audit.py`): KEEP 215, IMPROVE 214 (characters: 2-frame animation only), REGENERATE 127 (2D buildings next to the 3D castle).
- 10 icons have no code reference (icon/build, clock, heart, sell, shield, skull, star, sword, ult, upgrade) — kept, listed as unused.
- Performance: no FPS/frame-time measured (no device / Editor). Simulation tick cost is measured separately in `Docs/Enhancement/BENCHMARKS.md`.
