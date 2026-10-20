# Art audit — baseline (2026-10-20T10:17)

Sprites: **556** · validator BLOCK 0 / WARN 0 / waived 16 · RGBA32 total 35.32 MB

Verdicts: IMPROVE 214, KEEP 215, REGENERATE 127

| Category | Sprites | RGBA KB | Verdicts |
|---|---|---|---|
| building | 149 | 16647 | KEEP 22, REGENERATE 127 |
| decor | 13 | 361 | KEEP 13 |
| enemy | 110 | 5968 | IMPROVE 110 |
| fx | 33 | 414 | KEEP 33 |
| general | 32 | 1750 | IMPROVE 32 |
| icon | 64 | 1024 | KEEP 64 |
| portrait | 16 | 1024 | KEEP 16 |
| terrain | 49 | 1413 | KEEP 49 |
| ui | 18 | 5317 | KEEP 18 |
| unit | 72 | 2243 | IMPROVE 72 |

Unity linkage: no `.meta` files are committed — GUIDs are assigned on first Editor import; the game references sprites only by Resources path + manifest key (no scenes, prefabs, animator controllers or sprite atlases exist), so replacing a PNG in place keeps every reference valid.

Sprites with no literal or dynamic code reference: 10 (icon/build, icon/clock, icon/heart, icon/sell, icon/shield, icon/skull, icon/star, icon/sword, icon/ult, icon/upgrade)
