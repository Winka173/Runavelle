# Changelog — final quality / art / balance pass
- Art Quality Validator + self-tests + waivers; ArtPipeline CLI with staging, transactional import, rollback, history.
- Art provenance: manifest entries now record the generator module (`src`) and staged output hash (`sha256`).
- NUnit `ArtReferenceTests`: every runtime sprite key exists in the manifest.

## FQ7/P1 — Blender castle remake
- `Tools/ArtPipeline/blender/render_assets.py`: headless Blender renderer (camera/light contract per ART_BIBLE, azimuth corrected to 0°).
- `artpipe.py render` downsamples `_raw` 2× renders; validator ignores `_`-prefixed intermediate folders.
- Castle tiers remade (canvas 256×384, pivot = footprint centre) + 12 kingdom-dressed forms; overlay props only used when a dressed sprite is missing.
- `ArtKeys.CastleVariant`; `ArtReferenceTests` covers tier × kingdom.

## FQ7 — procedural asset refine
- `artlib.Canvas`: invisible 64 px safety margin; `save` grows the sprite only where content spills (touching counts) and compensates the pivot; `write_manifest` unifies frame groups (`unit/ enemy/ general/` `*_N`).
- 451 sprites regenerated (mostly un-cropped shadows/weapons), drone ×1.6, shadow icon rim; 2 reasoned waivers (brannoc_1 gallop hoof, storm serpent lunge).
- `artpipe generate` keeps Blender renders as precedence over procedural fallbacks.

## P2 — Night forecast
- `NightForecast` (fronts with compass/first contact/threat, special threats incl. wall bypasses, Exact/Estimate + reasons); `GameSession.Forecast`, `ForecastNext()` (same MakePlanFor path, cached).
- Day panel, report intel and lane arrows use it; flyer-only lanes now get violet arrows, arrow size ∝ threat share.
- Bug fix: next-night intel ignored boss override / Ascension 10 extra boss.

## P2 — Army Rally
- `GameSession.BeginRally/AssignRallyPosts`, `Building.RallyPost/RallyDir`, `World.Rally`; `AiTuning.RallyDistance` / `RallyRangedBack` knobs (`norally` harness/perf flag = 0).
- DefenderAI: formation at the post at dusk/night, leash anchor at the post at night.
- Dusk: horn + rally flags + skip ("TẬP HỢP… (BỎ QUA)" / start-night key).
- Navigation A* counters (`PathCalls`, `PathExpanded`) printed by `perf`, plus topology-change spike count.
- `Tools/Balance/ab_compare.py` (paired seeds, Wilson CI, bootstrap CI of the paired difference, exact McNemar) + self-tests.
- Tests adjusted with reason: leash invariant excludes `Returning` (walking to a post is not a chase); pursuit-cap test clears the post.

## P3 Breach Crisis / P5 Kingdom Chronicle
- Breach orders (plug / fall back / general guard / general inner) + `SimEventType.BreachResponse` + labels; knobs in `AiTuning` (Breach*, InnerLineOffset, BreachKeepEngaged).
- Harness: `tune.<field>=value` knob overrides, `nobreach`.
- Kingdom Chronicle with save/profile migration, search, replay; History screen tab "Biên niên"; report shows tonight's entries.
- `World.Observer` hook (emits before the 4000-event cap).

## P8 Balance lab / P4 General–army synergy
- AutoPlayer policies GreedyEconomy, WallFirst, MageHeavy, GeneralCarry, RandomPlay; `Tools/Balance/policy_report.py`; economy wealth fuzz test.
- Battle Link + Rally Strike (AuraSystem / GeneralSystem), visible ring colour, swing/projectile colour, label.

## P6 Loop polish
- `Core/QA/Insights.cs` (BuildInsight, NightInsights), NightReport.Insights, GameSession.CastleDamageThisNight.
- Report shows "Gợi ý" lines; build help shows ROI/DPS; defeat/victory screen "THỬ LẠI MÃ NÀY"; `GameRoot.Urgent.cs` edge arrows + tap-to-peek.

## P9
- `InputBindings` + Settings "Phím" tab + InputAdapter (version-cached KeyCodes, gamepad fixed).
- Save v3 (exact continuity) + `GameSession.NormalizeTransients` at save/load; Rng state get/set.
- `Tools/Sim stress <map> <extra>`; Perf counters.
