# Runavelle Art Bible (original "storybook-arcane" identity)

**Intent:** a hand-inked fairy-tale kingdom seen from a 3/4 top-down storybook camera; **buildings and terrain are the
focal point**, characters are chunky, readable tokens; magic is saturated light, the siege line stays readable.
Distinct from other games: soft ink outlines (dark plum `#2b2233`, never pure black), cream stone, cobalt roofs,
element colours only on magic.

## Measurable spec (enforced by `Tools/ArtPipeline/artspec.json` + validator)
| Property | Value |
|---|---|
| Pixels per unit | 64 (1 tile = 64 px); UI backdrop exception (100) waived |
| Camera | orthographic, elevation 35°, **azimuth 0°** (grid is axis-aligned; 45° would turn footprints into diamonds — changed after the first render test). Ground depth stretched ×1/sin35° on *positions and box depths only*, round shapes keep true form, so a 1×1 tile footprint is 64×64 px on screen. 2D sprites drawn front-3/4 to match |
| Light rig (3D) | key sun from upper-left, 20° off zenith (warm #fff1dc, 3.4), sky fill #b8c8e8 (0.55), warm rim from back-right (1.2); contact shadow via shadow-catcher plane clipped to the canvas width. Implemented in `Tools/ArtPipeline/blender/render_assets.py` |
| Outline | ink 2–4 px at 64 ppu (Freestyle for 3D renders, stroke for 2D) |
| Pivots | characters: foot contact (±8 px); buildings: footprint centre; walls: tile centre |
| Canvas | no opaque pixel on the canvas edge except tiles/walls (connections) |
| Edges | no white matte halo (validator `halo` ≤ 8% of 1-px fringe) |
| Scale (visible height) | soldiers 0.6–2.4 u, generals 0.9–2.4 u, bosses 1.2–3.6 u, buildings 0.6–7 u |
| Readability | contrast vs day & night ground ≥ 1.6 (rim or highlight) |
| Element signalling | shape + motion + colour (ElementGrammar); status icons distinct under deuteranopia/protanopia |
| Directions | 2 (facing right, mirrored for left) — **decision**: the camera is 3/4 side-on and gameplay facing is left/right, 4/8 directions would multiply sprite count ×4 without readability gain at 0.6–2.4 u |
| Animation | target: idle 2, walk 4, attack 3, hit 1, death 3 at 10 fps; current: 2 frames + procedural bob |

## Castle growth (P1 target)
Tier 1 keep → Tier 2 keep + curtain wall → Tier 3 citadel; kingdom forms: **Fortress** (thick battered walls,
squat towers, iron gate), **Legion** (banners, drill yard, barracks wing, red cloth), **Arcanum** (rune-lit crystal
spires, floating shards, violet glow). Each form must differ in **silhouette** (checked by the validator's
silhouette-IoU report in `previews/`), not only colour.

## Human review checklist (ACCEPT / REWORK)
1. Readable at 720p actual size? 2. Silhouette distinct from neighbours in the same family? 3. Shadow down-right?
4. No cropped parts? 5. Outline weight consistent? 6. Looks like the same world as the existing tiles/walls?
