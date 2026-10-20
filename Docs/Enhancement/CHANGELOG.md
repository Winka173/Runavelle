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
