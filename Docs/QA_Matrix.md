# QA Matrix

Status: **PASS** (ran and passed), **FAIL**, **NOT RUN** (with reason). Nothing here is claimed without running it.

## Automated (engine-free, `dotnet test Tools/Tests`) — 58/58 PASS after Phase III (2026-10-19)
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

## Phase II tests (added) — PASS
| Test | Status |
|---|---|
| Ninth element impossible (enum/allow-list; save with element 9 loads as None) | PASS |
| Light & Shadow present in Generals, soldier branches, magic towers, enemies, decrees, ≥3 reactions each, a Shadow boss ability | PASS |
| Mixed-source Eclipse (Light tower + Shadow General) | PASS |
| Purify heals structures, Fortify shields walls, Haunted Arc hits marked, Soul Frost executes normal but not elite | PASS |
| Stealth assassin untargetable until revealed by Light/proximity | PASS |
| Templar guards allies; Cultist marks defenders with Shadow | PASS |
| Rift Warlock phase 2 creates an enemy curse (Gloom) zone | PASS |
| Light/Shadow decree hooks (Shadow Harvest mana, Radiant Walls) | PASS |

## Phase III tests (added) — PASS
| Test | Status |
|---|---|
| 80–100 decrees, unique names, ≥6 per category, each has an effect, mutual exclusions | PASS |
| Every hook used by decrees/equipment/kingdoms is read by a system (source scan) | PASS |
| Royal Architect, Arcane Rebirth, First Strike/Siege Breakers/Sky Watch, Emergency Masons + Mana Shield, Insomnia, Legion night reinforcement | PASS |
| Kingdom evolutions change mechanics; Arcanum secondary slot persists through save/load | PASS |
| Equipment: 8 weapons + 8 relics, slot validation, weapon secondary element, Phoenix Charm revive, one-secondary-element rule | PASS |
| Full runs: drafts never offer ineligible/duplicate decrees | PASS |

## Phase IV tests (added) — PASS
| Test | Status |
|---|---|
| 8 maps in unlock order, all validate (lanes reach castle, 1–2 protected sides), 3-boss rotation each, distinct hazards | PASS |
| 38–46 regular enemies, every faction ≥4 enemies and on ≥1 map, 10–12 bosses with ≥2 phases, counterplay text, valid summons | PASS |
| Map boss rotation drives nights 5/10/15 | PASS |
| Early nights never empty; flyer cap per night (8 maps × 6 seeds) | PASS |
| High tide floods shallows yet every ground lane still reaches the castle | PASS |
| Bomber self-destructs on a wall, no bounty; Zombie rises once unless Fire; Frost Wraith disables tower; Glacier armour chills melee | PASS |
| Mist stealths ground enemies (not bosses/flyers); burning enemy cracks thin ice → impassable, refrozen at dawn; lightning/vents fire | PASS |
| Side objective deterministic + met/unmet; houses cannot seal the General in (path still exists) | PASS |
| Bot survives the first three nights on each new map (seed 5) | PASS |

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
