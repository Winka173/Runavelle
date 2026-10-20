"""Paired-seed A/B comparison of two harness runs (Tools/Sim CSV output).

    python3 Tools/Balance/ab_compare.py A.csv B.csv [--out report.md] [--label-a base --label-b change]

Runs are paired by (map, general, strategy, seed), so the only difference between a pair is the change under test.
Reports win rate per side, the paired difference with a bootstrap 95% CI, an exact McNemar test on discordant pairs,
and per-strategy / per-map breakdowns. Sample sizes are printed next to every number; automated policies are
not human players, so treat results as regression signals, not player experience.
"""
import argparse
import csv
import math
import random
import sys
from collections import defaultdict

COLS = "map,general,strategy,seed,result,nights,kills,castle_dmg_total,reactions,peak_enemies,max_rx_tick,gold_end,mana_end,seconds,exception".split(",")


def load(path):
    rows = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            parts = line.rstrip("\n").split(",")
            if len(parts) < len(COLS) or parts[4] not in ("WIN", "LOSS"):
                continue
            r = dict(zip(COLS, parts[:len(COLS)]))
            rows[(r["map"], r["general"], r["strategy"], r["seed"])] = r
    return rows


def mcnemar_exact(b, c):
    """Two-sided exact binomial p-value on discordant pairs (b: A win/B loss, c: A loss/B win)."""
    n = b + c
    if n == 0:
        return 1.0
    k = min(b, c)
    p = sum(math.comb(n, i) for i in range(0, k + 1)) / 2 ** n
    return min(1.0, 2 * p)


def bootstrap_ci(diffs, iters=4000, seed=1):
    if not diffs:
        return (0.0, 0.0)
    rng = random.Random(seed)
    n = len(diffs)
    means = sorted(sum(diffs[rng.randrange(n)] for _ in range(n)) / n for _ in range(iters))
    return (means[int(0.025 * iters)], means[int(0.975 * iters) - 1])


def wilson(k, n, z=1.96):
    if n == 0:
        return (0.0, 0.0)
    p = k / n
    d = 1 + z * z / n
    c = p + z * z / (2 * n)
    r = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n))
    return ((c - r) / d, (c + r) / d)


def compare(a, b):
    keys = sorted(set(a) & set(b))
    win = lambda r: 1 if r["result"] == "WIN" else 0
    out = {"pairs": len(keys), "only_a": len(set(a) - set(b)), "only_b": len(set(b) - set(a))}
    wa = sum(win(a[k]) for k in keys)
    wb = sum(win(b[k]) for k in keys)
    disc_b = sum(1 for k in keys if win(a[k]) and not win(b[k]))
    disc_c = sum(1 for k in keys if not win(a[k]) and win(b[k]))
    diffs = [win(b[k]) - win(a[k]) for k in keys]
    out.update(wins_a=wa, wins_b=wb, a_only_wins=disc_b, b_only_wins=disc_c, p_mcnemar=mcnemar_exact(disc_b, disc_c),
               diff=(wb - wa) / max(1, len(keys)), ci=bootstrap_ci(diffs),
               nights_a=sum(int(a[k]["nights"]) for k in keys) / max(1, len(keys)),
               nights_b=sum(int(b[k]["nights"]) for k in keys) / max(1, len(keys)),
               castle_a=sum(float(a[k]["castle_dmg_total"]) for k in keys) / max(1, len(keys)),
               castle_b=sum(float(b[k]["castle_dmg_total"]) for k in keys) / max(1, len(keys)),
               exceptions_b=sum(1 for k in keys if b[k]["exception"]))
    groups = {}
    for dim, idx in (("strategy", 2), ("map", 0), ("general", 1)):
        g = defaultdict(list)
        for k in keys:
            g[k[idx]].append(k)
        groups[dim] = {name: (len(ks), sum(win(a[k]) for k in ks), sum(win(b[k]) for k in ks)) for name, ks in sorted(g.items())}
    out["groups"] = groups
    return out


def report(r, la, lb):
    n = r["pairs"]
    pct = lambda x: f"{100 * x:.1f}%"
    lo_a, hi_a = wilson(r["wins_a"], n)
    lo_b, hi_b = wilson(r["wins_b"], n)
    lines = [f"# Paired A/B: {la} vs {lb}", "",
             f"Pairs: **{n}** (unpaired rows ignored: {r['only_a']} / {r['only_b']}). Policies are scripted bots, not humans.", "",
             "| Metric | " + la + " | " + lb + " |", "|---|---|---|",
             f"| Win rate (Wilson 95% CI) | {pct(r['wins_a'] / max(1, n))} [{pct(lo_a)}–{pct(hi_a)}] | {pct(r['wins_b'] / max(1, n))} [{pct(lo_b)}–{pct(hi_b)}] |",
             f"| Avg nights survived | {r['nights_a']:.2f} | {r['nights_b']:.2f} |",
             f"| Avg castle damage / run | {r['castle_a']:.0f} | {r['castle_b']:.0f} |", "",
             f"Paired win-rate difference ({lb} − {la}): **{100 * r['diff']:+.1f} pp**, bootstrap 95% CI [{100 * r['ci'][0]:+.1f}, {100 * r['ci'][1]:+.1f}] pp.",
             f"Discordant pairs: {la}-only wins {r['a_only_wins']}, {lb}-only wins {r['b_only_wins']}; exact McNemar p = {r['p_mcnemar']:.3f}.",
             f"Exceptions in {lb}: {r['exceptions_b']}.", ""]
    for dim, g in r["groups"].items():
        lines += [f"## By {dim}", "", f"| {dim} | n | {la} | {lb} | Δ |", "|---|---|---|---|---|"]
        for name, (cnt, wa, wb) in g.items():
            lines.append(f"| {name} | {cnt} | {pct(wa / cnt)} | {pct(wb / cnt)} | {100 * (wb - wa) / cnt:+.0f} pp |")
        lines.append("")
    return "\n".join(lines)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("a")
    ap.add_argument("b")
    ap.add_argument("--out")
    ap.add_argument("--label-a", default="A")
    ap.add_argument("--label-b", default="B")
    args = ap.parse_args(argv)
    r = compare(load(args.a), load(args.b))
    md = report(r, args.label_a, args.label_b)
    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            f.write(md + "\n")
    print(md)
    return 0 if r["pairs"] else 1


if __name__ == "__main__":
    sys.exit(main())
