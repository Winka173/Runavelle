import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import ab_compare as ab  # noqa: E402

HDR = "map,general,strategy,seed,result,nights,kills,castle_dmg_total,reactions,peak_enemies,max_rx_tick,gold_end,mana_end,seconds,exception\n"


def write(rows):
    f = tempfile.NamedTemporaryFile("w", suffix=".csv", delete=False)
    f.write("header line\n" + HDR)
    for i, res in enumerate(rows):
        f.write(f"m,g,S,{i},{res},{15 if res == 'WIN' else 9},1,100,1,1,1,1,1,0.1,\n")
    f.write("summary,ignored\n")
    f.close()
    return f.name


class AbCompareTests(unittest.TestCase):
    def test_mcnemar_exact(self):
        self.assertAlmostEqual(ab.mcnemar_exact(0, 6), 2 / 64)
        self.assertEqual(ab.mcnemar_exact(3, 3), 1.0)
        self.assertEqual(ab.mcnemar_exact(0, 0), 1.0)

    def test_pairs_and_difference(self):
        a = write(["WIN", "LOSS", "LOSS", "WIN"])
        b = write(["WIN", "WIN", "LOSS", "LOSS"])
        r = ab.compare(ab.load(a), ab.load(b))
        self.assertEqual(r["pairs"], 4)
        self.assertEqual((r["a_only_wins"], r["b_only_wins"]), (1, 1))
        self.assertEqual(r["diff"], 0.0)
        lo, hi = r["ci"]
        self.assertLessEqual(lo, 0.0)
        self.assertGreaterEqual(hi, 0.0)

    def test_wilson_bounds(self):
        lo, hi = ab.wilson(5, 10)
        self.assertTrue(0.18 < lo < 0.5 < hi < 0.82)


if __name__ == "__main__":
    unittest.main()
