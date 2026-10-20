# Baseline (final quality / art / balance pass) — evidence only

Date: 2026-10-20 · repo commit before this pass: `0bcce60`.

## Environment (measured)
| Item | Value |
|---|---|
| Unity project version | `6000.4.8f1` (ProjectSettings/ProjectVersion.txt) — **Unity Editor not installed here** |
| Render pipeline | Built-in, SpriteRenderer, no URP / 2D lights (Packages/manifest.json has no URP) |
| Scenes | 0 `.unity` files: the game boots from code (`RuntimeInitializeOnLoadMethod` in `Game/GameRoot.cs`) |
| Animation format | 2 sprite frames per character (`*_0` idle, `*_1` attack) + procedural bob/squash in `WorldView.UnitView` |
| Input | legacy Input Manager (`Game/InputAdapter.cs`): keyboard/mouse/gamepad/touch joystick |
| UI | IMGUI (`Game/Gui.cs`, `GameRoot.*.cs`) |
| Saves | `Core/Persistence/SaveSystem.cs` (atomic + .bak + checksum, run save v1, migration v0→v1), `Profile.cs` |
| Toolchain | .NET 8.0.132, Python 3.13 (skia, numpy, scipy, Pillow, soundfile), **Blender 4.0.2 (apt, headless, Cycles CPU; EEVEE unavailable — no GPU/EGL)** |
| MCP | Unity MCP, Blender MCP: **not connected** (optional adapters; CLI used instead) |
| Targets | Windows/Android/iOS build menus exist (`Editor/RunavelleEditor.cs`); **no builds possible here** |

## Commands run and results
| Command | Result |
|---|---|
| `cd Tools/Tests && dotnet test` | Passed 106/106 |
| `cd Tools/UnityCheck && dotnet build` | 0 errors, 0 warnings (against Unity 2021.3 reference DLLs) |
| `python3 -m unittest discover -s Tools/ArtPipeline/tests` | 9/9 validator self-tests OK |
| `python3 Tools/ArtPipeline/artpipe.py validate` | 543 sprites, BLOCK 0, WARN 90, waived 14 (see `art_validation/summary.md`) |
| `python3 Tools/ArtPipeline/artpipe.py pack` | 5 atlas groups, ≈8.9 MB ASTC 6x6 (estimate, `atlas_report.md`) |
| `cd Tools/Sim && dotnet run -c Release -- seeds=2 maps=all generals=all` | 256 runs, 77% bot wins, 0 exceptions (Docs/Balance_Spec.md) |
| `... perf moonstone_valley lyra 1000 15` | tick p99 0.28 ms, 0.45 KB alloc/tick (Docs/PerformanceBenchmarks.md) |

## Implemented vs missing (relevant to this prompt)
| Feature | State | Path |
|---|---|---|
| Castle tiers 1/2/3a/3b + kingdom emblem/props | implemented, **visually weak** (tiers look alike at game zoom) | `Tools/Art/terrain_buildings.py`, `WorldView.SyncKingdomProps` |
| Pre-night forecast (lanes, tags, counts) | implemented, no confidence/bypass info | `GameRoot.Hud.DrawDayHud`, `WaveDirector.BuildTags` |
| Army rally before night | missing | — |
| Breach: cracks, rubble, markers, re-path, squad breach priority | implemented | `WorldView`, `DefenderAI.InBreach`, `Navigation` |
| Inner fallback when breached | missing | — |
| General–army synergy (auras) | stat auras exist; little visible expression | `AuraSystem.cs` |
| Kingdom Chronicle | missing (only per-run analytics/history) | `RunAnalytics`, `Profile.History` |
| Art pipeline | procedural Python/skia generators only; no staging/validation/rollback | `Tools/Art/*.py` |
| Blender pipeline | missing | — |
| Balance lab | AutoPlayer with 4 strategies (Balanced/Economy/Towers/Army/Elemental) | `Core/QA/AutoPlayer.cs`, `Tools/Sim` |

## Art audit (human review of `previews/actual_zoom_game.png` + validator)
| Group | Tag | Reason |
|---|---|---|
| Castle 1/2/3a/3b | **REMAKE** | silhouettes nearly identical at 1080p/720p; no Fortress/Legion/Arcanum castle forms |
| Walls/gates 4 materials × 16 masks | KEEP (+ REFINE damage states) | seamless joins verified earlier; destruction only as overlay |
| Archer/magic towers, houses, wells, shrines, barracks | REFINE | magic spire tips cropped (validator `clipped`), towers fine at zoom |
| Characters (units, enemies, generals, bosses) | REFINE | readable silhouettes, but 85 attack frames cropped at the canvas edge; 2-frame animation only |
| Clockwork drone | REFINE | 0.34 world units tall — too small on phones (validator `scale`) |
| Terrain, decor, FX, UI, icons | KEEP | validator clean (FX are runtime-tinted masks) |
