# Balance Spec & measured results

All numbers are **starting values** tuned only through the headless harness (`Tools/Sim`, real shared simulation,
`AutoPlayer` heuristic bot). The bot is a weak, deterministic player (no wall micro-placement, simple General
pathing); human win rates are expected to differ. No numbers below are invented: each table states its command.

## Economy (Gold / Mana)
| Item | Cost | Yield | Break-even |
|---|---|---|---|
| Start | 140 Gold, 30 Mana | – | – |
| Night reward | – | (24 + 4·night) Gold, (8 + night) Mana | – |
| Cottage (House T1) | 40g | 12g/dawn | 3.3 days |
| Manor (T2) | +60g | 22g/dawn | – |
| Mana Well T1 | 35g | 9m/dawn | – |
| Reinforce soldier | 4g each | – | – |
| Imbue primary / secondary | 10g+25m / 10g+40m | – | – |

## Threat
`budget(n) = 7 + 4.2n + 1.1·n^1.55` (×0.75 on boss nights), HP scaling `1 + 0.055(n−1)`, new enemy types capped at 2
(swarm 6) on their intro night, siege units capped early. Enrage (+50% speed) after `240 + 6n` seconds to avoid stalls.

## Measured: MVP baseline (2026-10-19)
Command: `dotnet run --project Tools/Sim -c Release -- seeds=8` (8 seeds × 3 maps × 3 Generals, Balanced bot, asc 0)

| Map | Roland | Lyra | Kaelen |
|---|---|---|---|
| Moonstone Valley (★) | 38% (avg 8.5 nights) | 75% (12.3) | 63% (12.1) |
| Sunken Ruins (★★) | 13% (7.8) | 25% (9.0) | 13% (9.8) |
| Ember Highlands (★★★) | 13% (10.0) | 13% (10.0) | 0% (9.9) |

Total 72 runs: **20 wins (28%)**, 0 exceptions, wall time 17.4 s (~0.24 s per 15-night run).
Interpretation: difficulty ordering of maps matches design; Roland (melee) underperforms with the bot's simple
General control. Flagged for the Phase I balance pass.
