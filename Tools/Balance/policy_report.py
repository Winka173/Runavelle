"""Balance-lab policy report over harness CSVs (Tools/Sim output).

    python3 Tools/Balance/policy_report.py Build/lab/a0_*.csv --out Docs/Enhancement/balance/policies_a0.md [--label "Ascension 0"]

Per policy: win rate with Wilson 95% CI, nights survived, castle damage, gold/mana left (starvation vs. hoarding).
Checks (proposal envelopes from Docs/Enhancement/BALANCE_SPEC.md, not facts):
  - dominance: the best policy must not beat the runner-up by more than DOMINANCE_PP with non-overlapping CIs;
  - viability: at least MIN_VIABLE policies win >= VIABLE_RATE of their runs;
  - impossible setups: (map, general, seed) that no policy wins;
  - per-map spread and the weakest map.
Scripted bots are not humans: these are regression signals for tuning, not player-experience claims.
"""
import argparse
import math
import sys
from collections import defaultdict

sys.path.insert(0, __import__("os").path.dirname(__file__))
from ab_compare import load, wilson  # noqa: E402

DOMINANCE_PP = 15.0
VIABLE_RATE = 0.5
MIN_VIABLE = 3


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("csv", nargs="+")
    ap.add_argument("--out")
    ap.add_argument("--label", default="")
    a = ap.parse_args(argv)
    rows = {}
    for p in a.csv:
        rows.update(load(p))
    by = defaultdict(list)
    for k, r in rows.items():
        by[r["strategy"]].append(r)
    win = lambda r: r["result"] == "WIN"
    stats = []
    for pol, rs in by.items():
        n, w = len(rs), sum(win(r) for r in rs)
        lo, hi = wilson(w, n)
        stats.append((w / n, pol, n, w, lo, hi,
                      sum(int(r["nights"]) for r in rs) / n,
                      sum(float(r["castle_dmg_total"]) for r in rs) / n,
                      sum(int(r["gold_end"] or 0) for r in rs) / n,
                      sum(int(r["mana_end"] or 0) for r in rs) / n,
                      sum(1 for r in rs if r["exception"])))
    stats.sort(reverse=True)
    L = [f"# Balance lab — policies {a.label}", "", f"Runs: **{len(rows)}** ({len(by)} policies). Bots are scripted heuristics, not human players.", "",
         "| Policy | n | Win rate [95% CI] | Avg nights | Castle dmg/run | Gold left | Mana left | Exceptions |", "|---|---|---|---|---|---|---|---|"]
    for wr, pol, n, w, lo, hi, nights, cd, g, m, ex in stats:
        L.append(f"| {pol} | {n} | {wr:.0%} [{lo:.0%}–{hi:.0%}] | {nights:.1f} | {cd:.0f} | {g:.0f} | {m:.0f} | {ex} |")
    best, second = stats[0], stats[1]
    gap = (best[0] - second[0]) * 100
    overlap = best[4] <= second[5]
    dominant = gap > DOMINANCE_PP and not overlap
    viable = [s[1] for s in stats if s[0] >= VIABLE_RATE]
    L += ["", "## Checks", "",
          f"- Dominance: best **{best[1]}** {best[0]:.0%} vs runner-up **{second[1]}** {second[0]:.0%} → gap {gap:.1f} pp, CIs {'overlap' if overlap else 'do NOT overlap'} → "
          + ("**WARNING: dominant policy**" if dominant else "no dominant policy (envelope: gap ≤ 15 pp or overlapping CIs)"),
          f"- Viability: {len(viable)} policies win ≥ {VIABLE_RATE:.0%} ({', '.join(viable)}) → " + ("OK" if len(viable) >= MIN_VIABLE else f"**WARNING: fewer than {MIN_VIABLE} viable policies**")]
    setups = defaultdict(list)
    for r in rows.values():
        setups[(r["map"], r["general"], r["seed"])].append(win(r))
    impossible = sorted(k for k, v in setups.items() if not any(v))
    L.append(f"- Impossible setups (no policy wins): {len(impossible)}/{len(setups)}" + (f" — {', '.join('/'.join(x) for x in impossible[:12])}" if impossible else ""))
    L += ["", "## By map (all policies)", "", "| Map | n | Win rate | Best policy | Worst policy |", "|---|---|---|---|---|"]
    bm = defaultdict(lambda: defaultdict(list))
    for r in rows.values():
        bm[r["map"]][r["strategy"]].append(win(r))
    for mp in sorted(bm):
        allr = [x for v in bm[mp].values() for x in v]
        per = sorted(((sum(v) / len(v), p) for p, v in bm[mp].items()), reverse=True)
        L.append(f"| {mp} | {len(allr)} | {sum(allr) / len(allr):.0%} | {per[0][1]} {per[0][0]:.0%} | {per[-1][1]} {per[-1][0]:.0%} |")
    L += ["", "## By general (all policies)", "", "| General | n | Win rate |", "|---|---|---|"]
    bg = defaultdict(list)
    for r in rows.values():
        bg[r["general"]].append(win(r))
    for g, v in sorted(bg.items()):
        L.append(f"| {g} | {len(v)} | {sum(v) / len(v):.0%} |")
    md = "\n".join(L) + "\n"
    if a.out:
        open(a.out, "w", encoding="utf-8").write(md)
    print(md)
    return 0


if __name__ == "__main__":
    sys.exit(main())
