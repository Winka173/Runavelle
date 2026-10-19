# Expansion Implementation Plan

Order: Phase I → II → III → IV → V, each a vertical slice first, then data batches. Every phase gate requires:
1. Core + Unity layer compile (Tools/Core, Tools/UnityCheck), 2. all NUnit tests pass (new tests added per phase),
3. headless harness runs with 0 exceptions on all maps, 4. content validation (design locks) passes, 5. catalog regenerated,
6. Vietnamese phase report + `Expansion_Progress.md` update, 7. commit + push to `main`.

## Risk register
| Risk | Mitigation |
|---|---|
| No Unity Editor – runtime-only Unity issues | Keep Unity layer thin (IMGUI/SpriteRenderer), compile-check vs reference assemblies, document first-open checklist |
| Balance drift with many new options | Harness per phase across Generals/strategies/maps; flag 0% and >90% outliers |
| Content explosion breaks saves | Stable string IDs, `IdMigrations`, unknown IDs skipped on load, migration tests |
| Reaction/proc spikes with more sources | global budget + ICD + generation guard; harness records max reactions per tick |
| Hooks declared but unimplemented | Each `Hook` must be referenced by a system; test enumerates hooks used by decrees and checks implementation list |
| Art scope (16 Generals, 40 enemies) | Parametric character builder; contact sheets reviewed each phase |

## Rollback
Each phase lands as separate commits on `main`; reverting a phase = reverting its commits. Save files carry
`saveVersion`; content removals go through `IdMigrations` so older saves keep loading.
