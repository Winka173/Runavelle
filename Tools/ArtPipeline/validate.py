"""Validate the shipped art (game folder) or staging.

    python3 Tools/ArtPipeline/validate.py [--strict] [--staging]

Writes reports/art_quality_validation.json (+ md/html/csv under reports/art_validation_current/).
Exit 1 on any unwaived BLOCK; with --strict also on any unwaived WARN.
"""
import argparse
import json
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
import validator  # noqa: E402


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--strict", action="store_true")
    ap.add_argument("--staging", action="store_true")
    a = ap.parse_args(argv)
    art = os.path.join(ROOT, "Build", "art_staging") if a.staging else os.path.join(ROOT, "Assets", "Runavelle", "Resources", "Art")
    out = os.path.join(ROOT, "reports", "art_validation_" + ("staging" if a.staging else "current"))
    summary, findings, _ = validator.run(art, os.path.join(HERE, "artspec.json"), os.path.join(HERE, "waivers.json"), out,
                                         None if a.staging else os.path.join(ROOT, "Assets", "Runavelle", "Scripts"))
    os.makedirs(os.path.join(ROOT, "reports"), exist_ok=True)
    shutil.copy2(os.path.join(out, "report.json"), os.path.join(ROOT, "reports", "art_quality_validation.json"))
    print(f"validate {'staging' if a.staging else 'game'}: assets={summary['assets']} BLOCK={summary['block']} WARN={summary['warn']} "
          f"waived={summary['waived']} RGBA32={summary['texture_mb_rgba32']}MB unity_meta={summary.get('unity_meta')}")
    if summary["block"]:
        return 1
    return 1 if a.strict and summary["warn"] else 0


if __name__ == "__main__":
    sys.exit(main())
