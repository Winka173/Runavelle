# STATUS (DONE WITH EVIDENCE / PARTIAL / BLOCKED / NOT STARTED)
| Item | Status | Evidence |
|---|---|---|
| Baseline audit + docs | DONE WITH EVIDENCE | BASELINE.md (commands + outputs) |
| Art Quality Validator (CLI, JSON/CSV/MD/HTML, exit code, waivers, self-tests) | DONE WITH EVIDENCE | `Tools/ArtPipeline/validator.py`, `tests/test_validator.py` 9/9, `art_validation/` |
| Art pipeline CLI (generate/render/validate/preview/diff/pack/import/rollback) | DONE WITH EVIDENCE | render→validate→diff→import→rollback→re-import exercised on 16 castle sprites (rollback restored files byte-identical to HEAD, re-import hash `4caea36c10be` matched). `generate` (555 sprites, ≈3 s) → validate → diff (451 changed) → import exercised end-to-end |
| Blender headless 3D→2D renderer | DONE WITH EVIDENCE | `blender/render_assets.py` (Cycles CPU, Freestyle ink, shadow catcher, 2× → premultiplied Lanczos). 16 assets in 6 m 13 s on 4 cores. Validator on staging: BLOCK 0 / WARN 0 |
| Validator Unity Editor window | NOT STARTED | — |
| P1 Castle growth — castle art | DONE WITH EVIDENCE | castle_1/2/3a/3b remade in 3D + 12 kingdom forms `castle_<tier>_<fortress|legion|arcanum>`; wired via `ArtKeys.CastleVariant` (fallback to base + overlay props); `ArtReferenceTests` requires every tier×kingdom key (106/106 pass); previews `previews/castle_tiers_x_kingdoms.png`, `previews/castle_in_context_moonstone_n6.png`, `previews/before_after.png`. Known REFINE: plinth reads slightly sandy, hard shadow edge on the right |
| P1 Castle growth — living world | NOT STARTED | — |
| REFINE: cropped sprites (85 clipped) | DONE WITH EVIDENCE | `artlib.Canvas` 64 px invisible safety margin, sprites grow to fit, frames of one animation unified (same size+pivot), pivot compensated. Validator on game art: BLOCK 0 / WARN 0 / waived 16 (was WARN 90). Snapshot `previews/castle_in_context_moonstone_n6.png` layout unchanged |
| REFINE: clockwork drone scale, el_shadow icon readability | DONE WITH EVIDENCE | drone ×1.6 (0.34 → in range), shadow icon pale rim; both pass validator |
| P2 Night threat preview (directions, specials, bypass, confidence) | DONE WITH EVIDENCE | `Core/Systems/NightForecast.cs` built only from the authoritative NightPlan. `ForecastTests`: 96 nights × 8 maps — spawned unit×lane counts == forecast; next-night report forecast Exact 16 / Estimate 12 (Estimate always carries a reason); no side effects (identical run state with/without peeking); flyers & burrowers always flagged VƯỢT TƯỜNG with their sky lane shown. Fixed: report screen used `Waves.Plan` without boss override/Ascension rules |
| P2 Army rally choreography | NOT STARTED | — |
| P3 … P9 | NOT STARTED | — |
| Unity MCP / Blender MCP | BLOCKED | not connected; CLI used |
| Unity builds / device profiling | BLOCKED | no Unity Editor / devices |
