# Expansion Progress

Statuses: `not_started`, `in_progress`, `verified` (built + automated checks ran), `blocked` (reason).
"verified" never means Unity-Editor verified unless stated: Unity Editor is unavailable in this environment.

| Phase | Status | Evidence / notes |
|---|---|---|
| MVP foundation (prerequisite – repo was empty) | verified (engine-free) | 31/31 tests, 72-run harness 0 exceptions, Unity-layer compile-check, art/audio generated, scene previews |
| I – Generals & Armies | verified (engine-free) | 16 Generals (2 per element, all 4 groups, 32 evolutions), 12 soldier types × 2 branches, 2 AI slots; 39/39 tests; 384-run harness 0 exceptions |
| II – Light & Shadow | verified (engine-free) | 8-element hard cap tests (incl. save with id 9 → None); Light/Shadow in Generals, soldier branches, towers, walls, 3 Shadow Court enemies (stealth/curse/shield), Rift Warlock curse zone, 6 decrees, synergy inspect UI, onboarding hint; 47/47 tests |
| III – Kingdom evolution, Decrees, Equipment | verified (engine-free) | Fortress/Legion/Arcanum (mechanics + castle banner/aura), 94 decrees across 6 categories with mutual exclusions, every content hook implemented (source-scan test), Weapon+Relic (8+8), real-time siege warnings; 58/58 tests |
| IV – Siegefronts (maps, factions, enemies, bosses) | verified (engine-free) | 8 maps with hazards + per-map boss rotation, 6 factions (43 regular enemies), 11 multi-phase bosses, nightly side objectives, General-trap placement rule; 72/72 tests; 256-run harness (see Balance_Spec) |
| V – Endless, Ascension, Mastery, Retention | verified (engine-free) | 10 cumulative Ascension tiers (incl. elite/boss affixes, 2-option drafts, building cap, partial dawn repair, double final boss), Endless after the campaign (per-night escalation + seeded mutator, boss every 5 nights, retire), 8 Challenge runs, offline daily run, checksummed share codes, 22 achievements, 5-level General mastery, 5 cosmetics, end-run share/mastery lines, one-time feature hints, slow-nights accessibility option; 83/83 tests; ascension sweep 576 runs |

## Blocked items (all phases)
- Unity Editor import, PlayMode tests, screenshots/video, player builds, device FPS: **blocked** – no Unity Editor/SDK/devices.
- Unity MCP / Blender MCP: **blocked** – not connected in this environment.
