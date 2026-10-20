# Balance lab — policies (Ascension 10)

Runs: **336** (7 policies). Bots are scripted heuristics, not human players.

| Policy | n | Win rate [95% CI] | Avg nights | Castle dmg/run | Gold left | Mana left | Exceptions |
|---|---|---|---|---|---|---|---|
| GeneralCarry | 48 | 44% [31%–58%] | 12.4 | 1847 | 609 | 79 | 0 |
| Balanced | 48 | 44% [31%–58%] | 12.1 | 1801 | 635 | 118 | 0 |
| Army | 48 | 44% [31%–58%] | 12.8 | 2048 | 632 | 143 | 0 |
| WallFirst | 48 | 33% [22%–47%] | 12.1 | 2106 | 487 | 107 | 0 |
| GreedyEconomy | 48 | 23% [13%–37%] | 9.3 | 2107 | 602 | 248 | 0 |
| RandomPlay | 48 | 19% [10%–32%] | 9.2 | 2173 | 334 | 95 | 0 |
| MageHeavy | 48 | 10% [5%–22%] | 9.2 | 2104 | 385 | 135 | 0 |

## Checks

- Dominance: best **GeneralCarry** 44% vs runner-up **Balanced** 44% → gap 0.0 pp, CIs overlap → no dominant policy (envelope: gap ≤ 15 pp or overlapping CIs)
- Viability: 0 policies win ≥ 50% () → **WARNING: fewer than 3 viable policies**
- Impossible setups (no policy wins): 18/48 — dawnspire/kaelen/1000, dawnspire/lyra/1000, dawnspire/lyra/8919, dawnspire/roland/1000, dawnspire/roland/8919, ember_highlands/kaelen/1000, ember_highlands/kaelen/8919, ember_highlands/lyra/1000, ember_highlands/roland/1000, ember_highlands/roland/8919, frosthaven/lyra/8919, frosthaven/roland/8919

## By map (all policies)

| Map | n | Win rate | Best policy | Worst policy |
|---|---|---|---|---|
| dawnspire | 42 | 2% | GeneralCarry 17% | Army 0% |
| ember_highlands | 42 | 7% | WallFirst 17% | Army 0% |
| frosthaven | 42 | 36% | Balanced 67% | RandomPlay 0% |
| moonstone_valley | 42 | 64% | WallFirst 83% | MageHeavy 17% |
| shadowfen | 42 | 26% | RandomPlay 50% | GreedyEconomy 0% |
| shattered_crown | 42 | 14% | Army 33% | RandomPlay 0% |
| stormcliff | 42 | 36% | Army 83% | MageHeavy 0% |
| sunken_ruins | 42 | 62% | GeneralCarry 100% | MageHeavy 0% |

## By general (all policies)

| General | n | Win rate |
|---|---|---|
| kaelen | 112 | 38% |
| lyra | 112 | 30% |
| roland | 112 | 24% |
