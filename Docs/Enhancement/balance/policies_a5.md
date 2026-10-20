# Balance lab — policies (Ascension 5)

Runs: **336** (7 policies). Bots are scripted heuristics, not human players.

| Policy | n | Win rate [95% CI] | Avg nights | Castle dmg/run | Gold left | Mana left | Exceptions |
|---|---|---|---|---|---|---|---|
| GeneralCarry | 48 | 83% [70%–91%] | 14.5 | 1134 | 877 | 101 | 0 |
| WallFirst | 48 | 69% [55%–80%] | 13.4 | 1359 | 353 | 86 | 0 |
| Army | 48 | 69% [55%–80%] | 14.0 | 1442 | 362 | 167 | 0 |
| Balanced | 48 | 65% [50%–77%] | 13.5 | 1264 | 412 | 108 | 0 |
| RandomPlay | 48 | 60% [46%–73%] | 11.8 | 1558 | 346 | 99 | 0 |
| GreedyEconomy | 48 | 60% [46%–73%] | 13.0 | 1479 | 1004 | 279 | 0 |
| MageHeavy | 48 | 38% [25%–52%] | 12.2 | 2337 | 360 | 58 | 0 |

## Checks

- Dominance: best **GeneralCarry** 83% vs runner-up **WallFirst** 69% → gap 14.6 pp, CIs overlap → no dominant policy (envelope: gap ≤ 15 pp or overlapping CIs)
- Viability: 6 policies win ≥ 50% (GeneralCarry, WallFirst, Army, Balanced, RandomPlay, GreedyEconomy) → OK
- Impossible setups (no policy wins): 1/48 — ember_highlands/roland/1000

## By map (all policies)

| Map | n | Win rate | Best policy | Worst policy |
|---|---|---|---|---|
| dawnspire | 42 | 50% | GreedyEconomy 83% | MageHeavy 0% |
| ember_highlands | 42 | 45% | WallFirst 67% | MageHeavy 17% |
| frosthaven | 42 | 74% | GeneralCarry 100% | MageHeavy 50% |
| moonstone_valley | 42 | 88% | WallFirst 100% | Army 67% |
| shadowfen | 42 | 76% | GeneralCarry 100% | MageHeavy 33% |
| shattered_crown | 42 | 29% | GeneralCarry 83% | Army 0% |
| stormcliff | 42 | 57% | GreedyEconomy 67% | Balanced 50% |
| sunken_ruins | 42 | 88% | WallFirst 100% | MageHeavy 50% |

## By general (all policies)

| General | n | Win rate |
|---|---|---|
| kaelen | 112 | 71% |
| lyra | 112 | 62% |
| roland | 112 | 57% |
