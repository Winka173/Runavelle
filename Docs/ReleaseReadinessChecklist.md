# Release Readiness Checklist

Legend: PASS (verified here) · NOT RUN (needs Unity Editor / SDK / device) · TODO.

## Build & compile
| Item | Status | Evidence / how |
|---|---|---|
| Core simulation compiles (netstandard2.1, C# 9) | PASS | `cd Tools/Core && dotnet build` |
| Game + Editor compile against Unity reference assemblies | PASS | `cd Tools/UnityCheck && dotnet build` (UnityEngine 2021.3 refs; Unity 6 API compatibility assumed) |
| Unity 6000.4.8f1 import / compile / play | NOT RUN | open project, wait for import, run *Runavelle → Checks* menu |
| Windows / Android / iOS player builds | NOT RUN | *Runavelle → Build* menus (`Editor/RunavelleEditor.cs`); iOS needs macOS + Xcode |
| CI (GitHub Actions) | configured | `.github/workflows/ci.yml` builds Core, runs NUnit, runs a short harness |

## Automated tests (NUnit, `cd Tools/Tests && dotnet test`)
| Area | Status |
|---|---|
| Core rules, economy, placement, sealing, topology, reactions, saves (incl. interrupted write) | PASS |
| Expansion phases I–V content + rules | PASS |
| Quality pass: feedback tiers/budgets/shake/damage text, element grammar, music hysteresis, highlights, barks, milestones, placement verdict, army AI (deploy, spacing, pursuit cap, breach), Last Bastion, staging, upgrade preview, battlefield zones, breach→path→repair, economy exploit loop, presentation-does-not-change-simulation | PASS |

## Headless gameplay sweeps (`Tools/Sim`)
| Item | Status |
|---|---|
| 8 maps × 16 Generals, 0 exceptions | PASS (see Balance_Spec / PerformanceBenchmarks) |
| Ascension 0/3/6/10 sweep | PASS |
| Endless / challenges | PASS (earlier sweeps, rules unchanged) |

## Manual (Unity) — NOT RUN, script in QualityEnhancementFinalReport.md §6
Visual verification of VFX/decals/props/ambient, audio mix on speakers, touch feel, safe areas on notched phones,
4:3 / ultrawide layouts, device FPS/thermals, GC spikes on a mid-range Android (IL2CPP).

## Known risks
* First Unity import may surface Unity-6-specific API warnings (only compile-checked against older reference DLLs).
* Sprites are individual PNGs — create a Sprite Atlas in the Editor before profiling draw calls.
* Bot-measured balance (bot ≠ humans).
