# Expansion Progress

Statuses: `not_started`, `in_progress`, `verified` (built + automated checks ran), `blocked` (reason).
"verified" never means Unity-Editor verified unless stated: Unity Editor is unavailable in this environment.

| Phase | Status | Evidence / notes |
|---|---|---|
| MVP foundation (prerequisite – repo was empty) | verified (engine-free) | 31/31 tests, 72-run harness 0 exceptions, Unity-layer compile-check, art/audio generated, scene previews |
| I – Generals & Armies | verified (engine-free) | 16 Generals (2 per element, all 4 groups, 32 evolutions), 12 soldier types × 2 branches, 2 AI slots; 39/39 tests; 384-run harness 0 exceptions |
| II – Light & Shadow | verified (engine-free) | 8-element hard cap tests (incl. save with id 9 → None); Light/Shadow in Generals, soldier branches, towers, walls, 3 Shadow Court enemies (stealth/curse/shield), Rift Warlock curse zone, 6 decrees, synergy inspect UI, onboarding hint; 47/47 tests |
| III – Kingdom evolution, Decrees, Equipment | verified (engine-free) | Fortress/Legion/Arcanum (mechanics + castle banner/aura), 94 decrees across 6 categories with mutual exclusions, every content hook implemented (source-scan test), Weapon+Relic (8+8), real-time siege warnings; 58/58 tests |
| IV – Siegefronts (maps, factions, enemies, bosses) | not_started | |
| V – Endless, Ascension, Mastery, Retention | not_started | |

## Blocked items (all phases)
- Unity Editor import, PlayMode tests, screenshots/video, player builds, device FPS: **blocked** – no Unity Editor/SDK/devices.
- Unity MCP / Blender MCP: **blocked** – not connected in this environment.
