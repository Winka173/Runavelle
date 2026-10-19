# QA Matrix

Status: **PASS** (ran and passed), **FAIL**, **NOT RUN** (with reason). Nothing here is claimed without running it.

## Automated (engine-free, `dotnet test Tools/Tests`) — 39/39 PASS after Phase I (2026-10-19)
| Area | Test | Status |
|---|---|---|
| Design locks | content validates; exactly 8 families; 8-element allow-list; 2 currencies; tiers ≤3 / one final choice; no full immunity | PASS |
| Reactions | every pair resolves; 18–22 bespoke; Wet+Lightning; order independence; generation>0 cannot chain; ICD + per-tick budget; Light+Shadow Eclipse | PASS |
| Combat | elemental ignores armor, physical reduced; boss CC diminishing; ice buildup→freeze→decay | PASS |
| Economy | never negative; place/sell same-day full refund then 50%; undo; no build at night / near spawns | PASS |
| Walls & paths | L-shaped contiguous wall line; gate passable for friendlies only; sealed castle keeps a breach route and enemies attack walls; flow field rebuilds only on topology change | PASS |
| Run | determinism (same seed ⇒ same state, different seed differs); wave plans (bosses on 5/10/15, flyers only on sky lanes); deterministic distinct eligible drafts | PASS |
| Saves | round trip at day boundary; corrupted file → .bak fallback; v0→v1 migration | PASS |
| Soak | soldiers stay within leash; AI Generals hold sectors (9 nights); full 15-night runs on every map without exceptions | PASS |

## Phase I tests (added) — PASS
| Test | Status |
|---|---|
| Roster targets: 16 Generals, 12 soldiers, 24 branches, 2 distinct evolutions each, every element + group covered, unique ultimates | PASS |
| Every ultimate and all 32 evolutions produce a measurable effect (damage / control / heal / castle shield / barrier) | PASS |
| Shield Guardians intercept projectiles; Griffin Riders damage flyers; Rune Golem taunts; Battlemage casts ranged spell | PASS |
| Player + 2 AI Generals through 12 nights: no stalled night, both AI Generals deal damage | PASS |
| General evolution choice + AI roster survive save/load | PASS |

## Compile checks
| Check | Status |
|---|---|
| Core compiles (`Tools/Core`, netstandard2.1, C# 9) | PASS |
| Game + Editor compile against Unity reference assemblies (UnityEngine 2021.3.33 / UnityEditor 2018.1.6) | PASS – Unity 6 API compatibility of this subset assumed, not proven |
| Unity 6000.4.8f1 import/compile | **NOT RUN** – no Unity Editor in this environment |

## Gameplay / device
| Check | Status |
|---|---|
| Headless 15-night runs (72 runs, `Tools/Sim seeds=8`) | PASS – 0 exceptions, see Balance_Spec |
| Scene composition previews from real snapshots (`Docs/Previews/scene_*.png`) | PASS (rendered, reviewed) – not a Unity capture |
| PlayMode tests, Unity screenshots, input feel, audio listening | **NOT RUN** – needs Unity Editor / speakers |
| Windows / Android / iOS player builds, FPS, thermal | **NOT RUN** – no Unity/SDKs/devices; build menus provided |
