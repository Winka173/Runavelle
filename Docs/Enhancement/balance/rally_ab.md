# Paired A/B: base vs rally

Pairs: **320** (unpaired rows ignored: 0 / 0). Policies are scripted bots, not humans.

| Metric | base | rally |
|---|---|---|
| Win rate (Wilson 95% CI) | 67.5% [62.2%–72.4%] | 68.1% [62.8%–73.0%] |
| Avg nights survived | 13.38 | 13.40 |
| Avg castle damage / run | 1239 | 1187 |

Paired win-rate difference (rally − base): **+0.6 pp**, bootstrap 95% CI [-4.1, +5.3] pp.
Discordant pairs: base-only wins 28, rally-only wins 30; exact McNemar p = 0.896.
Exceptions in rally: 0.

## By strategy

| strategy | n | base | rally | Δ |
|---|---|---|---|---|
| Army | 64 | 62.5% | 60.9% | -2 pp |
| Balanced | 64 | 68.8% | 70.3% | +2 pp |
| Economy | 64 | 73.4% | 73.4% | +0 pp |
| Elemental | 64 | 65.6% | 67.2% | +2 pp |
| Towers | 64 | 67.2% | 68.8% | +2 pp |

## By map

| map | n | base | rally | Δ |
|---|---|---|---|---|
| dawnspire | 40 | 25.0% | 40.0% | +15 pp |
| ember_highlands | 40 | 50.0% | 42.5% | -8 pp |
| frosthaven | 40 | 82.5% | 82.5% | +0 pp |
| moonstone_valley | 40 | 97.5% | 100.0% | +2 pp |
| shadowfen | 40 | 75.0% | 77.5% | +2 pp |
| shattered_crown | 40 | 47.5% | 47.5% | +0 pp |
| stormcliff | 40 | 90.0% | 90.0% | +0 pp |
| sunken_ruins | 40 | 72.5% | 65.0% | -8 pp |

## By general

| general | n | base | rally | Δ |
|---|---|---|---|---|
| lyra | 160 | 66.2% | 67.5% | +1 pp |
| roland | 160 | 68.8% | 68.8% | +0 pp |


## Method / reproduce
- Base = commit 9f87ae7 build, Change = rally posts (this commit). Same 320 setups: 8 maps × roland,lyra × 5 bot policies × 4 seeds (seed = 1000 + i·7919).
- `dotnet run --project Tools/Sim -c Release -- seeds=4 strategies=Balanced,Economy,Towers,Army,Elemental generals=roland,lyra [norally]` then `python3 Tools/Balance/ab_compare.py base.csv rally.csv`.
- Other harness metrics: soldier share of kills 16% → 20%, castle HP left (mean per night) 94% → 95%, nights with a breach 1660 → 1578.
- Wall time of the two 320-run batches (212 s vs 349 s) is NOT comparable: they ran under different container load. Controlled sequential re-run on one binary (`seeds=2 generals=roland` vs `… norally`): 10 s vs 9 s; friendly A* 0.07 → 0.10 calls/tick (perf dawnspire/roland/1000/8).
- Verdict: no significant win-rate change (McNemar p = 0.90); dawnspire +15 pp and ember/sunken −8 pp are within per-map noise at n = 40 and are tracked, not tuned.
