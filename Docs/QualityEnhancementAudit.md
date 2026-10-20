# Quality Enhancement — Phase 0 Audit (game feel, quality, operations)

Source of truth is the code at commit `9cb971a` (after Expansion Phase V). Tools available in this environment:
.NET 8 SDK, Python (skia/numpy/soundfile), headless harness `Tools/Sim`, NUnit `Tools/Tests`, Unity-reference compile
check `Tools/UnityCheck`, snapshot renderer `Tools/Art/render_snapshot.py`. **Not available:** Unity Editor (no
PlayMode, profiler, screenshots, builds), Unity MCP, Blender/Blender MCP, target devices. Every "verify" below is
therefore headless/compile/test based unless stated; visual items are checked through code review + snapshot renders.

## Audit matrix
| Feature | Source | Current behaviour | Issues found | Proposed change | Risk | Verification |
|---|---|---|---|---|---|---|
| Controls | `Game/InputAdapter.cs` | WASD/stick/touch joystick, dash, ultimate, zoom/pan | no camera-zoom preference, shake fixed strength | zoom + shake-amount settings | low | compile + settings round-trip |
| Day/night transition | `GameRoot.HandlePhaseTransitions`, `WorldView.Sync` | instant tint lerp (0.5/s), horn, banner | no dusk stage, no dawn celebration, music switches on phase only | dusk ramp in last day seconds (visual only), dawn burst, intensity music | low | code + tests of director |
| Build placement | `GameRoot.Hud.LateUpdate`, `GameSession.Build` | ghost pad green/red, range ring, cost in panel | invalid **reason** not shown at ghost; no warning when a placement seals a lane (enemies will breach); no rise animation | reason label + seal warning (Core query) + rise anim | low | tests for seal query |
| General combat | `GeneralSystem`, `UnitView` | instant hit with 0.25 s attack pose + squash | no windup/anticipation; hits feel uniform | visual windup when cooldown nearly ready; tiered impacts | low (visual) | director tests |
| AI Generals | `GeneralSystem.TickAI/Decide` | sector post, utility score, retreat <30% HP | reasons only visible in panel; no barks | barks with cooldowns + overlay | low | bark tests |
| Army auto-deploy | `DefenderAI` | leash-bounded utility targeting, formation slots | ranged soldiers walk into melee range (move to `range*0.85` of target then stand) — no spacing; cavalry has no explicit return/pursuit cap beyond leash | ranged spacing (step back when enemy in melee), pursuit timer for fast units | medium (balance) | tests + harness |
| Pathfinding after breach | `Navigation` (flow field, lazy rebuild on `TopologyVersion`) | rebuilds once per topology change | none found; cosmetic effects never touch topology | keep; add explicit test breach→gap→repair | low | test |
| Towers | `DefenderAI.TickStructure` | auto-fire, imbue, disable timer | fine | impact tiers / volley audio limit | low | — |
| Elements/reactions | `CombatSystem` | 21 reactions, budget 24/tick, generation guard | VFX: `Vfx.Element` uses 3 sprite types; elements differ mostly by colour (colour-blind risk) | per-element silhouette grammar + reaction showcases | low | code review + test of grammar table (8 distinct) |
| Decrees / Kingdom | `GameSession.Run`, `KingdomContent` | 94 decrees; kingdom banner+ring | kingdom visuals are an emblem + ring only | castle props per path (Phase 2/7) | low | snapshot |
| Enemy director | `WaveDirector` | budget + caps, groups sorted by time | no staging (siege/elites can arrive first) | staged ordering: scouts → rush → siege/elite | medium | tests + harness |
| Bosses | `BossSystem` | telegraphs, phases, exhaust | boss death has no special moment | Epic tier + relief beat | low | — |
| Achievements/save | `Profile`, `SaveSystem` | atomic write + .bak + FNV checksum, v0→v1 migration | no test for interrupted write recovery path | test truncated/garbled primary → backup | low | test |
| Mobile UI | `Gui`, HUD | IMGUI, safe area, UI scale | settings missing: shake amount, zoom, dmg-text mode, reduced motion, ambient | add settings (persisted) | low | compile + round-trip |
| Audio | `AudioService`, `build_audio.py` | 16 voices, per-clip cap/interval, 4 music loops, 1.5 s crossfade | no buses beyond music/sfx, no category caps, no dusk/danger/victory layers, music thrash risk | intensity director with hysteresis, buses (music/sfx/ui/ambience/voice), category caps, captions | low | director tests |
| Graphics pipeline | `WorldView` (SpriteRenderer, built-in RP) | unit/building GameObjects created/destroyed per actor; particles pooled | GameObject churn for units (alloc/GC on device) | pool unit views | low | code |
| Simulation perf | Core | see PerformanceBenchmarks.md | ~2–7.5 KB managed alloc per tick (ToArray/closures/HashSet in hot paths); max tick spikes 10–11 ms | remove hot-path allocations | low | perf probe before/after |

## Baseline route (headless)
`Tools/Sim perf <map> <general> <seed> 15` drives a full campaign with the AutoPlayer: build → day/night → waves →
upgrades/imbues → ultimates → breaches → castle defence → victory/defeat. Save/resume is covered by tests
(`RunTests`, Phase 3–5 save tests). Unity FPS, GPU, draw calls, overdraw and audio voices: **unverified** (no editor/device).

## Prioritised backlog (acceptance criteria)
1. Feedback director: impact tiers + budgets, aggregated shake (off/amount), aggregated damage text (full/aggregated/off) — unit tests prove budgets and aggregation.
2. Element grammar: 8 distinct particle silhouettes (shape + motion, not only colour) + reaction showcases.
3. Build feel: rise animation (reduced-motion aware), placement reason + seal warning, wall crack states, rubble.
4. AI: ranged spacing, bounded pursuit for fast units, debug overlay — tests that archers do not stand in melee and cavalry returns.
5. Music intensity with hysteresis + new stems; buses/category caps; captions.
6. Last Bastion opt-in decree, milestones, upgrade preview deltas.
7. Highlights (event ring buffer, honest: no replay), barks, elemental battlefield zones, siege aftermath.
8. Tests + harness metrics (breach time, castle HP left, procs/s).
9. Allocation reduction, view pooling, save-recovery test, release checklist.
