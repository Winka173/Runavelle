# Changelog — final quality / art / balance pass
- Art Quality Validator + self-tests + waivers; ArtPipeline CLI with staging, transactional import, rollback, history.
- Art provenance: manifest entries now record the generator module (`src`) and staged output hash (`sha256`).
- NUnit `ArtReferenceTests`: every runtime sprite key exists in the manifest.

## FQ7/P1 — Blender castle remake
- `Tools/ArtPipeline/blender/render_assets.py`: headless Blender renderer (camera/light contract per ART_BIBLE, azimuth corrected to 0°).
- `artpipe.py render` downsamples `_raw` 2× renders; validator ignores `_`-prefixed intermediate folders.
- Castle tiers remade (canvas 256×384, pivot = footprint centre) + 12 kingdom-dressed forms; overlay props only used when a dressed sprite is missing.
- `ArtKeys.CastleVariant`; `ArtReferenceTests` covers tier × kingdom.
