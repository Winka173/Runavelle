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
After un-cropping refine: 35.3 MB (+1.6 MB for restored shadows/weapons and the larger drone).

## P9 high-density siege stress (`dotnet run --project Tools/Sim -c Release -- stress <map> 150`)
Night 9+, both AI Generals commissioned, +150 enemies on the ground fronts, a wall broken every 3 s. Container CPU (4 logical), not a device.

| Map | Peak units | Walls broken | Tick p50 / p95 / p99 / max (ms) | Topology-change ticks: p50 / max (ms) | Managed heap |
|---|---|---|---|---|---|
| dawnspire | 169 | 12 | 0.091 / 0.872 / 1.147 / 5.834 | 0.333 / 5.834 | 0.9 → 1.3 MB |
| shattered_crown | 170 | 16 | 0.017 / 0.477 / 0.782 / 2.959 | 0.165 / 2.307 | 1.0 → 2.1 MB |
| ember_highlands | 170 | 10 | 0.130 / 1.001 / 1.388 / 6.268 | 0.441 / 6.268 | 0.9 → 1.5 MB |

Worst spike 6.3 ms on a wall-break tick (flow-field rebuild) — inside the 50 ms simulation tick and the 16.7 ms frame budget, but it is the
largest single cost; rendering/GPU cost is not measured here (no Unity player).
