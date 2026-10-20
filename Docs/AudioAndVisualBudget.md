# Audio & Visual Budget

## Audio (`Game/Services.cs` AudioService)
* 24 pooled one-shot voices; per-clip cap (2–4) + minimum retrigger interval per clip.
* Category caps: Combat 10, Stinger 4, UI 3, Ambience 2, Voice 2. A Stinger may steal the oldest Combat voice when
  the pool is full, so breach/boss cues are never lost under arrow spam.
* Buses (Settings → Âm thanh): Master, Music, Combat SFX, UI, Ambience, Voice; captions toggle.
* Music layers (`MusicDirector`): Day, Dusk, Fight, Danger, Boss, Victory/Defeat. Danger enters at 0.62 and exits at
  0.40 (hysteresis), intensity changes need ≥ 6 s dwell; phase changes switch immediately; 1.5 s crossfade.
* All clips are synthesized by `Tools/Audio/build_audio.py` (40 files). New in this pass: `fx_fire/water/ice/wind/
  earth/light/shadow`, `dusk_bell`, `dawn_chime`, `boss_down`, `milestone`, `bastion`, `coin`, `music_dusk`,
  `music_danger`. Peak levels checked (≤ 0.8, no NaN). **Not listened to on speakers** (no audio device).

## Visual (`Game/Vfx.cs`, `Game/WorldView.cs`)
| Budget | Low | Medium | High |
|---|---|---|---|
| Live particles (hard cap) | 120 | 300 | 600 |
| Particle counts per effect | ¼ | ½ | 1× |
| Decals (rubble/scorch/frost/puddle) | persistent only | ≤ 48 | ≤ 48 |
| Damage numbers on screen | ≤ 36 (all levels) |||
| Floating labels | ≤ 40 |||
| Ambient birds | 0 (level 0) / 1 every ~22 s / flocks every ~10 s ||  |
* Effect *starts* are additionally rate-limited per impact tier (FeedbackBudget).
* Reaction ground zones (gameplay): ≤ 6 at once, refreshed instead of stacked.
* Quality presets change only cosmetics + frame cap (30/60); enemy counts/speeds/damage never change.
* Unit views are pooled (no GameObject destroy/create per death); particles are pooled; decals are capped.
* Draw calls / overdraw / atlas packing: **not measured** (no Unity editor). Sprites are individual PNGs with an
  import postprocessor; packing them into a Sprite Atlas is a recommended first step in the Editor.
