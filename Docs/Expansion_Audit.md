# Expansion Audit (baseline before Phase I)

Repository was empty at session start; the MVP below was built in this session first (commit history on `main`), then
used as the baseline for the five expansion phases. Counts are from the generated `Docs/Content_Catalog.md`.

| Area | Spec target (full expansion) | Baseline (MVP) | Delta to build |
|---|---|---|---|
| Economy | Gold + Mana only | Gold + Mana only (enum + tests) | – |
| Building families | exactly 8, 3 tiers, 1 final branch | 8 families, 102 levels, T3 A/B branches incl. Merchant Estate/Mana Dwelling, Deep/Resonance Well, Bastion/Rune Wall, Sniper/Multishot, Spire/Catalyst, Legion Hall/Elite Academy, Sanctuary/War Altar, Fortress/Arcane castle | polish only |
| Elements | exactly 8 | 8 (Light/Shadow statuses exist) | Phase II depth |
| Reactions | 18–22 bespoke | 21 bespoke + Resonance | Phase II verification/UX |
| Generals | 16, two evolutions each | 6, no evolutions | +10, +32 evolutions (Phase I) |
| Soldier types | 12, two branches each | 6 × 2 branches | +6 types (Phase I) |
| AI Generals | up to 2 | 2 slots implemented (days 4/9) | Phase I validation |
| Royal Decrees | 80–100 | 31 | +~60 (Phase III) |
| Kingdom evolution | Fortress/Legion/Arcanum | framework only | Phase III |
| Equipment | Weapon + Relic | framework only | Phase III |
| Maps | 8 | 3 | +5 with hazards (Phase IV) |
| Factions | 6 | 1 (Marauders) | +6 (Phase IV) |
| Enemy types | ~40 | 13 | +~27 (Phase IV) |
| Bosses | 10–12 | 3 | +8 (Phase IV) |
| Endless / Ascension / Mastery / Daily | all | Endless toggle + daily seed + mastery points (basic) | Phase V |

## Tooling
Unity Editor, Unity MCP, Blender, Blender MCP: **not available**. Alternatives: .NET compile/test/headless sim, Unity
reference-assembly compile-check, Skia art, numpy audio, snapshot renderer.
