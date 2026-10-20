# Benchmarks
Simulation CPU/allocation: `Docs/PerformanceBenchmarks.md` (headless, container — not a device).
Art: texture memory and atlas estimate in `atlas_report.md`. Unity FPS, GPU, draw calls, overdraw: **NOT RUN**.
New measurements of this pass are appended below per phase.

## Art render (Blender 4.0.2, Cycles CPU, 4 cores, 96 spp adaptive, 2× res)
| Batch | Wall time | Per asset |
|---|---|---|
| 16 castle sprites (512×768 raw → 256×384) | 6 m 13 s (user 23 m 47 s) | ≈23 s |
Texture memory after import: validator reports total RGBA32 for the art folder (see `art_validation/summary.md`).
Art folder RGBA32 total: 28.65 MB → 33.67 MB (+5.0 MB: 12 new castle forms + bigger castle canvas). Only one castle sprite is loaded at runtime (ArtLibrary loads lazily from Resources), so live cost is ≈0.4 MB.
