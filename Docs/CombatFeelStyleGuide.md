# Combat Feel Style Guide

All rules live in engine-free code (`Assets/Runavelle/Scripts/Core/Presentation/`) and are unit-tested
(`Tools/Tests/QualityTests*.cs`). Unity only renders their decisions (`Game/GameRoot.Feedback.cs`, `Game/Vfx.cs`).
Presentation never feeds back into the simulation (test `PresentationNeverChangesTheSimulation`).

## Impact tiers (`FeedbackRules.Classify`)
| Tier | Events | Shake (before player setting) | Local hit-stop | Budget (start/s, burst) |
|---|---|---|---|---|
| Light | ordinary hits, chain/arc reactions, common kills | 0 | none | 24/s, 12 |
| Medium | major reactions, hazards, big hits on bosses/Generals, castle hits < 40 | 0.08 | none | 10/s, 6 |
| Heavy | elite kill, wall breach, building destroyed, explosion, castle hit ≥ 40 | 0.28 | 0.07 s (struck sprite only) | 5/s, 4 |
| Epic | Ultimate, boss phase, boss kill | 0.45 (+ camera punch) | 0.12 s | never dropped |

* Hit-stop is **visual only** (the struck sprite holds its pose); the fixed 20 Hz simulation is never paused.
* Shake requests in the same frame take the **maximum** (an AoE on 30 enemies = one shake), are capped at 0.6,
  decay quickly, scale with *Settings → Rung màn hình* (Tắt / Nhẹ / Đầy đủ) and are disabled by reduced motion for punches.
* Damage numbers: one growing number per target per 0.35 s, emphasis for elites/bosses/reactions, max 36 on screen
  (smallest dropped first), modes Tắt / Chỉ quan trọng / Tất cả.

## Element grammar (`ElementGrammar`) — silhouette + motion, never colour alone
| Element | Shape | Motion | Sound |
|---|---|---|---|
| Fire | ember teardrops | rise (negative gravity) + glow | `fx_fire` roar + crackle |
| Water | droplets | splash arcs with gravity + ring | `fx_water` |
| Lightning | jagged arc segments | zigzag arcs to points | `zap` |
| Ice | long shards | fast outward burst with drag | `fx_ice` glassy crack |
| Wind | spiral strokes | tangential spiral | `fx_wind` whoosh |
| Earth | rock chips | thrown up, heavy fall + dust | `fx_earth` thud |
| Light | tapered rays | radial + ring | `fx_light` chime |
| Shadow | soft wisps | slow sinking | `fx_shadow` low drone |

Status icons keep the existing shape-coded element glyph sprites (`icon/el_*`).

## Reaction showcases (21, one silhouette each)
Weight 2 (major — larger, Medium shake): Overload (blast ring), Melt (steam + puddle decal), Firestorm (fire vortex),
Magma (eruption + scorch), Flash Freeze (inward shard cage + frost decal), Static Storm (storm cell arcs), Blizzard
(whiteout + frost), Eclipse (dark/bright ring), Solar Flare (flare). Weight 1: the rest (steam, chain arcs, downpour,
quagmire, shatter, pull, spikes, dust, bulwark, haunted arcs, soul chill, cleanse).

## Signature moments
* **Ultimate**: white anticipation ring → element ring + 2.2× element burst, camera punch (player only), nearby
  soldiers hop (cheer), ultimate-ready shimmer on the General when the cooldown is up.
* **Boss defeat**: radiant burst + gold ring, punch, banner, `boss_down` stinger, music relief beat.
* **Wall breach**: earth fragments + dust, persistent rubble decal until dawn/rebuild, caption, danger music pulse,
  pulsing "!" marker over the gap.
* **Last Bastion** (opt-in decree): gold dome ring + Light burst + `bastion` stinger + caption.
