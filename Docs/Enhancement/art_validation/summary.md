# Art validation — 2026-10-20T01:55:06

Assets: **543** · BLOCK: **35** · WARN: **330** · waived: 3 · texture memory (RGBA32, uncompressed): 28.65 MB

| Check | Count |
|---|---|
| clipped | 137 |
| foot_pivot | 108 |
| halo | 52 |
| opaque_expected | 24 |
| tile_size | 24 |
| cvd_deuteranopia | 5 |
| readability | 4 |
| cvd_protanopia | 4 |
| scale | 2 |
| no_alpha | 2 |
| max_dim | 2 |
| frame_jump | 1 |

## Findings (not waived)

| Severity | Asset | Check | Message |
|---|---|---|---|
| BLOCK | `icon/el_earth vs icon/el_fire` | cvd_deuteranopia | indistinguishable under deuteranopia: colour distance 0.024, shape IoU 1.00 |
| BLOCK | `icon/el_earth vs icon/el_lightning` | cvd_deuteranopia | indistinguishable under deuteranopia: colour distance 0.067, shape IoU 1.00 |
| BLOCK | `icon/el_fire vs icon/el_lightning` | cvd_deuteranopia | indistinguishable under deuteranopia: colour distance 0.047, shape IoU 1.00 |
| BLOCK | `icon/el_light vs icon/el_wind` | cvd_deuteranopia | indistinguishable under deuteranopia: colour distance 0.044, shape IoU 1.00 |
| BLOCK | `icon/el_shadow vs icon/el_water` | cvd_deuteranopia | indistinguishable under deuteranopia: colour distance 0.068, shape IoU 1.00 |
| BLOCK | `icon/el_earth vs icon/el_fire` | cvd_protanopia | indistinguishable under protanopia: colour distance 0.016, shape IoU 1.00 |
| BLOCK | `icon/el_earth vs icon/el_lightning` | cvd_protanopia | indistinguishable under protanopia: colour distance 0.070, shape IoU 1.00 |
| BLOCK | `icon/el_fire vs icon/el_lightning` | cvd_protanopia | indistinguishable under protanopia: colour distance 0.073, shape IoU 1.00 |
| BLOCK | `icon/el_light vs icon/el_wind` | cvd_protanopia | indistinguishable under protanopia: colour distance 0.018, shape IoU 1.00 |
| BLOCK | `ui/logo` | max_dim | 1024x256 exceeds 512 px |
| BLOCK | `ui/title_bg` | max_dim | 1280x720 exceeds 512 px |
| BLOCK | `terrain/chasm_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/chasm_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/chasm_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/dirt_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/dirt_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/dirt_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/ice_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/ice_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/ice_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/lava_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/lava_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/lava_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/plateau_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/plateau_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/plateau_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/sand_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/sand_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/sand_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/shallows_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/shallows_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/shallows_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/water_blob_0` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/water_blob_1` | tile_size | expected [64, 64], got [104, 104] |
| BLOCK | `terrain/water_blob_2` | tile_size | expected [64, 64], got [104, 104] |
| WARN | `building/gate_bastion_h` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/gate_rune_h` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/gate_stone_h` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/gate_wood_h` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/magic_spire_earth` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_fire` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_ice` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_light` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_lightning` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_shadow` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_water` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/magic_spire_wind` | clipped | opaque pixels touch the canvas edge (bbox [21, 0, 130, 238]) — silhouette may be cropped |
| WARN | `building/wall_bastion_10` | clipped | opaque pixels touch the canvas edge (bbox [0, 29, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_11` | clipped | opaque pixels touch the canvas edge (bbox [0, 5, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_12` | clipped | opaque pixels touch the canvas edge (bbox [0, 14, 48, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_13` | clipped | opaque pixels touch the canvas edge (bbox [0, 5, 48, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_14` | clipped | opaque pixels touch the canvas edge (bbox [0, 14, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_15` | clipped | opaque pixels touch the canvas edge (bbox [0, 5, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_2` | clipped | opaque pixels touch the canvas edge (bbox [15, 14, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_3` | clipped | opaque pixels touch the canvas edge (bbox [15, 5, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_6` | clipped | opaque pixels touch the canvas edge (bbox [15, 14, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_7` | clipped | opaque pixels touch the canvas edge (bbox [15, 5, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_8` | clipped | opaque pixels touch the canvas edge (bbox [0, 14, 48, 90]) — silhouette may be cropped |
| WARN | `building/wall_bastion_9` | clipped | opaque pixels touch the canvas edge (bbox [0, 5, 48, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_10` | clipped | opaque pixels touch the canvas edge (bbox [0, 33, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_11` | clipped | opaque pixels touch the canvas edge (bbox [0, 9, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_12` | clipped | opaque pixels touch the canvas edge (bbox [0, 18, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_13` | clipped | opaque pixels touch the canvas edge (bbox [0, 9, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_14` | clipped | opaque pixels touch the canvas edge (bbox [0, 18, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_15` | clipped | opaque pixels touch the canvas edge (bbox [0, 9, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_2` | clipped | opaque pixels touch the canvas edge (bbox [17, 18, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_3` | clipped | opaque pixels touch the canvas edge (bbox [17, 9, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_6` | clipped | opaque pixels touch the canvas edge (bbox [17, 18, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_7` | clipped | opaque pixels touch the canvas edge (bbox [17, 9, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_8` | clipped | opaque pixels touch the canvas edge (bbox [0, 18, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_rune_9` | clipped | opaque pixels touch the canvas edge (bbox [0, 9, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_10` | clipped | opaque pixels touch the canvas edge (bbox [0, 35, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_11` | clipped | opaque pixels touch the canvas edge (bbox [0, 11, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_12` | clipped | opaque pixels touch the canvas edge (bbox [0, 20, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_13` | clipped | opaque pixels touch the canvas edge (bbox [0, 11, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_14` | clipped | opaque pixels touch the canvas edge (bbox [0, 20, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_15` | clipped | opaque pixels touch the canvas edge (bbox [0, 11, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_2` | clipped | opaque pixels touch the canvas edge (bbox [17, 20, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_3` | clipped | opaque pixels touch the canvas edge (bbox [17, 11, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_6` | clipped | opaque pixels touch the canvas edge (bbox [17, 20, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_7` | clipped | opaque pixels touch the canvas edge (bbox [17, 11, 63, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_8` | clipped | opaque pixels touch the canvas edge (bbox [0, 20, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_stone_9` | clipped | opaque pixels touch the canvas edge (bbox [0, 11, 46, 90]) — silhouette may be cropped |
| WARN | `building/wall_wood_10` | clipped | opaque pixels touch the canvas edge (bbox [0, 34, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_11` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_12` | clipped | opaque pixels touch the canvas edge (bbox [0, 21, 43, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_13` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 43, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_14` | clipped | opaque pixels touch the canvas edge (bbox [0, 21, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_15` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_2` | clipped | opaque pixels touch the canvas edge (bbox [20, 21, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_3` | clipped | opaque pixels touch the canvas edge (bbox [20, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_6` | clipped | opaque pixels touch the canvas edge (bbox [20, 21, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_7` | clipped | opaque pixels touch the canvas edge (bbox [20, 15, 63, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_8` | clipped | opaque pixels touch the canvas edge (bbox [0, 21, 43, 88]) — silhouette may be cropped |
| WARN | `building/wall_wood_9` | clipped | opaque pixels touch the canvas edge (bbox [0, 15, 43, 88]) — silhouette may be cropped |
| WARN | `decor/mountain_3` | clipped | opaque pixels touch the canvas edge (bbox [0, 34, 110, 109]) — silhouette may be cropped |
| WARN | `enemy/boss_bone_colossus_0` | clipped | opaque pixels touch the canvas edge (bbox [20, 0, 146, 176]) — silhouette may be cropped |
| WARN | `enemy/boss_bone_colossus_1` | clipped | opaque pixels touch the canvas edge (bbox [20, 0, 183, 176]) — silhouette may be cropped |
| WARN | `enemy/boss_hollow_emperor_0` | clipped | opaque pixels touch the canvas edge (bbox [31, 0, 142, 184]) — silhouette may be cropped |
| WARN | `enemy/boss_hollow_emperor_1` | clipped | opaque pixels touch the canvas edge (bbox [31, 0, 191, 184]) — silhouette may be cropped |
| WARN | `enemy/boss_siege_lord_0` | clipped | opaque pixels touch the canvas edge (bbox [28, 0, 146, 184]) — silhouette may be cropped |
| WARN | `enemy/boss_siege_lord_1` | clipped | opaque pixels touch the canvas edge (bbox [28, 0, 191, 184]) — silhouette may be cropped |
| WARN | `enemy/boss_siege_titan_1` | clipped | opaque pixels touch the canvas edge (bbox [15, 2, 175, 168]) — silhouette may be cropped |
| WARN | `enemy/catapult_1` | clipped | opaque pixels touch the canvas edge (bbox [14, 0, 97, 87]) — silhouette may be cropped |
| WARN | `enemy/crypt_knight_0` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 72, 96]) — silhouette may be cropped |
| WARN | `enemy/crypt_knight_1` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 100, 96]) — silhouette may be cropped |
| WARN | `enemy/cyclone_shaman_0` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `enemy/cyclone_shaman_1` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `enemy/fallen_templar_0` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 68, 88]) — silhouette may be cropped |
| WARN | `enemy/fallen_templar_1` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 91, 88]) — silhouette may be cropped |
| WARN | `enemy/flame_priest_0` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `enemy/flame_priest_1` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `enemy/glacier_golem_1` | clipped | opaque pixels touch the canvas edge (bbox [13, 4, 119, 112]) — silhouette may be cropped |
| WARN | `enemy/hex_shaman_0` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 57, 72]) — silhouette may be cropped |
| WARN | `enemy/hex_shaman_1` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 57, 72]) — silhouette may be cropped |
| WARN | `enemy/magma_brute_0` | clipped | opaque pixels touch the canvas edge (bbox [12, 0, 81, 96]) — silhouette may be cropped |
| WARN | `enemy/magma_brute_1` | clipped | opaque pixels touch the canvas edge (bbox [12, 0, 103, 96]) — silhouette may be cropped |
| WARN | `enemy/orc_brute_1` | clipped | opaque pixels touch the canvas edge (bbox [16, 7, 95, 88]) — silhouette may be cropped |
| WARN | `enemy/ram_crew_0` | clipped | opaque pixels touch the canvas edge (bbox [9, 17, 111, 88]) — silhouette may be cropped |
| WARN | `enemy/ram_crew_1` | clipped | opaque pixels touch the canvas edge (bbox [9, 17, 111, 88]) — silhouette may be cropped |
| WARN | `enemy/rune_sentinel_0` | clipped | opaque pixels touch the canvas edge (bbox [0, 0, 69, 72]) — silhouette may be cropped |
| WARN | `enemy/rune_sentinel_1` | clipped | opaque pixels touch the canvas edge (bbox [0, 0, 79, 72]) — silhouette may be cropped |
| WARN | `enemy/siege_golem_0` | clipped | opaque pixels touch the canvas edge (bbox [10, 0, 90, 104]) — silhouette may be cropped |
| WARN | `enemy/siege_golem_1` | clipped | opaque pixels touch the canvas edge (bbox [10, 0, 111, 104]) — silhouette may be cropped |
| WARN | `enemy/sky_lancer_1` | clipped | opaque pixels touch the canvas edge (bbox [19, 10, 111, 100]) — silhouette may be cropped |
| WARN | `enemy/yeti_0` | clipped | opaque pixels touch the canvas edge (bbox [14, 0, 79, 96]) — silhouette may be cropped |
| WARN | `enemy/yeti_1` | clipped | opaque pixels touch the canvas edge (bbox [14, 0, 79, 96]) — silhouette may be cropped |
| WARN | `general/aurelia_1` | clipped | opaque pixels touch the canvas edge (bbox [14, 1, 111, 104]) — silhouette may be cropped |
| WARN | `general/brannoc_1` | clipped | opaque pixels touch the canvas edge (bbox [33, 2, 127, 124]) — silhouette may be cropped |
| WARN | `general/corvin_1` | clipped | opaque pixels touch the canvas edge (bbox [12, 16, 111, 104]) — silhouette may be cropped |
| WARN | `general/maelis_1` | clipped | opaque pixels touch the canvas edge (bbox [14, 13, 111, 104]) — silhouette may be cropped |
| WARN | `general/maren_1` | clipped | opaque pixels touch the canvas edge (bbox [27, 19, 111, 104]) — silhouette may be cropped |
| WARN | `general/pyra_0` | clipped | opaque pixels touch the canvas edge (bbox [26, 0, 79, 104]) — silhouette may be cropped |
| WARN | `general/pyra_1` | clipped | opaque pixels touch the canvas edge (bbox [26, 0, 79, 104]) — silhouette may be cropped |
| WARN | `general/roland_0` | clipped | opaque pixels touch the canvas edge (bbox [11, 0, 78, 104]) — silhouette may be cropped |
| WARN | `general/roland_1` | clipped | opaque pixels touch the canvas edge (bbox [11, 0, 103, 104]) — silhouette may be cropped |
| WARN | `unit/cavalry_0` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 86, 88]) — silhouette may be cropped |
| WARN | `unit/cavalry_1` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 95, 91]) — silhouette may be cropped |
| WARN | `unit/cavalry_a_0` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 86, 88]) — silhouette may be cropped |
| WARN | `unit/cavalry_a_1` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 95, 91]) — silhouette may be cropped |
| WARN | `unit/cavalry_b_0` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 86, 88]) — silhouette may be cropped |
| WARN | `unit/cavalry_b_1` | clipped | opaque pixels touch the canvas edge (bbox [24, 0, 95, 91]) — silhouette may be cropped |
| WARN | `unit/elementalknight_0` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 56, 72]) — silhouette may be cropped |
| WARN | `unit/elementalknight_1` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 78, 72]) — silhouette may be cropped |
| WARN | `unit/elementalknight_a_0` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 56, 72]) — silhouette may be cropped |
| WARN | `unit/elementalknight_a_1` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 78, 72]) — silhouette may be cropped |
| WARN | `unit/elementalknight_b_0` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 56, 72]) — silhouette may be cropped |
| WARN | `unit/elementalknight_b_1` | clipped | opaque pixels touch the canvas edge (bbox [15, 0, 78, 72]) — silhouette may be cropped |
| WARN | `unit/griffinrider_1` | clipped | opaque pixels touch the canvas edge (bbox [13, 13, 95, 84]) — silhouette may be cropped |
| WARN | `unit/griffinrider_a_1` | clipped | opaque pixels touch the canvas edge (bbox [13, 13, 95, 84]) — silhouette may be cropped |
| WARN | `unit/griffinrider_b_1` | clipped | opaque pixels touch the canvas edge (bbox [13, 13, 95, 84]) — silhouette may be cropped |
| WARN | `unit/knight_0` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 57, 72]) — silhouette may be cropped |
| WARN | `unit/knight_1` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 69, 72]) — silhouette may be cropped |
| WARN | `unit/knight_a_0` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 57, 72]) — silhouette may be cropped |
| WARN | `unit/knight_a_1` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 69, 72]) — silhouette may be cropped |
| WARN | `unit/knight_b_0` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 57, 72]) — silhouette may be cropped |
| WARN | `unit/knight_b_1` | clipped | opaque pixels touch the canvas edge (bbox [6, 0, 69, 72]) — silhouette may be cropped |
| WARN | `unit/mage_0` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/mage_1` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/mage_a_0` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/mage_a_1` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/mage_b_0` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/mage_b_1` | clipped | opaque pixels touch the canvas edge (bbox [17, 0, 58, 72]) — silhouette may be cropped |
| WARN | `unit/runegolem_0` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 76, 88]) — silhouette may be cropped |
| WARN | `unit/runegolem_1` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 95, 88]) — silhouette may be cropped |
| WARN | `unit/runegolem_a_0` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 76, 88]) — silhouette may be cropped |
| WARN | `unit/runegolem_a_1` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 95, 88]) — silhouette may be cropped |
| WARN | `unit/runegolem_b_0` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 76, 88]) — silhouette may be cropped |
| WARN | `unit/runegolem_b_1` | clipped | opaque pixels touch the canvas edge (bbox [9, 0, 95, 88]) — silhouette may be cropped |
| WARN | `unit/spearman_1` | clipped | opaque pixels touch the canvas edge (bbox [18, 9, 79, 72]) — silhouette may be cropped |
| WARN | `unit/spearman_a_1` | clipped | opaque pixels touch the canvas edge (bbox [18, 9, 79, 72]) — silhouette may be cropped |
| WARN | `unit/spearman_b_1` | clipped | opaque pixels touch the canvas edge (bbox [18, 9, 79, 72]) — silhouette may be cropped |
| WARN | `building/archer_1` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/archer_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/archer_3a` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/archer_3b` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/barracks_1` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/barracks_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/barracks_elite` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/barracks_legion` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/castle_1` | foot_pivot | pivot 100px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/castle_2` | foot_pivot | pivot 100px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/castle_3a` | foot_pivot | pivot 100px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/castle_3b` | foot_pivot | pivot 100px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/gate_bastion_v` | foot_pivot | pivot 32px vs lowest opaque pixel 1px from bottom (> 10px): units will float or sink |
| WARN | `building/gate_rune_v` | foot_pivot | pivot 32px vs lowest opaque pixel 1px from bottom (> 10px): units will float or sink |
| WARN | `building/gate_stone_v` | foot_pivot | pivot 32px vs lowest opaque pixel 1px from bottom (> 10px): units will float or sink |
| WARN | `building/gate_wood_v` | foot_pivot | pivot 32px vs lowest opaque pixel 1px from bottom (> 10px): units will float or sink |
| WARN | `building/house_1` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/house_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/house_3a` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/house_3b` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_earth` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_fire` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_ice` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_light` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_lightning` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_shadow` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_water` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_1_wind` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_earth` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_fire` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_ice` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_light` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_lightning` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_shadow` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_water` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_2_wind` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_earth` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_fire` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_ice` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_light` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_lightning` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_shadow` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_water` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_catalyst_wind` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_earth` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_fire` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_ice` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_light` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_lightning` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_shadow` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_water` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/magic_spire_wind` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/ruin_1` | foot_pivot | pivot 36px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/ruin_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/ruin_3` | foot_pivot | pivot 100px vs lowest opaque pixel 11px from bottom (> 10px): units will float or sink |
| WARN | `building/shrine_1` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/shrine_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/shrine_3a` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/shrine_3b` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_10` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_11` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_12` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_13` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_14` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_15` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_2` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_3` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_6` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_7` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_8` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_bastion_9` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_10` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_11` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_12` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_13` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_14` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_15` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_2` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_3` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_6` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_7` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_8` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_rune_9` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_10` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_11` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_12` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_13` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_14` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_15` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_2` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_3` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_6` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_7` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_8` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/wall_stone_9` | foot_pivot | pivot 32px vs lowest opaque pixel 21px from bottom (> 10px): units will float or sink |
| WARN | `building/well_1` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/well_2` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/well_3a` | foot_pivot | pivot 68px vs lowest opaque pixel 9px from bottom (> 10px): units will float or sink |
| WARN | `building/well_3b` | foot_pivot | pivot 68px vs lowest opaque pixel 8px from bottom (> 10px): units will float or sink |
| WARN | `enemy/boss_storm_serpent_0` | foot_pivot | pivot 12px vs lowest opaque pixel 40px from bottom (> 14px): units will float or sink |
| WARN | `enemy/boss_storm_serpent_1` | foot_pivot | pivot 12px vs lowest opaque pixel 34px from bottom (> 14px): units will float or sink |
| WARN | `enemy/boss_tempest_roc_0` | foot_pivot | pivot 8px vs lowest opaque pixel 53px from bottom (> 14px): units will float or sink |
| WARN | `enemy/boss_tempest_roc_1` | foot_pivot | pivot 8px vs lowest opaque pixel 34px from bottom (> 14px): units will float or sink |
| WARN | `enemy/clockwork_drone_0` | foot_pivot | pivot 8px vs lowest opaque pixel 24px from bottom (> 10px): units will float or sink |
| WARN | `enemy/clockwork_drone_1` | foot_pivot | pivot 8px vs lowest opaque pixel 24px from bottom (> 10px): units will float or sink |
| WARN | `enemy/thunder_bird_0` | foot_pivot | pivot 8px vs lowest opaque pixel 31px from bottom (> 10px): units will float or sink |
| WARN | `enemy/thunder_bird_1` | foot_pivot | pivot 8px vs lowest opaque pixel 22px from bottom (> 10px): units will float or sink |
| WARN | `general/brannoc_1` | foot_pivot | pivot 12px vs lowest opaque pixel 3px from bottom (> 8px): units will float or sink |
| WARN | `enemy/boss_storm_serpent` | frame_jump | boss_storm_serpent_0→boss_storm_serpent_1 silhouette IoU 0.44 < 0.55 (identity drift / popping) |
| WARN | `building/magic_1_ice` | halo | bright fringe on 18% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_1_light` | halo | bright fringe on 21% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_1_lightning` | halo | bright fringe on 16% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_2_ice` | halo | bright fringe on 23% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_2_light` | halo | bright fringe on 27% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_2_lightning` | halo | bright fringe on 20% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_catalyst_ice` | halo | bright fringe on 53% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_catalyst_light` | halo | bright fringe on 58% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_catalyst_lightning` | halo | bright fringe on 45% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_spire_ice` | halo | bright fringe on 23% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_spire_light` | halo | bright fringe on 27% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/magic_spire_lightning` | halo | bright fringe on 20% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/shrine_2` | halo | bright fringe on 10% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `building/shrine_3a` | halo | bright fringe on 10% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_frost_queen_0` | halo | bright fringe on 12% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_frost_queen_1` | halo | bright fringe on 33% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_hollow_emperor_0` | halo | bright fringe on 21% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_hollow_emperor_1` | halo | bright fringe on 17% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_storm_serpent_0` | halo | bright fringe on 12% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/boss_storm_serpent_1` | halo | bright fringe on 19% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/fallen_templar_1` | halo | bright fringe on 16% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/frost_wolf_1` | halo | bright fringe on 9% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/frost_wraith_1` | halo | bright fringe on 18% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/glacier_golem_1` | halo | bright fringe on 14% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/necromancer_0` | halo | bright fringe on 10% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/necromancer_1` | halo | bright fringe on 26% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/rime_caster_0` | halo | bright fringe on 9% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/rime_caster_1` | halo | bright fringe on 22% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/sky_lancer_1` | halo | bright fringe on 16% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/storm_harpy_1` | halo | bright fringe on 15% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/thunder_bird_0` | halo | bright fringe on 24% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/thunder_bird_1` | halo | bright fringe on 38% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/void_shifter_0` | halo | bright fringe on 10% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `enemy/void_shifter_1` | halo | bright fringe on 26% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/aurelia_1` | halo | bright fringe on 23% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/brannoc_1` | halo | bright fringe on 13% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/hilda_1` | halo | bright fringe on 14% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/isolde_0` | halo | bright fringe on 9% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/isolde_1` | halo | bright fringe on 34% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/lyra_1` | halo | bright fringe on 30% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/seraphine_0` | halo | bright fringe on 37% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `general/seraphine_1` | halo | bright fringe on 54% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `ui/joystick_base` | halo | bright fringe on 23% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/archer_a_1` | halo | bright fringe on 13% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/archer_b_1` | halo | bright fringe on 15% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/battlemage_a_1` | halo | bright fringe on 13% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/battlemage_b_1` | halo | bright fringe on 15% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/cavalry_b_1` | halo | bright fringe on 14% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/griffinrider_b_1` | halo | bright fringe on 17% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/knight_b_1` | halo | bright fringe on 19% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/runegolem_b_1` | halo | bright fringe on 14% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `unit/shieldguardian_a_1` | halo | bright fringe on 22% of edge pixels (white halo / un-premultiplied edge) |
| WARN | `fx/pixel` | no_alpha | sprite has no transparency (box silhouette) |
| WARN | `ui/title_bg` | no_alpha | sprite has no transparency (box silhouette) |
| WARN | `terrain/chasm_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/chasm_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/chasm_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/dirt_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/dirt_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/dirt_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/ice_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/ice_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/ice_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/lava_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/lava_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/lava_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/plateau_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/plateau_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/plateau_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/sand_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/sand_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/sand_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/shallows_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/shallows_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/shallows_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/water_blob_0` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/water_blob_1` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `terrain/water_blob_2` | opaque_expected | tile should be fully opaque (seams at runtime) |
| WARN | `decor/reeds_0` | readability | low contrast vs day/night ground (min ratio 1.52 < 1.6) |
| WARN | `icon/el_shadow` | readability | low contrast vs day/night ground (min ratio 1.55 < 1.6) |
| WARN | `ui/bar_bg` | readability | low contrast vs day/night ground (min ratio 1.08 < 1.6) |
| WARN | `ui/panel_dark` | readability | low contrast vs day/night ground (min ratio 1.27 < 1.6) |
| WARN | `enemy/clockwork_drone_0` | scale | visible height 0.34 world units outside [0.5, 3.4] |
| WARN | `enemy/clockwork_drone_1` | scale | visible height 0.41 world units outside [0.5, 3.4] |
