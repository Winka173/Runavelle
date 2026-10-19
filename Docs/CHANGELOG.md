# Changelog

## Phase II — Light & Shadow
- Shadow Court faction (eclipse theme): Shade Stalker (stealth assassin, revealed by Light/proximity), Eclipse Cultist
  (curses defenders with Shadow), Fallen Templar (Light shield aura for allies). Maps now carry faction lists.
- Rift Warlock phase 2 adds "Màn Nhật Thực" curse zone (defenders inside take +20%).
- 6 Light/Shadow decrees with real hooks (Dawn Vigil, Eclipse Pact, Shadow Harvest, Radiant Walls, Umbral Sacrifice, Luminous Arsenal).
- Synergy quick-inspect in the building panel (reactions available with nearby allied elements); Light/Shadow onboarding hint.

## Phase I — Generals & Armies
- +10 Generals (Tharion, Aurelia, Nyxara, Isolde, Maren, Brannoc, Pyra, Corvin, Hilda, Maelis) → 16 total, 2 per element.
- Two in-run evolutions for all 16 Generals (chosen after night 6), changing ultimate behaviour/aura, not just stats.
- +6 soldier types with distinct behaviour: Shield Guardian (projectile interception), Crossbowman (armor pierce),
  Battlemage (melee + ranged spell), Elemental Knight (always elemental), Griffin Rider (flying, anti-air), Rune Golem (taunt/stun).
- Up to 2 AI Generals (slots from day 4 / day 9) with sector assignment; AutoPlayer General now path-finds.
- Balance pass from 384-run harness (see Balance_Spec).

## 2026-10-19 — MVP foundation
- Engine-free deterministic simulation (economy, map/navigation, combat + 21 reactions, AI, waves, bosses, decrees, saves).
- 8 building families × 3 tiers + final branch; 6 soldier classes × 2 branches; 6 Generals; 13 enemies + 3 bosses; 3 maps; 31 decrees.
- Unity layer: code bootstrap, sprite world view with interpolation/lights/VFX, IMGUI screens and HUD (Vietnamese), touch/PC/gamepad input, audio.
- Procedural art (424 sprites) and audio (25 clips) pipelines; headless harness, NUnit tests, snapshot renderer, CI workflow.
