# Paired A/B: rally vs breach_keepengaged

Pairs: **320** (unpaired rows ignored: 0 / 0). Policies are scripted bots, not humans.

| Metric | rally | breach_keepengaged |
|---|---|---|
| Win rate (Wilson 95% CI) | 68.1% [62.8%–73.0%] | 67.8% [62.5%–72.7%] |
| Avg nights survived | 13.40 | 13.38 |
| Avg castle damage / run | 1187 | 1204 |

Paired win-rate difference (breach_keepengaged − rally): **-0.3 pp**, bootstrap 95% CI [-3.4, +2.8] pp.
Discordant pairs: rally-only wins 13, breach_keepengaged-only wins 12; exact McNemar p = 1.000.
Exceptions in breach_keepengaged: 0.

## By strategy

| strategy | n | rally | breach_keepengaged | Δ |
|---|---|---|---|---|
| Army | 64 | 60.9% | 60.9% | +0 pp |
| Balanced | 64 | 70.3% | 71.9% | +2 pp |
| Economy | 64 | 73.4% | 73.4% | +0 pp |
| Elemental | 64 | 67.2% | 64.1% | -3 pp |
| Towers | 64 | 68.8% | 68.8% | +0 pp |

## By map

| map | n | rally | breach_keepengaged | Δ |
|---|---|---|---|---|
| dawnspire | 40 | 40.0% | 40.0% | +0 pp |
| ember_highlands | 40 | 42.5% | 45.0% | +2 pp |
| frosthaven | 40 | 82.5% | 80.0% | -2 pp |
| moonstone_valley | 40 | 100.0% | 100.0% | +0 pp |
| shadowfen | 40 | 77.5% | 65.0% | -12 pp |
| shattered_crown | 40 | 47.5% | 50.0% | +2 pp |
| stormcliff | 40 | 90.0% | 90.0% | +0 pp |
| sunken_ruins | 40 | 65.0% | 72.5% | +8 pp |

## By general

| general | n | rally | breach_keepengaged | Δ |
|---|---|---|---|---|
| lyra | 160 | 67.5% | 67.5% | +0 pp |
| roland | 160 | 68.8% | 68.1% | -1 pp |


## Attribution & sensitivity (same 320 paired setups, base = rally build)
| Variant | Δ win rate vs rally | 95% CI | McNemar p |
|---|---|---|---|
| breach orders v1 (squads + generals) | −2.2 pp | [−5.6, +1.2] | 0.265 |
| `nobreach` on the same binary (Chronicle included) | 0.0 pp — all 320 rows identical | — | 1.000 |
| `tune.BreachPlugRatio=2.5` | −1.9 pp | [−5.3, +1.6] | 0.392 |
| always plug (`ratio=1000, minHp=0`) | −3.1 pp | [−5.9, −0.3] | 0.052 |
| squads only | −2.2 pp | [−5.6, +0.9] | 0.281 |
| generals only | −0.9 pp | [−3.1, +1.6] | 0.607 |
| **shipped: squads already fighting keep fighting (`BreachKeepEngaged`)** | **−0.3 pp** | **[−3.4, +2.8]** | **1.000** |

Reading: re-posting a squad that is mid-fight cost the bots ground; always plugging is the worst policy. The shipped rule keeps the
autonomous plug/fall-back for idle squads and generals with no measurable win-rate change. Chronicle has zero effect on the simulation.
Reproduce: `dotnet run --project Tools/Sim -c Release -- seeds=4 strategies=Balanced,Economy,Towers,Army,Elemental generals=roland,lyra [nobreach] [tune.<AiTuning field>=v]`.
