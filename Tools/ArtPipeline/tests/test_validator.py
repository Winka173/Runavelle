"""Self-tests for the Art Quality Validator: intentionally broken samples must trigger the expected findings.
Run: python3 -m unittest discover -s Tools/ArtPipeline/tests -v"""
import json
import os
import sys
import tempfile
import unittest

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import validator  # noqa: E402

SPEC = os.path.join(os.path.dirname(__file__), "..", "artspec.json")


def sprite(w=64, h=80, body=(20, 10, 44, 70), col=(180, 60, 50), outline=True):
    a = np.zeros((h, w, 4), np.uint8)
    x0, y0, x1, y1 = body
    a[y0:y1, x0:x1, :3] = col
    a[y0:y1, x0:x1, 3] = 255
    if outline:
        a[y0:y1, x0, :3] = 20; a[y0:y1, x1 - 1, :3] = 20; a[y0, x0:x1, :3] = 20; a[y1 - 1, x0:x1, :3] = 20
    return a


class ValidatorTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.art = self.tmp.name
        self.manifest = {}

    def tearDown(self):
        self.tmp.cleanup()

    def put(self, key, arr, ppu=64, px=0.5, py=None, listed=True, manifest_size=None):
        path = os.path.join(self.art, key + ".png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        Image.fromarray(arr, "RGBA").save(path)
        h, w = arr.shape[:2]
        if py is None:
            alpha = arr[..., 3] > 127
            foot = np.nonzero(alpha)[0].max() if alpha.any() else h - 1
            py = (h - 1 - foot) / h
        if listed:
            mw, mh = manifest_size or (w, h)
            self.manifest[key] = {"w": mw, "h": mh, "px": px, "py": round(py, 4), "ppu": ppu}

    def run_validator(self, waivers=None, code=None):
        with open(os.path.join(self.art, "manifest.json"), "w") as f:
            json.dump(self.manifest, f)
        wpath = None
        if waivers is not None:
            wpath = os.path.join(self.art, "waivers.json")
            with open(wpath, "w") as f:
                json.dump({"waivers": waivers}, f)
        _, findings, _ = validator.run(self.art, SPEC, wpath, os.path.join(self.art, "out"), code, write=False)
        return findings

    def checks(self, findings, key=None):
        return {(f["check"], f["severity"]) for f in findings if not f["waived"] and (key is None or f["asset"].startswith(key))}

    def test_good_sprite_is_clean(self):
        self.put("unit/good_0", sprite())
        self.put("unit/good_1", sprite(body=(21, 12, 45, 70)))
        self.assertEqual(set(), self.checks(self.run_validator()))

    def test_manifest_and_file_problems_block(self):
        self.put("unit/unlisted_0", sprite(), listed=False)
        self.manifest["unit/ghost_0"] = {"w": 64, "h": 80, "px": 0.5, "py": 0.1, "ppu": 64}
        self.put("unit/size_0", sprite(), manifest_size=(32, 32))
        self.put("unit/ppu_0", sprite(), ppu=100)
        c = self.checks(self.run_validator())
        for chk in ("unlisted_file", "missing_file", "manifest_size", "ppu"):
            self.assertIn((chk, "BLOCK"), c)

    def test_empty_and_pivot_outside_block(self):
        self.put("unit/empty_0", np.zeros((80, 64, 4), np.uint8), py=0.1)
        self.put("unit/pivot_0", sprite(), px=1.4)
        c = self.checks(self.run_validator())
        self.assertIn(("empty", "BLOCK"), c)
        self.assertIn(("pivot_bounds", "BLOCK"), c)

    def test_frames_must_match_and_be_contiguous(self):
        self.put("unit/dim_0", sprite())
        self.put("unit/dim_1", sprite(w=70))
        self.put("unit/gap_0", sprite())
        self.put("unit/gap_2", sprite())
        c = self.checks(self.run_validator())
        self.assertIn(("frame_consistency", "BLOCK"), c)
        self.assertIn(("frame_sequence", "BLOCK"), c)

    def test_identity_drift_between_frames_warns(self):
        self.put("unit/jump_0", sprite(body=(4, 10, 20, 70)))
        self.put("unit/jump_1", sprite(body=(40, 10, 60, 70)))
        self.assertIn(("frame_jump", "WARN"), self.checks(self.run_validator()))

    def test_clipped_halo_and_floating_feet_warn(self):
        self.put("unit/clip_0", sprite(body=(0, 0, 40, 70)))
        halo = sprite(col=(60, 40, 40))
        x0, y0, x1, y1 = 20, 10, 44, 70
        halo[y0 - 1, x0:x1] = (255, 255, 255, 90); halo[y1, x0:x1] = (255, 255, 255, 90)
        halo[y0:y1, x0 - 1] = (255, 255, 255, 90); halo[y0:y1, x1] = (255, 255, 255, 90)
        self.put("unit/halo_0", halo)
        self.put("unit/float_0", sprite(), py=0.6)
        c = self.checks(self.run_validator())
        self.assertIn(("clipped", "WARN"), c)
        self.assertIn(("halo", "WARN"), c)
        self.assertIn(("foot_pivot", "WARN"), c)

    def test_colour_blind_clash_blocks(self):
        badge_r = sprite(w=64, h=64, body=(8, 8, 56, 56), col=(200, 60, 40))
        badge_g = sprite(w=64, h=64, body=(8, 8, 56, 56), col=(170, 90, 40))
        self.put("icon/el_fire", badge_r, py=0.5)
        self.put("icon/el_earth", badge_g, py=0.5)
        self.assertTrue(any(c[0].startswith("cvd_") and c[1] == "BLOCK" for c in self.checks(self.run_validator())))

    def test_missing_code_reference_blocks(self):
        self.put("fx/spark", sprite(w=16, h=16, body=(2, 2, 14, 14)), py=0.5)
        code = os.path.join(self.art, "code")
        os.makedirs(code)
        with open(os.path.join(code, "A.cs"), "w") as f:
            f.write('var a = ArtLibrary.Get("fx/spark"); var b = ArtLibrary.Get("fx/missing_thing"); var c = "enemy/" + id;')
        c = self.checks(self.run_validator(code=code))
        self.assertIn(("missing_reference", "BLOCK"), c)
        self.assertEqual(1, sum(1 for x in c if x[0] == "missing_reference"))

    def test_waivers_apply_until_expiry(self):
        self.put("unit/clip_0", sprite(body=(0, 0, 40, 70)))
        f = self.run_validator(waivers=[{"asset": "unit/clip_*", "check": "clipped", "reason": "test", "expires": "2999-01-01"}])
        self.assertNotIn(("clipped", "WARN"), self.checks(f))
        f = self.run_validator(waivers=[{"asset": "unit/clip_*", "check": "clipped", "reason": "test", "expires": "2000-01-01"}])
        self.assertIn(("clipped", "WARN"), self.checks(f))


    # ---- Phase 3 fixtures (intentionally broken) -------------------------------------------------------------------
    def test_missing_attack_frame_blocks(self):
        self.put("enemy/lonely_0", sprite())
        self.assertIn(("missing_frame", "BLOCK"), self.checks(self.run_validator(), "enemy/lonely"))

    def test_identical_adjacent_frames_warn(self):
        self.put("unit/frozen_0", sprite())
        self.put("unit/frozen_1", sprite())
        self.assertIn(("identical_frames", "WARN"), self.checks(self.run_validator(), "unit/frozen"))

    def test_rgb_under_zero_alpha_warns(self):
        a = sprite()
        a[a[..., 3] == 0, :3] = 250  # bright colour hidden under fully transparent pixels
        self.put("unit/fringe_0", a)
        self.put("unit/fringe_1", sprite(body=(21, 12, 45, 70)))
        self.assertIn(("zero_alpha_rgb", "WARN"), self.checks(self.run_validator(), "unit/fringe_0"))

    def test_illegal_texture_dimension_blocks(self):
        self.put("fx/huge", np.zeros((2, 4100, 4), np.uint8) + np.array([200, 200, 200, 255], np.uint8), py=0.5)
        self.assertIn(("illegal_dim", "BLOCK"), self.checks(self.run_validator(), "fx/huge"))

    def test_broken_unity_meta_links_block(self):
        self.put("unit/a_0", sprite()); self.put("unit/a_1", sprite(body=(21, 12, 45, 70)))
        self.put("unit/b_0", sprite()); self.put("unit/b_1", sprite(body=(21, 12, 45, 70)))
        guid = "0123456789abcdef0123456789abcdef"
        for k in ("unit/a_0", "unit/a_1", "unit/b_0"):  # b_1 has no meta; a_1 duplicates a_0's GUID
            with open(os.path.join(self.art, k + ".png.meta"), "w") as f:
                f.write("fileFormatVersion: 2\nguid: " + (guid if k != "unit/b_0" else "f" * 32) + "\n")
        c = self.checks(self.run_validator())
        self.assertIn(("meta_missing", "BLOCK"), c)
        self.assertIn(("meta_duplicate_guid", "BLOCK"), c)

    def test_atlas_bleed_and_bounds_block(self):
        out = []
        validator.atlas_layout_check([("a", 2, 2, 10, 10, 0), ("b", 13, 2, 10, 10, 0), ("c", 0, 40, 10, 10, 0)], 64, 2, out)
        self.assertIn("atlas_bleed", {f["check"] for f in out})   # a and b only 1 px apart
        self.assertIn("atlas_bounds", {f["check"] for f in out})  # c touches the page edge
        import pack
        good = pack.layout_pack([("k%d" % i, 30 + i, 20 + i % 7, ) for i in range(40)], PAGE=256)
        out = []
        validator.atlas_layout_check(good, 256, pack.PAD, out)
        self.assertEqual([], [f for f in out if f["check"] == "atlas_bleed"], "the packer itself never bleeds")

if __name__ == "__main__":
    unittest.main()
