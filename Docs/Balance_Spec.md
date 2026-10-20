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

## Measured: Phase I roster (2026-10-19)
Command: `dotnet run --project Tools/Sim -c Release -- seeds=8 generals=all` (8 seeds × 3 maps × 16 Generals = 384 runs; soldier
type and magic school rotate per seed, identical across Generals; bot General now path-finds around buildings).
Result: **162/384 wins (42%)**, 0 exceptions, 148 s. Per map: Moonstone 52%, Ember 41%, Sunken 33%.

| General | Win | Avg nights | | General | Win | Avg nights |
|---|---|---|---|---|---|---|
| Seraphine | 75% | 13.6 | | Pyra | 42% | 12.3 |
| Aurelia | 67% | 11.8 | | Isolde | 38% | 11.5 |
| Elowen | 54% | 12.3 | | Kaelen | 38% | 11.1 |
| Maelis | 50% | 10.6 | | Maren | 38% | 10.6 |
| Nyxara | 46% | 11.3 | | Brannoc | 33% | 10.5 |
| Varric | 46% | 11.0 | | Hilda | 33% | 9.7 |
| Lyra | 42% | 11.8 | | Roland | 33% | 10.5 |
| | | | | Tharion | 25% | 10.5 |
| | | | | Corvin | 17% | 8.8 |

Actions after this run: Seraphine aura regen 2.5%→1.8% and heal 35%→30%; Corvin damage 22→26, ultimate frenzy 30%→45%.
Earlier in Phase I, Tharion/Aurelia/Hilda/Isolde/Corvin/Brannoc were buffed after a first pass showed 12–21% wins.
Note: support Generals benefit from the bot's simple ultimate timing; human results will differ.

## Measured: Phase II (Shadow Court added, 2026-10-19)
Same command (`seeds=8 generals=all`, 384 runs): **136 wins (35%)**, 0 exceptions, 139 s. Per map: Moonstone 45%,
Ember 36%, Sunken 26%. Per General: Aurelia 67%, Seraphine/Varric 50%, Elowen/Nyxara 46% … Brannoc 21%. The new
Light/Shadow enemies (stealth assassin, cursing cultist, shield templar) made runs harder (42% → 35%) as intended.
Light/Shadow reactions observed in bot runs: Solar Flare, Eclipse, Haunted Arc, Fortify, Soul Frost.

## Measured: Phase III (Kingdom Evolution, 94 decrees, equipment)
`seeds=8 generals=all` (384 runs): **171 wins (45%)**, 0 exceptions, 165 s. Per map: Moonstone 51%, Ember 46%, Sunken 37%.
General spread narrowed to 29–58% (Elowen 58% … Maelis 29%). Bot picks a random Kingdom Evolution and the first
non-cursed decree; equipment is not used by the bot (loadout effects verified by tests instead).

## Measured: Phase IV (8 maps, 6 factions, 11 bosses)
`seeds=2 maps=all generals=all` (256 runs): **183 wins (71%)**, 0 exceptions, 193 s.
Per map: Shadowfen 100%, Moonstone 94%, Stormcliff 88%, Frosthaven 88%, Sunken 84%, Ember 53%, Shattered Crown 41%, Dawnspire 25%.
General spread 56–81% (Aurelia/Brannoc/Kaelen 81% … Seraphine 56%).
The jump from 45% is mostly the General-trap fix (houses no longer seal the General in; previously the bot General
sat idle for whole nights on many seeds). Tuning during the phase: Siege Titan 1350→1150 HP, armour 10→6, one boulder
per summon; Catapult unlock 5→7; early per-type cap (≤2 of a non-swarm type through night 4) and a flyer cap
(2 + 1.5×night). Open: Shadowfen's mist is too weak against the bot (100%) and Dawnspire is the hardest map —
both to be revisited with Ascension in Phase V.

## Measured: Phase V (Ascension, Endless, Challenges)
Ascension sweep, `seeds=4 maps=all` (96 runs per tier, Roland/Lyra/Kaelen), 0 exceptions:

| Ascension | 0 | 1 | 3 | 5 | 7 | 10 |
|---|---|---|---|---|---|---|
| Bot win rate | 63% | 52% | 43% | 47% | 29% | 14% |

Endless (`endless`, Moonstone/Frosthaven/Dawnspire, 18 runs): the bot falls between night 6 (Dawnspire) and 29;
Moonstone 21–29, Frosthaven 14–24. Before the per-night escalation (+15% HP, +8% damage, +6% troops) bots reached
night 44–56, so the escalation was added.
Challenges (`seeds=2`, Moonstone/Stormcliff/Sunken, 18 runs each): No Walls 89%, Arcane Only 89% (after the bot
learned to build magic towers when archers are banned; 22% before), Lone General 100%, Horde 100%, Pauper 78%,
Glass Castle 72%, Cursed Crown 94%, Blitz 72%. Lone General and Horde stayed at 100% even after a nerf — they are
easy for the bot; human difficulty unmeasured.

## Measured: Quality Enhancement pass (soldiers now deploy, staged waves, AI fixes)
`seeds=2 maps=all generals=all` (256 runs): **196 wins (77%)**, 0 exceptions, 216 s. Ascension sweep (96 runs each):
A3 53%, A6 57%, A10 26%. Metrics (all 256 runs): median first breach 48 s into a night, castle HP left 97% at night end
(won nights dominate), reactions 0.24/s, player General 36% of player damage; kill share: player General 41%,
Archer Towers 17%, Soldiers 14%, AI Generals 12%, Magic Towers 12%, hazards 2%. Last Bastion: 0 triggers because the
bot never drafts it (it picks the first non-cursed decree); behaviour covered by tests.
Note: the squad-deployment fix (soldiers used to stay inside their Barracks) makes armies matter; bot win rate moved
from 74% to 77% at Ascension 0.
