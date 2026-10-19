# Runavelle — Architecture

## Layers
```
Assets/Runavelle/Scripts/Core   (asmdef Runavelle.Core, noEngineReferences)  ← deterministic simulation, content, saves
Assets/Runavelle/Scripts/Game   (asmdef Runavelle.Game → Core)               ← Unity presentation: sprites, IMGUI HUD, input, audio
Assets/Runavelle/Scripts/Editor (asmdef Runavelle.Editor, Editor only)        ← import rules, project setup, checks, player builds
Tools/Core | Tools/Tests | Tools/Sim | Tools/UnityCheck                        ← .NET projects compiling the same .cs files outside Unity
Tools/Art (Python + Skia) | Tools/Audio (Python + numpy)                       ← regenerable original art & audio
```
The game boots from code (`Bootstrap` → `RuntimeInitializeOnLoadMethod`), so no scene/prefab GUID references
exist that could break. `Runavelle > 1. Setup project` creates `Assets/Runavelle/Scenes/Main.unity` for builds.
Tech choices mirror Arcane-Brigade: Unity 6000.4.8f1, built-in renderer + SpriteRenderer, IMGUI UI with
Be Vietnam Pro fonts, legacy Input Manager, Resources-loaded generated art, Editor menus for checks/builds.

## Simulation (Core)
- **Fixed tick** 20 Hz (`World.Dt = 0.05`). Presentation interpolates `PrevPosition → Position`. 1×/2× speed
  changes how many ticks run per frame, never the tick itself → identical results at any speed.
- **Determinism**: seeded `Rng` (xoshiro128**, derived streams per subsystem), id-ordered tie-breaks, float math.
  Guarantee: reproducible on the same build/architecture (tested: same seed + same commands ⇒ identical state).
  Cross-platform bit-identical float results are *not* claimed.
- **World** (state) + **systems** (logic): `NavigationService` (flow fields with breach costs + friendly A*),
  `CombatSystem` (single damage/status/reaction pipeline, attribution), `AuraSystem` (4 Hz aura recompute, DoT,
  regen), `EffectsSystem` (attacks, projectiles, zones, telegraphs), `EnemyAI`, `DefenderAI` (soldiers + towers),
  `GeneralSystem` (player + AI Generals, ultimates), `BossSystem` (data-driven phases), `WaveDirector`
  (threat budgets, seeded fronts, previews), `Spawner` (effective stats from content + `RunModifiers`).
- **GameSession** is the only mutation API (place/drag walls/upgrade/imbue/sell/undo/rebuild/start night/
  decrees/evolutions/AI sectors). Bots, tests, the headless harness and the Unity HUD all use it.
- **Navigation policy**: enemy flow fields are rebuilt lazily only when `TopologyVersion` changes (build,
  destroy, repair, temporary barriers, tide). Structures are traversable at a breach cost, so sealed castles get
  attacked through the cheapest wall instead of freezing enemies. Gates: friendlies pass, enemies must break them.
- **Reactions**: aura gauge model; a different element on an aura ≥10 triggers a reaction (21 bespoke pairs +
  generic Resonance), consumes gauge, per-target ICD, global budget 24/tick, reaction damage has
  `Generation>0` and can never trigger further reactions.

## Content
Data tables in `Core/Content/*Content.cs` (stable string IDs, `IdMigrations` for renames) validated at startup
and in tests (8 families, 8 elements, no full immunity, tier ≤3, references). `Docs/Content_Catalog.md` is generated.

## Persistence
`SaveStore` (atomic temp+move, `.bak`, FNV checksum). `RunSave` v1 snapshots at day boundaries / choice screens
(content IDs only) with migration from v0. `Profile` holds unlocks, mastery, history, settings.

## Verification without the Editor
- `dotnet test Tools/Tests` – EditMode-equivalent tests.
- `dotnet run --project Tools/Sim -c Release -- seeds=8` – headless balance runs with the `AutoPlayer` bot.
- `dotnet build Tools/UnityCheck` – compiles Game+Editor against Unity reference assemblies.
- `python3 Tools/Art/render_snapshot.py` – renders real simulation snapshots with the generated sprites
  (`Docs/Previews/scene_*.png`) – a faithful layout preview, **not** a Unity capture.
