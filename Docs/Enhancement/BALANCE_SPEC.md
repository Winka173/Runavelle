# Balance spec — design envelopes (PROPOSALS, not facts)

Bots are not humans; these envelopes are targets for the automated lab, used to raise warnings, not to auto-tune.
| Metric | Envelope (proposal) |
|---|---|
| Bot win rate, Ascension 0, per map | 45–90% (first maps easier) |
| Share of wins by any single policy | ≤ 45% of all policy wins on a map (no dominant strategy) |
| Policies with ≥ 30% win rate at A0 | ≥ 4 of 7 |
| "Impossible seed" rate (all policies lose) at A0 | ≤ 5% |
| Gold / Mana at night 15 | no unbounded growth (> 3000 Gold flags a possible exploit) |
| Forecast accuracy | 100% of spawned unit types appear in the preview (hard test) |
| Reaction chains | ≤ 24 reactions per tick (hard cap, test) |
Current measurements: `Docs/Balance_Spec.md`; lab outputs: `BALANCE_REPORT.md` (P8).

## Balance lab results (P8) — measured, bots are not humans
Reports: `balance/policies_a0.md` (672 runs: 8 maps × roland/lyra/kaelen × 7 policies × 4 seeds), `balance/policies_a5.md`, `balance/policies_a10.md` (336 runs each).
Reproduce: `dotnet run --project Tools/Sim -c Release -- generals=roland,lyra,kaelen seeds=4 strategies=Balanced,GreedyEconomy,WallFirst,Army,MageHeavy,GeneralCarry,RandomPlay [ascension=N]` → `python3 Tools/Balance/policy_report.py <csvs>`.

| Ascension | Best policy | Runner-up | Dominant? | Viable (≥50%) | Setups no policy wins |
|---|---|---|---|---|---|
| 0 | GeneralCarry 89% | WallFirst 81% | no (gap 7 pp, CIs overlap) | 7/7 | 1/96 |
| 5 | GeneralCarry 83% | WallFirst/Army 69% | no (gap 15 pp, CIs overlap) | 6/7 | 1/48 |
| 10 | GeneralCarry/Balanced/Army 44% | — | no | 0/7 (**envelope miss**) | 18/48 |

Findings (tracked, not auto-tuned — no large global nerf without evidence/regression review):
1. **MageHeavy is the weakest policy at every level** (59% / 38% / 10%) and takes the most castle damage. Candidate causes to test with paired A/B: mage squads' survivability, lightning school choice, mana starvation (only 58–135 mana left).
2. **GeneralCarry is always top**: a lean kingdom that commissions AI generals early wins most. Consistent with the PlayerGeneral kill share (~44%). Watch AI-general cost/power before adding power elsewhere.
3. **Kaelen 85% vs Lyra 71% / Roland 72%** at A0.
4. **Ascension 10 misses the viability envelope** (no policy ≥ 50%; dawnspire/ember_highlands account for most impossible setups). A10 is the top difficulty, so this may be intended; it needs a design decision, not an automatic fix.
5. GreedyEconomy finishes with ~1000–1266 unspent gold: bot spending limit, not proof of an economy problem.
6. WallFirst with swordsman + fire defaults loses both tutorial seeds at the night-15 siege boss (gold sunk into bastion walls); with the lab's rotating soldier/school it wins ≥ 92% there.

Exploits: `PolicyTests.NoDaytimeActionSequenceCreatesGoldOrMana` fuzzes 3000 random place/upgrade/sell/undo/imbue/rebuild operations; "cash + resale value of every building" never increases. `EconomyCannotBeExploitedThroughBuildSellUndoLoops` (existing) still passes.
Knobs: every `AiTuning` field can be swept from the command line (`tune.<Field>=value`); the breach study in `balance/breach_ab.md` is an example sensitivity run.
