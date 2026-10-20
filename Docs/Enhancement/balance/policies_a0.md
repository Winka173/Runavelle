# Balance lab — policies (Ascension 0)

Runs: **672** (7 policies). Bots are scripted heuristics, not human players.

| Policy | n | Win rate [95% CI] | Avg nights | Castle dmg/run | Gold left | Mana left | Exceptions |
|---|---|---|---|---|---|---|---|
| GeneralCarry | 96 | 89% [81%–93%] | 14.6 | 664 | 694 | 137 | 0 |
| WallFirst | 96 | 81% [72%–88%] | 14.1 | 772 | 366 | 96 | 0 |
| GreedyEconomy | 96 | 81% [72%–88%] | 14.0 | 885 | 1266 | 297 | 0 |
| RandomPlay | 96 | 76% [67%–83%] | 13.9 | 1072 | 435 | 117 | 0 |
| Balanced | 96 | 76% [67%–83%] | 13.8 | 937 | 415 | 83 | 0 |
| Army | 96 | 69% [59%–77%] | 13.7 | 1154 | 314 | 123 | 0 |
| MageHeavy | 96 | 59% [49%–69%] | 13.6 | 1541 | 481 | 87 | 0 |

## Checks

- Dominance: best **GeneralCarry** 89% vs runner-up **WallFirst** 81% → gap 7.3 pp, CIs overlap → no dominant policy (envelope: gap ≤ 15 pp or overlapping CIs)
- Viability: 7 policies win ≥ 50% (GeneralCarry, WallFirst, GreedyEconomy, RandomPlay, Balanced, Army, MageHeavy) → OK
- Impossible setups (no policy wins): 1/96 — ember_highlands/roland/24757

## By map (all policies)

| Map | n | Win rate | Best policy | Worst policy |
|---|---|---|---|---|
| dawnspire | 84 | 52% | WallFirst 67% | Army 42% |
| ember_highlands | 84 | 56% | GreedyEconomy 75% | MageHeavy 33% |
| frosthaven | 84 | 85% | WallFirst 100% | MageHeavy 58% |
| moonstone_valley | 84 | 98% | MageHeavy 100% | RandomPlay 92% |
| shadowfen | 84 | 83% | RandomPlay 100% | MageHeavy 50% |
| shattered_crown | 84 | 58% | GeneralCarry 83% | MageHeavy 25% |
| stormcliff | 84 | 92% | WallFirst 100% | GreedyEconomy 75% |
| sunken_ruins | 84 | 83% | GreedyEconomy 100% | Balanced 75% |

## By general (all policies)

| General | n | Win rate |
|---|---|---|
| kaelen | 224 | 85% |
| lyra | 224 | 71% |
| roland | 224 | 72% |
