# Changelog

## Phase IV — Siegefronts (maps, factions, enemies, bosses)
- 6 factions with real rosters: Marauders, Shadow Court, Undead Legion, Siege Constructs, Ashborn, Frostbound, Wild Tempest
  (+28 enemies → 43 regular enemies). New mechanics: self-exploding Bomber (no bounty), Zombie "undying" (rises once
  unless killed by Fire/Light), Frost Wraith freezes towers, Glacier Golem chills melee attackers, catapult min range.
- +8 multi-phase bosses (11 total): Siege Titan, Storm Serpent (flying), Void Commander, Frost Queen (3 phases),
  Corrupted King (3), Bone Colossus, Tempest Roc (flying), Hollow Emperor (3, combines summon/possess/freeze/meteor/revive).
  Each map has its own night 5/10/15 boss rotation.
- 8 maps: + Stormcliff (lightning strikes both sides), Frosthaven (Fire cracks thin ice → water until dawn),
  Shadowfen (mist: ground enemies arrive stealthed), Dawnspire (plateau ringed by cliffs, +1 tower range),
  Shattered Crown (collapsed rampart, ruined gates rebuild cheaply); Sunken Ruins gets Tide (shallows flood on even
  nights), Ember Highlands gets Lava Vents. Map unlock night capped at 15.
- Nightly side objective (castle safe / ≤2 losses / swift / no ruins / General never downed) with dawn reward, shown in
  the forecast and dawn report. Hazard note in the day forecast; tide/cracked-ice/mist overlays and hazard VFX.
- Fixes: houses/towers can no longer seal the General's exit (placement rule) — this trap made the bot General idle
  for whole nights; AI Generals no longer chase fleeing skirmishers out of their sector; stealthed enemies reveal
  when they attack; thin faction pools are topped up with Marauders; early per-type and air caps in wave planning.
- Tools: `Runavelle.Sim plan <map> [seed]` prints wave composition per night.

## Phase III — Kingdom Evolution, Decrees, Equipment
- Kingdom Evolution after night 7: Fortress (stronger/thorned walls, free wall rebuild), Legion (+1 soldier, night
  reinforcements, free reinforcement, stronger auras), Arcanum (mana economy, magic-tower secondary slot from T2,
  reaction mana). Shown as a castle banner + ground aura.
- Royal Decrees 37 → 94 (Economy/Fortress/Army/General/Elemental/Cursed), 25+ new mechanics hooks, mutual exclusions.
- General equipment: 8 weapons (element secondary or attack profile) + 8 relics; chosen before the run and between nights.
- Real-time siege warnings (flyers, burrowers, stealth, siege, elites, mana drainers) once per type per night.

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
