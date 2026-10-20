"""Regenerate every Runavelle sprite: python3 Tools/Art/build_art.py [--sheet]
Outputs PNGs + manifest.json into Assets/Runavelle/Resources/Art (pivots/ppu for Unity)."""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import artlib  # noqa: E402
import terrain_buildings  # noqa: E402

MODULES = [terrain_buildings]
for extra in ("units", "units_phase4", "ui_fx", "fx_feel"):
    try:
        MODULES.append(__import__(extra))
    except ImportError:
        pass

if __name__ == "__main__":
    for m in MODULES:
        m.run()
    p = artlib.write_manifest()
    print(f"{len(artlib.MANIFEST)} sprites -> {os.path.dirname(os.path.abspath(p))}")
    if "--sheet" in sys.argv:
        import contact_sheet
        contact_sheet.make()
